// OTLP/JSON per the OTLP specification: lowerCamelCase names, trace and span ids as hex strings (not base64),
// enums as integers, 64-bit integers as decimal strings.

import type { AttributeValue, Attributes, LogRecord, SpanRecord } from '../model';
import { groupByScope, isInt, msToNanosString } from './otlp-common';

type Json = Record<string, unknown>;

export function toAnyValue(value: AttributeValue): Json {
    if (typeof value === 'string') return { stringValue: value };
    if (typeof value === 'boolean') return { boolValue: value };
    if (typeof value === 'number') {
        if (isInt(value)) return { intValue: String(value) };
        // JSON has no NaN/Infinity; the protobuf JSON mapping spells them as strings.
        return { doubleValue: isFinite(value) ? value : String(value) };
    }
    if (Array.isArray(value)) return { arrayValue: { values: (value as (string | number | boolean)[]).map(toAnyValue) } };
    return {};
}

export function toKeyValues(attributes: Attributes): Json[] {
    const list: Json[] = [];
    for (const key of Object.keys(attributes || {})) {
        const value = attributes[key];
        if (value != null) list.push({ key, value: toAnyValue(value) });
    }
    return list;
}

function scopeJson(name: string, version?: string): Json {
    return version ? { name, version } : { name };
}

function spanJson(span: SpanRecord): Json {
    const json: Json = {
        traceId: span.traceId,
        spanId: span.spanId,
    };
    if (span.parentSpanId) json.parentSpanId = span.parentSpanId;
    json.flags = 1;
    json.name = span.name;
    json.kind = span.kind;
    json.startTimeUnixNano = msToNanosString(span.startTime);
    json.endTimeUnixNano = msToNanosString(span.endTime);
    json.attributes = toKeyValues(span.attributes);
    if (span.events && span.events.length) {
        json.events = span.events.map(e => ({ timeUnixNano: msToNanosString(e.time), name: e.name, attributes: toKeyValues(e.attributes) }));
    }
    const status: Json = {};
    if (span.status && span.status.message) status.message = span.status.message;
    if (span.status && span.status.code) status.code = span.status.code;
    json.status = status;
    return json;
}

function logJson(log: LogRecord): Json {
    const json: Json = {
        timeUnixNano: msToNanosString(log.time),
        observedTimeUnixNano: msToNanosString(log.time),
        severityNumber: log.severityNumber,
        severityText: log.severityText,
        body: toAnyValue(log.body),
        attributes: toKeyValues(log.attributes),
    };
    if (log.traceId) {
        json.flags = 1;
        json.traceId = log.traceId;
    }
    if (log.spanId) json.spanId = log.spanId;
    if (log.eventName) json.eventName = log.eventName;
    return json;
}

/** Builds an ExportTraceServiceRequest JSON object. */
export function tracesToJson(resource: Attributes, spans: SpanRecord[], scopeVersion?: string): Json {
    return {
        resourceSpans: [{
            resource: { attributes: toKeyValues(resource) },
            scopeSpans: groupByScope(spans).map(([scope, records]) => ({ scope: scopeJson(scope, scopeVersion), spans: records.map(spanJson) })),
        }],
    };
}

/** Builds an ExportLogsServiceRequest JSON object. */
export function logsToJson(resource: Attributes, logs: LogRecord[], scopeVersion?: string): Json {
    return {
        resourceLogs: [{
            resource: { attributes: toKeyValues(resource) },
            scopeLogs: groupByScope(logs).map(([scope, records]) => ({ scope: scopeJson(scope, scopeVersion), logRecords: records.map(logJson) })),
        }],
    };
}
