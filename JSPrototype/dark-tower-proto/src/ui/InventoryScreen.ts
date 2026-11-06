import { CONFIG } from "../data/config";
import type { GameEngine } from "../engine/GameEngine";
import { escapeHtml, conditionBadges, activityLabel } from "./format";
import type { InventoryTab } from "../main";

function isTreasure(itemId: string): boolean {
    return CONFIG.items.find(i => i.id === itemId)?.tags.includes("treasure") ?? false;
}

function renderStacks(engine: GameEngine, treasures: boolean): string {
    const state = engine.state;
    const stacks = state.stacks.filter(s => isTreasure(s.itemId) === treasures);
    if (stacks.length === 0) return `<p class="muted">Nothing here yet.</p>`;
    return `<div class="stack-grid">${stacks.map(s => {
        const def = CONFIG.items.find(i => i.id === s.itemId)!;
        const selling = state.processes.find(p => p.type === "sale" && p.stackId === s.id);
        return `<div class="stack-card">
            <div class="stack-name">${escapeHtml(def.name)}</div>
            <div class="muted">${s.count} / ${def.maxStackSize} — value ${def.baseValueGold * s.count}g</div>
            ${selling
                ? `<span class="badge badge-tired">Selling…</span>`
                : `<div class="stack-actions">
                    <button data-action="sell-normal" data-id="${s.id}">Sell</button>
                    <button data-action="sell-instant" data-id="${s.id}">Instant (50%)</button>
                   </div>`}
        </div>`;
    }).join("")}</div>`;
}

function renderMinions(engine: GameEngine): string {
    const state = engine.state;
    const rows = state.minions.map(m => `
        <div class="minion-row">
            <span>Rank ${m.rank} — ${activityLabel(m.activity.type)} ${conditionBadges(m.tired, m.injured)}</span>
        </div>`).join("");
    return `<p class="muted">Population: ${state.minions.length}. Weekly upkeep: ${state.weeklySalary()} gold.</p>${rows}`;
}

export function renderInventoryScreen(engine: GameEngine, tab: InventoryTab): string {
    const tabs: { id: InventoryTab; label: string }[] = [
        { id: "treasures", label: "Treasures" },
        { id: "items", label: "Items" },
        { id: "minions", label: "Minions" },
    ];
    const tabsHtml = tabs.map(t =>
        `<button class="tab ${t.id === tab ? "tab-active" : ""}" data-action="switch-inv-tab" data-id="${t.id}">${t.label}</button>`
    ).join("");

    let body: string;
    if (tab === "treasures") body = renderStacks(engine, true);
    else if (tab === "items") body = renderStacks(engine, false);
    else body = renderMinions(engine);

    return `
        <div id="inventory-screen">
            <div class="hud-row hud-tabs">${tabsHtml}</div>
            <div class="screen-body">${body}</div>
        </div>`;
}
