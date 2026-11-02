using UnityEngine;

namespace Game
{
    /// Cross-system clock, payroll and combat coefficients not specific to a single content asset.
    /// The combat/injury/duration coefficients below are PROVISIONAL illustrative values from the
    /// design doc; this asset only stores them. No combat/outcome calculation lives in the data layer.
    [CreateAssetMenu(menuName = "Dark Tower/Global Balance", fileName = "GlobalBalance")]
    public sealed class GlobalBalanceDefinition : ScriptableObject
    {
        [Header("Clock")]
        [SerializeField, Min(1f)] private float realSecondsPerGameDay = 60f;
        [SerializeField, Min(1f)] private float gameHoursPerDay = 24f;
        [SerializeField, Min(1)] private int daysPerWeek = 7;

        [Header("Payroll")]
        [SerializeField, Min(1)] private int paydayIntervalDays = 7;

        [Header("Combat (provisional coefficients)")]
        [SerializeField] private float attackWeight = 0.65f;
        [SerializeField] private float defenseWeight = 0.35f;
        [SerializeField] private float rankPowerBonusPerRank = 0.10f;

        [Header("Raid outcome (provisional coefficients)")]
        [SerializeField] private float failureCurveSlope = 2.0f;

        [Header("Raid injuries (provisional coefficients)")]
        [SerializeField] private float injuryUnderpowerWeight = 0.20f;
        [SerializeField] private float injuryRankReduction = 0.02f;
        [SerializeField] private float injuryDefenseReduction = 0.005f;
        [SerializeField] private float injuryMin = 0.01f;
        [SerializeField] private float injuryMax = 0.90f;

        [Header("Raid duration (provisional coefficients)")]
        [SerializeField] private float durationSquadSizeReduction = 0.04f;
        [SerializeField] private float durationAverageRankReduction = 0.01f;
        [SerializeField] private float durationMinFactor = 0.80f;

        public float RealSecondsPerGameDay => realSecondsPerGameDay;
        public float GameHoursPerDay => gameHoursPerDay;
        public int DaysPerWeek => daysPerWeek;
        public int PaydayIntervalDays => paydayIntervalDays;

        public float AttackWeight => attackWeight;
        public float DefenseWeight => defenseWeight;
        public float RankPowerBonusPerRank => rankPowerBonusPerRank;
        public float FailureCurveSlope => failureCurveSlope;

        public float InjuryUnderpowerWeight => injuryUnderpowerWeight;
        public float InjuryRankReduction => injuryRankReduction;
        public float InjuryDefenseReduction => injuryDefenseReduction;
        public float InjuryMin => injuryMin;
        public float InjuryMax => injuryMax;

        public float DurationSquadSizeReduction => durationSquadSizeReduction;
        public float DurationAverageRankReduction => durationAverageRankReduction;
        public float DurationMinFactor => durationMinFactor;
    }
}
