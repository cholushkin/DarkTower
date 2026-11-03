using R3;

namespace Game
{
    /// Root aggregate of all mutable gameplay-session state. Owns every sub-model and the entity-id
    /// allocator, and disposes them all on shutdown. Holds a read-only reference to the content
    /// database; it never mutates definitions. The owning VContainer session scope disposes this.
    ///
    /// IsVictory/IsBankrupt are the run's terminal-state flags. Each is set exactly once, only by
    /// WorldProgressionService (victory: every mission Claimed) and PayrollService (bankruptcy:
    /// payday ends unpaid). Gameplay services consult IsTerminal to reject new player-initiated
    /// operations once the run has ended.
    public sealed class GameSessionModel : ModelBase
    {
        private readonly ReactiveProperty<bool> _isVictory = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _isBankrupt = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> IsVictory => _isVictory;
        public ReadOnlyReactiveProperty<bool> IsBankrupt => _isBankrupt;
        public bool IsTerminal => _isVictory.CurrentValue || _isBankrupt.CurrentValue;

        internal void DeclareVictory() => _isVictory.Value = true;
        internal void DeclareBankruptcy() => _isBankrupt.Value = true;

        public GameSessionModel(
            DarkTowerDatabase database,
            EntityIdAllocator idAllocator,
            ClockModel clock,
            EconomyModel economy,
            TowerModel tower,
            MinionRosterModel roster,
            InventoryModel inventory,
            DungeonModel dungeon,
            ResearchModel research,
            WorldModel world,
            RecoveryModel recovery,
            TrainingModel training,
            IntelligenceModel intelligence,
            PayrollModel payroll)
        {
            Database = database;
            IdAllocator = idAllocator;
            Clock = clock;
            Economy = economy;
            Tower = tower;
            Roster = roster;
            Inventory = inventory;
            Dungeon = dungeon;
            Research = research;
            World = world;
            Recovery = recovery;
            Training = training;
            Intelligence = intelligence;
            Payroll = payroll;
            Disposables.Add(_isVictory);
            Disposables.Add(_isBankrupt);
        }

        public DarkTowerDatabase Database { get; }
        public EntityIdAllocator IdAllocator { get; }

        public ClockModel Clock { get; }
        public EconomyModel Economy { get; }
        public TowerModel Tower { get; }
        public MinionRosterModel Roster { get; }
        public InventoryModel Inventory { get; }
        public DungeonModel Dungeon { get; }
        public ResearchModel Research { get; }
        public WorldModel World { get; }
        public RecoveryModel Recovery { get; }
        public TrainingModel Training { get; }
        public IntelligenceModel Intelligence { get; }
        public PayrollModel Payroll { get; }

        /// Total weekly salary bill across the current roster.
        public int WeeklySalary => Roster.ComputeWeeklySalary(Database);

        protected override void OnDispose()
        {
            Clock.Dispose();
            Economy.Dispose();
            Tower.Dispose();
            Roster.Dispose();
            Inventory.Dispose();
            Dungeon.Dispose();
            Research.Dispose();
            World.Dispose();
            Recovery.Dispose();
            Training.Dispose();
            Intelligence.Dispose();
            Payroll.Dispose();
        }
    }
}
