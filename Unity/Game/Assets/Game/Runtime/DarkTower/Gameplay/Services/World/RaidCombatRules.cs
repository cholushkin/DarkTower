using UnityEngine;

namespace Game
{
    /// Implements the game design's PROVISIONAL combat/injury/duration formulas (design doc §10).
    /// GlobalBalanceDefinition explicitly only stores these coefficients and says "no combat/outcome
    /// calculation lives in the data layer" — this is that calculation, kept in one place so
    /// RaidService (outcome/duration at launch & finish) and the injury roll at Claim use identical
    /// math. Coefficients are read from the database and are NOT hardcoded balance values; swapping
    /// the config asset changes behavior without touching this file.
    public static class RaidCombatRules
    {
        public static float ComputeTotalPower(RaidLaunchSnapshot snapshot, GlobalBalanceDefinition balance)
        {
            var members = snapshot.Members;
            if (members == null) return 0f;

            float attackWeight = balance != null ? balance.AttackWeight : 0.65f;
            float defenseWeight = balance != null ? balance.DefenseWeight : 0.35f;
            float rankBonus = balance != null ? balance.RankPowerBonusPerRank : 0.10f;

            float total = 0f;
            for (int i = 0; i < members.Length; i++)
            {
                var m = members[i];
                float unitPower = (attackWeight * m.Stats.Attack + defenseWeight * m.Stats.Defense) * (1f + rankBonus * (m.Rank - 1));
                total += unitPower;
            }
            return total;
        }

        public static float ComputePowerRatio(RaidLaunchSnapshot snapshot, MissionDefinition mission, GlobalBalanceDefinition balance)
        {
            float totalPower = ComputeTotalPower(snapshot, balance);
            float requiredPower = mission != null ? mission.RequiredPower : 0f;
            return totalPower / Mathf.Max(0.0001f, requiredPower);
        }

        public static float ComputeFailureChance(float powerRatio, GlobalBalanceDefinition balance)
        {
            float slope = balance != null ? balance.FailureCurveSlope : 2.0f;
            return Mathf.Clamp01(slope * (1f - powerRatio));
        }

        public static float ComputeInjuryChance(RaidMember member, float powerRatio, float baseInjuryChance, GlobalBalanceDefinition balance)
        {
            float underpowerWeight = balance != null ? balance.InjuryUnderpowerWeight : 0.20f;
            float rankReduction = balance != null ? balance.InjuryRankReduction : 0.02f;
            float defenseReduction = balance != null ? balance.InjuryDefenseReduction : 0.005f;
            float min = balance != null ? balance.InjuryMin : 0.01f;
            float max = balance != null ? balance.InjuryMax : 0.90f;

            float chance = baseInjuryChance
                + underpowerWeight * Mathf.Max(0f, 1f - powerRatio)
                - rankReduction * (member.Rank - 1)
                - defenseReduction * Mathf.Max(0f, member.Stats.Defense - 5f);
            return Mathf.Clamp(chance, min, max);
        }

        public static float ComputeDurationHours(MissionDefinition mission, RaidLaunchSnapshot snapshot, GlobalBalanceDefinition balance, float gameHoursPerDay)
        {
            float baseHours = mission.BaseDuration.ToHours(gameHoursPerDay);
            float squadSizeReduction = balance != null ? balance.DurationSquadSizeReduction : 0.04f;
            float avgRankReduction = balance != null ? balance.DurationAverageRankReduction : 0.01f;
            float minFactor = balance != null ? balance.DurationMinFactor : 0.80f;

            int squadSize = snapshot.SquadSize;
            float avgRank = ComputeAverageRank(snapshot);
            float factor = Mathf.Max(minFactor, 1f - squadSizeReduction * (squadSize - 1) - avgRankReduction * (avgRank - 1));
            return baseHours * factor;
        }

        private static float ComputeAverageRank(RaidLaunchSnapshot snapshot)
        {
            var members = snapshot.Members;
            if (members == null || members.Length == 0) return 1f;
            float sum = 0f;
            for (int i = 0; i < members.Length; i++) sum += members[i].Rank;
            return sum / members.Length;
        }
    }
}
