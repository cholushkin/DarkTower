using ObservableCollections;
using R3;

namespace Game
{
    /// One active training-station assignment raising a minion by exactly one rank.
    public sealed class TrainingAssignmentModel : ModelBase
    {
        public TrainingAssignmentModel(EntityId id, EntityId minionId, int fromRank, int toRank,
            float startGameHours, float dueGameHours)
        {
            Id = id;
            MinionId = minionId;
            FromRank = fromRank;
            ToRank = toRank;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
        }

        public EntityId Id { get; }
        public EntityId MinionId { get; }
        public int FromRank { get; }
        public int ToRank { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
    }

    /// Training Hall station capacity, active assignments, and which rank transitions are unlocked
    /// (via completed advanced-training upgrades).
    public sealed class TrainingModel : ModelBase
    {
        private readonly ReactiveProperty<int> _stationCapacity;
        private readonly ObservableList<TrainingAssignmentModel> _assignments = new ObservableList<TrainingAssignmentModel>();
        private readonly ObservableList<string> _unlockedTransitions = new ObservableList<string>();

        public TrainingModel(int stationCapacity)
        {
            _stationCapacity = new ReactiveProperty<int>(stationCapacity);
            Disposables.Add(_stationCapacity);
        }

        public ReadOnlyReactiveProperty<int> StationCapacity => _stationCapacity;
        public IReadOnlyObservableList<TrainingAssignmentModel> Assignments => _assignments;
        public IReadOnlyObservableList<string> UnlockedTransitions => _unlockedTransitions;

        public int OccupiedStations => _assignments.Count;
        public int FreeStations => _stationCapacity.CurrentValue - _assignments.Count;

        public void AddStations(int delta) => _stationCapacity.Value += delta;

        public bool IsTransitionUnlocked(int fromRank, int toRank) =>
            _unlockedTransitions.Contains(TransitionKey(fromRank, toRank));

        public void UnlockTransition(int fromRank, int toRank)
        {
            var key = TransitionKey(fromRank, toRank);
            if (!_unlockedTransitions.Contains(key)) _unlockedTransitions.Add(key);
        }

        public void AddAssignment(TrainingAssignmentModel assignment) => _assignments.Add(assignment);

        public bool RemoveAssignment(TrainingAssignmentModel assignment)
        {
            if (_assignments.Remove(assignment))
            {
                assignment.Dispose();
                return true;
            }
            return false;
        }

        public static string TransitionKey(int fromRank, int toRank) => fromRank + "-" + toRank;

        protected override void OnDispose()
        {
            for (int i = 0; i < _assignments.Count; i++) _assignments[i].Dispose();
            _assignments.Clear();
            _unlockedTransitions.Clear();
        }
    }
}
