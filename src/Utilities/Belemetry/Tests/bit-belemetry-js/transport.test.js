import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { createOtlpExporter } from '../../Bit.Belemetry/Scripts/exporters/otlp.ts';
import { encodeUtf8 } from '../../Bit.Belemetry/Scripts/exporters/protobuf.ts';
import { _keepaliveInFlight, fromBase64, gunzip, loadPersisted, parseRetryAfter, toBase64, Transport } from '../../Bit.Belemetry/Scripts/transport.ts';
import { log, mockFetch, span } from './helpers.js';

const persistence = { enabled: true, maxBytes: 1048576, maxAgeHours: 48 };

function transport(options = {}) {
    const sleeps = [];
    const t = new Transport({ persistence, canPersist: () => true, sleep: ms => (sleeps.push(ms), Promise.resolve()), ...options });
    t.sleeps = sleeps;
    return t;
}

const request = (overrides = {}) => ({ exporter: 'otlp', url: 'https://collector.test/v1/logs', contentType: 'application/x-protobuf', body: new Uint8Array([1, 2, 3]), ...overrides });

// An entry of the stored queue, n-th oldest (the queue is kept oldest first), with a body of the byte n.
const storedAt = Date.now() - 60000;
const storedRequest = (n, overrides = {}) => ({ id: 'r' + n, exporter: 'otlp', url: 'https://collector.test/v1/logs', contentType: 'c', body: toBase64(new Uint8Array([n])), time: storedAt + n, ...overrides });

