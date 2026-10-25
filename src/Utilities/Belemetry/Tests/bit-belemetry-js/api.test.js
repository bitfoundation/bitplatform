import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mockFetch } from './helpers.js';


let api;

async function load() {
    vi.resetModules();
    api = await import('../../Bit.Belemetry/Scripts/bit-belemetry.ts');
    return api;
}

const relayConfig = (overrides = {}) => ({
    service: { name: 'shop', version: '1.0.0' },
    sdkVersion: '10.6.2',
    transport: 'relay',
    performance: { webVitals: false, documentLoad: false },
    dependencies: { fetch: false, xhr: false },
    ...overrides,
});

function fakeDotNet(drain = null) {
    const calls = [];
    return {
        calls,
        invokeMethodAsync: vi.fn(async (id, arg) => {
            calls.push([id, JSON.parse(JSON.stringify(arg))]);
        }),
        invokeMethod: vi.fn(id => {
            calls.push([id]);
            return drain;
        }),
    };
}

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    document.head.innerHTML = '';
    history.replaceState(null, '', '/');
});

afterEach(() => {
    if (api) api._reset();
    api = null;
    vi.unstubAllGlobals();
});

describe('auto-start', () => {
    it('starts from the config element on module evaluation', async () => {
        document.head.innerHTML = '<script type="application/json" id="bit-belemetry-config">'
            + JSON.stringify(relayConfig({ consent: { default: 4095 } })) + '</script>';
        await load();
        expect(api._getState()).not.toBeNull();
        expect(api.getConsent()).toBe(4095);
        expect(window.BitBelemetry.start).toBe(api.start);
        // The initial page view is already queued.
        expect(api._getState().pipeline.size).toBe(1);
    });

    it('ignores an invalid config element', async () => {
        document.head.innerHTML = '<script type="application/json" id="bit-belemetry-config">{not json</script>';
        await load();
        expect(api._getState()).toBeNull();
    });

    it('does not start without the element', async () => {
        await load();
        expect(api._getState()).toBeNull();
        expect(api.getContext()).toEqual({ consent: 0, decided: false, sessionId: null, pseudoId: null, userId: null });
    });
});

