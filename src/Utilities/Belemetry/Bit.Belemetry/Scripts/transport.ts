import type { PersistenceConfig } from './config';
import { isRetryable, rebuildPartial, RETRYABLE_STATUS } from './exporters/azure';
import { newSpanId } from './ids';
import { debug } from './log';
import { countRequest } from './stats';
import { readItem, removeItem, StorageKeys, writeItem } from './storage';

/** A serialized export request, built by a JS exporter or by .NET (`sendRequests`, `DrainForUnload`). */
export interface OutgoingRequest {
    exporter: string;
    url: string;
    contentType: string;
    contentEncoding?: string | null;
    headers?: Record<string, string> | null;
    body: Uint8Array;
    /** The uncompressed body, kept for persistence (stored requests carry no content encoding). */
    raw?: Uint8Array | null;
    /** When the request was first persisted, so re-persisting it keeps its age. */
    time?: number;
}

/** An entry of `bit-belemetry-queue` in localStorage (contract shape). */
export interface PersistedRequest {
    id: string;
    exporter: string;
    url: string;
    contentType: string;
    body: string;
    time: number;
}

export interface TransportOptions {
    persistence: PersistenceConfig;
    /** Whether persistence is both configured and consented right now. */
    canPersist: () => boolean;
    /** Headers for a resent request; stored requests carry no headers (they may hold secrets). */
    headersFor?: (exporter: string, url: string) => Record<string, string> | null | undefined;
    sleep?: (ms: number) => Promise<void>;
}

export { isRetryable, RETRYABLE_STATUS };

/**
 * The OTLP specification's retryable statuses - shorter than Azure Monitor's: "All other 4xx or 5xx response status
 * codes MUST NOT be retried".
 */
export const OTLP_RETRYABLE_STATUS = [429, 502, 503, 504];

/** Whether a request went to Azure Monitor: the script's exporter is `azureMonitor`, .NET's are `azuremonitor`, `azuremonitor1`, ... */
export function isAzure(exporter: string): boolean {
    return /^azuremonitor\d*$/i.test(exporter);
}

/** Whether a status is transient for the backend a request went to. */
export function isRetryableFor(exporter: string, status: number): boolean {
    return isAzure(exporter) ? isRetryable(status) : OTLP_RETRYABLE_STATUS.indexOf(status) >= 0;
}

export const MAX_ATTEMPTS = 3;
const KEEPALIVE_LIMIT = 60 * 1024;
// Chromium also caps the number of keepalive requests in flight (9, as the OpenTelemetry and Faro browser exporters
// assume); past it they fail like an over-budget body.
const KEEPALIVE_MAX_REQUESTS = 9;
const MAX_RETRY_AFTER_MS = 60000;
const MAX_RESPONSE_BYTES = 4 * 1024 * 1024;

let suppressed = 0;

/** True while the transport itself is calling fetch, so the fetch instrumentation does not trace exports. */
export function isSuppressed(): boolean {
    return suppressed > 0;
}

/** Parses Retry-After (seconds or an HTTP date) to milliseconds. */
export function parseRetryAfter(value: string | null): number | null {
    if (!value) return null;
    const seconds = Number(value);
    if (!isNaN(seconds)) return Math.max(0, seconds * 1000);
    const date = Date.parse(value);
    return isNaN(date) ? null : Math.max(0, date - Date.now());
}

export function backoff(attempt: number): number {
    // 1 s, 2 s, ... with +-20% jitter so a fleet of tabs does not retry in lockstep.
    return 1000 * Math.pow(2, attempt - 1) * (0.8 + Math.random() * 0.4);
}

export function toBase64(bytes: Uint8Array): string {
    let binary = '';
    for (let i = 0; i < bytes.length; i += 0x8000) {
        binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000) as unknown as number[]);
    }
    return btoa(binary);
}

export function fromBase64(text: string): Uint8Array {
    const binary = atob(text);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return bytes;
}

async function readText(response: Response, max: number): Promise<string | null> {
    if (!response.body) return response.text();
    if (Number(response.headers.get('Content-Length')) > max) {
        response.body.cancel().catch(() => undefined);
        return null;
    }
    const reader = response.body.getReader();
    const chunks: Uint8Array[] = [];
    let size = 0;
    for (;;) {
        const result = await reader.read();
        if (result.done) break;
        size += result.value.length;
        if (size > max) {
            reader.cancel().catch(() => undefined);
            return null;
        }
        chunks.push(result.value);
    }
    const bytes = new Uint8Array(size);
    let offset = 0;
    for (const chunk of chunks) {
        bytes.set(chunk, offset);
        offset += chunk.length;
    }
    return new TextDecoder().decode(bytes);
}

