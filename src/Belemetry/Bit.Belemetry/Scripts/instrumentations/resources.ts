import { toEpoch } from '../clock';
import { Features } from '../features';
import { SpanKind } from '../model';
import { createSpan } from '../records';
import { guard, noop, type InstrumentationContext, type Uninstall } from './context';

export const RESOURCES_SCOPE = 'bit.belemetry.resources';

// fetch and XHR are covered by the dependency spans; beacons are usually telemetry.
const SKIPPED_INITIATORS = ['fetch', 'xmlhttprequest', 'beacon'];

/** `resourceFetch` spans from resource timing, excluding exporter URLs. */
export function installResources(ctx: InstrumentationContext): Uninstall {
    if (typeof PerformanceObserver === 'undefined') return noop;
    const onEntries = (entries: PerformanceEntry[]) => {
        for (const entry of entries as PerformanceResourceTiming[]) {
            if (SKIPPED_INITIATORS.indexOf(entry.initiatorType) >= 0 || ctx.isExporterUrl(entry.name)) continue;
            const span = createSpan(RESOURCES_SCOPE, Features.Performance, 'resourceFetch', SpanKind.Internal, toEpoch(entry.startTime), toEpoch(entry.responseEnd || entry.startTime + entry.duration));
            span.attributes['url.full'] = entry.name;
            if (entry.encodedBodySize) span.attributes['http.response.body.size'] = entry.encodedBodySize;
            if (entry.nextHopProtocol) span.attributes['network.protocol.name'] = entry.nextHopProtocol;
            ctx.emit(span);
        }
    };
    try {
        const observer = new PerformanceObserver(guard((list: PerformanceObserverEntryList) => onEntries(list.getEntries())));
        observer.observe({ type: 'resource', buffered: true });
        return () => observer.disconnect();
    } catch {
        return noop;
    }
}
