// Azure Monitor (Application Insights) ingestion: envelopes per the "Azure Monitor mapping" contract in CLAUDE.md,
// identical to the C# exporter.

import type { AzureMonitorExporterConfig } from '../config';
import { debug } from '../log';
import { newSpanId } from '../ids';
import { hasException, Severity, SpanKind, StatusCode, type AttributeValue, type Attributes, type LogRecord, type RecordBatch, type SpanRecord } from '../model';
import { gzip, type OutgoingRequest, type Transport } from '../transport';
import { Signals, signalsOf, type Exporter } from './exporter';
import { encodeUtf8 } from './protobuf';

export const DEFAULT_INGESTION_ENDPOINT = 'https://dc.services.visualstudio.com';

/** Retried status codes (also used for the items of a 206 partial-success response). */
export const RETRYABLE_STATUS = [408, 429, 439, 500, 502, 503, 504];

export function isRetryable(status: number): boolean {
    return RETRYABLE_STATUS.indexOf(status) >= 0;
}

export interface AzureTarget {
    iKey: string;
    endpoint: string;
}

/** Parses `InstrumentationKey=...;IngestionEndpoint=...` (keys case-insensitive). */
export function parseConnectionString(connectionString: string | null | undefined): AzureTarget | null {
    const map: Record<string, string> = {};
    for (const part of String(connectionString || '').split(';')) {
        const index = part.indexOf('=');
        if (index > 0) map[part.slice(0, index).trim().toLowerCase()] = part.slice(index + 1).trim();
    }
    const iKey = map['instrumentationkey'];
    if (!iKey) return null;
    let endpoint = map['ingestionendpoint'];
    if (!endpoint && map['endpointsuffix']) endpoint = 'https://' + (map['location'] ? map['location'] + '.' : '') + 'dc.' + map['endpointsuffix'];
    return { iKey, endpoint: (endpoint || DEFAULT_INGESTION_ENDPOINT).replace(/\/+$/, '') };
}

function pad(value: number, length: number): string {
    let text = String(value);
    while (text.length < length) text = '0' + text;
    return text;
}

/** Formats milliseconds as a .NET TimeSpan "c" string, always with 7 fractional digits: `[d.]hh:mm:ss.fffffff`. */
export function formatDuration(ms: number): string {
    const ticks = Math.max(0, Math.round(ms * 10000));
    const totalSeconds = Math.floor(ticks / 10000000);
    const fraction = ticks % 10000000;
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return (days ? days + '.' : '') + pad(hours, 2) + ':' + pad(minutes, 2) + ':' + pad(seconds, 2) + '.' + pad(fraction, 7);
}

/** Maps an OTLP severity number to the Application Insights severity level. */
export function severityLevel(severityNumber: number): number {
    return severityNumber >= 21 ? 4 : severityNumber >= 17 ? 3 : severityNumber >= 13 ? 2 : severityNumber >= 9 ? 1 : 0;
}

export interface AzureContext {
    iKey: string;
    maskIp: boolean;
    sdkVersion: string;
    /** Every envelope's `sampleRate` (`sampleRate(sampling.sessionRatio)`): each record belongs to a session kept at that ratio. Absent means 100. */
    sampleRate?: number;
}

/**
 * The envelope's `sampleRate` for records kept at `ratio`: the percentage `100 / N`, so Application Insights counts each
 * one N times (`itemCount`), N being `1 / ratio` rounded to a whole number (it counts items only in whole numbers), at
 * most 2^31 - 1. 100 for a ratio of 1 or more, 0 or less, or not a number, as `isSessionSampled` reads one: it samples
 * nothing out (or keeps nothing). The same in C# (`SampleRate`).
 */
export function sampleRate(ratio: number | undefined): number {
    const r = Number(ratio);
    return r > 0 && r < 1 ? 100 / Math.min(Math.round(1 / r), 2147483647) : 100;
}

type Json = Record<string, unknown>;

// Attributes that become tags rather than properties.
const TAG_ATTRIBUTES = ['session.id', 'enduser.pseudo.id', 'user.id'];

function stringify(value: AttributeValue): string {
    return typeof value === 'string' ? value : Array.isArray(value) ? JSON.stringify(value) : String(value);
}

