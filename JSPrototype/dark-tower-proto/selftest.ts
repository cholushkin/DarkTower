/**
 * Headless end-to-end smoke test for the Dark Tower prototype engine (no DOM/browser needed).
 * Run with `npm run selftest`. Throws (non-zero exit) on the first failed assertion.
 */
import { CONFIG } from "./src/data/config";
import { GameEngine } from "./src/engine/GameEngine";
import { previewRaid } from "./src/systems/world";
import { admitPrisoner, assignCells, freeCells } from "./src/systems/dungeon";
import type { Process, RaidReportDraft } from "./src/model/types";

let passed = 0;
function check(condition: boolean, message: string): void {
    if (!condition) throw new Error(`FAILED: ${message}`);
    passed++;
    console.log(`  ok: ${message}`);
}

function findProcess(engine: GameEngine, type: Process["type"]): Process {
    const p = engine.state.processes.find(x => x.type === type);
    if (!p) throw new Error(`expected an active '${type}' process`);
    return p;
}

console.log("== Dark Tower prototype self-test ==\n");

// --- 1. Initial state ---
console.log("[1] Initial state");
{
    const engine = new GameEngine();
    check(engine.state.minions.length === 3, "starts with 3 minions");
    check(engine.state.gold === CONFIG.startingBalance.gold, "starts with configured gold");
    check(engine.state.missionState("t1_millbrook") === "AVAILABLE", "tier-1 mission is available at start");
    check(engine.state.missionState("t2_ravenswood") === "LOCKED", "tier-2 mission is locked at start");
}

// --- 2. Room upgrade purchase + construction ---
console.log("\n[2] Storehouse upgrade");
{
    const engine = new GameEngine();
    const before = engine.state.storageSlots();
    check(engine.buyUpgrade("sh_storage_1"), "can start Storage Shelves I");
    check(!engine.buyUpgrade("sh_storage_1"), "cannot buy the same upgrade twice while building");
    const process = findProcess(engine, "upgrade");
    engine.forceCompleteProcess(process.id);
    check(engine.state.purchasedUpgrades.has("sh_storage_1"), "upgrade marked purchased after completion");
    check(engine.state.storageSlots() === before + 5, "storage capacity increased by configured amount");
    check(engine.state.towerXp === 5, "tower XP awarded on upgrade completion");
}

// --- 3. Raid lifecycle: launch -> complete -> report -> claim ---
console.log("\n[3] Raid lifecycle");
{
    const engine = new GameEngine();
    const squad = engine.state.minions.map(m => m.id);
    const preview = previewRaid(engine.state, "t1_millbrook", squad);
    check(preview.durationHours > 0, "raid preview computes a positive duration");

    check(engine.launchRaid("t1_millbrook", squad), "raid launches with the full starting squad");
    check(engine.state.missionState("t1_millbrook") === "RUNNING", "mission is RUNNING once launched");
    check(!engine.launchRaid("t1_stonebridge", squad), "squad already on a raid cannot launch a second one");

    const raidProcess = findProcess(engine, "raid");
    engine.state.rng = () => 1; // force success, and force "no bonus loot/prisoners" for a clean base-case assertion
    engine.forceCompleteProcess(raidProcess.id);
    engine.state.rng = Math.random;

    check(engine.state.missionState("t1_millbrook") === "COMPLETED_UNCLAIMED", "mission awaits Claim after raid completes");
    const draft = engine.state.pendingReports.get("t1_millbrook")!;
    check(draft !== undefined, "a raid report draft exists");
    check(draft.success === true, "forced roll produced a successful raid");
    check(squad.every(id => engine.state.findMinion(id)!.activity.type === "raid"), "squad stays reserved until Claim");

    const goldBefore = engine.state.gold;
    const crystalsBefore = engine.state.crystals;
    check(engine.claimReport("t1_millbrook"), "Claim succeeds");
    check(engine.state.gold === goldBefore + draft.goldFound, "Claim credits exactly the discovered gold");
    check(engine.state.crystals === crystalsBefore + draft.crystalsFound, "Claim credits exactly the discovered crystals");
    check(squad.every(id => engine.state.findMinion(id)!.activity.type !== "raid"), "squad released from raid reservation after Claim");
    check(squad.every(id => engine.state.findMinion(id)!.tired), "every participant becomes Tired after a raid");
    check(engine.state.claimedMissions.has("t1_millbrook"), "mission marked as claimed");
    check(engine.state.missionState("t1_millbrook") === "COOLDOWN", "mission enters weekly cooldown after launch");
    check(!engine.state.pendingReports.has("t1_millbrook"), "pending report cleared after Claim");
}