describe('public API', () => {
    it('holds records until consent and relays the granted ones with the record model', async () => {
        await load();
        api.start(relayConfig());
        const dotnet = fakeDotNet();
        api.attachDotNet(dotnet, { relay: true });
        api.trackEvent('checkout', { items: 2, bad: { nested: true } });
        api.trackException('TypeError', 'mail a@b.io', 'at x');
        await api.flush();
        expect(dotnet.calls.filter(c => c[0] === 'Belemetry.Relay')).toHaveLength(0);

        api.setConsent(1 /* Errors */);
        await api.flush();
        const relays = dotnet.calls.filter(c => c[0] === 'Belemetry.Relay').map(c => c[1]);
        expect(relays).toHaveLength(1);
        const batch = relays[0];
        expect(batch.resource).toMatchObject({ 'service.name': 'shop', 'telemetry.sdk.name': 'bit-belemetry', 'telemetry.sdk.language': 'webjs', 'telemetry.sdk.version': '10.6.2' });
        expect(batch.spans).toEqual([]);
        expect(batch.logs).toHaveLength(1);
        const record = batch.logs[0];
        expect(Object.keys(record).sort()).toEqual(['attributes', 'body', 'eventName', 'feature', 'scope', 'severityNumber', 'severityText', 'spanId', 'time', 'traceId']);
        expect(record).toMatchObject({ eventName: 'exception', feature: 1, severityNumber: 17, body: 'mail [email]' });
        expect(record.attributes['session.id']).toMatch(/^[0-9a-f]{32}$/);
        expect(record.attributes['enduser.pseudo.id']).toBeUndefined();
    });

    it('syncs the context with .NET and keeps user ids only with UserTracking', async () => {
        await load();
        api.start(relayConfig());
        const dotnet = fakeDotNet();
        api.attachDotNet(dotnet, { relay: true });
        const contexts = () => dotnet.calls.filter(c => c[0] === 'Belemetry.ContextChanged').map(c => c[1]);
        expect(contexts()).toHaveLength(1);
        expect(contexts()[0]).toMatchObject({ consent: 0, decided: false, pseudoId: null, userId: null });

        api.setUser('alice');
        expect(api.getContext().userId).toBeNull();

        api.setConsent(4095);
        api.setUser('alice');
        const context = api.getContext();
        expect(context).toMatchObject({ consent: 4095, decided: true, userId: 'alice' });
        expect(context.pseudoId).toMatch(/^[0-9a-f]{32}$/);
        expect(contexts().at(-1)).toEqual(context);
        expect(api.isConsentDecided()).toBe(true);

        api.trackEvent('e');
        await api.flush();
        const relayed = dotnet.calls.filter(c => c[0] === 'Belemetry.Relay').flatMap(c => c[1].logs);
        const event = relayed.find(l => l.eventName === 'e');
        expect(event.attributes).toMatchObject({ 'user.id': 'alice', 'enduser.pseudo.id': context.pseudoId, 'session.id': context.sessionId });

        api.setConsent(1);
        expect(api.getContext()).toMatchObject({ pseudoId: null, userId: null });
        expect(localStorage.getItem('bit-belemetry-pseudo-id')).toBeNull();
    });

    it('does not add an id the configuration blocks', async () => {
        await load();
        api.start(relayConfig({ consent: { default: 4095 }, privacy: { blockedAttributes: ['user.id', 'session.id'] } }));
        const dotnet = fakeDotNet();
        api.attachDotNet(dotnet, { relay: true });
        api.setUser('alice');
        api.trackEvent('e');
        await api.flush();
        const event = dotnet.calls.filter(c => c[0] === 'Belemetry.Relay').flatMap(c => c[1].logs).find(l => l.eventName === 'e');
        expect(event.attributes['enduser.pseudo.id']).toMatch(/^[0-9a-f]{32}$/);
        expect(event.attributes['user.id']).toBeUndefined();
        expect(event.attributes['session.id']).toBeUndefined();
        expect(api.getContext().userId).toBe('alice'); // still the context .NET is kept in step with
    });

    it('purges queued records of revoked features', async () => {
        await load();
        api.start(relayConfig({ consent: { default: 0 } }));
        api.setConsent(4095);
        api.trackEvent('a');
        api.trackException('E', 'm');
        expect(api._getState().pipeline.size).toBe(3); // with the initial page view
        api.setConsent(1);
        expect(api._getState().pipeline.size).toBe(1);
    });

    it('exports directly over OTLP and drains .NET on unload', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        await load();
        api.start({
            ...relayConfig({ transport: 'direct' }),
            consent: { default: 4095 },
            exporters: [{ type: 'otlp', endpoint: 'https://collector.test', protocol: 'http/json' }],
        });
        const body = btoa('{"resourceMetrics":[]}');
        const dotnet = fakeDotNet([{ exporter: 'otlp', url: 'https://collector.test/v1/metrics', contentType: 'application/json', contentEncoding: null, headers: {}, body }]);
        api.attachDotNet(dotnet, { relay: false, unloadDrain: true });
        // Attaching flushed the queued initial page view.
        expect(fetch.calls).toHaveLength(1);
        api.trackEvent('bye');
        window.dispatchEvent(new Event('pagehide'));
        // Everything was started synchronously inside the pagehide handler.
        const calls = fetch.calls.slice(1);
        expect(calls.map(c => c.url)).toEqual(['https://collector.test/v1/logs', 'https://collector.test/v1/metrics']);
        expect(calls.every(c => c.init.keepalive)).toBe(true);
        expect(dotnet.invokeMethod).toHaveBeenCalledWith('Belemetry.DrainForUnload');
        const logs = JSON.parse(new TextDecoder().decode(calls[0].init.body)).resourceLogs[0].scopeLogs.flatMap(s => s.logRecords);
        expect(logs.map(l => l.eventName)).toEqual(['bye']);
        expect(new TextDecoder().decode(calls[1].init.body)).toBe('{"resourceMetrics":[]}');
    });

    it('sends .NET requests through the transport', async () => {
        const fetch = mockFetch(200);
        vi.stubGlobal('fetch', fetch);
        await load();
        await api.sendRequests([{ exporter: 'azureMonitor', url: 'https://dc.test/v2.1/track', contentType: 'application/json', headers: { a: 'b' }, body: btoa('[]') }]);
        expect(fetch.calls[0].init.headers).toMatchObject({ a: 'b', 'Content-Type': 'application/json' });
    });

    it('reports the grant within the configured features', async () => {
        await load();
        api.start(relayConfig({ features: 1 | 128 }));
        api.setConsent(4095);
        expect(api.getConsent()).toBe(1 | 128);
        expect(api.getContext().consent).toBe(1 | 128);
    });

    it('recognizes a relative exporter endpoint as its own URL', async () => {
        await load();
        api.start({
            ...relayConfig({ transport: 'direct' }),
            exporters: [{ type: 'otlp', endpoint: '/otlp', headers: { 'x-api-key': 'k' } }],
            dependencies: { fetch: false, xhr: false, ignoreUrls: [] },
        });
        const ctx = api._getState().ctx;
        expect(ctx.isExporterUrl(location.origin + '/otlp/v1/traces')).toBe(true);
        expect(ctx.isExporterUrl('/otlp/v1/logs')).toBe(true);
        expect(ctx.isExporterUrl(location.origin + '/api/orders')).toBe(false);
    });

    it('retries the stored requests every retryIntervalMs', async () => {
        vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
        try {
            const fetch = mockFetch(200);
            vi.stubGlobal('fetch', fetch);
            await load();
            api.start({
                ...relayConfig({ transport: 'direct' }),
                consent: { default: 4095 },
                persistence: { retryIntervalMs: 5000 },
            });
            localStorage.setItem('bit-belemetry-queue', JSON.stringify([
                { id: '1', exporter: 'otlp', url: 'https://collector.test/v1/logs', contentType: 'application/json', body: btoa('{}'), time: Date.now() },
            ]));
            expect(fetch.calls).toHaveLength(0);
            await vi.advanceTimersByTimeAsync(5000);
            expect(fetch.calls.map(c => c.url)).toEqual(['https://collector.test/v1/logs']);
        } finally {
            vi.useRealTimers();
        }
    });

    it('is idempotent and updates consent defaults on a second start', async () => {
        await load();
        api.start(relayConfig());
        const state = api._getState();
        api.start(relayConfig({ consent: { default: 1 } }));
        expect(api._getState()).toBe(state);
        expect(api.getConsent()).toBe(1);
    });

    it('never throws into the page', async () => {
        await load();
        expect(() => api.trackEvent('x')).not.toThrow();
        api.start(relayConfig());
        expect(() => api.trackEvent(null)).not.toThrow();
        expect(() => api.trackPageView(undefined, 'http://[bad')).not.toThrow();
        expect(() => api.attachDotNet({ invokeMethodAsync: () => { throw new Error('x'); } }, { relay: true })).not.toThrow();
        expect(() => api.setConsent(4095)).not.toThrow();
        await expect(api.flush()).resolves.toBeUndefined();
    });
});

