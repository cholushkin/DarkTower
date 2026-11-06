import type { GameEngine } from "../engine/GameEngine";
import { formatClock } from "../engine/Clock";
import type { Screen } from "../main";

export function renderHud(engine: GameEngine, screen: Screen): string {
    const state = engine.state;
    const tabs: { id: Screen; label: string }[] = [
        { id: "tower", label: "Tower" },
        { id: "inventory", label: "Inventory" },
        { id: "world", label: "World Map" },
    ];
    const tabsHtml = tabs.map(t =>
        `<button class="tab ${t.id === screen ? "tab-active" : ""}" data-action="switch-screen" data-id="${t.id}">${t.label}</button>`
    ).join("");

    const paydayBtn = state.paydayPending
        ? `<button class="payday-btn" data-action="open-payroll">💰 Payday Ready!</button>`
        : "";

    return `
        <div id="hud">
            <div class="hud-row">
                <div class="hud-currency">🪙 ${state.gold} Gold</div>
                <div class="hud-currency">💎 ${state.crystals} Crystals</div>
                <div class="hud-currency">👹 ${state.minions.length} Minions</div>
                <div class="hud-currency">🏯 Tower Lv.${state.towerLevel()}</div>
                <div class="hud-clock">${formatClock(state.hours)}</div>
                ${paydayBtn}
            </div>
            <div class="hud-row hud-tabs">${tabsHtml}</div>
        </div>
    `;
}
