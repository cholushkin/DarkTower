export type MinionActivityType = "idle" | "raid" | "training" | "resting" | "treatment";

export interface MinionActivity {
    type: MinionActivityType;
    processId?: string;
    missionId?: string;
}

export interface Minion {
    id: string;
    rank: number;
    tired: boolean;
    injured: boolean;
    activity: MinionActivity;
    /** Game-hour timestamp since this minion became free + (Tired or Injured) without a bed. */
    waitingSince: number | null;
}

export type PrisonerOutcome = "recruit" | "interrogate" | "death";

export interface Prisoner {
    id: string;
    rank: number;
    processId: string | null;
}

export interface ItemStack {
    id: string;
    itemId: string;
    count: number;
}

interface BaseProcess {
    id: string;
    startHour: number;
    endHour: number;
}

export interface RaidProcess extends BaseProcess {
    type: "raid";
    missionId: string;
    squad: string[];
}

export interface RestProcess extends BaseProcess {
    type: "rest";
    minionId: string;
}

export interface TreatmentProcess extends BaseProcess {
    type: "treatment";
    minionId: string;
}

export interface TrainingProcess extends BaseProcess {
    type: "training";
    minionId: string;
    fromRank: number;
    toRank: number;
}

export interface SaleProcess extends BaseProcess {
    type: "sale";
    stackId: string;
    itemId: string;
    count: number;
    goldValue: number;
}

export interface ResearchProcess extends BaseProcess {
    type: "research";
    researchId: string;
}

export interface UpgradeProcess extends BaseProcess {
    type: "upgrade";
    upgradeId: string;
}

export interface DungeonProcess extends BaseProcess {
    type: "dungeon";
    prisonerId: string;
    rank: number;
}

export type Process =
    | RaidProcess
    | RestProcess
    | TreatmentProcess
    | TrainingProcess
    | SaleProcess
    | ResearchProcess
    | UpgradeProcess
    | DungeonProcess;

export type MissionState =
    | "LOCKED"
    | "AVAILABLE"
    | "RUNNING"
    | "COMPLETED_UNCLAIMED"
    | "COOLDOWN";

export interface SelectedLootEntry {
    itemId: string;
    count: number;
}

export interface RaidReportDraft {
    missionId: string;
    success: boolean;
    squad: string[];
    goldFound: number;
    crystalsFound: number;
    /** One entry per discovered physical item copy, per §6.1. */
    lootCopies: string[];
    prisonersFound: number[];
    carryLimit: number;
    /** Player's current selection, itemId -> copies selected. Preselected to a valid subset. */
    selection: Record<string, number>;
    injuryRolls: Record<string, number>;
}
