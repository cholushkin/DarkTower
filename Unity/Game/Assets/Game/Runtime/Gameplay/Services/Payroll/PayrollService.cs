using System;
using VitalRouter;

namespace Game
{
    /// Weekly payroll: full-payment-only, manual Pay All, and the bankruptcy terminal state.
    ///
    /// PayrollModel.IsWindowOpen/DeadlineGameHours mean "a payday period is currently pending its
    /// deadline" — NOT "the Payroll popup is on screen". The latter is purely a ClockService pause
    /// token obtained via RequestPayrollPause(); rWS/the clock never need to know why time is paused.
    ///
    /// Payday deadline (open catalog question: GlobalBalanceDefinition authors paydayIntervalDays
    /// with no separate grace-window field): resolved as "the start of the next payday period", i.e.
    /// the player has the full period to pay before the next boundary closes the previous one out.
    /// A window for payday period 0 is opened explicitly at session start (OpenInitialWindow) so
    /// IsWindowOpen/DeadlineGameHours are meaningful for the whole first period too, not only from
    /// the first boundary onward.
    public sealed class PayrollService
    {
        private readonly GameSessionModel _session;
        private readonly EconomyService _economy;
        private readonly ClockService _clock;
        private readonly ICommandPublisher _publisher;

        public PayrollService(GameSessionModel session, EconomyService economy, ClockService clock, ICommandPublisher publisher)
        {
            _session = session;
            _economy = economy;
            _clock = clock;
            _publisher = publisher;
        }

        private PayrollModel Payroll => _session.Payroll;

        public int ComputeSalaryDue() => _session.WeeklySalary;

        public bool CanPayAll => _economy.Gold.CurrentValue >= ComputeSalaryDue();

        /// Gameplay-owned pause request a future Payroll UI adapter calls while its popup is open.
        public IDisposable RequestPayrollPause() => _clock.RequestPause();

        public OperationResult TryPayAll()
        {
            if (_session.IsTerminal) return OperationResult.Fail("game_over");

            int currentPeriod = _clock.CurrentPaydayPeriod;
            if (Payroll.LastPaidWeek.HasValue && Payroll.LastPaidWeek.Value >= currentPeriod)
                return OperationResult.Fail("already_paid_this_week");

            int due = ComputeSalaryDue();
            var spend = _economy.TrySpendGold(due);
            if (!spend.Success) return OperationResult.Fail(spend.Reason);

            Payroll.MarkPaid(currentPeriod);
            return OperationResult.Ok;
        }

        /// Opens the payday-period-0 window at session start, so a Payroll UI has a consistent
        /// "payment pending, due by this deadline" signal for the whole first period rather than only
        /// from the first boundary onward. Called once by GameSessionStartup.
        public void OpenInitialWindow()
        {
            if (_session.IsTerminal) return;
            Payroll.OpenWindow(_clock.NextPaydayBoundaryAfter(_clock.NowHours));
        }

        /// Called by GameScheduler at each payday boundary (the start of payday period N, N >= 1):
        /// checks that period N-1's bill was paid, declares bankruptcy if not, then opens the next
        /// payday window.
        public void EvaluatePaydayBoundary(float nowHours, int periodJustStarted)
        {
            if (periodJustStarted <= 0 || _session.IsTerminal) return;

            int periodToCheck = periodJustStarted - 1;
            bool paid = Payroll.LastPaidWeek.HasValue && Payroll.LastPaidWeek.Value >= periodToCheck;
            if (!paid)
            {
                _session.DeclareBankruptcy();
                _ = _publisher.PublishAsync(new BankruptcyDeclaredEvent());
                return;
            }

            float nextDeadline = _clock.NextPaydayBoundaryAfter(nowHours);
            Payroll.OpenWindow(nextDeadline);
        }
    }
}
