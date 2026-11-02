using VContainer;

namespace Game
{
    /// Registers the designer-authored database and builds the fresh-session root Model from it.
    /// Installers are plain organization, not scopes (per DataFlow.md §5) — every gameplay service
    /// still lives in the one session container a LifetimeScope or test harness builds.
    public static class ConfigInstaller
    {
        public static void Register(IContainerBuilder builder, DarkTowerDatabase database)
        {
            builder.RegisterInstance(database);
            builder.Register<GameSessionModel>(resolver => SessionStateFactory.CreateNewSession(resolver.Resolve<DarkTowerDatabase>()), Lifetime.Singleton);
        }
    }
}
