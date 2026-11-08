// The browser half of production diagnostics: a bounded timeline of what the page did (page views, fetch/XHR, JS
// errors, interactions, long tasks, reconnection, visibility), read by .NET when it makes a capture
// (`getDiagnostics`, `getDiagnosticsStream`). It holds what the pipeline accepted - after consent, the error limits and
// privacy - and only metadata of it: the shape of every event is a wire contract with C# BelemetryBrowserEvidence.

import { now } from './clock';
import type { DiagnosticsConfig } from './config';
import { Features } from './features';
import { debug } from './log';
import { isSpan, type Attributes, type TelemetryRecord } from './model';
import { templatePath } from './routes';
import { readItem, removeItem, StorageKeys, writeItem } from './storage';

export { templatePath };

/** The category names of C# BelemetryDiagnosticCategories, with their bits (the config's `categories` mask). */
export const Category = {
    navigation: 1,
    component: 2,
    exception: 4,
    http: 8,
    'js-interop': 16,
    browser: 32,
    circuit: 64,
    log: 128,
    trace: 256,
    custom: 512,
} as const;

export type CategoryName = keyof typeof Category;

/** One event of the browser timeline (read by C# BelemetryBrowserEvidence). */
export interface DiagnosticEvent {
    seq: number;
    /** Milliseconds since the Unix epoch. */
    time: number;
    /** Milliseconds since the page's time origin (monotonic). */
    elapsed: number;
    cat: CategoryName;
    name: string;
    sev: number;
    outcome?: 'success' | 'failure' | 'cancelled';
    traceId?: string;
    spanId?: string;
    parentSpanId?: string;
    /** A path template: ids replaced (`/orders/{n}`), never a raw path. */
    route?: string;
    dur?: number;
    feature: number;
    msg?: string;
    exc?: { type: string; message: string; stack?: string };
    attrs?: Record<string, string | number | boolean>;
    /** Recorded before the page last reloaded. */
    prev?: boolean;
}

/** `getDiagnostics()` result. */
export interface DiagnosticsSnapshot {
    v: 1;
    capacity: number;
    dropped: Record<string, number>;
    resource: Attributes;
    events: DiagnosticEvent[];
}

interface Entry {
    seq: number;
    elapsed: number;
    /** A record, converted when the timeline is read: the app's processors may still change or drop it until then. */
    record?: TelemetryRecord;
    event?: DiagnosticEvent;
}

const MAX_PERSISTED_EVENTS = 100;
const MAX_PERSISTED_CHARS = 256 * 1024;
const RESOURCE_KEYS = ['browser.brands', 'browser.platform', 'browser.mobile', 'browser.language'];

function perfNow(): number {
    return typeof performance !== 'undefined' && performance.now ? performance.now() : 0;
}

