var BitButil = BitButil || {};

(function (butil: any) {
    const _perfObservers: { [id: string]: PerformanceObserver } = {};

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
