using VContainer;

namespace Game
{
    public static class PayrollInstaller
    {
        public static void Register(IContainerBuilder builder)
        {
            builder.Register<PayrollService>(Lifetime.Singleton);
        }
    }
}
