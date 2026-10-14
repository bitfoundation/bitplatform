import { describe, expect, it } from 'vitest';
import { encodeLogs, encodeTraces } from '../../Bit.Belemetry/Scripts/exporters/otlp-proto.ts';
import { logsToJson, tracesToJson } from '../../Bit.Belemetry/Scripts/exporters/otlp-json.ts';
import { otlpUrls } from '../../Bit.Belemetry/Scripts/exporters/otlp.ts';
import { all, attributesOf, anyValue, decode, hex, log, one, span, str, u32, u64 } from './helpers.js';

const resource = { 'service.name': 'shop', 'telemetry.sdk.language': 'webjs' };

describe('OTLP protobuf traces', () => {
    const parent = span({
        parentSpanId: '00f067aa0ba902b7',
        status: { code: 2, message: 'boom' },
        attributes: { s: 'x', b: true, i: 42, neg: -7, d: 1.5, arr: ['a', 'b'], nums: [1, 2.5] },
        events: [{ name: 'domInteractive', time: 1700000000100, attributes: { k: 'v' } }],
    });
    const other = span({ spanId: '1111111111111111', scope: 'bit.belemetry.document_load', kind: 1, name: 'documentLoad', attributes: {} });
    const request = decode(encodeTraces(resource, [parent, other], '1.2.3'));

    it('nests resource_spans > scope_spans > spans with the v1.11 field numbers', () => {
        expect(request.map(f => f.field)).toEqual([1]);
        const resourceSpans = decode(one(request, 1).value);
        expect(attributesOf(decode(one(resourceSpans, 1).value), 1)).toEqual(resource);
        const scopes = all(resourceSpans, 2).map(s => decode(s.value));
        expect(scopes).toHaveLength(2);
        const scope = decode(one(scopes[0], 1).value);
        expect(str(one(scope, 1).value)).toBe('bit.belemetry.http');
        expect(str(one(scope, 2).value)).toBe('1.2.3');
        expect(str(one(decode(one(scopes[1], 1).value), 1).value)).toBe('bit.belemetry.document_load');
    });

    it('encodes every span field', () => {
        const resourceSpans = decode(one(request, 1).value);
        const s = decode(one(decode(all(resourceSpans, 2)[0].value), 2).value);
        expect(hex(one(s, 1).value)).toBe(parent.traceId);
        expect(hex(one(s, 2).value)).toBe(parent.spanId);
        expect(hex(one(s, 4).value)).toBe('00f067aa0ba902b7');
        expect(str(one(s, 5).value)).toBe('GET');
        expect(one(s, 6)).toMatchObject({ wire: 0, value: 3n });
        expect(one(s, 7).wire).toBe(1);
        expect(u64(one(s, 7).value)).toBe(1700000000000500000n);
        expect(u64(one(s, 8).value)).toBe(1700000000250750000n);
        expect(attributesOf(s, 9)).toEqual({ s: 'x', b: true, i: 42n, neg: -7n, d: 1.5, arr: ['a', 'b'], nums: [1n, 2.5] });
        const event = decode(one(s, 11).value);
        expect(u64(one(event, 1).value)).toBe(1700000000100000000n);
        expect(str(one(event, 2).value)).toBe('domInteractive');
        expect(attributesOf(event, 3)).toEqual({ k: 'v' });
        const status = decode(one(s, 15).value);
        expect(str(one(status, 2).value)).toBe('boom');
        expect(one(status, 3).value).toBe(2n);
        expect(one(s, 16).wire).toBe(5);
        expect(u32(one(s, 16).value)).toBe(1);
    });

    it('omits the parent and an unset status', () => {
        const resourceSpans = decode(one(request, 1).value);
        const s = decode(one(decode(all(resourceSpans, 2)[1].value), 2).value);
        expect(one(s, 4)).toBeUndefined();
        expect(one(s, 15)).toBeUndefined();
        expect(one(s, 6).value).toBe(1n);
    });
});

