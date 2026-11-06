import { CONFIG } from "../data/config";
import type { MissionDef } from "../data/types";
import type { GameState } from "../model/GameState";
import type { RaidProcess, RaidReportDraft } from "../model/types";

export function missionDef(missionId: string): MissionDef {
    return CONFIG.missions.find(m => m.id === missionId)!;
}

export function unitPower(state: GameState, minionId: string): number {
    const minion = state.findMinion(minionId)!;
    const attack = state.effectiveAttack(minion.rank);
    const defense = state.effectiveDefense(minion.rank);
    return (0.65 * attack + 0.35 * defense) * (1 + 0.1 * (minion.rank - 1));
}

export function squadPower(state: GameState, squad: string[]): number {
    return squad.reduce((sum, id) => sum + unitPower(state, id), 0);
}

export interface RaidPreview {
    totalPower: number;
    powerRatio: number;
    failureChance: number;
    durationHours: number;
    avgInjuryChance: number;
}

export function previewRaid(state: GameState, missionId: string, squad: string[]): RaidPreview {
    const mission = missionDef(missionId);
    const totalPower = squadPower(state, squad);
    const powerRatio = mission.requiredPower > 0 ? totalPower / mission.requiredPower : 1;
    const failureChance = clamp(2.0 * (1 - powerRatio), 0, 1);
    const avgRank = squad.length ? squad.reduce((s, id) => s + state.findMinion(id)!.rank, 0) / squad.length : 1;
    const durationHours = mission.baseDurationHours *
        Math.max(0.8, 1 - 0.04 * (squad.length - 1) - 0.01 * (avgRank - 1));
    const avgInjuryChance = squad.length
        ? squad.reduce((s, id) => s + injuryChanceFor(state, mission, id, powerRatio), 0) / squad.length
        : mission.baseInjuryChance;
    return { totalPower, powerRatio, failureChance, durationHours, avgInjuryChance };
}

function injuryChanceFor(state: GameState, mission: MissionDef, minionId: string, powerRatio: number): number {
    const minion = state.findMinion(minionId)!;
    const defense = state.effectiveDefense(minion.rank);
    return clamp(
        mission.baseInjuryChance
        + 0.2 * Math.max(0, 1 - powerRatio)
        - 0.02 * (minion.rank - 1)
        - 0.005 * Math.max(0, defense - 5),
        0.01, 0.9);
}

function clamp(v: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, v));
}

export function canLaunchRaid(state: GameState, missionId: string, squad: string[]): boolean {
    if (squad.length === 0) return false;
    if (state.missionState(missionId) !== "AVAILABLE") return false;
    return squad.every(id => {
        const m = state.findMinion(id);
        return m && m.activity.type === "idle" && !m.tired && !m.injured;
    });
}

export function launchRaid(state: GameState, missionId: string, squad: string[], now: number): boolean {
    if (!canLaunchRaid(state, missionId, squad)) return false;
    const preview = previewRaid(state, missionId, squad);
    const process: RaidProcess = {
        id: state.genId("raid"), type: "raid",
        startHour: now, endHour: now + preview.durationHours, missionId, squad: [...squad],
    };
    state.processes.push(process);
    state.missionRunningProcess.set(missionId, process.id);
    state.missionLastLaunchWeek.set(missionId, state.week);
    for (const id of squad) {
        const minion = state.findMinion(id)!;
        minion.activity = { type: "raid", missionId, processId: process.id };
    }
    state.log(`Raid launched on ${missionDef(missionId).name} with ${squad.length} minion(s).`);
    return true;
}

export function resolveRaid(state: GameState, process: RaidProcess): RaidReportDraft {
    const mission = missionDef(process.missionId);
    const preview = previewRaid(state, process.missionId, process.squad);
    const success = state.rng() >= preview.failureChance;

    const injuryRolls: Record<string, number> = {};
    for (const id of process.squad) {
        injuryRolls[id] = success
            ? (state.rng() < injuryChanceFor(state, mission, id, preview.powerRatio) ? 1 : 0)
            : 1;
    }

    let goldFound = 0;
    let crystalsFound = 0;
    const lootCopies: string[] = [];
    const prisonersFound: number[] = [];

    if (success) {
        goldFound = randRange(state, mission.rewardGoldRange);
        crystalsFound = randRange(state, mission.rewardCrystalsRange);
        for (const loot of mission.loot) {
            if (state.rng() >= loot.chance) continue;
            const count = randRange(state, [loot.min, loot.max]);
            for (let i = 0; i < count; i++) lootCopies.push(loot.itemId);
        }
        if (state.rng() < mission.prisonerChance) {
            const count = randRange(state, mission.prisonerCountRange);
            for (let i = 0; i < count; i++) {
                prisonersFound.push(randRange(state, mission.prisonerRankRange));
            }
        }
    }

    const carryLimit = Math.floor(process.squad.reduce((sum, id) => {
        const minion = state.findMinion(id)!;
        return sum + CONFIG.ranks[minion.rank - 1].carryCapacity;
    }, 0));

    const selection = preselectWithinBudget(lootCopies, carryLimit);

    return {
        missionId: process.missionId, success, squad: [...process.squad],
        goldFound, crystalsFound, lootCopies, prisonersFound, carryLimit,
        selection, injuryRolls,
    };
}

function randRange(state: GameState, [min, max]: [number, number]): number {
    return Math.floor(state.rng() * (max - min + 1)) + min;
}

function preselectWithinBudget(lootCopies: string[], carryLimit: number): Record<string, number> {
    const selection: Record<string, number> = {};
    let slotsUsed = 0;
    const perItemCount: Record<string, number> = {};
    for (const itemId of lootCopies) perItemCount[itemId] = 0;

    for (const itemId of lootCopies) {
        const def = CONFIG.items.find(i => i.id === itemId)!;
        const before = selection[itemId] ?? 0;
        const slotsBefore = Math.ceil(before / def.maxStackSize);
        const slotsAfter = Math.ceil((before + 1) / def.maxStackSize);
        const extraSlots = slotsAfter - slotsBefore;
        if (slotsUsed + extraSlots > carryLimit) continue;
        selection[itemId] = before + 1;
        slotsUsed += extraSlots;
    }
    return selection;
}
