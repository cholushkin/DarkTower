using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Tests
{
    public class DungeonTests
    {
        [Test]
        public void AdmitPrisonersRespectsFreeCellsAndDiscardsOverflow()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase(); // 2 dungeon cells
            using var session = GameplaySessionComposer.Create(db, 1);

            var prisoners = new List<CapturedPrisoner>
            {
                new CapturedPrisoner(1), new CapturedPrisoner(2), new CapturedPrisoner(3),
                new CapturedPrisoner(1), new CapturedPrisoner(2)
            };
            session.Dungeon.AdmitPrisoners(prisoners, 0f);

            Assert.AreEqual(2, session.Session.Dungeon.Prisoners.Count, "Capacity is 2; the other 3 are discarded.");
            Assert.AreEqual(0, session.Session.Dungeon.FreeCells);
        }

        [Test]
        public void SortDirectionDecidesWhichPrisonerSurvivesWhenCellsAreScarce()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();

            using (var ascending = GameplaySessionComposer.Create(db, 1))
            {
                ascending.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(2) }, 0f); // occupy 1 of 2 cells
                ascending.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(3), new CapturedPrisoner(1) }, 0f); // 1 free cell left

                Assert.AreEqual(1, CountRank(ascending, 1), "SortHighRankFirst=false (default) admits the lowest rank first.");
                Assert.AreEqual(0, CountRank(ascending, 3));
            }

            GameplayTestFixtures.SetField(h.DungeonRules, "sortHighRankFirst", true);

            using (var descending = GameplaySessionComposer.Create(db, 1))
            {
                descending.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(2) }, 0f);
                descending.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(3), new CapturedPrisoner(1) }, 0f);

                Assert.AreEqual(1, CountRank(descending, 3), "SortHighRankFirst=true admits the highest rank first.");
                Assert.AreEqual(0, CountRank(descending, 1));
            }
        }

        private static int CountRank(GameplaySession session, int rank)
        {
            int count = 0;
            var prisoners = session.Session.Dungeon.Prisoners;
            for (int i = 0; i < prisoners.Count; i++)
                if (prisoners[i].Rank == rank) count++;
            return count;
        }

        [Test]
        public void ProcessingResolvesEveryPrisonerExactlyOnceAndRecruitsAtTheSameRank()
        {
            var (db, _) = GameplayTestFixtures.BuildMinimalDatabase(); // outcomeDistribution is 100% recruitment, 0% injury
            using var session = GameplaySessionComposer.Create(db, 1);

            int rosterBefore = session.Minions.Roster.Count;
            session.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(1), new CapturedPrisoner(2) }, 0f);
            Assert.AreEqual(2, session.Session.Dungeon.Prisoners.Count);

            session.Scheduler.AdvanceGameTime(2f); // base processing duration is 1h

            Assert.AreEqual(0, session.Session.Dungeon.Prisoners.Count, "Every prisoner should have been resolved and removed.");
            Assert.AreEqual(rosterBefore + 2, session.Minions.Roster.Count);

            var minions = session.Minions.Roster.Minions;
            bool foundRank1Recruit = false, foundRank2Recruit = false;
            for (int i = 0; i < minions.Count; i++)
            {
                if (minions[i].Rank.CurrentValue == 1 && minions[i].HasCondition(MinionCondition.Tired)) foundRank1Recruit = true;
                if (minions[i].Rank.CurrentValue == 2 && minions[i].HasCondition(MinionCondition.Tired)) foundRank2Recruit = true;
                Assert.IsFalse(minions[i].HasCondition(MinionCondition.Injured), "recruitedInjuredProbability is 0 in this fixture.");
            }
            Assert.IsTrue(foundRank1Recruit);
            Assert.IsTrue(foundRank2Recruit);
        }
    }
}
