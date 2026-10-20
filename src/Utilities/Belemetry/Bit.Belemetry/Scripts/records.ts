import { now } from './clock';
import { Features } from './features';
import { newSpanId, newTraceId } from './ids';
import { Severity, severityText, StatusCode, type Attributes, type LogRecord, type SpanRecord } from './model';

export function createLog(scope: string, feature: number, severityNumber: number, body: string, eventName: string | null,
    attributes: Attributes = {}, time = now()): LogRecord {
    return {
        time,
        severityNumber,
        severityText: severityText(severityNumber),
        body,
        eventName,
        attributes,
        traceId: null,
        spanId: null,
        scope,
        feature,
    };
}

export function createSpan(scope: string, feature: number, name: string, kind: number, startTime: number, endTime: number,
    attributes: Attributes = {}, traceId = newTraceId(), spanId = newSpanId()): SpanRecord {
    return {
        traceId,
        spanId,
        parentSpanId: null,
        name,
        kind,
        startTime,
        endTime,
        attributes,
        status: { code: StatusCode.Unset },
        events: [],
        scope,
        feature,
    };
}

export function createException(scope: string, type: string, message: string, stack?: string | null, attributes?: Attributes | null): LogRecord {
    const attrs: Attributes = { ...(attributes || {}) };
    attrs['exception.type'] = type || 'Error';
    attrs['exception.message'] = message || '';
    if (stack) attrs['exception.stacktrace'] = stack;
    return createLog(scope, Features.Errors, Severity.Error, message || type || 'Error', 'exception', attrs);
}

/** Copies the JSON-compatible attributes the app passed in, dropping anything that is not a valid value. */
export function sanitizeAttributes(input: unknown): Attributes {
    const output: Attributes = {};
    if (!input || typeof input !== 'object') return output;
    for (const key of Object.keys(input as object)) {
        const value = (input as Record<string, unknown>)[key];
        const type = typeof value;
        if (type === 'string' || type === 'boolean' || (type === 'number' && isFinite(value as number))) {
            output[key] = value as string;
        } else if (Array.isArray(value)) {
            const first = typeof value[0];
            if (value.every(v => typeof v === first && (first === 'string' || first === 'boolean' || first === 'number'))) output[key] = value as string[];
        }
    }
    return output;
}
