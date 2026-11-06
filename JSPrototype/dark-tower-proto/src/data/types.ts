export type RoomId =
    | "storehouse"
    | "quarters"
    | "infirmary"
    | "trainingHall"
    | "laboratory"
    | "dungeon"
    | "warRoom";

export interface RankDef {
    rank: number;
    name: string;
    attack: number;
    defense: number;
    salaryGoldPerWeek: number;
    carryCapacity: number;
}

export interface ItemDef {
    id: string;
    name: string;
    tags: string[];
    maxStackSize: number;
    baseValueGold: number;
}

export interface TrainingTransitionDef {
    fromRank: number;
    toRank: number;
    costGold: number;
    durationHours: number;
}

export type UpgradeEffect =
    | { type: "storageSlots"; amount: number }
    | { type: "saleEfficiency"; multiplier: number }
    | { type: "bed" }
    | { type: "treatmentBed" }
    | { type: "treatmentEfficiency"; multiplier: number }
    | { type: "trainingStation" }
    | { type: "trainingUnlock"; fromRank: number }
    | { type: "researchTier"; tier: number }
    | { type: "researchEfficiency"; multiplier: number }
    | { type: "dungeonCell" }
    | { type: "tortureEfficiency"; multiplier: number }
    | { type: "recruitmentEfficiency"; bonus: number }
    | { type: "enemyIntel"; level: number }
    | { type: "lootIntel"; level: number };

export interface UpgradeDef {
    id: string;
    room: RoomId;
    name: string;
    description: string;
    costGold: number;
    costCrystals?: number;
    durationHours: number;
    requires: string[];
    effect: UpgradeEffect;
    towerXp: number;
}

export interface ResearchEffect {
    stat: "attack" | "defense";
    mode: "add" | "mult";
    value: number;
}

export interface ResearchDef {
    id: string;
    name: string;
    description: string;
    tier: number;
    costGold: number;
    durationHours: number;
    requires: string[];
    effects: ResearchEffect[];
    towerXp: number;
}

export interface MissionLootDef {
    itemId: string;
    chance: number;
    min: number;
    max: number;
}

export interface MissionDef {
    id: string;
    tier: number;
    name: string;
    requiredPower: number;
    baseDurationHours: number;
    baseInjuryChance: number;
    rewardGoldRange: [number, number];
    rewardCrystalsRange: [number, number];
    loot: MissionLootDef[];
    prisonerChance: number;
    prisonerCountRange: [number, number];
    prisonerRankRange: [number, number];
}

export interface WorldTierDef {
    tier: number;
    name: string;
}

export interface StartingBalance {
    gold: number;
    crystals: number;
    minionRanks: number[];
    storageSlots: number;
    beds: number;
    treatmentBeds: number;
    trainingStations: number;
    dungeonCells: number;
}

export interface StartingItemDef {
    itemId: string;
    count: number;
}

export interface Constants {
    dayLengthRealSeconds: number;
    weekDays: number;
    restDurationHours: number;
    treatmentDurationHours: number;
    bedEvictionHours: number;
    paydayWindowHours: number;
    instantSaleFraction: number;
    saleBaseHours: number;
    saleHoursPerCopy: number;
    dungeonBaseDurationHours: number;
    dungeonOutcomeBase: { recruit: number; interrogate: number; death: number };
    recruitedInjuredChance: number;
}

export interface GameConfig {
    ranks: RankDef[];
    items: ItemDef[];
    missions: MissionDef[];
    worldTiers: WorldTierDef[];
    upgrades: UpgradeDef[];
    research: ResearchDef[];
    trainingTransitions: TrainingTransitionDef[];
    towerXpThresholds: number[];
    startingBalance: StartingBalance;
    startingItems: StartingItemDef[];
    constants: Constants;
}
