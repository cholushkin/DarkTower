import { CONFIG } from "../data/config";
import type { UpgradeEffect } from "../data/types";
import type {
    Minion,
    Prisoner,
    ItemStack,
    Process,
    MissionState,
    RaidReportDraft,
} from "./types";

export class GameState {
    hours = 0;
    speed = 1;
    paused = false;
    rng: () => number = Math.random;

    gold = CONFIG.startingBalance.gold;
    crystals = CONFIG.startingBalance.crystals;

    minions: Minion[] = [];
    prisoners: Prisoner[] = [];
    stacks: ItemStack[] = [];
    processes: Process[] = [];
    private nextSeq = 1;

    purchasedUpgrades = new Set<string>();
    completedResearch = new Set<string>();
    activeResearchId: string | null = null;

    unlockedTiers = new Set<number>([1]);
    claimedMissions = new Set<string>();
    missionLastLaunchWeek = new Map<string, number>();
    missionRunningProcess = new Map<string, string>();
    pendingReports = new Map<string, RaidReportDraft>();

    towerXp = 0;

    paydayPending = false;
    paydayDueAtHour: number | null = null;

    gameOver: "bankruptcy" | null = null;
    victory = false;

    logLines: string[] = [];
    departureNotices: string[] = [];

    constructor() {
        for (const rank of CONFIG.startingBalance.minionRanks) {
            this.minions.push({
                id: this.genId("minion"),
                rank,
                tired: false,
                injured: false,
                activity: { type: "idle" },
                waitingSince: null,
            });
        }
        for (const starting of CONFIG.startingItems) {
            this.addItems(starting.itemId, starting.count);
        }
        this.log("The tower stirs. Three minions await your command.");
    }

    genId(prefix: string): string {
        return `${prefix}_${this.nextSeq++}`;
    }

    log(message: string): void {
        this.logLines.push(message);
        if (this.logLines.length > 250) this.logLines.shift();
    }

    get day(): number {
        return Math.floor(this.hours / 24);
    }

    get week(): number {
        return Math.floor(this.day / CONFIG.constants.weekDays);
    }

    // ---- Derived room capacities / upgrade effects ----

    private sumEffect(pick: (e: UpgradeEffect) => number | null): number {
        let total = 0;
        for (const u of CONFIG.upgrades) {
            if (!this.purchasedUpgrades.has(u.id)) continue;
            const v = pick(u.effect);
            if (v !== null) total += v;
        }
        return total;
    }

    private productEffect(pick: (e: UpgradeEffect) => number | null): number {
        let total = 1;
        for (const u of CONFIG.upgrades) {
            if (!this.purchasedUpgrades.has(u.id)) continue;
            const v = pick(u.effect);
            if (v !== null) total *= v;
        }
        return total;
    }

    storageSlots(): number {
        return CONFIG.startingBalance.storageSlots + this.sumEffect(e => (e.type === "storageSlots" ? e.amount : null));
    }

    usedSlots(): number {
        return this.stacks.length;
    }

    bedCount(): number {
        return CONFIG.startingBalance.beds + this.sumEffect(e => (e.type === "bed" ? 1 : null));
    }

    treatmentBedCount(): number {
        return CONFIG.startingBalance.treatmentBeds + this.sumEffect(e => (e.type === "treatmentBed" ? 1 : null));
    }

    trainingStationCount(): number {
        return CONFIG.startingBalance.trainingStations + this.sumEffect(e => (e.type === "trainingStation" ? 1 : null));
    }

    dungeonCellCount(): number {
        return CONFIG.startingBalance.dungeonCells + this.sumEffect(e => (e.type === "dungeonCell" ? 1 : null));
    }

    saleEfficiencyMultiplier(): number {
        return this.productEffect(e => (e.type === "saleEfficiency" ? e.multiplier : null));
    }

    treatmentEfficiencyMultiplier(): number {
        return this.productEffect(e => (e.type === "treatmentEfficiency" ? e.multiplier : null));
    }

    researchEfficiencyMultiplier(): number {
        return this.productEffect(e => (e.type === "researchEfficiency" ? e.multiplier : null));
    }