async function pipe(bytes: Uint8Array, transform: GenericTransformStream): Promise<Uint8Array> {
    const stream = new Blob([bytes as BlobPart]).stream().pipeThrough(transform as unknown as ReadableWritablePair<Uint8Array, Uint8Array>);
    return new Uint8Array(await new Response(stream).arrayBuffer());
}

/** gzip via CompressionStream; null when unsupported. */
export async function gzip(bytes: Uint8Array): Promise<Uint8Array | null> {
    if (typeof CompressionStream === 'undefined') return null;
    try {
        return await pipe(bytes, new CompressionStream('gzip'));
    } catch {
        return null;
    }
}

export async function gunzip(bytes: Uint8Array): Promise<Uint8Array | null> {
    if (typeof DecompressionStream === 'undefined') return null;
    try {
        return await pipe(bytes, new DecompressionStream('gzip'));
    } catch {
        return null;
    }
}

// keepalive lets a request outlive the page, but a browser caps the bodies of all the page's in-flight keepalive
// requests together at 64 KiB, and refuses (TypeError) the one that does not fit - spuriously failing parallel batches
// that each fit on their own. So the budget is tracked here, and a request claims it only when it fits.
let keepaliveInFlight = 0;
let keepaliveRequests = 0;

/** The keepalive bytes in flight (tests only). */
export function _keepaliveInFlight(): number {
    return keepaliveInFlight;
}

function fitsKeepalive(request: OutgoingRequest): boolean {
    return request.body.length <= KEEPALIVE_LIMIT - keepaliveInFlight && keepaliveRequests < KEEPALIVE_MAX_REQUESTS;
}

function post(request: OutgoingRequest, keepalive: boolean): Promise<Response> {
    const headers: Record<string, string> = {};
    const extra = request.headers || {};
    for (const key of Object.keys(extra)) headers[key] = extra[key];
    headers['Content-Type'] = request.contentType;
    if (request.contentEncoding) headers['Content-Encoding'] = request.contentEncoding;
    const size = request.body.length;
    const release = () => {
        keepaliveInFlight = Math.max(0, keepaliveInFlight - size);
        keepaliveRequests = Math.max(0, keepaliveRequests - 1);
    };
    if (keepalive) {
        keepaliveInFlight += size;
        keepaliveRequests++;
    }
    suppressed++;
    try {
        const response = fetch(request.url, { method: 'POST', headers, body: request.body as BodyInit, keepalive, credentials: 'omit' });
        if (keepalive) response.then(release, release);
        return response;
    } catch (e) {
        if (keepalive) release();
        throw e;
    } finally {
        suppressed--;
    }
}

/** What became of a request: accepted, refused for good, stored for later, or lost (no persistence). */
type Outcome = 'delivered' | 'rejected' | 'kept' | 'dropped';

/** How the attempts at a request ended, and what is still undelivered: after an Azure 206, only the items to retry. */
interface Attempted {
    outcome: 'delivered' | 'rejected' | 'failed';
    remaining: OutgoingRequest;
}

export function loadPersisted(): PersistedRequest[] {
    const raw = readItem('local', StorageKeys.queue);
    if (!raw) return [];
    try {
        const list = JSON.parse(raw);
        return Array.isArray(list) ? list.filter(e => e && typeof e.url === 'string' && typeof e.body === 'string') : [];
    } catch {
        return [];
    }
}

/** Drops entries older than maxAgeHours, evicts the oldest beyond maxBytes, and writes the queue, oldest first. */
export function savePersisted(list: PersistedRequest[], config: PersistenceConfig, time = Date.now()): PersistedRequest[] {
    const minTime = time - config.maxAgeHours * 3600000;
    // A request stored again after a failed resend keeps its age: sorted back into place, so eviction drops the oldest.
    const kept = list.filter(e => e.time >= minTime).sort((a, b) => a.time - b.time);
    let json = JSON.stringify(kept);
    while (kept.length && json.length > config.maxBytes) {
        kept.shift();
        json = JSON.stringify(kept);
    }
    while (kept.length && !writeItem('local', StorageKeys.queue, json)) {
        // Quota exceeded: make room by evicting the oldest.
        kept.shift();
        json = JSON.stringify(kept);
    }
    if (!kept.length) removeItem('local', StorageKeys.queue);
    return kept;
}

