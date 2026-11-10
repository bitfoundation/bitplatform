// Bit.Belemetry browser SDK: the public API of `_content/Bit.Belemetry/bit-belemetry.js` (also `window.BitBelemetry`).
// See "JS module API" in CLAUDE.md for the contract with the .NET side.

import { resolveConfig, type BelemetryConfig, type ResolvedConfig } from './config';
import { ConsentManager } from './consent';
import { createAzureExporter } from './exporters/azure';
import { createConsoleExporter } from './exporters/console';
import type { Exporter } from './exporters/exporter';
import { createOtlpExporter } from './exporters/otlp';
import { DotNetCallbacks, relayBatch, type DotNetRef } from './exporters/relay';
import { createSentryExporter } from './exporters/sentry';
import { Features } from './features';
import { installConsole } from './instrumentations/console';
import { absoluteUrl, type InstrumentationContext, type Uninstall } from './instrumentations/context';
import { installDocumentLoad, resetDocumentLoadState } from './instrumentations/document-load';
import { EXCEPTIONS_SCOPE, installErrors } from './instrumentations/errors';
import { installFetch } from './instrumentations/fetch';
import { installInteractions } from './instrumentations/interactions';
import { installLongTasks } from './instrumentations/long-tasks';
import { installPageViews, resetPageViewState, trackPageView as trackPageViewCore } from './instrumentations/page-views';
import { installResources } from './instrumentations/resources';
import { installWebVitals, resetWebVitalsState } from './instrumentations/web-vitals';
import { installXhr } from './instrumentations/xhr';
import { DiagnosticRecorder, type DiagnosticEvent, type DiagnosticsSnapshot } from './diagnostics';
import { onHidden } from './lifecycle';
import { debug, setDebug } from './log';
import { Severity, type Attributes, type TelemetryRecord } from './model';
import { Pipeline, type Processor } from './pipeline';
import { createException, createLog, sanitizeAttributes } from './records';
import { buildResource } from './resource';
import { SessionManager } from './session';
import { countRequest, resetStats, statsSnapshot, type StatsSnapshot } from './stats';
import { StorageKeys } from './storage';
import { fromBase64, Transport } from './transport';

export { Features };
export type { BelemetryConfig, Processor, TelemetryRecord };

/** `getContext()` result (C# BelemetryJsContext). */
export interface BelemetryContext {
    consent: number;
    decided: boolean;
    sessionId: string | null;
    pseudoId: string | null;
    userId: string | null;
}

/** A request serialized by .NET (`sendRequests`, `DrainForUnload`); `body` is base64. */
export interface DotNetRequest {
    exporter: string;
    url: string;
    contentType: string;
    contentEncoding?: string | null;
    headers?: Record<string, string> | null;
    body: string;
    /** The features of its records (C# `BelemetryHttpRequest.Features`): given up when the page no longer grants one. */
    features?: number | null;
}

export interface AttachOptions {
    relay?: boolean;
    unloadDrain?: boolean;
}

interface Bridge {
    ref: DotNetRef;
    relay: boolean;
    unloadDrain: boolean;
}

interface State {
    config: ResolvedConfig;
    consent: ConsentManager;
    session: SessionManager;
    pipeline: Pipeline;
    transport: Transport;
    exporters: Exporter[];
    ctx: InstrumentationContext;
    installed: Record<string, Uninstall>;
    disposers: (() => void)[];
    /** The diagnostic timeline, when diagnostics are enabled. */
    diagnostics: DiagnosticRecorder | null;
}

type Installer = [key: string, features: number, enabled: (config: ResolvedConfig) => boolean, install: (ctx: InstrumentationContext) => Uninstall];

const INSTRUMENTATIONS: Installer[] = [
    ['errors', Features.Errors, () => true, installErrors],
    ['pageViews', Features.PageViews, () => true, installPageViews],
    ['interactions', Features.Interactions, () => true, installInteractions],
    ['webVitals', Features.Performance, c => c.performance.webVitals, installWebVitals],
    ['documentLoad', Features.Performance, c => c.performance.documentLoad, installDocumentLoad],
    ['resources', Features.Performance, c => c.performance.resources, installResources],
    ['longTasks', Features.Performance, c => c.performance.longTasks, installLongTasks],
    ['fetch', Features.Dependencies, c => c.dependencies.fetch, installFetch],
    ['xhr', Features.Dependencies, c => c.dependencies.xhr, installXhr],
    ['console', Features.Errors | Features.Logs, c => c.logs.captureConsole, installConsole],
];

