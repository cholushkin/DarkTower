import type { GameConfig } from "./types";

// Single source of truth for all tunable prototype data. Mirrors the "Configuration Catalog"
// role described in DarkTowerGameDesign.md §9 — static rules/values live here, runtime state
// lives in GameState. Values are placeholder balancing, not final numbers (per §10).

export const CONFIG: GameConfig = {
    ranks: [
        { rank: 1, name: "Rookie", attack: 5, defense: 5, salaryGoldPerWeek: 10, carryCapacity: 1.0 },
        { rank: 2, name: "Grunt", attack: 8, defense: 7, salaryGoldPerWeek: 18, carryCapacity: 1.2 },
        { rank: 3, name: "Raider", attack: 12, defense: 10, salaryGoldPerWeek: 30, carryCapacity: 1.5 },
        { rank: 4, name: "Veteran", attack: 17, defense: 14, salaryGoldPerWeek: 48, carryCapacity: 1.8 },
        { rank: 5, name: "Captain", attack: 23, defense: 19, salaryGoldPerWeek: 70, carryCapacity: 2.2 },
    ],

    items: [
        { id: "golden_cup", name: "Golden Cup", tags: ["treasure"], maxStackSize: 5, baseValueGold: 40 },
        { id: "ancient_crown", name: "Ancient Crown", tags: ["treasure"], maxStackSize: 1, baseValueGold: 150 },
        { id: "silver_necklace", name: "Silver Necklace", tags: ["treasure"], maxStackSize: 5, baseValueGold: 25 },
        { id: "enchanted_gem", name: "Enchanted Gem", tags: ["treasure"], maxStackSize: 3, baseValueGold: 80 },
        { id: "cow", name: "Cow", tags: [], maxStackSize: 1, baseValueGold: 15 },
        { id: "cobblestone", name: "Cobblestone", tags: [], maxStackSize: 30, baseValueGold: 1 },
        { id: "iron_ore", name: "Iron Ore", tags: [], maxStackSize: 20, baseValueGold: 3 },
        { id: "wheat_sack", name: "Wheat Sack", tags: [], maxStackSize: 15, baseValueGold: 2 },
        { id: "leather_hide", name: "Leather Hide", tags: [], maxStackSize: 10, baseValueGold: 4 },
    ],

    worldTiers: [
        { tier: 1, name: "Outer Villages" },
        { tier: 2, name: "Borderlands" },
    ],

    missions: [
        {
            id: "t1_millbrook", tier: 1, name: "Millbrook Village",
            requiredPower: 18, baseDurationHours: 3, baseInjuryChance: 0.05,
            rewardGoldRange: [20, 50], rewardCrystalsRange: [0, 2],
            loot: [
                { itemId: "golden_cup", chance: 0.5, min: 1, max: 2 },
                { itemId: "cobblestone", chance: 0.9, min: 3, max: 8 },
                { itemId: "cow", chance: 0.3, min: 1, max: 1 },
            ],
            prisonerChance: 0.4, prisonerCountRange: [0, 2], prisonerRankRange: [1, 1],
        },
        {
            id: "t1_stonebridge", tier: 1, name: "Stonebridge Hamlet",
            requiredPower: 25, baseDurationHours: 4, baseInjuryChance: 0.07,
            rewardGoldRange: [30, 70], rewardCrystalsRange: [0, 3],
            loot: [
                { itemId: "iron_ore", chance: 0.8, min: 2, max: 6 },
                { itemId: "silver_necklace", chance: 0.4, min: 1, max: 2 },
                { itemId: "wheat_sack", chance: 0.6, min: 2, max: 5 },
            ],
            prisonerChance: 0.45, prisonerCountRange: [0, 2], prisonerRankRange: [1, 2],
        },
        {
            id: "t1_fallowcreek", tier: 1, name: "Fallow Creek Farms",
            requiredPower: 22, baseDurationHours: 3.5, baseInjuryChance: 0.06,
            rewardGoldRange: [25, 60], rewardCrystalsRange: [0, 2],
            loot: [
                { itemId: "wheat_sack", chance: 0.9, min: 3, max: 8 },
                { itemId: "cow", chance: 0.5, min: 1, max: 2 },
                { itemId: "leather_hide", chance: 0.5, min: 1, max: 3 },
            ],
            prisonerChance: 0.4, prisonerCountRange: [0, 1], prisonerRankRange: [1, 1],
        },
        {
            id: "t1_oakhollow", tier: 1, name: "Oakhollow Outpost",
            requiredPower: 32, baseDurationHours: 5, baseInjuryChance: 0.09,
            rewardGoldRange: [40, 90], rewardCrystalsRange: [1, 4],
            loot: [
                { itemId: "enchanted_gem", chance: 0.15, min: 1, max: 1 },
                { itemId: "golden_cup", chance: 0.4, min: 1, max: 2 },
                { itemId: "leather_hide", chance: 0.6, min: 2, max: 4 },
            ],
            prisonerChance: 0.5, prisonerCountRange: [0, 2], prisonerRankRange: [1, 2],
        },
        {
            id: "t2_ravenswood", tier: 2, name: "Ravenswood Keep",
            requiredPower: 55, baseDurationHours: 8, baseInjuryChance: 0.12,
            rewardGoldRange: [80, 160], rewardCrystalsRange: [3, 8],
            loot: [
                { itemId: "ancient_crown", chance: 0.2, min: 1, max: 1 },
                { itemId: "enchanted_gem", chance: 0.3, min: 1, max: 2 },
                { itemId: "silver_necklace", chance: 0.6, min: 2, max: 4 },
            ],
            prisonerChance: 0.5, prisonerCountRange: [0, 3], prisonerRankRange: [2, 3],
        },
        {
            id: "t2_thornwall", tier: 2, name: "Thornwall Garrison",
            requiredPower: 68, baseDurationHours: 10, baseInjuryChance: 0.15,
            rewardGoldRange: [100, 200], rewardCrystalsRange: [4, 10],
            loot: [
                { itemId: "iron_ore", chance: 0.9, min: 4, max: 10 },
                { itemId: "golden_cup", chance: 0.5, min: 1, max: 3 },
                { itemId: "enchanted_gem", chance: 0.25, min: 1, max: 1 },
            ],
            prisonerChance: 0.55, prisonerCountRange: [0, 3], prisonerRankRange: [2, 4],
        },
        {
            id: "t2_blackmire", tier: 2, name: "Blackmire Citadel",
            requiredPower: 85, baseDurationHours: 14, baseInjuryChance: 0.18,
            rewardGoldRange: [150, 300], rewardCrystalsRange: [6, 15],
            loot: [
                { itemId: "ancient_crown", chance: 0.3, min: 1, max: 2 },
                { itemId: "enchanted_gem", chance: 0.4, min: 2, max: 3 },
                { itemId: "cobblestone", chance: 0.9, min: 5, max: 15 },
            ],
            prisonerChance: 0.6, prisonerCountRange: [1, 4], prisonerRankRange: [3, 5],
        },
    ],

    upgrades: [
        // Storehouse
        { id: "sh_storage_1", room: "storehouse", name: "Storage Shelves I", description: "Add 5 storage slots.", costGold: 40, durationHours: 1, requires: [], effect: { type: "storageSlots", amount: 5 }, towerXp: 5 },
        { id: "sh_storage_2", room: "storehouse", name: "Storage Shelves II", description: "Add 8 more storage slots.", costGold: 120, durationHours: 3, requires: ["sh_storage_1"], effect: { type: "storageSlots", amount: 8 }, towerXp: 8 },
        { id: "sh_storage_3", room: "storehouse", name: "Storage Shelves III", description: "Add 12 more storage slots.", costGold: 300, durationHours: 6, requires: ["sh_storage_2"], effect: { type: "storageSlots", amount: 12 }, towerXp: 12 },
        { id: "sh_sale_eff_1", room: "storehouse", name: "Sale Efficiency I", description: "Normal sales take 25% less time.", costGold: 60, durationHours: 2, requires: [], effect: { type: "saleEfficiency", multiplier: 0.75 }, towerXp: 5 },
        { id: "sh_sale_eff_2", room: "storehouse", name: "Sale Efficiency II", description: "Normal sales take another 20% less time.", costGold: 150, durationHours: 4, requires: ["sh_sale_eff_1"], effect: { type: "saleEfficiency", multiplier: 0.8 }, towerXp: 8 },

        // Quarters
        { id: "qt_bed_1", room: "quarters", name: "Extra Bed I", description: "Add one Quarters bed.", costGold: 30, durationHours: 1, requires: [], effect: { type: "bed" }, towerXp: 3 },
        { id: "qt_bed_2", room: "quarters", name: "Extra Bed II", description: "Add one more Quarters bed.", costGold: 70, durationHours: 2, requires: ["qt_bed_1"], effect: { type: "bed" }, towerXp: 5 },
        { id: "qt_bed_3", room: "quarters", name: "Extra Bed III", description: "Add one more Quarters bed.", costGold: 150, durationHours: 4, requires: ["qt_bed_2"], effect: { type: "bed" }, towerXp: 8 },

        // Infirmary
        { id: "inf_bed_1", room: "infirmary", name: "Treatment Bed I", description: "Add one treatment bed.", costGold: 40, durationHours: 1, requires: [], effect: { type: "treatmentBed" }, towerXp: 3 },
        { id: "inf_bed_2", room: "infirmary", name: "Treatment Bed II", description: "Add one more treatment bed.", costGold: 90, durationHours: 2, requires: ["inf_bed_1"], effect: { type: "treatmentBed" }, towerXp: 5 },
        { id: "inf_eff_1", room: "infirmary", name: "Treatment Efficiency I", description: "Treatment takes 25% less time.", costGold: 100, durationHours: 3, requires: [], effect: { type: "treatmentEfficiency", multiplier: 0.75 }, towerXp: 6 },
        { id: "inf_eff_2", room: "infirmary", name: "Treatment Efficiency II", description: "Treatment takes another 27% less time.", costGold: 220, durationHours: 5, requires: ["inf_eff_1"], effect: { type: "treatmentEfficiency", multiplier: 0.73 }, towerXp: 9 },

        // Training Hall
        { id: "th_station_1", room: "trainingHall", name: "Training Station I", description: "Add one training station.", costGold: 50, durationHours: 1, requires: [], effect: { type: "trainingStation" }, towerXp: 4 },
        { id: "th_station_2", room: "trainingHall", name: "Training Station II", description: "Add one more training station.", costGold: 120, durationHours: 3, requires: ["th_station_1"], effect: { type: "trainingStation" }, towerXp: 6 },
        { id: "th_unlock_1_2", room: "trainingHall", name: "Advanced Training: Rookie→Grunt", description: "Unlocks training Rank 1 to Rank 2.", costGold: 80, durationHours: 2, requires: [], effect: { type: "trainingUnlock", fromRank: 1 }, towerXp: 6 },
        { id: "th_unlock_2_3", room: "trainingHall", name: "Advanced Training: Grunt→Raider", description: "Unlocks training Rank 2 to Rank 3.", costGold: 200, durationHours: 4, requires: ["th_unlock_1_2"], effect: { type: "trainingUnlock", fromRank: 2 }, towerXp: 9 },
        { id: "th_unlock_3_4", room: "trainingHall", name: "Advanced Training: Raider→Veteran", description: "Unlocks training Rank 3 to Rank 4.", costGold: 450, durationHours: 7, requires: ["th_unlock_2_3"], effect: { type: "trainingUnlock", fromRank: 3 }, towerXp: 12 },
        { id: "th_unlock_4_5", room: "trainingHall", name: "Advanced Training: Veteran→Captain", description: "Unlocks training Rank 4 to Rank 5.", costGold: 900, durationHours: 12, requires: ["th_unlock_3_4"], effect: { type: "trainingUnlock", fromRank: 4 }, towerXp: 16 },

        // Laboratory
        { id: "lab_tier_1", room: "laboratory", name: "Research Tier Unlock I", description: "Unlocks Tier 2 research projects.", costGold: 200, durationHours: 5, requires: [], effect: { type: "researchTier", tier: 2 }, towerXp: 10 },
        { id: "lab_tier_2", room: "laboratory", name: "Research Tier Unlock II", description: "Unlocks Tier 3 research projects.", costGold: 500, durationHours: 9, requires: ["lab_tier_1"], effect: { type: "researchTier", tier: 3 }, towerXp: 14 },
        { id: "lab_eff_1", room: "laboratory", name: "Research Efficiency I", description: "Research takes 20% less time.", costGold: 150, durationHours: 4, requires: [], effect: { type: "researchEfficiency", multiplier: 0.8 }, towerXp: 8 },
        { id: "lab_eff_2", room: "laboratory", name: "Research Efficiency II", description: "Research takes another 25% less time.", costGold: 350, durationHours: 7, requires: ["lab_eff_1"], effect: { type: "researchEfficiency", multiplier: 0.75 }, towerXp: 12 },

        // Dungeon
        { id: "dg_cell_1", room: "dungeon", name: "Extra Cell I", description: "Add one prisoner cell.", costGold: 50, durationHours: 1, requires: [], effect: { type: "dungeonCell" }, towerXp: 4 },
        { id: "dg_cell_2", room: "dungeon", name: "Extra Cell II", description: "Add one more prisoner cell.", costGold: 120, durationHours: 3, requires: ["dg_cell_1"], effect: { type: "dungeonCell" }, towerXp: 6 },
        { id: "dg_torture_eff_1", room: "dungeon", name: "Torture Efficiency I", description: "Processing takes 20% less time.", costGold: 100, durationHours: 3, requires: [], effect: { type: "tortureEfficiency", multiplier: 0.8 }, towerXp: 6 },
        { id: "dg_torture_eff_2", room: "dungeon", name: "Torture Efficiency II", description: "Processing takes another 25% less time.", costGold: 250, durationHours: 6, requires: ["dg_torture_eff_1"], effect: { type: "tortureEfficiency", multiplier: 0.75 }, towerXp: 9 },
        { id: "dg_recruit_eff_1", room: "dungeon", name: "Recruitment Efficiency I", description: "Increases recruitment chance.", costGold: 120, durationHours: 3, requires: [], effect: { type: "recruitmentEfficiency", bonus: 0.1 }, towerXp: 7 },
        { id: "dg_recruit_eff_2", room: "dungeon", name: "Recruitment Efficiency II", description: "Increases recruitment chance further.", costGold: 300, durationHours: 6, requires: ["dg_recruit_eff_1"], effect: { type: "recruitmentEfficiency", bonus: 0.15 }, towerXp: 10 },

        // War Room
        { id: "wr_enemy_intel_1", room: "warRoom", name: "Enemy Intelligence I", description: "Narrower estimated enemy-strength range.", costGold: 100, durationHours: 3, requires: [], effect: { type: "enemyIntel", level: 1 }, towerXp: 6 },
        { id: "wr_enemy_intel_2", room: "warRoom", name: "Enemy Intelligence II", description: "Even narrower estimated enemy-strength range.", costGold: 250, durationHours: 6, requires: ["wr_enemy_intel_1"], effect: { type: "enemyIntel", level: 2 }, towerXp: 9 },
        { id: "wr_loot_intel_1", room: "warRoom", name: "Loot Intelligence I", description: "Reveals possible loot types.", costGold: 100, durationHours: 3, requires: [], effect: { type: "lootIntel", level: 1 }, towerXp: 6 },
        { id: "wr_loot_intel_2", room: "warRoom", name: "Loot Intelligence II", description: "Reveals estimated drop chances.", costGold: 250, durationHours: 6, requires: ["wr_loot_intel_1"], effect: { type: "lootIntel", level: 2 }, towerXp: 9 },
    ],

    research: [
        { id: "res_improved_weapons", name: "Improved Weapons", description: "+3 Attack for all minions.", tier: 1, costGold: 150, durationHours: 6, requires: [], effects: [{ stat: "attack", mode: "add", value: 3 }], towerXp: 10 },
        { id: "res_improved_armor", name: "Improved Armor", description: "+3 Defense for all minions.", tier: 1, costGold: 150, durationHours: 6, requires: [], effects: [{ stat: "defense", mode: "add", value: 3 }], towerXp: 10 },
        { id: "res_fury_spell", name: "Fury Spell", description: "+15% Attack for all minions.", tier: 2, costGold: 400, durationHours: 10, requires: ["res_improved_weapons"], effects: [{ stat: "attack", mode: "mult", value: 1.15 }], towerXp: 16 },
    ],

    trainingTransitions: [
        { fromRank: 1, toRank: 2, costGold: 50, durationHours: 4 },
        { fromRank: 2, toRank: 3, costGold: 120, durationHours: 8 },
        { fromRank: 3, toRank: 4, costGold: 260, durationHours: 14 },
        { fromRank: 4, toRank: 5, costGold: 500, durationHours: 22 },
    ],

    towerXpThresholds: [0, 30, 70, 120, 190, 280, 400, 550, 750, 1000, 1300],

    startingBalance: {
        gold: 150,
        crystals: 5,
        minionRanks: [1, 1, 1],
        storageSlots: 20,
        beds: 3,
        treatmentBeds: 2,
        trainingStations: 1,
        dungeonCells: 2,
    },

    constants: {
        dayLengthRealSeconds: 60,
        weekDays: 7,
        restDurationHours: 6,
        treatmentDurationHours: 72,
        bedEvictionHours: 48,
        paydayWindowHours: 24,
        instantSaleFraction: 0.5,
        saleBaseHours: 1,
        saleHoursPerCopy: 0.5,
        dungeonBaseDurationHours: 24,
        dungeonOutcomeBase: { recruit: 0.6, interrogate: 0.15, death: 0.25 },
        recruitedInjuredChance: 0.5,
    },
};
