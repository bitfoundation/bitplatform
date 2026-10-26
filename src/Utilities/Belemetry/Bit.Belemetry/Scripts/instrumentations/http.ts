// Shared by the fetch and XHR instrumentations: which requests to trace, where to propagate trace context, and
// the client span shape.

import { now } from '../clock';
import { Features } from '../features';
import { SpanKind, StatusCode, type SpanRecord } from '../model';
import { createSpan } from '../records';
import { isSuppressed } from '../transport';
import { compilePatterns, matchesAny, type InstrumentationContext } from './context';

export const DEPENDENCIES_SCOPE = 'bit.belemetry.http';

/**
 * The fetch option .NET's exporter sets in WebAssembly (`BelemetryHttpSender.ExportFetchOption`): an export, whatever
 * its URL, even when this script was started by a server's configuration, which lists no .NET exporter URLs.
 */
export const DOTNET_EXPORT_OPTION = 'bitBelemetryExport';

// The methods the HTTP conventions know; anything else is `_OTHER` (with the original kept), so a typo or a custom
// verb cannot add a span name or a metric series per value.
const KNOWN_METHODS = ['CONNECT', 'DELETE', 'GET', 'HEAD', 'OPTIONS', 'PATCH', 'POST', 'PUT', 'QUERY', 'TRACE'];

/** How a request without a response ended: cancelled by the app, timed out, or failed on the network. */
export type HttpFailure = { name?: string; message?: string } | null | undefined;

export interface HttpTracer {
    /** Whether a request should be traced (not an export, not ignored, not already traced by .NET). */
    shouldTrace(url: string, hasTraceparent: boolean): boolean;
    /** Whether `traceparent` may be added for this URL (`propagateTraceContextTo`). */
    shouldPropagate(url: string): boolean;
    start(method: string, url: string): SpanRecord;
    end(span: SpanRecord, status: number, error?: unknown): void;
}

/** Matches `"same-origin"`, `"*"` or a regex source against an absolute URL. */
export function createPropagationMatcher(targets: string[] | null | undefined): (url: string) => boolean {
    const list = targets || [];
    const sameOrigin = list.indexOf('same-origin') >= 0;
    const all = list.indexOf('*') >= 0;
    const patterns = compilePatterns(list.filter(t => t !== 'same-origin' && t !== '*'));
    return (url: string) => {
        if (all) return true;
        if (sameOrigin) {
            try {
                if (new URL(url, location.href).origin === location.origin) return true;
            } catch {
                // Not a URL: no propagation.
            }
        }
        return matchesAny(patterns, url);
    };
}

export function createHttpTracer(ctx: InstrumentationContext): HttpTracer {
    // Recompiled when the list grows: a later start (a WebAssembly client's) appends its exporter URLs.
    let ignore: RegExp[] = [];
    let compiled = -1;
    const ignored = (url: string) => {
        const list = ctx.config.dependencies.ignoreUrls || [];
        if (list.length !== compiled) {
            ignore = compilePatterns(list);
            compiled = list.length;
        }
        return matchesAny(ignore, url);
    };
    const propagate = createPropagationMatcher(ctx.config.dependencies.propagateTraceContextTo);
    return {
        shouldTrace: (url, hasTraceparent) => !hasTraceparent && !isSuppressed() && !ctx.isExporterUrl(url) && !ignored(url),
        shouldPropagate: propagate,
        start(method, url) {
            const known = KNOWN_METHODS.indexOf(method) >= 0;
            // The conventions name a span of an unknown method "HTTP".
            const span = createSpan(DEPENDENCIES_SCOPE, Features.Dependencies, known ? method : 'HTTP', SpanKind.Client, now(), 0);
            span.attributes['http.request.method'] = known ? method : '_OTHER';
            if (!known) span.attributes['http.request.method_original'] = method;
            span.attributes['url.full'] = url;
            try {
                const parsed = new URL(url);
                span.attributes['server.address'] = parsed.hostname;
                span.attributes['server.port'] = Number(parsed.port) || (parsed.protocol === 'https:' ? 443 : 80);
            } catch {
                // Relative or opaque URL: no server attributes.
            }
            return span;
        },
        end(span, status, error) {
            span.endTime = now();
            if (status > 0) span.attributes['http.response.status_code'] = status;
            if (error !== undefined) {
                const e = error as HttpFailure;
                const type = (e && e.name) || 'Error';
                span.attributes['error.type'] = type;
                // A request the app cancelled itself (navigation, a superseded search) did not fail: the conventions
                // leave its status unset, so it does not count against the dependency's success rate.
                if (type !== 'AbortError') span.status = { code: StatusCode.Error, message: (e && e.message) || String(error) };
            } else if (status >= 400) {
                span.attributes['error.type'] = String(status);
                span.status = { code: StatusCode.Error };
            }
            ctx.emit(span);
        },
    };
}
