// Sentry: error events, one envelope each, per the "Sentry mapping" contract in CLAUDE.md - the same event the C#
// exporter writes for a browser record. Neither Relay nor Sentry parses a stack string, so the stack is parsed here.

import type { SentryExporterConfig } from '../config';
import type { ConsentMark } from '../consent';
import { newTraceId } from '../ids';
import { debug } from '../log';
import { hasException, isExceptionEvent, Severity, type AttributeValue, type Attributes, type RecordBatch } from '../model';
import { gzip, type Transport } from '../transport';
import type { Exporter } from './exporter';
import { encodeUtf8 } from './protobuf';

/** The scope of the script's own errors (`error`, `unhandledrejection`): nothing caught them. */
export const BROWSER_ERRORS_SCOPE = 'bit.belemetry.errors';

/** The most frames an exception keeps: the newest, where it was thrown. */
export const MAX_FRAMES = 100;

/** The most exceptions of a chain an event keeps, outermost first. */
export const MAX_EXCEPTIONS = 16;

export interface SentryDsn {
    publicKey: string;
    projectId: string;
    /** `{scheme}://{host}{path}/api/{project}/envelope/`, without the authentication. */
    envelopeEndpoint: string;
    /** The endpoint with the key in the query: no header is needed, from a page or for a stored request. */
    envelopeUrl: string;
    /** The DSN without a legacy secret key. */
    publicDsn: string;
}

/** Parses `{scheme}://{public key}[:{secret}]@{host}[:{port}][/{path}]/{project id}`, as C# SentryDsn.Parse. */
export function parseDsn(dsn: string | null | undefined): SentryDsn | null {
    let url: URL;
    try {
        url = new URL(String(dsn || '').trim());
    } catch {
        return null;
    }
    if (url.protocol !== 'https:' && url.protocol !== 'http:') return null;
    const publicKey = decodeURIComponent(url.username);
    if (!publicKey) return null;
    const path = url.pathname.replace(/\/+$/, '');
    const slash = path.lastIndexOf('/');
    const projectId = decodeURIComponent(path.slice(slash + 1));
    if (!projectId) return null;
    const prefix = path.slice(0, Math.max(0, slash));
    const project = encodeURIComponent(projectId);
    const envelopeEndpoint = url.protocol + '//' + url.host + prefix + '/api/' + project + '/envelope/';
    return {
        publicKey,
        projectId,
        envelopeEndpoint,
        envelopeUrl: envelopeEndpoint + '?sentry_key=' + encodeURIComponent(publicKey) + '&sentry_version=7',
        publicDsn: url.protocol + '//' + encodeURIComponent(publicKey) + '@' + url.host + prefix + '/' + project,
    };
}

export interface SentryFrame {
    function?: string;
    module?: string;
    filename?: string;
    abs_path?: string;
    lineno?: number;
    colno?: number;
    in_app: boolean;
}

const EXTENSION_SCHEME = /^(?:chrome|moz|safari|safari-web|ms-browser)-extension:/i;
const LOCATION = /^(.*?):(\d+)(?::(\d+))?$/;
const EVAL_LOCATION = /\(([^()]+:\d+:\d+)\)/;

/**
 * Whether a script is the app's: one loaded from a URL (any scheme - a Hybrid app's page is `app://` on iOS) that is
 * not a browser extension's, nor Blazor's runtime (`/_framework/`) or Belemetry's script.
 */
function isInApp(url: string): boolean {
    return url.indexOf('://') >= 0 && !EXTENSION_SCHEME.test(url) && url.indexOf('/_framework/') < 0 && url.indexOf('/_content/Bit.Belemetry/') < 0;
}

