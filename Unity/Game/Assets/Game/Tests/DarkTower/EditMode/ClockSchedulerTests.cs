using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class ClockSchedulerTests
    {
        [Test]
        public void ClockConvertsHoursToDaysAndWeeks()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.AreEqual(24f, session.Clock.GameHoursPerDay);
            Assert.AreEqual(7, session.Clock.DaysPerWeek);

            session.Scheduler.AdvanceGameTime(26f);

            Assert.AreEqual(1, session.Clock.CurrentDay);
            Assert.AreEqual(0, session.Clock.CurrentWeek);
            Assert.AreEqual(2f, session.Clock.HourOfDay, 0.01f);
        }

        [Test]
        public void NestedPauseOnlyResumesWhenAllRequestsReleased()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            Assert.IsFalse(session.Clock.IsPaused);
            var first = session.Clock.RequestPause();
            var second = session.Clock.RequestPause();
            Assert.IsTrue(session.Clock.IsPaused);

            first.Dispose();
            Assert.IsTrue(session.Clock.IsPaused, "Still paused while one request remains outstanding.");

            second.Dispose();
            Assert.IsFalse(session.Clock.IsPaused);
        }

        [Test]
        public void AdvanceGameTimeAdvancesExactlyTheRequestedAmount()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            float before = session.Clock.NowHours;
            session.Scheduler.AdvanceGameTime(5f);

            Assert.AreEqual(before + 5f, session.Clock.NowHours, 0.001f);
        }

        [Test]
        public void OneLargeAdvanceCompletesEveryDueActivityAcrossEveryService()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var researchResult = session.Research.TryStartResearch(h.Research1.Id);
            var upgradeResult = session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id);

            session.Session.Training.UnlockTransition(1, 2);
            var minion = session.Minions.Roster.Minions[0];
            var trainingResult = session.Training.TryStartTraining(minion.Id);

            session.Inventory.TryInsert(h.PlainItem, 3);
            var stack = session.Inventory.Stacks[0];
            var saleResult = session.Sales.TryStartSale(stack.Id, SaleKind.Normal);

            Assert.IsTrue(researchResult.Success);
            Assert.IsTrue(upgradeResult.Success);
            Assert.IsTrue(trainingResult.Success);
            Assert.IsTrue(saleResult.Success);

            int goldBefore = session.Economy.Gold.CurrentValue;
            int bedsBefore = session.Session.Recovery.QuartersBedCapacity.CurrentValue;

            session.Scheduler.AdvanceGameTime(3f);

            Assert.IsTrue(session.Research.NextDueHours() == null, "Research should have completed.");
            Assert.IsTrue(session.Session.Research.IsCompleted(h.Research1.Id));
            Assert.IsTrue(session.Session.Tower.IsUpgradeCompleted(h.ExtraBedUpgrade.Id));
            Assert.AreEqual(bedsBefore + 1, session.Session.Recovery.QuartersBedCapacity.CurrentValue);
            Assert.AreEqual(2, minion.Rank.CurrentValue);
            Assert.AreEqual(0, session.Session.Economy.ActiveSales.Count, "Sale should have completed and been removed.");
            Assert.Greater(session.Economy.Gold.CurrentValue, goldBefore, "Sale payout should have been credited.");
        }

        [Test]
        public void NightfallAppliesTiredToEveryMinionEvenWhileBusy()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            // Advance to just before nightfall (default hour 20), then launch a raid whose ~2h
            // duration spans the nightfall boundary, so the squad member is still Activity.Raiding
            // (busy) exactly when nightfall fires at hour 20.
            session.Scheduler.AdvanceGameTime(19f);

            var busyMinion = session.Minions.Roster.Minions[0];
            var launch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { busyMinion.Id });
            Assert.IsTrue(launch.Success);
            Assert.AreEqual(MinionActivity.Raiding, busyMinion.Activity.CurrentValue);

            session.Scheduler.AdvanceGameTime(3f); // crosses hour 20 (nightfall) while the raid is still running

            Assert.AreEqual(MinionActivity.Raiding, busyMinion.Activity.CurrentValue, "Raid squad is not released until Claim.");

            var minions = session.Minions.Roster.Minions;
            for (int i = 0; i < minions.Count; i++)
                Assert.IsTrue(minions[i].HasCondition(MinionCondition.Tired), $"Minion {i} should be Tired after nightfall.");
        }
    }
}
