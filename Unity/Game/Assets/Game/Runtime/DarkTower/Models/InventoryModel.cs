using ObservableCollections;
using R3;

namespace Game
{
    /// One physical stack occupying exactly one shared storage slot. Holds up to the item's
    /// max_stack_size copies. Treasure vs Item is a filter over the same pool, derived from the tag.
    public sealed class ItemStackModel : ModelBase
    {
        private readonly ReactiveProperty<int> _quantity;

        public ItemStackModel(EntityId id, ItemDefinition item, int quantity)
        {
            Id = id;
            Item = item;
            _quantity = new ReactiveProperty<int>(quantity);
            Disposables.Add(_quantity);
        }

        public EntityId Id { get; }
        public ItemDefinition Item { get; }
        public ReadOnlyReactiveProperty<int> Quantity => _quantity;

        public bool IsTreasure => Item != null && Item.IsTreasure;
        public bool IsFull => Item != null && _quantity.CurrentValue >= Item.MaxStackSize;

        public void SetQuantity(int quantity) => _quantity.Value = quantity;
        public void Add(int amount) => _quantity.Value += amount;
    }

    /// Shared item storage. Treasures and Items are filters over one slot pool; each stack uses one
    /// slot. Capacity grows through completed Storehouse upgrades. Currencies are not stored here.
    public sealed class InventoryModel : ModelBase
    {
        private readonly ReactiveProperty<int> _slotCapacity;
        private readonly ObservableList<ItemStackModel> _stacks = new ObservableList<ItemStackModel>();

        public InventoryModel(int slotCapacity)
        {
            _slotCapacity = new ReactiveProperty<int>(slotCapacity);
            Disposables.Add(_slotCapacity);
        }

        public ReadOnlyReactiveProperty<int> SlotCapacity => _slotCapacity;
        public IReadOnlyObservableList<ItemStackModel> Stacks => _stacks;

        /// Occupied slots equals the number of stacks (one slot per stack).
        public int UsedSlots => _stacks.Count;
        public int FreeSlots => _slotCapacity.CurrentValue - _stacks.Count;

        public void SetSlotCapacity(int capacity) => _slotCapacity.Value = capacity;
        public void AddSlotCapacity(int delta) => _slotCapacity.Value += delta;

        public void AddStack(ItemStackModel stack) => _stacks.Add(stack);

        public bool RemoveStack(ItemStackModel stack)
        {
            if (_stacks.Remove(stack))
            {
                stack.Dispose();
                return true;
            }
            return false;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _stacks.Count; i++) _stacks[i].Dispose();
            _stacks.Clear();
        }
    }
}
