namespace Game
{
    /// Result of inserting copies into shared Storehouse storage: how many copies were accepted into
    /// stacks versus silently discarded because no partial stack or free slot could hold them.
    public readonly struct InsertResult
    {
        public readonly int Accepted;
        public readonly int Discarded;
        public InsertResult(int accepted, int discarded) { Accepted = accepted; Discarded = discarded; }
    }

    /// Storehouse operations: shared Treasures/Items stack-slot storage. Treasures vs Items is a
    /// display filter over this single pool (ItemDefinition.IsTreasure); there is no second capacity
    /// system. Currencies never occupy slots here.
    public sealed class InventoryService
    {
        private readonly GameSessionModel _session;
        private InventoryModel Inventory => _session.Inventory;

        public InventoryService(GameSessionModel session)
        {
            _session = session;
        }

        public ObservableCollections.IReadOnlyObservableList<ItemStackModel> Stacks => Inventory.Stacks;
        public int FreeSlots => Inventory.FreeSlots;

        public ItemStackModel FindStack(EntityId id)
        {
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Id.Equals(id)) return stacks[i];
            return null;
        }

        public int QuantityOf(ItemDefinition item)
        {
            if (item == null) return 0;
            int total = 0;
            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Item == item) total += stacks[i].Quantity.CurrentValue;
            return total;
        }

        public bool HasQuantity(ItemDefinition item, int amount) => QuantityOf(item) >= amount;

        /// Fills compatible partial stacks first, then creates new stacks in free slots. Any copies
        /// that still don't fit are silently discarded (per the Claim-overflow rule), and this method
        /// is also the general-purpose insertion path for non-Claim sources (e.g. starting stacks).
        public InsertResult TryInsert(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return new InsertResult(0, 0);

            int maxStack = item.MaxStackSize < 1 ? 1 : item.MaxStackSize;
            int remaining = amount;

            var stacks = Inventory.Stacks;
            for (int i = 0; i < stacks.Count && remaining > 0; i++)
            {
                var stack = stacks[i];
                if (stack.Item != item || stack.IsFull) continue;
                int space = maxStack - stack.Quantity.CurrentValue;
                int add = space < remaining ? space : remaining;
                if (add <= 0) continue;
                stack.Add(add);
                remaining -= add;
            }

            while (remaining > 0 && Inventory.FreeSlots > 0)
            {
                int add = remaining < maxStack ? remaining : maxStack;
                Inventory.AddStack(new ItemStackModel(_session.IdAllocator.Next(), item, add));
                remaining -= add;
            }

            int discarded = remaining;
            return new InsertResult(amount - discarded, discarded);
        }

        /// Validates then removes exactly `amount` copies of `item` across one or more stacks,
        /// deleting any stack that becomes empty. Fails without mutation if the quantity is short.
        public OperationResult TryRemoveQuantity(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return OperationResult.Fail("invalid_request");
            if (!HasQuantity(item, amount)) return OperationResult.Fail("insufficient_items");

            int remaining = amount;
            var stacks = Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = stacks[i];
                if (stack.Item != item) continue;
                int take = stack.Quantity.CurrentValue < remaining ? stack.Quantity.CurrentValue : remaining;
                if (take >= stack.Quantity.CurrentValue) Inventory.RemoveStack(stack);
                else stack.Add(-take);
                remaining -= take;
            }

            return OperationResult.Ok;
        }

        /// Removes an entire stack regardless of quantity (full-stack sale, both sale kinds).
        public OperationResult TryRemoveStack(EntityId stackId)
        {
            var stack = FindStack(stackId);
            if (stack == null) return OperationResult.Fail("stack_not_found");
            Inventory.RemoveStack(stack);
            return OperationResult.Ok;
        }
    }
}
