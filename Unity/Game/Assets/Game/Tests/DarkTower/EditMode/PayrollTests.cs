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
    }
}
