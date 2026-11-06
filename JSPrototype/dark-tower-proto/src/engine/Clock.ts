import { CONFIG } from "../data/config";

/** 1 real minute = 1 in-game day (§2), scaled further by the debug speed multiplier. */
export function realSecondsToGameHours(realSeconds: number, speed: number): number {
    const hoursPerRealSecond = 24 / CONFIG.constants.dayLengthRealSeconds;
    return realSeconds * hoursPerRealSecond * speed;
}

export function dayOfHour(hours: number): number {
    return Math.floor(hours / 24);
}

export function weekOfHour(hours: number): number {
    return Math.floor(dayOfHour(hours) / CONFIG.constants.weekDays);
}

export function isNightHour(hours: number): boolean {
    return hours % 24 < 1;
}

export function formatClock(hours: number): string {
    const day = dayOfHour(hours) + 1;
    const hourOfDay = Math.floor(hours % 24);
    const night = hourOfDay < 6 || hourOfDay >= 20;
    return `Day ${day}, ${hourOfDay.toString().padStart(2, "0")}:00 ${night ? "🌙" : "☀️"}`;
}
