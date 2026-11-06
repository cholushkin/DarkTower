import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { DungeonProcess, Prisoner } from "../model/types";
import { markWaitingIfIdle } from "./recovery";

function occupiedCells(state: GameState): number {
    return state.prisoners.filter(p => p.processId !== null).length;
}

export function freeCells(state: GameState): number {
    return state.dungeonCellCount() - occupiedCells(state);
}

export function admitPrisoner(state: GameState, rank: number): boolean {
    if (freeCells(state) <= 0) return false;
    const prisoner: Prisoner = { id: state.genId("prisoner"), rank, processId: null };
    state.prisoners.push(prisoner);
    return true;
}

/** Starts processing for any waiting prisoner once a cell is free (§4.6: "starts automatically"). */
export function assignCells(state: GameState, now: number): void {
    for (const prisoner of state.prisoners) {
        if (prisoner.processId !== null) continue;
        if (freeCells(state) <= 0) break;
        const duration = CONFIG.constants.dungeonBaseDurationHours *
            (1 + 0.15 * (prisoner.rank - 1)) * state.tortureEfficiencyMultiplier();
        const process: DungeonProcess = {
            id: state.genId("dungeon"), type: "dungeon",
            startHour: now, endHour: now + duration, prisonerId: prisoner.id, rank: prisoner.rank,
        };
        state.processes.push(process);
        prisoner.processId = process.id;
    }
}

function rollOutcome(state: GameState, rank: number): "recruit" | "interrogate" | "death" {
    const base = CONFIG.constants.dungeonOutcomeBase;
    const bonus = state.recruitmentEfficiencyBonus();
    const recruit = clamp(base.recruit - 0.08 * (rank - 1) + bonus, 0.05, 0.95);
    const death = clamp(base.death + 0.05 * (rank - 1) - bonus * 0.3, 0.05, 0.9);
    const interrogate = Math.max(0, 1 - recruit - death);
    const total = recruit + death + interrogate;
    const roll = state.rng() * total;
    if (roll < recruit) return "recruit";
    if (roll < recruit + interrogate) return "interrogate";
    return "death";
}

function clamp(v: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, v));
}

export function completeDungeonProcess(state: GameState, process: DungeonProcess, now: number): void {
    const prisoner = state.prisoners.find(p => p.id === process.prisonerId);
    state.prisoners = state.prisoners.filter(p => p.id !== process.prisonerId);
    if (!prisoner) return;

    const outcome = rollOutcome(state, prisoner.rank);
    if (outcome === "recruit") {
        const injured = state.rng() < CONFIG.constants.recruitedInjuredChance;
        const minion = {
            id: state.genId("minion"), rank: prisoner.rank,
            tired: true, injured, activity: { type: "idle" as const }, waitingSince: null,
        };
        state.minions.push(minion);
        markWaitingIfIdle(state, minion, now);
        state.log(`A Rank ${prisoner.rank} prisoner was recruited into the army.`);
    } else if (outcome === "interrogate") {
        state.log(`A Rank ${prisoner.rank} prisoner revealed secrets under interrogation.`);
    } else {
        state.log(`A Rank ${prisoner.rank} prisoner died during processing.`);
    }
}
