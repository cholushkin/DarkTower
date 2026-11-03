using System.Collections.Generic;
using ObservableCollections;
using R3;

namespace Game
{
    /// The single active Laboratory research process. There is no queue; at most one exists.
    public sealed class ResearchProcessModel : ModelBase
    {
        private readonly ReactiveProperty<ActivityStatus> _status;

        public ResearchProcessModel(EntityId id, string researchId, float startGameHours, float dueGameHours)
        {
            Id = id;
            ResearchId = researchId;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
            _status = new ReactiveProperty<ActivityStatus>(ActivityStatus.Running);
            Disposables.Add(_status);
        }

        public EntityId Id { get; }
        public string ResearchId { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
        public ReadOnlyReactiveProperty<ActivityStatus> Status => _status;

        internal void Complete() => _status.Value = ActivityStatus.Completed;
    }

    /// Completed research and the at-most-one active process. Permanent global effects are resolved
    /// from the completed research definitions rather than stored as a separate writable copy.
    public sealed class ResearchModel : ModelBase
    {
        private readonly ObservableList<string> _completedResearchIds = new ObservableList<string>();
        private readonly ReactiveProperty<ResearchProcessModel> _activeProcess;
        private readonly DarkTowerDatabase _db;

        public ResearchModel(DarkTowerDatabase db, int baseUnlockedResearchTier = 0)
        {
            _db = db;
            BaseUnlockedResearchTier = baseUnlockedResearchTier;
            _activeProcess = new ReactiveProperty<ResearchProcessModel>(null);
            Disposables.Add(_activeProcess);
        }

        public IReadOnlyObservableList<string> CompletedResearchIds => _completedResearchIds;
        public ReadOnlyReactiveProperty<ResearchProcessModel> ActiveProcess => _activeProcess;
        public bool HasActiveProcess => _activeProcess.CurrentValue != null;

        /// Starting research tier captured from StartingBalanceDefinition.InitialRoomCapabilities at
        /// session creation — the single source of truth SessionStateFactory already reads every
        /// other starting capacity from. ResearchService adds completed UnlockResearchTier effects on
        /// top of this rather than re-reading StartingBalanceDefinition itself.
        public int BaseUnlockedResearchTier { get; }

        public bool IsCompleted(string researchId) => _completedResearchIds.Contains(researchId);

        internal void MarkCompleted(string researchId)
        {
            if (!_completedResearchIds.Contains(researchId)) _completedResearchIds.Add(researchId);
        }

        internal void SetActiveProcess(ResearchProcessModel process)
        {
            var previous = _activeProcess.Value;
            if (previous != null && previous != process) previous.Dispose();
            _activeProcess.Value = process;
        }

        internal void ClearActiveProcess()
        {
            var previous = _activeProcess.Value;
            _activeProcess.Value = null;
            previous?.Dispose();
        }

        /// All global effects currently active, aggregated from completed research definitions.
        public List<GlobalEffect> CollectActiveEffects()
        {
            var result = new List<GlobalEffect>();
            if (_db == null) return result;
            for (int i = 0; i < _completedResearchIds.Count; i++)
            {
                var def = _db.GetResearch(_completedResearchIds[i]);
                if (def?.Effects == null) continue;
                result.AddRange(def.Effects);
            }
            return result;
        }

        protected override void OnDispose()
        {
            _activeProcess.Value?.Dispose();
            _completedResearchIds.Clear();
        }
    }
}
