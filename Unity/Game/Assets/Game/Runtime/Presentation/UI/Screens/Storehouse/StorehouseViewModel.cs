using ObservableCollections;
using R3;

namespace Game
{
    public sealed class StorehouseViewModel
    {
        private readonly InventoryModel _inventory;

        public IReadOnlyObservableList<ItemStackModel> Items => _inventory.Stacks;
        public ReadOnlyReactiveProperty<int> SlotCapacity => _inventory.SlotCapacity;

        public StorehouseViewModel(InventoryModel inventory)
        {
            _inventory = inventory;
        }
    }
}