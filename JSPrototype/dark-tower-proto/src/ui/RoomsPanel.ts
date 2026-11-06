import { CONFIG } from "../data/config";
import type { RoomId } from "../data/types";
import type { GameEngine } from "../engine/GameEngine";
import { canPurchaseUpgrade, isUpgradeInProgress } from "../systems/upgrades";
import { availableResearch } from "../systems/laboratory";
import { canTrain, transitionFor, availableStations } from "../systems/trainingHall";
import { freeCells } from "../systems/dungeon";
import { escapeHtml, fmtHours, conditionBadges } from "./format";

function renderUpgradeList(engine: GameEngine, room: RoomId): string {
    const state = engine.state;
    const defs = CONFIG.upgrades.filter(u => u.room === room);
    const rows = defs.map(u => {
        const owned = state.purchasedUpgrades.has(u.id);
        const inProgress = isUpgradeInProgress(state, u.id);
        const locked = !u.requires.every(r => state.purchasedUpgrades.has(r));
        const buyable = canPurchaseUpgrade(state, u.id);
        let status: string;
        if (owned) status = `<span class="badge badge-ok">Owned</span>`;
        else if (inProgress) status = `<span class="badge badge-tired">Building…</span>`;
        else if (locked) status = `<span class="badge">Locked</span>`;
        else status = `<button data-action="buy-upgrade" data-id="${u.id}" ${buyable ? "" : "disabled"}>Build (${u.costGold}g, ${fmtHours(u.durationHours)})</button>`;
        return `<div class="upgrade-row">
            <div class="upgrade-name">${escapeHtml(u.name)}<div class="muted">${escapeHtml(u.description)}</div></div>
            <div class="upgrade-status">${status}</div>
        </div>`;
    }).join("");
    return `<div class="upgrade-list">${rows}</div>`;
}

function renderStorehouse(engine: GameEngine): string {
    const state = engine.state;
    return `
        <section class="room-panel" id="room-storehouse">
            <h3>Storehouse</h3>
            <p class="muted">Storage: ${state.usedSlots()} / ${state.storageSlots()} slots used. Sell items from the Inventory screen.</p>
            ${renderUpgradeList(engine, "storehouse")}
        </section>`;
}

function renderQuarters(engine: GameEngine): string {
    const state = engine.state;
    const occupied = state.processes.filter(p => p.type === "rest").length;
    return `
        <section class="room-panel" id="room-quarters">
            <h3>Quarters</h3>
            <p class="muted">Beds: ${occupied} / ${state.bedCount()} occupied. Tired minions rest automatically (6h).</p>
            ${renderUpgradeList(engine, "quarters")}
        </section>`;
}

function renderInfirmary(engine: GameEngine): string {
    const state = engine.state;
    const occupied = state.processes.filter(p => p.type === "treatment").length;
    return `
        <section class="room-panel" id="room-infirmary">
            <h3>Infirmary</h3>
            <p class="muted">Treatment beds: ${occupied} / ${state.treatmentBedCount()} occupied. Injured minions are treated automatically (takes priority over Tired).</p>
            ${renderUpgradeList(engine, "infirmary")}
        </section>`;
}

function renderTrainingHall(engine: GameEngine): string {
    const state = engine.state;
    const stationsFree = availableStations(state);
    const trainable = state.minions.filter(m => transitionFor(m.rank));
    const rows = trainable.map(m => {
        const transition = transitionFor(m.rank)!;
        const unlocked = state.isTrainingTransitionUnlocked(m.rank);
        const can = canTrain(state, m.id);
        let action: string;
        if (m.activity.type === "training") action = `<span class="badge badge-tired">Training…</span>`;
        else if (!unlocked) action = `<span class="badge">Transition locked</span>`;
        else action = `<button data-action="train" data-id="${m.id}" ${can ? "" : "disabled"}>Train → Rank ${transition.toRank} (${transition.costGold}g, ${fmtHours(transition.durationHours)})</button>`;
        return `<div class="minion-row">
            <span>Rank ${m.rank} minion ${conditionBadges(m.tired, m.injured)}</span>
            ${action}
        </div>`;
    }).join("") || `<p class="muted">No minions eligible for further training.</p>`;

    return `
        <section class="room-panel" id="room-training">
            <h3>Training Hall</h3>
            <p class="muted">Stations free: ${stationsFree} / ${state.trainingStationCount()}.</p>
            ${rows}
            ${renderUpgradeList(engine, "trainingHall")}
        </section>`;
}

function renderLaboratory(engine: GameEngine): string {
    const state = engine.state;
    const active = state.activeResearchId ? CONFIG.research.find(r => r.id === state.activeResearchId) : null;
    const activeHtml = active
        ? `<p class="muted">Researching: <strong>${escapeHtml(active.name)}</strong>…</p>`
        : `<p class="muted">No active research (only one at a time, no queue).</p>`;
    const options = availableResearch(state).map(r => `
        <div class="upgrade-row">
            <div class="upgrade-name">${escapeHtml(r.name)}<div class="muted">${escapeHtml(r.description)}</div></div>
            <div class="upgrade-status">
                <button data-action="research" data-id="${r.id}" ${state.activeResearchId || !canAfford(state, r.costGold) ? "disabled" : ""}>
                    Research (${r.costGold}g, ${fmtHours(r.durationHours * state.researchEfficiencyMultiplier())})
                </button>
            </div>
        </div>`).join("") || `<p class="muted">No further research currently available.</p>`;
    const completed = CONFIG.research.filter(r => state.completedResearch.has(r.id))
        .map(r => `<li>${escapeHtml(r.name)}</li>`).join("");

    return `
        <section class="room-panel" id="room-laboratory">
            <h3>Laboratory</h3>
            ${activeHtml}
            <div class="upgrade-list">${options}</div>
            ${completed ? `<p class="muted">Completed:</p><ul>${completed}</ul>` : ""}
            ${renderUpgradeList(engine, "laboratory")}
        </section>`;
}

function canAfford(state: GameEngine["state"], gold: number): boolean {
    return state.gold >= gold;
}

function renderDungeon(engine: GameEngine): string {
    const state = engine.state;
    const occupied = state.prisoners.filter(p => p.processId !== null).length;
    const waiting = state.prisoners.length - occupied;
    return `
        <section class="room-panel" id="room-dungeon">
            <h3>Dungeon</h3>
            <p class="muted">Cells: ${occupied} / ${state.dungeonCellCount()} occupied (free: ${freeCells(state)}). Waiting: ${waiting}.</p>
            ${renderUpgradeList(engine, "dungeon")}
        </section>`;
}

function renderWarRoom(engine: GameEngine): string {
    const state = engine.state;
    return `
        <section class="room-panel" id="room-warroom">
            <h3>War Room</h3>
            <p class="muted">Enemy Intel Lv.${state.enemyIntelLevel()}, Loot Intel Lv.${state.lootIntelLevel()}. Plan raids from the World Map screen.</p>
            ${renderUpgradeList(engine, "warRoom")}
        </section>`;
}

export function renderRoomsScreen(engine: GameEngine): string {
    return `
        <div id="rooms-screen">
            ${renderStorehouse(engine)}
            ${renderQuarters(engine)}
            ${renderInfirmary(engine)}
            ${renderTrainingHall(engine)}
            ${renderLaboratory(engine)}
            ${renderDungeon(engine)}
            ${renderWarRoom(engine)}
        </div>`;
}
