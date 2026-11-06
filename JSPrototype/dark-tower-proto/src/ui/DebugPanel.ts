import type { GameEngine } from "../engine/GameEngine";
import { fmtHours } from "./format";

export function renderDebugPanel(engine: GameEngine): string {
    const state = engine.state;
    const speeds = [1, 5, 20];
    const speedBtns = speeds.map(s =>
        `<button class="tab ${state.speed === s ? "tab-active" : ""}" data-action="set-speed" data-value="${s}">${s}×</button>`
    ).join("");

    const processRows = state.processes.map(p => `
        <div class="process-row">
            <span>${p.type} — ${fmtHours(Math.max(0, p.endHour - state.hours))} left</span>
            <button data-action="force-complete" data-id="${p.id}">⚡ Force complete</button>
        </div>`).join("") || `<p class="muted">No active processes.</p>`;

    return `
        <details id="debug-panel" open>
            <summary>Debug / Scheduler</summary>
            <div class="debug-body">
                <div>Speed: ${speedBtns}</div>
                ${processRows}
            </div>
        </details>`;
}