let state: State | null = null;
let bridge: Bridge | null = null;
let lastContext = '';

// Module-level, so a page can register them before the script starts (or before .NET starts it).
const processors: Processor[] = [];
const globalAttributes: Attributes = {};

// Consent governs these: they are set through setUser and the session, never as plain attributes.
const RESERVED_ATTRIBUTES = ['session.id', 'enduser.pseudo.id', 'user.id'];

function recordAttributes(attributes: Attributes | null | undefined): Attributes {
    const valid = sanitizeAttributes(attributes);
    for (const key of RESERVED_ATTRIBUTES) delete valid[key];
    return valid;
}

function effective(s: State): number {
    return s.config.features & s.consent.granted();
}

function createExporters(config: ResolvedConfig, transport: Transport): Exporter[] {
    const list: Exporter[] = [];
    for (const exporter of config.exporters || []) {
        try {
            if (exporter.type === 'otlp') list.push(createOtlpExporter(exporter, transport, config.sdkVersion));
            else if (exporter.type === 'azureMonitor') list.push(createAzureExporter(exporter, transport, config.sdkVersion, config.sampling.sessionRatio));
            else if (exporter.type === 'sentry') list.push(createSentryExporter(exporter, transport, config.sdkVersion));
            else if (exporter.type === 'console') list.push(createConsoleExporter());
        } catch (e) {
            debug('invalid exporter', exporter, e);
        }
    }
    return list;
}

function exporterFor(s: State, url: string): Exporter | undefined {
    const absolute = absoluteUrl(url);
    for (const exporter of s.exporters) {
        for (const prefix of exporter.urls) {
            // A relative endpoint (BrowserEndpoint = "/otlp") is posted to relative to the page, so it is compared the
            // same way; resolved on every call because the page URL changes with client-side navigation.
            if (prefix && absolute.indexOf(absoluteUrl(prefix)) === 0) return exporter;
        }
    }
    return undefined;
}

/** Applies the effective grant to the session (sessionStorage) and the user ids. */
function applyGrant(s: State): void {
    const features = effective(s);
    s.session.setPersistent((features & Features.Sessions) !== 0);
    s.session.setUserTracking((features & Features.UserTracking) !== 0);
}

/** Installs and uninstalls instrumentations: configured AND (granted, or still undecided and so worth holding). */
function reconcile(s: State): void {
    const mask = s.config.features & s.consent.collectable();
    for (const [key, features, enabled, install] of INSTRUMENTATIONS) {
        const want = (mask & features) !== 0 && enabled(s.config);
        const current = s.installed[key];
        try {
            if (want && !current) {
                s.installed[key] = install(s.ctx);
                debug('installed', key);
            } else if (!want && current) {
                delete s.installed[key];
                current();
                debug('uninstalled', key);
            }
        } catch (e) {
            debug('instrumentation failed', key, e);
        }
    }
}

function updateSink(s: State): void {
    const relay = s.config.transport === 'relay' || (bridge !== null && bridge.relay);
    if (relay) {
        // Without the .NET bridge yet, records wait in the queue.
        s.pipeline.sink = bridge ? (batch => relayBatch((bridge as Bridge).ref, batch)) : null;
    } else {
        s.pipeline.sink = (batch, unload, mark) => Promise.all(s.exporters.map(exporter => {
            try {
                return exporter.export(batch, unload, mark).catch(e => debug('export failed', e));
            } catch (e) {
                debug('export failed', e);
                return undefined;
            }
        })).then(() => undefined);
    }
}

function syncContext(): void {
    if (!bridge) return;
    try {
        const context = getContext();
        const json = JSON.stringify(context);
        if (json === lastContext) return;
        lastContext = json;
        bridge.ref.invokeMethodAsync(DotNetCallbacks.contextChanged, context).catch(e => debug('context sync failed', e));
    } catch (e) {
        debug('context sync failed', e);
    }
}

