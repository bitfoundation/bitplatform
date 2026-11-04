import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { log, span } from './helpers.js';
import { DiagnosticRecorder, templatePath, templateUrl } from '../../Bit.Belemetry/Scripts/diagnostics.ts';

let api;

async function load() {
    vi.resetModules();
    api = await import('../../Bit.Belemetry/Scripts/bit-belemetry.ts');
    return api;
}

const config = (diagnostics = {}, overrides = {}) => ({
    service: { name: 'shop', version: '1.0.0' },
    sdkVersion: '11.0.0',
    transport: 'relay',
    performance: { webVitals: false, documentLoad: false },
    dependencies: { fetch: false, xhr: false },
    consent: { default: 4095 },
    diagnostics: { enabled: true, maxEvents: 50, categories: 1023, includeSuccessfulActivity: true, persist: false, triggerBrowserErrors: false, allowedQueryParameters: [], ...diagnostics },
    ...overrides,
});

const recorderConfig = (overrides = {}) => ({
    enabled: true, maxEvents: 5, categories: 1023, includeSuccessfulActivity: true, includeLogMessages: false, persist: false,
    triggerBrowserErrors: false, allowedQueryParameters: [], ...overrides,
});

beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    document.head.innerHTML = '';
    history.replaceState(null, '', '/orders/10423');
});

afterEach(() => {
    if (api) api._reset();
    api = null;
    vi.unstubAllGlobals();
});

describe('templates (C# BelemetryUrlTemplate)', () => {
    it('replaces the segments that identify an instance', () => {
        expect(templatePath('/orders/10423/lines/3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e')).toBe('/orders/{n}/lines/{guid}');
        expect(templatePath('/files/a1b2c3d4e5f60718293a')).toBe('/files/{hex}');
        expect(templatePath('/users/jane@example.com/profile')).toBe('/users/{value}/profile');
        expect(templatePath('/products/blue-shoes')).toBe('/products/blue-shoes');
    });

    it('keeps the origin and the allowlisted query parameters only, never credentials or the fragment', () => {
        expect(templateUrl('https://jane:pw@api.shop.test/orders/42?page=2&token=x#top', ['page'])).toBe('https://api.shop.test/orders/{n}?page=2');
        expect(templateUrl('/orders/42?token=x', [])).toBe('/orders/{n}');
    });
});

