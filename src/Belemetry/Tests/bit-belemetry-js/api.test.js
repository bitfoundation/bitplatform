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