function onConsentChanged(s: State, previous: number, next: number, released: TelemetryRecord[]): void {
    s.pipeline.purge(previous & ~next);
    if (s.diagnostics) {
        s.diagnostics.purge(previous & ~next);
        // Undecided records were held in case they were granted: the decision drops what was not.
        if (s.consent.decided) s.diagnostics.purge(Features.All & ~next & ~(previous & ~next));
    }
    applyGrant(s);
    for (const record of released) {
        if (s.config.features & record.feature) s.pipeline.enqueue(record);
    }
    reconcile(s);
    if (next & ~previous & Features.Persistence) s.transport.resendPersisted();
    debug('consent', next, 'decided', s.consent.decided);
    syncContext();
}

function onPageHidden(s: State, unloading: boolean): void {
    // Everything here must start its requests synchronously: nothing async is guaranteed to run after pagehide.
    s.pipeline.flush(true);
    const diagnostics = s.diagnostics;
    if (diagnostics) {
        if (s.consent.collectable() & Features.PageViews) diagnostics.add('browser', unloading ? 'page.unload' : 'page.hidden', Features.PageViews, 9);
        if (diagnostics.config.persist && (effective(s) & Features.Persistence)) diagnostics.persist(s.consent.granted());
    }
    if (bridge && bridge.unloadDrain) {
        try {
            // Synchronous JS -> .NET call, only available in WebAssembly; it throws elsewhere.
            const requests = bridge.ref.invokeMethod<DotNetRequest[] | null>(DotNetCallbacks.drainForUnload);
            sendRequestsCore(requests, true);
        } catch (e) {
            debug('unload drain unavailable', e);
        }
    }
    s.transport.pageHidden(unloading);
}

function init(input: BelemetryConfig | null | undefined): State {
    const config = resolveConfig(input);
    setDebug(!!config.debug);
    const consent = new ConsentManager(config.consent, config.features);
    consent.load();
    const session = new SessionManager(config.session);
    let s: State;
    const transport = new Transport({
        persistence: config.persistence,
        canPersist: () => (effective(s) & Features.Persistence) !== 0,
        headersFor: (_exporter, url) => {
            const exporter = exporterFor(s, url);
            return exporter ? exporter.headers : null;
        },
        revocations: features => s.consent.revocations(features),
    });
    const exporters = createExporters(config, transport);
    const pipeline = new Pipeline(config, consent, session, buildResource(config));
    pipeline.processors = processors;
    pipeline.attributes = globalAttributes;
    const diagnostics = config.diagnostics && config.diagnostics.enabled ? new DiagnosticRecorder(config.diagnostics) : null;
    if (diagnostics) {
        pipeline.onRecord = record => diagnostics.record(record);
        pipeline.onFiltered = record => diagnostics.forget(record);
    }
    s = {
        config,
        consent,
        session,
        pipeline,
        transport,
        exporters,
        ctx: {
            config,
            emit: record => pipeline.emit(record),
            isExporterUrl: url => exporterFor(s, url) !== undefined,
        },
        installed: {},
        disposers: [],
        diagnostics,
    };
    return s;
}

// A JS error offered to .NET as a capture trigger at most this often; .NET applies its own cooldown per failure too.
const TRIGGER_INTERVAL_MS = 10000;
let lastTrigger = 0;

