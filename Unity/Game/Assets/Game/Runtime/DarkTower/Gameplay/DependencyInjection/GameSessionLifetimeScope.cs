using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game
{
    /// Scene/runtime composition root for one gameplay session. Deliberately the ONLY VContainer
    /// scope this task creates (per DataFlow.md §5: a session scope, no per-feature child scopes). A
    /// future UI child scope resolves gameplay services from this parent; this parent must never
    /// resolve a UI-only service (none are registered here).
    public sealed class GameSessionLifetimeScope : LifetimeScope
    {
        [field: SerializeField] private DarkTowerDatabase Database { get; set; }
        [field: SerializeField] private int RandomSeed { get; set; } = 12345;

        protected override void Configure(IContainerBuilder builder)
        {
            GameInstallers.RegisterAll(builder, Database, RandomSeed);
            builder.RegisterEntryPoint<GameSessionStartup>();
        }
    }
}
