using VContainer.Unity;

namespace Game
{
    /// Explicit startup sequencing (per rMVVM.md §7 / DataFlow.md §5: "prefer an explicit entry point
    /// over constructors with hidden side effects"). Runs once, after every gameplay service has been
    /// constructed, and before the caller starts issuing operations or advancing time: an initial bed
    /// auto-assignment pass (covers any Tired/Injured starting minions) and an accessibility refresh
    /// (covers a mission whose unlock conditions are already met at session start).
    ///
    /// Implements IInitializable so a real VContainer LifetimeScope dispatches it automatically; a
    /// headless/test caller without a Unity PlayerLoop (see GameplaySessionComposer) calls Initialize()
    /// directly after resolving instead of relying on that dispatch.
    public sealed class GameSessionStartup : IInitializable
    {
        private readonly RecoveryService _recovery;
        private readonly WorldProgressionService _world;
        private readonly ClockService _clock;

        public GameSessionStartup(RecoveryService recovery, WorldProgressionService world, ClockService clock)
        {
            _recovery = recovery;
            _world = world;
            _clock = clock;
        }

        public void Initialize()
        {
            _world.RefreshAccessibility();
            _recovery.RunAutoAssignment(_clock.NowHours);
        }
    }
}
