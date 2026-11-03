using NUnit.Framework;
using UnityEngine;

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
        public void SequentialSeriesStepsMustBePurchasedInOrder()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();

            var step0 = BuildSeriesStep("sale_eff_0", "sale_efficiency", 0);
            var step1 = BuildSeriesStep("sale_eff_1", "sale_efficiency", 1);
            GameplayTestFixtures.SetField(db, "roomUpgrades", new[] { h.ExtraBedUpgrade, h.UnlockTrainingUpgrade, step0, step1 });
            db.InvalidateCaches();

            using var session = GameplaySessionComposer.Create(db, 1);

            var outOfOrder = session.TowerUpgrades.TryPurchaseUpgrade(step1.Id);
            Assert.IsFalse(outOfOrder.Success);
            Assert.AreEqual("sequence_out_of_order", outOfOrder.Reason);
            Assert.IsFalse(session.Session.Tower.IsUpgradeCompleted(step1.Id));

            var inOrder = session.TowerUpgrades.TryPurchaseUpgrade(step0.Id);
            Assert.IsTrue(inOrder.Success);
            session.Scheduler.AdvanceGameTime(1f);
            Assert.IsTrue(session.Session.Tower.IsUpgradeCompleted(step0.Id));

            var nowAllowed = session.TowerUpgrades.TryPurchaseUpgrade(step1.Id);
            Assert.IsTrue(nowAllowed.Success, "Step 0 is now completed, so step 1 may be purchased.");
        }

        private static RoomUpgradeDefinition BuildSeriesStep(string upgradeId, string seriesId, int stepIndex)
        {
            var def = ScriptableObject.CreateInstance<RoomUpgradeDefinition>();
            GameplayTestFixtures.SetField(def, "upgradeId", upgradeId);
            GameplayTestFixtures.SetField(def, "roomId", RoomId.Storehouse);
            GameplayTestFixtures.SetField(def, "seriesId", seriesId);
            GameplayTestFixtures.SetField(def, "stepIndex", stepIndex);
            GameplayTestFixtures.SetField(def, "cost", GameplayTestFixtures.NewStruct<ResourceCost>(("gold", 1), ("crystals", 0), ("items", null)));
            GameplayTestFixtures.SetField(def, "duration", new GameDuration(0.1f, DurationUnit.Hours));
            GameplayTestFixtures.SetField(def, "effects", System.Array.Empty<UpgradeEffect>());
            GameplayTestFixtures.SetField(def, "towerXpReward", 0L);
            return def;
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
