using System.Collections.Generic;

namespace Game
{
    /// Dungeon prisoner admission (Claim only) and automatic processing. The three-outcome evaluation
    /// below is a documented implementation choice over fully authored data (weights + rank modifiers
    /// + the recruitment_efficiency upgrade), not an invented balance number: the design explicitly
    /// leaves the exact formula as a balance/implementation-pass decision.
    ///   recruitmentWeight = max(0, distribution.Recruitment + rankRecruitmentModifier(rank) + recruitmentEfficiencyBonus)
    ///   interrogationWeight = max(0, distribution.Interrogation); deathWeight = max(0, distribution.Death)
    ///   outcome drawn proportionally to these three weights (all-zero falls back to Death).
    /// Rank modifiers are treated as additive deltas (to a weight or to base hours), consistent with
    /// their "per-rank adjustment" authoring shape.
    public sealed class DungeonService : ITimedProcess
    {
        private readonly GameSessionModel _session;
        private readonly IRandomSource _random;
        private readonly ClockService _clock;
        private readonly WorldProgressionService _world;

        public DungeonService(GameSessionModel session, IRandomSource random, ClockService clock, WorldProgressionService world)
        {
            _session = session;
            _random = random;
            _clock = clock;
            _world = world;
        }

        private DungeonModel Dungeon => _session.Dungeon;
        private DungeonRulesDefinition Rules => _session.Database.DungeonRules;

        /// Sorts captured prisoners by rank (direction per DungeonRulesDefinition.SortHighRankFirst)
        /// and admits them into free cells one at a time, discarding any that don't fit. Called only
        /// from RaidService.TryClaimRaid. Capturing uses no Carry Capacity, so this never consults it.
        public void AdmitPrisoners(IReadOnlyList<CapturedPrisoner> prisoners, float nowHours)
        {
            if (prisoners == null || prisoners.Count == 0) return;

            var sorted = new List<CapturedPrisoner>(prisoners);
            bool highFirst = Rules != null && Rules.SortHighRankFirst;
            sorted.Sort((a, b) => highFirst ? b.Rank.CompareTo(a.Rank) : a.Rank.CompareTo(b.Rank));

            for (int i = 0; i < sorted.Count; i++)
            {
                if (Dungeon.FreeCells <= 0) break;
                AdmitOne(sorted[i].Rank, nowHours);
            }
        }

        private void AdmitOne(int rank, float nowHours)
        {
            float duration = ComputeProcessingDurationHours(rank);
            var id = _session.IdAllocator.Next();
            var prisoner = new PrisonerModel(id, rank, nowHours, nowHours + duration);
            Dungeon.AddPrisoner(prisoner);
        }

        public void ProcessDue(float nowHours)
        {
            var prisoners = Dungeon.Prisoners;
            for (int i = prisoners.Count - 1; i >= 0; i--)
            {
                var prisoner = prisoners[i];
                if (prisoner.DueGameHours > nowHours) continue;

                var outcome = RollOutcome(prisoner.Rank);
                prisoner.Resolve(outcome);
                ApplyOutcome(prisoner, outcome, nowHours);
                Dungeon.RemovePrisoner(prisoner);
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var prisoners = Dungeon.Prisoners;
            for (int i = 0; i < prisoners.Count; i++)
                if (min == null || prisoners[i].DueGameHours < min.Value) min = prisoners[i].DueGameHours;
            return min;
        }

        private PrisonerProcessOutcome RollOutcome(int rank)
        {
            var dist = Rules != null ? Rules.OutcomeDistribution : default;
            float recruitment = UnityEngine.Mathf.Max(0f, dist.RecruitmentWeight + RankModifierFor(Rules?.RankRecruitmentModifiers, rank)
                + UpgradeEffectRules.GetAdditiveAmount(_session.Tower, _session.Database, UpgradeEffectKind.IncreaseRecruitmentChance));
            float interrogation = UnityEngine.Mathf.Max(0f, dist.InterrogationWeight);
            float death = UnityEngine.Mathf.Max(0f, dist.DeathWeight);
            float total = recruitment + interrogation + death;
            if (total <= 0f) return PrisonerProcessOutcome.Death;

            float roll = _random.NextFloat01() * total;
            if (roll < recruitment) return PrisonerProcessOutcome.Recruitment;
            if (roll < recruitment + interrogation) return PrisonerProcessOutcome.Interrogation;
            return PrisonerProcessOutcome.Death;
        }

        private void ApplyOutcome(PrisonerModel prisoner, PrisonerProcessOutcome outcome, float nowHours)
        {
            switch (outcome)
            {
                case PrisonerProcessOutcome.Recruitment:
                {
                    var id = _session.IdAllocator.Next();
                    var condition = MinionCondition.None;
                    bool recruitedTired = Rules == null || Rules.RecruitedTired;
                    if (recruitedTired) condition |= MinionCondition.Tired;
                    float injuredProbability = Rules != null ? Rules.RecruitedInjuredProbability.Value : 0.5f;
                    if (_random.NextBool(injuredProbability)) condition |= MinionCondition.Injured;

                    var minion = new MinionModel(id, prisoner.Rank, condition, recruitedTired ? nowHours : (float?)null);
                    _session.Roster.Add(minion);
                    break;
                }
                case PrisonerProcessOutcome.Interrogation:
                    ApplyRandomInterrogationEvent();
                    break;
                case PrisonerProcessOutcome.Death:
                default:
                    break;
            }
        }

        /// Resolves a random interrogation event through the central world-accessibility rules rather
        /// than mutating mission state directly: an explicitly named unlock still respects tier
        /// gating (WorldProgressionService.TryUnlockMissionFromEvent), and a newly set flag triggers a
        /// full RefreshAccessibility so any OTHER mission whose own UnlockConditions reference that
        /// flag opens too, not only the missions this event names.
        private void ApplyRandomInterrogationEvent()
        {
            var events = Rules != null ? Rules.InterrogationEvents : null;
            if (events == null || events.Length == 0) return;

            var evt = events[_random.NextInt(0, events.Length - 1)];
            if (evt == null) return;

            bool flagChanged = !string.IsNullOrEmpty(evt.SetsWorldFlag) && !_session.World.HasFlag(evt.SetsWorldFlag);
            if (flagChanged) _session.World.SetFlag(evt.SetsWorldFlag);

            var unlocks = evt.UnlocksMissions;
            if (unlocks != null)
            {
                for (int i = 0; i < unlocks.Length; i++)
                {
                    var missionDef = unlocks[i];
                    if (missionDef != null) _world.TryUnlockMissionFromEvent(missionDef.Id);
                }
            }

            if (flagChanged) _world.RefreshAccessibility();
        }

        private float ComputeProcessingDurationHours(int rank)
        {
            float baseHours = Rules != null ? Rules.BaseProcessingDuration.ToHours(_clock.GameHoursPerDay) : 0f;
            float rankDelta = RankModifierFor(Rules?.RankDurationModifiers, rank);
            float factor = UpgradeEffectRules.GetDurationFactor(_session.Tower, _session.Database, UpgradeEffectKind.ReduceTortureDuration);
            return UnityEngine.Mathf.Max(0f, (baseHours + rankDelta) * factor);
        }

        private static float RankModifierFor(RankModifier[] modifiers, int rank)
        {
            if (modifiers == null) return 0f;
            for (int i = 0; i < modifiers.Length; i++)
                if (modifiers[i].Rank == rank) return modifiers[i].Modifier;
            return 0f;
        }
    }
}
