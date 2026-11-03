using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class PayrollTests
    {
        [Test]
        public void SalaryDueSumsOwnedMinionsByRankAndExcludesPrisoners()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.AreEqual(30, session.Payroll.ComputeSalaryDue(), "3 starting Rank 1 minions at 10 gold/week each.");

            session.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(1) }, 0f);

            Assert.AreEqual(30, session.Payroll.ComputeSalaryDue(), "An un-recruited prisoner must not count toward salary.");
        }

        [Test]
        public void TryPayAllFailsWhenInsufficientAndChangesNothing()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            session.Economy.TrySpendGold(session.Economy.Gold.CurrentValue); // drain to 0

            var result = session.Payroll.TryPayAll();

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_resources", result.Reason, "TryPayAll forwards EconomyService.TrySpendGold's reason verbatim.");
            Assert.IsNull(session.Session.Payroll.LastPaidWeek);
        }

        [Test]
        public void TryPayAllDeductsExactlyTheDueAmountAndRecordsTheWeek()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            int goldBefore = session.Economy.Gold.CurrentValue;
            int due = session.Payroll.ComputeSalaryDue();

            var result = session.Payroll.TryPayAll();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(goldBefore - due, session.Economy.Gold.CurrentValue);
            Assert.AreEqual(0, session.Session.Payroll.LastPaidWeek);
        }

        [Test]
        public void SecondPaymentInTheSameWeekIsRejectedAndDoesNotDoubleDeduct()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.IsTrue(session.Payroll.TryPayAll().Success);
            int goldAfterFirstPayment = session.Economy.Gold.CurrentValue;

            var second = session.Payroll.TryPayAll();

            Assert.IsFalse(second.Success);
            Assert.AreEqual("already_paid_this_week", second.Reason);
            Assert.AreEqual(goldAfterFirstPayment, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void UnpaidPaydayBoundaryDeclaresBankruptcy()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Scheduler.AdvanceGameTime(24f * 7f + 1f); // past the first payday boundary (day 7), unpaid

            Assert.IsTrue(session.Session.IsBankrupt.CurrentValue);
            Assert.IsTrue(session.Session.IsTerminal);
        }

        [Test]
        public void PayingBeforeTheBoundaryAvoidsBankruptcy()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.IsTrue(session.Payroll.TryPayAll().Success);
            session.Scheduler.AdvanceGameTime(24f * 7f + 1f);

            Assert.IsFalse(session.Session.IsBankrupt.CurrentValue);
            Assert.IsFalse(session.Session.IsTerminal);
        }

        [Test]
        public void PaydayWindowForTheFirstPeriodIsOpenFromSessionStart()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.IsTrue(session.Session.Payroll.IsWindowOpen.CurrentValue,
                "A bill is accruing from hour 0, so the window must already be open rather than only appearing at the first boundary.");
            Assert.AreEqual(24f * 7f, session.Session.Payroll.DeadlineGameHours.Value, 0.01f);
        }

        [Test]
        public void PaydayCadenceFollowsPaydayIntervalDaysIndependentlyOfMissionWeekLength()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            // Payday every 3 game-days while missions still turn over weekly (7 days) — previously
            // PaydayIntervalDays was authored but never consumed, so payday silently followed
            // DaysPerWeek regardless of this setting.
            GameplayTestFixtures.SetField(db.GlobalBalance, "paydayIntervalDays", 3);
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.AreEqual(24f * 3f, session.Session.Payroll.DeadlineGameHours.Value, 0.01f);

            session.Scheduler.AdvanceGameTime(24f * 3f + 1f); // past the 3-day payday boundary, unpaid

            Assert.IsTrue(session.Session.IsBankrupt.CurrentValue, "Bankruptcy must follow the 3-day payday cadence, not the 7-day mission week.");
            Assert.AreEqual(0, session.Clock.CurrentWeek, "The mission week must still be governed by DaysPerWeek, unaffected by the payday cadence.");
        }
    }
}
