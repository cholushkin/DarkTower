using VContainer;

namespace Game
{
    public static class MinionsInstaller
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<MinionService>(Lifetime.Singleton);
            builder.Register<RecoveryService>(Lifetime.Singleton);
            builder.Register<TrainingService>(Lifetime.Singleton);
        }
    }
}
