import type { GameState } from "../model/GameState";

export function payAll(state: GameState): boolean {
    const total = state.weeklySalary();
    if (state.gold < total) return false;
    state.gold -= total;
    state.paydayPending = false;
    state.paydayDueAtHour = null;
    state.log(`Payroll paid: ${total} gold for ${state.minions.length} minions.`);
    return true;
}
