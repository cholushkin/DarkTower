using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class WorldProgressionTests
    {
        [Test]
        public void ClaimingTheOnlyMissionTriggersVictory()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minion.Id });
            session.Scheduler.AdvanceGameTime(3f);
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);

            Assert.IsTrue(claim.Success);
            Assert.IsTrue(session.Session.IsVictory.CurrentValue);
            Assert.IsTrue(session.Session.IsTerminal);
        }

        [Test]
        public void MissionObeysWeeklyCooldownAndIsRelaunchableNextWeek()
        {
            // Two missions, so claiming the first doesn't trigger Victory (which would otherwise
            // block every further TryStartRaid via the IsTerminal guard and mask what this test
            // actually wants to check: the weekly-cooldown transition).
            var (db, h, _) = GameplayTestFixtures.BuildTwoMissionDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minions = session.Minions.Roster.Minions;

            var launch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minions[0].Id });
            session.Scheduler.AdvanceGameTime(3f);
            session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);

            var sameWeekRelaunch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minions[1].Id });
            Assert.IsFalse(sameWeekRelaunch.Success);
            Assert.AreEqual("mission_not_launchable", sameWeekRelaunch.Reason);

            // Advance well into the next in-game week.
            session.Scheduler.AdvanceGameTime(7f * 24f);

            var nextWeekRelaunch = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId> { minions[1].Id });
            Assert.IsTrue(nextWeekRelaunch.Success, "A mission may be replayed in a later week, even after a prior successful completion.");
        }
    }
}
