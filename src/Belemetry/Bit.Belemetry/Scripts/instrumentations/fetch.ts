import { traceparent } from '../ids';
import type { SpanRecord } from '../model';
import { absoluteUrl, noop, type InstrumentationContext, type Uninstall } from './context';
import { createHttpTracer } from './http';

/** Client spans for window.fetch. */
export function installFetch(ctx: InstrumentationContext): Uninstall {
    const g = globalThis as unknown as { fetch?: typeof fetch };
    const original = g.fetch;
    if (typeof original !== 'function') return noop;
    const tracer = createHttpTracer(ctx);
    let active = true;

    const wrapped = function (input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
        let span: SpanRecord | null = null;
        try {
            if (active) {
                const request = typeof Request !== 'undefined' && input instanceof Request ? input : null;
                const url = absoluteUrl(request ? request.url : String(input));
                const headers = new Headers((init && init.headers) || (request ? request.headers : undefined));
                // A traceparent already present means .NET's HttpClient (WASM) traced this request itself.
                if (tracer.shouldTrace(url, headers.has('traceparent'))) {
                    const method = String((init && init.method) || (request && request.method) || 'GET').toUpperCase();
                    span = tracer.start(method, url);
                    if (tracer.shouldPropagate(url)) {
                        headers.set('traceparent', traceparent(span.traceId, span.spanId));
                        init = { ...(init || {}), headers };
                    }
                }
            }
        } catch {
            span = null;
        }
        const result = original.call(globalThis, input, init);
        if (!span) return result;
        const s = span;
        return result.then(response => {
            try {
                tracer.end(s, response.status);
            } catch {
                // Never throw into the host page.
            }
            return response;
        }, error => {
            try {
                tracer.end(s, 0, error);
            } catch {
                // Never throw into the host page.
            }
            throw error;
        });
    };

    g.fetch = wrapped as typeof fetch;
    return () => {
        active = false;
        if (g.fetch === wrapped) g.fetch = original;
    };
}
