using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void InterrogationEventRespectsTierGatingAndRefreshesOtherFlagGatedMissions()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();

            // Named directly in the event's UnlocksMissions, but placed in tier 1 — not yet reached —
            // so the explicit unlock must not bypass tier gating.
            var farMission = BuildBareMission("far_mission");
            // Reachable (tier 0) but gated on the secret flag the event sets, and NOT named in the
            // event's UnlocksMissions — only a RefreshAccessibility pass can open it.
            var flagGatedMission = BuildBareMission("flag_gated_mission");
            GameplayTestFixtures.SetField(flagGatedMission, "unlockConditions", new[]
            {
                GameplayTestFixtures.NewStruct<UnlockCondition>(("type", UnlockConditionType.SecretRevealed), ("targetId", "found_secret"))
            });

            GameplayTestFixtures.SetField(db, "missions", new[] { h.Mission, farMission, flagGatedMission });

            var tier0 = GameplayTestFixtures.NewStruct<WorldTierEntry>(("tierId", "tier0"), ("missions", new[]
            {
                GameplayTestFixtures.NewStruct<WorldMissionPlacement>(("mission", h.Mission), ("position", Vector2.zero)),
                GameplayTestFixtures.NewStruct<WorldMissionPlacement>(("mission", flagGatedMission), ("position", Vector2.one)),
            }));
            var tier1 = GameplayTestFixtures.NewStruct<WorldTierEntry>(("tierId", "tier1"), ("missions", new[]
            {
                GameplayTestFixtures.NewStruct<WorldMissionPlacement>(("mission", farMission), ("position", Vector2.zero)),
            }));
            GameplayTestFixtures.SetField(db.WorldMap, "tiers", new[] { tier0, tier1 });

            var evt = ScriptableObject.CreateInstance<EventDefinition>();
            GameplayTestFixtures.SetField(evt, "eventId", "found_catacombs");
            GameplayTestFixtures.SetField(evt, "setsWorldFlag", "found_secret");
            GameplayTestFixtures.SetField(evt, "unlocksMissions", new[] { farMission });

            GameplayTestFixtures.SetField(h.DungeonRules, "interrogationEvents", new[] { evt });
            GameplayTestFixtures.SetField(h.DungeonRules, "outcomeDistribution", GameplayTestFixtures.NewStruct<PrisonerOutcomeDistribution>(
                ("recruitmentWeight", 0f), ("interrogationWeight", 1f), ("deathWeight", 0f)));

            db.InvalidateCaches();
            using var session = GameplaySessionComposer.Create(db, 1);

            session.Dungeon.AdmitPrisoners(new List<CapturedPrisoner> { new CapturedPrisoner(1) }, 0f);
            session.Scheduler.AdvanceGameTime(2f); // base processing duration is 1h -> Interrogation resolves

            var far = session.Session.World.FindMission(farMission.Id);
            var flagGated = session.Session.World.FindMission(flagGatedMission.Id);

            Assert.IsTrue(session.Session.World.HasFlag("found_secret"));
            Assert.AreEqual(MissionState.Locked, far.State.CurrentValue,
                "Tier 1 is not reached yet; an explicit event unlock must not bypass tier gating.");
            Assert.AreEqual(MissionState.Available, flagGated.State.CurrentValue,
                "Setting the flag must refresh OTHER missions whose own unlock conditions it satisfies, not only the ones the event names.");
        }

        private static MissionDefinition BuildBareMission(string missionId)
        {
            var def = ScriptableObject.CreateInstance<MissionDefinition>();
            GameplayTestFixtures.SetField(def, "missionId", missionId);
            GameplayTestFixtures.SetField(def, "displayName", missionId);
            GameplayTestFixtures.SetField(def, "requiredPower", 1f);
            GameplayTestFixtures.SetField(def, "baseDuration", new GameDuration(2f, DurationUnit.Hours));
            GameplayTestFixtures.SetField(def, "baseInjuryChance", new Probability(0f));
            GameplayTestFixtures.SetField(def, "goldRange", new IntRange(0, 0));
            GameplayTestFixtures.SetField(def, "crystalRange", new IntRange(0, 0));
            GameplayTestFixtures.SetField(def, "itemRewards", System.Array.Empty<ItemRewardEntry>());
            GameplayTestFixtures.SetField(def, "prisonerCountRange", new IntRange(0, 0));
            GameplayTestFixtures.SetField(def, "prisonerRankDistribution", System.Array.Empty<RankWeight>());
            GameplayTestFixtures.SetField(def, "squadConstraints", default(SquadConstraints));
            return def;
        }
    }
}