/** A frame of a JavaScript stack (V8, SpiderMonkey, JavaScriptCore); null for a line that is not one. */
export function parseFrame(line: string): SentryFrame | null {
    let fn: string | null;
    let location: string;
    const v8 = line.indexOf('at ') === 0;
    if (v8) {
        // V8: "at fn (url:1:2)", "at url:1:2", "at async fn (url:1:2)", "at new Foo (url:1:2)", "at fn (<anonymous>)".
        let body = line.slice(3).trim();
        if (body.indexOf('async ') === 0) body = body.slice(6);
        const open = body.indexOf(' (');
        if (open > 0 && body.charAt(body.length - 1) === ')') {
            fn = body.slice(0, open);
            location = body.slice(open + 2, -1);
        } else {
            fn = null;
            location = body;
        }
        // "eval at fn (url:1:2), <anonymous>:3:4": the script that called eval is where the code came from.
        if (location.indexOf('eval at ') === 0) {
            const evaluated = EVAL_LOCATION.exec(location);
            if (!evaluated) return null;
            location = evaluated[1];
        }
    } else {
        // SpiderMonkey and JavaScriptCore: "fn@url:1:2", "@url:1:2", "async*fn@url:1:2", "global code@url:1:2",
        // "map@[native code]". The first '@': a URL may hold one (a CDN's "pkg@1.0.0"), a function name may not.
        const at = line.indexOf('@');
        if (at < 0) return null;
        fn = line.slice(0, at);
        if (fn.indexOf('async*') === 0) fn = fn.slice(6);
        location = line.slice(at + 1);
        // SpiderMonkey's eval: "url line 2 > eval:1:1" - the script that called eval, at its line.
        const evalLine = location.indexOf(' line ');
        if (evalLine > 0 && location.indexOf(' > ') >= 0) {
            const digits = /^\d+/.exec(location.slice(evalLine + 6));
            location = location.slice(0, evalLine) + (digits ? ':' + digits[0] : '');
        }
    }
    let url: string;
    let lineno: number | undefined;
    let colno: number | undefined;
    const parts = LOCATION.exec(location);
    if (parts) {
        url = parts[1];
        lineno = Number(parts[2]);
        if (parts[3] !== undefined) colno = Number(parts[3]);
    } else {
        url = location;
    }
    // An '@' line of something else (a message holding an e-mail address) has no script location.
    if (!v8 && lineno === undefined && url.indexOf('://') < 0 && url !== '[native code]') return null;
    if (!url) return null;
    const frame: SentryFrame = { function: fn && fn.trim() ? fn.trim() : '?', filename: url, abs_path: url, in_app: isInApp(url) };
    if (lineno !== undefined) frame.lineno = lineno;
    if (colno !== undefined) frame.colno = colno;
    return frame;
}

/** The frames of a JavaScript stack, oldest first as Sentry orders them, at most MAX_FRAMES of the newest. */
export function parseStack(stack: string | null | undefined): SentryFrame[] {
    const frames: SentryFrame[] = [];
    if (!stack) return frames;
    for (const line of stack.split(/\r?\n/)) {
        if (frames.length === MAX_FRAMES) break;
        const frame = parseFrame(line.trim());
        if (frame) frames.push(frame);
    }
    return frames.reverse();
}

// .NET stack traces, as C# SentryStackTrace parses them: a .NET exception the script recorded - a Blazor Server
// component's TrackException, forwarded to the page - has a .NET stack, told by its lines rather than by who recorded it.

