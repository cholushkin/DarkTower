using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // Pure, deterministic data rules that are explicit in the specification (not provisional balance).
    // The provisional combat/injury/duration formulas are NOT implemented here; their coefficients
    // live in GlobalBalanceDefinition and belong to a future gameplay service.

    public static class CarryRules
    {
        /// Documented carry rule: sum fractional per-rank carry capacities, floor ONCE.
        public static int ComputeCarryLimit(IEnumerable<float> perMemberCapacities)
        {
            float sum = 0f;
            if (perMemberCapacities != null)
            {
                foreach (var c in perMemberCapacities) sum += c;
            }
            return Mathf.FloorToInt(sum);
        }
    }

    public static class StorageRules
    {
        /// Slots needed to hold a quantity of one item type, given its max stack size.
        public static int StacksFor(int quantity, int maxStackSize)
        {
            if (quantity <= 0) return 0;
            if (maxStackSize < 1) maxStackSize = 1;
            return Mathf.CeilToInt(quantity / (float)maxStackSize);
        }
    }

    public static class StatCalculator
    {
        /// Applies global research effects to one base combat stat. Additive effects are summed,
        /// then multiplicative effects are applied.
        /// TODO: additive/multiplicative composition order across effects is an open design decision;
        /// this is a straightforward default, not a finalized rule.
        public static float Effective(float baseValue, CombatStat stat, IReadOnlyList<GlobalEffect> effects)
        {
            float additive = 0f;
            float multiplier = 1f;
            if (effects != null)
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    var e = effects[i];
                    if (e.Kind != GlobalEffectKind.StatModifier || e.Stat != stat) continue;
                    if (e.Operation == EffectOperation.Additive) additive += e.Value;
                    else multiplier *= e.Value;
                }
            }
            return (baseValue + additive) * multiplier;
        }
    }

    public static class TowerProgressionRules
    {
        /// Derives the current tower level from total XP using the configured thresholds.
        /// Interprets each entry's xpRequired as a CUMULATIVE threshold.
        /// TODO: incremental-vs-cumulative threshold convention is unresolved; this reader assumes
        /// cumulative. Change here once the design decides.
        public static int DeriveLevel(long totalXp, TowerLevelProgressionDefinition progression)
        {
            if (progression == null || progression.Levels == null || progression.Levels.Length == 0)
                return 0;

            int level = 0;
            var levels = progression.Levels;
            for (int i = 0; i < levels.Length; i++)
            {
                if (totalXp >= levels[i].XpRequired) level = levels[i].Level;
                else break;
            }
            return level;
        }
    }
}