beforeEach(() => {
    localStorage.clear();
});

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('Transport', () => {
    it('posts with headers, keepalive for small bodies and no credentials', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request({ headers: { 'x-api-key': 'k' }, contentEncoding: 'gzip' }));
        expect(fetch.calls).toHaveLength(1);
        const init = fetch.calls[0].init;
        expect(init).toMatchObject({ method: 'POST', keepalive: true, credentials: 'omit' });
        expect(init.headers).toEqual({ 'x-api-key': 'k', 'Content-Type': 'application/x-protobuf', 'Content-Encoding': 'gzip' });
    });

    it('sends a body above the keepalive budget without keepalive, and stores it instead during unload', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        const t = transport();
        await t.send(request({ body: new Uint8Array(70 * 1024) }));
        expect(fetch.calls.map(c => c.init.keepalive)).toEqual([false]);

        // A browser would refuse it with keepalive and cancel it without: kept for the next visit.
        await t.send(request({ body: new Uint8Array(70 * 1024) }), true);
        expect(fetch.calls).toHaveLength(1);
        expect(loadPersisted()).toHaveLength(1);

        // Without persistence a plain request is still the best chance - the page may only be hidden.
        await transport({ canPersist: () => false }).send(request({ body: new Uint8Array(70 * 1024) }), true);
        expect(fetch.calls.map(c => c.init.keepalive)).toEqual([false, false]);
    });

    it('shares the 64 KiB keepalive budget between requests in flight, and gives it back', async () => {
        let release;
        const pending = new Promise(resolve => (release = resolve));
        const calls = [];
        vi.stubGlobal('fetch', (url, init) => {
            calls.push(init);
            return pending.then(() => new Response(null, { status: 200 }));
        });
        const t = transport();
        const first = t.send(request({ body: new Uint8Array(40 * 1024) }));
        const second = t.send(request({ body: new Uint8Array(40 * 1024) }));
        const third = t.send(request({ body: new Uint8Array(10 * 1024) }));
        expect(calls.map(c => c.keepalive)).toEqual([true, false, true]);
        expect(_keepaliveInFlight()).toBe(50 * 1024);

        release();
        await Promise.all([first, second, third]);
        expect(_keepaliveInFlight()).toBe(0);
    });

    it('claims keepalive for at most 9 requests in flight, however small', async () => {
        let release;
        const pending = new Promise(resolve => (release = resolve));
        const calls = [];
        vi.stubGlobal('fetch', (url, init) => {
            calls.push(init);
            return pending.then(() => new Response(null, { status: 200 }));
        });
        const t = transport();
        const sends = Array.from({ length: 10 }, () => t.send(request()));
        expect(calls.map(c => c.keepalive)).toEqual([...Array(9).fill(true), false]);
        release();
        await Promise.all(sends);
        await t.send(request());
        expect(calls[10].keepalive).toBe(true);
    });

    it('resends the stored requests in order and stops at the first failure', async () => {
        const stored = [1, 2, 3].map(n => storedRequest(n));
        localStorage.setItem('bit-belemetry-queue', JSON.stringify(stored));

        const down = mockFetch(503);
        vi.stubGlobal('fetch', down);
        const t = transport();
        await t.resendPersisted();
        expect(down.calls).toHaveLength(1); // one attempt, then the backlog waits
        expect(t.sleeps).toEqual([]);
        expect(loadPersisted().map(e => Array.from(fromBase64(e.body))[0])).toEqual([1, 2, 3]);
        expect(loadPersisted().map(e => e.time)).toEqual(stored.map(e => e.time));

        const up = mockFetch(200);
        vi.stubGlobal('fetch', up);
        await t.resendPersisted();
        expect(up.calls.map(c => Array.from(c.init.body)[0])).toEqual([1, 2, 3]);
        expect(localStorage.getItem('bit-belemetry-queue')).toBeNull();
    });

    it('keeps the backlog stored while a resend pass is in flight', async () => {
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([1, 2, 3].map(n => storedRequest(n))));
        let release;
        vi.stubGlobal('fetch', () => new Promise(resolve => (release = () => resolve(new Response(null, { status: 200 })))));
        const pass = transport().resendPersisted();

        // Only the request on the wire is out of storage: the page may go away now and lose nothing else.
        expect(loadPersisted().map(e => e.id)).toEqual(['r2', 'r3']);
        release();
        await vi.waitFor(() => expect(loadPersisted().map(e => e.id)).toEqual(['r3']));
        release();
        await vi.waitFor(() => expect(loadPersisted()).toEqual([]));
        release();
        await pass;
    });

    it('holds back only the backlog of the backend that is down', async () => {
        const down = 'https://down.test/v1/logs';
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([
            storedRequest(1, { url: down }), storedRequest(2), storedRequest(3, { url: down }), storedRequest(4, { exporter: 'azuremonitor' }),
        ]));
        const calls = [];
        vi.stubGlobal('fetch', url => (calls.push(url), Promise.resolve(new Response(null, { status: url === down ? 503 : 200 }))));
        await transport().resendPersisted();

        expect(calls).toEqual([down, 'https://collector.test/v1/logs', 'https://collector.test/v1/logs']);
        expect(loadPersisted().map(e => [e.url, Array.from(fromBase64(e.body))[0]])).toEqual([[down, 1], [down, 3]]);
    });

    it('goes on after an Azure 206 during a resend, and keeps only the items to retry', async () => {
        const items = [{ a: 0 }, { a: 1 }];
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([
            storedRequest(1, { exporter: 'azuremonitor', url: 'https://ingest.test/v2.1/track', body: toBase64(encodeUtf8(JSON.stringify(items))) }),
            storedRequest(2, { exporter: 'azuremonitor', url: 'https://ingest.test/v2.1/track' }),
        ]));
        const fetch = mockFetch({ status: 206, body: JSON.stringify({ itemsReceived: 2, itemsAccepted: 1, errors: [{ index: 1, statusCode: 503 }] }) }, 200);
        vi.stubGlobal('fetch', fetch);
        await transport().resendPersisted();

        expect(fetch.calls).toHaveLength(2); // the backend answered: the next stored request still goes
        const stored = loadPersisted();
        expect(stored).toHaveLength(1);
        expect(JSON.parse(new TextDecoder().decode(fromBase64(stored[0].body)))).toEqual([{ a: 1 }]);
        expect(stored[0].time).toBe(storedRequest(1).time);
    });

    it('puts a request being resent without keepalive back into storage when the page is hidden', async () => {
        const big = toBase64(new Uint8Array(70 * 1024));
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([storedRequest(1, { body: big })]));
        let release;
        vi.stubGlobal('fetch', () => new Promise(resolve => (release = () => resolve(new Response(null, { status: 200 })))));
        const t = transport();
        const pass = t.resendPersisted();
        expect(loadPersisted()).toEqual([]);

        // The page unloads: the request is cancelled with it, so the next visit must find it.
        t.pageHidden();
        expect(loadPersisted().map(e => e.id)).toEqual(['r1']);

        // It was only hidden, and the request got through: out of storage again.
        release();
        await pass;
        expect(loadPersisted()).toEqual([]);
    });

    it('leaves a request being resent with keepalive alone when the page is hidden', async () => {
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([storedRequest(1)]));
        let release;
        vi.stubGlobal('fetch', () => new Promise(resolve => (release = () => resolve(new Response(null, { status: 200 })))));
        const t = transport();
        const pass = t.resendPersisted();
        t.pageHidden(); // keepalive outlives the page: storing it again would only send it twice
        expect(loadPersisted()).toEqual([]);
        release();
        await pass;
    });

    it('does not store a keepalive request whose fetch fails as the page unloads', async () => {
        // Chromium fails the fetch of every keepalive request in flight when its page unloads, and sends them all
        // the same: stored too, the next visit would deliver each one twice.
        const rejects = [];
        vi.stubGlobal('fetch', () => new Promise((_, reject) => rejects.push(reject)));
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([storedRequest(1)]));
        const t = transport();
        const pass = t.resendPersisted();
        const sent = t.send(request(), true);
        t.pageHidden(true);
        for (const reject of rejects) reject(new TypeError('Failed to fetch'));
        await Promise.all([pass, sent]);
        expect(loadPersisted()).toEqual([]);
    });

    it('still stores a request that fails while the page is only hidden, or offline as it unloads', async () => {
        vi.stubGlobal('fetch', mockFetch(new TypeError('Failed to fetch')));
        const t = transport();
        t.pageHidden(false); // a tab switch: the page lives on, and the failure is real
        await t.send(request(), true);
        expect(loadPersisted()).toHaveLength(1);

        vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);
        t.pageHidden(true);
        await t.send(request(), true);
        expect(loadPersisted()).toHaveLength(2);
    });

    it('retries retryable statuses with backoff and honors Retry-After', async () => {
        const fetch = mockFetch(503, { status: 429, headers: { 'Retry-After': '2' } }, 200);
        vi.stubGlobal('fetch', fetch);
        const t = transport();
        await t.send(request());
        expect(fetch.calls).toHaveLength(3);
        expect(t.sleeps[0]).toBeGreaterThanOrEqual(800);
        expect(t.sleeps[0]).toBeLessThanOrEqual(1200);
        expect(t.sleeps[1]).toBe(2000);
        expect(loadPersisted()).toEqual([]);
    });

    it('retries what each backend asks to: OTLP only 429/502/503/504, Azure Monitor 408/439/500 too', async () => {
        for (const status of [408, 439, 500]) {
            const otlp = mockFetch(status);
            vi.stubGlobal('fetch', otlp);
            await transport().send(request());
            expect(otlp.calls).toHaveLength(1); // the OTLP spec: "MUST NOT be retried"

            const azure = mockFetch(status, 200);
            vi.stubGlobal('fetch', azure);
            await transport().send(request({ exporter: 'azuremonitor1', contentType: 'application/json' }));
            expect(azure.calls).toHaveLength(2);
        }
        expect(loadPersisted()).toEqual([]);
    });

    it('drops non-retryable failures without persisting', async () => {
        const fetch = mockFetch(400);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request());
        expect(fetch.calls).toHaveLength(1);
        expect(loadPersisted()).toEqual([]);
    });

    it('persists after exhausted retries and resends later', async () => {
        vi.stubGlobal('fetch', mockFetch(503));
        const t = transport({ headersFor: () => ({ 'x-api-key': 'k' }) });
        await t.send(request());
        const stored = loadPersisted();
        expect(stored).toHaveLength(1);
        expect(stored[0]).toMatchObject({ exporter: 'otlp', url: 'https://collector.test/v1/logs', contentType: 'application/x-protobuf' });
        expect(Array.from(fromBase64(stored[0].body))).toEqual([1, 2, 3]);
        expect(Object.keys(stored[0]).sort()).toEqual(['body', 'contentType', 'exporter', 'id', 'time', 'url']);

        const ok = mockFetch(200);
        vi.stubGlobal('fetch', ok);
        await t.resendPersisted();
        expect(ok.calls).toHaveLength(1);
        expect(ok.calls[0].init.headers['x-api-key']).toBe('k');
        expect(Array.from(ok.calls[0].init.body)).toEqual([1, 2, 3]);
        expect(localStorage.getItem('bit-belemetry-queue')).toBeNull();
    });

    it('persists network failures', async () => {
        const fetch = mockFetch(new TypeError('Failed to fetch'));
        vi.stubGlobal('fetch', fetch);
        await transport().send(request());
        expect(fetch.calls).toHaveLength(3);
        expect(loadPersisted()).toHaveLength(1);
    });

    it('persists immediately when offline or during unload', async () => {
        vi.stubGlobal('fetch', mockFetch(new TypeError('offline')));
        const online = vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);
        const t = transport();
        await t.send(request());
        expect(t.sleeps).toEqual([]);
        online.mockReturnValue(true);
        await t.send(request(), true);
        expect(t.sleeps).toEqual([]);
        expect(loadPersisted()).toHaveLength(2);
    });

    it('drops instead of persisting without consent', async () => {
        vi.stubGlobal('fetch', mockFetch(503));
        await transport({ canPersist: () => false }).send(request());
        expect(localStorage.getItem('bit-belemetry-queue')).toBeNull();
    });

    it('persists the uncompressed body of a gzip request', async () => {
        vi.stubGlobal('fetch', mockFetch(503));
        await transport().send(request({ contentEncoding: 'gzip', body: new Uint8Array([9]), raw: new Uint8Array([1, 2]) }));
        expect(Array.from(fromBase64(loadPersisted()[0].body))).toEqual([1, 2]);
    });

    it('evicts old and oversized entries', async () => {
        vi.stubGlobal('fetch', mockFetch(503));
        const old = { id: 'x', exporter: 'otlp', url: 'u', contentType: 'c', body: 'AA==', time: Date.now() - 49 * 3600000 };
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([old]));
        const t = transport({ persistence: { ...persistence, maxBytes: 600 } });
        for (let i = 0; i < 5; i++) await t.send(request({ body: new Uint8Array(100).fill(i) }));
        const stored = loadPersisted();
        expect(stored.find(e => e.id === 'x')).toBeUndefined();
        expect(JSON.stringify(stored).length).toBeLessThanOrEqual(600);
        expect(stored.length).toBeGreaterThan(0);
        expect(Array.from(fromBase64(stored[stored.length - 1].body))[0]).toBe(4);
    });

    it('retries only the failed items of an Azure 206 response', async () => {
        const items = [{ a: 0 }, { a: 1 }, { a: 2 }];
        const fetch = mockFetch({ status: 206, body: JSON.stringify({ itemsReceived: 3, itemsAccepted: 1, errors: [{ index: 1, statusCode: 500 }, { index: 2, statusCode: 400 }] }) }, 200);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request({ exporter: 'azureMonitor', contentType: 'application/json', body: encodeUtf8(JSON.stringify(items)) }));
        expect(fetch.calls).toHaveLength(2);
        expect(JSON.parse(new TextDecoder().decode(fetch.calls[1].init.body))).toEqual([{ a: 1 }]);
    });

    it('reads at most 4 MB of an Azure 206 response', async () => {
        const items = [{ a: 0 }, { a: 1 }];
        const chunk = encodeUtf8('x'.repeat(64 * 1024));
        const total = 8 * 1024 * 1024;
        let read = 0;
        // Counts what the reader pulls: a body read whole before the size check would pull all of it.
        const body = new ReadableStream({
            pull(controller) {
                if (read >= total) {
                    controller.close();
                    return;
                }
                read += chunk.length;
                controller.enqueue(chunk);
            },
        });
        const fetch = mockFetch({ status: 206, body }, 200);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request({ exporter: 'azureMonitor', contentType: 'application/json', body: encodeUtf8(JSON.stringify(items)) }));
        expect(fetch.calls).toHaveLength(1);
        expect(read).toBeLessThan(total);
        // The stream may pull a chunk ahead of the one that crosses the limit.
        expect(read).toBeLessThanOrEqual(4 * 1024 * 1024 + 2 * chunk.length);
    });

    it('reads an Azure 206 response of exactly 4 MB, and retries its failed items', async () => {
        const items = [{ a: 0 }, { a: 1 }];
        const head = '{"itemsReceived":2,"itemsAccepted":1,"errors":[{"index":1,"statusCode":503}],"padding":"';
        const tail = '"}';
        const body = head + 'x'.repeat(4 * 1024 * 1024 - head.length - tail.length) + tail;
        expect(encodeUtf8(body).length).toBe(4 * 1024 * 1024);
        const fetch = mockFetch({ status: 206, body }, 200);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request({ exporter: 'azureMonitor', contentType: 'application/json', body: encodeUtf8(JSON.stringify(items)) }));
        expect(fetch.calls).toHaveLength(2);
        expect(JSON.parse(new TextDecoder().decode(fetch.calls[1].init.body))).toEqual([{ a: 1 }]);
    });

    it('neither retries nor stores a request once consent is revoked while it waits', async () => {
        const fetch = mockFetch(503);
        vi.stubGlobal('fetch', fetch);
        let revocations = 0;
        // The revoke lands during the backoff, and clears the stored requests as it does.
        await transport({ revocations: () => revocations, sleep: () => (revocations++, Promise.resolve()) }).send(request());
        expect(fetch.calls).toHaveLength(1);
        expect(loadPersisted()).toEqual([]);
    });

    it('withdraws a request before its first attempt once one of its features is revoked, and only then', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        // Interactions (4) was revoked once since the marks were taken.
        const t = transport({ revocations: features => (features & 4 ? 1 : 0) });
        await t.send(request(), false, { features: 4 | 128, revocations: 0 });
        expect(fetch.calls).toHaveLength(0);
        expect(loadPersisted()).toEqual([]);
        await t.send(request(), false, { features: 1 | 128, revocations: 0 });
        expect(fetch.calls).toHaveLength(1);
    });

    it('does not store a resent request again once consent is revoked while it is sent', async () => {
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([storedRequest(1)]));
        let revocations = 0;
        vi.stubGlobal('fetch', async () => {
            revocations++;
            localStorage.removeItem('bit-belemetry-queue');
            return new Response(null, { status: 503 });
        });
        await transport({ revocations: () => revocations }).resendPersisted();
        expect(loadPersisted()).toEqual([]);
    });

    it('does not put a request being resent back into storage once consent is revoked', async () => {
        const big = toBase64(new Uint8Array(70 * 1024));
        localStorage.setItem('bit-belemetry-queue', JSON.stringify([storedRequest(1, { body: big })]));
        let release;
        vi.stubGlobal('fetch', () => new Promise(resolve => (release = () => resolve(new Response(null, { status: 200 })))));
        let revocations = 0;
        const t = transport({ revocations: () => revocations });
        const pass = t.resendPersisted();
        revocations++;
        t.pageHidden();
        expect(loadPersisted()).toEqual([]);
        release();
        await pass;
    });

    it('parses Retry-After', () => {
        expect(parseRetryAfter('3')).toBe(3000);
        expect(parseRetryAfter(null)).toBeNull();
        expect(parseRetryAfter(new Date(Date.now() + 10000).toUTCString())).toBeGreaterThan(8000);
    });

    it('round-trips base64', () => {
        const bytes = new Uint8Array(70000).map((_, i) => i % 256);
        expect(Array.from(fromBase64(toBase64(bytes)))).toEqual(Array.from(bytes));
    });
});

