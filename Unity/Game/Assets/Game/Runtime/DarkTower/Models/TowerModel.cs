// TODO: XP award trigger (on purchase vs on completion) is unresolved; this model stores the awarded
//   total and lets a future service decide when AddXp is called.
// TODO: whether a Tower Level grants direct gameplay benefits is unresolved.

using ObservableCollections;
using R3;

namespace Game
{
    /// An in-progress room upgrade construction. Completed upgrades are tracked by id, not by level.
    public sealed class UpgradeProcessModel : ModelBase
    {
        private readonly ReactiveProperty<ActivityStatus> _status;

        public UpgradeProcessModel(EntityId id, string upgradeId, float startGameHours, float dueGameHours)
        {
            Id = id;
            UpgradeId = upgradeId;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
            _status = new ReactiveProperty<ActivityStatus>(ActivityStatus.Running);
            Disposables.Add(_status);
        }

        public EntityId Id { get; }
        public string UpgradeId { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
        public ReadOnlyReactiveProperty<ActivityStatus> Status => _status;

        internal void Complete() => _status.Value = ActivityStatus.Completed;
    }

    /// Hidden Tower XP and the completed/in-progress room upgrades. Tower Level is derived from XP
    /// against the configured progression, never stored as a second authoritative value.
    public sealed class TowerModel : ModelBase
    {
        private readonly ReactiveProperty<long> _totalXp;
        private readonly ObservableList<string> _completedUpgradeIds = new ObservableList<string>();
        private readonly ObservableList<UpgradeProcessModel> _inProgressUpgrades = new ObservableList<UpgradeProcessModel>();
        private readonly TowerLevelProgressionDefinition _progression;

        public TowerModel(long startingXp, TowerLevelProgressionDefinition progression)
        {
            _progression = progression;
            _totalXp = new ReactiveProperty<long>(startingXp);
            Disposables.Add(_totalXp);
        }

        public ReadOnlyReactiveProperty<long> TotalXp => _totalXp;
        public IReadOnlyObservableList<string> CompletedUpgradeIds => _completedUpgradeIds;
        public IReadOnlyObservableList<UpgradeProcessModel> InProgressUpgrades => _inProgressUpgrades;

        /// Current tower level derived from total XP and the configured thresholds.
        public int Level => TowerProgressionRules.DeriveLevel(_totalXp.CurrentValue, _progression);

        internal void AddXp(long amount) => _totalXp.Value += amount;

        public bool IsUpgradeCompleted(string upgradeId) => _completedUpgradeIds.Contains(upgradeId);

        internal void MarkUpgradeCompleted(string upgradeId)
        {
            if (!_completedUpgradeIds.Contains(upgradeId)) _completedUpgradeIds.Add(upgradeId);
        }

        internal void AddInProgress(UpgradeProcessModel process) => _inProgressUpgrades.Add(process);

        internal bool RemoveInProgress(UpgradeProcessModel process)
        {
            if (_inProgressUpgrades.Remove(process))
            {
                process.Dispose();
                return true;
            }
            return false;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _inProgressUpgrades.Count; i++) _inProgressUpgrades[i].Dispose();
            _inProgressUpgrades.Clear();
            _completedUpgradeIds.Clear();
        }
    }
}
