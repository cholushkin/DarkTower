import type { GameState } from "../model/GameState";

export function addTowerXp(state: GameState, amount: number): void {
    if (amount <= 0) return;
    const before = state.towerLevel();
    state.towerXp += amount;
    const after = state.towerLevel();
    if (after > before) state.log(`Tower Level increased to ${after}!`);
}
