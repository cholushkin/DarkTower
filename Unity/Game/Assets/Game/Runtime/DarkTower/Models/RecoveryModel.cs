// TODO: how bed-wait duration accrues for minions occupied by raids/training at night, and
//   treatment-queue tie-breaking among several Injured, are open design decisions.

using System.Collections.Generic;
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
    ///
    /// Bed-shortage wait tracking (since-hours per waiting minion) lives here rather than as a
    /// RecoveryService-private field, so the full session state remains inspectable/reconstructable
    /// from Models alone.
    public sealed class RecoveryModel : ModelBase
    {
        private readonly ReactiveProperty<int> _quartersBedCapacity;
        private readonly ReactiveProperty<int> _infirmaryBedCapacity;
        private readonly ObservableList<BedAssignmentModel> _assignments = new ObservableList<BedAssignmentModel>();
        private readonly Dictionary<EntityId, float> _waitingSinceHours = new Dictionary<EntityId, float>();

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

        internal void AddQuartersBeds(int delta) => _quartersBedCapacity.Value += delta;
        internal void AddInfirmaryBeds(int delta) => _infirmaryBedCapacity.Value += delta;

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

        internal void AddAssignment(BedAssignmentModel assignment) => _assignments.Add(assignment);

        internal bool RemoveAssignment(BedAssignmentModel assignment)
        {
            if (_assignments.Remove(assignment))
            {
                assignment.Dispose();
                return true;
            }
            return false;
        }

        /// Every currently-accumulating bed-shortage wait start time, for computing the next
        /// departure deadline.
        internal IReadOnlyCollection<float> WaitingSinceHoursValues => _waitingSinceHours.Values;

        internal bool TryGetWaitingSince(EntityId minionId, out float sinceHours) =>
            _waitingSinceHours.TryGetValue(minionId, out sinceHours);

        /// Starts the wait clock for a minion the first time it is observed free+idle+needing a bed
        /// with none available; a minion already waiting keeps its original since-time.
        internal void MarkWaitingIfAbsent(EntityId minionId, float nowHours)
        {
            if (!_waitingSinceHours.ContainsKey(minionId)) _waitingSinceHours[minionId] = nowHours;
        }

        internal void ClearWaiting(EntityId minionId) => _waitingSinceHours.Remove(minionId);

        protected override void OnDispose()
        {
            for (int i = 0; i < _assignments.Count; i++) _assignments[i].Dispose();
            _assignments.Clear();
            _waitingSinceHours.Clear();
        }
    }
}