/** Takes a stored request out of the queue; null when it is gone (another tab sent it, or it expired or was evicted). */
function takePersisted(id: string, config: PersistenceConfig): PersistedRequest | null {
    const list = loadPersisted();
    for (let i = 0; i < list.length; i++) {
        if (list[i].id !== id) continue;
        const entry = list.splice(i, 1)[0];
        savePersisted(list, config);
        return entry;
    }
    return null;
}

/**
 * Sends requests with fetch: keepalive for small bodies and during unload, retries with exponential backoff (and
 * Retry-After), Azure 206 partial-success handling, and persistence of what could not be sent.
 */
export class Transport {
    private inflight: Promise<void>[] = [];
    private resending = false;
    /** Whether the page is being unloaded for good: a keepalive request's fetch may fail then and still be sent. */
    private unloading = false;
    /** The stored request a resend pass is sending: out of storage until its outcome is known. */
    private resent: { entry: PersistedRequest; keepalive: boolean; restored: boolean } | null = null;

    constructor(public options: TransportOptions) { }

    /** Sends a request. During unload there is one keepalive attempt and no waiting. Never rejects. */
    send(request: OutgoingRequest, unload = false): Promise<void> {
        // deliver() calls fetch before its first await, so during unload the request starts synchronously.
        const promise: Promise<void> = this.deliver(request, unload).then(countRequest, e => debug('send failed', e)).then(() => {
            const index = this.inflight.indexOf(promise);
            if (index >= 0) this.inflight.splice(index, 1);
        });
        this.inflight.push(promise);
        return promise;
    }

    /** Resolves when every request in flight (including retries) has settled. */
    idle(): Promise<void> {
        return Promise.all(this.inflight.slice()).then(() => undefined);
    }

    private canPersist(): boolean {
        return this.options.persistence.enabled && this.options.canPersist();
    }

    private async deliver(request: OutgoingRequest, unload: boolean): Promise<Outcome> {
        // A request that cannot have keepalive while the page unloads would be cancelled with the page: stored, it is
        // sent on the next visit. Without persistence a plain request is still worth it - the page may only be hidden.
        if (unload && !fitsKeepalive(request) && this.canPersist()) return this.persist(request);
        const result = await this.attempt(request, unload, MAX_ATTEMPTS);
        return result.outcome === 'failed' ? this.persist(result.remaining) : result.outcome;
    }

    /**
     * Posts a request until it is delivered, refused for good, or out of attempts: retries with exponential backoff
     * (and Retry-After), and narrows an Azure 206 to the items worth retrying. Calls fetch before its first await.
     */
    private async attempt(request: OutgoingRequest, unload: boolean, maxAttempts: number): Promise<Attempted> {
        let current = request;
        for (let attempt = 1; ; attempt++) {
            const keepalive = fitsKeepalive(current);
            let response: Response | null = null;
            try {
                response = await post(current, keepalive);
            } catch (e) {
                // Chromium fails the fetch of a keepalive request when its page unloads, and sends the request all the
                // same: stored as well, it would arrive twice. Only offline is it certain that it did not go.
                if (keepalive && this.unloading && !(typeof navigator !== 'undefined' && navigator.onLine === false)) {
                    return { outcome: 'delivered', remaining: current };
                }
                debug('network error', current.url, e);
            }
            let delay: number | null = null;
            if (response) {
                const status = response.status;
                if (status === 206 && isAzure(current.exporter)) {
                    let next: OutgoingRequest | null = null;
                    try {
                        const text = await readText(response, MAX_RESPONSE_BYTES);
                        if (text !== null) {
                            const plain = current.raw || (current.contentEncoding === 'gzip' ? await gunzip(current.body) : null);
                            next = rebuildPartial(current, text, plain);
                        }
                    } catch {
                        // An unreadable 206 body: the accepted items are in, nothing to retry.
                    }
                    if (!next) return { outcome: 'delivered', remaining: current };
                    current = next;
                } else if (status >= 200 && status < 300) {
                    return { outcome: 'delivered', remaining: current };
                } else if (!isRetryableFor(current.exporter, status)) {
                    debug('dropped request, status', status, current.url);
                    return { outcome: 'rejected', remaining: current };
                }
                delay = parseRetryAfter(response.headers.get('Retry-After'));
            }
            const offline = !response && typeof navigator !== 'undefined' && navigator.onLine === false;
            if (unload || offline || attempt >= maxAttempts || (delay !== null && delay > MAX_RETRY_AFTER_MS)) {
                return { outcome: 'failed', remaining: current };
            }
            const wait = delay !== null ? delay : backoff(attempt);
            await (this.options.sleep ? this.options.sleep(wait) : new Promise<void>(resolve => setTimeout(resolve, wait)));
        }
    }

