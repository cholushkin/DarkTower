using System;
using System.Reflection;
using UnityEngine;

namespace Game.Tests
{
    /// Builds a tiny, fully-wired DarkTowerDatabase for EditMode tests. Every Definition/record type
    /// in Data/Definitions and Data/Records is a Unity-serialization-only type (private fields, no
    /// public constructor for most nested structs), so this uses reflection purely to construct test
    /// fixtures — gameplay assertions in the actual test files always go through public service APIs,
    /// never reflection.
    public static class GameplayTestFixtures
    {
        public sealed class FixtureHandles
        {
            public DarkTowerDatabase Database;
            public RankDefinition Rank1;
            public RankDefinition Rank2;
            public ItemDefinition PlainItem;
            public ItemDefinition TreasureItem;
            public MissionDefinition Mission;
            public RoomUpgradeDefinition ExtraBedUpgrade;
            public RoomUpgradeDefinition UnlockTrainingUpgrade;
            public TrainingTransitionDefinition Rank1To2;
            public ResearchDefinition Research1;
            public GlobalBalanceDefinition GlobalBalance;
            public StorehouseRulesDefinition StorehouseRules;
            public RecoveryRulesDefinition RecoveryRules;
            public DungeonRulesDefinition DungeonRules;
        }

        public static (DarkTowerDatabase db, FixtureHandles handles) BuildMinimalDatabase() =>
            Build(requiredPower: 1f, missionId: "forest_camp");

        /// A variant whose single mission has an enormous required power, so
        /// RaidCombatRules.ComputeFailureChance clamps to 1 regardless of squad/seed — used by tests
        /// that need a deterministic raid failure rather than hunting for a magic seed.
        public static (DarkTowerDatabase db, FixtureHandles handles) BuildHardMissionDatabase() =>
            Build(requiredPower: 100000f, missionId: "siege_impossible");

        /// Two independently-launchable easy missions in tier 0 — needed only to test "a squad member
        /// already committed to another mission's raid is rejected", which the single-mission fixture
        /// can't exercise (a second launch of the SAME mission is already rejected for a different
        /// reason: mission_not_launchable).
        public static (DarkTowerDatabase db, FixtureHandles handles, MissionDefinition secondMission) BuildTwoMissionDatabase()
        {
            var (db, h) = Build(requiredPower: 1f, missionId: "forest_camp");

            var secondMission = NewSo<MissionDefinition>(so =>
            {
                SetField(so, "missionId", "lake_village");
                SetField(so, "displayName", "Lake Village");
                SetField(so, "requiredPower", 1f);
                SetField(so, "baseDuration", new GameDuration(2f, DurationUnit.Hours));
                SetField(so, "baseInjuryChance", new Probability(0f));
                SetField(so, "goldRange", new IntRange(5, 10));
                SetField(so, "crystalRange", new IntRange(0, 0));
                SetField(so, "itemRewards", Array.Empty<ItemRewardEntry>());
                SetField(so, "prisonerCountRange", new IntRange(0, 0));
                SetField(so, "prisonerRankDistribution", Array.Empty<RankWeight>());
                SetField(so, "unlockConditions", null);
                SetField(so, "squadConstraints", default(SquadConstraints));
            });

            SetField(db, "missions", new[] { h.Mission, secondMission });

            var placement1 = NewStruct<WorldMissionPlacement>(("mission", h.Mission), ("position", Vector2.zero));
            var placement2 = NewStruct<WorldMissionPlacement>(("mission", secondMission), ("position", Vector2.one));
            var tier = NewStruct<WorldTierEntry>(("tierId", "tier0"), ("missions", new[] { placement1, placement2 }));
            SetField(db.WorldMap, "tiers", new[] { tier });

            SetField(db.StartingBalance, "startingWorldUnlocks", new[]
            {
                NewStruct<WorldUnlockEntry>(("mission", h.Mission), ("initialState", MissionState.Available)),
                NewStruct<WorldUnlockEntry>(("mission", secondMission), ("initialState", MissionState.Available)),
            });

            db.InvalidateCaches();
            return (db, h, secondMission);
        }

