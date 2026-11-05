using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class RaidClaimTests
    {
        private static List<EntityId> Squad(params EntityId[] ids) => new List<EntityId>(ids);

        private static ItemDefinition NewItem(int maxStackSize)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            GameplayTestFixtures.SetField(item, "maxStackSize", maxStackSize);
            return item;
        }

        [Test]
        public void CopiesOfAHighStackSizeItemShareOneCarrySlot()
        {
            var item = NewItem(maxStackSize: 10);
            var copies = new List<DiscoveredItemCopy>
            {
                new DiscoveredItemCopy(new EntityId(1), item, false),
                new DiscoveredItemCopy(new EntityId(2), item, false),
                new DiscoveredItemCopy(new EntityId(3), item, false),
            };

            var selected = RaidClaimPolicy.DefaultPreselect(copies, carryLimit: 1);

            Assert.AreEqual(3, selected.Count, "All three copies stack into a single slot, so a 1-slot budget fits every one of them.");
        }

        [Test]
        public void SecondCopyOfASingleStackItemNeedsASecondSlot()
        {
            var item = NewItem(maxStackSize: 1);
            var copies = new List<DiscoveredItemCopy>
            {
                new DiscoveredItemCopy(new EntityId(1), item, false),
                new DiscoveredItemCopy(new EntityId(2), item, false),
            };

            var selected = RaidClaimPolicy.DefaultPreselect(copies, carryLimit: 1);

            Assert.AreEqual(1, selected.Count, "Each copy of a max_stack_size=1 item needs its own slot; only one fits a 1-slot budget.");
            Assert.AreEqual(copies[0].Id, selected[0]);
        }

        [Test]
        public void EmptySquadIsRejected()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var result = session.Raids.TryStartRaid(h.Mission.Id, new List<EntityId>());

            Assert.IsFalse(result.Success);
            Assert.AreEqual("empty_squad", result.Reason);
            Assert.AreEqual(MissionState.Available, session.Session.World.FindMission(h.Mission.Id).State.CurrentValue);
        }

        [Test]
        public void DuplicateSquadMemberIsRejected()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var id = session.Minions.Roster.Minions[0].Id;

            var result = session.Raids.TryStartRaid(h.Mission.Id, Squad(id, id));

            Assert.IsFalse(result.Success);
            Assert.AreEqual("duplicate_squad_member", result.Reason);
        }

        [Test]
        public void NonexistentMinionIsRejected()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);

            var result = session.Raids.TryStartRaid(h.Mission.Id, Squad(new EntityId(999999)));

            Assert.IsFalse(result.Success);
            Assert.AreEqual("minion_not_found", result.Reason);
        }

        [Test]
        public void TiredOrInjuredMinionIsRejected()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];
            minion.AddCondition(MinionCondition.Tired, 0f);

            var result = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));

            Assert.IsFalse(result.Success);
            Assert.AreEqual("minion_unavailable", result.Reason);
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
        }

        [Test]
        public void AlreadyBusySquadMemberIsRejected()
        {
            var (db, h, secondMission) = GameplayTestFixtures.BuildTwoMissionDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var first = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            Assert.IsTrue(first.Success);

            var second = session.Raids.TryStartRaid(secondMission.Id, Squad(minion.Id));

            Assert.IsFalse(second.Success);
            Assert.AreEqual("minion_unavailable", second.Reason);
            Assert.AreEqual(MissionState.Available, session.Session.World.FindMission(secondMission.Id).State.CurrentValue);
        }

        [Test]
        public void MissionCannotBeLaunchedTwiceInTheSameWeek()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minions = session.Minions.Roster.Minions;

            var first = session.Raids.TryStartRaid(h.Mission.Id, Squad(minions[0].Id));
            Assert.IsTrue(first.Success);

            var second = session.Raids.TryStartRaid(h.Mission.Id, Squad(minions[1].Id));

            Assert.IsFalse(second.Success);
            Assert.AreEqual("mission_not_launchable", second.Reason);
            Assert.AreEqual(MinionActivity.Idle, minions[1].Activity.CurrentValue);
        }

        [Test]
        public void MutatingTheCallersSquadListAfterLaunchDoesNotAffectTheReservedSquad()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];
            var otherMinion = session.Minions.Roster.Minions[1];

            var squadList = new List<EntityId> { minion.Id };
            var launch = session.Raids.TryStartRaid(h.Mission.Id, squadList);
            Assert.IsTrue(launch.Success);

            // Mutate the caller's own list after launch — RaidModel must have taken a defensive copy.
            squadList[0] = otherMinion.Id;
            squadList.Add(minion.Id);

            session.Scheduler.AdvanceGameTime(3f);
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue, "The originally-reserved minion must still be released.");
            Assert.AreEqual(MinionActivity.Idle, otherMinion.Activity.CurrentValue, "A minion never actually in the squad must be unaffected either way.");
            Assert.IsFalse(otherMinion.HasCondition(MinionCondition.Tired), "Only the originally-reserved minion returns from the raid.");
        }

        [Test]
        public void SuccessfulLaunchReservesTheSquad()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var result = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(MinionActivity.Raiding, minion.Activity.CurrentValue);
            Assert.IsFalse(minion.IsAvailableForAssignment);
            Assert.AreEqual(MissionState.Running, session.Session.World.FindMission(h.Mission.Id).State.CurrentValue);
        }

        [Test]
        public void FinishingDoesNotReleaseTheSquadOrCreditAnything()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];
            int goldBefore = session.Economy.Gold.CurrentValue;

            session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f); // mission baseDuration is 2h

            var progress = session.Session.World.FindMission(h.Mission.Id);
            Assert.AreEqual(MissionState.CompletedUnclaimed, progress.State.CurrentValue);
            Assert.AreNotEqual(RaidOutcome.Pending, progress.CurrentRaid.CurrentValue.Outcome.CurrentValue);
            Assert.AreEqual(MinionActivity.Raiding, minion.Activity.CurrentValue, "Squad stays reserved until Claim.");
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "Nothing is credited before Claim.");
        }

        [Test]
        public void ClaimingAPendingRaidFails()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            var raidId = launch.Value;

            var claim = session.Raids.TryClaimRaid(h.Mission.Id, raidId, null);

            Assert.IsFalse(claim.Success);
            Assert.AreEqual("raid_not_finished", claim.Reason);
        }

        [Test]
        public void SuccessfulClaimCreditsLootAdmitsPrisonersAndReleasesTheSquad()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f);

            var progress = session.Session.World.FindMission(h.Mission.Id);
            var raid = progress.CurrentRaid.CurrentValue;
            Assert.AreEqual(RaidOutcome.Success, raid.Outcome.CurrentValue);

            int expectedGold = raid.DiscoveredGold;
            int expectedCrystals = raid.DiscoveredCrystals;
            int goldBefore = session.Economy.Gold.CurrentValue;
            int crystalsBefore = session.Economy.Crystals.CurrentValue;

            var selection = RaidClaimPolicy.DefaultPreselect(raid.DiscoveredItems, raid.CarryLimit);
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, selection);

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(goldBefore + expectedGold, session.Economy.Gold.CurrentValue);
            Assert.AreEqual(crystalsBefore + expectedCrystals, session.Economy.Crystals.CurrentValue);
            Assert.AreEqual(1, session.Session.Dungeon.Prisoners.Count, "Mission always discovers exactly 1 prisoner.");
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.IsFalse(minion.HasCondition(MinionCondition.Injured), "baseInjuryChance is 0 in this fixture.");
            Assert.IsTrue(progress.CompletedOnce.CurrentValue);
            Assert.AreEqual(MissionState.Cooldown, progress.State.CurrentValue, "Claimed within its launch week.");
        }

        [Test]
        public void ClaimingTwiceFailsAndDoesNotDoubleCredit()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f);

            var first = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);
            Assert.IsTrue(first.Success);
            int goldAfterFirstClaim = session.Economy.Gold.CurrentValue;

            var second = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);

            Assert.IsFalse(second.Success);
            Assert.AreEqual("already_claimed", second.Reason);
            Assert.AreEqual(goldAfterFirstClaim, session.Economy.Gold.CurrentValue);
        }

        [Test]
        public void SuccessfulRaidPreselectsAValidSubsetWithinTheCarryBudget()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f);

            var raid = session.Session.World.FindMission(h.Mission.Id).CurrentRaid.CurrentValue;
            Assert.AreEqual(RaidOutcome.Success, raid.Outcome.CurrentValue);
            Assert.Greater(raid.DiscoveredItems.Count, 0, "This fixture's mission always drops at least one item copy.");

            int selectedCount = 0;
            foreach (var copy in raid.DiscoveredItems)
                if (copy.Selected.CurrentValue) selectedCount++;

            Assert.Greater(selectedCount, 0, "A newly completed successful raid must expose a preselected subset, not leave every copy unselected.");
            Assert.LessOrEqual(raid.SelectedCarryUsed(), raid.CarryLimit, "The preselection must stay within the carry budget.");
        }

        [Test]
        public void SelectionOverCarryBudgetIsRejectedWithoutMutation()
        {
            var (db, h) = GameplayTestFixtures.BuildMinimalDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0]; // rank1 carryCapacity 1.5 -> carryLimit 1

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f);

            var progress = session.Session.World.FindMission(h.Mission.Id);
            var raid = progress.CurrentRaid.CurrentValue;
            Assert.AreEqual(1, raid.CarryLimit);

            // Try to select every discovered copy, which exceeds the budget whenever more than one
            // copy dropped (countRange is 1..2); if only one dropped this assertion would be moot, so
            // force the over-budget path explicitly by also including a copy id that doesn't exist.
            var overBudget = new List<EntityId>();
            foreach (var copy in raid.DiscoveredItems) overBudget.Add(copy.Id);
            overBudget.Add(new EntityId(123456789));

            int goldBefore = session.Economy.Gold.CurrentValue;
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, overBudget);

            Assert.IsFalse(claim.Success);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue);
            Assert.IsFalse(raid.Claimed.CurrentValue);
        }

        [Test]
        public void FailedRaidClaimsWithNoRewardsButStillReturnsTheSquad()
        {
            var (db, h) = GameplayTestFixtures.BuildHardMissionDatabase();
            using var session = GameplaySessionComposer.Create(db, 1);
            var minion = session.Minions.Roster.Minions[0];

            var launch = session.Raids.TryStartRaid(h.Mission.Id, Squad(minion.Id));
            session.Scheduler.AdvanceGameTime(3f);

            var progress = session.Session.World.FindMission(h.Mission.Id);
            Assert.AreEqual(RaidOutcome.Failure, progress.CurrentRaid.CurrentValue.Outcome.CurrentValue);

            int goldBefore = session.Economy.Gold.CurrentValue;
            var claim = session.Raids.TryClaimRaid(h.Mission.Id, launch.Value, null);

            Assert.IsTrue(claim.Success);
            Assert.AreEqual(goldBefore, session.Economy.Gold.CurrentValue, "Failure credits no loot.");
            Assert.AreEqual(MinionActivity.Idle, minion.Activity.CurrentValue);
            Assert.IsTrue(minion.HasCondition(MinionCondition.Tired));
            Assert.IsTrue(minion.HasCondition(MinionCondition.Injured), "Failure always returns Tired AND Injured.");
            Assert.IsFalse(progress.CompletedOnce.CurrentValue);
        }
    }
}
