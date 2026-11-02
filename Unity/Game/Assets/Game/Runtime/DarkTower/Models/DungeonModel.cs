// TODO: prisoner Rank sort direction at Claim (high->low vs low->high) is unresolved; the model
//   stores prisoners and ranks without imposing a direction.

using ObservableCollections;
using R3;

namespace Game
{
    /// A captured prisoner occupying one Dungeon cell, processed automatically. Keeps its Rank; a
    /// recruited prisoner becomes an owned minion of the same Rank.
    public sealed class PrisonerModel : ModelBase
    {
        private readonly ReactiveProperty<PrisonerProcessOutcome> _outcome;

        public PrisonerModel(EntityId id, int rank, float startGameHours, float dueGameHours)
        {
            Id = id;
            Rank = rank;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
            _outcome = new ReactiveProperty<PrisonerProcessOutcome>(PrisonerProcessOutcome.Pending);
            Disposables.Add(_outcome);
        }

        public EntityId Id { get; }
        public int Rank { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
        public ReadOnlyReactiveProperty<PrisonerProcessOutcome> Outcome => _outcome;

        public void Resolve(PrisonerProcessOutcome outcome) => _outcome.Value = outcome;
    }

    /// Dungeon cells and the prisoners in them. Cell capacity grows through completed upgrades.
    public sealed class DungeonModel : ModelBase
    {
        private readonly ReactiveProperty<int> _cellCapacity;
        private readonly ObservableList<PrisonerModel> _prisoners = new ObservableList<PrisonerModel>();

        public DungeonModel(int cellCapacity)
        {
            _cellCapacity = new ReactiveProperty<int>(cellCapacity);
            Disposables.Add(_cellCapacity);
        }

        public ReadOnlyReactiveProperty<int> CellCapacity => _cellCapacity;
        public IReadOnlyObservableList<PrisonerModel> Prisoners => _prisoners;

        public int OccupiedCells => _prisoners.Count;
        public int FreeCells => _cellCapacity.CurrentValue - _prisoners.Count;

        public void SetCellCapacity(int capacity) => _cellCapacity.Value = capacity;
        public void AddCellCapacity(int delta) => _cellCapacity.Value += delta;

        public void AddPrisoner(PrisonerModel prisoner) => _prisoners.Add(prisoner);

        public bool RemovePrisoner(PrisonerModel prisoner)
        {
            if (_prisoners.Remove(prisoner))
            {
                prisoner.Dispose();
                return true;
            }
            return false;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _prisoners.Count; i++) _prisoners[i].Dispose();
            _prisoners.Clear();
        }
    }
}