function properties(attributes: Attributes, consumed: string[]): Record<string, string> | null {
    let result: Record<string, string> | null = null;
    for (const key of Object.keys(attributes)) {
        if (consumed.indexOf(key) >= 0 || TAG_ATTRIBUTES.indexOf(key) >= 0) continue;
        const value = attributes[key];
        if (value == null) continue;
        (result || (result = {}))[key.slice(0, 150)] = stringify(value).slice(0, 8192);
    }
    return result;
}

function str(attributes: Attributes, key: string): string | undefined {
    const value = attributes[key];
    return value == null ? undefined : stringify(value);
}

function baseTags(resource: Attributes, ctx: AzureContext): Record<string, string> {
    const tags: Record<string, string> = {};
    const name = str(resource, 'service.name');
    const ns = str(resource, 'service.namespace');
    // Same shape as the Azure Monitor OpenTelemetry exporter.
    if (name) tags['ai.cloud.role'] = ns ? '[' + ns + ']/' + name : name;
    const instance = str(resource, 'service.instance.id');
    if (instance) tags['ai.cloud.roleInstance'] = instance;
    const version = str(resource, 'service.version');
    if (version) tags['ai.application.ver'] = version;
    const userAgent = str(resource, 'user_agent.original');
    if (userAgent) tags['ai.user.agent'] = userAgent;
    const language = str(resource, 'browser.language');
    if (language) tags['ai.device.locale'] = language;
    tags['ai.device.type'] = 'Browser';
    if (ctx.maskIp) tags['ai.location.ip'] = '0.0.0.0';
    tags['ai.internal.sdkVersion'] = 'bitblmjs:' + ctx.sdkVersion;
    return tags;
}

function envelope(type: string, time: number, ctx: AzureContext, tags: Record<string, string>, baseData: Json, props: Record<string, string> | null): Json {
    if (props) baseData.properties = props;
    return {
        ver: 1,
        name: 'Microsoft.ApplicationInsights.' + type,
        time: new Date(time).toISOString(),
        sampleRate: ctx.sampleRate ?? 100,
        iKey: ctx.iKey,
        tags,
        data: { baseType: type + 'Data', baseData },
    };
}

function recordTags(base: Record<string, string>, attributes: Attributes, traceId: string | null, parentId: string | null, operationName?: string): Record<string, string> {
    const tags: Record<string, string> = { ...base };
    if (traceId) tags['ai.operation.id'] = traceId;
    if (parentId) tags['ai.operation.parentId'] = parentId;
    if (operationName) tags['ai.operation.name'] = operationName;
    const session = str(attributes, 'session.id');
    if (session) tags['ai.session.id'] = session;
    const pseudo = str(attributes, 'enduser.pseudo.id');
    if (pseudo) tags['ai.user.id'] = pseudo;
    const user = str(attributes, 'user.id');
    if (user) tags['ai.user.authUserId'] = user;
    return tags;
}

function dependencyTarget(a: Attributes, url: string | undefined): string | undefined {
    const host = str(a, 'server.address');
    const port = str(a, 'server.port');
    if (host) return port && port !== '80' && port !== '443' ? host + ':' + port : host;
    if (!url) return undefined;
    try {
        return new URL(url).host || undefined;
    } catch {
        return undefined;
    }
}

function isRequest(span: SpanRecord): boolean {
    return span.kind === SpanKind.Server || span.kind === SpanKind.Consumer;
}

// A request names the operation (ai.operation.name), as in C#: "{method} {route}" when both are known.
function requestName(span: SpanRecord): string {
    const method = str(span.attributes, 'http.request.method');
    const route = str(span.attributes, 'http.route');
    return method && route ? method + ' ' + route : span.name;
}

// The ingestion limits, as in C#.
const MAX_TYPE_NAME = 1024;
const MAX_MESSAGE = 32768;

function exceptionEnvelope(a: Attributes, time: number, ctx: AzureContext, tags: Record<string, string>, severityNumber: number, body: string | null): Json {
    const stack = str(a, 'exception.stacktrace');
    const detail: Json = {
        id: 1,
        outerId: 0,
        typeName: (str(a, 'exception.type') || 'Error').slice(0, MAX_TYPE_NAME),
        message: (str(a, 'exception.message') || body || '').slice(0, MAX_MESSAGE),
        hasFullStack: !!stack,
    };
    if (stack) detail.stack = stack.slice(0, MAX_MESSAGE);
    return envelope('Exception', time, ctx, tags, { ver: 2, exceptions: [detail], severityLevel: severityLevel(severityNumber) },
        properties(a, Object.keys(a).filter(key => key.indexOf('exception.') === 0)));
}

