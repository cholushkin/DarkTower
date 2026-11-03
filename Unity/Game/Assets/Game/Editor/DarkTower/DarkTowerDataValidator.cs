using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game
{
    /// Editor-time data validation for a DarkTowerDatabase: uniqueness, missing references, ordered
    /// ranks/tiers, nonnegative costs/durations, valid ranges/probabilities, adjacent training
    /// transitions, prerequisite cycles, starting-capacity agreement, and unlock-target existence.
    /// Pure checks; it never mutates assets.
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

            ValidateSingletons(db, errors, warnings);
            ValidateRanks(db, errors);
            ValidateIdUniqueness(db, errors);
            ValidateItems(db, errors);
            ValidateMissions(db, errors);
            ValidateTrainingTransitions(db, errors);
            ValidateWorldMap(db, errors, warnings);
            ValidateUpgradePrerequisites(db, errors);
            ValidateUpgradeSeriesContinuity(db, errors, warnings);
            ValidateResearchPrerequisites(db, errors);
            ValidateUnlockConditionTargets(db, errors);
            ValidateTowerLevelProgression(db, errors);
            ValidateStartingBalance(db, errors, warnings);
        }

        private static void ValidateSingletons(DarkTowerDatabase db, List<string> errors, List<string> warnings)
        {
            // Without these, the runtime does not crash, but a new session degrades to an
            // unplayable default: SessionStateFactory treats a missing StartingBalance/GlobalBalance
            // as all-zero starting capacities and WorldProgressionService.IsAccessible treats a
            // missing WorldMap as "nothing beyond an explicit starting unlock is ever accessible".
            if (db.GlobalBalance == null) errors.Add("GlobalBalance is not assigned.");
            if (db.StartingBalance == null) errors.Add("StartingBalance is not assigned.");
            if (db.WorldMap == null) errors.Add("WorldMap is not assigned.");

            if (db.TowerLevelProgression == null) warnings.Add("TowerLevelProgression is not assigned.");
            if (db.StorehouseRules == null) warnings.Add("StorehouseRules is not assigned.");
            if (db.RecoveryRules == null) warnings.Add("RecoveryRules is not assigned.");
            if (db.DungeonRules == null) warnings.Add("DungeonRules is not assigned.");
            if (db.WarRoomIntelligence == null) warnings.Add("WarRoomIntelligence is not assigned.");
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
                CheckDuration(m.BaseDuration, $"Mission '{m.Id}' base_duration", errors);

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
                CheckDuration(t.Duration, $"Training transition {t.FromRank}->{t.ToRank}", errors);
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
                CheckDuration(up.Duration, $"Upgrade '{up.Id}'", errors);
                ValidateUpgradeEffects(up, errors);

                var path = new HashSet<RoomUpgradeDefinition>();
                if (HasUpgradeCycle(up, path)) errors.Add($"Upgrade '{up.Id}' is part of a prerequisite cycle.");
            }
        }

        /// Sanity-checks each completed-upgrade effect's Amount/TargetId against the convention
        /// UpgradeEffectRules/UpgradeEffectApplier actually interpret it with for that Kind (see
        /// those classes' doc comments for the convention itself — this only catches authoring
        /// mistakes against it, never invents a new rule).
        private static void ValidateUpgradeEffects(RoomUpgradeDefinition up, List<string> errors)
        {
            if (up.Effects == null) return;
            foreach (var effect in up.Effects)
            {
                switch (effect.Kind)
                {
                    case UpgradeEffectKind.AddStorageSlots:
                    case UpgradeEffectKind.AddQuartersBed:
                    case UpgradeEffectKind.AddInfirmaryBed:
                    case UpgradeEffectKind.AddTrainingStation:
                    case UpgradeEffectKind.AddDungeonCell:
                        if (effect.Amount <= 0f) errors.Add($"Upgrade '{up.Id}' {effect.Kind} amount must be > 0.");
                        break;
                    case UpgradeEffectKind.ReduceSaleDuration:
                    case UpgradeEffectKind.ReduceTreatmentDuration:
                    case UpgradeEffectKind.ReduceResearchDuration:
                    case UpgradeEffectKind.ReduceTortureDuration:
                        // UpgradeEffectRules.GetDurationFactor multiplies these per completed step;
                        // outside (0,1] a step would leave duration unchanged, negative, or growing.
                        if (effect.Amount <= 0f || effect.Amount > 1f)
                            errors.Add($"Upgrade '{up.Id}' {effect.Kind} amount must be a (0,1] duration factor.");
                        break;
                    case UpgradeEffectKind.IncreaseRecruitmentChance:
                        if (effect.Amount <= 0f) errors.Add($"Upgrade '{up.Id}' IncreaseRecruitmentChance amount must be > 0.");
                        break;
                    case UpgradeEffectKind.UnlockTrainingTransition:
                        if (!IsValidTransitionTarget(effect.TargetId))
                            errors.Add($"Upgrade '{up.Id}' UnlockTrainingTransition target_id '{effect.TargetId}' must be \"fromRank-toRank\" with ranks 1..5.");
                        break;
                    case UpgradeEffectKind.AdvanceEnemyIntelligence:
                    case UpgradeEffectKind.AdvanceLootIntelligence:
                    case UpgradeEffectKind.UnlockResearchTier:
                    case UpgradeEffectKind.Custom:
                    default:
                        break;
                }
            }
        }

        private static bool IsValidTransitionTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;
            var parts = targetId.Split('-');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var from) || !int.TryParse(parts[1], out var to)) return false;
            return from >= 1 && from <= 5 && to >= 1 && to <= 5;
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

        /// TowerUpgradeService.IsEarliestIncompleteStepInSeries only requires every LOWER StepIndex
        /// sibling to be completed first; it does not itself require contiguous numbering. A gap or
        /// duplicate is still almost certainly an authoring mistake, so it is flagged here rather
        /// than silently tolerated.
        private static void ValidateUpgradeSeriesContinuity(DarkTowerDatabase db, List<string> errors, List<string> warnings)
        {
            if (db.RoomUpgrades == null) return;

            var bySeries = new Dictionary<string, List<int>>();
            foreach (var up in db.RoomUpgrades)
            {
                if (up == null || !up.IsSequential) continue;
                if (!bySeries.TryGetValue(up.SeriesId, out var steps)) bySeries[up.SeriesId] = steps = new List<int>();
                steps.Add(up.StepIndex);
            }

            foreach (var pair in bySeries)
            {
                var steps = pair.Value;
                steps.Sort();
                var seen = new HashSet<int>();
                for (int i = 0; i < steps.Count; i++)
                    if (!seen.Add(steps[i])) errors.Add($"Upgrade series '{pair.Key}' has duplicate step_index {steps[i]}.");

                for (int expected = 0; expected < steps.Count; expected++)
                    if (!seen.Contains(expected))
                        warnings.Add($"Upgrade series '{pair.Key}' is missing step_index {expected} (steps are usually contiguous from 0).");
            }
        }

        private static void ValidateResearchPrerequisites(DarkTowerDatabase db, List<string> errors)
        {
            if (db.Research == null) return;
            foreach (var r in db.Research)
            {
                if (r == null) continue;
                if (r.ResearchTier < 0) errors.Add($"Research '{r.Id}' research_tier must be >= 0.");
                CheckDuration(r.Duration, $"Research '{r.Id}'", errors);

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

        /// Checks every authored UnlockCondition target against the db list the condition's Type
        /// actually resolves against at runtime (UnlockConditionRules.IsSatisfied), so a typo'd id
        /// is caught here instead of silently evaluating to "never satisfied" in play. SecretRevealed/
        /// EventFlag target free-form flag strings set at runtime (events, starting flags) with no
        /// fixed universe to check against, so those are intentionally not validated here.
        private static void ValidateUnlockConditionTargets(DarkTowerDatabase db, List<string> errors)
        {
            if (db.RoomUpgrades != null)
                foreach (var up in db.RoomUpgrades)
                    if (up != null) ValidateConditions(db, up.AdditionalRequirements, $"Upgrade '{up.Id}' additional_requirements", errors);

            if (db.Research != null)
                foreach (var r in db.Research)
                    if (r != null) ValidateConditions(db, r.Prerequisites.Additional, $"Research '{r.Id}' prerequisites.additional", errors);

            if (db.Missions != null)
                foreach (var m in db.Missions)
                    if (m != null) ValidateConditions(db, m.UnlockConditions, $"Mission '{m.Id}' unlock_conditions", errors);
        }

        private static void ValidateConditions(DarkTowerDatabase db, UnlockCondition[] conditions, string context, List<string> errors)
        {
            if (conditions == null) return;
            foreach (var c in conditions)
            {
                switch (c.Type)
                {
                    case UnlockConditionType.UpgradeCompleted:
                        if (db.GetUpgrade(c.TargetId) == null) errors.Add($"{context}: no upgrade with id '{c.TargetId}'.");
                        break;
                    case UnlockConditionType.ResearchCompleted:
                        if (db.GetResearch(c.TargetId) == null) errors.Add($"{context}: no research with id '{c.TargetId}'.");
                        break;
                    case UnlockConditionType.MissionCompletedOnce:
                        if (db.GetMission(c.TargetId) == null) errors.Add($"{context}: no mission with id '{c.TargetId}'.");
                        break;
                    case UnlockConditionType.TierCompleted:
                        if (!TierExists(db, c.TargetId)) errors.Add($"{context}: no world tier with id '{c.TargetId}'.");
                        break;
                    case UnlockConditionType.SecretRevealed:
                    case UnlockConditionType.EventFlag:
                    default:
                        break;
                }
            }
        }

        private static bool TierExists(DarkTowerDatabase db, string tierId)
        {
            var tiers = db.WorldMap != null ? db.WorldMap.Tiers : null;
            if (tiers == null) return false;
            foreach (var t in tiers) if (t.TierId == tierId) return true;
            return false;
        }

        private static void ValidateTowerLevelProgression(DarkTowerDatabase db, List<string> errors)
        {
            var prog = db.TowerLevelProgression;
            if (prog == null || prog.Levels == null) return;

            long lastXp = -1;
            int lastLevel = -1;
            foreach (var entry in prog.Levels)
            {
                if (entry.XpRequired < 0) errors.Add($"TowerLevelProgression level {entry.Level} xp_required must be >= 0.");
                if (entry.Level <= lastLevel) errors.Add($"TowerLevelProgression levels must be strictly increasing by authored order (level {entry.Level} after {lastLevel}).");
                if (entry.XpRequired <= lastXp) errors.Add($"TowerLevelProgression xp_required must be strictly increasing by authored order (level {entry.Level}).");
                lastLevel = entry.Level;
                lastXp = entry.XpRequired;
            }
        }

        private static void ValidateStartingBalance(DarkTowerDatabase db, List<string> errors, List<string> warnings)
        {
            var start = db.StartingBalance;
            if (start == null) return;

            if (start.StartingMinions != null)
                foreach (var sm in start.StartingMinions)
                    if (sm.Rank < 1 || sm.Rank > 5) errors.Add($"StartingBalance minion rank {sm.Rank} out of 1..5.");

            int storageSlots = ResolveStorageSlots(db, start);
            if (start.StartingItemStacks != null)
            {
                int stacksNeeded = 0;
                foreach (var entry in start.StartingItemStacks)
                {
                    if (entry.Item == null) { errors.Add("StartingBalance has an item stack with no item."); continue; }
                    stacksNeeded += StorageRules.StacksFor(entry.Amount, entry.Item.MaxStackSize);
                }
                if (stacksNeeded > storageSlots)
                    errors.Add($"StartingBalance item stacks need {stacksNeeded} storage slot(s) but only {storageSlots} are available at session start (see SessionStateFactory.AddStartingStacks).");
            }

            // Capacity-authority agreement (see RoomDefinitions.cs/RulesDefinitions.cs ownership notes):
            // when both are authored, InitialRoomCapabilities wins and InitialStorageSlots is unused.
            if (start.InitialRoomCapabilities.StorageSlots > 0 && db.StorehouseRules != null && db.StorehouseRules.InitialStorageSlots > 0)
                warnings.Add("Both StartingBalance.InitialRoomCapabilities.StorageSlots and StorehouseRulesDefinition.InitialStorageSlots are set; the former wins and the latter is never used.");

            if (start.StartingCompletedUpgrades != null)
            {
                foreach (var up in start.StartingCompletedUpgrades)
                {
                    if (up == null) { errors.Add("StartingBalance has a null completed-upgrade reference."); continue; }
                    if (!ContainsReference(db.RoomUpgrades, up))
                        errors.Add($"StartingBalance references completed upgrade '{up.Id}' which is not in the database's room_upgrades list.");
                }
            }

            if (start.StartingCompletedResearch != null)
            {
                foreach (var r in start.StartingCompletedResearch)
                {
                    if (r == null) { errors.Add("StartingBalance has a null completed-research reference."); continue; }
                    if (!ContainsReference(db.Research, r))
                        errors.Add($"StartingBalance references completed research '{r.Id}' which is not in the database's research list.");
                }
            }

            if (start.StartingWorldUnlocks != null)
                foreach (var u in start.StartingWorldUnlocks)
                    if (u.Mission == null) errors.Add("StartingBalance has a world unlock with no mission.");
        }

        private static int ResolveStorageSlots(DarkTowerDatabase db, StartingBalanceDefinition start)
        {
            int slots = start.InitialRoomCapabilities.StorageSlots;
            if (slots <= 0 && db.StorehouseRules != null) slots = db.StorehouseRules.InitialStorageSlots;
            return slots;
        }

        private static bool ContainsReference<T>(IReadOnlyList<T> list, T item) where T : class
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == item) return true;
            return false;
        }

        private static void CheckDuration(GameDuration duration, string context, List<string> errors)
        {
            if (duration.Value < 0f) errors.Add($"{context} duration value must be >= 0.");
        }
    }
}
