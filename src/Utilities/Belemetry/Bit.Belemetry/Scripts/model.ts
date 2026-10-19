// The browser record model. It is also the relay payload (C# BelemetryRelayBatch), so the property names are a
// wire contract. Times are milliseconds since the Unix epoch (fractional).

export type AttributeValue = string | number | boolean | string[] | number[] | boolean[];

export type Attributes = Record<string, AttributeValue>;

export interface SpanEvent {
    name: string;
    time: number;
    attributes: Attributes;
}

export interface SpanStatus {
    code: number;
    message?: string | null;
}

export interface SpanRecord {
    traceId: string;
    spanId: string;
    parentSpanId: string | null;
    name: string;
    kind: number;
    startTime: number;
    endTime: number;
    attributes: Attributes;
    status: SpanStatus;
    events: SpanEvent[];
    scope: string;
    feature: number;
}

export interface LogRecord {
    time: number;
    severityNumber: number;
    severityText: string;
    body: string;
    eventName: string | null;
    attributes: Attributes;
    traceId: string | null;
    spanId: string | null;
    scope: string;
    feature: number;
}

export type TelemetryRecord = SpanRecord | LogRecord;

export interface RecordBatch {
    resource: Attributes;
    spans: SpanRecord[];
    logs: LogRecord[];
}

export function isSpan(record: TelemetryRecord): record is SpanRecord {
    return 'startTime' in record;
}

// OTLP SpanKind numbers.
export const SpanKind = { Internal: 1, Server: 2, Client: 3, Producer: 4, Consumer: 5 } as const;

// OTLP Status.code numbers.
export const StatusCode = { Unset: 0, Ok: 1, Error: 2 } as const;

// OTLP SeverityNumber (the first number of each range).
export const Severity = { Trace: 1, Debug: 5, Info: 9, Warn: 13, Error: 17, Fatal: 21 } as const;

export function severityText(severityNumber: number): string {
    return severityNumber >= 21 ? 'FATAL'
        : severityNumber >= 17 ? 'ERROR'
            : severityNumber >= 13 ? 'WARN'
                : severityNumber >= 9 ? 'INFO'
                    : severityNumber >= 5 ? 'DEBUG'
                        : 'TRACE';
}