const END_OF_INNER = '--- End of inner exception stack trace ---';
const DOTNET_FRAME = /^at ([^\s(]+)\((.*?)\)(?: \+ 0x[0-9a-fA-F]+)?(?: in (.+):line (\d+))?$/;
const STATE_MACHINE = /^<([^<>]+)>d(?:__\d+)?$/;

/**
 * Whether a .NET frame is framework or runtime code (C# BelemetryExceptionFingerprint.IsFrameworkFrame): System.*,
 * Microsoft.*, interop, and Belemetry's own types (Bit.Belemetry.Belemetry*, Bit.Belemetry.IBelemetry*).
 */
function isFrameworkFrame(frame: string): boolean {
    const own = frame.indexOf('Bit.Belemetry.') === 0 && (frame.indexOf('Belemetry', 14) === 14 || frame.indexOf('IBelemetry', 14) === 14);
    return frame.indexOf('System.') === 0 || frame.indexOf('Microsoft.') === 0 || own || frame.indexOf('Interop+') === 0
        || frame.indexOf('@blazor.') >= 0 || frame.indexOf('@dotnet.') >= 0 || frame.indexOf('@bit-belemetry.') >= 0 || /@native$/.test(frame);
}

function fileNameOf(path: string): string {
    const slash = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
    return slash >= 0 ? path.slice(slash + 1) : path;
}

/** A frame of a .NET stack trace's text (`at Namespace.Type.Method(Args) in path:line 42`); null for another line. */
export function dotnetFrame(line: string): SentryFrame | null {
    const match = DOTNET_FRAME.exec(line);
    if (!match) return null;
    let method = match[1].trim();
    // A generic method's arguments ("Method[T]") are not part of its name.
    const bracket = method.indexOf('[');
    if (method.charAt(method.length - 1) === ']' && bracket > 0) method = method.slice(0, bracket);
    if (!method || method.indexOf(' ') >= 0) return null;
    let module: string | undefined;
    let fn: string;
    if (/\.\.c?ctor$/.test(method)) {
        const cut = method.lastIndexOf('..');
        module = method.slice(0, cut);
        fn = method.slice(cut + 1);
    } else {
        const dot = method.lastIndexOf('.');
        module = dot > 0 ? method.slice(0, dot) : undefined;
        fn = dot > 0 ? method.slice(dot + 1) : method;
    }
    // An async method's or iterator's MoveNext is the method it was written as, on the type that declares it.
    if (fn === 'MoveNext' && module) {
        const ownerDot = module.lastIndexOf('.');
        const machine = STATE_MACHINE.exec(ownerDot >= 0 ? module.slice(ownerDot + 1) : module);
        if (machine && ownerDot > 0) {
            fn = machine[1];
            module = module.slice(0, ownerDot);
        }
    }
    const frame: SentryFrame = { function: fn, in_app: !isFrameworkFrame(module ? module + '.' + fn : fn) };
    if (module) frame.module = module;
    if (match[3] !== undefined) {
        frame.filename = fileNameOf(match[3]);
        frame.abs_path = match[3];
    }
    if (match[4] !== undefined) frame.lineno = Number(match[4]);
    return frame;
}

/** Whether a stack is .NET's: a line is a .NET frame, which a JavaScript frame never is. */
export function isDotnetStack(stack: string | null | undefined): boolean {
    if (!stack) return false;
    for (const line of stack.split(/\r?\n/)) {
        if (dotnetFrame(line.trim())) return true;
    }
    return false;
}

export interface DotnetException {
    type?: string;
    message?: string;
    frames: SentryFrame[];
}

function splitHeader(part: string): { type?: string; message?: string } {
    const colon = part.indexOf(': ');
    const type = (colon >= 0 ? part.slice(0, colon) : part).trim();
    const message = colon >= 0 ? part.slice(colon + 2).replace(/\s+$/, '') : '';
    return { type: type && type.indexOf(' ') < 0 ? type : undefined, message: message || undefined };
}

/**
 * The exceptions of a .NET stack trace's text, outermost first (C# SentryStackTrace.ParseDotnet): the header names
 * them (`Outer: message ---> Inner: message`), and their frames follow innermost first, each set closed by "End of
 * inner exception stack trace". When the two do not line up, only the outermost is told, with its own frames.
 */
export function parseDotnetStack(stack: string | null | undefined): DotnetException[] {
    if (!stack) return [];
    const header: string[] = [];
    const segments: SentryFrame[][] = [[]];
    let inFrames = false;
    for (const raw of stack.split(/\r?\n/)) {
        const line = raw.trim();
        if (!inFrames) {
            if (line.indexOf('at ') !== 0 && line !== END_OF_INNER) {
                header.push(raw);
                continue;
            }
            inFrames = true;
        }
        if (line === END_OF_INNER) {
            segments.push([]);
            continue;
        }
        // An AggregateException's other inner exceptions follow its own frames: they are not the chain's.
        if (line.indexOf('--->') === 0) break;
        const frame = dotnetFrame(line);
        if (frame) segments[segments.length - 1].push(frame);
    }
    for (const segment of segments) {
        if (segment.length > MAX_FRAMES) segment.length = MAX_FRAMES;
        segment.reverse();
    }
    const parts = header.join('\n').split(' ---> ');
    if (parts.length !== segments.length) return [{ ...splitHeader(parts[0]), frames: segments[segments.length - 1] }];
    return parts.slice(0, MAX_EXCEPTIONS).map((part, i) => ({ ...splitHeader(part), frames: segments[segments.length - 1 - i] }));
}

export interface SentryContext {
    dsn: SentryDsn;
    /** The configured release; null takes `{service.name}@{service.version}` of the resource. */
    release: string | null;
    /** The least severe record that becomes an event. */
    minimumSeverity: number;
    maskIp: boolean;
    sdkVersion: string;
}

type Json = Record<string, unknown>;

/** The Sentry level of an OTLP severity number. */
export function level(severityNumber: number): string {
    return severityNumber >= 21 ? 'fatal' : severityNumber >= 17 ? 'error' : severityNumber >= 13 ? 'warning' : severityNumber >= 9 ? 'info' : 'debug';
}

function str(attributes: Attributes, key: string): string | undefined {
    const value = attributes[key];
    if (value == null) return undefined;
    return typeof value === 'string' ? value : Array.isArray(value) ? '[' + value.join(',') + ']' : String(value);
}

/** The configured release, else `{service.name}@{service.version}` of the resource; undefined without a version. */
export function releaseOf(ctx: SentryContext, resource: Attributes): string | undefined {
    if (ctx.release) return ctx.release;
    const version = str(resource, 'service.version');
    if (!version) return undefined;
    const name = str(resource, 'service.name');
    return name ? name + '@' + version : version;
}

function isHex(value: string | null | undefined, length: number): value is string {
    return typeof value === 'string' && value.length === length && /^[0-9a-fA-F]+$/.test(value);
}

/** What an event is made of (C# `Source`). */
interface Source {
    attributes: Attributes;
    /** Where the session and user ids are: the record's attributes, or the span's for its exception. */
    ids: Attributes;
    severityNumber: number;
    body: string | null;
    scope: string;
    time: number;
    traceId: string | null;
    spanId: string | null;
    parentSpanId: string | null;
    transaction: string | null;
}

function copy(attributes: Attributes, skip: (key: string) => boolean): Json | null {
    let result: Json | null = null;
    for (const key of Object.keys(attributes)) {
        if (skip(key)) continue;
        const value: AttributeValue = attributes[key];
        if (value == null) continue;
        (result || (result = {}))[key] = value;
    }
    return result;
}

export function toEvent(source: Source, resource: Attributes, ctx: SentryContext, eventId: string): Json {
    const a = source.attributes;
    const browser = resource['telemetry.sdk.language'] === 'webjs';
    const exceptionMessage = str(a, 'exception.message');
    const stack = str(a, 'exception.stacktrace');
    const dotnet = isDotnetStack(stack);
    const event: Json = {
        event_id: eventId,
        timestamp: Math.floor(source.time) / 1000,
        // The frames' platform: a .NET exception the script recorded has .NET frames.
        platform: browser && !dotnet ? 'javascript' : 'csharp',
        level: level(source.severityNumber),
    };
    if (source.scope) event.logger = source.scope;
    if (source.transaction) event.transaction = source.transaction;
    const release = releaseOf(ctx, resource);
    if (release) event.release = release;
    const environment = str(resource, 'deployment.environment.name');
    if (environment) event.environment = environment;
    const host = str(resource, 'host.name');
    if (host) event.server_name = host;
    // The log message, when it says more than the exception's own message (trackException's body is that message).
    if (source.body && source.body !== exceptionMessage) event.message = { formatted: source.body };

    if (hasException(a)) {
        // Outermost first; its type and message are the attributes' (what privacy and the processors left).
        const type = str(a, 'exception.type');
        let chain: DotnetException[];
        if (stack && dotnet) {
            const parsed = parseDotnetStack(stack);
            const outer: DotnetException = { type: type !== undefined ? type : parsed.length ? parsed[0].type : undefined, message: exceptionMessage, frames: parsed.length ? parsed[0].frames : [] };
            chain = [outer].concat(parsed.slice(1));
        } else {
            chain = [{ type, message: exceptionMessage, frames: parseStack(stack) }];
        }
        // Sentry orders a chain oldest first: the innermost exception (the cause) first, the outermost last.
        const values: Json[] = [];
        for (let i = chain.length - 1; i >= 0; i--) {
            const value: Json = {};
            if (chain[i].type) value.type = chain[i].type;
            if (chain[i].message) value.value = chain[i].message;
            if (chain[i].frames.length) value.stacktrace = { frames: chain[i].frames };
            if (i === 0) value.mechanism = { type: 'generic', handled: source.scope !== BROWSER_ERRORS_SCOPE };
            values.push(value);
        }
        event.exception = { values };
    }

    const contexts: Json = {};
    // The trace the record was produced in: Sentry links the Issue to it.
    if (isHex(source.traceId, 32) && isHex(source.spanId, 16)) {
        const trace: Json = { trace_id: source.traceId, span_id: source.spanId };
        if (isHex(source.parentSpanId, 16)) trace.parent_span_id = source.parentSpanId;
        trace.type = 'trace';
        contexts.trace = trace;
    }
    const runtimeName = str(resource, 'process.runtime.name');
    const runtimeVersion = str(resource, 'process.runtime.version');
    const runtimeDescription = str(resource, 'process.runtime.description');
    if (runtimeName !== undefined || runtimeVersion !== undefined || runtimeDescription !== undefined) {
        const runtime: Json = {};
        if (runtimeName !== undefined) runtime.name = runtimeName;
        if (runtimeVersion !== undefined) runtime.version = runtimeVersion;
        if (runtimeDescription !== undefined) runtime.raw_description = runtimeDescription;
        runtime.type = 'runtime';
        contexts.runtime = runtime;
    }
    const osType = str(resource, 'os.type');
    const osDescription = str(resource, 'os.description');
    if (osType !== undefined || osDescription !== undefined) {
        const os: Json = {};
        if (osType !== undefined) os.name = osType;
        if (osDescription !== undefined) os.raw_description = osDescription;
        os.type = 'os';
        contexts.os = os;
    }
    const locale = str(resource, 'browser.language');
    if (locale) contexts.culture = { locale, type: 'culture' };
    // The whole resource, as Sentry's OpenTelemetry integration shows it.
    const resourceCopy = copy(resource, () => false);
    if (resourceCopy) contexts.otel = { resource: resourceCopy, type: 'otel' };
    event.contexts = contexts;

    const session = str(source.ids, 'session.id');
    if (session) event.tags = { 'session.id': session };
    const userId = str(source.ids, 'user.id');
    const pseudoId = str(source.ids, 'enduser.pseudo.id');
    if (userId || pseudoId) event.user = { id: userId || pseudoId };

    // Sentry tells the browser and the operating system from it.
    const userAgent = str(resource, 'user_agent.original');
    if (userAgent) event.request = { headers: { 'User-Agent': userAgent } };

    // The ids are the event's tags and user when they are the record's own; the user id names the user, and the pseudo
    // id stays here beside it.
    const own = a === source.ids;
    const extra = copy(a, key => key === 'exception.type' || key === 'exception.message' || key === 'exception.stacktrace'
        || (own && (key === 'session.id' || (key === 'user.id' && !!userId) || (key === 'enduser.pseudo.id' && !userId))));
    if (extra) event.extra = extra;

    event.sdk = {
        name: browser ? 'bit.belemetry.javascript' : 'bit.belemetry.dotnet',
        version: str(resource, 'telemetry.sdk.version') || ctx.sdkVersion,
        settings: { infer_ip: ctx.maskIp ? 'never' : 'auto' },
    };
    return event;
}

/** The envelope of one event: its header, the item's header, and the event. */
export function toEnvelope(event: Json, ctx: SentryContext): Uint8Array {
    const item = encodeUtf8(JSON.stringify(event));
    const header = JSON.stringify({ event_id: event.event_id, dsn: ctx.dsn.publicDsn, sdk: { name: (event.sdk as Json).name, version: (event.sdk as Json).version } });
    const head = encodeUtf8(header + '\n' + JSON.stringify({ type: 'event', length: item.length }) + '\n');
    const body = new Uint8Array(head.length + item.length + 1);
    body.set(head, 0);
    body.set(item, head.length);
    body[body.length - 1] = 10;
    return body;
}

/**
 * The events of a batch, as C# picks them: a record carrying an exception, or a plain log record (no event name) with
 * a body, at or above the minimum level; an exception recorded on a span (unless the error limits dropped it) as an
 * error. Page views, web vitals and custom events are not events to Sentry.
 */
export function toEvents(batch: RecordBatch, ctx: SentryContext, newId: () => string = newTraceId): Json[] {
    const events: Json[] = [];
    const passes = (severityNumber: number) => severityNumber >= ctx.minimumSeverity;
    for (const span of batch.spans) {
        for (const event of span.events || []) {
            if (!isExceptionEvent(event) || !hasException(event.attributes) || event.limited || !passes(Severity.Error)) continue;
            events.push(toEvent({
                attributes: event.attributes, ids: span.attributes, severityNumber: Severity.Error, body: null, scope: span.scope, time: event.time,
                traceId: span.traceId, spanId: span.spanId, parentSpanId: span.parentSpanId, transaction: span.name,
            }, batch.resource, ctx, newId()));
        }
    }
    for (const log of batch.logs) {
        if (!passes(log.severityNumber)) continue;
        if (!hasException(log.attributes) && (log.eventName != null || !log.body)) continue;
        events.push(toEvent({
            attributes: log.attributes, ids: log.attributes, severityNumber: log.severityNumber, body: log.body, scope: log.scope, time: log.time,
            traceId: log.traceId, spanId: log.spanId, parentSpanId: null, transaction: null,
        }, batch.resource, ctx, newId()));
    }
    return events;
}

export function createSentryExporter(config: SentryExporterConfig, transport: Transport, sdkVersion: string): Exporter {
    const dsn = parseDsn(config.dsn);
    if (!dsn) {
        debug('sentry: invalid DSN');
        return { urls: [], export: () => Promise.resolve() };
    }
    const ctx: SentryContext = {
        dsn,
        release: config.release || null,
        minimumSeverity: typeof config.minimumSeverity === 'number' ? config.minimumSeverity : Severity.Error,
        maskIp: config.maskIp !== false,
        sdkVersion,
    };
    const compress = config.compression === 'gzip';
    const send = async (body: Uint8Array, unload: boolean, mark: ConsentMark | undefined): Promise<void> => {
        // Compression is async, and nothing async survives unload, so unload batches go uncompressed.
        const zipped = compress && !unload ? await gzip(body) : null;
        transport.send({
            exporter: 'sentry',
            url: dsn.envelopeUrl,
            contentType: 'application/x-sentry-envelope',
            contentEncoding: zipped ? 'gzip' : null,
            body: zipped || body,
            raw: zipped ? body : null,
        }, unload, mark);
    };
    return {
        urls: [dsn.envelopeEndpoint],
        export(batch, unload, mark) {
            // One event per envelope, as Sentry takes them; every send starts synchronously, which matters during unload.
            return Promise.all(toEvents(batch, ctx).map(event => send(toEnvelope(event, ctx), unload, mark))).then(() => undefined);
        },
    };
}
