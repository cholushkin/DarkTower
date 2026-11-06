import { CONFIG } from "../data/config";
import type { ResearchDef } from "../data/types";
import type { GameState } from "../model/GameState";
import type { ResearchProcess } from "../model/types";
import { canAfford, spend } from "./economy";
import { addTowerXp } from "./tower";

export function availableResearch(state: GameState): ResearchDef[] {
    const tier = state.researchTierUnlocked();
    return CONFIG.research.filter(r =>
        r.tier <= tier &&
        !state.completedResearch.has(r.id) &&
        r.requires.every(id => state.completedResearch.has(id)));
}

export function startResearch(state: GameState, researchId: string, now: number): boolean {
    if (state.activeResearchId !== null) return false;
    const def = availableResearch(state).find(r => r.id === researchId);
    if (!def || !canAfford(state, def.costGold)) return false;
    spend(state, def.costGold);
    const duration = def.durationHours * state.researchEfficiencyMultiplier();
    const process: ResearchProcess = {
        id: state.genId("research"), type: "research",
        startHour: now, endHour: now + duration, researchId,
    };
    state.processes.push(process);
    state.activeResearchId = researchId;
    state.log(`Research started: ${def.name}.`);
    return true;
}

export function completeResearch(state: GameState, process: ResearchProcess): void {
    const def = CONFIG.research.find(r => r.id === process.researchId)!;
    state.completedResearch.add(def.id);
    state.activeResearchId = null;
    addTowerXp(state, def.towerXp);
    state.log(`Research complete: ${def.name}.`);
}
