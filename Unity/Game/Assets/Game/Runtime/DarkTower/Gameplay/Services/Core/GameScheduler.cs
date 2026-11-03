using System;

namespace Game
{
    /// The single orchestrator that moves the game clock forward. Every other service only reads the
    /// clock or reports its own next-due instant through ITimedProcess; this is the one place that
    /// advances time and decides processing order.
    ///
    /// AdvanceGameTime is an internal test/debug entry point and deliberately bypasses
    /// ClockService.IsPaused — a developer force-advances time even while a future Payroll/Raid
    /// Report pause would otherwise hold a real-time-driven loop. No real-time driver exists yet, so
    /// that distinction has no other observable effect today; a future runtime driver is expected to
    /// check IsPaused itself before calling AdvanceGameTime (never here), converting elapsed real
    /// seconds into game hours at a fixed ratio (e.g. 1 real minute = 1 in-game day), and to integrate
    /// with the payday and raid-report pause lifecycle through that same IsPaused gate.
    ///
    /// Boundary-jump algorithm: repeatedly advances to the SMALLEST of (remaining target time, next
    /// nightfall, next payday boundary, the minimum "next due" reported by every timed service), then
    /// processes everything due at that exact instant before taking another step. This guarantees a
    /// large jump (or many events sharing an instant) completes every due timer exactly once, in a
    /// fixed and documented order, never skipping one. The fixed order below is a technical
    /// scheduling choice, not a gameplay-priority decision: raids never auto-credit anything at this
    /// point (only Claim does), so nothing in this order can change a gameplay outcome by being
    /// processed before or after payday/nightfall on the same instant. The one deliberate exception:
    /// within ProcessBoundary, nightfall's effects and every ITimedProcess's due work always run
    /// before the trailing recovery auto-assignment/departure pass, so bed assignment at a boundary
    /// always reflects that same-instant nightfall outcome rather than racing it.
    public sealed class GameScheduler
    {
        private readonly ClockService _clock;
        private readonly GameSessionModel _session;
        private readonly MinionService _minions;
        private readonly RecoveryService _recovery;
        private readonly PayrollService _payroll;
        private readonly WorldProgressionService _world;

        /// Every timed process GameScheduler drives, in the fixed order ProcessBoundary processes
        /// them — a technical scheduling choice (see class remarks), not a gameplay-priority decision.
        private readonly ITimedProcess[] _timedProcesses;

        public GameScheduler(
            ClockService clock,
            GameSessionModel session,
            MinionService minions,
            RecoveryService recovery,
            TrainingService training,
            SalesService sales,
            ResearchService research,
            TowerUpgradeService towerUpgrades,
            DungeonService dungeon,
            RaidService raids,
            PayrollService payroll,
            WorldProgressionService world)
        {
            _clock = clock;
            _session = session;
            _minions = minions;
            _recovery = recovery;
            _payroll = payroll;
            _world = world;
            _timedProcesses = new ITimedProcess[] { recovery, training, sales, research, towerUpgrades, dungeon, raids };
        }

        public void AdvanceGameTime(float hours)
        {
            if (hours <= 0f) return;
            float target = _clock.NowHours + hours;

            // Run one auto-assignment pass up front so a bed freed by something that happened
            // between the previous AdvanceGameTime call and this one (e.g. a TryClaimRaid releasing
            // a squad) is picked up before new time elapses.
            _recovery.RunAutoAssignment(_clock.NowHours);

            const int MAX_STEPS = 100000; // guards against a misconfigured zero-length boundary loop
            for (int step = 0; step < MAX_STEPS && _clock.NowHours < target; step++)
            {
                float now = _clock.NowHours;
                float next = target;

                float nightfall = _clock.NextNightfallAfter(now);
                float payday = _clock.NextPaydayBoundaryAfter(now);
                float weekBoundary = _clock.NextWeekBoundaryAfter(now);
                next = Math.Min(next, nightfall);
                next = Math.Min(next, payday);
                next = Math.Min(next, weekBoundary);
                next = Math.Min(next, NextDueAcrossServices(now));

                if (next > target) next = target;
                if (next <= now) next = Math.Min(target, now + 0.0001f); // float-safety, always progresses

                bool isNightfall = Approximately(next, nightfall);
                bool isPayday = Approximately(next, payday);
                bool isWeekBoundary = Approximately(next, weekBoundary);

                _clock.AdvanceTo(next);
                ProcessBoundary(next, isNightfall, isPayday, isWeekBoundary);
            }
        }

        private void ProcessBoundary(float now, bool isNightfall, bool isPayday, bool isWeekBoundary)
        {
            if (isNightfall) _minions.ApplyNightfallTired(now);
            if (isWeekBoundary) _world.OnWeekAdvanced(_clock.CurrentWeek);

            for (int i = 0; i < _timedProcesses.Length; i++) _timedProcesses[i].ProcessDue(now);

            if (isPayday) _payroll.EvaluatePaydayBoundary(now, _clock.CurrentPaydayPeriod);

            _recovery.RunAutoAssignment(now);
            _recovery.CheckBedShortageDepartures(now);
        }

        private float NextDueAcrossServices(float now)
        {
            float min = float.PositiveInfinity;
            for (int i = 0; i < _timedProcesses.Length; i++) min = Min(min, _timedProcesses[i].NextDueHours());
            return min;
        }

        private static float Min(float current, float? candidate) =>
            candidate.HasValue && candidate.Value < current ? candidate.Value : current;

        private static bool Approximately(float a, float b) => Math.Abs(a - b) < 0.0001f;
    }
}