        private static (DarkTowerDatabase, FixtureHandles) Build(float requiredPower, string missionId)
        {
            var h = new FixtureHandles();

            h.Rank1 = NewSo<RankDefinition>(so =>
            {
                SetField(so, "rank", 1);
                SetField(so, "displayName", "Rookie");
                SetField(so, "attack", 5);
                SetField(so, "defense", 5);
                SetField(so, "salaryGoldPerWeek", 10);
                SetField(so, "carryCapacity", 1.5f);
            });
            h.Rank2 = NewSo<RankDefinition>(so =>
            {
                SetField(so, "rank", 2);
                SetField(so, "displayName", "Grunt");
                SetField(so, "attack", 8);
                SetField(so, "defense", 8);
                SetField(so, "salaryGoldPerWeek", 15);
                SetField(so, "carryCapacity", 2.0f);
            });

            h.PlainItem = NewSo<ItemDefinition>(so =>
            {
                SetField(so, "itemId", "wood");
                SetField(so, "displayName", "Wood");
                SetField(so, "tags", ItemTag.Material);
                SetField(so, "maxStackSize", 10);
                SetField(so, "baseValueGold", 2);
            });
            h.TreasureItem = NewSo<ItemDefinition>(so =>
            {
                SetField(so, "itemId", "goldcup");
                SetField(so, "displayName", "Golden Cup");
                SetField(so, "tags", ItemTag.Treasure);
                SetField(so, "maxStackSize", 5);
                SetField(so, "baseValueGold", 20);
            });

            var itemReward = NewStruct<ItemRewardEntry>(
                ("item", h.PlainItem),
                ("countRange", new IntRange(1, 2)),
                ("dropChance", new Probability(1f)));

            var prisonerRankWeight = NewStruct<RankWeight>(("rank", 1), ("weight", 1f));

            h.Mission = NewSo<MissionDefinition>(so =>
            {
                SetField(so, "missionId", missionId);
                SetField(so, "displayName", "Forest Camp");
                SetField(so, "requiredPower", requiredPower);
                SetField(so, "baseDuration", new GameDuration(2f, DurationUnit.Hours));
                SetField(so, "baseInjuryChance", new Probability(0f));
                SetField(so, "goldRange", new IntRange(5, 10));
                SetField(so, "crystalRange", new IntRange(0, 0));
                SetField(so, "itemRewards", new[] { itemReward });
                SetField(so, "prisonerCountRange", new IntRange(1, 1));
                SetField(so, "prisonerRankDistribution", new[] { prisonerRankWeight });
                SetField(so, "unlockConditions", null);
                SetField(so, "squadConstraints", default(SquadConstraints));
            });

            h.ExtraBedUpgrade = NewSo<RoomUpgradeDefinition>(so =>
            {
                SetField(so, "upgradeId", "extra_bed");
                SetField(so, "roomId", RoomId.Quarters);
                SetField(so, "displayName", "Extra Bed");
                SetField(so, "cost", NewStruct<ResourceCost>(("gold", 10), ("crystals", 0), ("items", null)));
                SetField(so, "duration", new GameDuration(0.1f, DurationUnit.Hours));
                SetField(so, "effects", new[] { NewStruct<UpgradeEffect>(("kind", UpgradeEffectKind.AddQuartersBed), ("amount", 1f)) });
                SetField(so, "towerXpReward", 5L);
            });

            h.UnlockTrainingUpgrade = NewSo<RoomUpgradeDefinition>(so =>
            {
                SetField(so, "upgradeId", "unlock_training_1_2");
                SetField(so, "roomId", RoomId.TrainingHall);
                SetField(so, "displayName", "Advanced Training I");
                SetField(so, "cost", NewStruct<ResourceCost>(("gold", 5), ("crystals", 0), ("items", null)));
                SetField(so, "duration", new GameDuration(0.1f, DurationUnit.Hours));
                SetField(so, "effects", new[] { NewStruct<UpgradeEffect>(("kind", UpgradeEffectKind.UnlockTrainingTransition), ("amount", 0f), ("targetId", "1-2")) });
                SetField(so, "towerXpReward", 0L);
            });

            h.Rank1To2 = NewSo<TrainingTransitionDefinition>(so =>
            {
                SetField(so, "fromRank", 1);
                SetField(so, "toRank", 2);
                SetField(so, "cost", NewStruct<ResourceCost>(("gold", 10), ("crystals", 0), ("items", null)));
                SetField(so, "duration", new GameDuration(0.1f, DurationUnit.Hours));
            });

            h.Research1 = NewSo<ResearchDefinition>(so =>
            {
                SetField(so, "researchId", "r1");
                SetField(so, "displayName", "Sharper Blades");
                SetField(so, "researchTier", 1);
                SetField(so, "cost", NewStruct<ResourceCost>(("gold", 10), ("crystals", 0), ("items", null)));
                SetField(so, "duration", new GameDuration(0.1f, DurationUnit.Hours));
                SetField(so, "effects", new[]
                {
                    NewStruct<GlobalEffect>(
                        ("kind", GlobalEffectKind.StatModifier),
                        ("stat", CombatStat.Attack),
                        ("operation", EffectOperation.Additive),
                        ("value", 2f))
                });
            });

            h.GlobalBalance = NewSo<GlobalBalanceDefinition>(so =>
            {
                // Everything else keeps its coded default (inline field initializers run via
                // CreateInstance). Only widen the injury clamp bounds so a 0%/100% injury chance in
                // test fixtures is exact instead of floored/ceilinged at the design's provisional
                // 0.01/0.90 illustrative bounds.
                SetField(so, "injuryMin", 0f);
                SetField(so, "injuryMax", 1f);
            });

            h.StorehouseRules = NewSo<StorehouseRulesDefinition>(so =>
            {
                SetField(so, "initialStorageSlots", 5);
                SetField(so, "saleDurationRule", NewStruct<SaleDurationRule>(
                    ("baseDuration", new GameDuration(1f, DurationUnit.Hours)),
                    ("durationPerCopyHours", 0.5f)));
            });

            h.RecoveryRules = NewSo<RecoveryRulesDefinition>(_ => { }); // keep design defaults (6h/3d/20h/2d)

            h.DungeonRules = NewSo<DungeonRulesDefinition>(so =>
            {
                SetField(so, "baseProcessingDuration", new GameDuration(1f, DurationUnit.Hours));
                SetField(so, "outcomeDistribution", NewStruct<PrisonerOutcomeDistribution>(
                    ("recruitmentWeight", 1f), ("interrogationWeight", 0f), ("deathWeight", 0f)));
                SetField(so, "recruitedInjuredProbability", new Probability(0f));
            });

            var towerLevels = NewSo<TowerLevelProgressionDefinition>(so =>
            {
                SetField(so, "levels", new[]
                {
                    NewStruct<TowerLevelEntry>(("level", 1), ("xpRequired", 0L)),
                    NewStruct<TowerLevelEntry>(("level", 2), ("xpRequired", 5L)),
                });
            });

            var warRoomIntel = NewSo<WarRoomIntelligenceDefinition>(_ => { });

            var worldMap = NewSo<WorldMapDefinition>(so =>
            {
                var placement = NewStruct<WorldMissionPlacement>(("mission", h.Mission), ("position", Vector2.zero));
                var tier = NewStruct<WorldTierEntry>(("tierId", "tier0"), ("missions", new[] { placement }));
                SetField(so, "tiers", new[] { tier });
            });

            var startingBalance = NewSo<StartingBalanceDefinition>(so =>
            {
                SetField(so, "startingGold", 100);
                SetField(so, "startingCrystals", 0);
                SetField(so, "startingMinions", new[]
                {
                    NewStruct<StartingMinion>(("rank", 1), ("conditions", MinionCondition.None)),
                    NewStruct<StartingMinion>(("rank", 1), ("conditions", MinionCondition.None)),
                    NewStruct<StartingMinion>(("rank", 1), ("conditions", MinionCondition.None)),
                });
                SetField(so, "initialRoomCapabilities", NewStruct<RoomCapabilities>(
                    ("storageSlots", 5), ("quartersBeds", 2), ("infirmaryBeds", 1),
                    ("trainingStations", 1), ("dungeonCells", 2), ("unlockedResearchTier", 1)));
                SetField(so, "startingWorldUnlocks", new[]
                {
                    NewStruct<WorldUnlockEntry>(("mission", h.Mission), ("initialState", MissionState.Available))
                });
            });

            var database = NewSo<DarkTowerDatabase>(so =>
            {
                SetField(so, "ranks", new[] { h.Rank1, h.Rank2 });
                SetField(so, "items", new[] { h.PlainItem, h.TreasureItem });
                SetField(so, "missions", new[] { h.Mission });
                SetField(so, "roomUpgrades", new[] { h.ExtraBedUpgrade, h.UnlockTrainingUpgrade });
                SetField(so, "trainingTransitions", new[] { h.Rank1To2 });
                SetField(so, "research", new[] { h.Research1 });
                SetField(so, "worldMap", worldMap);
                SetField(so, "globalBalance", h.GlobalBalance);
                SetField(so, "startingBalance", startingBalance);
                SetField(so, "towerLevelProgression", towerLevels);
                SetField(so, "storehouseRules", h.StorehouseRules);
                SetField(so, "recoveryRules", h.RecoveryRules);
                SetField(so, "dungeonRules", h.DungeonRules);
                SetField(so, "warRoomIntelligence", warRoomIntel);
            });

            h.Database = database;
            database.InvalidateCaches();
            return (database, h);
        }

        private static T NewSo<T>(Action<T> configure) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            configure(instance);
            return instance;
        }

        /// Builds a boxed instance of a no-public-constructor struct by setting its private fields.
        /// Boxing is required and sufficient here: FieldInfo.SetValue on a boxed value type mutates
        /// that box in place (unlike unboxing to a local struct variable), so this correctly produces
        /// one fully-populated struct value to assign onto an enclosing field.
        public static T NewStruct<T>(params (string field, object value)[] fields) where T : struct
        {
            object boxed = Activator.CreateInstance(typeof(T));
            foreach (var (field, value) in fields) SetField(boxed, field, value);
            return (T)boxed;
        }

        public static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType()}.");
            field.SetValue(target, value);
        }
    }
}
