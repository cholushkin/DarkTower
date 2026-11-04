using VContainer;
using VitalRouter;

namespace Game
{
    /// Clock, scheduler, randomness and the VitalRouter command bus used only for cross-cutting
    /// domain notifications (see Events/DomainEvents.cs) — not for player-initiated operations,
    /// which go through direct service calls elsewhere.
    public static class CoreInstaller
    {
        public static void Register(IContainerBuilder builder, int randomSeed)
        {
            builder.Register<ClockService>(resolver => new ClockService(resolver.Resolve<GameSessionModel>().Clock), Lifetime.Singleton);
            builder.Register<IRandomSource>(_ => new SystemRandomSource(randomSeed), Lifetime.Singleton);

            // Plain Router registration (no VITALROUTER_VCONTAINER_INTEGRATION dependency, no
            // [Routes]-attributed subscriber mapping needed): this layer only ever publishes
            // milestone notifications, it never subscribes.
            builder.Register<Router>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            builder.Register<GameScheduler>(Lifetime.Singleton);
        }
    }
}
