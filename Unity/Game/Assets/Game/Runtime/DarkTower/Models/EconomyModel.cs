using ObservableCollections;
using R3;

namespace Game
{
    /// A timed or instant stack sale.
    /// TODO: whether the stack is reserved/removed when the sale starts, and how many sales may run
    /// at once, are open design decisions; this model carries the data either policy needs.
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

        public void Complete() => _status.Value = ActivityStatus.Completed;
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

        public void SetGold(int value) => _gold.Value = value;
        public void AddGold(int delta) => _gold.Value += delta;
        public void SetCrystals(int value) => _crystals.Value = value;
        public void AddCrystals(int delta) => _crystals.Value += delta;

        public void AddSale(SaleOperationModel sale) => _activeSales.Add(sale);

        public bool RemoveSale(SaleOperationModel sale)
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
