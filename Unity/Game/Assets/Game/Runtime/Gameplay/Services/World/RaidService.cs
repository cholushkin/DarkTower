using System.Collections.Generic;
using VitalRouter;

namespace Game
{
    /// Raid launch validation, timed resolution, and Claim. Outcome/discoveries are fixed once at
    /// ProcessDue (no reroll on reopening the report); return conditions (Tired/independent Injured
    /// roll) are rolled once at Claim, applied exactly at that transition — this is a single roll
    /// either way (Claim is single-use via RaidModel's Claimed flag), so nothing here can double-roll
    /// or reroll.
    public sealed class RaidService : ITimedProcess
    {
        private readonly GameSessionModel _session;
        private readonly MinionService _minions;
        private readonly EconomyService _economy;
        private readonly InventoryService _inventory;
        private readonly DungeonService _dungeon;
        private readonly WorldProgressionService _world;
        private readonly ClockService _clock;
        private readonly IRandomSource _random;
        private readonly ICommandPublisher _publisher;

        public RaidService(
            GameSessionModel session,
            MinionService minions,
            EconomyService economy,
            InventoryService inventory,
            DungeonService dungeon,
            WorldProgressionService world,
            ClockService clock,
            IRandomSource random,
            ICommandPublisher publisher)
        {
            _session = session;
            _minions = minions;
            _economy = economy;
            _inventory = inventory;
            _dungeon = dungeon;
            _world = world;
            _clock = clock;
            _random = random;
            _publisher = publisher;
        }

        private DarkTowerDatabase Db => _session.Database;
        private GlobalBalanceDefinition Balance => Db.GlobalBalance;

        public OperationResult<EntityId> TryStartRaid(string missionId, IReadOnlyList<EntityId> squadIds)
        {
            if (_session.IsTerminal) return OperationResult<EntityId>.Fail("game_over");

            var missionDef = Db.GetMission(missionId);
            if (missionDef == null) return OperationResult<EntityId>.Fail("mission_not_found");

            int currentWeek = _clock.CurrentWeek;
            if (!_world.IsLaunchable(missionId, currentWeek)) return OperationResult<EntityId>.Fail("mission_not_launchable");

            if (squadIds == null || squadIds.Count == 0) return OperationResult<EntityId>.Fail("empty_squad");

            var seen = new HashSet<EntityId>();
            for (int i = 0; i < squadIds.Count; i++)
                if (!seen.Add(squadIds[i])) return OperationResult<EntityId>.Fail("duplicate_squad_member");

            var constraints = missionDef.SquadConstraints;
            if (constraints.MinSquadSize > 0 && squadIds.Count < constraints.MinSquadSize)
                return OperationResult<EntityId>.Fail("squad_too_small");
            if (constraints.MaxSquadSize > 0 && squadIds.Count > constraints.MaxSquadSize)
                return OperationResult<EntityId>.Fail("squad_too_large");

            var squad = new MinionModel[squadIds.Count];
            for (int i = 0; i < squadIds.Count; i++)
            {
                var minion = _minions.Find(squadIds[i]);
                if (minion == null) return OperationResult<EntityId>.Fail("minion_not_found");
                if (!minion.IsAvailableForAssignment) return OperationResult<EntityId>.Fail("minion_unavailable");
                squad[i] = minion;
            }

            var members = new RaidMember[squad.Length];
            var carryCapacities = new float[squad.Length];
            for (int i = 0; i < squad.Length; i++)
            {
                var stats = _minions.GetEffectiveStats(squad[i]);
                float carry = _minions.GetCarryCapacity(squad[i]);
                members[i] = new RaidMember(squad[i].Id, squad[i].Rank.CurrentValue, stats, carry);
                carryCapacities[i] = carry;
            }
            int carryLimit = CarryRules.ComputeCarryLimit(carryCapacities);
            var snapshot = new RaidLaunchSnapshot(members, carryLimit);

            float now = _clock.NowHours;
            float durationHours = RaidCombatRules.ComputeDurationHours(missionDef, snapshot, Balance, _clock.GameHoursPerDay);

            for (int i = 0; i < squad.Length; i++) squad[i].SetActivity(MinionActivity.Raiding);
            _world.SetMissionRunning(missionId, currentWeek);

            var raidId = _session.IdAllocator.Next();
            var raid = new RaidModel(raidId, missionId, squadIds, currentWeek, now, now + durationHours, snapshot);

            var progress = _session.World.FindMission(missionId);
            progress?.SetCurrentRaid(raid);

            return OperationResult<EntityId>.Ok(raidId);
        }