/**
 * Appends the events of a span to `out`, each an item of its own under the span, as in C# and the Azure Monitor
 * OpenTelemetry exporter: an exception (`exception` with `exception.type` or `exception.message`) an Exception - unless
 * the error limits dropped it (`limited`, set by the pipeline) - any other named event a Message (with no severity: an
 * event is not a log level). An `exception` event with neither is neither. The script's own instrumentations record no
 * exception on a span (an error is a log record), but an app processor may add one.
 */
function addEventEnvelopes(out: Json[], span: SpanRecord, resource: Attributes, ctx: AzureContext): void {
    const events = span.events;
    if (!events || !events.length) return;
    let tags: Record<string, string> | undefined;
    for (const event of events) {
        const exception = event.name === 'exception';
        if (exception ? !hasException(event.attributes) || event.limited : !event.name) continue;
        // Built once, for the first item; the ids are the span's attributes (records carry them, their events do not).
        tags = tags || recordTags(baseTags(resource, ctx), span.attributes, span.traceId, span.spanId, isRequest(span) ? requestName(span) : undefined);
        out.push(exception
            ? exceptionEnvelope(event.attributes, event.time, ctx, tags, Severity.Error, null)
            : envelope('Message', event.time, ctx, tags, { ver: 2, message: event.name.slice(0, MAX_MESSAGE) }, properties(event.attributes, [])));
    }
}

export function spanToEnvelope(span: SpanRecord, resource: Attributes, ctx: AzureContext): Json {
    const a = span.attributes;
    const success = !span.status || span.status.code !== StatusCode.Error;
    const duration = formatDuration(span.endTime - span.startTime);
    const statusCode = str(a, 'http.response.status_code');
    if (isRequest(span)) {
        const name = requestName(span);
        const tags = recordTags(baseTags(resource, ctx), a, span.traceId, span.parentSpanId, name);
        const consumed = ['http.response.status_code', 'url.full'];
        const data: Json = { ver: 2, id: span.spanId, name, duration, responseCode: statusCode || (success ? '200' : '500'), success };
        const url = str(a, 'url.full');
        if (url) data.url = url;
        return envelope('Request', span.startTime, ctx, tags, data, properties(a, consumed));
    }
    // A dependency belongs to the operation of its request or page; it does not name one.
    const tags = recordTags(baseTags(resource, ctx), a, span.traceId, span.parentSpanId);
    const consumed = ['http.response.status_code', 'url.full', 'server.address', 'server.port'];
    const url = str(a, 'url.full');
    const isHttp = a['http.request.method'] != null || url !== undefined;
    const code = Number(a['http.response.status_code']);
    const data: Json = {
        ver: 2,
        id: span.spanId,
        name: span.name,
        resultCode: statusCode || '',
        data: url || span.name,
        duration,
        success: success && (!code || code < 400),
        type: isHttp ? 'Ajax' : 'InProc',
    };
    const target = dependencyTarget(a, url);
    if (target) data.target = target;
    return envelope('RemoteDependency', span.startTime, ctx, tags, data, properties(a, consumed));
}

