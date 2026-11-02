namespace Game
{
    /// Minion queries and condition/stat helpers shared across other services. Owns no timers itself
    /// (see RecoveryService/TrainingService); this is the small shared surface for eligibility and
    /// effective-stat computation so raid/training/dungeon services don't duplicate it.
    public sealed class MinionService
    {
        private readonly GameSessionModel _session;

        public MinionService(GameSessionModel session)
        {
            _session = session;
        }

        private DarkTowerDatabase Db => _session.Database;
        public MinionRosterModel Roster => _session.Roster;

        public MinionModel Find(EntityId id) => Roster.Find(id);

        public bool IsAvailable(MinionModel minion) => minion != null && minion.IsAvailableForAssignment;

        public EffectiveStats GetEffectiveStats(MinionModel minion)
        {
            if (minion == null) return default;
            var rankDef = Db.GetRank(minion.Rank.CurrentValue);
            var effects = _session.Research.CollectActiveEffects();
            return minion.GetEffectiveStats(rankDef, effects);
        }

        public float GetCarryCapacity(MinionModel minion)
        {
            if (minion == null) return 0f;
            var rankDef = Db.GetRank(minion.Rank.CurrentValue);
            return rankDef != null ? rankDef.CarryCapacity : 0f;
        }

        public int ComputeWeeklySalary() => Roster.ComputeWeeklySalary(Db);

        /// Applies Tired to every owned minion, regardless of current activity/condition: nightfall
        /// affects the whole population per the design, independent of being mid-activity.
        public void ApplyNightfallTired(float nowHours)
        {
            var minions = Roster.Minions;
            for (int i = 0; i < minions.Count; i++)
                minions[i].AddCondition(MinionCondition.Tired, nowHours);
        }
    }
}