/** The Blazor-specific signals only the diagnostic timeline records: reconnection and the back/forward cache. */
function installDiagnostics(s: State, diagnostics: DiagnosticRecorder): void {
    if (diagnostics.config.persist && (effective(s) & Features.Persistence)) diagnostics.restore();

    diagnostics.onException = record => {
        if (!diagnostics.config.triggerBrowserErrors || !bridge) return;
        // Once the app's processors had their say: they run at export, after the record reached the timeline, and an error
        // one of them drops is neither exported nor evidence, so it starts no capture either. Deferred, so a processor the
        // app adds right after start still sees it, as the export would.
        setTimeout(() => {
            try {
                if (!bridge || state !== s || !s.pipeline.keeps(record) || !diagnostics.has(record)) return;
                const time = Date.now();
                if (time - lastTrigger < TRIGGER_INTERVAL_MS) return;
                lastTrigger = time;
                const type = record.attributes['exception.type'];
                const message = record.attributes['exception.message'];
                const error = {
                    type: (typeof type === 'string' && type ? type : 'Error').slice(0, 256),
                    message: (typeof message === 'string' ? message : '').slice(0, 1024),
                };
                bridge.ref.invokeMethodAsync(DotNetCallbacks.diagnosticTrigger, error).catch(e => debug('diagnostic trigger failed', e));
            } catch (e) {
                debug('diagnostic trigger failed', e);
            }
        }, 0);
    };

    // Blazor dispatches it on the reconnect dialog (#components-reconnect-modal) without bubbling: a capturing listener on
    // the document sees it whatever element it targets. Only an app with a reconnect UI of its own gets it.
    const onReconnect = (event: Event) => {
        try {
            if (!(s.consent.collectable() & Features.Errors)) return;
            const detail = (event as CustomEvent).detail || {};
            const state = String(detail.state || 'unknown').slice(0, 32);
            const failed = state === 'failed' || state === 'rejected' || state === 'resume-failed';
            const attrs: Record<string, string | number | boolean> = { 'reconnect.state': state };
            if (typeof detail.currentAttempt === 'number') attrs['reconnect.attempt'] = detail.currentAttempt;
            diagnostics.add('circuit', 'reconnect.' + state, Features.Errors, failed ? 17 : state === 'hide' ? 9 : 13, failed ? 'failure' : undefined, attrs);
        } catch (e) {
            debug('diagnostics: reconnect event failed', e);
        }
    };
    const onShow = (event: PageTransitionEvent) => {
        if (event.persisted && (s.consent.collectable() & Features.PageViews)) diagnostics.add('browser', 'page.restored', Features.PageViews, 9);
    };
    document.addEventListener('components-reconnect-state-changed', onReconnect, true);
    addEventListener('pageshow', onShow);
    s.disposers.push(() => {
        document.removeEventListener('components-reconnect-state-changed', onReconnect, true);
        removeEventListener('pageshow', onShow);
    });
}

/** Idempotent start. A second call only updates the consent settings and the transport (bridge). */
export function start(config?: BelemetryConfig | null): void {
    try {
        if (state) {
            const s = state;
            const next = resolveConfig(config);
            s.config.transport = next.transport;
            updateSink(s);
            s.config.consent = next.consent;
            s.consent.update(next.consent);
            // A WebAssembly client starting the script a server already started: its .NET pipeline exports through
            // fetch, to URLs only it knows.
            const ignore = s.config.dependencies.ignoreUrls;
            for (const url of next.dependencies.ignoreUrls || []) if (ignore.indexOf(url) < 0) ignore.push(url);
            // Its routes may be its own (configured on the client only). A new list, so the compiled routes are rebuilt.
            const routes = Array.isArray(s.config.pageViews.routes) ? s.config.pageViews.routes : [];
            const known = routes.map(r => String(r).toLowerCase());
            const added: string[] = [];
            for (const route of Array.isArray(next.pageViews.routes) ? next.pageViews.routes : []) {
                if (typeof route !== 'string' || known.indexOf(route.toLowerCase()) >= 0) continue;
                known.push(route.toLowerCase());
                added.push(route);
            }
            if (added.length) s.config.pageViews.routes = routes.concat(added);
            return;
        }
        const s = state = init(config);
        applyGrant(s);
        updateSink(s);
        s.consent.onChange((previous, next, released) => onConsentChanged(s, previous, next, released));
        if (s.diagnostics && typeof document !== 'undefined') installDiagnostics(s, s.diagnostics);
        s.session.onChange = syncContext;
        s.disposers.push(onHidden(unloading => onPageHidden(s, unloading), 'last'));
        const onOnline = () => s.transport.resendPersisted();
        addEventListener('online', onOnline);
        s.disposers.push(() => removeEventListener('online', onOnline));
        // A decision made in another tab of the app reaches this one through the stored choice.
        const onStorage = (event: StorageEvent) => {
            if (event.key === StorageKeys.consent) s.consent.adopt(event.newValue);
        };
        addEventListener('storage', onStorage);
        s.disposers.push(() => removeEventListener('storage', onStorage));
        // The online event only covers the device's own connection; a backend that was down needs a periodic retry.
        const retryMs = s.config.persistence.retryIntervalMs;
        if (s.config.persistence.enabled && retryMs > 0) {
            const timer = setInterval(onOnline, Math.max(1000, retryMs));
            s.disposers.push(() => clearInterval(timer));
        }
        reconcile(s);
        s.transport.resendPersisted();
        syncContext();
        debug('started', s.config);
    } catch (e) {
        debug('start failed', e);
    }
}

