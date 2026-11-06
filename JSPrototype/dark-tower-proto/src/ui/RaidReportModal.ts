import { CONFIG } from "../data/config";
import type { GameEngine } from "../engine/GameEngine";
import { discoveredCounts, selectedSlots } from "../systems/raidReport";
import { missionDef } from "../systems/world";
import { escapeHtml } from "./format";

export function renderRaidReportModal(engine: GameEngine, missionId: string): string {
    const draft = engine.state.pendingReports.get(missionId);
    if (!draft) return "";
    const mission = missionDef(missionId);
    const counts = discoveredCounts(draft);
    const usedSlots = selectedSlots(draft);

    const lootRows = Object.entries(counts).map(([itemId, total]) => {
        const def = CONFIG.items.find(i => i.id === itemId)!;
        const selected = draft.selection[itemId] ?? 0;
        return `
            <div class="loot-row">
                <span>${escapeHtml(def.name)} — found ${total}, selected ${selected}</span>
                <div class="loot-controls">
                    <button data-action="adjust-selection" data-id="${itemId}" data-delta="-1">−</button>
                    <button data-action="adjust-selection" data-id="${itemId}" data-delta="1">+</button>
                </div>
            </div>`;
    }).join("") || `<p class="muted">No physical loot discovered.</p>`;

    return `
        <div class="modal-backdrop">
            <div class="modal">
                <h2>Raid Report — ${escapeHtml(mission.name)}</h2>
                <p class="${draft.success ? "text-ok" : "text-bad"}">${draft.success ? "Success!" : "Failed."}</p>
                ${draft.success ? `<p>Gold found: ${draft.goldFound}, Crystals found: ${draft.crystalsFound}</p>` : ""}
                ${draft.success ? `<p>Carry capacity: ${usedSlots} / ${draft.carryLimit} slots</p>` : ""}
                ${draft.success ? lootRows : ""}
                ${draft.success && draft.prisonersFound.length ? `<p>Prisoners captured: ${draft.prisonersFound.length} (ranks ${draft.prisonersFound.join(", ")})</p>` : ""}
                <button data-action="claim-report" data-id="${missionId}">Claim</button>
            </div>
        </div>`;
}
