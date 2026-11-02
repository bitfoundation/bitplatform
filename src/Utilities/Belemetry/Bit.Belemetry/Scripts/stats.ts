// What the script dropped and why, and what became of its requests, since the page loaded - `getStats()`, the first
// thing to look at in the browser console when telemetry does not arrive. The reasons match the C# self-telemetry's
// `belemetry.reason` values where both sides drop for the same reason.

const dropped: Record<string, number> = {};
const requests: Record<string, number> = {};

/** Counts records dropped before export: queue_full, pending_full, not_consented, consent_revoked, error_limit, sampled_out, filtered. */
export function countDropped(reason: string, count = 1): void {
    if (count > 0) dropped[reason] = (dropped[reason] || 0) + count;
}

/**
 * Counts a request's outcome: delivered, rejected (refused for good), kept (stored for later), dropped (lost), withdrawn
 * (a feature of its records was revoked after it was built: neither sent again nor stored).
 */
export function countRequest(outcome: string): void {
    requests[outcome] = (requests[outcome] || 0) + 1;
}

export interface StatsSnapshot {
    dropped: Record<string, number>;
    requests: Record<string, number>;
}

export function statsSnapshot(): StatsSnapshot {
    return { dropped: { ...dropped }, requests: { ...requests } };
}

/** Clears the counters (tests only). */
export function resetStats(): void {
    for (const key of Object.keys(dropped)) delete dropped[key];
    for (const key of Object.keys(requests)) delete requests[key];
}
