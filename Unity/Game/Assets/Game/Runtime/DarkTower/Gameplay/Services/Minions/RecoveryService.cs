using System.Collections.Generic;
using VitalRouter;

namespace Game
{
    /// Automatic Quarters (Tired) and Infirmary (Injured) bed assignment. Injured takes priority over
    /// Tired (a minion with both goes to Infirmary; completing treatment clears both). Only free,
    /// idle, not-already-bedded minions are considered; a minion busy elsewhere (raiding/training, or
    /// a still-reserved just-finished raid) simply waits and is not pulled out of that activity.
    ///
    /// Bed-wait / departure timing (open design question, resolved here): the >2-day departure clock
    /// is tracked independently of MinionModel.TiredSinceGameHours (which marks when Tired began, even
    /// while the minion was still busy elsewhere) and instead starts only once a minion is observed
    /// free+idle+needing a bed with none available. This avoids unfairly penalizing a minion who was
    /// marked Tired at nightfall while still off on a multi-day raid. The wait-start times themselves
    /// live in RecoveryModel, not here, so the session stays fully inspectable from Models alone.
    ///
    /// Infirmary queue tie-break among multiple Injured waiters (also open in the design) is resolved
    /// by the same longest-waiting-first rule used for Quarters, reusing TiredSinceGameHours as the
    /// ordering key (in practice every path that sets Injured also sets Tired at the same instant).
    public sealed class RecoveryService : ITimedProcess
    {
        private readonly GameSessionModel _session;
        private readonly ClockService _clock;
        private readonly ICommandPublisher _publisher;

        public RecoveryService(GameSessionModel session, ClockService clock, ICommandPublisher publisher)
        {
            _session = session;
            _clock = clock;
            _publisher = publisher;
        }

        private RecoveryModel Recovery => _session.Recovery;
        private RecoveryRulesDefinition Rules => _session.Database.RecoveryRules;

        public bool IsBedded(EntityId minionId)
        {
            var assignments = Recovery.Assignments;
            for (int i = 0; i < assignments.Count; i++)
                if (assignments[i].MinionId.Equals(minionId)) return true;
            return false;
        }

        private bool NeedsInfirmary(MinionModel m) =>
            m.Activity.CurrentValue == MinionActivity.Idle &&
            m.HasCondition(MinionCondition.Injured) &&
            !IsBedded(m.Id);

        private bool NeedsQuarters(MinionModel m) =>
            m.Activity.CurrentValue == MinionActivity.Idle &&
            m.HasCondition(MinionCondition.Tired) &&
            !m.HasCondition(MinionCondition.Injured) &&
            !IsBedded(m.Id);

        /// Completes every bed assignment due at or before nowHours, applying the room's completion
        /// conditions and freeing the bed.
        public void ProcessDue(float nowHours)
        {
            var assignments = Recovery.Assignments;
            for (int i = assignments.Count - 1; i >= 0; i--)
            {
                var a = assignments[i];
                if (a.DueGameHours > nowHours) continue;

                var minion = _session.Roster.Find(a.MinionId);
                if (minion != null)
                {
                    if (a.Room == RoomId.Infirmary)
                        minion.RemoveCondition(MinionCondition.Injured | MinionCondition.Tired);
                    else
                        minion.RemoveCondition(MinionCondition.Tired);
                    minion.SetActivity(MinionActivity.Idle);
                }

                Recovery.RemoveAssignment(a);
            }
        }

        public float? NextDueHours()
        {
            float? min = null;
            var assignments = Recovery.Assignments;
            for (int i = 0; i < assignments.Count; i++)
                if (min == null || assignments[i].DueGameHours < min.Value) min = assignments[i].DueGameHours;

            float? departureDue = NextDepartureDueHours();
            if (departureDue.HasValue && (min == null || departureDue.Value < min.Value)) min = departureDue.Value;

            return min;
        }

