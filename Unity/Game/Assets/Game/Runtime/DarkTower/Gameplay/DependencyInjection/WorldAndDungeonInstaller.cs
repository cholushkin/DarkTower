using VContainer;

namespace Game
{
    public static class WorldAndDungeonInstaller
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<DungeonService>(Lifetime.Singleton);
            builder.Register<WorldProgressionService>(Lifetime.Singleton);
            builder.Register<IntelligenceService>(Lifetime.Singleton);
            builder.Register<RaidService>(Lifetime.Singleton);
        }
    }
}