describe('DiagnosticRecorder', () => {
    it('keeps metadata only, converts lazily, and is bounded', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        const failed = span({ status: { code: 2 }, attributes: { 'http.request.method': 'POST', 'url.full': 'https://api.example.com/orders/42?token=x', 'http.response.status_code': 503, 'http.request.header.authorization': 'Bearer x' } });
        recorder.record(failed);
        for (let i = 0; i < 10; i++) recorder.record(log({ eventName: 'browser.user_interaction', scope: 'bit.belemetry.interactions', feature: 4, attributes: { 'event.type': 'click', 'target.element': 'button', 'target.text': 'Pay jane' } }));

        const snapshot = recorder.snapshot(4095, { 'browser.language': 'en-US', 'user_agent.original': 'UA' });
        expect(snapshot.events).toHaveLength(5);
        expect(snapshot.dropped.capacity).toBe(6);
        expect(snapshot.resource).toEqual({ 'browser.language': 'en-US' });
        const click = snapshot.events[0];
        expect(click.cat).toBe('browser');
        expect(click.attrs).toEqual({ 'event.type': 'click', 'target.element': 'button' });
    });

    it('turns HTTP spans into failure events without payloads or headers', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        recorder.record(span({ status: { code: 2 }, attributes: { 'http.request.method': 'POST', 'url.full': 'https://api.example.com/orders/42?token=x', 'http.response.status_code': 503, 'http.request.header.authorization': 'Bearer x' } }));
        const e = recorder.snapshot(4095, {}).events[0];
        expect(e).toMatchObject({ cat: 'http', name: 'POST', outcome: 'failure', sev: 17, traceId: '0af7651916cd43dd8448eb211c80319c', feature: 16 });
        expect(e.attrs).toEqual({ 'http.request.method': 'POST', 'http.response.status_code': 503, 'url.template': 'https://api.example.com/orders/{n}' });
    });

    it('keeps JS errors with their type, message and stack', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        const seen = [];
        recorder.onException = (type, message) => seen.push([type, message]);
        recorder.record(log({ eventName: 'exception', scope: 'bit.belemetry.errors', feature: 1, severityNumber: 17, attributes: { 'exception.type': 'TypeError', 'exception.message': 'x is undefined', 'exception.stacktrace': 'at f (https://a/app.js:1:2)' } }));
        const e = recorder.snapshot(4095, {}).events[0];
        expect(e.exc).toEqual({ type: 'TypeError', message: 'x is undefined', stack: 'at f (https://a/app.js:1:2)' });
        expect(seen).toEqual([['TypeError', 'x is undefined']]);
    });

    it('page views carry a path template, never the URL', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        recorder.record(log({ eventName: 'browser.page_view', scope: 'bit.belemetry.page_views', feature: 2, attributes: { 'url.full': 'https://shop.test/orders/42?email=a@b.c', 'url.path': '/orders/42', 'browser.navigation.type': 'push' } }));
        const e = recorder.snapshot(4095, {}).events[0];
        expect(e).toMatchObject({ cat: 'navigation', name: 'page_view', route: '/orders/{n}', attrs: { 'browser.navigation.type': 'push' } });
        expect(JSON.stringify(e)).not.toContain('a@b.c');
    });

    it('console messages are left out unless log messages are allowed', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        recorder.record(log({ scope: 'bit.belemetry.console', feature: 1, severityNumber: 17, body: 'user typed secret' }));
        expect(recorder.snapshot(4095, {}).events[0].msg).toBeUndefined();
        const verbose = new DiagnosticRecorder(recorderConfig({ includeLogMessages: true }));
        verbose.record(log({ scope: 'bit.belemetry.console', feature: 1, severityNumber: 17, body: 'shown' }));
        expect(verbose.snapshot(4095, {}).events[0].msg).toBe('shown');
    });

    it('snapshots only what the user granted, and a purge removes the rest', () => {
        const recorder = new DiagnosticRecorder(recorderConfig());
        recorder.record(log({ eventName: 'exception', scope: 'bit.belemetry.errors', feature: 1, severityNumber: 17, attributes: { 'exception.type': 'E' } }));
        recorder.record(log({ eventName: 'browser.page_view', scope: 'bit.belemetry.page_views', feature: 2, attributes: { 'url.path': '/' } }));
        expect(recorder.snapshot(1, {}).events.map(e => e.cat)).toEqual(['exception']);
        recorder.purge(1);
        expect(recorder.snapshot(4095, {}).events.map(e => e.cat)).toEqual(['navigation']);
        expect(recorder.snapshot(4095, {}).dropped.consent_revoked).toBe(1);
    });

    it('drops successful activity when asked to keep failures only', () => {
        const recorder = new DiagnosticRecorder(recorderConfig({ includeSuccessfulActivity: false }));
        recorder.record(span());
        recorder.record(span({ status: { code: 2 } }));
        expect(recorder.snapshot(4095, {}).events.map(e => e.outcome)).toEqual(['failure']);
    });

    it('keeps only the configured categories', () => {
        const recorder = new DiagnosticRecorder(recorderConfig({ categories: 4 }));
        recorder.record(span());
        recorder.record(log({ eventName: 'exception', scope: 'bit.belemetry.errors', feature: 1, severityNumber: 17, attributes: {} }));
        expect(recorder.snapshot(4095, {}).events.map(e => e.cat)).toEqual(['exception']);
    });

    it('survives a reload when asked, marked as the previous page, once', () => {
        const recorder = new DiagnosticRecorder(recorderConfig({ persist: true }));
        recorder.record(log({ eventName: 'exception', scope: 'bit.belemetry.errors', feature: 1, severityNumber: 17, attributes: { 'exception.type': 'E' } }));
        recorder.persist(4095);

        const next = new DiagnosticRecorder(recorderConfig({ persist: true }));
        next.restore();
        const events = next.snapshot(4095, {}).events;
        expect(events).toHaveLength(1);
        expect(events[0]).toMatchObject({ cat: 'exception', prev: true });
        expect(sessionStorage.getItem('bit-belemetry-diagnostics')).toBeNull();

        const malformed = new DiagnosticRecorder(recorderConfig({ persist: true }));
        sessionStorage.setItem('bit-belemetry-diagnostics', '{"v":1,"events":[{"time":"x"},null,7]}');
        malformed.restore();
        expect(malformed.size).toBe(0);
    });
});