        /// Earliest instant at which any minion currently accumulating a bed-shortage wait reaches
        /// the departure grace period, or null if nobody is waiting or no grace period is configured.
        /// Folded into NextDueHours so the scheduler can land a boundary exactly at that deadline
        /// instead of only detecting the departure whenever some unrelated event next happens to run.
        private float? NextDepartureDueHours()
        {
            if (Rules == null) return null;
            float graceHours = Rules.BedShortageDeparture.ToHours(_clock.GameHoursPerDay);
            if (graceHours <= 0f) return null;

            float? min = null;
            foreach (var since in Recovery.WaitingSinceHoursValues)
            {
                float due = since + graceHours;
                if (min == null || due < min.Value) min = due;
            }
            return min;
        }

        /// Assigns every currently-free bed to the longest-waiting eligible minion, Infirmary first.
        /// Call after any event that could free a bed or newly require one (nightfall, a completed
        /// activity returning a minion, a claimed raid, a completed bed assignment).
        public void RunAutoAssignment(float nowHours)
        {
            AssignRoom(RoomId.Infirmary, nowHours, NeedsInfirmary);
            AssignRoom(RoomId.Quarters, nowHours, NeedsQuarters);
            UpdateWaitTracking(nowHours);
        }

        private void AssignRoom(RoomId room, float nowHours, System.Func<MinionModel, bool> eligible)
        {
            while (Recovery.FreeBeds(room) > 0)
            {
                var candidate = PickLongestWaiting(eligible);
                if (candidate == null) break;
                AssignBed(candidate, room, nowHours);
            }
        }

        private MinionModel PickLongestWaiting(System.Func<MinionModel, bool> eligible)
        {
            var minions = _session.Roster.Minions;
            MinionModel best = null;
            float bestSince = float.PositiveInfinity;
            for (int i = 0; i < minions.Count; i++)
            {
                var m = minions[i];
                if (!eligible(m)) continue;
                float since = m.TiredSinceGameHours ?? 0f;
                if (best == null || since < bestSince)
                {
                    best = m;
                    bestSince = since;
                }
            }
            return best;
        }

        private void AssignBed(MinionModel minion, RoomId room, float nowHours)
        {
            float durationHours = room == RoomId.Infirmary
                ? ComputeTreatmentDurationHours()
                : (Rules != null ? Rules.RestDuration.ToHours(_clock.GameHoursPerDay) : 6f);

            var id = _session.IdAllocator.Next();
            var assignment = new BedAssignmentModel(id, minion.Id, room, nowHours, nowHours + durationHours);
            Recovery.AddAssignment(assignment);
            minion.SetActivity(room == RoomId.Infirmary ? MinionActivity.Treating : MinionActivity.Resting);
            Recovery.ClearWaiting(minion.Id);
        }

        private float ComputeTreatmentDurationHours()
        {
            float baseHours = Rules != null ? Rules.TreatmentDuration.ToHours(_clock.GameHoursPerDay) : 72f;
            float factor = UpgradeEffectRules.GetDurationFactor(_session.Tower, _session.Database, UpgradeEffectKind.ReduceTreatmentDuration);
            return UnityEngine.Mathf.Max(0f, baseHours * factor);
        }

        private void UpdateWaitTracking(float nowHours)
        {
            var minions = _session.Roster.Minions;
            for (int i = 0; i < minions.Count; i++)
            {
                var m = minions[i];
                bool needsBed = NeedsInfirmary(m) || NeedsQuarters(m);
                if (needsBed) Recovery.MarkWaitingIfAbsent(m.Id, nowHours);
                else Recovery.ClearWaiting(m.Id);
            }
        }

        /// Removes any minion that has gone longer than the configured grace period free/idle without
        /// a bed, publishing a MinionDepartedEvent for each. Call once per processed boundary.
        public void CheckBedShortageDepartures(float nowHours)
        {
            if (Rules == null) return;
            float graceHours = Rules.BedShortageDeparture.ToHours(_clock.GameHoursPerDay);
            if (graceHours <= 0f) return;

            var toRemove = new List<MinionModel>();
            var minions = _session.Roster.Minions;
            for (int i = 0; i < minions.Count; i++)
            {
                var m = minions[i];
                if (Recovery.TryGetWaitingSince(m.Id, out var since) && nowHours - since > graceHours)
                    toRemove.Add(m);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                var m = toRemove[i];
                Recovery.ClearWaiting(m.Id);
                _ = _publisher.PublishAsync(new MinionDepartedEvent(m.Id));
                _session.Roster.Remove(m);
            }
        }
    }
}
