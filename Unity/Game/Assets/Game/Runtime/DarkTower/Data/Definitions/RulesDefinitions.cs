using UnityEngine;

namespace Game
{
    /// Dungeon processing parameters and prisoner-outcome configuration.
    /// TODO: exact three-outcome evaluation weighting beyond rank modifiers is still a balance call.
    /// Prisoner Rank sort direction at Claim is now authored via SortHighRankFirst (Task 05) rather
    /// than hardcoded, since the design leaves the direction itself undecided.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Dungeon", fileName = "DungeonRules")]
    public sealed class DungeonRulesDefinition : ScriptableObject
    {
        [SerializeField] private GameDuration baseProcessingDuration;
        [SerializeField] private RankModifier[] rankDurationModifiers;
        [SerializeField] private RankModifier[] rankRecruitmentModifiers;
        [SerializeField] private PrisonerOutcomeDistribution outcomeDistribution;
        [SerializeField] private bool recruitedTired = true;
        [SerializeField] private Probability recruitedInjuredProbability = new Probability(0.5f);
        [SerializeField] private EventDefinition[] interrogationEvents;

        // ADDED (Task 05): the design/catalog explicitly leaves the Claim-time prisoner sort
        // direction undecided ("ordering direction is a configurable/design detail to finalize").
        // Rather than hardcode a direction, it is now a designer-authored switch; default (false)
        // sorts ascending (low rank first), matching the field's natural default value.
        [SerializeField] private bool sortHighRankFirst;

        public GameDuration BaseProcessingDuration => baseProcessingDuration;
        public RankModifier[] RankDurationModifiers => rankDurationModifiers;
        public RankModifier[] RankRecruitmentModifiers => rankRecruitmentModifiers;
        public PrisonerOutcomeDistribution OutcomeDistribution => outcomeDistribution;
        public bool RecruitedTired => recruitedTired;
        public Probability RecruitedInjuredProbability => recruitedInjuredProbability;
        public EventDefinition[] InterrogationEvents => interrogationEvents;
        public bool SortHighRankFirst => sortHighRankFirst;
    }

    /// War Room intelligence reveal stages. Two independent series: enemy strength and loot.
    /// These affect information disclosure only, never the mission's actual values.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/War Room Intelligence", fileName = "WarRoomIntelligence")]
    public sealed class WarRoomIntelligenceDefinition : ScriptableObject
    {
        [SerializeField] private EnemyIntelStage[] enemyIntelligenceStages;
        [SerializeField] private LootIntelStage[] lootIntelligenceStages;

        public EnemyIntelStage[] EnemyIntelligenceStages => enemyIntelligenceStages;
        public LootIntelStage[] LootIntelligenceStages => lootIntelligenceStages;
    }

    /// Storehouse storage and sales rules.
    /// TODO: active-sale concurrency, the moment a stack is reserved/consumed, and half-price
    /// rounding are open design decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Storehouse", fileName = "StorehouseRules")]
    public sealed class StorehouseRulesDefinition : ScriptableObject
    {
        [SerializeField, Min(0)] private int initialStorageSlots;
        [SerializeField] private SaleDurationRule saleDurationRule;
        [SerializeField, Range(0f, 1f)] private float instantSaleMultiplier = 0.5f;
        [SerializeField] private SaleRoundingRule priceRoundingRule = SaleRoundingRule.Floor;

        public int InitialStorageSlots => initialStorageSlots;
        public SaleDurationRule SaleDurationRule => saleDurationRule;
        public float InstantSaleMultiplier => instantSaleMultiplier;
        public SaleRoundingRule PriceRoundingRule => priceRoundingRule;
    }

    /// Quarters (rest) and Infirmary (treatment) recovery parameters.
    /// TODO: bed-wait accrual for occupied minions and treatment-queue tie-breaking are open decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Recovery", fileName = "RecoveryRules")]
    public sealed class RecoveryRulesDefinition : ScriptableObject
    {
        [Header("Quarters")]
        [SerializeField] private GameDuration restDuration = new GameDuration(6f, DurationUnit.Hours);
        [SerializeField, Range(0f, 24f)] private float nightfallHourOfDay = 20f;
        [SerializeField] private GameDuration bedShortageDeparture = new GameDuration(2f, DurationUnit.Days);
        [SerializeField] private RestAssignmentPriority restAssignmentPriority = RestAssignmentPriority.LongestTiredFirst;

        [Header("Infirmary")]
        [SerializeField] private GameDuration treatmentDuration = new GameDuration(3f, DurationUnit.Days);

        public GameDuration RestDuration => restDuration;
        public float NightfallHourOfDay => nightfallHourOfDay;
        public GameDuration BedShortageDeparture => bedShortageDeparture;
        public RestAssignmentPriority RestAssignmentPriority => restAssignmentPriority;
        public GameDuration TreatmentDuration => treatmentDuration;

        /// Design rule: Injured is handled before Tired; a minion with both is routed to Infirmary.
        public bool InjuredBeforeTired => true;
        /// Design rule: completing treatment clears both Injured and Tired.
        public bool TreatmentClearsBoth => true;
    }

    /// The tower's XP-to-Level progression table.
    /// TODO: whether xpRequired entries are per-level increments or cumulative thresholds, the XP
    /// award trigger (purchase vs completion), and any direct level benefits are open decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Tower Level Progression", fileName = "TowerLevelProgression")]
    public sealed class TowerLevelProgressionDefinition : ScriptableObject
    {
        [SerializeField] private TowerLevelEntry[] levels;

        public TowerLevelEntry[] Levels => levels;
    }
}