    /** Stores a request for a later resend, if persistence is configured and consented; otherwise drops it. */
    async persist(request: OutgoingRequest): Promise<Outcome> {
        if (!this.canPersist()) {
            debug('dropped request (no persistence)', request.url);
            return 'dropped';
        }
        let body: Uint8Array | null = request.raw || null;
        if (!body) body = !request.contentEncoding ? request.body : request.contentEncoding === 'gzip' ? await gunzip(request.body) : null;
        if (!body) return 'dropped';
        const list = loadPersisted();
        list.push({
            id: newSpanId(),
            exporter: request.exporter,
            url: request.url,
            contentType: request.contentType,
            body: toBase64(body),
            time: request.time || Date.now(),
        });
        savePersisted(list, this.options.persistence);
        return 'kept';
    }

    /**
     * Resends the persisted requests (on start, periodically, and when the browser comes back online): oldest first,
     * one attempt each. A request that fails holds back the rest of its URL's backlog - that backend is still
     * unreachable, and hammering it with every stored request (each with its own retries) would only waste the user's
     * network - but not the requests to other backends. The rest stay stored.
     */
    resendPersisted(): Promise<void> {
        if (this.resending || !this.canPersist()) return Promise.resolve();
        const list = savePersisted(loadPersisted(), this.options.persistence);
        if (!list.length) return Promise.resolve();
        this.resending = true;
        const promise: Promise<void> = this.resend(list).catch(e => debug('resend failed', e)).then(() => {
            this.resending = false;
            const index = this.inflight.indexOf(promise);
            if (index >= 0) this.inflight.splice(index, 1);
        });
        this.inflight.push(promise);
        return promise;
    }

    private async resend(list: PersistedRequest[]): Promise<void> {
        const headersFor = this.options.headersFor;
        const config = this.options.persistence;
        // The URLs whose request failed in this pass: those backends are still unreachable.
        const unreachable: string[] = [];
        for (const stored of list) {
            // Revoked mid-pass: the stored requests are cleared, and must not come back.
            if (!this.canPersist()) return;
            if (unreachable.indexOf(stored.url) >= 0) continue;
            // Taken out of storage only as it is sent: the rest of the backlog survives the page going away mid-pass,
            // and another tab's pass cannot send it too (gone already when that tab got there first).
            const entry = takePersisted(stored.id, config);
            if (!entry) continue;
            let body: Uint8Array;
            try {
                body = fromBase64(entry.body);
            } catch {
                continue;
            }
            const request: OutgoingRequest = {
                exporter: entry.exporter,
                url: entry.url,
                contentType: entry.contentType,
                headers: headersFor ? headersFor(entry.exporter, entry.url) : null,
                body,
                time: entry.time,
            };
            const resent = (this.resent = { entry, keepalive: fitsKeepalive(request), restored: false });
            let result: Attempted;
            try {
                result = await this.attempt(request, false, 1);
            } finally {
                this.resent = null;
            }
            // Put back while the page was hidden: its outcome is known now.
            if (resent.restored) takePersisted(entry.id, config);
            if (result.outcome !== 'failed') {
                countRequest(result.outcome);
                continue;
            }
            // Partly accepted (an Azure 206), the backend is up: the rest of its backlog still goes. What is left is
            // stored again either way - counted as kept when it was first stored, it is counted again only if lost.
            if (result.remaining === request) unreachable.push(entry.url);
            if ((await this.persist(result.remaining)) === 'dropped') countRequest('dropped');
        }
    }

    /**
     * The page is being hidden, maybe for good: a stored request being resent without keepalive would be cancelled
     * with it, so it goes back to storage for the next visit. Should it get through after all (the page was only
     * hidden), its outcome takes it out again. Synchronous, as everything that runs on pagehide must be.
     * @param unloading Whether the page is being unloaded for good (a pagehide outside the back/forward cache).
     */
    pageHidden(unloading = false): void {
        if (unloading) this.unloading = true;
        const resent = this.resent;
        if (!resent || resent.keepalive || resent.restored || !this.canPersist()) return;
        resent.restored = true;
        savePersisted(loadPersisted().concat([resent.entry]), this.options.persistence);
    }
}
