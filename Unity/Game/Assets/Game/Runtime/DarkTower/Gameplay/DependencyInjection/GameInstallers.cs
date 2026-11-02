using VContainer;

namespace Game
{
    /// Runs every per-area installer in dependency order. Shared by the real GameSessionLifetimeScope
    /// and by GameplaySessionComposer (headless/test container, no scene).
    public static class GameInstallers
    {
        public static void RegisterAll(IContainerBuilder builder, DarkTowerDatabase database, int randomSeed)
        {
            ConfigInstaller.Register(builder, database);
            CoreInstaller.Register(builder, randomSeed);
            EconomyInstaller.Register(builder);
            MinionsInstaller.Register(builder);
            TowerAndLaboratoryInstaller.Register(builder);
            WorldAndDungeonInstaller.Register(builder);
            PayrollInstaller.Register(builder);
        }
    }
}
