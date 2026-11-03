using NUnit.Framework;

namespace Game.Tests
{
    public class EconomyInventoryTests
    {
        [Test]
        public void TrySpendGoldFailsWhenInsufficientAndChangesNothing()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            int goldBefore = session.Economy.Gold.CurrentValue;
            var result = session.Economy.TrySpendGold(goldBefore + 1000);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_resources", result.Reason);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void CurrencyNeverGoesNegative()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var result = session.Economy.TrySpendGold(session.Economy.Gold.CurrentValue + 1);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_resources", result.Reason);
            Assert.GreaterOrEqual(session.Economy.Gold.CurrentValue, 0);
        }

        [Test]
        public void TryInsertFillsPartialStacksBeforeCreatingNewOnes()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var first = session.Inventory.TryInsert(h.PlainItem, 4); // maxStackSize is 10
            Assert.AreEqual(4, first.Accepted);
            Assert.AreEqual(0, first.Discarded);
            Assert.AreEqual(1, session.Inventory.Stacks.Count);

            var second = session.Inventory.TryInsert(h.PlainItem, 4); // fills the same stack up to 8
            Assert.AreEqual(4, second.Accepted);
            Assert.AreEqual(1, session.Inventory.Stacks.Count, "Should fill the existing partial stack, not create a new one.");
            Assert.AreEqual(8, session.Inventory.Stacks[0].Quantity.CurrentValue);

            var third = session.Inventory.TryInsert(h.PlainItem, 5); // 2 to finish this stack, 3 start a new one
            Assert.AreEqual(5, third.Accepted);
            Assert.AreEqual(2, session.Inventory.Stacks.Count);
            Assert.AreEqual(10, session.Inventory.Stacks[0].Quantity.CurrentValue);
            Assert.AreEqual(3, session.Inventory.Stacks[1].Quantity.CurrentValue);
        }

        [Test]
        public void TryInsertDiscardsOverflowWhenSlotsAreFull()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            int capacity = session.Inventory.FreeSlots; // 5, per the fixture
            for (int i = 0; i < capacity; i++)
                session.Inventory.TryInsert(h.PlainItem, 10); // force a distinct full stack each time

            Assert.AreEqual(0, session.Inventory.FreeSlots);

            var overflow = session.Inventory.TryInsert(h.PlainItem, 3);
            Assert.AreEqual(0, overflow.Accepted);
            Assert.AreEqual(3, overflow.Discarded);
            Assert.AreEqual(capacity, session.Inventory.Stacks.Count, "Overflow must not create or grow a stack.");
        }

        [Test]
        public void TryRemoveQuantityFailsWithoutMutationWhenInsufficient()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 3);
            var result = session.Inventory.TryRemoveQuantity(h.PlainItem, 10);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_items", result.Reason);
            Assert.AreEqual(3, session.Inventory.QuantityOf(h.PlainItem));
        }

        [Test]
        public void TryRemoveQuantityDeletesEmptiedStacks()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 3);
            var result = session.Inventory.TryRemoveQuantity(h.PlainItem, 3);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, session.Inventory.Stacks.Count);
        }

        [Test]
        public void MixedCurrencyAndItemCostIsAllOrNothing()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            // Enough gold, but not enough of the required item — the whole purchase must fail and
            // neither the gold nor (absent) item quantity should move.
            session.Inventory.TryInsert(h.PlainItem, 1);
            var cost = GameplayTestFixtures.NewStruct<ResourceCost>(
                ("gold", 5),
                ("crystals", 0),
                ("items", new[] { GameplayTestFixtures.NewStruct<ItemAmount>(("item", h.PlainItem), ("amount", 5)) }));

            int goldBefore = session.Economy.Gold.CurrentValue;
            var result = session.Economy.TrySpend(cost);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_resources", result.Reason);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);
            Assert.AreEqual(1, session.Inventory.QuantityOf(h.PlainItem));
        }

        [Test]
        public void DuplicateItemCostEntriesAreAggregatedAndRemainAtomic()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Inventory.TryInsert(h.PlainItem, 5); // only 5 copies on hand

            // The same item appears twice (3 + 5 = 8), more than the 5 actually held. Each entry
            // previously passed CanAfford independently against the full 5 held, so the deduction
            // removed 3, then failed (silently) trying to remove the remaining 5 from only 2 left.
            var cost = GameplayTestFixtures.NewStruct<ResourceCost>(
                ("gold", 0),
                ("crystals", 0),
                ("items", new[]
                {
                    GameplayTestFixtures.NewStruct<ItemAmount>(("item", h.PlainItem), ("amount", 3)),
                    GameplayTestFixtures.NewStruct<ItemAmount>(("item", h.PlainItem), ("amount", 5)),
                }));

            var result = session.Economy.TrySpend(cost);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("insufficient_resources", result.Reason);
            Assert.AreEqual(5, session.Inventory.QuantityOf(h.PlainItem), "A failed spend must not remove any of the held copies.");
        }

        [Test]
        public void StartingStacksNeverExceedConfiguredStorageCapacity()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase(); // 5 storage slots
            GameplayTestFixtures.SetField(db.StartingBalance, "startingItemStacks", new[]
            {
                GameplayTestFixtures.NewStruct<ItemAmount>(("item", h.PlainItem), ("amount", 100)) // 10 stacks' worth (maxStackSize 10)
            });

            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.LessOrEqual(session.Inventory.Stacks.Count, session.Session.Inventory.SlotCapacity.CurrentValue,
                "Starting stacks authored beyond StorageSlots must never produce an over-capacity inventory.");
            Assert.AreEqual(0, session.Inventory.FreeSlots);
        }
    }
}
