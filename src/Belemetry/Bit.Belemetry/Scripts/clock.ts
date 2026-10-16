// All record times are epoch milliseconds derived from the monotonic clock, so spans measured with
// performance.now() and entries from the Performance Timeline share one time base.

export function timeOrigin(): number {
    const p = performance as Performance & { timing?: { navigationStart: number } };
    return p.timeOrigin || (p.timing && p.timing.navigationStart) || Date.now() - p.now();
}

/** The current time in epoch milliseconds (fractional). */
export function now(): number {
    return timeOrigin() + performance.now();
}

/** Converts a DOMHighResTimeStamp (relative to the time origin, e.g. a PerformanceEntry time) to epoch milliseconds. */
export function toEpoch(highResTime: number): number {
    return timeOrigin() + highResTime;
}
