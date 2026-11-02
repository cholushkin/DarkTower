using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class EndToEndScenarioTests
    {
        [Test]
        public void FullLoop_Raid_Claim_Recovery_Upgrade_Research_Victory()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            // 1. Raid launch. Start the upgrade/research BEFORE claiming — claiming this fixture's
            //    only mission drives Victory, a terminal state, after which TryPurchaseUpgrade/
            //    TryStartResearch would correctly be rejected (see IntegrityTests), so the order here
            //    matters.
            var launch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minion.Id });
            Assert.IsTrue(launch.Success);

            var upgrade = session.TowerUpgrades.TryPurchaseUpgrade(h.ExtraBedUpgrade.Id);
            var research = session.Research.TryStartResearch(h.Research1.Id);
            Assert.IsTrue(upgrade.Success);
            Assert.IsTrue(research.Success);

            int bedsBefore = session.Session.Recovery.QuartersBedCapacity.CurrentValue;
            float attackBefore = session.Minions.GetEffectiveStats(minion).Attack;

            // 2. Advance past the raid (2h), the upgrade and the research (0.1h each) in one jump.
            session.Scheduler.AdvanceGameTime(3f);

            var progress = session.Session.World.FindMission(h.Mission.Id);
            Assert.AreEqual(MissionState.CompletedUnclaimed, progress.State.CurrentValue);
            Assert.AreEqual(bedsBefore + 1, session.Session.Recovery.QuartersBedCapacity.CurrentValue);
            Assert.AreEqual(attackBefore + 2f, session.Minions.GetEffectiveStats(minion).Attack);

            // 3. Claim and inspect returning conditions. With only one mission in the world, this
            //    Claim also drives Victory.
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);
            Assert.IsTrue(claim.Success);
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.IsTrue(session.Session.IsVictory.CurrentValue);
            Assert.IsTrue(session.Session.IsTerminal);

            // 4. Recovery keeps running even in a terminal state (only player-initiated operations
            //    are gated by IsTerminal) — the Tired minion still auto-assigns to a Quarters bed and
            //    clears on completion (rest duration defaults to 6h).
            session.Scheduler.AdvanceGameTime(7f);
            Assert.IsFalse(minion.HasCondition(MinionCondition.Tired));
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
        }

        [Test]
        public void FailedRaidThenUnpaidPayroll_EndsInBankruptcy()
        {
            var (db, h) = GameplayTestFixtures.BuildHardMissionDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minion.Id });
            Assert.IsTrue(launch.Success);
            session.Scheduler.AdvanceGameTime(3f);

            var progress = session.Session.World.FindMission(h.Mission.Id);
            Assert.AreEqual(RaidOutcome.Failure, progress.CurrentRaid.CurrentValue.Outcome.CurrentValue);

            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);
            Assert.IsTrue(claim.Success);
            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.IsTrue(minion.HasCondition(MinionCondition.Injured));
            Assert.IsFalse(session.Session.IsTerminal, "A failed raid alone does not end the game.");

            // Never pay payroll; advance past the first payday boundary.
            session.Scheduler.AdvanceGameTime(24f * 7f);

            Assert.IsTrue(session.Session.IsBankrupt.CurrentValue);
            Assert.IsTrue(session.Session.IsTerminal);
        }
    }
}
