using ObservableCollections;
using R3;

namespace Game
{
    /// A timed or instant stack sale. For SaleKind.Normal, the stack stays reserved in InventoryModel
    /// until this sale completes, is rushed, or is cancelled (see SalesService) — only one Running
    /// sale may exist per stack at a time.
    public sealed class SaleOperationModel : ModelBase
    {
        private readonly ReactiveProperty<ActivityStatus> _status;

        public SaleOperationModel(EntityId id, EntityId stackId, SaleKind kind, int quantity,
            int grossValueGold, float startGameHours, float dueGameHours)
        {
            Id = id;
            StackId = stackId;
            Kind = kind;
            Quantity = quantity;
            GrossValueGold = grossValueGold;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
            _status = new ReactiveProperty<ActivityStatus>(ActivityStatus.Running);
            Disposables.Add(_status);
        }

        public EntityId Id { get; }
        public EntityId StackId { get; }
        public SaleKind Kind { get; }
        public int Quantity { get; }
        public int GrossValueGold { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
        public ReadOnlyReactiveProperty<ActivityStatus> Status => _status;

        internal void Complete() => _status.Value = ActivityStatus.Completed;
        internal void Cancel() => _status.Value = ActivityStatus.Cancelled;
    }

    /// Currencies and active sale operations. Gold/Crystals are HUD currencies, not inventory stacks.
    /// Unclaimed raid rewards never appear here until Claim credits them.
    public sealed class EconomyModel : ModelBase
    {
        private readonly ReactiveProperty<int> _gold;
        private readonly ReactiveProperty<int> _crystals;
        private readonly ObservableList<SaleOperationModel> _activeSales = new ObservableList<SaleOperationModel>();

        public EconomyModel(int gold, int crystals)
        {
            _gold = new ReactiveProperty<int>(gold);
            _crystals = new ReactiveProperty<int>(crystals);
            Disposables.Add(_gold);
            Disposables.Add(_crystals);
        }

        public ReadOnlyReactiveProperty<int> Gold => _gold;
        public ReadOnlyReactiveProperty<int> Crystals => _crystals;
        public IReadOnlyObservableList<SaleOperationModel> ActiveSales => _activeSales;

        internal void SetGold(int value) => _gold.Value = value;
        internal void AddGold(int delta) => _gold.Value += delta;
        internal void SetCrystals(int value) => _crystals.Value = value;
        internal void AddCrystals(int delta) => _crystals.Value += delta;

        internal void AddSale(SaleOperationModel sale) => _activeSales.Add(sale);

        internal bool RemoveSale(SaleOperationModel sale)
        {
            if (_activeSales.Remove(sale))
            {
                sale.Dispose();
                return true;
            }
            return false;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _activeSales.Count; i++) _activeSales[i].Dispose();
            _activeSales.Clear();
        }
    }
}
