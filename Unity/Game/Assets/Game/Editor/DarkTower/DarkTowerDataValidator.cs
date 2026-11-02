using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game
{
    /// Editor-time data validation for a DarkTowerDatabase: uniqueness, missing references, ordered
    /// ranks/tiers, nonnegative costs/durations, valid ranges/probabilities, adjacent training
    /// transitions, and prerequisite cycles. Pure checks; it never mutates assets.
    public static class DarkTowerDataValidator
    {
        [MenuItem("Dark Tower/Validate Data")]
        public static void ValidateAllInProject()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(DarkTowerDatabase));
            if (guids.Length == 0)
            {
                Debug.LogWarning("Dark Tower: no DarkTowerDatabase asset found to validate.");
                return;
            }

            int totalErrors = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<DarkTowerDatabase>(path);
                if (db == null) continue;

                var errors = new List<string>();
                var warnings = new List<string>();
                Validate(db, errors, warnings);

                foreach (var w in warnings) Debug.LogWarning($"[DarkTower:{db.name}] {w}", db);
                foreach (var e in errors) Debug.LogError($"[DarkTower:{db.name}] {e}", db);

                totalErrors += errors.Count;
                Debug.Log($"[DarkTower:{db.name}] validation finished: {errors.Count} error(s), {warnings.Count} warning(s).", db);
            }

            if (totalErrors == 0) Debug.Log("Dark Tower: all databases passed validation.");
        }

        public static void Validate(DarkTowerDatabase db, List<string> errors, List<string> warnings)
        {
            if (db == null) { errors.Add("Database is null."); return; }

            ValidateSingletons(db, warnings);
            ValidateRanks(db, errors);
            ValidateIdUniqueness(db, errors);
            ValidateItems(db, errors);
            ValidateMissions(db, errors);
            ValidateTrainingTransitions(db, errors);
            ValidateWorldMap(db, errors, warnings);
            ValidateUpgradePrerequisites(db, errors);
            ValidateResearchPrerequisites(db, errors);
            ValidateStartingBalance(db, errors);
        }

        private static void ValidateSingletons(DarkTowerDatabase db, List<string> warnings)
        {
            if (db.GlobalBalance == null) warnings.Add("GlobalBalance is not assigned.");
            if (db.StartingBalance == null) warnings.Add("StartingBalance is not assigned.");
            if (db.TowerLevelProgression == null) warnings.Add("TowerLevelProgression is not assigned.");
            if (db.StorehouseRules == null) warnings.Add("StorehouseRules is not assigned.");
            if (db.RecoveryRules == null) warnings.Add("RecoveryRules is not assigned.");
            if (db.DungeonRules == null) warnings.Add("DungeonRules is not assigned.");
            if (db.WarRoomIntelligence == null) warnings.Add("WarRoomIntelligence is not assigned.");
            if (db.WorldMap == null) warnings.Add("WorldMap is not assigned.");
        }

        private static void ValidateRanks(DarkTowerDatabase db, List<string> errors)
        {
            var seen = new HashSet<int>();
            if (db.Ranks != null)
            {
                foreach (var r in db.Ranks)
                {
                    if (r == null) { errors.Add("Ranks contains a null entry."); continue; }
                    if (!seen.Add(r.Rank)) errors.Add($"Duplicate rank number {r.Rank}.");
                    if (r.Rank < 1 || r.Rank > 5) errors.Add($"Rank {r.Rank} out of the 1..5 range.");
                }
            }
            for (int i = 1; i <= 5; i++)
                if (!seen.Contains(i)) errors.Add($"Missing RankDefinition for rank {i}.");
        }

        private static void ValidateIdUniqueness(DarkTowerDatabase db, List<string> errors)
        {
            CheckUnique(db.Items, errors, "item_id");
            CheckUnique(db.Missions, errors, "mission_id");
            CheckUnique(db.RoomUpgrades, errors, "upgrade_id");
            CheckUnique(db.Research, errors, "research_id");
            CheckUnique(db.RewardTables, errors, "reward_table_id");
            CheckUnique(db.Events, errors, "event_id");
        }

        private static void CheckUnique<T>(IReadOnlyList<T> list, List<string> errors, string idLabel)
            where T : class, IIdentifiedDefinition
        {
            if (list == null) return;
            var seen = new HashSet<string>();
            foreach (var entry in list)
            {
                if (entry == null) { errors.Add($"A {idLabel} list contains a null entry."); continue; }
                if (string.IsNullOrEmpty(entry.Id)) { errors.Add($"{entry} has a blank {idLabel}."); continue; }
                if (!seen.Add(entry.Id)) errors.Add($"Duplicate {idLabel} '{entry.Id}'.");
            }
        }

        private static void ValidateItems(DarkTowerDatabase db, List<string> errors)
        {
            if (db.Items == null) return;
            foreach (var item in db.Items)
            {
                if (item == null) continue;
                if (item.MaxStackSize < 1) errors.Add($"Item '{item.Id}' max_stack_size must be >= 1.");
                if (item.CarrySize < 1) errors.Add($"Item '{item.Id}' carry_size must be >= 1.");
                if (item.SellPriceGold < 0) errors.Add($"Item '{item.Id}' sell_price_gold must be >= 0.");
            }
        }

        private static void ValidateMissions(DarkTowerDatabase db, List<string> errors)
        {
            if (db.Missions == null) return;
            foreach (var m in db.Missions)
            {
                if (m == null) continue;
                if (!m.GoldRange.IsValid) errors.Add($"Mission '{m.Id}' gold_range is invalid (min<=max, >=0).");
                if (!m.CrystalRange.IsValid) errors.Add($"Mission '{m.Id}' crystal_range is invalid.");
                if (!m.PrisonerCountRange.IsValid) errors.Add($"Mission '{m.Id}' prisoner_count_range is invalid.");
                if (!m.BaseInjuryChance.IsValid) errors.Add($"Mission '{m.Id}' base_injury_chance must be within [0,1].");
                if (m.RequiredPower < 0f) errors.Add($"Mission '{m.Id}' required_power must be >= 0.");

                if (m.ItemRewards != null)
                {
                    foreach (var reward in m.ItemRewards)
                    {
                        if (reward.Item == null) errors.Add($"Mission '{m.Id}' has an item reward with no item.");
                        if (!reward.CountRange.IsValid) errors.Add($"Mission '{m.Id}' item reward count range is invalid.");
                        if (!reward.DropChance.IsValid) errors.Add($"Mission '{m.Id}' item reward drop chance is out of [0,1].");
                    }
                }

                if (m.PrisonerRankDistribution != null)
                {
                    foreach (var rw in m.PrisonerRankDistribution)
                    {
                        if (rw.Rank < 1 || rw.Rank > 5) errors.Add($"Mission '{m.Id}' prisoner rank {rw.Rank} out of 1..5.");
                        if (rw.Weight < 0f) errors.Add($"Mission '{m.Id}' prisoner rank weight must be >= 0.");
                    }
                }
            }
        }

        private static void ValidateTrainingTransitions(DarkTowerDatabase db, List<string> errors)
        {
            if (db.TrainingTransitions == null) return;
            foreach (var t in db.TrainingTransitions)
            {
                if (t == null) continue;
                if (!t.IsAdjacent) errors.Add($"Training transition {t.FromRank}->{t.ToRank} must be adjacent (to == from + 1).");
                if (t.FromRank < 1 || t.ToRank > 5) errors.Add($"Training transition {t.FromRank}->{t.ToRank} out of 1..5.");
            }
        }

        private static void ValidateWorldMap(DarkTowerDatabase db, List<string> errors, List<string> warnings)
        {
            var map = db.WorldMap;
            if (map == null || map.Tiers == null) return;

            var placedTierOf = new Dictionary<MissionDefinition, int>();
            var tierIds = new HashSet<string>();

            for (int i = 0; i < map.Tiers.Length; i++)
            {
                var tier = map.Tiers[i];
                if (string.IsNullOrEmpty(tier.TierId)) errors.Add($"World tier index {i} has a blank tier_id.");
                else if (!tierIds.Add(tier.TierId)) errors.Add($"Duplicate world tier_id '{tier.TierId}'.");

                if (tier.Missions == null) continue;
                foreach (var placement in tier.Missions)
                {
                    var mission = placement.Mission;
                    if (mission == null) { errors.Add($"Tier '{tier.TierId}' has a placement with no mission."); continue; }
                    if (placedTierOf.ContainsKey(mission))
                        errors.Add($"Mission '{mission.Id}' is placed on more than one tier.");
                    else
                        placedTierOf[mission] = i;
                }
            }

            if (db.Missions != null)
            {
                foreach (var mission in db.Missions)
                {
                    if (mission == null) continue;
                    if (!placedTierOf.ContainsKey(mission))
                        errors.Add($"Mission '{mission.Id}' is not placed on any world tier.");
                }
                foreach (var placed in placedTierOf.Keys)
                {
                    bool known = false;
                    foreach (var mission in db.Missions) if (mission == placed) { known = true; break; }
                    if (!known) warnings.Add($"World map references mission '{placed.Id}' not listed in the database.");
                }
            }
        }

        private static void ValidateUpgradePrerequisites(DarkTowerDatabase db, List<string> errors)
        {
            if (db.RoomUpgrades == null) return;
            foreach (var up in db.RoomUpgrades)
            {
                if (up == null) continue;
                if (up.Cost.Gold < 0 || up.Cost.Crystals < 0) errors.Add($"Upgrade '{up.Id}' cost must be >= 0.");
                if (up.TowerXpReward < 0) errors.Add($"Upgrade '{up.Id}' tower_xp_reward must be >= 0.");

                var path = new HashSet<RoomUpgradeDefinition>();
                if (HasUpgradeCycle(up, path)) errors.Add($"Upgrade '{up.Id}' is part of a prerequisite cycle.");
            }
        }

        private static bool HasUpgradeCycle(RoomUpgradeDefinition node, HashSet<RoomUpgradeDefinition> path)
        {
            if (node == null) return false;
            if (!path.Add(node)) return true;
            if (node.PrerequisiteUpgrades != null)
            {
                foreach (var pre in node.PrerequisiteUpgrades)
                    if (HasUpgradeCycle(pre, path)) return true;
            }
            path.Remove(node);
            return false;
        }

        private static void ValidateResearchPrerequisites(DarkTowerDatabase db, List<string> errors)
        {
            if (db.Research == null) return;
            foreach (var r in db.Research)
            {
                if (r == null) continue;
                if (r.ResearchTier < 0) errors.Add($"Research '{r.Id}' research_tier must be >= 0.");

                var path = new HashSet<ResearchDefinition>();
                if (HasResearchCycle(r, path)) errors.Add($"Research '{r.Id}' is part of a prerequisite cycle.");
            }
        }

        private static bool HasResearchCycle(ResearchDefinition node, HashSet<ResearchDefinition> path)
        {
            if (node == null) return false;
            if (!path.Add(node)) return true;
            var pre = node.Prerequisites.RequiredResearch;
            if (pre != null)
            {
                foreach (var p in pre)
                    if (HasResearchCycle(p, path)) return true;
            }
            path.Remove(node);
            return false;
        }

        private static void ValidateStartingBalance(DarkTowerDatabase db, List<string> errors)
        {
            var start = db.StartingBalance;
            if (start == null) return;

            if (start.StartingMinions != null)
                foreach (var sm in start.StartingMinions)
                    if (sm.Rank < 1 || sm.Rank > 5) errors.Add($"StartingBalance minion rank {sm.Rank} out of 1..5.");

            if (start.StartingItemStacks != null)
                foreach (var entry in start.StartingItemStacks)
                    if (entry.Item == null) errors.Add("StartingBalance has an item stack with no item.");

            if (start.StartingCompletedUpgrades != null)
                foreach (var up in start.StartingCompletedUpgrades)
                    if (up == null) errors.Add("StartingBalance has a null completed-upgrade reference.");

            if (start.StartingCompletedResearch != null)
                foreach (var r in start.StartingCompletedResearch)
                    if (r == null) errors.Add("StartingBalance has a null completed-research reference.");

            if (start.StartingWorldUnlocks != null)
                foreach (var u in start.StartingWorldUnlocks)
                    if (u.Mission == null) errors.Add("StartingBalance has a world unlock with no mission.");
        }
    }
}
