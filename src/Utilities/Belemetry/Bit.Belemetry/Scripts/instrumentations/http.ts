// Shared by the fetch and XHR instrumentations: which requests to trace, where to propagate trace context, and
// the client span shape.

import { now } from '../clock';
import { Features } from '../features';
import { SpanKind, StatusCode, type SpanRecord } from '../model';
import { createSpan } from '../records';
import { isSuppressed } from '../transport';
import { compilePatterns, matchesAny, type InstrumentationContext } from './context';

export const DEPENDENCIES_SCOPE = 'bit.belemetry.http';

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
    const ignore = compilePatterns(ctx.config.dependencies.ignoreUrls);
    const propagate = createPropagationMatcher(ctx.config.dependencies.propagateTraceContextTo);
    return {
        shouldTrace: (url, hasTraceparent) => !hasTraceparent && !isSuppressed() && !ctx.isExporterUrl(url) && !matchesAny(ignore, url),
        shouldPropagate: propagate,
        start(method, url) {
            const span = createSpan(DEPENDENCIES_SCOPE, Features.Dependencies, method, SpanKind.Client, now(), 0);
            span.attributes['http.request.method'] = method;
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
                const e = error as { name?: string; message?: string } | null;
                span.attributes['error.type'] = (e && e.name) || 'Error';
                span.status = { code: StatusCode.Error, message: (e && e.message) || String(error) };
            } else if (status >= 400) {
                span.attributes['error.type'] = String(status);
                span.status = { code: StatusCode.Error };
            }
            ctx.emit(span);
        },
    };
}
