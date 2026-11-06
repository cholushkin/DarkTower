import type { GameState } from "../model/GameState";

export function canAfford(state: GameState, gold: number, crystals = 0): boolean {
    return state.gold >= gold && state.crystals >= crystals;
}

export function spend(state: GameState, gold: number, crystals = 0): void {
    state.gold -= gold;
    state.crystals -= crystals;
}

export function creditGold(state: GameState, amount: number): void {
    state.gold += amount;
}

export function creditCrystals(state: GameState, amount: number): void {
    state.crystals += amount;
}