        /// Resolves every raid whose timer is due at or before nowHours: fixes success/failure and,
        /// on success, fixes discoveries. Never touches the squad or credits anything (Claim only).
        public void ProcessDue(float nowHours)
        {
            var missions = _session.World.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                var progress = missions[i];
                var raid = progress.CurrentRaid.CurrentValue;
                if (raid == null) continue;
                if (raid.Outcome.CurrentValue != RaidOutcome.Pending) continue;
                if (raid.DueGameHours > nowHours) continue;

                var missionDef = Db.GetMission(raid.MissionId);
                float powerRatio = RaidCombatRules.ComputePowerRatio(raid.LaunchSnapshot, missionDef, Balance);
                float failureChance = RaidCombatRules.ComputeFailureChance(powerRatio, Balance);
                bool success = !_random.NextBool(failureChance);

                if (success) ResolveSuccess(raid, missionDef);
                else raid.ResolveFailure();

                _world.SetMissionCompletedUnclaimed(raid.MissionId);
                _ = _publisher.PublishAsync(new RaidFinishedEvent(raid.Id, raid.Outcome.CurrentValue));
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var missions = _session.World.Missions;
            for (int i = 0; i < missions.Count; i++)
            {
                var raid = missions[i].CurrentRaid.CurrentValue;
                if (raid == null || raid.Outcome.CurrentValue != RaidOutcome.Pending) continue;
                if (min == null || raid.DueGameHours < min.Value) min = raid.DueGameHours;
            }
            return min;
        }

        private void ResolveSuccess(RaidModel raid, MissionDefinition missionDef)
        {
            int gold = missionDef.GoldRange.IsValid ? _random.NextInt(missionDef.GoldRange.Min, missionDef.GoldRange.Max) : 0;
            int crystals = missionDef.CrystalRange.IsValid ? _random.NextInt(missionDef.CrystalRange.Min, missionDef.CrystalRange.Max) : 0;

            var items = new List<DiscoveredItemCopy>();
            var rewards = missionDef.ItemRewards;
            if (rewards != null)
            {
                for (int r = 0; r < rewards.Length; r++)
                {
                    var reward = rewards[r];
                    if (reward.Item == null) continue;
                    if (!_random.NextBool(reward.DropChance.Value)) continue;
                    int count = reward.CountRange.IsValid ? _random.NextInt(reward.CountRange.Min, reward.CountRange.Max) : 0;
                    for (int c = 0; c < count; c++)
                        items.Add(new DiscoveredItemCopy(_session.IdAllocator.Next(), reward.Item, false));
                }
            }

            var prisoners = new List<CapturedPrisoner>();
            if (missionDef.PrisonerCountRange.IsValid && missionDef.PrisonerRankDistribution != null && missionDef.PrisonerRankDistribution.Length > 0)
            {
                int count = _random.NextInt(missionDef.PrisonerCountRange.Min, missionDef.PrisonerCountRange.Max);
                for (int p = 0; p < count; p++)
                    prisoners.Add(new CapturedPrisoner(RollPrisonerRank(missionDef.PrisonerRankDistribution)));
            }

            // Preselect a valid within-budget subset so the Raid Report has a sensible default
            // instead of exposing every discovered copy unselected; the player may still change it
            // before Claim. RaidClaimPolicy's greedy discovery-order fill is a provisional default,
            // not a finalized selection-priority rule.
            var preselected = RaidClaimPolicy.DefaultPreselect(items, raid.CarryLimit);
            if (preselected.Count > 0)
            {
                var preselectedSet = new HashSet<EntityId>(preselected);
                for (int i = 0; i < items.Count; i++)
                    if (preselectedSet.Contains(items[i].Id)) items[i].SetSelected(true);
            }

            raid.ResolveSuccess(gold, crystals, items, prisoners);
        }

        private int RollPrisonerRank(RankWeight[] distribution)
        {
            float total = 0f;
            for (int i = 0; i < distribution.Length; i++) total += distribution[i].Weight;
            if (total <= 0f) return distribution[0].Rank;

            float roll = _random.NextFloat01() * total;
            float cumulative = 0f;
            for (int i = 0; i < distribution.Length; i++)
            {
                cumulative += distribution[i].Weight;
                if (roll < cumulative) return distribution[i].Rank;
            }
            return distribution[distribution.Length - 1].Rank;
        }

