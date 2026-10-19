import { toEpoch } from '../clock';
import { Features } from '../features';
import { SpanKind } from '../model';
import { createSpan } from '../records';
import { guard, noop, type InstrumentationContext, type Uninstall } from './context';

export const LONG_TASKS_SCOPE = 'bit.belemetry.long_tasks';

/** `longtask` spans (main-thread tasks over 50 ms). */
export function installLongTasks(ctx: InstrumentationContext): Uninstall {
    if (typeof PerformanceObserver === 'undefined') return noop;
    try {
        const supported = PerformanceObserver.supportedEntryTypes;
        if (supported && supported.indexOf('longtask') < 0) return noop;
        const observer = new PerformanceObserver(guard((list: PerformanceObserverEntryList) => {
            for (const entry of list.getEntries()) {
                const start = toEpoch(entry.startTime);
                ctx.emit(createSpan(LONG_TASKS_SCOPE, Features.Performance, 'longtask', SpanKind.Internal, start, start + entry.duration));
            }
        }));
        observer.observe({ type: 'longtask', buffered: true });
        return () => observer.disconnect();
    } catch {
        return noop;
    }
}
