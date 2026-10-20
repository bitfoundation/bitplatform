// Shared test helpers: a tiny protobuf decoder (independent of the SDK's writer), record factories and a fake
// instrumentation context.

import { resolveConfig } from '../../Bit.Belemetry/Scripts/config.ts';

export const SRC = '../../Bit.Belemetry/Scripts';

/** Decodes one protobuf message level into [{ field, wire, value }]. Varints are BigInt, the rest raw bytes. */
export function decode(bytes) {
    const fields = [];
    let pos = 0;
    const varint = () => {
        let result = 0n;
        let shift = 0n;
        for (;;) {
            const b = bytes[pos++];
            result |= BigInt(b & 0x7f) << shift;
            if (!(b & 0x80)) return result;
            shift += 7n;
        }
    };
    while (pos < bytes.length) {
        const key = Number(varint());
        const field = key >>> 3;
        const wire = key & 7;
        let value;
        if (wire === 0) value = varint();
        else if (wire === 1) value = bytes.slice(pos, (pos += 8));
        else if (wire === 2) {
            const length = Number(varint());
            value = bytes.slice(pos, (pos += length));
        } else if (wire === 5) value = bytes.slice(pos, (pos += 4));
        else throw new Error('unsupported wire type ' + wire);
        fields.push({ field, wire, value });
    }
    return fields;
}

export const one = (fields, n) => fields.find(f => f.field === n);
export const all = (fields, n) => fields.filter(f => f.field === n);
export const str = bytes => new TextDecoder().decode(bytes);
export const hex = bytes => Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
export const u64 = bytes => new DataView(bytes.buffer, bytes.byteOffset, 8).getBigUint64(0, true);
export const u32 = bytes => new DataView(bytes.buffer, bytes.byteOffset, 4).getUint32(0, true);
export const f64 = bytes => new DataView(bytes.buffer, bytes.byteOffset, 8).getFloat64(0, true);

/** Decodes a KeyValue list (repeated field n) into a plain object of decoded AnyValues. */
export function attributesOf(fields, n) {
    const result = {};
    for (const kv of all(fields, n)) {
        const parts = decode(kv.value);
        result[str(one(parts, 1).value)] = anyValue(one(parts, 2).value);
    }
    return result;
}

export function anyValue(bytes) {
    const f = decode(bytes)[0];
    switch (f.field) {
        case 1: return str(f.value);
        case 2: return f.value === 1n;
        case 3: return BigInt.asIntN(64, f.value);
        case 4: return f64(f.value);
        case 5: return all(decode(f.value), 1).map(v => anyValue(v.value));
        default: throw new Error('unexpected AnyValue field ' + f.field);
    }
}

export function span(overrides = {}) {
    return {
        traceId: '0af7651916cd43dd8448eb211c80319c',
        spanId: 'b7ad6b7169203331',
        parentSpanId: null,
        name: 'GET',
        kind: 3,
        startTime: 1700000000000.5,
        endTime: 1700000000250.75,
        attributes: { 'http.request.method': 'GET', 'url.full': 'https://api.example.com/items', 'http.response.status_code': 200 },
        status: { code: 0 },
        events: [],
        scope: 'bit.belemetry.http',
        feature: 16,
        ...overrides,
    };
}

export function log(overrides = {}) {
    return {
        time: 1700000000123.456,
        severityNumber: 9,
        severityText: 'INFO',
        body: 'hello',
        eventName: null,
        attributes: {},
        traceId: null,
        spanId: null,
        scope: 'bit.belemetry.events',
        feature: 2048,
        ...overrides,
    };
}

/** A fake instrumentation context collecting emitted records. */
export function fakeContext(config = {}, exporterUrls = []) {
    const records = [];
    return {
        records,
        config: resolveConfig(config),
        emit: record => records.push(record),
        isExporterUrl: url => exporterUrls.some(prefix => url.startsWith(prefix)),
    };
}

/** A mocked fetch returning queued responses (status or { status, headers, body }) and recording calls. */
export function mockFetch(...responses) {
    const calls = [];
    const fn = async (url, init = {}) => {
        calls.push({ url: String(url), init });
        const next = responses.length > 1 ? responses.shift() : responses[0];
        if (next instanceof Error) throw next;
        const spec = typeof next === 'number' ? { status: next } : (next || { status: 200 });
        return new Response(spec.body ?? null, { status: spec.status, headers: spec.headers || {} });
    };
    fn.calls = calls;
    return fn;
}
