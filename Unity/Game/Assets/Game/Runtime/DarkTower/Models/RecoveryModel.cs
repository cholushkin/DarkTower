// TODO: how bed-wait duration accrues for minions occupied by raids/training at night, and
//   treatment-queue tie-breaking among several Injured, are open design decisions.

using ObservableCollections;
using R3;

namespace Game
{
    /// One minion occupying a Quarters or Infirmary bed until its completion time.
    public sealed class BedAssignmentModel : ModelBase
    {
        public BedAssignmentModel(EntityId id, EntityId minionId, RoomId room,
            float startGameHours, float dueGameHours)
        {
            Id = id;
            MinionId = minionId;
            Room = room;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
        }

        public EntityId Id { get; }
        public EntityId MinionId { get; }
        public RoomId Room { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
    }

    /// Quarters and Infirmary bed capacities and current assignments. Waiting/queue sets are derived
    /// from the roster (who is Tired/Injured and unassigned), not stored as a second copy.
    public sealed class RecoveryModel : ModelBase
    {
        private readonly ReactiveProperty<int> _quartersBedCapacity;
        private readonly ReactiveProperty<int> _infirmaryBedCapacity;
        private readonly ObservableList<BedAssignmentModel> _assignments = new ObservableList<BedAssignmentModel>();

        public RecoveryModel(int quartersBeds, int infirmaryBeds)
        {
            _quartersBedCapacity = new ReactiveProperty<int>(quartersBeds);
            _infirmaryBedCapacity = new ReactiveProperty<int>(infirmaryBeds);
            Disposables.Add(_quartersBedCapacity);
            Disposables.Add(_infirmaryBedCapacity);
        }

        public ReadOnlyReactiveProperty<int> QuartersBedCapacity => _quartersBedCapacity;
        public ReadOnlyReactiveProperty<int> InfirmaryBedCapacity => _infirmaryBedCapacity;
        public IReadOnlyObservableList<BedAssignmentModel> Assignments => _assignments;

        public void AddQuartersBeds(int delta) => _quartersBedCapacity.Value += delta;
        public void AddInfirmaryBeds(int delta) => _infirmaryBedCapacity.Value += delta;

        public int OccupiedBeds(RoomId room)
        {
            int count = 0;
            for (int i = 0; i < _assignments.Count; i++)
                if (_assignments[i].Room == room) count++;
            return count;
        }

        public int FreeBeds(RoomId room)
        {
            int capacity = room == RoomId.Infirmary ? _infirmaryBedCapacity.CurrentValue : _quartersBedCapacity.CurrentValue;
            return capacity - OccupiedBeds(room);
        }

        public void AddAssignment(BedAssignmentModel assignment) => _assignments.Add(assignment);

        public bool RemoveAssignment(BedAssignmentModel assignment)
        {
            if (_assignments.Remove(assignment))
            {
                assignment.Dispose();
                return true;
            }
            return false;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _assignments.Count; i++) _assignments[i].Dispose();
            _assignments.Clear();
        }
    }
}
