var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _perfObservers: { [id: string]: PerformanceObserver } = {};

    // These entry types are never kept on the performance timeline: getEntriesByType() answers them
    // with an empty array on every engine, and they only ever arrive through a PerformanceObserver.
    // So reading one has to mean "what an observer has collected", and this module keeps its own
    // records for them - see retainedEntries().
    const OBSERVER_ONLY_TYPES = ['longtask', 'long-animation-frame', 'largest-contentful-paint', 'layout-shift', 'event', 'first-input', 'element'];

    // type -> the entries an observer of that type has reported so far, once something has asked.
    const _retained: { [type: string]: any[] } = {};

    // type -> the observer filling _retained[type], so stopRetained() can shut them down. They are
    // not in _perfObservers: that map is keyed by a subscription id and disconnect() answers to a
    // caller who asked for one, while these belong to the module rather than to any one caller.
    const _retainedObservers: { [type: string]: PerformanceObserver } = {};

    // The most recent entries kept per type. These observers run for the life of the document, and
    // an interaction-heavy page produces 'event' entries without end - so the records are a window
    // rather than a log, both to bound memory and to bound what each read marshals back to .NET.
    // 250 matches the resource buffer the platform itself keeps.
    const RETAINED_MAX = 250;

    // What has to run when this module is torn down but is not this module's to run - the Web
    // Vitals accumulator stopping its own observers. A hook rather than a call into
    // performanceVitals, so that module can depend on this one and not the other way around.
    const _stopHooks: (() => void)[] = [];

    // The performance timeline: marks, measures, entries and observers. The Web Vitals accumulator
    // is performanceVitals - it runs observers for the life of the document and carries the CLS
    // session-window and INP percentile logic, none of which a page that only measures its own
    // marks has any use for.
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
            // An observer-only type would come back empty from the timeline whatever it was asked -
            // answer it from this module's own records instead.
            if (type && OBSERVER_ONLY_TYPES.indexOf(type) >= 0) return retainedEntries(type, name);

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
        },
        // Stops every observer this module started for itself - the ones behind the observer-only
        // entry types and the ones behind the Web Vitals accumulator - and drops what they collected.
        // Called when the Performance service is disposed: the scope owning it is the document's, so
        // the records have no reader left and the observers have no reason to keep filling them.
        stopRetained() {
            for (const type of Object.keys(_retainedObservers)) {
                _retainedObservers[type].disconnect();
                delete _retainedObservers[type];
                delete _retained[type];
            }

            for (const hook of _stopHooks) hook();
        },

        // For the modules layered on this one.
        observeVital,
        onStopRetained(hook: () => void) { _stopHooks.push(hook); }
    };

    function supportsEntryType(type: string) {
        // Read the constructor off window rather than as a bare identifier: optional chaining does
        // not rescue an undeclared global, so `PerformanceObserver?.x` is still a ReferenceError on
        // an engine that has no PerformanceObserver at all.
        const types = (window as any).PerformanceObserver?.supportedEntryTypes;
        return Array.isArray(types) && types.indexOf(type) >= 0;
    }

    // Returns the observer it started, or null when the type isn't collectable here - callers test
    // it for truthiness where all they wanted was "did this metric start".
    function observeVital(type: string, handler: (list: PerformanceObserverEntryList) => void, options: any = {}): PerformanceObserver | null {
        if (!supportsEntryType(type)) return null;
        try {
            const observer = new PerformanceObserver(handler);
            observer.observe({ type, buffered: true, ...options });
            return observer;
        } catch {
            // The type is advertised but rejected here (a permissions policy, an unsupported
            // option) - leave the metric at null rather than reporting a number nothing feeds.
            return null;
        }
    }

    // Appends what an observer reported to the records of its type, as plain JSON, keeping only the
    // most recent RETAINED_MAX of them. Shared by the observer callback and the synchronous drain
    // below so both apply the same conversion and the same window.
    function retain(type: string, entries: any[]) {
        const bucket = _retained[type];
        if (!bucket) return;
        for (const entry of entries) bucket.push(entry.toJSON ? entry.toJSON() : entry);
        if (bucket.length > RETAINED_MAX) bucket.splice(0, bucket.length - RETAINED_MAX);
    }

    // The records for one observer-only type. The first ask starts the observer; buffered:true
    // backfills what the engine held from before it existed, but that report would only reach the
    // callback on a later task - so the queue is drained synchronously here too, and the first read
    // returns what was already buffered. Entries produced after the call still need a later read.
    function retainedEntries(type: string, name?: string) {
        if (!_retained[type]) {
            _retained[type] = [];
            // durationThreshold below the 104ms default so short interactions are counted too, for
            // the same reason the vitals collector lowers it.
            const options = type === 'event' ? { durationThreshold: 16 } : {};
            const observer = observeVital(type, list => retain(type, list.getEntries()), options);
            if (observer) {
                _retainedObservers[type] = observer;
                // takeRecords() hands over the queued reports - the buffered backfill among them -
                // without waiting for the task that would have delivered them to the callback.
                try { retain(type, observer.takeRecords()); } catch { /* not implemented here */ }
            }
        }

        const entries = _retained[type];
        return name ? entries.filter(e => e.name === name) : entries.slice();
    }
}(BitButil));
