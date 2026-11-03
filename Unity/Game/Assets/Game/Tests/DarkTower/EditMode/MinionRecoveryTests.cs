using NUnit.Framework;

namespace Game.Tests
{
    public class MinionRecoveryTests
    {
        [Test]
        public void TiredAndInjuredCanCoexist()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            minion.AddCondition(MinionCondition.Tired, 0f);
            minion.AddCondition(MinionCondition.Injured, 0f);

            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.IsTrue(minion.HasCondition(MinionCondition.Injured));
        }

        [Test]
        public void InjuredTakesPriorityOverTiredForBedAssignment()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minions = session.Minions.Roster.Minions;
            minions[0].AddCondition(MinionCondition.Tired, 0f);
            minions[0].AddCondition(MinionCondition.Injured, 0f); // both -> must go to Infirmary
            minions[1].AddCondition(MinionCondition.Tired, 0f);
            minions[2].AddCondition(MinionCondition.Tired, 0f);

            session.Recovery.RunAutoAssignment(0f);

            Assert.AreEqual(MinionActivity.Treating, minions[0].Activity.CurrentValue);
            Assert.AreEqual(MinionActivity.Resting, minions[1].Activity.CurrentValue);
            Assert.AreEqual(MinionActivity.Resting, minions[2].Activity.CurrentValue);
        }

        [Test]
        public void AutoAssignmentPicksLongestTiredFirstWhenBedsAreScarce()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase(); // 2 Quarters beds, 3 minions
            using var session = GameplaySessionComposer.Create(db, 1);

            var minions = session.Minions.Roster.Minions;
            var oldest = minions[0];
            var middle = minions[1];
            var newest = minions[2];

            newest.AddCondition(MinionCondition.Tired, 2f);
            oldest.AddCondition(MinionCondition.Tired, 0f);
            middle.AddCondition(MinionCondition.Tired, 1f);

            session.Recovery.RunAutoAssignment(2f);

            Assert.IsTrue(session.Recovery.IsBedded(oldest.Id));
            Assert.IsTrue(session.Recovery.IsBedded(middle.Id));
            Assert.IsFalse(session.Recovery.IsBedded(newest.Id), "Only 2 beds for 3 waiting minions; the most recently Tired one should still be waiting.");
        }

        [Test]
        public void TreatmentClearsBothConditionsAndFreesBed()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            minion.AddCondition(MinionCondition.Tired, 0f);
            minion.AddCondition(MinionCondition.Injured, 0f);
            session.Recovery.RunAutoAssignment(0f);
            Assert.AreEqual(MinionActivity.Treating, minion.Activity.CurrentValue);

            Assert.AreEqual(1, session.Session.Recovery.OccupiedBeds(RoomId.Infirmary));

            session.Recovery.ProcessDue(72.1f); // default treatment duration is 3 game-days (72h)

            Assert.IsFalse(minion.HasCondition(MinionCondition.Tired));
            Assert.IsFalse(minion.HasCondition(MinionCondition.Injured));
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
            Assert.AreEqual(session.Session.Recovery.InfirmaryBedCapacity.CurrentValue, session.Session.Recovery.FreeBeds(RoomId.Infirmary));
        }

        [Test]
        public void RestClearsOnlyTired()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            minion.AddCondition(MinionCondition.Tired, 0f);
            session.Recovery.RunAutoAssignment(0f);
            Assert.AreEqual(MinionActivity.Resting, minion.Activity.CurrentValue);

            session.Recovery.ProcessDue(6.1f); // default rest duration is 6 game-hours

            Assert.IsFalse(minion.HasCondition(MinionCondition.Tired));
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
        }

        [Test]
        public void BedShortageDepartureDeadlineIsObservableAsTheServicesNextDueTime()
        {
            // 1 Infirmary bed; occupy it, then make a second minion wait with none free. Previously
            // the >2-day departure clock was invisible to the scheduler, so a departure could only be
            // caught whenever some unrelated boundary (nightfall, payday, another timer) next ran.
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minions = session.Minions.Roster.Minions;
            minions[0].AddCondition(MinionCondition.Injured, 0f);
            minions[1].AddCondition(MinionCondition.Injured, 0f);
            session.Recovery.RunAutoAssignment(0f);

            Assert.AreEqual(MinionActivity.Treating, minions[0].Activity.CurrentValue);
            Assert.AreEqual(MinionActivity.Idle, minions[1].Activity.CurrentValue, "No Infirmary bed left; still waiting.");

            float graceHours = session.Session.Database.RecoveryRules.BedShortageDeparture.ToHours(session.Clock.GameHoursPerDay);
            var nextDue = session.Recovery.NextDueHours();

            Assert.IsTrue(nextDue.HasValue);
            Assert.AreEqual(graceHours, nextDue.Value, 0.01f,
                "The waiting minion's departure deadline must be observable as the service's next due time.");
        }

        [Test]
        public void RunAutoAssignmentNeverDoubleAssignsTheSameMinion()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            minion.AddCondition(MinionCondition.Tired, 0f);

            session.Recovery.RunAutoAssignment(0f);
            session.Recovery.RunAutoAssignment(0f);
            session.Recovery.RunAutoAssignment(0f);

            int assignmentsForMinion = 0;
            var assignments = session.Session.Recovery.Assignments;
            for (int i = 0; i < assignments.Count; i++)
                if (assignments[i].MinionId.Equals(minion.Id)) assignmentsForMinion++;

            Assert.AreEqual(1, assignmentsForMinion);
        }
    }
}
