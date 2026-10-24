// Error-flood protection and the value length limit. Mirrors BelemetryErrorLimiter, BelemetryRateLimiter and
// BelemetryValueLimit in C#; keep both in step.

import type { ErrorsConfig } from './config';
import { isSpan, type Attributes, type LogRecord, type TelemetryRecord } from './model';

// Enough distinct errors for any real app; past it the oldest signatures are forgotten (worst case, a duplicate gets
// through - the per-minute cap still holds).
const MAX_SIGNATURES = 128;

const monotonic = () => performance.now();

/** A token bucket: a burst of `capacity`, then `capacity` per `periodMs` on average. */
export class RateLimiter {
    private tokens: number;
    private last: number | null = null;

    constructor(private capacity: number, private periodMs: number, private clock: () => number = monotonic) {
        this.capacity = Math.max(1, capacity);
        this.tokens = this.capacity;
    }

    /** Takes up to `count` tokens and returns how many it got. */
    take(count: number): number {
        if (!(count > 0)) return 0;
        const now = this.clock();
        if (this.last !== null && now > this.last) this.tokens = Math.min(this.capacity, this.tokens + (now - this.last) * this.capacity / Math.max(1, this.periodMs));
        this.last = now;
        const granted = Math.min(count, Math.floor(this.tokens));
        this.tokens -= granted;
        return granted;
    }
}

/**
 * The scope, the exception type and message (or the body), and the first two lines of the stack trace - the message
 * line and the first frame in Chrome and .NET, the first two frames in Firefox and Safari.
 */
export function errorSignature(log: LogRecord): string {
    const attributes = log.attributes;
    const stack = attributes['exception.stacktrace'];
    let head = typeof stack === 'string' ? stack : '';
    const first = head.indexOf('\n');
    const second = first < 0 ? -1 : head.indexOf('\n', first + 1);
    if (second >= 0) head = head.slice(0, second);
    const type = attributes['exception.type'];
    const message = attributes['exception.message'];
    return [log.scope, typeof type === 'string' ? type : '', typeof message === 'string' ? message : log.body, head.trimEnd()].join('\u0001');
}

/** Suppresses an error identical to one recorded within the duplicate window, then caps the rest per minute. */
export class ErrorLimiter {
    private recent = new Map<string, number>();
    private rate: RateLimiter | null;

    constructor(private config: ErrorsConfig, private clock: () => number = monotonic) {
        this.rate = config.maxPerMinute > 0 ? new RateLimiter(config.maxPerMinute, 60000, clock) : null;
    }

    allow(log: LogRecord): boolean {
        if (!(this.config.duplicateWindowMs > 0)) return !this.rate || this.rate.take(1) === 1;
        const key = errorSignature(log);
        const now = this.clock();
        const recorded = this.recent.get(key);
        if (recorded !== undefined && now - recorded < this.config.duplicateWindowMs) return false;
        // Only a recorded error opens a window: one the per-minute cap dropped must get through once tokens are back.
        if (this.rate && this.rate.take(1) === 0) return false;
        if (recorded === undefined && this.recent.size >= MAX_SIGNATURES) {
            const oldest = this.recent.keys().next();
            if (!oldest.done) this.recent.delete(oldest.value);
        }
        this.recent.set(key, now);
        return true;
    }
}

/** The first `max` characters, one fewer when the cut would split a surrogate pair. */
export function truncate(value: string, max: number): string {
    if (!(max > 0) || value.length <= max) return value;
    const code = value.charCodeAt(max - 1);
    return value.slice(0, code >= 0xd800 && code <= 0xdbff ? max - 1 : max);
}

function limitAttributes(attributes: Attributes, max: number): void {
    for (const key of Object.keys(attributes)) {
        const value = attributes[key];
        if (typeof value === 'string') {
            if (value.length > max) attributes[key] = truncate(value, max);
        } else if (Array.isArray(value) && typeof value[0] === 'string') {
            attributes[key] = (value as string[]).map(v => truncate(v, max));
        }
    }
}

/** Cuts the long strings of a record in place: attributes, the body or status message, span event attributes. */
export function limitRecord(record: TelemetryRecord, max: number): void {
    if (!(max > 0)) return;
    limitAttributes(record.attributes, max);
    if (isSpan(record)) {
        if (record.status.message) record.status.message = truncate(record.status.message, max);
        for (const event of record.events) limitAttributes(event.attributes, max);
    } else if (record.body) {
        record.body = truncate(record.body, max);
    }
}
