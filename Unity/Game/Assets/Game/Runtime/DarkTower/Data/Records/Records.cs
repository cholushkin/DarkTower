using System;
using UnityEngine;

namespace Game
{
    // Serializable nested records shared across definitions. These are plain data rows,
    // never runtime state and never reactive.

    /// A quantity of a specific item, used in costs and starting stacks.
    [Serializable]
    public struct ItemAmount
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount;

        public ItemDefinition Item => item;
        public int Amount => amount;
    }

    /// A payable cost in currencies and/or item quantities.
    [Serializable]
    public struct ResourceCost
    {
        [SerializeField, Min(0)] private int gold;
        [SerializeField, Min(0)] private int crystals;
        [SerializeField] private ItemAmount[] items;

        public int Gold => gold;
        public int Crystals => crystals;
        public ItemAmount[] Items => items;
    }

    /// One global, permanent research effect. StatModifier targets a combat stat; Custom carries
    /// a designer key for effects whose mechanics are not finalized (e.g. a spell).
    [Serializable]
    public struct GlobalEffect
    {
        [SerializeField] private GlobalEffectKind kind;
        [SerializeField] private CombatStat stat;
        [SerializeField] private EffectOperation operation;
        [SerializeField] private float value;
        [SerializeField] private string customKey;
        [SerializeField] private string note;

        public GlobalEffectKind Kind => kind;
        public CombatStat Stat => stat;
        public EffectOperation Operation => operation;
        public float Value => value;
        public string CustomKey => customKey;
        public string Note => note;
    }

    /// One effect of a completed room upgrade. Amount meaning depends on kind (slots, +1 capacity,
    /// a reduction factor, a chance delta). TargetId references an unlock (transition/tier) for unlock kinds.
    /// TODO: ReduceX amount convention (flat vs factor) is a balance decision.
    [Serializable]
    public struct UpgradeEffect
    {
        [SerializeField] private UpgradeEffectKind kind;
        [SerializeField] private float amount;
        [SerializeField] private string targetId;
        [SerializeField] private string note;

        public UpgradeEffectKind Kind => kind;
        public float Amount => amount;
        public string TargetId => targetId;
        public string Note => note;
    }

    /// A requirement gating availability of a mission, upgrade, or research.
    [Serializable]
    public struct UnlockCondition
    {
        [SerializeField] private UnlockConditionType type;
        [SerializeField] private string targetId;

        public UnlockConditionType Type => type;
        public string TargetId => targetId;
    }

    /// Research-entry prerequisites: completed research, completed upgrades, and extra conditions.
    [Serializable]
    public struct ResearchPrerequisites
    {
        [SerializeField] private ResearchDefinition[] requiredResearch;
        [SerializeField] private RoomUpgradeDefinition[] requiredUpgrades;
        [SerializeField] private UnlockCondition[] additional;

        public ResearchDefinition[] RequiredResearch => requiredResearch;
        public RoomUpgradeDefinition[] RequiredUpgrades => requiredUpgrades;
        public UnlockCondition[] Additional => additional;
    }

    /// One mission reward row: an item, how many copies may drop, and the drop probability.
    /// TODO: exact weighted/independent draw procedure and default preselection are open decisions.
    [Serializable]
    public struct ItemRewardEntry
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private IntRange countRange;
        [SerializeField] private Probability dropChance;

        public ItemDefinition Item => item;
        public IntRange CountRange => countRange;
        public Probability DropChance => dropChance;
    }

    /// Weight of a given rank within a distribution (e.g. prisoner ranks).
    [Serializable]
    public struct RankWeight
    {
        [SerializeField, Range(1, 5)] private int rank;
        [SerializeField, Min(0f)] private float weight;

        public int Rank => rank;
        public float Weight => weight;
    }

    /// A per-rank float adjustment (e.g. dungeon processing duration or recruitment difficulty by rank).
    [Serializable]
    public struct RankModifier
    {
        [SerializeField, Range(1, 5)] private int rank;
        [SerializeField] private float modifier;

        public int Rank => rank;
        public float Modifier => modifier;
    }

    /// Relative weights for the three prisoner-processing outcomes.
    /// TODO: exact outcome evaluation (incl. rank influence) is a balance decision.
    [Serializable]
    public struct PrisonerOutcomeDistribution
    {
        [SerializeField, Min(0f)] private float recruitmentWeight;
        [SerializeField, Min(0f)] private float interrogationWeight;
        [SerializeField, Min(0f)] private float deathWeight;

        public float RecruitmentWeight => recruitmentWeight;
        public float InterrogationWeight => interrogationWeight;
        public float DeathWeight => deathWeight;
    }

    /// Optional per-mission squad limits. Zero means unconstrained.
    [Serializable]
    public struct SquadConstraints
    {
        [SerializeField, Min(0)] private int minSquadSize;
        [SerializeField, Min(0)] private int maxSquadSize;

        public int MinSquadSize => minSquadSize;
        public int MaxSquadSize => maxSquadSize;
    }

    /// Initial individual minion in the starting state.
    [Serializable]
    public struct StartingMinion
    {
        [SerializeField, Range(1, 5)] private int rank;
        [SerializeField] private MinionCondition conditions;

        public int Rank => rank;
        public MinionCondition Conditions => conditions;
    }

    /// Initial counts and rates for a room / the whole base. A room definition fills only the
    /// fields relevant to it; StartingBalance fills the whole base's initial capabilities.
    [Serializable]
    public struct RoomCapabilities
    {
        [SerializeField, Min(0)] private int storageSlots;
        [SerializeField, Min(0)] private int quartersBeds;
        [SerializeField, Min(0)] private int infirmaryBeds;
        [SerializeField, Min(0)] private int trainingStations;
        [SerializeField, Min(0)] private int dungeonCells;
        [SerializeField, Min(0)] private int unlockedResearchTier;

        public int StorageSlots => storageSlots;
        public int QuartersBeds => quartersBeds;
        public int InfirmaryBeds => infirmaryBeds;
        public int TrainingStations => trainingStations;
        public int DungeonCells => dungeonCells;
        public int UnlockedResearchTier => unlockedResearchTier;
    }

    /// Initial accessibility of a specific mission at game start.
    [Serializable]
    public struct WorldUnlockEntry
    {
        [SerializeField] private MissionDefinition mission;
        [SerializeField] private MissionState initialState;

        public MissionDefinition Mission => mission;
        public MissionState InitialState => initialState;
    }

    /// Placement of a mission marker inside a world tier ring.
    [Serializable]
    public struct WorldMissionPlacement
    {
        [SerializeField] private MissionDefinition mission;
        [SerializeField] private Vector2 position;

        public MissionDefinition Mission => mission;
        public Vector2 Position => position;
    }

    /// One ordered world tier (concentric ring) and the missions placed on it.
    [Serializable]
    public struct WorldTierEntry
    {
        [SerializeField] private string tierId;
        [SerializeField] private WorldMissionPlacement[] missions;

        public string TierId => tierId;
        public WorldMissionPlacement[] Missions => missions;
    }

    /// One tower-level threshold.
    /// TODO: whether xpRequired is a per-level increment or a cumulative threshold is an open decision.
    [Serializable]
    public struct TowerLevelEntry
    {
        [SerializeField, Min(1)] private int level;
        [SerializeField, Min(0)] private long xpRequired;

        public int Level => level;
        public long XpRequired => xpRequired;
    }

    /// Enemy-intelligence reveal rule for one achieved War Room stage.
    /// Precision 0 hides the estimate; 1 reveals the exact value.
    [Serializable]
    public struct EnemyIntelStage
    {
        [SerializeField, Min(0)] private int stage;
        [SerializeField, Range(0f, 1f)] private float estimatePrecision;
        [SerializeField] private string note;

        public int Stage => stage;
        public float EstimatePrecision => estimatePrecision;
        public string Note => note;
    }

    /// Loot-intelligence reveal rule for one achieved War Room stage.
    [Serializable]
    public struct LootIntelStage
    {
        [SerializeField, Min(0)] private int stage;
        [SerializeField] private bool revealItemTypes;
        [SerializeField] private bool revealDropEstimates;
        [SerializeField, Range(0f, 1f)] private float estimatePrecision;

        public int Stage => stage;
        public bool RevealItemTypes => revealItemTypes;
        public bool RevealDropEstimates => revealDropEstimates;
        public float EstimatePrecision => estimatePrecision;
    }

    /// Normal-sale duration rule as a function of stack quantity, before efficiency modifiers.
    /// TODO: exact quantity-to-duration function and efficiency application are open decisions.
    [Serializable]
    public struct SaleDurationRule
    {
        [SerializeField] private GameDuration baseDuration;
        [SerializeField, Min(0f)] private float durationPerCopyHours;

        public GameDuration BaseDuration => baseDuration;
        public float DurationPerCopyHours => durationPerCopyHours;
    }
}