describe('a second start', () => {
    it('adds the ignored URLs it brings (a WebAssembly client\'s exporters) to those of the first', async () => {
        await load();
        vi.stubGlobal('fetch', mockFetch(200));
        api.start(relayConfig({ consent: { default: 4095 }, dependencies: { fetch: true, xhr: false } }));
        api.start(relayConfig({ consent: { default: 4095 }, dependencies: { ignoreUrls: ['/_framework/', '^https://wasm-collector\\.test/'] } }));
        expect(api._getState().config.dependencies.ignoreUrls).toEqual(['/_framework/', '/_blazor', '/_content/', '^https://wasm-collector\\.test/']);

        const before = api._getState().pipeline.size;
        await window.fetch('https://wasm-collector.test/v1/logs', { method: 'POST' });
        await window.fetch('https://api.test/items');
        const spans = api._getState().pipeline.queue.slice(before);
        expect(spans.map(s => s.attributes['url.full'])).toEqual(['https://api.test/items']);
    });
});

describe('getStats', () => {
    it('says what was dropped and why, and what became of the requests', async () => {
        await load();
        const fetch = mockFetch(200, 400);
        vi.stubGlobal('fetch', fetch);
        api.start({
            service: { name: 'shop' },
            performance: { webVitals: false, documentLoad: false },
            dependencies: { fetch: false, xhr: false },
            consent: { default: 0, pendingLimit: 2 },
            exporters: [{ type: 'otlp', endpoint: 'https://collector.test' }],
        });
        // Held before the decision: the initial page view plus 3 events, in a hold of 2.
        api.trackEvent('a');
        api.trackEvent('b');
        api.trackEvent('c');
        api.addProcessor(record => record.eventName !== 'c');
        // Errors only: the held page view and event are not consented.
        api.setConsent(1);
        api.trackEvent('after');
        for (let i = 0; i < 3; i++) api.trackException('TypeError', 'boom', 'TypeError: boom\n    at loop (app.js:1:2)');
        await api.flush();
        await api._getState().transport.idle();

        // pending_full: the page view and 'a' made room for 'b' and 'c'; not_consented: 'b' and 'c' at the decision,
        // then 'after'; error_limit: the loop's two duplicates.
        expect(api.getStats()).toEqual({
            dropped: { pending_full: 2, not_consented: 3, error_limit: 2 },
            requests: { delivered: 1 },
            queued: 0,
            pending: 0,
        });

        api.trackException('RangeError', 'other');
        await api.flush();
        await api._getState().transport.idle();
        expect(api.getStats().requests).toEqual({ delivered: 1, rejected: 1 });
    });
});

