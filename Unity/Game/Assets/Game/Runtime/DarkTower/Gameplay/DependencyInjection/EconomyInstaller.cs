using VContainer;

namespace Game
{
    public static class EconomyInstaller
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<InventoryService>(Lifetime.Singleton);
            builder.Register<EconomyService>(Lifetime.Singleton);
            builder.Register<SalesService>(Lifetime.Singleton);
        }
    }
}
