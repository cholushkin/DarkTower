import { CONFIG } from "../data/config";
import { GameState } from "../model/GameState";
import type { Process, RaidProcess } from "../model/types";
import { realSecondsToGameHours } from "./Clock";
import { completeRest, completeTreatment, assignBeds } from "../systems/recovery";
import { completeTraining, startTraining as sysStartTraining } from "../systems/trainingHall";
import { completeSale, startNormalSale as sysStartNormalSale, instantSale as sysInstantSale } from "../systems/storehouse";
import { completeResearch, startResearch as sysStartResearch } from "../systems/laboratory";
import { completeUpgrade, purchaseUpgrade as sysPurchaseUpgrade } from "../systems/upgrades";
import { completeDungeonProcess, assignCells } from "../systems/dungeon";
import { launchRaid as sysLaunchRaid, resolveRaid, missionDef } from "../systems/world";
import { claimReport as sysClaimReport } from "../systems/raidReport";
import { payAll as sysPayAll } from "../systems/payroll";
import { nightfall, checkDepartures, triggerPaydayDue, checkBankruptcy } from "../systems/lifecycle";

export class GameEngine {
    readonly state: GameState;

    constructor(state = new GameState()) {
        this.state = state;
    }

    tick(realDeltaSeconds: number): void {
        if (this.state.gameOver || this.state.victory || this.state.paused) return;
        const deltaHours = realSecondsToGameHours(realDeltaSeconds, this.state.speed);
        this.advance(deltaHours);
    }

    setPaused(paused: boolean): void {
        this.state.paused = paused;
    }

    setSpeed(speed: number): void {
        this.state.speed = speed;
    }

    private advance(deltaHours: number): void {
        const target = this.state.hours + deltaHours;
        let guard = 0;
        while (this.state.hours < target - 1e-9 && guard++ < 100000) {
            const nextDayBoundary = (Math.floor(this.state.hours / 24) + 1) * 24;
            const stepTo = Math.min(nextDayBoundary, target);
            this.completeProcessesUpTo(stepTo);
            this.state.hours = stepTo;
            this.runAutoAssignment(stepTo);
            if (Math.abs(stepTo - nextDayBoundary) < 1e-6) {
                this.onDayBoundary(stepTo);
                this.runAutoAssignment(stepTo);
            }
        }
        checkBankruptcy(this.state, this.state.hours);
    }

    private onDayBoundary(now: number): void {
        nightfall(this.state, now);
        const day = Math.round(now / 24);
        if (day > 0 && day % CONFIG.constants.weekDays === 0) {
            triggerPaydayDue(this.state, now);
        }
        checkDepartures(this.state, now);
    }

    private runAutoAssignment(now: number): void {
        assignBeds(this.state, now);
        assignCells(this.state, now);
    }

    private completeProcessesUpTo(boundary: number): void {
        const due = this.state.processes
            .filter(p => p.endHour <= boundary + 1e-9)
            .sort((a, b) => a.endHour - b.endHour);
        for (const process of due) {
            this.dispatchCompletion(process, process.endHour);
            this.state.processes = this.state.processes.filter(p => p.id !== process.id);
        }
    }

    private dispatchCompletion(process: Process, now: number): void {
        switch (process.type) {
            case "rest": completeRest(this.state, process, now); break;
            case "treatment": completeTreatment(this.state, process, now); break;
            case "training": completeTraining(this.state, process, now); break;
            case "sale": completeSale(this.state, process); break;
            case "research": completeResearch(this.state, process); break;
            case "upgrade": completeUpgrade(this.state, process); break;
            case "dungeon": completeDungeonProcess(this.state, process, now); break;
            case "raid": this.completeRaid(process); break;
        }
    }

    private completeRaid(process: RaidProcess): void {
        const draft = resolveRaid(this.state, process);
        this.state.pendingReports.set(process.missionId, draft);
        this.state.missionRunningProcess.delete(process.missionId);
        this.state.log(`Raid on ${missionDef(process.missionId).name} has returned. Open the Raid Report.`);
    }

    forceCompleteProcess(processId: string): void {
        const process = this.state.processes.find(p => p.id === processId);
        if (!process) return;
        this.dispatchCompletion(process, this.state.hours);
        this.state.processes = this.state.processes.filter(p => p.id !== processId);
        this.runAutoAssignment(this.state.hours);
        checkBankruptcy(this.state, this.state.hours);
    }

    // ---- Action API (thin pass-throughs the UI calls directly) ----

    buyUpgrade(upgradeId: string): boolean {
        return sysPurchaseUpgrade(this.state, upgradeId, this.state.hours);
    }

    startTraining(minionId: string): boolean {
        return sysStartTraining(this.state, minionId, this.state.hours);
    }

    startResearch(researchId: string): boolean {
        return sysStartResearch(this.state, researchId, this.state.hours);
    }

    startNormalSale(stackId: string): boolean {
        return sysStartNormalSale(this.state, stackId, this.state.hours);
    }

    instantSale(stackId: string): boolean {
        return sysInstantSale(this.state, stackId);
    }

    launchRaid(missionId: string, squad: string[]): boolean {
        return sysLaunchRaid(this.state, missionId, squad, this.state.hours);
    }

    claimReport(missionId: string): boolean {
        const claimed = sysClaimReport(this.state, missionId, this.state.hours);
        if (claimed) this.runAutoAssignment(this.state.hours);
        return claimed;
    }

    payPayroll(): boolean {
        return sysPayAll(this.state);
    }
}
