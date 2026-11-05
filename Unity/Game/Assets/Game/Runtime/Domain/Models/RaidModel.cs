// IDEA: a future RaidService computes outcome/injuries/duration from the launch snapshot and
//   GlobalBalanceDefinition coefficients; this model only stores inputs and resolved discoveries.

using System.Collections.Generic;
using ObservableCollections;
using R3;

namespace Game
{
    /// One squad member's effective parameters captured at raid launch.
    public readonly struct RaidMember
    {
        public readonly EntityId MinionId;
        public readonly int Rank;
        public readonly EffectiveStats Stats;
        public readonly float CarryCapacity;

        public RaidMember(EntityId minionId, int rank, EffectiveStats stats, float carryCapacity)
        {
            MinionId = minionId;
            Rank = rank;
            Stats = stats;
            CarryCapacity = carryCapacity;
        }
    }

    /// Effective parameters captured when a raid launches. Research completed afterward does not
    /// change these; the raid uses this snapshot.
    public readonly struct RaidLaunchSnapshot
    {
        public readonly RaidMember[] Members;
        public readonly int CarryLimit;

        public RaidLaunchSnapshot(RaidMember[] members, int carryLimit)
        {
            Members = members;
            CarryLimit = carryLimit;
        }

        public int SquadSize => Members?.Length ?? 0;
    }

    /// A single discovered physical item copy shown in the Raid Report. One selectable icon per copy;
    /// RaidCarryRules groups selected copies into stacks (by max_stack_size) to compute the carry
    /// slots a selection costs, the same accounting Storehouse uses for stored stacks.
    public sealed class DiscoveredItemCopy : ModelBase
    {
        private readonly ReactiveProperty<bool> _selected;

        public DiscoveredItemCopy(EntityId id, ItemDefinition item, bool selected)
        {
            Id = id;
            Item = item;
            _selected = new ReactiveProperty<bool>(selected);
            Disposables.Add(_selected);
        }

        public EntityId Id { get; }
        public ItemDefinition Item { get; }
        public ReadOnlyReactiveProperty<bool> Selected => _selected;

        internal void SetSelected(bool selected) => _selected.Value = selected;
    }

    /// A captured prisoner as discovered by a raid (pre-Claim data). Becomes a Dungeon PrisonerModel
    /// only at Claim. Prisoners consume no carry capacity.
    public readonly struct CapturedPrisoner
    {
        public readonly int Rank;
        public CapturedPrisoner(int rank) { Rank = rank; }
    }

    /// A raid: pending while RUNNING, then carrying a resolved outcome and fixed discoveries while
    /// COMPLETED_UNCLAIMED. The squad stays reserved until Claim, which happens outside this model.
    /// Discoveries are not credited to player balances before Claim.
    public sealed class RaidModel : ModelBase
    {
        private readonly ReactiveProperty<RaidOutcome> _outcome;
        private readonly ReactiveProperty<bool> _claimed;
        private readonly ObservableList<DiscoveredItemCopy> _discoveredItems = new ObservableList<DiscoveredItemCopy>();
        private readonly List<CapturedPrisoner> _capturedPrisoners = new List<CapturedPrisoner>();

        public RaidModel(EntityId id, string missionId, IReadOnlyList<EntityId> reservedSquad,
            int launchWeek, float startGameHours, float dueGameHours, RaidLaunchSnapshot snapshot)
        {
            Id = id;
            MissionId = missionId;
            // Defensive copy: the caller's list must not be able to corrupt release/injury resolution
            // by mutating it after launch (e.g. a List<EntityId> the caller keeps a reference to).
            var squadCopy = new EntityId[reservedSquad?.Count ?? 0];
            for (int i = 0; i < squadCopy.Length; i++) squadCopy[i] = reservedSquad[i];
            ReservedSquad = squadCopy;
            LaunchWeek = launchWeek;
            StartGameHours = startGameHours;
            DueGameHours = dueGameHours;
            LaunchSnapshot = snapshot;
            _outcome = new ReactiveProperty<RaidOutcome>(RaidOutcome.Pending);
            _claimed = new ReactiveProperty<bool>(false);
            Disposables.Add(_outcome);
            Disposables.Add(_claimed);
        }

        public EntityId Id { get; }
        public string MissionId { get; }
        public IReadOnlyList<EntityId> ReservedSquad { get; }
        public int LaunchWeek { get; }
        public float StartGameHours { get; }
        public float DueGameHours { get; }
        public RaidLaunchSnapshot LaunchSnapshot { get; }

        public ReadOnlyReactiveProperty<RaidOutcome> Outcome => _outcome;

        /// ADDED (Task 05): set exactly once by RaidService.TryClaimRaid. Kept separate from Outcome
        /// (which stays Success/Failure for display/testing) so Claim idempotency does not overload
        /// the result enum.
        public ReadOnlyReactiveProperty<bool> Claimed => _claimed;
        internal void MarkClaimed() => _claimed.Value = true;

        public IReadOnlyObservableList<DiscoveredItemCopy> DiscoveredItems => _discoveredItems;
        public IReadOnlyList<CapturedPrisoner> CapturedPrisoners => _capturedPrisoners;

        public int DiscoveredGold { get; private set; }
        public int DiscoveredCrystals { get; private set; }
        public int CarryLimit => LaunchSnapshot.CarryLimit;

        /// Fixes a successful result and its discoveries when the timer completes (still pre-Claim).
        internal void ResolveSuccess(int gold, int crystals,
            IEnumerable<DiscoveredItemCopy> items, IEnumerable<CapturedPrisoner> prisoners)
        {
            DiscoveredGold = gold;
            DiscoveredCrystals = crystals;
            if (items != null) foreach (var i in items) _discoveredItems.Add(i);
            if (prisoners != null) _capturedPrisoners.AddRange(prisoners);
            _outcome.Value = RaidOutcome.Success;
        }

        /// Fixes a failed result: no discoveries.
        internal void ResolveFailure() => _outcome.Value = RaidOutcome.Failure;

        /// Total carry slots used by currently selected copies; must not exceed CarryLimit. Copies of
        /// the same item stack by its max_stack_size before costing a slot, exactly like Storehouse.
        public int SelectedCarryUsed()
        {
            var selectedItems = new List<ItemDefinition>();
            for (int i = 0; i < _discoveredItems.Count; i++)
                if (_discoveredItems[i].Selected.CurrentValue) selectedItems.Add(_discoveredItems[i].Item);
            return RaidCarryRules.SlotsUsed(selectedItems);
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _discoveredItems.Count; i++) _discoveredItems[i].Dispose();
            _discoveredItems.Clear();
            _capturedPrisoners.Clear();
        }
    }
}
