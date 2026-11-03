// IDEA: a single authoritative game-time source/scheduler advances this clock; features register
//   timed operations against it rather than each running their own delay loop.
// TODO: event tie-breaking at midnight/nightfall/payday/raid-finish/upgrade-completion is an open
//   scheduler decision; this model only exposes the current time.

using R3;

namespace Game
{
    /// Authoritative game time, expressed as total elapsed game-hours. Day, week, hour-of-day and
    /// night state are derived from the configured hours-per-day / days-per-week.
    public sealed class ClockModel : ModelBase
    {
        private readonly ReactiveProperty<float> _totalGameHours;

        public ClockModel(float gameHoursPerDay, int daysPerWeek, float nightfallHourOfDay,
            int paydayIntervalDays = 0, float startGameHours = 0f)
        {
            GameHoursPerDay = gameHoursPerDay <= 0f ? 24f : gameHoursPerDay;
            DaysPerWeek = daysPerWeek < 1 ? 7 : daysPerWeek;
            NightfallHourOfDay = nightfallHourOfDay;
            PaydayIntervalDays = paydayIntervalDays < 1 ? DaysPerWeek : paydayIntervalDays;
            _totalGameHours = new ReactiveProperty<float>(startGameHours);
            Disposables.Add(_totalGameHours);
        }

        public float GameHoursPerDay { get; }
        public int DaysPerWeek { get; }
        public float NightfallHourOfDay { get; }

        /// Payday cadence in game-days. Independent of DaysPerWeek: missions become launchable again
        /// on a mission-week boundary, while payroll comes due on a payday-period boundary, and the
        /// two need not coincide.
        public int PaydayIntervalDays { get; }

        public ReadOnlyReactiveProperty<float> TotalGameHours => _totalGameHours;

        public int CurrentDay => (int)(_totalGameHours.CurrentValue / GameHoursPerDay);
        public int CurrentWeek => CurrentDay / DaysPerWeek;
        public int CurrentPaydayPeriod => CurrentDay / PaydayIntervalDays;
        public float HourOfDay => _totalGameHours.CurrentValue % GameHoursPerDay;
        public bool IsNight => HourOfDay >= NightfallHourOfDay;

        public void SetGameHours(float hours) => _totalGameHours.Value = hours;
        public void Advance(float deltaHours) => _totalGameHours.Value += deltaHours;
    }
}