        /// Validates the selection (only discovered copies, no duplicates, within the carry budget),
        /// then — on first Claim only — credits currencies, inserts selected items (overflow silently
        /// discarded), admits prisoners, applies return conditions, releases the squad, marks
        /// first-success/tier/victory progress, and marks the raid Claimed. Idempotent: a second call
        /// fails with "already_claimed" and changes nothing.
        public OperationResult TryClaimRaid(string missionId, EntityId raidId, IReadOnlyList<EntityId> selectedCopyIds)
        {
            var progress = _session.World.FindMission(missionId);
            var raid = progress?.CurrentRaid.CurrentValue;
            if (raid == null || !raid.Id.Equals(raidId)) return OperationResult.Fail("raid_not_found");
            if (raid.Claimed.CurrentValue) return OperationResult.Fail("already_claimed");
            if (raid.Outcome.CurrentValue == RaidOutcome.Pending) return OperationResult.Fail("raid_not_finished");

            // Checked only after the identity/idempotency checks above: claiming the last mission can
            // itself end the game (Victory), and a repeat Claim of that same raid afterward must still
            // report "already_claimed", not a generic game_over, since it mutates nothing either way.
            if (_session.IsTerminal) return OperationResult.Fail("game_over");

            var discovered = raid.DiscoveredItems;
            var selectedSet = new HashSet<EntityId>();
            if (selectedCopyIds != null)
            {
                for (int i = 0; i < selectedCopyIds.Count; i++)
                {
                    if (!selectedSet.Add(selectedCopyIds[i])) return OperationResult.Fail("duplicate_selection");
                }
            }

            int carryUsed = 0;
            if (selectedSet.Count > 0)
            {
                var byId = new Dictionary<EntityId, DiscoveredItemCopy>();
                for (int i = 0; i < discovered.Count; i++) byId[discovered[i].Id] = discovered[i];

                var selectedItems = new List<ItemDefinition>();
                foreach (var id in selectedSet)
                {
                    if (!byId.TryGetValue(id, out var copy)) return OperationResult.Fail("invalid_selection");
                    selectedItems.Add(copy.Item);
                }
                carryUsed = RaidCarryRules.SlotsUsed(selectedItems);
            }
            if (carryUsed > raid.CarryLimit) return OperationResult.Fail("carry_budget_exceeded");

            for (int i = 0; i < discovered.Count; i++)
                discovered[i].SetSelected(selectedSet.Contains(discovered[i].Id));

            bool success = raid.Outcome.CurrentValue == RaidOutcome.Success;
            float now = _clock.NowHours;

            if (success)
            {
                _economy.CreditGold(raid.DiscoveredGold);
                _economy.CreditCrystals(raid.DiscoveredCrystals);

                for (int i = 0; i < discovered.Count; i++)
                {
                    if (discovered[i].Selected.CurrentValue) _inventory.TryInsert(discovered[i].Item, 1);
                }

                _dungeon.AdmitPrisoners(raid.CapturedPrisoners, now);
            }

            ApplyReturnConditions(raid, success, now);

            raid.MarkClaimed();
            _world.OnMissionClaimed(missionId, success, _clock.CurrentWeek, raid.LaunchWeek);

            return OperationResult.Ok;
        }

        private void ApplyReturnConditions(RaidModel raid, bool success, float now)
        {
            var missionDef = Db.GetMission(raid.MissionId);
            float powerRatio = success ? RaidCombatRules.ComputePowerRatio(raid.LaunchSnapshot, missionDef, Balance) : 0f;
            float baseInjuryChance = missionDef != null ? missionDef.BaseInjuryChance.Value : 0f;

            var members = raid.LaunchSnapshot.Members;
            for (int i = 0; i < raid.ReservedSquad.Count; i++)
            {
                var minion = _session.Roster.Find(raid.ReservedSquad[i]);
                if (minion == null) continue;

                minion.SetActivity(MinionActivity.Idle);
                minion.AddCondition(MinionCondition.Tired, now);

                if (!success)
                {
                    minion.AddCondition(MinionCondition.Injured, now);
                    continue;
                }

                var member = FindMember(members, raid.ReservedSquad[i]);
                float injuryChance = RaidCombatRules.ComputeInjuryChance(member, powerRatio, baseInjuryChance, Balance);
                if (_random.NextBool(injuryChance)) minion.AddCondition(MinionCondition.Injured, now);
            }
        }

        private static RaidMember FindMember(RaidMember[] members, EntityId minionId)
        {
            if (members == null) return default;
            for (int i = 0; i < members.Length; i++)
                if (members[i].MinionId.Equals(minionId)) return members[i];
            return default;
        }
    }
}
