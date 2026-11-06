import { CONFIG } from "../data/config";
import type { GameEngine } from "../engine/GameEngine";
import { previewRaid, canLaunchRaid } from "../systems/world";
import { escapeHtml, pct, fmtHours } from "./format";

export interface SquadUiState {
    activeSquadMission: string | null;
    selection: Set<string>;
}

function enemyEstimate(state: GameEngine["state"], requiredPower: number): string {
    const level = state.enemyIntelLevel();
    if (level <= 0) return "Enemy strength unknown";
    const spread = level === 1 ? 0.5 : 0.2;
    const lo = Math.round(requiredPower * (1 - spread));
    const hi = Math.round(requiredPower * (1 + spread));
    return `Estimated strength: ${lo}–${hi}`;
}

function lootEstimate(state: GameEngine["state"], missionId: string): string {
    const level = state.lootIntelLevel();
    const mission = CONFIG.missions.find(m => m.id === missionId)!;
    if (level <= 0) return "Loot intelligence unavailable";
    const names = mission.loot.map(l => {
        const def = CONFIG.items.find(i => i.id === l.itemId)!;
        return level >= 2 ? `${def.name} (${pct(l.chance)})` : def.name;
    });
    return `Possible loot: ${names.join(", ")}`;
}

function remainingTime(engine: GameEngine, missionId: string): string {
    const processId = engine.state.missionRunningProcess.get(missionId);
    const process = engine.state.processes.find(p => p.id === processId);
    if (!process) return "";
    return fmtHours(Math.max(0, process.endHour - engine.state.hours));
}

function renderMission(engine: GameEngine, missionId: string, ui: SquadUiState): string {
    const state = engine.state;
    const mission = CONFIG.missions.find(m => m.id === missionId)!;
    const missionState = state.missionState(missionId);
    const claimed = state.claimedMissions.has(missionId);

    let body = "";
    if (missionState === "LOCKED") {
        body = `<span class="badge">Locked</span>`;
    } else if (missionState === "RUNNING") {
        body = `<span class="badge badge-tired">Raid running — ${remainingTime(engine, missionId)} left</span>`;
    } else if (missionState === "COMPLETED_UNCLAIMED") {
        body = `<button data-action="open-report" data-id="${missionId}">Open Raid Report</button>`;
    } else if (missionState === "COOLDOWN") {
        body = `<span class="badge">Attempted this week</span>`;
    } else {
        const isEditing = ui.activeSquadMission === missionId;
        if (!isEditing) {
            body = `<button data-action="select-squad" data-id="${missionId}">Choose Squad</button>`;
        } else {
            const squad = [...ui.selection];
            const idle = state.minions.filter(m => m.activity.type === "idle" && !m.tired && !m.injured);
            const checkboxes = idle.map(m => `
                <label class="squad-pick">
                    <input type="checkbox" data-action="toggle-squad-member" data-id="${m.id}" ${ui.selection.has(m.id) ? "checked" : ""}>
                    Rank ${m.rank}
                </label>`).join("") || `<p class="muted">No available minions.</p>`;
            const preview = squad.length ? previewRaid(state, missionId, squad) : null;
            const previewHtml = preview ? `
                <p class="muted">Failure chance ≈ ${pct(preview.failureChance)}, duration ≈ ${fmtHours(preview.durationHours)}, avg injury chance ≈ ${pct(preview.avgInjuryChance)}</p>
            ` : "";
            const canLaunch = canLaunchRaid(state, missionId, squad);
            body = `
                <div class="squad-editor">
                    ${checkboxes}
                    ${previewHtml}
                    <button data-action="launch-raid" data-id="${missionId}" ${canLaunch ? "" : "disabled"}>Launch Raid</button>
                    <button data-action="cancel-squad">Cancel</button>
                </div>`;
        }
    }

    return `
        <div class="mission-card">
            <div class="mission-header">
                <strong>${escapeHtml(mission.name)}</strong>
                ${claimed ? `<span class="badge badge-ok">✔ Claimed</span>` : ""}
            </div>
            <div class="muted">${enemyEstimate(state, mission.requiredPower)}</div>
            <div class="muted">${lootEstimate(state, missionId)}</div>
            <div class="mission-body">${body}</div>
        </div>`;
}

export function renderWorldMapScreen(engine: GameEngine, ui: SquadUiState): string {
    const state = engine.state;
    const tiersHtml = CONFIG.worldTiers.map(tier => {
        const unlocked = state.unlockedTiers.has(tier.tier);
        const missions = CONFIG.missions.filter(m => m.tier === tier.tier);
        return `
            <section class="tier-section">
                <h3>World Tier ${tier.tier}: ${escapeHtml(tier.name)} ${unlocked ? "" : "🔒"}</h3>
                <div class="mission-grid">${missions.map(m => renderMission(engine, m.id, ui)).join("")}</div>
            </section>`;
    }).join("");

    return `<div id="world-screen">${tiersHtml}</div>`;
}