/**
 * Whether the script has started: a repeated `start` only updates consent and transport and adds to the ignored URLs and
 * the routes, so .NET leaves out the discovered routes (the page has its own server's).
 */
export function isStarted(): boolean {
    return state !== null;
}

/** Records the user's consent decision (a bit mask of Features). */
export function setConsent(features: number): void {
    try {
        if (state) state.consent.set(features);
    } catch (e) {
        debug('setConsent failed', e);
    }
}

export function getConsent(): number {
    return state ? state.consent.granted() : 0;
}

export function isConsentDecided(): boolean {
    return state ? state.consent.decided : false;
}

/** Sets (or clears, with null) the authenticated user id. Kept and sent only with UserTracking. */
export function setUser(id: string | null): void {
    try {
        if (state) state.session.setUser(id);
    } catch (e) {
        debug('setUser failed', e);
    }
}

/** A custom event: a log record with `eventName = name`, feature Events. */
export function trackEvent(name: string, attributes?: Attributes | null): void {
    try {
        if (state && name) state.pipeline.emit(createLog('bit.belemetry.events', Features.Events, Severity.Info, String(name), String(name), recordAttributes(attributes)));
    } catch (e) {
        debug('trackEvent failed', e);
    }
}

/** A manual page view (the current URL unless `url` is given). */
export function trackPageView(name?: string | null, url?: string | null, attributes?: Attributes | null): void {
    try {
        if (state) trackPageViewCore(state.ctx, 'manual', { name, url, attributes: recordAttributes(attributes) });
    } catch (e) {
        debug('trackPageView failed', e);
    }
}

/** A manual exception, feature Errors. */
export function trackException(type: string, message: string, stack?: string | null, attributes?: Attributes | null): void {
    try {
        // A scope of its own: the app caught it, unlike what the errors instrumentation records (an exporter tells
        // handled from unhandled by it), and a Blazor Server component's C# TrackException arrives here too.
        if (state) state.pipeline.emit(createException(EXCEPTIONS_SCOPE, String(type || 'Error'), String(message || ''), stack, recordAttributes(attributes)));
    } catch (e) {
        debug('trackException failed', e);
    }
}

/**
 * Adds a processor: a function run on every browser record as it is handed to the exporters (or relayed to .NET),
 * after consent and privacy. It may change the record in place, and return `false` to drop it; one that throws is
 * skipped for that record. Returns a function that removes it.
 */
export function addProcessor(processor: Processor): () => void {
    if (typeof processor !== 'function') return () => undefined;
    processors.push(processor);
    return () => {
        const index = processors.indexOf(processor);
        if (index >= 0) processors.splice(index, 1);
    };
}

/**
 * Sets attributes added to every browser record from now on (the record's own attributes win) - app context such as
 * a tenant or a feature flag. Merged with the previous ones; a null value removes a key, and `setAttributes(null)`
 * removes them all. They go through privacy like any attribute. The consent-governed ids (`session.id`,
 * `enduser.pseudo.id`, `user.id`) cannot be set here - use `setUser`.
 */
export function setAttributes(attributes?: Attributes | Record<string, unknown> | null): void {
    try {
        if (!attributes) {
            for (const key of Object.keys(globalAttributes)) delete globalAttributes[key];
            return;
        }
        const valid = sanitizeAttributes(attributes);
        for (const key of Object.keys(attributes)) {
            if (RESERVED_ATTRIBUTES.indexOf(key) >= 0) continue;
            if (key in valid) globalAttributes[key] = valid[key];
            else if ((attributes as Record<string, unknown>)[key] == null) delete globalAttributes[key];
        }
    } catch (e) {
        debug('setAttributes failed', e);
    }
}

/** Resolves when the queued records have been handed to the transport. */
export function flush(): Promise<void> {
    try {
        return state ? state.pipeline.flush(false) : Promise.resolve();
    } catch (e) {
        debug('flush failed', e);
        return Promise.resolve();
    }
}