export function logToEnvelope(log: LogRecord, resource: Attributes, ctx: AzureContext): Json {
    const a = log.attributes;
    const base = baseTags(resource, ctx);
    if (hasException(a)) {
        return exceptionEnvelope(a, log.time, ctx, recordTags(base, a, log.traceId, log.spanId), log.severityNumber, log.body);
    }
    if (log.eventName === 'browser.page_view') {
        const url = str(a, 'url.full');
        // The body is the page view's name: the one given to trackPageView, else the title, else the template.
        const name = log.body || str(a, 'page.title') || str(a, 'url.template') || str(a, 'url.path') || url || 'page';
        const tags = recordTags(base, a, log.traceId, log.spanId, name);
        const data: Json = { ver: 2, name, id: newSpanId(), duration: '00:00:00' };
        if (url) data.url = url;
        return envelope('PageView', log.time, ctx, tags, data, properties(a, ['url.full', 'page.title']));
    }
    const tags = recordTags(base, a, log.traceId, log.spanId);
    if (log.eventName === 'browser.web_vital') {
        const value = a['web_vital.value'];
        const metric = { name: str(a, 'web_vital.name') || log.body, kind: 0, value: typeof value === 'number' ? value : Number(value) || 0, count: 1 };
        return envelope('Metric', log.time, ctx, tags, { ver: 2, metrics: [metric] }, properties(a, ['web_vital.name', 'web_vital.value']));
    }
    if (log.eventName === 'browser.page_visit_time') {
        // The shape the Application Insights JS SDK's autoTrackPageVisitTime produces, so the same workbooks apply.
        const url = str(a, 'url.full');
        const props = properties(a, ['url.full', 'url.path', 'page.title', 'page.visit.duration']) || {};
        props.PageName = log.body || str(a, 'page.title') || str(a, 'url.template') || str(a, 'url.path') || 'page';
        if (url) props.PageUrl = url;
        const duration = Number(a['page.visit.duration']);
        return envelope('Event', log.time, ctx, tags, { ver: 2, name: 'PageVisitTime', measurements: { PageVisitTime: isFinite(duration) ? duration : 0 } }, props);
    }
    if (log.eventName) {
        return envelope('Event', log.time, ctx, tags, { ver: 2, name: log.eventName }, properties(a, []));
    }
    return envelope('Message', log.time, ctx, tags, { ver: 2, message: (log.body || '').slice(0, 32768), severityLevel: severityLevel(log.severityNumber) }, properties(a, []));
}

export function toEnvelopes(batch: RecordBatch, ctx: AzureContext): Json[] {
    const envelopes: Json[] = [];
    for (const span of batch.spans) {
        envelopes.push(spanToEnvelope(span, batch.resource, ctx));
        addEventEnvelopes(envelopes, span, batch.resource, ctx);
    }
    for (const log of batch.logs) envelopes.push(logToEnvelope(log, batch.resource, ctx));
    return envelopes;
}

/**
 * Handles a 206 response (`{itemsReceived, itemsAccepted, errors:[{index, statusCode, message}]}`): returns a
 * request with only the items whose status is retryable, or null when nothing is worth retrying.
 */
export function rebuildPartial(request: OutgoingRequest, responseText: string, plainBody?: Uint8Array | null): OutgoingRequest | null {
    const result = JSON.parse(responseText);
    const errors = result && result.errors;
    if (!Array.isArray(errors)) return null;
    const items = JSON.parse(new TextDecoder().decode(plainBody || request.raw || request.body));
    if (!Array.isArray(items)) return null;
    const indexes = new Set<unknown>();
    for (const error of errors) {
        if (error && isRetryable(error.statusCode)) indexes.add(error.index);
    }
    const retry = items.filter((_: unknown, index: number) => indexes.has(index));
    if (!retry.length) return null;
    return { ...request, body: encodeUtf8(JSON.stringify(retry)), raw: null, contentEncoding: null };
}

export function createAzureExporter(config: AzureMonitorExporterConfig, transport: Transport, sdkVersion: string, sessionRatio?: number): Exporter {
    const target = parseConnectionString(config.connectionString);
    if (!target) {
        debug('azureMonitor: invalid connection string');
        return { urls: [], export: () => Promise.resolve() };
    }
    const url = target.endpoint + '/v2.1/track';
    const ctx: AzureContext = { iKey: target.iKey, maskIp: config.maskIp !== false, sdkVersion, sampleRate: sampleRate(sessionRatio) };
    const compress = config.compression === 'gzip';
    const signals = signalsOf(config);
    return {
        urls: [target.endpoint],
        async export(batch, unload, mark) {
            const filtered: RecordBatch = {
                resource: batch.resource,
                spans: signals & Signals.Traces ? batch.spans : [],
                logs: signals & Signals.Logs ? batch.logs : [],
            };
            const envelopes = toEnvelopes(filtered, ctx);
            if (!envelopes.length) return;
            const body = encodeUtf8(JSON.stringify(envelopes));
            // Compression is async, and nothing async survives unload, so unload batches go uncompressed. The raw
            // body is kept for persistence and for rebuilding a 206 partial success.
            const zipped = compress && !unload ? await gzip(body) : null;
            transport.send({
                exporter: 'azureMonitor',
                url,
                contentType: 'application/json',
                contentEncoding: zipped ? 'gzip' : null,
                body: zipped || body,
                raw: zipped ? body : null,
            }, unload, mark);
        },
    };
}
