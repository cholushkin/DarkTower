namespace Game
{
    /// Manual Training Hall assignments. The target rank is always current+1 (training only ever
    /// advances a minion exactly one adjacent rank at a time), so callers need only name the minion.
    ///
    /// Authoring convention (documented, not a data-contract change): an "advanced_training" upgrade's
    /// UnlockTrainingTransition effect carries TargetId in the same "{fromRank}-{toRank}" form as
    /// TrainingModel.TransitionKey (e.g. "1-2"), so TowerUpgradeService can unlock the matching
    /// transition on completion without a separate lookup table.
    public sealed class TrainingService : ITimedProcess
    {
        private readonly GameSessionModel _session;
        private readonly EconomyService _economy;
        private readonly ClockService _clock;

        public TrainingService(GameSessionModel session, EconomyService economy, ClockService clock)
        {
            _session = session;
            _economy = economy;
            _clock = clock;
        }

        private TrainingModel Training => _session.Training;
        private DarkTowerDatabase Db => _session.Database;

        public TrainingTransitionDefinition FindTransition(int fromRank, int toRank)
        {
            var transitions = Db.TrainingTransitions;
            if (transitions == null) return null;
            for (int i = 0; i < transitions.Count; i++)
            {
                var t = transitions[i];
                if (t != null && t.FromRank == fromRank && t.ToRank == toRank) return t;
            }
            return null;
        }

        public OperationResult<EntityId> TryStartTraining(EntityId minionId)
        {
            if (_session.IsTerminal) return OperationResult<EntityId>.Fail("game_over");

            var minion = _session.Roster.Find(minionId);
            if (minion == null) return OperationResult<EntityId>.Fail("minion_not_found");

            int fromRank = minion.Rank.CurrentValue;
            int toRank = fromRank + 1;
            if (toRank > 5) return OperationResult<EntityId>.Fail("max_rank");

            var transition = FindTransition(fromRank, toRank);
            if (transition == null) return OperationResult<EntityId>.Fail("transition_not_configured");

            if (!Training.IsTransitionUnlocked(fromRank, toRank))
                return OperationResult<EntityId>.Fail("transition_locked");

            if (!minion.IsAvailableForAssignment)
                return OperationResult<EntityId>.Fail("minion_unavailable");

            if (Training.FreeStations <= 0)
                return OperationResult<EntityId>.Fail("no_free_station");

            if (!_economy.CanAfford(transition.Cost))
                return OperationResult<EntityId>.Fail("insufficient_resources");

            var spend = _economy.TrySpend(transition.Cost);
            if (!spend.Success) return OperationResult<EntityId>.Fail(spend.Reason);

            float now = _clock.NowHours;
            float durationHours = transition.Duration.ToHours(_clock.GameHoursPerDay);
            var id = _session.IdAllocator.Next();
            var assignment = new TrainingAssignmentModel(id, minionId, fromRank, toRank, now, now + durationHours);
            Training.AddAssignment(assignment);
            minion.SetActivity(MinionActivity.Training);

            return OperationResult<EntityId>.Ok(id);
        }

        public void ProcessDue(float nowHours)
        {
            var assignments = Training.Assignments;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                var a = assignments[i];
                if (a.DueGameHours > nowHours) continue;

                var minion = _session.Roster.Find(a.MinionId);
                if (minion != null)
                {
                    minion.SetRank(a.ToRank);
                    minion.AddCondition(MinionCondition.Tired, nowHours);
                    minion.SetActivity(MinionActivity.Idle);
                }

                Training.RemoveAssignment(a);
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var assignments = Training.Assignments;
            for (int i = 0; i < assignments.Count; i++)
                if (min == null || assignments[i].DueGameHours < min.Value) min = assignments[i].DueGameHours;
            return min;
        }
    }
}
