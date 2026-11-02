using NUnit.Framework;

namespace Game.Tests
{
    public class RoomUpgradeResearchTests
    {
        [Test]
        public void PurchaseSchedulesButDoesNotActivateEffectUntilCompletion()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            int bedsBefore = session.Session.Recovery.QuartersBedCapacity.CurrentValue;
            long xpBefore = session.Session.Tower.TotalXp.CurrentValue;

            var result = session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id);
            Assert.IsTrue(result.Success);

            Assert.AreEqual(bedsBefore, session.Session.Recovery.QuartersBedCapacity.CurrentValue, "Effect must not be active yet.");
            Assert.AreEqual(xpBefore, session.Session.Tower.TotalXp.CurrentValue);
            Assert.IsFalse(session.Session.Tower.IsUpgradeCompleted(h.ExtraBedUpgrade.Id));

            session.Scheduler.AdvanceGameTime(1f);

            Assert.AreEqual(bedsBefore + 1, session.Session.Recovery.QuartersBedCapacity.CurrentValue);
            Assert.AreEqual(xpBefore + h.ExtraBedUpgrade.TowerXpReward, session.Session.Tower.TotalXp.CurrentValue);
            Assert.IsTrue(session.Session.Tower.IsUpgradeCompleted(h.ExtraBedUpgrade.Id));
        }

        [Test]
        public void SamePurchaseCannotBeStartedTwice()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.IsTrue(session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id).Success);
            var second = session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id);

            Assert.IsFalse(second.Success);
            Assert.AreEqual("already_in_progress", second.Reason);
        }

        [Test]
        public void OnlyOneActiveResearchAtATime()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var first = session.Research.TryStartResearch(h.Research1.Id);
            Assert.IsTrue(first.Success);

            var second = session.Research.TryStartResearch(h.Research1.Id);
            Assert.IsFalse(second.Success);
            Assert.AreEqual("research_in_progress", second.Reason);
        }

        [Test]
        public void ResearchEffectOnlyAffectsEffectiveStatsAfterCompletion()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var minion = session.Minions.Roster.Minions[0];
            float attackBefore = session.Minions.GetEffectiveStats(minion).Attack;

            session.Research.TryStartResearch(h.Research1.Id);
            Assert.AreEqual(attackBefore, session.Minions.GetEffectiveStats(minion).Attack, "No effect while research is only in progress.");

            session.Scheduler.AdvanceGameTime(1f);

            Assert.AreEqual(attackBefore + 2f, session.Minions.GetEffectiveStats(minion).Attack, "Research1 grants +2 Attack on completion.");
            Assert.IsTrue(session.Session.Research.IsCompleted(h.Research1.Id));
        }
    }
}
