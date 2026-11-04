using R3;

namespace Game
{
    /// Weekly payroll window state. The total due is derived from the roster at display time; this
    /// model only tracks the open window, its deadline, and the last paid week. Opening/closing the
    /// window pauses/resumes time via a host policy, which is not part of the data layer.
    public sealed class PayrollModel : ModelBase
    {
        private readonly ReactiveProperty<bool> _isWindowOpen;

        public PayrollModel()
        {
            _isWindowOpen = new ReactiveProperty<bool>(false);
            Disposables.Add(_isWindowOpen);
            DeadlineGameHours = null;
            LastPaidWeek = null;
        }

        public ReadOnlyReactiveProperty<bool> IsWindowOpen => _isWindowOpen;

        /// Game-time hour by which the current payday must be fully paid; null when no window is open.
        public float? DeadlineGameHours { get; private set; }

        /// In-game week index of the last fully paid payroll; null if never paid.
        public int? LastPaidWeek { get; private set; }

        internal void OpenWindow(float deadlineGameHours)
        {
            DeadlineGameHours = deadlineGameHours;
            _isWindowOpen.Value = true;
        }

        internal void CloseWindow()
        {
            _isWindowOpen.Value = false;
        }

        internal void MarkPaid(int week)
        {
            LastPaidWeek = week;
            DeadlineGameHours = null;
            _isWindowOpen.Value = false;
        }
    }
}
