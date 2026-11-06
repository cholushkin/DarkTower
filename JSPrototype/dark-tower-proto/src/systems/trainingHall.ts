import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { TrainingProcess } from "../model/types";
import { canAfford, spend } from "./economy";
import { markWaitingIfIdle } from "./recovery";

function occupiedStations(state: GameState): number {
    return state.processes.filter(p => p.type === "training").length;
}

export function availableStations(state: GameState): number {
    return state.trainingStationCount() - occupiedStations(state);
}

export function transitionFor(rank: number) {
    return CONFIG.trainingTransitions.find(t => t.fromRank === rank);
}

export function canTrain(state: GameState, minionId: string): boolean {
    const minion = state.findMinion(minionId);
    if (!minion || minion.activity.type !== "idle" || minion.tired || minion.injured) return false;
    const transition = transitionFor(minion.rank);
    if (!transition) return false;
    if (!state.isTrainingTransitionUnlocked(minion.rank)) return false;
    if (availableStations(state) <= 0) return false;
    return canAfford(state, transition.costGold);
}

export function startTraining(state: GameState, minionId: string, now: number): boolean {
    if (!canTrain(state, minionId)) return false;
    const minion = state.findMinion(minionId)!;
    const transition = transitionFor(minion.rank)!;
    spend(state, transition.costGold);
    const process: TrainingProcess = {
        id: state.genId("train"), type: "training",
        startHour: now, endHour: now + transition.durationHours,
        minionId, fromRank: transition.fromRank, toRank: transition.toRank,
    };
    state.processes.push(process);
    minion.activity = { type: "training", processId: process.id };
    state.log(`Training started: Rank ${transition.fromRank} → ${transition.toRank}.`);
    return true;
}

export function completeTraining(state: GameState, process: TrainingProcess, now: number): void {
    const minion = state.findMinion(process.minionId);
    if (!minion) return;
    minion.rank = process.toRank;
    minion.tired = true;
    minion.activity = { type: "idle" };
    markWaitingIfIdle(state, minion, now);
    state.log(`A minion completed training and is now Rank ${process.toRank}.`);
}
