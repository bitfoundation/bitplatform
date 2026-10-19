import { describe, expect, it } from 'vitest';
import {
    createAzureExporter, formatDuration, logToEnvelope, parseConnectionString, rebuildPartial, severityLevel, spanToEnvelope, toEnvelopes,
} from '../../Bit.Belemetry/Scripts/exporters/azure.ts';
import { gunzip } from '../../Bit.Belemetry/Scripts/transport.ts';
import { encodeUtf8 } from '../../Bit.Belemetry/Scripts/exporters/protobuf.ts';
import { log, span } from './helpers.js';

const ctx = { iKey: 'ikey-1', maskIp: true, sdkVersion: '10.6.2' };
const resource = {
    'service.name': 'shop', 'service.namespace': 'contoso', 'service.instance.id': 'inst-1', 'service.version': '2.0.0',
    'user_agent.original': 'UA/1.0', 'browser.language': 'en-US',
};
const ids = { 'session.id': 's'.repeat(32), 'enduser.pseudo.id': 'p'.repeat(32), 'user.id': 'alice' };

describe('connection string', () => {
    it('parses the key and the ingestion endpoint', () => {
        expect(parseConnectionString('InstrumentationKey=abc;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://x/'))
            .toEqual({ iKey: 'abc', endpoint: 'https://westeurope-5.in.applicationinsights.azure.com' });
    });

    it('defaults the endpoint and ignores key case', () => {
        expect(parseConnectionString('instrumentationkey=abc')).toEqual({ iKey: 'abc', endpoint: 'https://dc.services.visualstudio.com' });
    });

    it('rejects a string without a key', () => {
        expect(parseConnectionString('IngestionEndpoint=https://x')).toBeNull();
        expect(parseConnectionString('')).toBeNull();
    });
});

