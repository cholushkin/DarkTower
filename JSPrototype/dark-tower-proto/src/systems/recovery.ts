import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { Minion, RestProcess, TreatmentProcess } from "../model/types";

export function markWaitingIfIdle(state: GameState, minion: Minion, now: number): void {
    if (minion.activity.type === "idle" && (minion.tired || minion.injured) && minion.waitingSince === null) {
        minion.waitingSince = now;
    }
}

function occupiedTreatmentBeds(state: GameState): number {
    return state.processes.filter(p => p.type === "treatment").length;
}

function occupiedBeds(state: GameState): number {
    return state.processes.filter(p => p.type === "rest").length;
}

/** Auto-assigns free Infirmary/Quarters beds; Injured takes priority over Tired (§4.3). */
export function assignBeds(state: GameState, now: number): void {
    let freeTreatment = state.treatmentBedCount() - occupiedTreatmentBeds(state);
    const injuredWaiting = state.minions
        .filter(m => m.activity.type === "idle" && m.injured)
        .sort((a, b) => (a.waitingSince ?? now) - (b.waitingSince ?? now));
    for (const minion of injuredWaiting) {
        if (freeTreatment <= 0) break;
        const duration = CONFIG.constants.treatmentDurationHours * state.treatmentEfficiencyMultiplier();
        const process: TreatmentProcess = {
            id: state.genId("treat"), type: "treatment",
            startHour: now, endHour: now + duration, minionId: minion.id,
        };
        state.processes.push(process);
        minion.activity = { type: "treatment", processId: process.id };
        minion.waitingSince = null;
        freeTreatment--;
    }

    let freeBeds = state.bedCount() - occupiedBeds(state);
    const tiredWaiting = state.minions
        .filter(m => m.activity.type === "idle" && m.tired && !m.injured)
        .sort((a, b) => (a.waitingSince ?? now) - (b.waitingSince ?? now));
    for (const minion of tiredWaiting) {
        if (freeBeds <= 0) break;
        const duration = CONFIG.constants.restDurationHours;
        const process: RestProcess = {
            id: state.genId("rest"), type: "rest",
            startHour: now, endHour: now + duration, minionId: minion.id,
        };
        state.processes.push(process);
        minion.activity = { type: "resting", processId: process.id };
        minion.waitingSince = null;
        freeBeds--;
    }
}

export function completeRest(state: GameState, process: RestProcess, now: number): void {
    const minion = state.findMinion(process.minionId);
    if (!minion) return;
    minion.tired = false;
    minion.activity = { type: "idle" };
    markWaitingIfIdle(state, minion, now);
}

export function completeTreatment(state: GameState, process: TreatmentProcess, now: number): void {
    const minion = state.findMinion(process.minionId);
    if (!minion) return;
    minion.tired = false;
    minion.injured = false;
    minion.activity = { type: "idle" };
    minion.waitingSince = null;
    void now;
}
