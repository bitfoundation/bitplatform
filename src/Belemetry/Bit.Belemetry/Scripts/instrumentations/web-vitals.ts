// Web vitals measured from the Performance Timeline, following the logic of Google's web-vitals library in a much
// smaller form (no attribution, no bfcache restores). Values are reported on hide, when they are final enough.

import { Features } from '../features';
import { onHidden } from '../lifecycle';
import { Severity } from '../model';
import { createLog } from '../records';
import { guard, type InstrumentationContext, type Uninstall } from './context';

export type VitalName = 'LCP' | 'INP' | 'CLS' | 'FCP' | 'TTFB';
export type Rating = 'good' | 'needs-improvement' | 'poor';

export const WEB_VITALS_SCOPE = 'bit.belemetry.web_vitals';

/** The official "good" and "poor" boundaries. */
export const THRESHOLDS: Record<VitalName, [number, number]> = {
    LCP: [2500, 4000],
    INP: [200, 500],
    CLS: [0.1, 0.25],
    FCP: [1800, 3000],
    TTFB: [800, 1800],
};

export function rate(name: VitalName, value: number): Rating {
    const [good, poor] = THRESHOLDS[name];
    return value > poor ? 'poor' : value > good ? 'needs-improvement' : 'good';
}

/** CLS: layout shifts grouped in session windows (1 s gap, 5 s cap); the value is the largest window. */
export class ClsAccumulator {
    value = 0;
    private windowValue = 0;
    private first = 0;
    private last = 0;

    add(startTime: number, shift: number): void {
        if (this.windowValue && startTime - this.last < 1000 && startTime - this.first < 5000) {
            this.windowValue += shift;
        } else {
            this.windowValue = shift;
            this.first = startTime;
        }
        this.last = startTime;
        if (this.windowValue > this.value) this.value = this.windowValue;
    }
}

/**
 * INP: the longest event duration per interaction, keeping the 10 longest; the value is the p98 approximation of
 * the official library (skip one candidate per 50 interactions).
 */
export class InpAccumulator {
    count = 0;
    private longest: { id: number; latency: number }[] = [];

    add(interactionId: number, duration: number): void {
        if (!interactionId) return;
        const existing = this.longest.find(i => i.id === interactionId);
        if (existing) {
            existing.latency = Math.max(existing.latency, duration);
        } else {
            this.count++;
            const min = this.longest[this.longest.length - 1];
            if (this.longest.length < 10 || duration > min.latency) this.longest.push({ id: interactionId, latency: duration });
        }
        this.longest.sort((a, b) => b.latency - a.latency);
        this.longest.splice(10);
    }

    value(interactionCount?: number): number | null {
        if (!this.longest.length) return null;
        const count = interactionCount || this.count;
        return this.longest[Math.min(this.longest.length - 1, Math.floor(count / 50))].latency;
    }
}

interface EventTimingEntry extends PerformanceEntry {
    interactionId?: number;
}

interface LayoutShiftEntry extends PerformanceEntry {
    value: number;
    hadRecentInput: boolean;
}

function navigationEntry(): (PerformanceNavigationTiming & { activationStart?: number }) | null {
    try {
        const entry = performance.getEntriesByType('navigation')[0];
        return entry ? entry as PerformanceNavigationTiming : null;
    } catch {
        return null;
    }
}

function observe(type: string, callback: (entries: PerformanceEntry[]) => void, options: Record<string, unknown> = {}): PerformanceObserver | null {
    try {
        if (typeof PerformanceObserver === 'undefined') return null;
        const supported = PerformanceObserver.supportedEntryTypes;
        if (supported && supported.indexOf(type) < 0) return null;
        const observer = new PerformanceObserver(guard((list: PerformanceObserverEntryList) => callback(list.getEntries())));
        observer.observe({ type, buffered: true, ...options } as PerformanceObserverInit);
        return observer;
    } catch {
        return null;
    }
}

// Module-level so a reinstall (consent granted again) does not report the same values twice.
let reported: Partial<Record<VitalName, number>> = {};
let ids: Partial<Record<VitalName, string>> = {};

/** Resets the reported values (tests only). */
export function resetWebVitalsState(): void {
    reported = {};
    ids = {};
}