describe('OTLP exporter', () => {
    it('posts protobuf traces and logs to the signal paths', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        const t = transport();
        const exporter = createOtlpExporter({ type: 'otlp', endpoint: 'https://collector.test', headers: { a: 'b' } }, t, '1.0.0');
        await exporter.export({ resource: {}, spans: [span()], logs: [log()] }, false);
        await t.idle();
        expect(fetch.calls.map(c => [c.url, c.init.headers['Content-Type']])).toEqual([
            ['https://collector.test/v1/traces', 'application/x-protobuf'],
            ['https://collector.test/v1/logs', 'application/x-protobuf'],
        ]);
        expect(fetch.calls[0].init.headers.a).toBe('b');
    });

    it('posts JSON with gzip when configured', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        const t = transport();
        const exporter = createOtlpExporter({ type: 'otlp', endpoint: 'https://collector.test', protocol: 'http/json', compression: 'gzip' }, t, '1.0.0');
        await exporter.export({ resource: {}, spans: [], logs: [log()] }, false);
        await t.idle();
        const init = fetch.calls[0].init;
        expect(init.headers).toMatchObject({ 'Content-Type': 'application/json', 'Content-Encoding': 'gzip' });
        const json = JSON.parse(new TextDecoder().decode(await gunzip(init.body)));
        expect(json.resourceLogs[0].scopeLogs[0].logRecords[0].body).toEqual({ stringValue: 'hello' });
    });

    it('sends uncompressed and synchronously during unload', () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        const exporter = createOtlpExporter({ ...resolveConfig().exporters[0], type: 'otlp', endpoint: 'https://collector.test', compression: 'gzip' }, transport(), '1.0.0');
        exporter.export({ resource: {}, spans: [span()], logs: [log()] }, true);
        expect(fetch.calls).toHaveLength(2);
        expect(fetch.calls[0].init.headers['Content-Encoding']).toBeUndefined();
        expect(fetch.calls[0].init.keepalive).toBe(true);
    });
});
