import { CONFIG } from "../data/config";
import type { GameEngine } from "../engine/GameEngine";

export function renderPayrollModal(engine: GameEngine): string {
    const state = engine.state;
    const total = state.weeklySalary();
    const byRank = CONFIG.ranks.map(r => {
        const count = state.minions.filter(m => m.rank === r.rank).length;
        return count > 0 ? `<div>${r.name} (Rank ${r.rank}): ${count} × ${r.salaryGoldPerWeek}g = ${count * r.salaryGoldPerWeek}g</div>` : "";
    }).join("");
    const canPay = state.gold >= total;

    return `
        <div class="modal-backdrop">
            <div class="modal">
                <h2>Payroll</h2>
                <p>Population: ${state.minions.length} minions</p>
                ${byRank}
                <p><strong>Total due: ${total} gold</strong> (available: ${state.gold} gold)</p>
                <p class="muted">${canPay ? `Balance after paying: ${state.gold - total} gold` : "Not enough gold to pay everyone!"}</p>
                <button data-action="pay-payroll" ${canPay ? "" : "disabled"}>Pay All</button>
                <button data-action="close-payroll">Close</button>
            </div>
        </div>`;
}
