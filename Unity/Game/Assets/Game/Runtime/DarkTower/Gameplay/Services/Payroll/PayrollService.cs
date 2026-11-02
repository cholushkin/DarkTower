using System;
using VitalRouter;

namespace Game
{
    /// Weekly payroll: full-payment-only, manual Pay All, and the bankruptcy terminal state.
    ///
    /// PayrollModel.IsWindowOpen/DeadlineGameHours are reinterpreted here (Task 04 added the fields
    /// with no consumer yet) as "a payday period is currently pending its deadline" — NOT "the
    /// Payroll popup is on screen". The latter is purely a ClockService pause token obtained via
    /// RequestPayrollPause(), per the design's "a UI screen may request pausing through a game-owned
    /// policy" — rWS/the clock never need to know why time is paused.
    ///
    /// Payday deadline (open catalog question: GlobalBalanceDefinition only authors
    /// paydayIntervalDays, no separate grace-window field): resolved as "the start of the next payday
    /// interval", i.e. the player has the full week to pay before the next payday boundary closes the
    /// previous one out. This needs no new config value and matches "every seven days, payday begins".
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

            int currentWeek = _clock.CurrentWeek;
            if (Payroll.LastPaidWeek.HasValue && Payroll.LastPaidWeek.Value >= currentWeek)
                return OperationResult.Fail("already_paid_this_week");

            int due = ComputeSalaryDue();
            var spend = _economy.TrySpendGold(due);
            if (!spend.Success) return OperationResult.Fail(spend.Reason);

            Payroll.MarkPaid(currentWeek);
            return OperationResult.Ok;
        }

        /// Called by GameScheduler at each payday boundary (the start of week N, N >= 1): checks that
        /// week N-1's bill was paid, declares bankruptcy if not, then opens the next payday window.
        public void EvaluatePaydayBoundary(float nowHours, int weekJustStarted)
        {
            if (weekJustStarted <= 0 || _session.IsTerminal) return;

            int weekToCheck = weekJustStarted - 1;
            bool paid = Payroll.LastPaidWeek.HasValue && Payroll.LastPaidWeek.Value >= weekToCheck;
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
