using VitalRouter;

namespace Game
{
    /// Mission accessibility, weekly launch cooldown, tier unlocks and victory. RaidService calls the
    /// small state-transition helpers here (SetMissionRunning / SetMissionCompletedUnclaimed /
    /// OnMissionClaimed) rather than mutating MissionProgressModel directly, so every mission-state
    /// transition funnels through one place.
    public sealed class WorldProgressionService
    {
        private readonly GameSessionModel _session;
        private readonly ICommandPublisher _publisher;

        public WorldProgressionService(GameSessionModel session, ICommandPublisher publisher)
        {
            _session = session;
            _publisher = publisher;
        }

        private WorldModel World => _session.World;
        private DarkTowerDatabase Db => _session.Database;

        public bool IsLaunchable(string missionId, int currentWeek)
        {
            var progress = World.FindMission(missionId);
            if (progress == null) return false;
            if (progress.State.CurrentValue != MissionState.Available) return false;
            if (progress.LastLaunchWeek.HasValue && progress.LastLaunchWeek.Value >= currentWeek) return false;
            return true;
        }

        public void SetMissionRunning(string missionId, int launchWeek)
        {
            var progress = World.FindMission(missionId);
            if (progress == null) return;
            progress.SetState(MissionState.Running);
            progress.SetLastLaunchWeek(launchWeek);
        }

        public void SetMissionCompletedUnclaimed(string missionId)
        {
            var progress = World.FindMission(missionId);
            if (progress != null) progress.SetState(MissionState.CompletedUnclaimed);
        }

        /// Called once by RaidService.TryClaimRaid after it has resolved currency/item/prisoner
        /// effects. Marks first-success, derives the mission's next visible state, advances tiers and
        /// checks victory.
        public void OnMissionClaimed(string missionId, bool success, int currentWeek, int launchWeek)
        {
            var progress = World.FindMission(missionId);
            if (progress == null) return;

            if (success) progress.MarkCompletedOnce();

            progress.SetState(currentWeek <= launchWeek ? MissionState.Cooldown : MissionState.Available);

            if (!success) return;

            TryAdvanceTierIfComplete();
            RefreshAccessibility();
            CheckVictory();
        }

        /// Promotes every currently Locked mission whose tier is accessible and authored unlock
        /// conditions are satisfied to Available. Call after anything that could change those
        /// conditions (Claim, an interrogation event setting a world flag, a tier unlock).
        public void RefreshAccessibility()
        {
            var missions = World.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                var progress = missions[i];
                if (progress.State.CurrentValue != MissionState.Locked) continue;

                var missionDef = Db.GetMission(progress.MissionId);
                if (IsAccessible(missionDef)) progress.SetState(MissionState.Available);
            }
        }

        /// Called by GameScheduler whenever the mission week advances. A mission claimed within its
        /// own launch week sits in Cooldown until a later week begins (see OnMissionClaimed); this
        /// reopens any such mission to Available now that it is launchable again, without bypassing
        /// the same tier/unlock-condition checks RefreshAccessibility enforces for Locked missions.
        public void OnWeekAdvanced(int currentWeek)
        {
            var missions = World.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                var progress = missions[i];
                if (progress.State.CurrentValue != MissionState.Cooldown) continue;
                if (progress.LastLaunchWeek.HasValue && progress.LastLaunchWeek.Value >= currentWeek) continue;

                var missionDef = Db.GetMission(progress.MissionId);
                if (IsAccessible(missionDef)) progress.SetState(MissionState.Available);
            }
        }

        /// Opens a Locked mission that an interrogation event explicitly names in its authored
        /// UnlocksMissions list. The event's unlock is independent of that mission's own
        /// UnlockConditions (the event itself is the unlock path, e.g. "a secret reveals this
        /// location"), but still respects tier accessibility — a secret cannot spring open a mission
        /// in a tier the player has not reached. Provisional: the exact interaction between
        /// event-driven unlocks and tier gating is not finalized by the design.
        public bool TryUnlockMissionFromEvent(string missionId)
        {
            var progress = World.FindMission(missionId);
            if (progress == null || progress.State.CurrentValue != MissionState.Locked) return false;

            var worldMap = Db.WorldMap;
            var missionDef = Db.GetMission(missionId);
            if (worldMap == null || missionDef == null) return false;

            int tierIndex = worldMap.FindTierIndexOf(missionDef);
            if (tierIndex < 0 || tierIndex > World.CurrentTierIndex.CurrentValue) return false;

            progress.SetState(MissionState.Available);
            return true;
        }

        private bool IsAccessible(MissionDefinition missionDef)
        {
            var worldMap = Db.WorldMap;
            if (worldMap == null || missionDef == null) return false;

            int tierIndex = worldMap.FindTierIndexOf(missionDef);
            if (tierIndex < 0 || tierIndex > World.CurrentTierIndex.CurrentValue) return false;

            return UnlockConditionRules.AllSatisfied(missionDef.UnlockConditions, _session);
        }

        private void TryAdvanceTierIfComplete()
        {
            var tiers = Db.WorldMap != null ? Db.WorldMap.Tiers : null;
            if (tiers == null) return;

            while (World.CurrentTierIndex.CurrentValue < tiers.Length - 1)
            {
                int idx = World.CurrentTierIndex.CurrentValue;
                if (!IsTierFullyCompleted(tiers[idx])) break;
                World.SetCurrentTierIndex(idx + 1);
                _ = _publisher.PublishAsync(new WorldTierUnlockedEvent(idx + 1));
            }
        }

        private bool IsTierFullyCompleted(WorldTierEntry tier)
        {
            var missions = tier.Missions;
            if (missions == null) return true;
            for (int i = 0; i < missions.Length; i++)
            {
                var missionDef = missions[i].Mission;
                if (missionDef == null) continue;
                var progress = World.FindMission(missionDef.Id);
                if (progress == null || !progress.CompletedOnce.CurrentValue) return false;
            }
            return true;
        }

        private void CheckVictory()
        {
            if (_session.IsTerminal) return;

            var missions = World.Missions;
            if (missions.Count == 0) return;

            for (int i = 0; i < missions.Count; i++)
                if (!missions[i].CompletedOnce.CurrentValue) return;

            _session.DeclareVictory();
            _ = _publisher.PublishAsync(new VictoryAchievedEvent());
        }
    }
}
