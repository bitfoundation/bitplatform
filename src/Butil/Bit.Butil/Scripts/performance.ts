var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _perfObservers: { [id: string]: PerformanceObserver } = {};

    // The Web Vitals accumulator. All three metrics are running totals over the life of the
    // document rather than values that can be read on demand, so the observers behind them stay up
    // from the first webVitals() call onwards and this object is what they write into. Every metric
    // starts as null and only becomes a number once the engine proves it reports that entry type -
    // a 0 CLS on a browser with no layout-shift support would be a lie, not a good score.
    const _vitals: any = {
        started: false,
        lcp: null, cls: null, inp: null, fcp: null, ttfb: null,
        interactionCount: 0, layoutShiftCount: 0
    };

    // The largest CLS "session window" seen so far, and the window currently open. A window ends
    // after a 1s gap between shifts or 5s in total, and CLS is the worst window - not the sum.
    let _clsWindowValue = 0;
    let _clsWindowStart = 0;
    let _clsWindowLast = 0;

    // interactionId -> the longest duration seen for that interaction. One tap produces several
    // events sharing an id, and the interaction's latency is the worst of them, not their sum.
    const _interactions: { [id: string]: number } = {};

    function supportsEntryType(type: string) {
        const types = (PerformanceObserver as any)?.supportedEntryTypes;
        return Array.isArray(types) && types.indexOf(type) >= 0;
    }

    function observeVital(type: string, handler: (list: PerformanceObserverEntryList) => void, options: any = {}) {
        if (!supportsEntryType(type)) return false;
        try {
            new PerformanceObserver(handler).observe({ type, buffered: true, ...options });
            return true;
        } catch {
            // The type is advertised but rejected here (a permissions policy, an unsupported
            // option) - leave the metric at null rather than reporting a number nothing feeds.
            return false;
        }
    }

    function startWebVitals() {
        if (_vitals.started || !('PerformanceObserver' in window)) return;
        _vitals.started = true;

        // LCP reports a new, larger candidate each time one paints; the one that counts is the last.
        observeVital('largest-contentful-paint', list => {
            const entries = list.getEntries() as any[];
            const last = entries[entries.length - 1];
            if (last) _vitals.lcp = last.renderTime || last.loadTime || last.startTime;
        });

        if (observeVital('layout-shift', list => {
            for (const entry of list.getEntries() as any[]) addLayoutShift(entry);
        })) _vitals.cls = 0;

        // durationThreshold below the 104ms default so short interactions are counted too: INP is a
        // percentile over the interactions there were, and dropping the fast ones inflates it.
        const events = observeVital('event', list => {
            for (const entry of list.getEntries() as any[]) addInteraction(entry);
        }, { durationThreshold: 16 });

        // first-input is reported by engines that have no 'event' support at all, so it is the
        // fallback rather than an addition.
        if (!events) {
            observeVital('first-input', list => {
                for (const entry of list.getEntries() as any[]) addInteraction(entry);
            });
        }

        observeVital('paint', list => {
            for (const entry of list.getEntries()) {
                if (entry.name === 'first-contentful-paint') _vitals.fcp = entry.startTime;
            }
        });

        const nav = performance.getEntriesByType('navigation')[0] as any;
        // activationStart is non-zero only for a prerendered document, where the other timestamps
        // are relative to the prerender rather than to the moment the user saw the page.
        if (nav) _vitals.ttfb = Math.max(0, nav.responseStart - (nav.activationStart || 0));
    }

    function addLayoutShift(entry: any) {
        // A shift within 500ms of user input is the user's own doing and is excluded from CLS.
        if (entry.hadRecentInput) return;

        _vitals.layoutShiftCount++;

        if (_clsWindowValue > 0 && (entry.startTime - _clsWindowLast > 1000 || entry.startTime - _clsWindowStart > 5000)) {
            _clsWindowValue = 0;
        }
        if (_clsWindowValue === 0) _clsWindowStart = entry.startTime;
        _clsWindowLast = entry.startTime;
        _clsWindowValue += entry.value;

        if (_clsWindowValue > _vitals.cls) _vitals.cls = _clsWindowValue;
    }

    function addInteraction(entry: any) {
        const id = entry.interactionId;
        // Events that are not part of an interaction carry id 0 and are not INP's business.
        if (!id) return;

        const previous = _interactions[id];
        if (previous === undefined) {
            _interactions[id] = entry.duration;
            _vitals.interactionCount++;
        } else if (entry.duration > previous) {
            _interactions[id] = entry.duration;
        }
    }

    // INP is not the worst interaction: it is the worst discounted by one for every 50 interactions,
    // so a single outlier in a long session does not define the page.
    function computeInp() {
        const durations = Object.keys(_interactions).map(k => _interactions[k]).sort((a, b) => b - a);
        if (durations.length === 0) return null;

        const index = Math.min(durations.length - 1, Math.floor(_vitals.interactionCount / 50));
        return durations[index];
    }

    butil.performance = {
        now() { return performance.now(); },
        timeOrigin() { return performance.timeOrigin; },
        mark(name: string) { performance.mark(name); },
        measure(name: string, startMark?: string, endMark?: string) {
            // measure() rejects undefined start/end; pass them only when set.
            if (startMark && endMark) performance.measure(name, startMark, endMark);
            else if (startMark) performance.measure(name, startMark);
            else performance.measure(name);
        },
        clearMarks(name?: string) { performance.clearMarks(name ?? undefined); },
        clearMeasures(name?: string) { performance.clearMeasures(name ?? undefined); },
        clearResourceTimings() { performance.clearResourceTimings(); },
        getEntries(name?: string, type?: string) {
            let entries: PerformanceEntry[];
            if (name) entries = performance.getEntriesByName(name, type ?? undefined);
            else if (type) entries = performance.getEntriesByType(type);
            else entries = performance.getEntries();
            // toJSON exists on entries; map to plain objects so dotnet can deserialize.
            return entries.map(e => (e as any).toJSON ? (e as any).toJSON() : e);
        },
        memory() {
            const m = (performance as any).memory;
            if (!m) return { jsHeapSizeLimit: null, totalJsHeapSize: null, usedJsHeapSize: null };
            return {
                jsHeapSizeLimit: m.jsHeapSizeLimit ?? null,
                totalJsHeapSize: m.totalJSHeapSize ?? null,
                usedJsHeapSize: m.usedJSHeapSize ?? null
            };
        },
        webVitals() {
            startWebVitals();
            return {
                lcp: _vitals.lcp,
                cls: _vitals.cls,
                inp: computeInp(),
                fcp: _vitals.fcp,
                ttfb: _vitals.ttfb,
                interactionCount: _vitals.interactionCount,
                layoutShiftCount: _vitals.layoutShiftCount
            };
        },
        observe(dotNetRef: any, listenerId: string, entryTypes: string[], buffered: boolean) {
            if (!('PerformanceObserver' in window) || !entryTypes?.length) return;
            const observer = new PerformanceObserver(list => {
                const payload = list.getEntries().map(e => (e as any).toJSON ? (e as any).toJSON() : e);
                butil.utils.dispatch(dotNetRef, 'InvokePerformanceObserver', listenerId, payload);
            });
            try {
                // observe() with a "type" + "buffered" can only handle one entry type at a time;
                // loop so we register each one separately and merge their reports.
                for (const t of entryTypes) {
                    try { observer.observe({ type: t, buffered }); }
                    catch { /* type isn't supported on this UA - skip silently */ }
                }
            } catch { /* observe() rejected the whole batch - fall through with no records */ }
            _perfObservers[listenerId] = observer;
        },
        disconnect(listenerId: string) {
            const observer = _perfObservers[listenerId];
            if (!observer) return;
            delete _perfObservers[listenerId];
            observer.disconnect();
        }
    };
}(BitButil));