/** `getStats()` result: what the script dropped and why, what became of its requests, and what it holds now. */
export interface BelemetryStats extends StatsSnapshot {
    /** Records queued for the next export. */
    queued: number;
    /** Records held until the user decides on consent. */
    pending: number;
}

/** What the script dropped and why, and what became of its requests, since the page loaded - for troubleshooting. */
export function getStats(): BelemetryStats {
    const snapshot = statsSnapshot();
    return { ...snapshot, queued: state ? state.pipeline.size : 0, pending: state ? state.consent.pendingCount : 0 };
}

/**
 * The diagnostic timeline (production diagnostics), as the user's grant allows now; null when diagnostics are off.
 * Read by .NET when it makes a capture: it never leaves the page otherwise.
 */
export function getDiagnostics(): DiagnosticsSnapshot | null {
    try {
        if (!state || !state.diagnostics) return null;
        // The app's processors may still drop or change queued records: run them first, as the export will.
        state.pipeline.processQueued();
        return state.diagnostics.snapshot(state.consent.granted(), state.pipeline.resource);
    } catch (e) {
        debug('getDiagnostics failed', e);
        return null;
    }
}

/**
 * `getDiagnostics()` as a Blob, for a Blazor Server circuit, whose messages from the page are capped: .NET asks for it as
 * an `IJSStreamReference`, and Blazor turns the returned Blob into one itself (returning a stream reference made here
 * would be wrapped twice and refused). Null when diagnostics are off.
 */
export function getDiagnosticsStream(): Blob | null {
    const snapshot = getDiagnostics();
    return snapshot ? fitDiagnostics(snapshot) : null;
}

// What .NET reads of the stream at most (C# BelemetryJsBridge.MaxDiagnosticsBytes), with room to spare.
const MAX_DIAGNOSTICS_BYTES = 4 * 1024 * 1024 - 64 * 1024;

/**
 * The timeline as a Blob .NET accepts: past its read limit the whole stream would be refused, so the oldest activity
 * goes first, then the oldest failures (as the capture's own limit does), counted as `size_limit`.
 */
function fitDiagnostics(snapshot: DiagnosticsSnapshot): Blob {
    let blob = new Blob([JSON.stringify(snapshot)], { type: 'application/json' });
    for (let pass = 0; blob.size > MAX_DIAGNOSTICS_BYTES && snapshot.events.length && pass < 8; pass++) {
        // Characters, not bytes: an underestimate for non-ASCII text, which the next pass makes up for.
        let excess = blob.size - MAX_DIAGNOSTICS_BYTES;
        const removed = new Set<DiagnosticEvent>();
        for (const failures of [false, true]) {
            for (const event of snapshot.events) {
                if (excess <= 0) break;
                if ((event.outcome === 'failure' || !!event.exc) !== failures) continue;
                removed.add(event);
                excess -= JSON.stringify(event).length + 1;
            }
        }
        snapshot.events = snapshot.events.filter(event => !removed.has(event));
        snapshot.dropped.size_limit = (snapshot.dropped.size_limit || 0) + removed.size;
        blob = new Blob([JSON.stringify(snapshot)], { type: 'application/json' });
    }
    return blob;
}

/** Forgets the diagnostic timeline (and its copy kept for the next load). */
export function clearDiagnostics(): void {
    if (state && state.diagnostics) state.diagnostics.clear();
}

/**
 * Saves a file .NET produced (a support bundle) as a download: `content` is bytes (WebAssembly, Hybrid) or a .NET stream
 * reference (Server). The name is reduced to a safe file name.
 */
