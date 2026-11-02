using VitalRouter;

namespace Game
{
    // Cross-cutting domain notifications, published as data/signals only (never a popup/UI control
    // here). A future presentation layer subscribes via VitalRouter to react (toast, banner, SFX).
    // These are milestones, not operations: nothing in this layer requires a subscriber to exist, and
    // publishing never blocks the publishing service on completion-order of any eventual handler.

    /// A minion left the tower after going more than the configured grace period without a suitable
    /// bed while free and idle.
    public readonly struct MinionDepartedEvent : ICommand
    {
        public readonly EntityId MinionId;
        public MinionDepartedEvent(EntityId minionId) => MinionId = minionId;
    }

    /// A World Tier's missions were all Claimed at least once and the next tier became accessible.
    public readonly struct WorldTierUnlockedEvent : ICommand
    {
        public readonly int UnlockedTierIndex;
        public WorldTierUnlockedEvent(int unlockedTierIndex) => UnlockedTierIndex = unlockedTierIndex;
    }

    /// Every mission in the world has been Claimed at least once.
    public readonly struct VictoryAchievedEvent : ICommand
    {
    }

    /// The weekly payroll deadline passed unpaid.
    public readonly struct BankruptcyDeclaredEvent : ICommand
    {
    }

    /// A raid finished (became CompletedUnclaimed) and is awaiting Claim.
    public readonly struct RaidFinishedEvent : ICommand
    {
        public readonly EntityId RaidId;
        public readonly RaidOutcome Outcome;
        public RaidFinishedEvent(EntityId raidId, RaidOutcome outcome) { RaidId = raidId; Outcome = outcome; }
    }
}