export function installWebVitals(ctx: InstrumentationContext): Uninstall {
    const nav = navigationEntry();
    const activationStart = (nav && nav.activationStart) || 0;
    const prerendering = (document as Document & { prerendering?: boolean }).prerendering;
    const navigationType = prerendering || activationStart > 0 ? 'prerender' : nav ? String(nav.type).replace(/_/g, '-') : 'navigate';
    // Entries after the page was first hidden do not count for LCP and FCP.
    let firstHidden = document.visibilityState === 'hidden' ? 0 : Infinity;
    const observers: PerformanceObserver[] = [];

    const report = (name: VitalName, value: number) => {
        const previous = reported[name];
        if (previous === value) return;
        reported[name] = value;
        const id = ids[name] || (ids[name] = 'v1-' + Date.now() + '-' + Math.floor(Math.random() * 9e12 + 1e12));
        ctx.emit(createLog(WEB_VITALS_SCOPE, Features.Performance, Severity.Info, name, 'browser.web_vital', {
            'web_vital.name': name,
            'web_vital.value': value,
            'web_vital.delta': value - (previous || 0),
            'web_vital.rating': rate(name, value),
            'web_vital.id': id,
            'web_vital.navigation_type': navigationType,
        }));
    };
    const track = (observer: PerformanceObserver | null) => {
        if (observer) observers.push(observer);
        return observer;
    };

    // TTFB: final once the response started.
    if (nav && nav.responseStart > 0) report('TTFB', Math.max(0, nav.responseStart - activationStart));

    // FCP: final when it happens.
    const fcp = track(observe('paint', entries => {
        for (const entry of entries) {
            if (entry.name === 'first-contentful-paint' && entry.startTime < firstHidden) {
                report('FCP', Math.max(0, entry.startTime - activationStart));
                if (fcp) fcp.disconnect();
            }
        }
    }));

    // LCP: the latest candidate, final at the first input or hide.
    let lcp: number | null = null;
    let lcpDone = false;
    const onLcp = (entries: PerformanceEntry[]) => {
        for (const entry of entries) if (entry.startTime < firstHidden) lcp = Math.max(0, entry.startTime - activationStart);
    };
    const lcpObserver = track(observe('largest-contentful-paint', onLcp));
    const finalizeLcp = guard(() => {
        if (lcpDone) return;
        lcpDone = true;
        if (lcpObserver) {
            onLcp(lcpObserver.takeRecords());
            lcpObserver.disconnect();
        }
        if (lcp !== null) report('LCP', lcp);
        for (const type of ['keydown', 'click']) removeEventListener(type, finalizeLcp, true);
    });
    for (const type of ['keydown', 'click']) addEventListener(type, finalizeLcp, true);

    // CLS.
    const cls = new ClsAccumulator();
    let sawShift = false;
    const onShift = (entries: PerformanceEntry[]) => {
        for (const entry of entries as LayoutShiftEntry[]) {
            if (!entry.hadRecentInput) {
                sawShift = true;
                cls.add(entry.startTime, entry.value);
            }
        }
    };
    const clsObserver = track(observe('layout-shift', onShift));

    // INP.
    const inp = new InpAccumulator();
    const onEvents = (entries: PerformanceEntry[]) => {
        for (const entry of entries as EventTimingEntry[]) inp.add(entry.interactionId || 0, entry.duration);
    };
    const inpObserver = track(observe('event', onEvents, { durationThreshold: 40 }));
    track(observe('first-input', onEvents));

    const onHide = () => {
        if (firstHidden === Infinity) firstHidden = performance.now();
        finalizeLcp();
        if (clsObserver) onShift(clsObserver.takeRecords());
        // CLS is 0 on a page that rendered and never shifted, which is worth reporting.
        if (clsObserver && (sawShift || reported.FCP !== undefined)) report('CLS', cls.value);
        if (inpObserver) onEvents(inpObserver.takeRecords());
        const count = (performance as Performance & { interactionCount?: number }).interactionCount;
        const value = inp.value(count);
        if (value !== null) report('INP', value);
    };
    const removeHidden = onHidden(onHide, 'first');

    return () => {
        removeHidden();
        for (const observer of observers) observer.disconnect();
        for (const type of ['keydown', 'click']) removeEventListener(type, finalizeLcp, true);
    };
}
