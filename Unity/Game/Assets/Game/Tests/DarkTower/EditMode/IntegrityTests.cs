using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class IntegrityTests
    {
        [Test]
        public void DisposingTheSessionCascadesToGameSessionModel()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            GameSessionModel model;

            using (var session = GameplaySessionComposer.Create(db, 1))
            {
                model = session.Session;
                Assert.IsFalse(model.IsDisposed);
            }

            Assert.IsTrue(model.IsDisposed);
        }

        [Test]
        public void SecondDisposeOfTheSameModelIsANoOp()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var model = session.Session;

            model.Dispose();
            Assert.IsTrue(model.IsDisposed);
            Assert.DoesNotThrow(() => model.Dispose());
        }

        [Test]
        public void IndependentSessionsDoNotShareMutableState()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase();
            using var sessionA = GameplaySessionComposer.Create(db, 1);
            using var sessionB = GameplaySessionComposer.Create(db, 2);

            int goldBBefore = sessionB.Economy.Gold.CurrentValue;
            sessionA.Economy.CreditGold(500);

            Assert.AreEqual(goldBBefore, sessionB.Economy.Gold.CurrentValue);
            Assert.AreNotEqual(sessionA.Economy.Gold.CurrentValue, sessionB.Economy.Gold.CurrentValue);
        }

        [Test]
        public void TerminalStateRejectsFurtherGameplayOperationsWithoutMutation()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Scheduler.AdvanceGameTime(24f * 7f + 1f); // unpaid payday -> bankruptcy
            Assert.IsTrue(session.Session.IsTerminal);

            int goldBefore = session.Economy.Gold.CurrentValue;
            var minion = session.Minions.Roster.Minions[0];

            var raid = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minion.Id });
            Assert.IsFalse(raid.Success);
            Assert.AreEqual("game_over", raid.Reason);
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);

            var upgrade = session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id);
            Assert.IsFalse(upgrade.Success);
            Assert.AreEqual("game_over", upgrade.Reason);

            var research = session.Research.TryStartResearch(h.Research1.Id);
            Assert.IsFalse(research.Success);
            Assert.AreEqual("game_over", research.Reason);

            var training = session.Training.TryStartTraining(minion.Id);
            Assert.IsFalse(training.Success);
            Assert.AreEqual("game_over", training.Reason);

            var pay = session.Payroll.TryPayAll();
            Assert.IsFalse(pay.Success);
            Assert.AreEqual("game_over", pay.Reason);

            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "No operation above should have spent anything.");
        }
    }
}
