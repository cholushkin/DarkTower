import { CONFIG } from "../data/config";
import type { GameState } from "../model/GameState";
import type { UpgradeProcess } from "../model/types";
import { canAfford, spend } from "./economy";
import { addTowerXp } from "./tower";

export function isUpgradeInProgress(state: GameState, upgradeId: string): boolean {
    return state.processes.some(p => p.type === "upgrade" && p.upgradeId === upgradeId);
}

export function canPurchaseUpgrade(state: GameState, upgradeId: string): boolean {
    const def = CONFIG.upgrades.find(u => u.id === upgradeId);
    if (!def) return false;
    if (state.purchasedUpgrades.has(upgradeId) || isUpgradeInProgress(state, upgradeId)) return false;
    if (!def.requires.every(id => state.purchasedUpgrades.has(id))) return false;
    return canAfford(state, def.costGold, def.costCrystals ?? 0);
}

export function purchaseUpgrade(state: GameState, upgradeId: string, now: number): boolean {
    if (!canPurchaseUpgrade(state, upgradeId)) return false;
    const def = CONFIG.upgrades.find(u => u.id === upgradeId)!;
    spend(state, def.costGold, def.costCrystals ?? 0);
    const process: UpgradeProcess = {
        id: state.genId("upgrade"), type: "upgrade",
        startHour: now, endHour: now + def.durationHours, upgradeId,
    };
    state.processes.push(process);
    state.log(`Construction started: ${def.name}.`);
    return true;
}

export function completeUpgrade(state: GameState, process: UpgradeProcess): void {
    const def = CONFIG.upgrades.find(u => u.id === process.upgradeId)!;
    state.purchasedUpgrades.add(def.id);
    addTowerXp(state, def.towerXp);
    state.log(`Upgrade complete: ${def.name}.`);
}
