import { traceparent } from '../ids';
import { absoluteUrl, noop, type InstrumentationContext, type Uninstall } from './context';
import { createHttpTracer } from './http';

interface XhrInfo {
    method: string;
    url: string;
    traced: boolean;
}

/** Client spans for XMLHttpRequest. */
export function installXhr(ctx: InstrumentationContext): Uninstall {
    const Xhr = (globalThis as unknown as { XMLHttpRequest?: typeof XMLHttpRequest }).XMLHttpRequest;
    if (typeof Xhr !== 'function') return noop;
    const proto = Xhr.prototype;
    const originalOpen = proto.open;
    const originalSend = proto.send;
    const originalSetRequestHeader = proto.setRequestHeader;
    const tracer = createHttpTracer(ctx);
    const info = new WeakMap<XMLHttpRequest, XhrInfo>();
    let active = true;

    const open = function (this: XMLHttpRequest, ...args: unknown[]) {
        try {
            if (active) info.set(this, { method: String(args[0]).toUpperCase(), url: absoluteUrl(String(args[1])), traced: false });
        } catch {
            // Never throw into the host page.
        }
        return (originalOpen as (...a: unknown[]) => void).apply(this, args);
    };

    const setRequestHeader = function (this: XMLHttpRequest, name: string, value: string) {
        try {
            const data = info.get(this);
            if (data && String(name).toLowerCase() === 'traceparent') data.traced = true;
        } catch {
            // Never throw into the host page.
        }
        return originalSetRequestHeader.call(this, name, value);
    };

    const send = function (this: XMLHttpRequest, body?: Document | XMLHttpRequestBodyInit | null) {
        try {
            const data = info.get(this);
            if (active && data && tracer.shouldTrace(data.url, data.traced)) {
                const span = tracer.start(data.method, data.url);
                if (tracer.shouldPropagate(data.url)) originalSetRequestHeader.call(this, 'traceparent', traceparent(span.traceId, span.spanId));
                const xhr = this;
                const onEnd = () => {
                    try {
                        xhr.removeEventListener('loadend', onEnd);
                        if (xhr.status > 0) tracer.end(span, xhr.status);
                        else tracer.end(span, 0, { name: 'NetworkError', message: 'XMLHttpRequest failed' });
                    } catch {
                        // Never throw into the host page.
                    }
                };
                xhr.addEventListener('loadend', onEnd);
            }
        } catch {
            // Never throw into the host page.
        }
        return originalSend.call(this, body);
    };

    proto.open = open as unknown as typeof proto.open;
    proto.setRequestHeader = setRequestHeader;
    proto.send = send;
    return () => {
        active = false;
        if (proto.open === open) proto.open = originalOpen;
        if (proto.setRequestHeader === setRequestHeader) proto.setRequestHeader = originalSetRequestHeader;
        if (proto.send === send) proto.send = originalSend;
    };
}
