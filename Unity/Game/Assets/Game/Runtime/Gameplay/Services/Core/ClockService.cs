using System;
using R3;

namespace Game
{
    /// Thin gameplay-owned wrapper around ClockModel. Owns only current time + the pause ref-count;
    /// it does not itself advance time or know about any specific activity (see GameScheduler, which
    /// is the only caller allowed to move the clock forward). Kept dependency-free so it is trivially
    /// testable on its own (clock conversion, nested pause).
    public sealed class ClockService
    {
        private readonly ClockModel _clock;
        private int _pauseCount;

        public ClockService(ClockModel clock)
        {
            _clock = clock;
        }

        public float GameHoursPerDay => _clock.GameHoursPerDay;
        public int DaysPerWeek => _clock.DaysPerWeek;
        public int PaydayIntervalDays => _clock.PaydayIntervalDays;
        public float NightfallHourOfDay => _clock.NightfallHourOfDay;

        public ReadOnlyReactiveProperty<float> TotalGameHours => _clock.TotalGameHours;
        public float NowHours => _clock.TotalGameHours.CurrentValue;
        public int CurrentDay => _clock.CurrentDay;
        public int CurrentWeek => _clock.CurrentWeek;
        public int CurrentPaydayPeriod => _clock.CurrentPaydayPeriod;
        public float HourOfDay => _clock.HourOfDay;
        public bool IsNight => _clock.IsNight;

        /// True while any pause request is outstanding. A future presentation layer that drives the
        /// clock from real time must check this before converting elapsed real seconds into game
        /// hours; it does not gate GameScheduler.AdvanceGameTime, which is a test/debug entry point
        /// (see GameScheduler remarks).
        public bool IsPaused => _pauseCount > 0;

        /// Requests a pause; disposing the returned token releases this specific request. Nested /
        /// overlapping requests (e.g. Payroll and Raid Report both open) only resume once every
        /// outstanding request has been released.
        public IDisposable RequestPause()
        {
            _pauseCount++;
            return new PauseHandle(this);
        }

        private void ReleasePause()
        {
            if (_pauseCount > 0) _pauseCount--;
        }

        /// Only GameScheduler may call this: moves the clock to an absolute game-hour instant.
        internal void AdvanceTo(float hours) => _clock.SetGameHours(hours);

        /// Smallest instant strictly after afterHours at which nightfall occurs.
        public float NextNightfallAfter(float afterHours)
        {
            float day = (float)Math.Floor(afterHours / GameHoursPerDay);
            float candidate = day * GameHoursPerDay + NightfallHourOfDay;
            if (candidate <= afterHours) candidate += GameHoursPerDay;
            return candidate;
        }

        /// Smallest instant strictly after afterHours at which the mission week advances (every
        /// DaysPerWeek game-days). Missions become launchable again on this boundary, independent of
        /// the payday cadence below.
        public float NextWeekBoundaryAfter(float afterHours) => NextBoundaryAfter(afterHours, DaysPerWeek);

        /// Smallest instant strictly after afterHours at which a payday period boundary occurs
        /// (every PaydayIntervalDays game-days, starting after the first full period).
        public float NextPaydayBoundaryAfter(float afterHours) => NextBoundaryAfter(afterHours, PaydayIntervalDays);

        private float NextBoundaryAfter(float afterHours, int intervalDays)
        {
            float periodHours = intervalDays * GameHoursPerDay;
            if (periodHours <= 0f) return float.PositiveInfinity;
            double periodIndex = Math.Floor(afterHours / periodHours) + 1;
            return (float)(periodIndex * periodHours);
        }

        private sealed class PauseHandle : IDisposable
        {
            private ClockService _owner;

            public PauseHandle(ClockService owner) => _owner = owner;

            public void Dispose()
            {
                if (_owner == null) return;
                _owner.ReleasePause();
                _owner = null;
            }
        }
    }
}