describe('envelopes', () => {
    it('formats durations as TimeSpan "c" with 7 fractional digits', () => {
        expect(formatDuration(123.456)).toBe('00:00:00.1234560');
        expect(formatDuration(((24 + 2) * 3600 + 3 * 60 + 4.5) * 1000)).toBe('1.02:03:04.5000000');
        expect(formatDuration(0)).toBe('00:00:00.0000000');
    });

    it('maps severity numbers to levels', () => {
        expect([1, 8, 9, 12, 13, 16, 17, 20, 21].map(severityLevel)).toEqual([0, 0, 1, 1, 2, 2, 3, 3, 4]);
    });

    it('writes the common envelope and tags', () => {
        const e = logToEnvelope(log({ eventName: 'checkout', attributes: { ...ids, cart: 3 } }), resource, ctx);
        expect(e).toMatchObject({ ver: 1, name: 'Microsoft.ApplicationInsights.Event', time: '2023-11-14T22:13:20.123Z', sampleRate: 100, iKey: 'ikey-1' });
        expect(e.tags).toEqual({
            'ai.cloud.role': '[contoso]/shop',
            'ai.cloud.roleInstance': 'inst-1',
            'ai.application.ver': '2.0.0',
            'ai.user.agent': 'UA/1.0',
            'ai.device.locale': 'en-US',
            'ai.device.type': 'Browser',
            'ai.location.ip': '0.0.0.0',
            'ai.internal.sdkVersion': 'bitblmjs:10.6.2',
            'ai.session.id': 's'.repeat(32),
            'ai.user.id': 'p'.repeat(32),
            'ai.user.authUserId': 'alice',
        });
        expect(e.data).toEqual({ baseType: 'EventData', baseData: { ver: 2, name: 'checkout', properties: { cart: '3' } } });
    });

    it('maps page visit times to the PageVisitTime event of the Application Insights JS SDK', () => {
        const e = logToEnvelope(log({
            eventName: 'browser.page_visit_time', body: 'Home page',
            attributes: { 'url.full': 'https://x.test/', 'url.path': '/', 'page.title': 'Home page', 'page.visit.duration': 1234, extra: 'y' },
        }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.Event');
        expect(e.data).toEqual({
            baseType: 'EventData',
            baseData: { ver: 2, name: 'PageVisitTime', measurements: { PageVisitTime: 1234 }, properties: { extra: 'y', PageName: 'Home page', PageUrl: 'https://x.test/' } },
        });
        const untitled = logToEnvelope(log({ eventName: 'browser.page_visit_time', attributes: { 'url.full': 'https://x.test/p', 'url.path': '/p', 'page.title': '' } }), resource, ctx);
        expect(untitled.data.baseData.properties.PageName).toBe('/p');
        expect(untitled.data.baseData.measurements.PageVisitTime).toBe(0);
    });

    it('uses the bare service name without a namespace and omits the ip tag without maskIp', () => {
        const e = logToEnvelope(log(), { 'service.name': 'shop' }, { ...ctx, maskIp: false });
        expect(e.tags['ai.cloud.role']).toBe('shop');
        expect(e.tags['ai.location.ip']).toBeUndefined();
    });

    it('maps client spans to RemoteDependency', () => {
        const s = span({ parentSpanId: '00f067aa0ba902b7', attributes: { ...span().attributes, 'server.address': 'api.example.com', 'server.port': 8443, ...ids } });
        const e = spanToEnvelope(s, resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.RemoteDependency');
        expect(e.tags).toMatchObject({ 'ai.operation.id': s.traceId, 'ai.operation.parentId': '00f067aa0ba902b7' });
        expect(e.tags['ai.operation.name'], 'Only requests and page views name the operation, as in C#.').toBeUndefined();
        expect(e.data.baseType).toBe('RemoteDependencyData');
        expect(e.data.baseData).toEqual({
            ver: 2, id: s.spanId, name: 'GET', resultCode: '200', data: 'https://api.example.com/items', duration: '00:00:00.2502500',
            success: true, type: 'Ajax', target: 'api.example.com:8443', properties: { 'http.request.method': 'GET' },
        });
    });

    it('marks failed dependencies and falls back to the URL authority', () => {
        const e = spanToEnvelope(span({ attributes: { 'url.full': 'https://x.test/a', 'http.response.status_code': 404 } }), resource, ctx);
        expect(e.data.baseData).toMatchObject({ success: false, resultCode: '404', target: 'x.test', type: 'Ajax' });
        const inproc = spanToEnvelope(span({ name: 'documentLoad', kind: 1, attributes: {}, status: { code: 2 } }), resource, ctx);
        expect(inproc.data.baseData).toMatchObject({ type: 'InProc', success: false, data: 'documentLoad' });
    });

    it('maps server spans to Request', () => {
        const e = spanToEnvelope(span({ kind: 2, attributes: { 'url.full': 'https://x.test/a', 'http.response.status_code': 201 } }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.Request');
        expect(e.data.baseType).toBe('RequestData');
        expect(e.data.baseData).toMatchObject({ ver: 2, name: 'GET', responseCode: '201', success: true, url: 'https://x.test/a' });
        expect(e.tags['ai.operation.name']).toBe('GET');
        const routed = spanToEnvelope(span({ kind: 2, attributes: { 'http.request.method': 'GET', 'http.route': '/a/{id}' } }), resource, ctx);
        expect(routed.data.baseData.name).toBe('GET /a/{id}');
        expect(routed.tags['ai.operation.name']).toBe('GET /a/{id}');
    });

    it('maps exception logs to Exception, excluding exception.* from properties', () => {
        const e = logToEnvelope(log({
            severityNumber: 17, eventName: 'exception', body: 'bad',
            attributes: { 'exception.type': 'TypeError', 'exception.message': 'bad', 'exception.stacktrace': 'at x', 'exception.escaped': true, extra: 'y', ...ids },
            traceId: '0af7651916cd43dd8448eb211c80319c', spanId: 'b7ad6b7169203331',
        }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.Exception');
        expect(e.tags['ai.operation.parentId']).toBe('b7ad6b7169203331');
        expect(e.data.baseData).toEqual({
            ver: 2, severityLevel: 3, properties: { extra: 'y' },
            exceptions: [{ id: 1, outerId: 0, typeName: 'TypeError', message: 'bad', hasFullStack: true, stack: 'at x' }],
        });
    });

    it('maps page views to PageView', () => {
        const e = logToEnvelope(log({
            eventName: 'browser.page_view', body: 'Home',
            attributes: { 'url.full': 'https://x.test/', 'url.path': '/', 'page.title': 'Home page', 'browser.navigation.type': 'push', ...ids },
        }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.PageView');
        expect(e.data.baseType).toBe('PageViewData');
        expect(e.tags['ai.operation.name']).toBe('Home page');
        const data = e.data.baseData;
        expect(data).toMatchObject({ ver: 2, name: 'Home page', url: 'https://x.test/', duration: '00:00:00' });
        expect(data.id).toMatch(/^[0-9a-f]{16}$/);
        expect(data.properties).toEqual({ 'url.path': '/', 'browser.navigation.type': 'push' });
        const untitled = logToEnvelope(log({ eventName: 'browser.page_view', attributes: { 'url.full': 'https://x.test/p', 'url.path': '/p', 'page.title': '' } }), resource, ctx);
        expect(untitled.data.baseData.name).toBe('/p');
    });

    it('maps web vitals to Metric', () => {
        const e = logToEnvelope(log({
            eventName: 'browser.web_vital', body: 'LCP',
            attributes: { 'web_vital.name': 'LCP', 'web_vital.value': 1234.5, 'web_vital.rating': 'good', 'web_vital.delta': 1234.5 },
        }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.Metric');
        expect(e.data.baseData).toEqual({
            ver: 2, metrics: [{ name: 'LCP', kind: 0, value: 1234.5, count: 1 }], properties: { 'web_vital.rating': 'good', 'web_vital.delta': '1234.5' },
        });
    });

    it('maps other logs to Message, truncated', () => {
        const e = logToEnvelope(log({ severityNumber: 13, body: 'x'.repeat(40000), attributes: { ['k'.repeat(200)]: 'v'.repeat(9000), arr: [1, 2] } }), resource, ctx);
        expect(e.name).toBe('Microsoft.ApplicationInsights.Message');
        expect(e.data.baseData.message).toHaveLength(32768);
        expect(e.data.baseData.severityLevel).toBe(2);
        expect(e.data.baseData.properties['k'.repeat(150)]).toHaveLength(8192);
        expect(e.data.baseData.properties.arr).toBe('[1,2]');
    });

    it('converts a whole batch', () => {
        expect(toEnvelopes({ resource, spans: [span()], logs: [log(), log()] }, ctx)).toHaveLength(3);
    });
});

describe('exporter', () => {
    const connectionString = 'InstrumentationKey=ikey-1;IngestionEndpoint=https://dc.test/';
    const sent = () => {
        const requests = [];
        return { requests, transport: { send: request => { requests.push(request); return Promise.resolve(); } } };
    };
    const items = request => JSON.parse(new TextDecoder().decode(request.raw || request.body));
    const batch = { resource, spans: [span()], logs: [log({ eventName: 'checkout' })] };

    it('sends every signal uncompressed by default', async () => {
        const { requests, transport } = sent();
        await createAzureExporter({ type: 'azureMonitor', connectionString }, transport, '1.0.0').export(batch, false);
        expect(requests).toHaveLength(1);
        expect(requests[0]).toMatchObject({ url: 'https://dc.test/v2.1/track', contentType: 'application/json', contentEncoding: null });
        expect(items(requests[0]).map(e => e.name)).toEqual(['Microsoft.ApplicationInsights.RemoteDependency', 'Microsoft.ApplicationInsights.Event']);
    });

    it('sends only the configured signals', async () => {
        const traces = sent();
        await createAzureExporter({ type: 'azureMonitor', connectionString, signals: 1 }, traces.transport, '1.0.0').export(batch, false);
        expect(items(traces.requests[0]).map(e => e.name)).toEqual(['Microsoft.ApplicationInsights.RemoteDependency']);

        const logs = sent();
        await createAzureExporter({ type: 'azureMonitor', connectionString, signals: 2 }, logs.transport, '1.0.0').export({ ...batch, logs: [] }, false);
        expect(logs.requests, 'Nothing left to send: no request at all.').toHaveLength(0);
    });

    it('compresses with gzip, except during unload', async () => {
        const zipped = sent();
        await createAzureExporter({ type: 'azureMonitor', connectionString, compression: 'gzip' }, zipped.transport, '1.0.0').export(batch, false);
        const request = zipped.requests[0];
        expect(request.contentEncoding).toBe('gzip');
        expect(JSON.parse(new TextDecoder().decode(await gunzip(request.body)))).toEqual(items(request));

        const unload = sent();
        await createAzureExporter({ type: 'azureMonitor', connectionString, compression: 'gzip' }, unload.transport, '1.0.0').export(batch, true);
        expect(unload.requests[0].contentEncoding).toBeNull();
    });
});

describe('206 partial success', () => {
    const items = [{ i: 0 }, { i: 1 }, { i: 2 }, { i: 3 }];
    const request = { exporter: 'azureMonitor', url: 'https://dc/v2.1/track', contentType: 'application/json', body: encodeUtf8(JSON.stringify(items)) };

    it('keeps only the items with retryable status codes', () => {
        const response = JSON.stringify({ itemsReceived: 4, itemsAccepted: 1, errors: [{ index: 1, statusCode: 400 }, { index: 2, statusCode: 429 }, { index: 3, statusCode: 503 }] });
        const next = rebuildPartial(request, response);
        expect(JSON.parse(new TextDecoder().decode(next.body))).toEqual([{ i: 2 }, { i: 3 }]);
        expect(next.url).toBe(request.url);
    });

    it('returns null when nothing is retryable', () => {
        expect(rebuildPartial(request, JSON.stringify({ errors: [{ index: 0, statusCode: 400 }] }))).toBeNull();
        expect(rebuildPartial(request, JSON.stringify({ itemsReceived: 4, itemsAccepted: 4 }))).toBeNull();
    });
});