describe('getDiagnostics', () => {
    it('is null while diagnostics are off', async () => {
        await load();
        api.start(config({}, { diagnostics: null }));
        expect(api.getDiagnostics()).toBeNull();
    });

    it('returns the timeline after the processors ran, and forgets what a processor dropped', async () => {
        await load();
        api.addProcessor(record => record.eventName !== 'secret');
        api.start(config());
        api.trackEvent('visible');
        api.trackEvent('secret');
        api.trackException('TypeError', 'boom');

        const snapshot = api.getDiagnostics();
        expect(snapshot.v).toBe(1);
        expect(snapshot.events.map(e => e.name)).toEqual(['page_view', 'visible', 'exception']);
        expect(snapshot.events[0].route).toBe('/orders/{n}');
        expect(snapshot.dropped.filtered).toBe(1);
    });

    it('holds nothing of a revoked feature', async () => {
        await load();
        api.start(config());
        api.trackException('TypeError', 'boom');
        api.setConsent(4095 & ~1);
        expect(api.getDiagnostics().events.some(e => e.cat === 'exception')).toBe(false);
    });

    it('records reconnection transitions of the Blazor reconnect dialog', async () => {
        await load();
        api.start(config());
        const modal = document.createElement('dialog');
        modal.id = 'components-reconnect-modal';
        document.body.appendChild(modal);
        modal.dispatchEvent(new CustomEvent('components-reconnect-state-changed', { detail: { state: 'retrying', currentAttempt: 2 } }));
        modal.dispatchEvent(new CustomEvent('components-reconnect-state-changed', { detail: { state: 'failed' } }));
        modal.remove();

        const circuit = api.getDiagnostics().events.filter(e => e.cat === 'circuit');
        expect(circuit.map(e => e.name)).toEqual(['reconnect.retrying', 'reconnect.failed']);
        expect(circuit[0].attrs).toEqual({ 'reconnect.state': 'retrying', 'reconnect.attempt': 2 });
        expect(circuit[1].outcome).toBe('failure');
    });

    it('offers JS errors to .NET as triggers, at most every ten seconds', async () => {
        await load();
        api.start(config({ triggerBrowserErrors: true }));
        const calls = [];
        api.attachDotNet({ invokeMethodAsync: vi.fn(async (id, arg) => calls.push([id, arg])), invokeMethod: vi.fn() }, { relay: true });

        api.trackException('TypeError', 'first');
        api.trackException('RangeError', 'second');

        const triggers = calls.filter(([id]) => id === 'Belemetry.DiagnosticTrigger');
        expect(triggers).toEqual([['Belemetry.DiagnosticTrigger', { type: 'TypeError', message: 'first' }]]);
    });

    it('keeps its timeline for a session sampled out of the export', async () => {
        await load();
        api.start(config({}, { sampling: { sessionRatio: 0 } }));
        api.trackException('TypeError', 'boom');
        expect(api.getDiagnostics().events.some(e => e.cat === 'exception')).toBe(true);
        expect(api._getState().pipeline.size).toBe(0);
    });

    it('hands a server circuit the timeline as a Blob, which Blazor turns into a stream reference itself', async () => {
        await load();
        api.start(config());
        api.trackException('TypeError', 'boom');
        const blob = api.getDiagnosticsStream();
        expect(blob).toBeInstanceOf(Blob);
        const snapshot = JSON.parse(await blob.text());
        expect(snapshot.events.some(e => e.cat === 'exception')).toBe(true);
        api._reset();
        api.start(config({}, { diagnostics: null }));
        expect(api.getDiagnosticsStream()).toBeNull();
    });

    it('runs the processors on the records of a sampled-out session before they reach the timeline', async () => {
        await load();
        api.addProcessor(record => {
            if (record.eventName === 'secret') return false;
            if (record.attributes['exception.message']) record.attributes['exception.message'] = '(rewritten)';
        });
        api.start(config({}, { sampling: { sessionRatio: 0 } }));
        api.trackEvent('secret');
        api.trackException('TypeError', 'jane@example.com was here');

        const events = api.getDiagnostics().events;
        expect(events.some(e => e.name === 'secret')).toBe(false);
        expect(events.find(e => e.cat === 'exception').exc.message).toBe('(rewritten)');
    });

    it('records page hides only with PageViews', async () => {
        await load();
        api.start(config({}, { consent: { default: 1 } }));
        window.dispatchEvent(new Event('pagehide'));
        expect(api.getDiagnostics().events.some(e => e.name.startsWith('page.'))).toBe(false);
    });

    it('clearDiagnostics forgets everything', async () => {
        await load();
        api.start(config());
        api.trackException('TypeError', 'boom');
        api.clearDiagnostics();
        expect(api.getDiagnostics().events).toEqual([]);
    });
});

describe('downloadFile', () => {
    it('saves bytes or a .NET stream reference under a safe name', async () => {
        await load();
        const created = [];
        vi.stubGlobal('URL', Object.assign(Object.create(URL), { createObjectURL: blob => { created.push(blob); return 'blob:x'; }, revokeObjectURL: () => undefined }));
        const clicks = [];
        const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function () { clicks.push(this.download); });

        await api.downloadFile('../../evil name.zip', new Uint8Array([80, 75]));
        await api.downloadFile('bundle.zip', { arrayBuffer: async () => new Uint8Array([1, 2, 3]).buffer });

        expect(clicks).toEqual(['..-..-evil-name.zip', 'bundle.zip']);
        expect(created).toHaveLength(2);
        expect(created[1].size).toBe(3);
        click.mockRestore();
    });
});
