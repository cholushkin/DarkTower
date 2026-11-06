using Cysharp.Threading.Tasks;
using Game.UI.WindowSystem;
using ObservableCollections;
using R3;

namespace Game
{
    public sealed class StorehouseViewModel
    {
        private readonly GameSessionModel _session;
        private readonly SalesService _sales;
        private readonly ClockService _clock;
        private readonly IWindowService _windowService;

        public IReadOnlyObservableList<ItemStackModel> Items => _session.Inventory.Stacks;
        public ReadOnlyReactiveProperty<int> SlotCapacity => _session.Inventory.SlotCapacity;

        public StorehouseViewModel(GameSessionModel session, SalesService sales, ClockService clock, IWindowService windowService)
        {
            _session = session;
            _sales = sales;
            _clock = clock;
            _windowService = windowService;
        }

        /// Factory for a per-row ViewModel; the caller (StorehouseView, which owns dynamic row
        /// creation) is responsible for disposing it when the row is removed.
        public StorehouseItemViewModel CreateItemViewModel(ItemStackModel stack) =>
            new StorehouseItemViewModel(stack, _session.Economy, _sales, _clock, _session.Database.StorehouseRules);

        public void Close() => _windowService.HideAsync(this).Forget();
    }
}