// --- 4. Claim resolution: loot stacking, carry budget, prisoner intake (synthetic draft, RNG-independent) ---
console.log("\n[4] Claim resolution — loot stacking and prisoners");
{
    const engine = new GameEngine();
    const squad = engine.state.minions.slice(0, 1).map(m => m.id);
    const minion = engine.state.findMinion(squad[0])!;
    minion.activity = { type: "raid", missionId: "t1_millbrook" };

    const draft: RaidReportDraft = {
        missionId: "t1_millbrook", success: true, squad,
        goldFound: 42, crystalsFound: 3,
        lootCopies: ["golden_cup", "golden_cup", "golden_cup", "golden_cup", "golden_cup", "golden_cup", "cobblestone"],
        prisonersFound: [1, 3],
        carryLimit: 10,
        selection: { golden_cup: 6, cobblestone: 1 },
        injuryRolls: { [squad[0]]: 0 },
    };
    engine.state.pendingReports.set("t1_millbrook", draft);
    engine.state.missionLastLaunchWeek.set("t1_millbrook", engine.state.week);

    const cellsBefore = freeCells(engine.state);
    check(engine.claimReport("t1_millbrook"), "Claim succeeds for the synthetic draft");

    const goldenStacks = engine.state.stacks.filter(s => s.itemId === "golden_cup");
    const totalGolden = goldenStacks.reduce((n, s) => n + s.count, 0);
    check(totalGolden === 6, "all 6 selected Golden Cups were stored");
    check(goldenStacks.length === 2, "6 Golden Cups (max stack 5) split into 2 stacks, costing 2 carry slots");
    check(engine.state.stacks.some(s => s.itemId === "cobblestone" && s.count === 1), "selected Cobblestone stored");

    check(engine.state.prisoners.length === 2, "both captured prisoners admitted (cells available)");
    const ranksAdmitted = engine.state.prisoners.map(p => p.rank).sort((a, b) => b - a);
    check(ranksAdmitted[0] === 3 && ranksAdmitted[1] === 1, "prisoners sorted high-to-low rank on admission");
    check(freeCells(engine.state) === cellsBefore - 2, "dungeon cell occupancy reflects admitted prisoners");
}

// --- 5. Training Hall: unlock transition, train, recover ---
console.log("\n[5] Training Hall");
{
    const engine = new GameEngine();
    const minionId = engine.state.minions[0].id;
    check(!engine.startTraining(minionId), "training is blocked before the rank-1→2 transition is unlocked");

    engine.buyUpgrade("th_unlock_1_2");
    engine.forceCompleteProcess(findProcess(engine, "upgrade").id);
    check(engine.state.isTrainingTransitionUnlocked(1), "rank 1→2 transition is unlocked after the upgrade completes");

    check(engine.startTraining(minionId), "training starts once unlocked and affordable");
    engine.forceCompleteProcess(findProcess(engine, "training").id);
    const minion = engine.state.findMinion(minionId)!;
    check(minion.rank === 2, "minion promoted to Rank 2 after training completes");
    check(minion.tired, "a trained minion becomes Tired");
    check(minion.activity.type === "resting", "a Tired, idle minion is auto-assigned a Quarters bed");

    engine.forceCompleteProcess(findProcess(engine, "rest").id);
    check(!engine.state.findMinion(minionId)!.tired, "rest clears Tired");
    check(engine.state.findMinion(minionId)!.activity.type === "idle", "minion is idle again after resting");
}

// --- 6. Laboratory: single active research, permanent effect ---
console.log("\n[6] Laboratory");
{
    const engine = new GameEngine();
    const baseAttack = engine.state.effectiveAttack(1);
    check(engine.startResearch("res_improved_weapons"), "research starts");
    check(!engine.startResearch("res_improved_armor"), "only one research project can be active at a time");
    engine.forceCompleteProcess(findProcess(engine, "research").id);
    check(engine.state.completedResearch.has("res_improved_weapons"), "research marked complete");
    check(engine.state.effectiveAttack(1) === baseAttack + 3, "completed research permanently boosts effective Attack");
    check(engine.state.activeResearchId === null, "no active research after completion, ready for the next");
}

