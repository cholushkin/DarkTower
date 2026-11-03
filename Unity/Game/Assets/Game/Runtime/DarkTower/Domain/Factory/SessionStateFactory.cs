// Builds new-session runtime Models from configuration. This creates the initial populations,
// stacks and per-mission progress a fresh run starts with, then applies each starting completed
// upgrade's capacity/unlock effects on top through the same UpgradeEffectApplier path
// TowerUpgradeService uses at runtime completion, so a bonus is never missing just because its
// upgrade happened to be pre-completed at session start. It does not run timers or otherwise
// orchestrate gameplay initialization.
//
// InitialRoomCapabilities is the authoritative BASE starting capacities, before any starting
// completed upgrade's effects are applied on top. Starting completed research is recorded (for
// prerequisite/ownership checks) but carries no capacity effects of its own to apply. A new session
// and a restored session must end up with the same Model APIs (see DataFlow.md).

using System.Collections.Generic;

namespace Game
{
    public static class SessionStateFactory
    {
        public static GameSessionModel CreateNewSession(DarkTowerDatabase db)
        {
            if (db == null) return null;

            var ids = new EntityIdAllocator();
            var balance = db.GlobalBalance;
            var start = db.StartingBalance;
            var caps = start != null ? start.InitialRoomCapabilities : default;

            float hoursPerDay = balance != null ? balance.GameHoursPerDay : 24f;
            int daysPerWeek = balance != null ? balance.DaysPerWeek : 7;
            int paydayIntervalDays = balance != null ? balance.PaydayIntervalDays : 7;
            float nightfall = db.RecoveryRules != null ? db.RecoveryRules.NightfallHourOfDay : 20f;

            var clock = new ClockModel(hoursPerDay, daysPerWeek, nightfall, paydayIntervalDays);
            var economy = new EconomyModel(
                start != null ? start.StartingGold : 0,
                start != null ? start.StartingCrystals : 0);

            var tower = new TowerModel(start != null ? start.StartingTowerXp : 0, db.TowerLevelProgression);
            if (start?.StartingCompletedUpgrades != null)
            {
                foreach (var up in start.StartingCompletedUpgrades)
                    if (up != null) tower.MarkUpgradeCompleted(up.Id);
            }

            var roster = new MinionRosterModel();
            if (start?.StartingMinions != null)
            {
                foreach (var sm in start.StartingMinions)
                    roster.Add(new MinionModel(ids.Next(), sm.Rank, sm.Conditions, 0f));
            }

            int storageSlots = start != null ? caps.StorageSlots : 0;
            if (storageSlots <= 0 && db.StorehouseRules != null) storageSlots = db.StorehouseRules.InitialStorageSlots;
            var inventory = new InventoryModel(storageSlots);
            if (start?.StartingItemStacks != null)
            {
                foreach (var entry in start.StartingItemStacks)
                    AddStartingStacks(inventory, ids, entry);
            }

            var dungeon = new DungeonModel(caps.DungeonCells);

            var research = new ResearchModel(db, caps.UnlockedResearchTier);
            if (start?.StartingCompletedResearch != null)
            {
                foreach (var r in start.StartingCompletedResearch)
                    if (r != null) research.MarkCompleted(r.Id);
            }

            var world = BuildWorld(db, start);

            var recovery = new RecoveryModel(caps.QuartersBeds, caps.InfirmaryBeds);
            var training = new TrainingModel(caps.TrainingStations);
            var intelligence = new IntelligenceModel();
            var payroll = new PayrollModel();

            var session = new GameSessionModel(db, ids, clock, economy, tower, roster, inventory,
                dungeon, research, world, recovery, training, intelligence, payroll);

            // Applied after the session is fully constructed (every sub-model it touches must exist
            // first): each starting completed upgrade's capacity/unlock effects on top of the base
            // capacities above, exactly once, through the same path a runtime purchase completion
            // uses — see UpgradeEffectApplier. TowerXpReward is deliberately excluded (see there).
            if (start?.StartingCompletedUpgrades != null)
            {
                foreach (var up in start.StartingCompletedUpgrades)
                    if (up != null) UpgradeEffectApplier.Apply(session, up);
            }

            return session;
        }

        /// Creates starting stacks up to the session's storage capacity. Authored starting stacks
        /// that would exceed StorageSlots stop filling once slots run out rather than producing an
        /// over-capacity InventoryModel; that mismatch is a configuration problem for
        /// DarkTowerDataValidator to flag, not a runtime state this factory should ever create.
        private static void AddStartingStacks(InventoryModel inventory, EntityIdAllocator ids, ItemAmount entry)
        {
            var item = entry.Item;
            if (item == null) return;
            int remaining = entry.Amount;
            int max = item.MaxStackSize < 1 ? 1 : item.MaxStackSize;
            while (remaining > 0 && inventory.FreeSlots > 0)
            {
                int inThisStack = remaining < max ? remaining : max;
                inventory.AddStack(new ItemStackModel(ids.Next(), item, inThisStack));
                remaining -= inThisStack;
            }
        }

        private static WorldModel BuildWorld(DarkTowerDatabase db, StartingBalanceDefinition start)
        {
            var world = new WorldModel(0);

            // Initial accessibility authored per mission; everything else defaults to Locked.
            var initialStates = new Dictionary<string, MissionState>();
            if (start?.StartingWorldUnlocks != null)
            {
                foreach (var u in start.StartingWorldUnlocks)
                    if (u.Mission != null) initialStates[u.Mission.Id] = u.InitialState;
            }

            if (db.Missions != null)
            {
                foreach (var mission in db.Missions)
                {
                    if (mission == null) continue;
                    var state = initialStates.TryGetValue(mission.Id, out var s) ? s : MissionState.Locked;
                    world.AddMission(new MissionProgressModel(mission.Id, state));
                }
            }

            if (start?.StartingSecretFlags != null)
            {
                foreach (var flag in start.StartingSecretFlags) world.SetFlag(flag);
            }

            return world;
        }
    }
}