describe('processors and global attributes', () => {
    const relayed = dotnet => dotnet.calls.filter(c => c[0] === 'Belemetry.Relay').flatMap(c => c[1].logs);

    it('runs the processors at export, so one added after the start still sees the initial page view', async () => {
        await load();
        api.start(relayConfig({ consent: { default: 4095 } }));
        const seen = [];
        api.addProcessor(record => {
            seen.push(record.eventName);
            record.attributes['app.processed'] = true;
        });
        const remove = api.addProcessor(record => record.eventName !== 'noise');
        api.addProcessor(() => {
            throw new Error('broken processor');
        });
        api.trackEvent('noise');
        api.trackEvent('checkout');

        const dotnet = fakeDotNet();
        api.attachDotNet(dotnet, { relay: true });
        await api.flush();
        expect(seen).toEqual(['browser.page_view', 'noise', 'checkout']);
        expect(relayed(dotnet).map(l => l.eventName)).toEqual(['browser.page_view', 'checkout']);
        expect(relayed(dotnet).every(l => l.attributes['app.processed'] === true)).toBe(true);

        remove();
        api.trackEvent('noise');
        await api.flush();
        expect(relayed(dotnet).map(l => l.eventName)).toEqual(['browser.page_view', 'checkout', 'noise']);
    });

    it('adds global attributes to every record, scrubbed, never over the record or the consent-governed ids', async () => {
        await load();
        api.setAttributes({ 'app.tenant': 'contoso', 'app.contact': 'ops@contoso.test', 'user.id': 'sneaky', 'event.kind': 'global' });
        api.start(relayConfig({ consent: { default: 4095 }, pageViews: { trackHashChanges: false } }));
        api.trackEvent('checkout', { 'event.kind': 'own' });
        api.setAttributes({ 'app.tenant': null, 'app.flag': true });
        api.trackEvent('later');

        const dotnet = fakeDotNet();
        api.attachDotNet(dotnet, { relay: true });
        await api.flush();
        const [, checkout, later] = relayed(dotnet);
        expect(checkout.attributes).toMatchObject({ 'app.tenant': 'contoso', 'app.contact': '[email]', 'event.kind': 'own' });
        expect(checkout.attributes['user.id']).toBeUndefined();
        expect(later.attributes['app.tenant']).toBeUndefined();
        expect(later.attributes['app.flag']).toBe(true);

        api.setAttributes(null);
        api.trackEvent('last');
        await api.flush();
        expect(relayed(dotnet).pop().attributes['app.contact']).toBeUndefined();
    });
});
