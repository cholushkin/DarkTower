using ObservableCollections;
using R3;

namespace Game
{
    /// Per-mission progression and current raid state. CompletedOnce is the permanent success
    /// checkmark and coexists with the current State. Weekly eligibility is tied to the launch week.
    public sealed class MissionProgressModel : ModelBase
    {
        private readonly ReactiveProperty<MissionState> _state;
        private readonly ReactiveProperty<bool> _completedOnce;
        private readonly ReactiveProperty<RaidModel> _currentRaid;

        public MissionProgressModel(string missionId, MissionState initialState)
        {
            MissionId = missionId;
            _state = new ReactiveProperty<MissionState>(initialState);
            _completedOnce = new ReactiveProperty<bool>(false);
            _currentRaid = new ReactiveProperty<RaidModel>(null);
            LastLaunchWeek = null;
            Disposables.Add(_state);
            Disposables.Add(_completedOnce);
            Disposables.Add(_currentRaid);
        }

        public string MissionId { get; }
        public ReadOnlyReactiveProperty<MissionState> State => _state;
        public ReadOnlyReactiveProperty<bool> CompletedOnce => _completedOnce;
        public ReadOnlyReactiveProperty<RaidModel> CurrentRaid => _currentRaid;

        /// In-game week index of the last launch; null if never launched. Launchable only in a later week.
        public int? LastLaunchWeek { get; private set; }

        public void SetState(MissionState state) => _state.Value = state;
        public void MarkCompletedOnce() => _completedOnce.Value = true;
        public void SetLastLaunchWeek(int week) => LastLaunchWeek = week;

        public void SetCurrentRaid(RaidModel raid)
        {
            var previous = _currentRaid.Value;
            if (previous != null && previous != raid) previous.Dispose();
            _currentRaid.Value = raid;
        }

        public void ClearCurrentRaid()
        {
            var previous = _currentRaid.Value;
            _currentRaid.Value = null;
            previous?.Dispose();
        }

        protected override void OnDispose()
        {
            _currentRaid.Value?.Dispose();
        }
    }

    /// World-map progression: per-mission progress, current accessible tier index, and secret/event
    /// flags. Tier/victory evaluation itself belongs to a future service; this holds the state it reads.
    public sealed class WorldModel : ModelBase
    {
        private readonly ObservableList<MissionProgressModel> _missions = new ObservableList<MissionProgressModel>();
        private readonly ObservableList<string> _worldFlags = new ObservableList<string>();
        private readonly ReactiveProperty<int> _currentTierIndex;

        public WorldModel(int currentTierIndex = 0)
        {
            _currentTierIndex = new ReactiveProperty<int>(currentTierIndex);
            Disposables.Add(_currentTierIndex);
        }

        public IReadOnlyObservableList<MissionProgressModel> Missions => _missions;
        public IReadOnlyObservableList<string> WorldFlags => _worldFlags;
        public ReadOnlyReactiveProperty<int> CurrentTierIndex => _currentTierIndex;

        public void AddMission(MissionProgressModel mission) => _missions.Add(mission);

        public MissionProgressModel FindMission(string missionId)
        {
            for (int i = 0; i < _missions.Count; i++)
                if (_missions[i].MissionId == missionId) return _missions[i];
            return null;
        }

        public void SetCurrentTierIndex(int index) => _currentTierIndex.Value = index;

        public bool HasFlag(string flag) => _worldFlags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !_worldFlags.Contains(flag)) _worldFlags.Add(flag);
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _missions.Count; i++) _missions[i].Dispose();
            _missions.Clear();
            _worldFlags.Clear();
        }
    }
}