export async function downloadFile(fileName: string, content: unknown): Promise<void> {
    let data: ArrayBuffer | Uint8Array;
    const streamRef = content as { arrayBuffer?: () => Promise<ArrayBuffer> } | null;
    if (streamRef && typeof streamRef.arrayBuffer === 'function') data = await streamRef.arrayBuffer();
    else if (content instanceof Uint8Array || content instanceof ArrayBuffer) data = content;
    else return;
    const name = String(fileName || 'diagnostics.zip').replace(/[^A-Za-z0-9._-]/g, '-').slice(0, 120) || 'diagnostics.zip';
    const url = URL.createObjectURL(new Blob([data as BlobPart], { type: 'application/zip' }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = name;
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 30000);
}

export function getContext(): BelemetryContext {
    if (!state) return { consent: 0, decided: false, sessionId: null, pseudoId: null, userId: null };
    const s = state;
    return { consent: s.consent.granted(), decided: s.consent.decided, sessionId: s.session.peek(), pseudoId: s.session.pseudoId, userId: s.session.userId };
}

/** Registers the .NET bridge (BelemetryJsCallbacks). */
export function attachDotNet(dotNetRef: DotNetRef, options?: AttachOptions | null): void {
    try {
        bridge = dotNetRef ? { ref: dotNetRef, relay: !!(options && options.relay), unloadDrain: !!(options && options.unloadDrain) } : null;
        lastContext = '';
        if (state) {
            updateSink(state);
            syncContext();
            state.pipeline.flush();
        }
    } catch (e) {
        debug('attachDotNet failed', e);
    }
}

function sendRequestsCore(requests: DotNetRequest[] | null | undefined, unload: boolean): Promise<void> {
    if (!Array.isArray(requests) || !requests.length) return Promise.resolve();
    // Before start there is no consent state, so nothing may be persisted. A start while it retries brings one: a revoke
    // from then on withdraws the request.
    const transport = state ? state.transport : new Transport({
        persistence: resolveConfig().persistence,
        canPersist: () => false,
        revocations: features => (state ? state.consent.revocations(features) : 0),
    });
    const sends: Promise<void>[] = [];
    for (const request of requests) {
        try {
            const features = typeof request.features === 'number' ? request.features : null;
            // A revoke made in the page (a banner, another tab, Global Privacy Control) reaches .NET only later: a
            // request it built under a feature the page no longer grants holds that feature's records.
            if (state && features !== null && (features & ~state.consent.granted())) {
                countRequest('withdrawn');
                continue;
            }
            sends.push(transport.send({
                exporter: request.exporter,
                url: request.url,
                contentType: request.contentType,
                contentEncoding: request.contentEncoding || null,
                headers: request.headers || null,
                body: fromBase64(request.body || ''),
            }, unload, state && features !== null ? state.consent.mark(features) : undefined));
        } catch (e) {
            debug('invalid request', e);
        }
    }
    return Promise.all(sends).then(() => undefined);
}

/** Sends requests serialized by .NET (WebAssembly) with keepalive fetch, retry and persistence. */
export function sendRequests(requests: DotNetRequest[] | null): Promise<void> {
    try {
        return sendRequestsCore(requests, false);
    } catch (e) {
        debug('sendRequests failed', e);
        return Promise.resolve();
    }
}

/** Tears everything down (tests only). */
export function _reset(): void {
    if (state) {
        for (const key of Object.keys(state.installed)) {
            try {
                state.installed[key]();
            } catch {
                // Ignore.
            }
        }
        for (const dispose of state.disposers) dispose();
        state.pipeline.sink = null;
    }
    state = null;
    bridge = null;
    lastContext = '';
    processors.length = 0;
    for (const key of Object.keys(globalAttributes)) delete globalAttributes[key];
    resetStats();
    resetPageViewState();
    resetDocumentLoadState();
    resetWebVitalsState();
    lastTrigger = 0;
}

/** The internal state (tests only). */
export function _getState(): State | null {
    return state;
}

const api = {
    start, isStarted, setConsent, getConsent, isConsentDecided, setUser, trackEvent, trackPageView, trackException, addProcessor, setAttributes, flush, getContext,
    getStats, attachDotNet, sendRequests, getDiagnostics, getDiagnosticsStream, clearDiagnostics, downloadFile, Features,
};

if (typeof window !== 'undefined') {
    (window as unknown as { BitBelemetry: typeof api }).BitBelemetry = api;
    // <BelemetryScript /> renders the config into the page, so telemetry starts before Blazor does.
    try {
        const element = document.getElementById('bit-belemetry-config');
        if (element) {
            let config: BelemetryConfig | null = null;
            try {
                config = JSON.parse(element.textContent || '');
            } catch {
                config = null;
            }
            if (config) start(config);
        }
    } catch {
        // Never throw into the host page.
    }
}
