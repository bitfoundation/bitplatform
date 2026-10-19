import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { installFetch } from '../../Bit.Belemetry/Scripts/instrumentations/fetch.ts';
import { createPropagationMatcher } from '../../Bit.Belemetry/Scripts/instrumentations/http.ts';
import { installXhr } from '../../Bit.Belemetry/Scripts/instrumentations/xhr.ts';
import { Transport } from '../../Bit.Belemetry/Scripts/transport.ts';
import { fakeContext, mockFetch } from './helpers.js';

const TRACEPARENT = /^00-[0-9a-f]{32}-[0-9a-f]{16}-01$/;
const headerOf = call => new Headers(call.init && call.init.headers).get('traceparent');

let uninstall = null;
let fetch;

function setup(config = {}, exporterUrls = ['https://collector.test']) {
    fetch = mockFetch(200);
    vi.stubGlobal('fetch', fetch);
    const ctx = fakeContext(config, exporterUrls);
    uninstall = installFetch(ctx);
    return ctx;
}

afterEach(() => {
    if (uninstall) uninstall();
    uninstall = null;
    vi.unstubAllGlobals();
});

describe('fetch instrumentation', () => {
    it('creates a client span and adds traceparent for same-origin requests', async () => {
        const ctx = setup();
        const response = await window.fetch('/api/items?id=1', { method: 'post' });
        expect(response.status).toBe(200);
        const call = fetch.calls[0];
        expect(headerOf(call)).toMatch(TRACEPARENT);
        expect(ctx.records).toHaveLength(1);
        const span = ctx.records[0];
        expect(span).toMatchObject({ name: 'POST', kind: 3, feature: 16, scope: 'bit.belemetry.http' });
        expect(headerOf(call)).toBe('00-' + span.traceId + '-' + span.spanId + '-01');
        expect(span.attributes).toMatchObject({
            'http.request.method': 'POST', 'url.full': location.origin + '/api/items?id=1', 'server.address': location.hostname, 'http.response.status_code': 200,
        });
        expect(span.endTime).toBeGreaterThanOrEqual(span.startTime);
    });

    it('traces but does not propagate to other origins by default', async () => {
        const ctx = setup();
        await window.fetch('https://third.party.test/x');
        expect(headerOf(fetch.calls[0])).toBeNull();
        expect(ctx.records).toHaveLength(1);
        expect(ctx.records[0].attributes['server.port']).toBe(443);
    });

    it('propagates to regex and "*" targets', async () => {
        setup({ dependencies: { propagateTraceContextTo: ['^https://api\\.example\\.com/'] } });
        await window.fetch('https://api.example.com/x');
        await window.fetch('https://other.example.com/x');
        await window.fetch('/same');
        expect(fetch.calls.map(headerOf).map(Boolean)).toEqual([true, false, false]);
        expect(createPropagationMatcher(['*'])('https://anything.test/')).toBe(true);
    });

    it('skips exporter URLs, ignored URLs and the transport itself', async () => {
        const ctx = setup();
        await window.fetch('https://collector.test/v1/traces', { method: 'POST' });
        await window.fetch('/_framework/blazor.boot.json');
        await new Transport({ persistence: { enabled: false }, canPersist: () => false }).send({ exporter: 'x', url: 'https://elsewhere.test/', contentType: 'a', body: new Uint8Array(1) });
        expect(ctx.records).toHaveLength(0);
        expect(fetch.calls.every(c => headerOf(c) === null)).toBe(true);
    });

    it('skips requests that already carry traceparent', async () => {
        const ctx = setup();
        const existing = '00-11111111111111111111111111111111-2222222222222222-01';
        await window.fetch('/api', { headers: { traceparent: existing } });
        await window.fetch(new Request(location.origin + '/api2', { headers: { traceparent: existing } }));
        expect(ctx.records).toHaveLength(0);
        expect(headerOf(fetch.calls[0])).toBe(existing);
    });

    it('records errors for failed responses and rejections', async () => {
        const ctx = setup();
        fetch = mockFetch(500, new TypeError('Failed to fetch'));
        vi.stubGlobal('fetch', fetch);
        uninstall();
        uninstall = installFetch(ctx);
        await window.fetch('/a');
        await expect(window.fetch('/b')).rejects.toThrow('Failed to fetch');
        expect(ctx.records[0]).toMatchObject({ status: { code: 2 }, attributes: { 'error.type': '500' } });
        expect(ctx.records[1]).toMatchObject({ status: { code: 2, message: 'Failed to fetch' }, attributes: { 'error.type': 'TypeError' } });
    });

    it('keeps the request headers of a Request input when propagating', async () => {
        setup();
        await window.fetch(new Request(location.origin + '/r', { headers: { 'x-a': '1' } }));
        const headers = new Headers(fetch.calls[0].init.headers);
        expect(headers.get('x-a')).toBe('1');
        expect(headers.get('traceparent')).toMatch(TRACEPARENT);
    });

    it('restores the original fetch on uninstall', () => {
        setup();
        expect(window.fetch).not.toBe(fetch);
        uninstall();
        uninstall = null;
        expect(window.fetch).toBe(fetch);
    });
});

describe('XHR instrumentation', () => {
    let restore;
    beforeEach(() => {
        restore = null;
    });
    afterEach(() => restore && restore());

    it('adds traceparent for same-origin and skips requests that set their own', () => {
        const headers = [];
        const proto = XMLHttpRequest.prototype;
        const spy = vi.spyOn(proto, 'setRequestHeader').mockImplementation(function (name, value) {
            headers.push([name, value]);
        });
        const send = vi.spyOn(proto, 'send').mockImplementation(() => { });
        vi.spyOn(proto, 'open').mockImplementation(() => { });
        const ctx = fakeContext();
        restore = installXhr(ctx);

        const a = new XMLHttpRequest();
        a.open('GET', '/x');
        a.send();
        expect(headers[0][0]).toBe('traceparent');
        expect(headers[0][1]).toMatch(TRACEPARENT);

        const b = new XMLHttpRequest();
        b.open('GET', '/y');
        b.setRequestHeader('traceparent', 'x');
        b.send();
        expect(headers).toHaveLength(2);
        expect(send).toHaveBeenCalledTimes(2);
        expect(spy).toHaveBeenCalledTimes(2);
    });
});
