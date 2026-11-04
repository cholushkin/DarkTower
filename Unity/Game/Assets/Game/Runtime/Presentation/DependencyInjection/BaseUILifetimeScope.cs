using VContainer;
using VContainer.Unity;

namespace Game
{
    /// Scene/runtime composition root for the BaseUI scene (per DataFlow.md §5: the UI child scope).
    /// Parents to GameSessionLifetimeScope via the SceneFlow DI ParentOverride on BaseUI, so
    /// GameSessionModel and every DarkTower.Gameplay service are resolvable here; this scope must
    /// never be resolved by GameSessionLifetimeScope itself.
    public sealed class BaseUILifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<WindowService>().AsImplementedInterfaces().AsSelf();

            builder.Register(resolver => new StorehouseViewModel(resolver.Resolve<GameSessionModel>().Inventory), Lifetime.Transient);
        }
    }
}