// --- 7. Dungeon: admit, auto-process, recruit outcome ---
console.log("\n[7] Dungeon");
{
    const engine = new GameEngine();
    const popBefore = engine.state.minions.length;
    check(admitPrisoner(engine.state, 2), "prisoner admitted into a free cell");
    assignCells(engine.state, engine.state.hours);
    const process = findProcess(engine, "dungeon");

    engine.state.rng = () => 0; // forces the recruit branch (lowest-probability-ordered outcome)
    engine.forceCompleteProcess(process.id);
    engine.state.rng = Math.random;

    check(engine.state.minions.length === popBefore + 1, "a recruited prisoner joins the army");
    check(engine.state.prisoners.length === 0, "the cell is freed once processing completes");
}

// --- 8. Storehouse sales ---
console.log("\n[8] Storehouse sales");
{
    const engine = new GameEngine();
    engine.state.addItems("cobblestone", 10);
    const stack = engine.state.stacks.find(s => s.itemId === "cobblestone")!;
    const goldBefore = engine.state.gold;

    check(engine.startNormalSale(stack.id), "normal sale starts and reserves the stack");
    check(engine.state.stacks.length === 0, "stack removed from inventory while selling");
    const saleProcess = findProcess(engine, "sale");
    engine.forceCompleteProcess(saleProcess.id);
    check(engine.state.gold === goldBefore + 10 * 1, "normal sale pays full value (10 Cobblestone * 1g)");

    engine.state.addItems("iron_ore", 4);
    const stack2 = engine.state.stacks.find(s => s.itemId === "iron_ore")!;
    const goldBefore2 = engine.state.gold;
    check(engine.instantSale(stack2.id), "instant sale completes immediately");
    check(engine.state.gold === goldBefore2 + Math.floor(4 * 3 * 0.5), "instant sale pays half value");
}

// --- 9. Victory condition ---
console.log("\n[9] Victory condition");
{
    const engine = new GameEngine();
    for (const mission of CONFIG.missions) engine.state.claimedMissions.add(mission.id);
    engine.state.claimedMissions.delete(CONFIG.missions[CONFIG.missions.length - 1].id);
    check(!engine.state.victory, "victory not yet triggered with one mission unclaimed");

    const lastMission = CONFIG.missions[CONFIG.missions.length - 1];
    engine.state.unlockedTiers.add(lastMission.tier);
    const draft: RaidReportDraft = {
        missionId: lastMission.id, success: true, squad: [],
        goldFound: 0, crystalsFound: 0, lootCopies: [], prisonersFound: [],
        carryLimit: 0, selection: {}, injuryRolls: {},
    };
    engine.state.pendingReports.set(lastMission.id, draft);
    engine.claimReport(lastMission.id);
    check(engine.state.victory, "claiming the last unclaimed mission triggers Victory");
}

// --- 10. Payroll and bankruptcy ---
console.log("\n[10] Payroll and bankruptcy");
{
    const engine = new GameEngine();
    engine.setSpeed(420);
    engine.tick(1); // 0.4 * 420 * 1 = 168 game hours = exactly 7 days
    check(engine.state.paydayPending, "payday becomes due after 7 in-game days");
    check(engine.payPayroll(), "payroll is paid while affordable");
    check(!engine.state.paydayPending, "payday clears once paid");
    check(!engine.state.gameOver, "no bankruptcy when payroll is paid in time");

    const broke = new GameEngine();
    broke.state.gold = 0;
    broke.setSpeed(60);
    broke.tick(7); // 0.4 * 60 * 7 = 168h (7 days) -> payday due, deadline +24h
    check(broke.state.paydayPending, "bankrupt-bound game reaches payday due");
    broke.tick(1); // +24h -> deadline passes unpaid
    check(broke.state.gameOver === "bankruptcy", "unpaid payroll at the deadline ends the game in bankruptcy");
}

console.log(`\n== All ${passed} checks passed ==`);
