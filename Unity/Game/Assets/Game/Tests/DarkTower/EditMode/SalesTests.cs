using NUnit.Framework;

namespace Game.Tests
{
    public class SalesTests
    {
        [Test]
        public void NormalSaleReservesStackUntilCompletion()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4); // baseValueGold 2 => gross value 8
            var stack = session.Inventory.Stacks[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var result = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, session.Inventory.Stacks.Count, "Stack stays reserved in place while the sale is running.");
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "No payout before the timer completes.");

            // Duration = 1h base + 0.5h/copy * 4 copies = 3h; advance just short of it.
            session.Scheduler.AdvanceGameTime(2.9f);
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count, "Should not have completed yet.");
            Assert.AreEqual(1, session.Inventory.Stacks.Count);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);

            session.Scheduler.AdvanceGameTime(0.2f);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(0, session.Inventory.Stacks.Count, "Stack is removed only once the sale completes.");
            Assert.AreEqual(goldBefore + 8, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void CancellingARunningSaleRestoresIdleStateUntouched()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4);
            var stack = session.Inventory.Stacks[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var started = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(started.Success);

            var cancelled = session.Sales.TryCancelSale(started.Value);
            Assert.IsTrue(cancelled.Success);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(1, session.Inventory.Stacks.Count, "Stack is untouched by cancellation.");
            Assert.AreEqual(4, stack.Quantity.CurrentValue);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "Cancelling grants no gold.");

            // The stack is sellable again after cancelling.
            var restarted = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(restarted.Success);
        }

        [Test]
        public void RushingARunningSalePaysHalfValueAndRemovesTheStack()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4); // gross value 8, half = 4
            var stack = session.Inventory.Stacks[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var started = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(started.Success);

            var rushed = session.Sales.TryRushSale(started.Value);
            Assert.IsTrue(rushed.Success);
            Assert.AreEqual(4, rushed.Value);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
            Assert.AreEqual(0, session.Inventory.Stacks.Count);
            Assert.AreEqual(goldBefore + 4, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void CannotStartASecondSaleOnAnAlreadySellingStack()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 4);
            var stack = session.Inventory.Stacks[0];

            var first = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsTrue(first.Success);

            var second = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsFalse(second.Success);
            Assert.AreEqual("stack_already_selling", second.Reason);
        }

        [Test]
        public void ItemMarkedNotSellableCannotBeSold()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            GameplayTestFixtures.SetField(h.PlainItem, "isSellable", false);

            session.Inventory.TryInsert(h.PlainItem, 4);
            var stack = session.Inventory.Stacks[0];

            var result = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("item_not_sellable", result.Reason);
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

            // Two distinct items so the second insert can't merge into the first stack, which now
            // stays reserved (not removed) while its own sale is running.
            session.Inventory.TryInsert(h.PlainItem, 2);
            var smallStack = session.Inventory.Stacks[^1];
            session.Sales.TryStartSale(smallStack.Id, SaleKind.Normal); // 1h + 0.5*2 = 2h

            session.Inventory.TryInsert(h.TreasureItem, 5);
            var bigStack = session.Inventory.Stacks[^1];
            session.Sales.TryStartSale(bigStack.Id, SaleKind.Normal); // 1h + 0.5*5 = 3.5h

            session.Scheduler.AdvanceGameTime(2.1f);
            Assert.AreEqual(1, session.Session.Economy.ActiveSales.Count, "Only the smaller (shorter) sale should have completed.");

            session.Scheduler.AdvanceGameTime(1.5f);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count);
        }
    }
}
