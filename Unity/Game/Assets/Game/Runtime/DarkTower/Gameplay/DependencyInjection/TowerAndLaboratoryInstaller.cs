using VContainer;

namespace Game
{
    public static class TowerAndLaboratoryInstaller
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<TowerUpgradeService>(Lifetime.Singleton);
            builder.Register<ResearchService>(Lifetime.Singleton);
        }
    }
}
