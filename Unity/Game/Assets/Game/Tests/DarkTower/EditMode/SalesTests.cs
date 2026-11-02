using NUnit.Framework;

namespace Game.Tests
{
    public class SalesTests
    {
        [Test]
        public void NormalSaleRemovesStackImmediatelyAndPaysOnCompletion()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4); // sellPriceGold 2 => gross value 8
            var stack = session.Inventory.Stacks[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var result = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, session.Inventory.Stacks.Count, "Stack is removed the instant a sale starts.");
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "No payout before the timer completes.");

            // Duration = 1h base + 0.5h/copy * 4 copies = 3h; advance just short of it.
            session.Scheduler.AdvanceGameTime(2.9f);
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count, "Should not have completed yet.");
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);

            session.Scheduler.AdvanceGameTime(0.2f);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(goldBefore + 8, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void InstantSalePaysHalfValueImmediatelyWithNoTimer()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4); // gross value 8, half = 4
            var stack = session.Inventory.Stacks[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var result = session.Sales.TryStartSale(stack.Id, SaleKind.Instant);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, session.Inventory.Stacks.Count);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count, "Instant sale never creates a running timer.");
            Assert.AreEqual(goldBefore + 4, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void SaleDurationScalesWithQuantity()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 2);
            var smallStack = session.Inventory.Stacks[0];
            session.Sales.TryStartSale(smallStack.Id, SaleKind.Normal); // 1h + 0.5*2 = 2h

            session.Inventory.TryInsert(h.PlainItem, 8);
            var bigStack = session.Inventory.Stacks[0];
            session.Sales.TryStartSale(bigStack.Id, SaleKind.Normal); // 1h + 0.5*8 = 5h

            session.Scheduler.AdvanceGameTime(2.1f);
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count, "Only the smaller (shorter) sale should have completed.");

            session.Scheduler.AdvanceGameTime(3f);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
        }
    }
}