describe('OTLP protobuf logs', () => {
    it('encodes every log record field', () => {
        const record = log({
            severityNumber: 17, severityText: 'ERROR', body: 'failed', eventName: 'exception',
            attributes: { 'exception.type': 'TypeError' }, traceId: '0af7651916cd43dd8448eb211c80319c', spanId: 'b7ad6b7169203331', scope: 'bit.belemetry.errors',
        });
        const request = decode(encodeLogs(resource, [record, log()], '1.0.0'));
        const resourceLogs = decode(one(request, 1).value);
        expect(attributesOf(decode(one(resourceLogs, 1).value), 1)).toEqual(resource);
        const scopes = all(resourceLogs, 2).map(s => decode(s.value));
        expect(scopes.map(s => str(one(decode(one(s, 1).value), 1).value))).toEqual(['bit.belemetry.errors', 'bit.belemetry.events']);
        const l = decode(one(scopes[0], 2).value);
        expect(u64(one(l, 1).value)).toBe(1700000000123456000n);
        expect(u64(one(l, 11).value)).toBe(1700000000123456000n);
        expect(one(l, 2).value).toBe(17n);
        expect(str(one(l, 3).value)).toBe('ERROR');
        expect(anyValue(one(l, 5).value)).toBe('failed');
        expect(attributesOf(l, 6)).toEqual({ 'exception.type': 'TypeError' });
        expect(u32(one(l, 8).value)).toBe(1);
        expect(hex(one(l, 9).value)).toBe(record.traceId);
        expect(hex(one(l, 10).value)).toBe(record.spanId);
        expect(str(one(l, 12).value)).toBe('exception');

        const plain = decode(one(scopes[1], 2).value);
        expect(one(plain, 9)).toBeUndefined();
        expect(one(plain, 12)).toBeUndefined();
    });
});

describe('OTLP JSON', () => {
    it('shapes traces per the OTLP/JSON mapping', () => {
        const json = tracesToJson(resource, [span({ parentSpanId: '00f067aa0ba902b7', status: { code: 2, message: 'x' }, attributes: { i: 3, d: 0.5, b: false, a: ['q'] } })], '1.0.0');
        const rs = json.resourceSpans[0];
        expect(rs.resource.attributes).toContainEqual({ key: 'service.name', value: { stringValue: 'shop' } });
        expect(rs.scopeSpans[0].scope).toEqual({ name: 'bit.belemetry.http', version: '1.0.0' });
        const s = rs.scopeSpans[0].spans[0];
        expect(s).toMatchObject({
            traceId: '0af7651916cd43dd8448eb211c80319c', spanId: 'b7ad6b7169203331', parentSpanId: '00f067aa0ba902b7',
            name: 'GET', kind: 3, startTimeUnixNano: '1700000000000500000', endTimeUnixNano: '1700000000250750000', status: { code: 2, message: 'x' },
        });
        expect(s.attributes).toEqual([
            { key: 'i', value: { intValue: '3' } },
            { key: 'd', value: { doubleValue: 0.5 } },
            { key: 'b', value: { boolValue: false } },
            { key: 'a', value: { arrayValue: { values: [{ stringValue: 'q' }] } } },
        ]);
        // Round-trips through JSON.stringify without BigInt or loss.
        expect(JSON.parse(JSON.stringify(json))).toEqual(json);
    });

    it('shapes logs per the OTLP/JSON mapping', () => {
        const json = logsToJson(resource, [log({ eventName: 'browser.page_view', body: 'Home', traceId: '0af7651916cd43dd8448eb211c80319c' })]);
        const record = json.resourceLogs[0].scopeLogs[0].logRecords[0];
        expect(json.resourceLogs[0].scopeLogs[0].scope).toEqual({ name: 'bit.belemetry.events' });
        expect(record).toMatchObject({
            timeUnixNano: '1700000000123456000', observedTimeUnixNano: '1700000000123456000', severityNumber: 9, severityText: 'INFO',
            body: { stringValue: 'Home' }, eventName: 'browser.page_view', traceId: '0af7651916cd43dd8448eb211c80319c', flags: 1,
        });
        expect(record.spanId).toBeUndefined();
    });

    it('resolves the signal endpoints', () => {
        expect(otlpUrls({ type: 'otlp', endpoint: 'http://c:4318/' })).toEqual({ base: 'http://c:4318', traces: 'http://c:4318/v1/traces', logs: 'http://c:4318/v1/logs' });
        expect(otlpUrls({ type: 'otlp', endpoint: 'http://c:4318', tracesEndpoint: 'https://t/x', logsEndpoint: 'https://l/y' })).toMatchObject({ traces: 'https://t/x', logs: 'https://l/y' });
    });
});
