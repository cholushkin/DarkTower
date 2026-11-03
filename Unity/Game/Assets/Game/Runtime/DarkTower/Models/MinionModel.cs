using System.Collections.Generic;
using ObservableCollections;
using R3;

namespace Game
{
    /// Effective combat stats after applying global research to a rank baseline.
    public readonly struct EffectiveStats
    {
        public readonly float Attack;
        public readonly float Defense;
        public EffectiveStats(float attack, float defense) { Attack = attack; Defense = defense; }
    }

    /// One individual owned minion. Minions are individuals, never item stacks.
    /// Rank, conditions and current activity are observed; derived stats are computed on demand.
    public sealed class MinionModel : ModelBase
    {
        private readonly ReactiveProperty<int> _rank;
        private readonly ReactiveProperty<MinionCondition> _condition;
        private readonly ReactiveProperty<MinionActivity> _activity;

        public MinionModel(EntityId id, int rank, MinionCondition condition, float? tiredSinceGameHours = null)
        {
            Id = id;
            _rank = new ReactiveProperty<int>(rank);
            _condition = new ReactiveProperty<MinionCondition>(condition);
            _activity = new ReactiveProperty<MinionActivity>(MinionActivity.Idle);
            TiredSinceGameHours = (condition & MinionCondition.Tired) != 0 ? tiredSinceGameHours : null;
            Disposables.Add(_rank);
            Disposables.Add(_condition);
            Disposables.Add(_activity);
        }

        public EntityId Id { get; }
        public ReadOnlyReactiveProperty<int> Rank => _rank;
        public ReadOnlyReactiveProperty<MinionCondition> Condition => _condition;
        public ReadOnlyReactiveProperty<MinionActivity> Activity => _activity;

        /// Game-time hour at which the current Tired state began; null when not Tired.
        /// Used for longest-tired bed priority and the bed-shortage departure rule.
        public float? TiredSinceGameHours { get; private set; }

        // Owned mutations. Call from the owning session service, not from presentation.

        internal void SetRank(int rank) => _rank.Value = rank;

        internal void SetActivity(MinionActivity activity) => _activity.Value = activity;

        internal void AddCondition(MinionCondition condition, float nowGameHours)
        {
            var before = _condition.Value;
            var after = before | condition;
            if ((condition & MinionCondition.Tired) != 0 && (before & MinionCondition.Tired) == 0)
                TiredSinceGameHours = nowGameHours;
            if (after != before) _condition.Value = after;
        }

        internal void RemoveCondition(MinionCondition condition)
        {
            var before = _condition.Value;
            var after = before & ~condition;
            if ((condition & MinionCondition.Tired) != 0) TiredSinceGameHours = null;
            if (after != before) _condition.Value = after;
        }

        public bool HasCondition(MinionCondition condition) => (_condition.Value & condition) != 0;

        /// Eligible to start a raid or training: idle, not Tired, not Injured.
        public bool IsAvailableForAssignment =>
            _activity.Value == MinionActivity.Idle && _condition.Value == MinionCondition.None;

        public EffectiveStats GetEffectiveStats(RankDefinition rankDef, IReadOnlyList<GlobalEffect> globalEffects)
        {
            if (rankDef == null) return new EffectiveStats(0f, 0f);
            float atk = StatCalculator.Effective(rankDef.Attack, CombatStat.Attack, globalEffects);
            float def = StatCalculator.Effective(rankDef.Defense, CombatStat.Defense, globalEffects);
            return new EffectiveStats(atk, def);
        }
    }

    /// The owned-minion roster. Population and aggregate weekly upkeep are derived on demand to
    /// avoid a second authoritative copy of state.
    public sealed class MinionRosterModel : ModelBase
    {
        private readonly ObservableList<MinionModel> _minions = new ObservableList<MinionModel>();

        public IReadOnlyObservableList<MinionModel> Minions => _minions;
        public int Count => _minions.Count;

        internal void Add(MinionModel minion) => _minions.Add(minion);

        internal bool Remove(MinionModel minion)
        {
            if (_minions.Remove(minion))
            {
                minion.Dispose();
                return true;
            }
            return false;
        }

        public MinionModel Find(EntityId id)
        {
            for (int i = 0; i < _minions.Count; i++)
                if (_minions[i].Id.Equals(id)) return _minions[i];
            return null;
        }

        /// Total weekly salary across every owned minion, using each rank's salary.
        public int ComputeWeeklySalary(DarkTowerDatabase db)
        {
            if (db == null) return 0;
            int total = 0;
            for (int i = 0; i < _minions.Count; i++)
            {
                var rankDef = db.GetRank(_minions[i].Rank.CurrentValue);
                if (rankDef != null) total += rankDef.SalaryGoldPerWeek;
            }
            return total;
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _minions.Count; i++) _minions[i].Dispose();
            _minions.Clear();
        }
    }
}
