using NUnit.Framework;

namespace Game.Tests
{
    public class TrainingTests
    {
        [Test]
        public void LockedTransitionIsRejectedWithoutSpendingOrReservingAStation()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            int goldBefore = session.Economy.Gold.CurrentValue;
            int freeStationsBefore = session.Session.Training.FreeStations;

            var result = session.Training.TryStartTraining(minion.Id); // transition 1->2 not unlocked yet

            Assert.IsFalse(result.Success);
            Assert.AreEqual("transition_locked", result.Reason);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);
            Assert.AreEqual(freeStationsBefore, session.Session.Training.FreeStations);
            Assert.AreEqual(1, minion.Rank.CurrentValue);
        }

        [Test]
        public void UnlockedTrainingReservesStationAndAdvancesRankByExactlyOneOnCompletion()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Session.Training.UnlockTransition(1, 2);
            var minion = session.Minions.Roster.Minions[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            var result = session.Training.TryStartTraining(minion.Id);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(goldBefore - h.Rank1To2.Cost.Gold, session.Economy.Gold.CurrentValue);
            Assert.AreEqual(0, session.Session.Training.FreeStations);
            Assert.AreEqual(MinionActivity.Training, minion.Activity.CurrentValue);
            Assert.AreEqual(1, minion.Rank.CurrentValue, "Rank only changes on completion, not on start.");

            session.Scheduler.AdvanceGameTime(1f);

            Assert.AreEqual(2, minion.Rank.CurrentValue);
            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.AreEqual(1, session.Session.Training.FreeStations, "Station should be freed on completion.");
        }

        [Test]
        public void AdvancedTrainingUpgradeUnlocksTheTransition()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var upgrade = session.TowerUpgrades.TryPurchaseUpgrade(h.UnlockTrainingUpgrade.Id);
            Assert.IsTrue(upgrade.Success);
            Assert.IsFalse(session.Session.Training.IsTransitionUnlocked(1, 2), "Not unlocked until the upgrade completes.");

            session.Scheduler.AdvanceGameTime(1f);

            Assert.IsTrue(session.Session.Training.IsTransitionUnlocked(1, 2));

            var minion = session.Minions.Roster.Minions[0];
            var result = session.Training.TryStartTraining(minion.Id);
            Assert.IsTrue(result.Success);
        }
    }
}
