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
            var worldMap = Db.WorldMap;
            if (worldMap == null) return;
            int currentTier = World.CurrentTierIndex.CurrentValue;

            var missions = World.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                var progress = missions[i];
                if (progress.State.CurrentValue != MissionState.Locked) continue;

                var missionDef = Db.GetMission(progress.MissionId);
                if (missionDef == null) continue;

                int tierIndex = worldMap.FindTierIndexOf(missionDef);
                if (tierIndex < 0 || tierIndex > currentTier) continue;

                if (UnlockConditionRules.AllSatisfied(missionDef.UnlockConditions, _session))
                    progress.SetState(MissionState.Available);
            }
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
