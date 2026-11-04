using System;
using VContainer;

namespace Game
{
    /// Headless composition: builds a full gameplay session container without any scene/LifetimeScope,
    /// runs GameSessionStartup once, and exposes every service a caller (test, or a small
    /// developer-only runner) needs — the acceptance-demonstration entry point from the task brief:
    /// "Create a fresh game session ... Query ... Issue player operations ... Advance virtual game
    /// time ... Inspect reactive Models ... without instantiating a Canvas, scene UI, ViewModel,
    /// Composer, window, or prefab."
    public sealed class GameplaySession : IDisposable
    {
        private readonly IObjectResolver _container;

        internal GameplaySession(IObjectResolver container)
        {
            _container = container;
            Session = container.Resolve<GameSessionModel>();
            Clock = container.Resolve<ClockService>();
            Scheduler = container.Resolve<GameScheduler>();
            Random = container.Resolve<IRandomSource>();
            Economy = container.Resolve<EconomyService>();
            Inventory = container.Resolve<InventoryService>();
            Sales = container.Resolve<SalesService>();
            Minions = container.Resolve<MinionService>();
            Recovery = container.Resolve<RecoveryService>();
            Training = container.Resolve<TrainingService>();
            TowerUpgrades = container.Resolve<TowerUpgradeService>();
            Research = container.Resolve<ResearchService>();
            Dungeon = container.Resolve<DungeonService>();
            World = container.Resolve<WorldProgressionService>();
            Intelligence = container.Resolve<IntelligenceService>();
            Raids = container.Resolve<RaidService>();
            Payroll = container.Resolve<PayrollService>();
        }

        public GameSessionModel Session { get; }
        public ClockService Clock { get; }
        public GameScheduler Scheduler { get; }
        public IRandomSource Random { get; }
        public EconomyService Economy { get; }
        public InventoryService Inventory { get; }
        public SalesService Sales { get; }
        public MinionService Minions { get; }
        public RecoveryService Recovery { get; }
        public TrainingService Training { get; }
        public TowerUpgradeService TowerUpgrades { get; }
        public ResearchService Research { get; }
        public DungeonService Dungeon { get; }
        public WorldProgressionService World { get; }
        public IntelligenceService Intelligence { get; }
        public RaidService Raids { get; }
        public PayrollService Payroll { get; }

        /// Disposes the VContainer container, which cascades into GameSessionModel.Dispose() and
        /// every sub-model it owns.
        public void Dispose() => _container.Dispose();
    }

    public static class GameplaySessionComposer
    {
        public static GameplaySession Create(DarkTowerDatabase database, int randomSeed = 1)
        {
            var builder = new ContainerBuilder();
            GameInstallers.RegisterAll(builder, database, randomSeed);
            builder.Register<GameSessionStartup>(Lifetime.Singleton);
            var container = builder.Build();

            container.Resolve<GameSessionStartup>().Initialize();

            return new GameplaySession(container);
        }
    }
}
