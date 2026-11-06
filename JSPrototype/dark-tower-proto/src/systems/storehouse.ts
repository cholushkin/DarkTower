import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { SaleProcess } from "../model/types";
import { creditGold } from "./economy";

function itemDef(itemId: string) {
    return CONFIG.items.find(i => i.id === itemId)!;
}

function fullStackValue(itemId: string, count: number): number {
    return itemDef(itemId).baseValueGold * count;
}

function normalSaleDurationHours(count: number, state: GameState): number {
    const base = CONFIG.constants.saleBaseHours + CONFIG.constants.saleHoursPerCopy * count;
    return base * state.saleEfficiencyMultiplier();
}

export function startNormalSale(state: GameState, stackId: string, now: number): boolean {
    const stack = state.stacks.find(s => s.id === stackId);
    if (!stack) return false;
    const goldValue = fullStackValue(stack.itemId, stack.count);
    const duration = normalSaleDurationHours(stack.count, state);
    state.removeStack(stackId);
    state.processes.push({
        id: state.genId("sale"),
        type: "sale",
        startHour: now,
        endHour: now + duration,
        stackId,
        itemId: stack.itemId,
        count: stack.count,
        goldValue,
    });
    state.log(`Selling a stack of ${stack.count} ${itemDef(stack.itemId).name} — ready in ${duration.toFixed(1)}h.`);
    return true;
}

export function instantSale(state: GameState, stackId: string): boolean {
    const stack = state.stacks.find(s => s.id === stackId);
    if (!stack) return false;
    const payout = Math.floor(fullStackValue(stack.itemId, stack.count) * CONFIG.constants.instantSaleFraction);
    state.removeStack(stackId);
    creditGold(state, payout);
    state.log(`Instantly sold ${stack.count} ${itemDef(stack.itemId).name} for ${payout} gold.`);
    return true;
}

export function completeSale(state: GameState, process: SaleProcess): void {
    creditGold(state, process.goldValue);
    state.log(`Sale complete: received ${process.goldValue} gold for ${process.count} ${itemDef(process.itemId).name}.`);
}
