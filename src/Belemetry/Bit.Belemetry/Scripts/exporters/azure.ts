// Azure Monitor (Application Insights) ingestion: envelopes per the "Azure Monitor mapping" contract in CLAUDE.md,
// identical to the C# exporter.

import type { AzureMonitorExporterConfig } from '../config';
import { debug } from '../log';
import { newSpanId } from '../ids';
import { SpanKind, StatusCode, type AttributeValue, type Attributes, type LogRecord, type RecordBatch, type SpanRecord } from '../model';
import type { OutgoingRequest, Transport } from '../transport';
import type { Exporter } from './exporter';
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
        sampleRate: 100,
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

export function spanToEnvelope(span: SpanRecord, resource: Attributes, ctx: AzureContext): Json {
    const a = span.attributes;
    const tags = recordTags(baseTags(resource, ctx), a, span.traceId, span.parentSpanId, span.name);
    const success = !span.status || span.status.code !== StatusCode.Error;
    const duration = formatDuration(span.endTime - span.startTime);
    const statusCode = str(a, 'http.response.status_code');
    if (span.kind === SpanKind.Server || span.kind === SpanKind.Consumer) {
        const consumed = ['http.response.status_code', 'url.full'];
        const data: Json = { ver: 2, id: span.spanId, name: span.name, duration, responseCode: statusCode || (success ? '200' : '500'), success };
        const url = str(a, 'url.full');
        if (url) data.url = url;
        return envelope('Request', span.startTime, ctx, tags, data, properties(a, consumed));
    }
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
    if (a['exception.type'] != null || a['exception.message'] != null) {
        const stack = str(a, 'exception.stacktrace');
        const detail: Json = { id: 1, outerId: 0, typeName: str(a, 'exception.type') || 'Error', message: str(a, 'exception.message') || log.body || '', hasFullStack: !!stack };
        if (stack) detail.stack = stack;
        const tags = recordTags(base, a, log.traceId, log.spanId);
        return envelope('Exception', log.time, ctx, tags, { ver: 2, exceptions: [detail], severityLevel: severityLevel(log.severityNumber) },
            properties(a, Object.keys(a).filter(key => key.indexOf('exception.') === 0)));
    }
    if (log.eventName === 'browser.page_view') {
        const url = str(a, 'url.full');
        const name = str(a, 'page.title') || str(a, 'url.path') || url || log.body;
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
    if (log.eventName) {
        return envelope('Event', log.time, ctx, tags, { ver: 2, name: log.eventName }, properties(a, []));
    }
    return envelope('Message', log.time, ctx, tags, { ver: 2, message: (log.body || '').slice(0, 32768), severityLevel: severityLevel(log.severityNumber) }, properties(a, []));
}

export function toEnvelopes(batch: RecordBatch, ctx: AzureContext): Json[] {
    const envelopes: Json[] = [];
    for (const span of batch.spans) envelopes.push(spanToEnvelope(span, batch.resource, ctx));
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
    const retry: unknown[] = [];
    for (const error of errors) {
        if (error && isRetryable(error.statusCode) && items[error.index] !== undefined) retry.push(items[error.index]);
    }
    if (!retry.length) return null;
    return { ...request, body: encodeUtf8(JSON.stringify(retry)), raw: null, contentEncoding: null };
}

export function createAzureExporter(config: AzureMonitorExporterConfig, transport: Transport, sdkVersion: string): Exporter {
    const target = parseConnectionString(config.connectionString);
    if (!target) {
        debug('azureMonitor: invalid connection string');
        return { urls: [], export: () => Promise.resolve() };
    }
    const url = target.endpoint + '/v2.1/track';
    const ctx: AzureContext = { iKey: target.iKey, maskIp: config.maskIp !== false, sdkVersion };
    return {
        urls: [target.endpoint],
        export(batch, unload) {
            const envelopes = toEnvelopes(batch, ctx);
            if (envelopes.length) {
                transport.send({ exporter: 'azureMonitor', url, contentType: 'application/json', body: encodeUtf8(JSON.stringify(envelopes)) }, unload);
            }
            return Promise.resolve();
        },
    };
}
