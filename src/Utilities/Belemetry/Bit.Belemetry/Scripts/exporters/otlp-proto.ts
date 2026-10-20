// OTLP protobuf encoders. Field numbers follow opentelemetry-proto v1.11 (common, resource, trace, logs and the
// collector service requests).

import type { AttributeValue, Attributes, LogRecord, SpanRecord } from '../model';
import { groupByScope, isInt } from './otlp-common';
import { hexToBytes, msToNanos, ProtobufWriter } from './protobuf';

function writeAnyValue(w: ProtobufWriter, value: AttributeValue): void {
    if (typeof value === 'string') w.stringField(1, value);
    else if (typeof value === 'boolean') w.boolField(2, value);
    else if (typeof value === 'number') {
        if (isInt(value)) w.int64Field(3, value);
        else w.doubleField(4, value);
    } else if (Array.isArray(value)) {
        // ArrayValue.values = 1
        w.messageField(5, a => {
            for (const item of value as (string | number | boolean)[]) a.messageField(1, v => writeAnyValue(v, item));
        });
    }
}

function writeAttributes(w: ProtobufWriter, field: number, attributes: Attributes): void {
    for (const key of Object.keys(attributes)) {
        const value = attributes[key];
        if (value == null) continue;
        // KeyValue: key = 1, value = 2
        w.messageField(field, kv => {
            kv.stringField(1, key);
            kv.messageField(2, v => writeAnyValue(v, value));
        });
    }
}

function writeScope(w: ProtobufWriter, name: string, version: string | undefined): void {
    // InstrumentationScope: name = 1, version = 2
    w.messageField(1, s => {
        s.stringField(1, name);
        if (version) s.stringField(2, version);
    });
}

function writeTime(w: ProtobufWriter, field: number, ms: number): void {
    const [lo, hi] = msToNanos(ms);
    w.fixed64Field(field, lo, hi);
}

function writeSpan(w: ProtobufWriter, span: SpanRecord): void {
    w.bytesField(1, hexToBytes(span.traceId));
    w.bytesField(2, hexToBytes(span.spanId));
    if (span.parentSpanId) w.bytesField(4, hexToBytes(span.parentSpanId));
    w.stringField(5, span.name);
    w.uint32Field(6, span.kind);
    writeTime(w, 7, span.startTime);
    writeTime(w, 8, span.endTime);
    writeAttributes(w, 9, span.attributes);
    for (const event of span.events || []) {
        // Span.Event: time_unix_nano = 1, name = 2, attributes = 3
        w.messageField(11, e => {
            writeTime(e, 1, event.time);
            e.stringField(2, event.name);
            writeAttributes(e, 3, event.attributes || {});
        });
    }
    const status = span.status;
    if (status && (status.code || status.message)) {
        // Status: message = 2, code = 3
        w.messageField(15, s => {
            if (status.message) s.stringField(2, status.message);
            if (status.code) s.uint32Field(3, status.code);
        });
    }
    // W3C trace flags in the low byte: sampled.
    w.fixed32Field(16, 1);
}

function writeLog(w: ProtobufWriter, log: LogRecord): void {
    writeTime(w, 1, log.time);
    if (log.severityNumber) w.uint32Field(2, log.severityNumber);
    if (log.severityText) w.stringField(3, log.severityText);
    if (log.body != null) w.messageField(5, v => writeAnyValue(v, log.body));
    writeAttributes(w, 6, log.attributes);
    if (log.traceId) w.fixed32Field(8, 1);
    if (log.traceId) w.bytesField(9, hexToBytes(log.traceId));
    if (log.spanId) w.bytesField(10, hexToBytes(log.spanId));
    writeTime(w, 11, log.time);
    if (log.eventName) w.stringField(12, log.eventName);
}

/** Encodes an ExportTraceServiceRequest. */
export function encodeTraces(resource: Attributes, spans: SpanRecord[], scopeVersion?: string): Uint8Array {
    const w = new ProtobufWriter();
    // ExportTraceServiceRequest.resource_spans = 1
    w.messageField(1, rs => {
        // ResourceSpans: resource = 1 (Resource.attributes = 1), scope_spans = 2
        rs.messageField(1, r => writeAttributes(r, 1, resource));
        for (const [scope, records] of groupByScope(spans)) {
            // ScopeSpans: scope = 1, spans = 2
            rs.messageField(2, ss => {
                writeScope(ss, scope, scopeVersion);
                for (const span of records) ss.messageField(2, s => writeSpan(s, span));
            });
        }
    });
    return w.finish();
}

/** Encodes an ExportLogsServiceRequest. */
export function encodeLogs(resource: Attributes, logs: LogRecord[], scopeVersion?: string): Uint8Array {
    const w = new ProtobufWriter();
    // ExportLogsServiceRequest.resource_logs = 1
    w.messageField(1, rl => {
        // ResourceLogs: resource = 1, scope_logs = 2
        rl.messageField(1, r => writeAttributes(r, 1, resource));
        for (const [scope, records] of groupByScope(logs)) {
            // ScopeLogs: scope = 1, log_records = 2
            rl.messageField(2, sl => {
                writeScope(sl, scope, scopeVersion);
                for (const log of records) sl.messageField(2, l => writeLog(l, log));
            });
        }
    });
    return w.finish();
}
