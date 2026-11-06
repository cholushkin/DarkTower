import { GameEngine } from "./engine/GameEngine";
import { adjustSelection } from "./systems/raidReport";
import { renderHud } from "./ui/Hud";
import { renderRoomsScreen } from "./ui/RoomsPanel";
import { renderInventoryScreen } from "./ui/InventoryScreen";
import { renderWorldMapScreen } from "./ui/WorldMapScreen";
import { renderRaidReportModal } from "./ui/RaidReportModal";
import { renderPayrollModal } from "./ui/PayrollModal";
import { renderDebugPanel } from "./ui/DebugPanel";

export type Screen = "tower" | "inventory" | "world";
export type InventoryTab = "treasures" | "items" | "minions";

const engine = new GameEngine();

let screen: Screen = "tower";
let invTab: InventoryTab = "treasures";
let activeSquadMission: string | null = null;
let squadSelection = new Set<string>();
let payrollOpen = false;
let openReportMissionId: string | null = null;

const app = document.getElementById("app")!;

function resetSquadEditor(): void {
    activeSquadMission = null;
    squadSelection = new Set<string>();
}

function render(): void {
    const state = engine.state;

    if (state.gameOver) {
        app.innerHTML = `<div class="end-screen"><h1>Game Over — Bankruptcy</h1><p>The weekly payroll went unpaid. The tower has fallen.</p></div>`;
        return;
    }
    if (state.victory) {
        app.innerHTML = `<div class="end-screen"><h1>Victory!</h1><p>Every mission on the World Map has been claimed.</p></div>`;
        return;
    }

    let screenHtml = "";
    if (screen === "tower") screenHtml = renderRoomsScreen(engine);
    else if (screen === "inventory") screenHtml = renderInventoryScreen(engine, invTab);
    else screenHtml = renderWorldMapScreen(engine, { activeSquadMission, selection: squadSelection });

    const overlay = payrollOpen
        ? renderPayrollModal(engine)
        : openReportMissionId
            ? renderRaidReportModal(engine, openReportMissionId)
            : "";

    app.innerHTML = `
        ${renderHud(engine, screen)}
        <main class="screen-body">${screenHtml}</main>
        ${renderDebugPanel(engine)}
        ${overlay}
    `;
}

app.addEventListener("click", (e) => {
    const target = (e.target as HTMLElement).closest("[data-action]") as HTMLElement | null;
    if (!target || (target as HTMLButtonElement).disabled) return;
    const action = target.getAttribute("data-action")!;
    const id = target.getAttribute("data-id") ?? "";

    switch (action) {
        case "switch-screen":
            screen = id as Screen;
            resetSquadEditor();
            break;
        case "switch-inv-tab":
            invTab = id as InventoryTab;
            break;
        case "buy-upgrade":
            engine.buyUpgrade(id);
            break;
        case "sell-normal":
            engine.startNormalSale(id);
            break;
        case "sell-instant":
            engine.instantSale(id);
            break;
        case "train":
            engine.startTraining(id);
            break;
        case "research":
            engine.startResearch(id);
            break;
        case "select-squad":
            activeSquadMission = id;
            squadSelection = new Set<string>();
            break;
        case "cancel-squad":
            resetSquadEditor();
            break;
        case "launch-raid":
            if (engine.launchRaid(id, [...squadSelection])) resetSquadEditor();
            break;
        case "open-report":
            engine.setPaused(true);
            openReportMissionId = id;
            break;
        case "claim-report":
            engine.claimReport(id);
            openReportMissionId = null;
            engine.setPaused(false);
            break;
        case "open-payroll":
            engine.setPaused(true);
            payrollOpen = true;
            break;
        case "close-payroll":
            payrollOpen = false;
            engine.setPaused(false);
            break;
        case "pay-payroll":
            if (engine.payPayroll()) {
                payrollOpen = false;
                engine.setPaused(false);
            }
            break;
        case "set-speed":
            engine.setSpeed(Number(target.getAttribute("data-value")));
            break;
        case "force-complete":
            engine.forceCompleteProcess(id);
            break;
        case "adjust-selection": {
            const missionId = openReportMissionId;
            const draft = missionId ? engine.state.pendingReports.get(missionId) : undefined;
            if (draft) adjustSelection(draft, id, Number(target.getAttribute("data-delta")));
            break;
        }
    }
    render();
});

app.addEventListener("change", (e) => {
    const target = e.target as HTMLInputElement;
    if (target.getAttribute("data-action") === "toggle-squad-member") {
        const id = target.getAttribute("data-id")!;
        if (target.checked) squadSelection.add(id);
        else squadSelection.delete(id);
        render();
    }
});

let lastTime = performance.now();
function loop(now: number): void {
    const deltaSeconds = (now - lastTime) / 1000;
    lastTime = now;
    engine.tick(deltaSeconds);
    requestAnimationFrame(loop);
}

// Rendering rebuilds the whole screen as HTML (simple/minimal by design — see README), so it
// runs on a slow timer rather than every animation frame: cheaper, and avoids the DOM churn
// that comes from replacing clickable elements 60 times a second while the game clock ticks.
render();
requestAnimationFrame(loop);
setInterval(render, 200);
