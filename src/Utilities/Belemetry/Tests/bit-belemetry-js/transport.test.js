import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';
import { createOtlpExporter } from '../../Bit.Belemetry/Scripts/exporters/otlp.ts';
import { encodeUtf8 } from '../../Bit.Belemetry/Scripts/exporters/protobuf.ts';
import { fromBase64, gunzip, loadPersisted, parseRetryAfter, toBase64, Transport } from '../../Bit.Belemetry/Scripts/transport.ts';
import { log, mockFetch, span } from './helpers.js';

const persistence = { enabled: true, maxBytes: 1048576, maxAgeHours: 48 };

function transport(options = {}) {
    const sleeps = [];
    const t = new Transport({ persistence, canPersist: () => true, sleep: ms => (sleeps.push(ms), Promise.resolve()), ...options });
    t.sleeps = sleeps;
    return t;
}

const request = (overrides = {}) => ({ exporter: 'otlp', url: 'https://collector.test/v1/logs', contentType: 'application/x-protobuf', body: new Uint8Array([1, 2, 3]), ...overrides });

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

    it('uses keepalive above 60 KB only during unload', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        const t = transport();
        await t.send(request({ body: new Uint8Array(70 * 1024) }));
        await t.send(request({ body: new Uint8Array(70 * 1024) }), true);
        expect(fetch.calls.map(c => c.init.keepalive)).toEqual([false, true]);
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

    it('drops non-retryable failures without persisting', async () => {
        const fetch = mockFetch(400);
        vi.stubGlobal('fetch', fetch);
        await transport().send(request());
        expect(fetch.calls).toHaveLength(1);
        expect(loadPersisted()).toEqual([]);
    });

    it('persists after exhausted retries and resends later', async () => {
        vi.stubGlobal('fetch', mockFetch(500));
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
        vi.stubGlobal('fetch', mockFetch(500));
        await transport({ canPersist: () => false }).send(request());
        expect(localStorage.getItem('bit-belemetry-queue')).toBeNull();
    });

    it('persists the uncompressed body of a gzip request', async () => {
        vi.stubGlobal('fetch', mockFetch(500));
        await transport().send(request({ contentEncoding: 'gzip', body: new Uint8Array([9]), raw: new Uint8Array([1, 2]) }));
        expect(Array.from(fromBase64(loadPersisted()[0].body))).toEqual([1, 2]);
    });

    it('evicts old and oversized entries', async () => {
        vi.stubGlobal('fetch', mockFetch(500));
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
