import { toEpoch } from '../clock';
import { Features } from '../features';
import { SpanKind } from '../model';
import { createSpan } from '../records';
import { guard, noop, type InstrumentationContext, type Uninstall } from './context';

export const DOCUMENT_LOAD_SCOPE = 'bit.belemetry.document_load';

const EVENTS = ['fetchStart', 'domainLookupStart', 'domainLookupEnd', 'connectStart', 'secureConnectionStart', 'connectEnd', 'requestStart',
    'responseStart', 'responseEnd', 'domInteractive', 'domContentLoadedEventStart', 'domContentLoadedEventEnd', 'domComplete', 'loadEventStart',
    'loadEventEnd'] as const;

let emitted = false;

/** Resets the once-per-page flag (tests only). */
export function resetDocumentLoadState(): void {
    emitted = false;
}

function emit(ctx: InstrumentationContext): void {
    if (emitted) return;
    const nav = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;
    if (!nav) return;
    emitted = true;
    const span = createSpan(DOCUMENT_LOAD_SCOPE, Features.Performance, 'documentLoad', SpanKind.Internal, toEpoch(nav.startTime),
        toEpoch(nav.loadEventEnd || nav.duration || performance.now()));
    span.attributes['url.full'] = location.href;
    if (nav.encodedBodySize) span.attributes['http.response.body.size'] = nav.encodedBodySize;
    if (nav.nextHopProtocol) span.attributes['network.protocol.name'] = nav.nextHopProtocol;
    for (const name of EVENTS) {
        const value = nav[name];
        if (typeof value === 'number' && value > 0) span.events.push({ name, time: toEpoch(value), attributes: {} });
    }
    ctx.emit(span);
}

/** The `documentLoad` span from PerformanceNavigationTiming, once per page, after `load` (so loadEventEnd is set). */
export function installDocumentLoad(ctx: InstrumentationContext): Uninstall {
    if (emitted) return noop;
    let timer: ReturnType<typeof setTimeout> | null = null;
    const run = guard(() => {
        timer = setTimeout(guard(() => emit(ctx)), 0);
    });
    if (document.readyState === 'complete') run();
    else addEventListener('load', run);
    return () => {
        removeEventListener('load', run);
        if (timer) clearTimeout(timer);
    };
}
