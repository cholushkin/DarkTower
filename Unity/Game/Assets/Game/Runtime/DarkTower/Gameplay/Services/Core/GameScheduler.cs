using System;

namespace Game
{
    /// The single orchestrator that moves the game clock forward. Every other service only reads the
    /// clock or stores its own Start/Due game-hours on its own Task-04 process models; this is the
    /// one place that advances time and decides processing order.
    ///
    /// AdvanceGameTime is an internal test/debug entry point ("direct time advancement is an internal
    /// test/debug capability only" per the design) and deliberately bypasses ClockService.IsPaused —
    /// a developer force-advances time even while a future Payroll/Raid Report pause would otherwise
    /// hold a real-time-driven loop. Nothing in this task drives the clock from real time, so that
    /// distinction has no other observable effect yet; a future runtime driver is expected to check
    /// IsPaused itself before calling in (see ClockService remarks), never here.
    ///
    /// Boundary-jump algorithm: repeatedly advances to the SMALLEST of (remaining target time, next
    /// nightfall, next payday boundary, the minimum "next due" reported by every timed service), then
    /// processes everything due at that exact instant before taking another step. This guarantees a
    /// large jump (or many events sharing an instant) completes every due timer exactly once, in a
    /// fixed and documented order, never skipping one. The fixed order below is a technical
    /// scheduling choice, not a gameplay-priority decision: raids never auto-credit anything at this
    /// point (only Claim does), so nothing in this order can change a gameplay outcome by being
    /// processed before or after payday/nightfall on the same instant — see GameplayServices_README.md
    /// for the one caveat called out there (simultaneous nightfall + bed auto-assignment ordering).
    public sealed class GameScheduler
    {
        private readonly ClockService _clock;
        private readonly GameSessionModel _session;
        private readonly MinionService _minions;
        private readonly RecoveryService _recovery;
        private readonly TrainingService _training;
        private readonly SalesService _sales;
        private readonly ResearchService _research;
        private readonly TowerUpgradeService _towerUpgrades;
        private readonly DungeonService _dungeon;
        private readonly RaidService _raids;
        private readonly PayrollService _payroll;

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
            PayrollService payroll)
        {
            _clock = clock;
            _session = session;
            _minions = minions;
            _recovery = recovery;
            _training = training;
            _sales = sales;
            _research = research;
            _towerUpgrades = towerUpgrades;
            _dungeon = dungeon;
            _raids = raids;
            _payroll = payroll;
        }

        public void AdvanceGameTime(float hours)
        {
            if (hours <= 0f) return;
            float target = _clock.NowHours + hours;

            // Run one auto-assignment pass up front so a bed freed by something that happened
            // between the previous AdvanceGameTime call and this one (e.g. a TryClaimRaid releasing
            // a squad) is picked up before new time elapses.
            _recovery.RunAutoAssignment(_clock.NowHours);

            const int maxSteps = 100000; // guards against a misconfigured zero-length boundary loop
            for (int step = 0; step < maxSteps && _clock.NowHours < target; step++)
            {
                float now = _clock.NowHours;
                float next = target;

                float nightfall = _clock.NextNightfallAfter(now);
                float payday = _clock.NextPaydayBoundaryAfter(now);
                next = Math.Min(next, nightfall);
                next = Math.Min(next, payday);
                next = Math.Min(next, NextDueAcrossServices(now));

                if (next > target) next = target;
                if (next <= now) next = Math.Min(target, now + 0.0001f); // float-safety, always progresses

                bool isNightfall = Approximately(next, nightfall);
                bool isPayday = Approximately(next, payday);

                _clock.AdvanceTo(next);
                ProcessBoundary(next, isNightfall, isPayday);
            }
        }

        private void ProcessBoundary(float now, bool isNightfall, bool isPayday)
        {
            if (isNightfall) _minions.ApplyNightfallTired(now);

            _recovery.ProcessDue(now);
            _training.ProcessDue(now);
            _sales.ProcessDue(now);
            _research.ProcessDue(now);
            _towerUpgrades.ProcessDue(now);
            _dungeon.ProcessDue(now);
            _raids.ProcessDue(now);

            if (isPayday) _payroll.EvaluatePaydayBoundary(now, _clock.CurrentWeek);

            _recovery.RunAutoAssignment(now);
            _recovery.CheckBedShortageDepartures(now);
        }

        private float NextDueAcrossServices(float now)
        {
            float min = float.PositiveInfinity;
            min = Min(min, _recovery.NextDueHours());
            min = Min(min, _training.NextDueHours());
            min = Min(min, _sales.NextDueHours());
            min = Min(min, _research.NextDueHours());
            min = Min(min, _towerUpgrades.NextDueHours());
            min = Min(min, _dungeon.NextDueHours());
            min = Min(min, _raids.NextDueHours());
            return min;
        }

        private static float Min(float current, float? candidate) =>
            candidate.HasValue && candidate.Value < current ? candidate.Value : current;

        private static bool Approximately(float a, float b) => Math.Abs(a - b) < 0.0001f;
    }
}