/** A URL as the timeline keeps it: origin, templated path, the allowlisted query parameters only, no fragment. */
export function templateUrl(url: string, allowed: string[]): string {
    let origin = '';
    let rest = url;
    const scheme = url.indexOf('://');
    if (scheme > 0) {
        // The authority ends at the path, the query or the fragment, whichever comes first.
        const authorityEnd = url.slice(scheme + 3).search(/[/?#]/);
        const end = authorityEnd < 0 ? -1 : scheme + 3 + authorityEnd;
        origin = end < 0 ? url : url.slice(0, end);
        rest = end < 0 ? '/' : url.slice(end);
        const at = origin.indexOf('@', scheme + 3);
        if (at > 0) origin = origin.slice(0, scheme + 3) + origin.slice(at + 1);
    }
    const hash = rest.indexOf('#');
    if (hash >= 0) rest = rest.slice(0, hash);
    let query = '';
    const question = rest.indexOf('?');
    if (question >= 0) {
        query = rest.slice(question + 1);
        rest = rest.slice(0, question);
    }
    let result = origin + templatePath(rest);
    if (query && allowed.length) {
        const lower = allowed.map(a => a.toLowerCase());
        const kept = query.split('&').filter(pair => {
            const eq = pair.indexOf('=');
            let name = eq < 0 ? pair : pair.slice(0, eq);
            try {
                name = decodeURIComponent(name);
            } catch {
                // Keep the raw name.
            }
            return lower.indexOf(name.toLowerCase()) >= 0;
        });
        if (kept.length) result += '?' + kept.join('&');
    }
    return result;
}

function str(attributes: Attributes, key: string): string | undefined {
    const value = attributes[key];
    return typeof value === 'string' ? value : undefined;
}

/**
 * The browser timeline. Bounded by `maxEvents`; purged on revoke; optionally kept across a reload of the tab in
 * sessionStorage (only while Persistence is granted).
 */
export class DiagnosticRecorder {
    private entries: Entry[] = [];
    private seq = 0;
    private dropped: Record<string, number> = {};
    private byRecord = new WeakMap<TelemetryRecord, Entry>();

    /**
     * Called for every JS exception recorded (the .NET trigger, when the bridge is attached). The app's processors may not
     * have run on the record yet: the listener decides when it is final (`Pipeline.keeps`) and whether it is still here (`has`).
     */
    onException: ((record: TelemetryRecord) => void) | null = null;

    constructor(public config: DiagnosticsConfig) { }

    get size(): number {
        return this.entries.length;
    }

    private wants(cat: CategoryName): boolean {
        return (this.config.categories & Category[cat]) !== 0;
    }

    private push(entry: Entry): void {
        this.entries.push(entry);
        const max = Math.max(1, this.config.maxEvents);
        if (this.entries.length > max) {
            const removed = this.entries.splice(0, this.entries.length - max);
            this.count('capacity', removed.length);
        }
    }

    private count(reason: string, n = 1): void {
        this.dropped[reason] = (this.dropped[reason] || 0) + n;
    }

    /** A record the pipeline accepted (after privacy). Records the timeline does not keep are ignored. */
    record(record: TelemetryRecord): void {
        try {
            const category = this.categoryOf(record);
            if (!category || !this.wants(category)) return;
            if (!this.config.includeSuccessfulActivity && !this.isFailure(record, category)) return;
            const entry: Entry = { seq: ++this.seq, elapsed: perfNow(), record };
            this.byRecord.set(record, entry);
            this.push(entry);
            if (category === 'exception' && this.onException) this.onException(record);
        } catch (e) {
            debug('diagnostics: record failed', e);
        }
    }

    /** An event observed directly (reconnection, visibility), not through a record. */
    add(cat: CategoryName, name: string, feature: number, sev: number, outcome?: DiagnosticEvent['outcome'], attrs?: DiagnosticEvent['attrs']): void {
        if (!this.wants(cat)) return;
        const event: DiagnosticEvent = { seq: ++this.seq, time: now(), elapsed: perfNow(), cat, name, sev, feature };
        if (outcome) event.outcome = outcome;
        if (attrs) event.attrs = attrs;
        this.push({ seq: event.seq, elapsed: event.elapsed, event });
    }

    /** Whether a record is on the timeline now (not dropped by a processor, evicted, purged or cleared). */
    has(record: TelemetryRecord): boolean {
        const entry = this.byRecord.get(record);
        return !!entry && this.entries.indexOf(entry) >= 0;
    }

    /** A record an app processor dropped: it is not exported, so it is not evidence either. */
    forget(record: TelemetryRecord): void {
        const entry = this.byRecord.get(record);
        if (!entry) return;
        const index = this.entries.indexOf(entry);
        if (index >= 0) {
            this.entries.splice(index, 1);
            this.count('filtered');
        }
    }

    /** Drops the events of revoked (or, after a decision, not granted) features. */
    purge(features: number): void {
        if (!features) return;
        const before = this.entries.length;
        this.entries = this.entries.filter(entry => (this.featureOf(entry) & features) === 0);
        if (before !== this.entries.length) this.count('consent_revoked', before - this.entries.length);
        if (features & Features.Persistence) removeItem('session', StorageKeys.diagnostics);
    }

    clear(): void {
        this.entries = [];
        this.dropped = {};
        removeItem('session', StorageKeys.diagnostics);
    }

    /** The timeline, as the user's grant allows now (a record held while undecided is not included). */
    snapshot(granted: number, resource: Attributes): DiagnosticsSnapshot {
        const events: DiagnosticEvent[] = [];
        for (const entry of this.entries) {
            const feature = this.featureOf(entry);
            if (feature && (feature & ~granted)) continue;
            const event = entry.event || this.convert(entry);
            if (event) events.push(event);
        }
        const kept: Attributes = {};
        for (const key of RESOURCE_KEYS) if (key in resource) kept[key] = resource[key];
        return { v: 1, capacity: this.config.maxEvents, dropped: { ...this.dropped }, resource: kept, events };
    }

    /** Keeps the last events for the next load of this tab (sessionStorage; the caller checks Persistence). */
    persist(granted: number): void {
        try {
            const events = this.snapshot(granted, {}).events.slice(-MAX_PERSISTED_EVENTS);
            let json = JSON.stringify({ v: 1, events });
            while (json.length > MAX_PERSISTED_CHARS && events.length > 1) {
                events.splice(0, Math.ceil(events.length / 2));
                json = JSON.stringify({ v: 1, events });
            }
            writeItem('session', StorageKeys.diagnostics, json);
        } catch (e) {
            debug('diagnostics: persist failed', e);
        }
    }

    /** Takes the previous load's events back (once: the stored copy is removed), marked `prev`. */
    restore(): void {
        const text = readItem('session', StorageKeys.diagnostics);
        if (!text) return;
        removeItem('session', StorageKeys.diagnostics);
        try {
            const data = JSON.parse(text) as { v?: number; events?: DiagnosticEvent[] };
            if (!data || data.v !== 1 || !Array.isArray(data.events)) return;
            const restored: Entry[] = [];
            for (const event of data.events.slice(-MAX_PERSISTED_EVENTS)) {
                if (!event || typeof event !== 'object' || typeof event.time !== 'number' || typeof event.cat !== 'string') continue;
                const copy: DiagnosticEvent = { ...event, seq: ++this.seq, prev: true };
                restored.push({ seq: copy.seq, elapsed: typeof copy.elapsed === 'number' ? copy.elapsed : 0, event: copy });
            }
            this.entries = restored.concat(this.entries);
            // The previous load's events count against the capacity like any other: the oldest go first.
            const max = Math.max(1, this.config.maxEvents);
            if (this.entries.length > max) this.count('capacity', this.entries.splice(0, this.entries.length - max).length);
        } catch (e) {
            debug('diagnostics: restore failed', e);
        }
    }

    private featureOf(entry: Entry): number {
        return entry.record ? entry.record.feature : entry.event ? entry.event.feature : 0;
    }

    private categoryOf(record: TelemetryRecord): CategoryName | null {
        if (isSpan(record)) {
            if ('http.request.method' in record.attributes) return 'http';
            if (record.name === 'longtask') return 'browser';
            return null;
        }
        switch (record.eventName) {
            case 'exception':
                return 'exception';
            case 'browser.page_view':
                return 'navigation';
            case 'browser.user_interaction':
            case 'browser.rage_click':
                return 'browser';
            case 'browser.web_vital':
            case 'browser.page_visit_time':
                return null;
            case null:
                return record.scope === 'bit.belemetry.console' ? 'log' : null;
            default:
                return record.scope === 'bit.belemetry.events' ? 'custom' : null;
        }
    }

    private isFailure(record: TelemetryRecord, category: CategoryName): boolean {
        if (category === 'exception') return true;
        if (isSpan(record)) return record.status.code === 2;
        return record.severityNumber >= 13;
    }

    private convert(entry: Entry): DiagnosticEvent | null {
        const record = entry.record as TelemetryRecord;
        const category = this.categoryOf(record);
        if (!category) return null;
        const a = record.attributes;
        const event: DiagnosticEvent = { seq: entry.seq, time: 0, elapsed: entry.elapsed, cat: category, name: category, sev: 9, feature: record.feature };

        if (isSpan(record)) {
            event.time = record.startTime;
            event.traceId = record.traceId;
            event.spanId = record.spanId;
            if (record.parentSpanId) event.parentSpanId = record.parentSpanId;
            if (record.endTime > record.startTime) event.dur = Math.round((record.endTime - record.startTime) * 1000) / 1000;
            if (category === 'http') {
                event.name = str(a, 'http.request.method') || 'HTTP';
                const errorType = str(a, 'error.type');
                const status = a['http.response.status_code'];
                event.outcome = record.status.code === 2 ? 'failure' : errorType === 'AbortError' ? 'cancelled' : 'success';
                event.sev = event.outcome !== 'failure' ? 9 : typeof status === 'number' && status >= 400 && status < 500 ? 13 : 17;
                const attrs: Record<string, string | number | boolean> = { 'http.request.method': event.name };
                if (typeof status === 'number') attrs['http.response.status_code'] = status;
                const host = str(a, 'server.address');
                if (host) attrs['server.address'] = host;
                if (errorType) attrs['error.type'] = errorType;
                const url = str(a, 'url.full');
                if (url) attrs['url.template'] = templateUrl(url, this.config.allowedQueryParameters);
                event.attrs = attrs;
            } else {
                event.name = 'long_task';
                event.sev = 13;
            }
            return event;
        }

        event.time = record.time;
        if (record.traceId) event.traceId = record.traceId;
        if (record.spanId) event.spanId = record.spanId;
        event.sev = record.severityNumber;
        switch (category) {
            case 'exception':
                event.name = 'exception';
                event.outcome = 'failure';
                event.exc = { type: str(a, 'exception.type') || 'Error', message: str(a, 'exception.message') || record.body || '' };
                if (str(a, 'exception.stacktrace')) event.exc.stack = str(a, 'exception.stacktrace');
                break;
            case 'navigation': {
                event.name = 'page_view';
                // The page's route template when the page view has one (.NET templates it again: the page is untrusted).
                const template = str(a, 'url.template');
                const path = str(a, 'url.path');
                if (template) event.route = template;
                else if (path) event.route = templatePath(path);
                const type = str(a, 'browser.navigation.type');
                if (type) event.attrs = { 'browser.navigation.type': type };
                break;
            }
            case 'browser': {
                event.name = record.eventName === 'browser.rage_click' ? 'rage_click' : 'interaction';
                const attrs: Record<string, string | number | boolean> = {};
                const type = str(a, 'event.type');
                const element = str(a, 'target.element');
                if (type) attrs['event.type'] = type;
                if (element) attrs['target.element'] = element;
                event.attrs = attrs;
                break;
            }
            case 'log':
                event.name = 'console';
                if (this.config.includeLogMessages) event.msg = (record.body || '').slice(0, 512);
                break;
            case 'custom':
                event.name = (record.eventName || 'event').slice(0, 64);
                break;
        }
        return event;
    }
}
