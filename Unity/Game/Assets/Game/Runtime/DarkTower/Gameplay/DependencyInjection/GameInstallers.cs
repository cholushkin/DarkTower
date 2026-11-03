using System;
using VContainer;

namespace Game
{
    /// Registers every gameplay service in dependency order. Shared by the real
    /// GameSessionLifetimeScope and by GameplaySessionComposer (headless/test container, no scene).
    public static class GameInstallers
    {
        /// Every per-session service beyond Config/Core needs nothing but a Singleton registration
        /// (no interface binding, no factory lambda), so one data-driven list replaces what used to
        /// be five near-identical per-area installer files. VContainer resolves constructor
        /// dependencies lazily, so this array's order carries no meaning of its own — it is not the
        /// fixed processing order GameScheduler enforces via its own ITimedProcess[] list.
        private static readonly Type[] ServiceTypes =
        {
            typeof(InventoryService), typeof(EconomyService), typeof(SalesService),
            typeof(MinionService), typeof(RecoveryService), typeof(TrainingService),
            typeof(TowerUpgradeService), typeof(ResearchService),
            typeof(DungeonService), typeof(WorldProgressionService), typeof(IntelligenceService), typeof(RaidService),
            typeof(PayrollService),
        };

        public static void RegisterAll(IContainerBuilder builder, DarkTowerDatabase database, int randomSeed)
        {
            ConfigInstaller.Register(builder, database);
            CoreInstaller.Register(builder, randomSeed);
            foreach (var serviceType in ServiceTypes) builder.Register(serviceType, Lifetime.Singleton);
        }
    }
}
