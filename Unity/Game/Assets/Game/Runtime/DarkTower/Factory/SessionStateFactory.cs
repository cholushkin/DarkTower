// Builds new-session runtime Models from configuration. This is DATA construction only: it creates
// the initial populations, stacks and per-mission progress a fresh run starts with. It does not run
// timers, apply upgrade/research effects, or orchestrate gameplay initialization.
//
// DECISION: initial_room_capabilities is treated as the authoritative starting capacities. Starting
//   completed upgrades/research are recorded (for prerequisite/ownership checks) but their effects
//   are NOT re-applied here, because applying effects is gameplay logic owned by a future service.
//   A new session and a restored session must end up with the same Model APIs (see DataFlow).

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
            float nightfall = db.RecoveryRules != null ? db.RecoveryRules.NightfallHourOfDay : 20f;

            var clock = new ClockModel(hoursPerDay, daysPerWeek, nightfall);
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

            var research = new ResearchModel(db);
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

            return new GameSessionModel(db, ids, clock, economy, tower, roster, inventory,
                dungeon, research, world, recovery, training, intelligence, payroll);
        }

        private static void AddStartingStacks(InventoryModel inventory, EntityIdAllocator ids, ItemAmount entry)
        {
            var item = entry.Item;
            if (item == null) return;
            int remaining = entry.Amount;
            int max = item.MaxStackSize < 1 ? 1 : item.MaxStackSize;
            while (remaining > 0)
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
