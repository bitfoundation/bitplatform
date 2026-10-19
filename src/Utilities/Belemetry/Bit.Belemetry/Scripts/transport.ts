import type { PersistenceConfig } from './config';
import { isRetryable, rebuildPartial, RETRYABLE_STATUS } from './exporters/azure';
import { newSpanId } from './ids';
import { debug } from './log';
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

export const MAX_ATTEMPTS = 3;
const KEEPALIVE_LIMIT = 60 * 1024;
const MAX_RETRY_AFTER_MS = 60000;

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

function post(request: OutgoingRequest, unload: boolean): Promise<Response> {
    const headers: Record<string, string> = {};
    const extra = request.headers || {};
    for (const key of Object.keys(extra)) headers[key] = extra[key];
    headers['Content-Type'] = request.contentType;
    if (request.contentEncoding) headers['Content-Encoding'] = request.contentEncoding;
    suppressed++;
    try {
        return fetch(request.url, {
            method: 'POST',
            headers,
            body: request.body as BodyInit,
            // keepalive lets the request outlive the page, but browsers cap in-flight keepalive bodies at 64 KB.
            keepalive: unload || request.body.length <= KEEPALIVE_LIMIT,
            credentials: 'omit',
        });
    } finally {
        suppressed--;
    }
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

/** Drops entries older than maxAgeHours, evicts the oldest beyond maxBytes, and writes the queue. */
export function savePersisted(list: PersistedRequest[], config: PersistenceConfig, time = Date.now()): PersistedRequest[] {
    const minTime = time - config.maxAgeHours * 3600000;
    const kept = list.filter(e => e.time >= minTime);
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

/**
 * Sends requests with fetch: keepalive for small bodies and during unload, retries with exponential backoff (and
 * Retry-After), Azure 206 partial-success handling, and persistence of what could not be sent.
 */
export class Transport {
    private inflight: Promise<void>[] = [];
    private resending = false;

    constructor(public options: TransportOptions) { }

    /** Sends a request. During unload there is one keepalive attempt and no waiting. Never rejects. */
    send(request: OutgoingRequest, unload = false): Promise<void> {
        // deliver() calls fetch before its first await, so during unload the request starts synchronously.
        const promise: Promise<void> = this.deliver(request, unload).catch(e => debug('send failed', e)).then(() => {
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

    private async deliver(request: OutgoingRequest, unload: boolean): Promise<void> {
        let current = request;
        for (let attempt = 1; ; attempt++) {
            let response: Response | null = null;
            try {
                response = await post(current, unload);
            } catch (e) {
                debug('network error', current.url, e);
            }
            let delay: number | null = null;
            if (response) {
                const status = response.status;
                if (status === 206 && current.exporter === 'azureMonitor') {
                    let next: OutgoingRequest | null = null;
                    try {
                        const text = await response.text();
                        const plain = current.raw || (current.contentEncoding === 'gzip' ? await gunzip(current.body) : null);
                        next = rebuildPartial(current, text, plain);
                    } catch {
                        // An unreadable 206 body: the accepted items are in, nothing to retry.
                    }
                    if (!next) return;
                    current = next;
                } else if (status >= 200 && status < 300) {
                    return;
                } else if (!isRetryable(status)) {
                    debug('dropped request, status', status, current.url);
                    return;
                }
                delay = parseRetryAfter(response.headers.get('Retry-After'));
            }
            const offline = !response && typeof navigator !== 'undefined' && navigator.onLine === false;
            if (unload || offline || attempt >= MAX_ATTEMPTS || (delay !== null && delay > MAX_RETRY_AFTER_MS)) {
                await this.persist(current);
                return;
            }
            const wait = delay !== null ? delay : backoff(attempt);
            await (this.options.sleep ? this.options.sleep(wait) : new Promise<void>(resolve => setTimeout(resolve, wait)));
        }
    }

    /** Stores a request for a later resend, if persistence is configured and consented; otherwise drops it. */
    async persist(request: OutgoingRequest): Promise<void> {
        if (!this.options.persistence.enabled || !this.options.canPersist()) {
            debug('dropped request (no persistence)', request.url);
            return;
        }
        let body: Uint8Array | null = request.raw || null;
        if (!body) body = !request.contentEncoding ? request.body : request.contentEncoding === 'gzip' ? await gunzip(request.body) : null;
        if (!body) return;
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
    }

    /** Resends the persisted requests (on start and when the browser comes back online). */
    resendPersisted(): Promise<void> {
        if (this.resending || !this.options.persistence.enabled || !this.options.canPersist()) return Promise.resolve();
        const list = savePersisted(loadPersisted(), this.options.persistence);
        if (!list.length) return Promise.resolve();
        removeItem('local', StorageKeys.queue);
        this.resending = true;
        const headersFor = this.options.headersFor;
        return Promise.all(list.map(entry => {
            let body: Uint8Array;
            try {
                body = fromBase64(entry.body);
            } catch {
                return undefined;
            }
            return this.send({
                exporter: entry.exporter,
                url: entry.url,
                contentType: entry.contentType,
                headers: headersFor ? headersFor(entry.exporter, entry.url) : null,
                body,
                time: entry.time,
            });
        })).then(() => {
            this.resending = false;
        });
    }
}
