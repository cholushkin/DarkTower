import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { RaidReportDraft } from "../model/types";
import { creditCrystals, creditGold } from "./economy";
import { markWaitingIfIdle } from "./recovery";
import { admitPrisoner } from "./dungeon";
import { addTowerXp } from "./tower";

export function discoveredCounts(draft: RaidReportDraft): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const itemId of draft.lootCopies) counts[itemId] = (counts[itemId] ?? 0) + 1;
    return counts;
}

export function selectedSlots(draft: RaidReportDraft): number {
    let slots = 0;
    for (const [itemId, count] of Object.entries(draft.selection)) {
        if (count <= 0) continue;
        const def = CONFIG.items.find(i => i.id === itemId)!;
        slots += Math.ceil(count / def.maxStackSize);
    }
    return slots;
}

export function adjustSelection(draft: RaidReportDraft, itemId: string, delta: number): boolean {
    const total = discoveredCounts(draft)[itemId] ?? 0;
    const current = draft.selection[itemId] ?? 0;
    const next = current + delta;
    if (next < 0 || next > total) return false;

    const def = CONFIG.items.find(i => i.id === itemId)!;
    const slotsBefore = Math.ceil(current / def.maxStackSize);
    const slotsAfter = Math.ceil(next / def.maxStackSize);
    const otherSlots = selectedSlots(draft) - slotsBefore;
    if (otherSlots + slotsAfter > draft.carryLimit) return false;

    draft.selection[itemId] = next;
    return true;
}

export function claimReport(state: GameState, missionId: string, now: number): boolean {
    const draft = state.pendingReports.get(missionId);
    if (!draft) return false;

    if (draft.success) {
        creditGold(state, draft.goldFound);
        creditCrystals(state, draft.crystalsFound);
        for (const [itemId, count] of Object.entries(draft.selection)) {
            if (count > 0) state.addItems(itemId, count);
        }
        const sortedPrisoners = [...draft.prisonersFound].sort((a, b) => b - a);
        for (const rank of sortedPrisoners) admitPrisoner(state, rank);
    }

    for (const minionId of draft.squad) {
        const minion = state.findMinion(minionId);
        if (!minion) continue;
        minion.tired = true;
        minion.injured = (draft.injuryRolls[minionId] ?? 0) > 0;
        minion.activity = { type: "idle" };
        markWaitingIfIdle(state, minion, now);
    }

    if (draft.success && !state.claimedMissions.has(missionId)) {
        state.claimedMissions.add(missionId);
        addTowerXp(state, 5);
        onMissionFirstClaim(state, missionId);
    }

    state.pendingReports.delete(missionId);
    state.log(draft.success
        ? `Claimed raid report for ${missionId}: success.`
        : `Claimed raid report for ${missionId}: failed, no loot.`);
    return true;
}

function onMissionFirstClaim(state: GameState, missionId: string): void {
    const mission = CONFIG.missions.find(m => m.id === missionId)!;
    const tierMissions = CONFIG.missions.filter(m => m.tier === mission.tier);
    const tierFullyClaimed = tierMissions.every(m => state.claimedMissions.has(m.id));
    if (tierFullyClaimed) {
        const nextTier = mission.tier + 1;
        const hasNextTier = CONFIG.worldTiers.some(t => t.tier === nextTier);
        if (hasNextTier) {
            state.unlockedTiers.add(nextTier);
            state.log(`World Tier ${nextTier} unlocked!`);
        }
    }
    const allMissionsClaimed = CONFIG.missions.every(m => state.claimedMissions.has(m.id));
    if (allMissionsClaimed) {
        state.victory = true;
        state.log("Victory! Every mission on the World Map has been claimed.");
    }
}
