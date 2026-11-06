export function escapeHtml(s: string): string {
    return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

export function pct(v: number): string {
    return `${Math.round(v * 100)}%`;
}

export function fmtHours(h: number): string {
    if (h < 1) return `${Math.round(h * 60)}m`;
    return `${h.toFixed(1)}h`;
}

export function conditionBadges(tired: boolean, injured: boolean): string {
    const badges: string[] = [];
    if (injured) badges.push(`<span class="badge badge-injured">Injured</span>`);
    if (tired) badges.push(`<span class="badge badge-tired">Tired</span>`);
    if (!tired && !injured) badges.push(`<span class="badge badge-ok">Ready</span>`);
    return badges.join(" ");
}

export function activityLabel(type: string): string {
    switch (type) {
        case "idle": return "Idle";
        case "raid": return "On Raid";
        case "training": return "Training";
        case "resting": return "Resting";
        case "treatment": return "In Treatment";
        default: return type;
    }
}
