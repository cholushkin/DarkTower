using System;

namespace Game
{
    /// Minion status conditions. Both can be present at once, so this is a flags set.
    [Flags]
    public enum MinionCondition
    {
        None = 0,
        Tired = 1 << 0,
        Injured = 1 << 1
    }

    /// The single activity a minion is occupied by. A minion cannot hold two at once.
    /// A finished-but-unclaimed raid still keeps its squad in Raiding until Claim.
    public enum MinionActivity
    {
        Idle,
        Raiding,
        Resting,
        Treating,
        Training
    }

    /// Stable identities of the seven tower rooms.
    public enum RoomId
    {
        Storehouse,
        Quarters,
        Infirmary,
        TrainingHall,
        Laboratory,
        Dungeon,
        WarRoom
    }

    /// Current raid/accessibility state of a mission marker.
    /// CompletedOnce (the permanent success checkmark) is tracked separately and coexists with these.
    public enum MissionState
    {
        Locked,
        Available,
        Running,
        CompletedUnclaimed,
        Cooldown
    }

    /// Resolved outcome of a finished raid. Pending means the timer has not produced a result yet.
    public enum RaidOutcome
    {
        Pending,
        Success,
        Failure
    }

    /// Possible result of automatic Dungeon prisoner processing.
    public enum PrisonerProcessOutcome
    {
        Pending,
        Recruitment,
        Interrogation,
        Death
    }

    /// Selling mode for a storehouse stack.
    public enum SaleKind
    {
        Normal,
        Instant
    }

    /// Lifecycle of a timed activity instance (sale, upgrade, research, training, rest, treatment, processing).
    public enum ActivityStatus
    {
        Running,
        Completed
    }

    /// Item gameplay categories. Treasure drives the Treasures inventory filter; the rest are open categories.
    /// Extend with additional typed categories rather than free-form strings.
    [Flags]
    public enum ItemTag
    {
        None = 0,
        Treasure = 1 << 0,
        Weapon = 1 << 1,
        Material = 1 << 2,
        MagicItem = 1 << 3,
        Ingredient = 1 << 4,
        MissionItem = 1 << 5
    }

    /// How a global research modifier combines with a base value.
    /// TODO: additive-vs-multiplicative composition order across multiple effects is an open design decision.
    public enum EffectOperation
    {
        Additive,
        Multiplicative
    }

    /// Combat/characteristic parameters a global research effect can target.
    /// TODO: "other globally eligible parameters" are not finalized; use GlobalEffectKind.Custom for undecided ones.
    public enum CombatStat
    {
        Attack,
        Defense
    }

    /// Shape of a global research effect.
    public enum GlobalEffectKind
    {
        StatModifier,
        Custom
    }

    /// Kinds of gameplay effect a completed room upgrade applies.
    public enum UpgradeEffectKind
    {
        AddStorageSlots,
        AddQuartersBed,
        AddInfirmaryBed,
        AddTrainingStation,
        AddDungeonCell,
        ReduceSaleDuration,
        ReduceTreatmentDuration,
        ReduceResearchDuration,
        ReduceTortureDuration,
        IncreaseRecruitmentChance,
        UnlockTrainingTransition,
        UnlockResearchTier,
        AdvanceEnemyIntelligence,
        AdvanceLootIntelligence,
        Custom
    }

    /// Kind of requirement gating a mission, upgrade, or research entry.
    public enum UnlockConditionType
    {
        TierCompleted,
        MissionCompletedOnce,
        UpgradeCompleted,
        ResearchCompleted,
        SecretRevealed,
        EventFlag
    }

    /// Currency rounding behavior for sales when integer balances require it.
    /// TODO: half-price instant-sale rounding direction is an open design decision.
    public enum SaleRoundingRule
    {
        Floor,
        Round,
        Ceil
    }

    /// Bed assignment priority among waiting minions.
    public enum RestAssignmentPriority
    {
        LongestTiredFirst
    }

    /// Unit a designer authored a duration in. Converted to game-hours against the clock's hours-per-day.
    public enum DurationUnit
    {
        Hours,
        Days
    }
}
