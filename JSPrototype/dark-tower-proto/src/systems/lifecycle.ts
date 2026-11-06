import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import { markWaitingIfIdle } from "./recovery";

export function nightfall(state: GameState, now: number): void {
    for (const minion of state.minions) {
        minion.tired = true;
        markWaitingIfIdle(state, minion, now);
    }
    state.log("Nightfall. All minions grow tired.");
}

export function checkDepartures(state: GameState, now: number): void {
    const departing = state.minions.filter(m =>
        m.waitingSince !== null && now - m.waitingSince > CONFIG.constants.bedEvictionHours);
    if (departing.length === 0) return;

    state.minions = state.minions.filter(m => !departing.includes(m));
    for (const minion of departing) {
        const notice = `A Rank ${minion.rank} minion left the tower after too long without a bed.`;
        state.departureNotices.push(notice);
        state.log(notice);
    }
}

export function triggerPaydayDue(state: GameState, now: number): void {
    state.paydayPending = true;
    state.paydayDueAtHour = now + CONFIG.constants.paydayWindowHours;
    state.log("Payday has arrived. The Dark Lord awaits payment.");
}

export function checkBankruptcy(state: GameState, now: number): void {
    if (state.gameOver) return;
    if (state.paydayPending && state.paydayDueAtHour !== null && now >= state.paydayDueAtHour) {
        state.gameOver = "bankruptcy";
        state.log("GAME OVER — Bankruptcy. The weekly payroll went unpaid.");
    }
}