    tortureEfficiencyMultiplier(): number {
        return this.productEffect(e => (e.type === "tortureEfficiency" ? e.multiplier : null));
    }

    recruitmentEfficiencyBonus(): number {
        return this.sumEffect(e => (e.type === "recruitmentEfficiency" ? e.bonus : null));
    }

    enemyIntelLevel(): number {
        return this.sumEffect(e => (e.type === "enemyIntel" ? 1 : null));
    }

    lootIntelLevel(): number {
        return this.sumEffect(e => (e.type === "lootIntel" ? 1 : null));
    }

    researchTierUnlocked(): number {
        return 1 + this.sumEffect(e => (e.type === "researchTier" ? 1 : null));
    }

    isTrainingTransitionUnlocked(fromRank: number): boolean {
        for (const u of CONFIG.upgrades) {
            if (u.effect.type === "trainingUnlock" && u.effect.fromRank === fromRank) {
                return this.purchasedUpgrades.has(u.id);
            }
        }
        return false;
    }

    effectiveAttack(rank: number): number {
        return this.effectiveStat(rank, "attack");
    }

    effectiveDefense(rank: number): number {
        return this.effectiveStat(rank, "defense");
    }

    private effectiveStat(rank: number, stat: "attack" | "defense"): number {
        const base = CONFIG.ranks[rank - 1][stat];
        let value = base;
        for (const r of CONFIG.research) {
            if (!this.completedResearch.has(r.id)) continue;
            for (const eff of r.effects) {
                if (eff.stat === stat && eff.mode === "add") value += eff.value;
            }
        }
        for (const r of CONFIG.research) {
            if (!this.completedResearch.has(r.id)) continue;
            for (const eff of r.effects) {
                if (eff.stat === stat && eff.mode === "mult") value *= eff.value;
            }
        }
        return value;
    }

    towerLevel(): number {
        let level = 1;
        for (let i = 0; i < CONFIG.towerXpThresholds.length; i++) {
            if (this.towerXp >= CONFIG.towerXpThresholds[i]) level = i + 1;
        }
        return level;
    }

    // ---- Minion / inventory helpers ----

    findMinion(id: string): Minion | undefined {
        return this.minions.find(m => m.id === id);
    }

    idleUnconditionedMinions(): Minion[] {
        return this.minions.filter(m => m.activity.type === "idle" && !m.tired && !m.injured);
    }

    weeklySalary(): number {
        return this.minions.reduce((sum, m) => sum + CONFIG.ranks[m.rank - 1].salaryGoldPerWeek, 0);
    }

    addItems(itemId: string, count: number): number {
        const def = CONFIG.items.find(i => i.id === itemId);
        if (!def || count <= 0) return count;
        let remaining = count;
        for (const stack of this.stacks) {
            if (stack.itemId !== itemId || stack.count >= def.maxStackSize) continue;
            const room = def.maxStackSize - stack.count;
            const add = Math.min(room, remaining);
            stack.count += add;
            remaining -= add;
            if (remaining <= 0) return 0;
        }
        while (remaining > 0 && this.usedSlots() < this.storageSlots()) {
            const add = Math.min(def.maxStackSize, remaining);
            this.stacks.push({ id: this.genId("stack"), itemId, count: add });
            remaining -= add;
        }
        return remaining;
    }

    removeStack(stackId: string): void {
        this.stacks = this.stacks.filter(s => s.id !== stackId);
    }

    missionState(missionId: string): MissionState {
        const mission = CONFIG.missions.find(m => m.id === missionId)!;
        if (!this.unlockedTiers.has(mission.tier)) return "LOCKED";
        if (this.pendingReports.has(missionId)) return "COMPLETED_UNCLAIMED";
        if (this.missionRunningProcess.has(missionId)) return "RUNNING";
        const lastWeek = this.missionLastLaunchWeek.get(missionId);
        if (lastWeek !== undefined && lastWeek >= this.week) return "COOLDOWN";
        return "AVAILABLE";
    }
}
