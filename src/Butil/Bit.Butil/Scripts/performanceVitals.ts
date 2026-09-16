var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
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

    // The interactions INP can still be answered by, worst first, and the same records keyed by
    // interactionId. One tap produces several events sharing an id, and the interaction's latency is
    // the worst of them, not their sum - hence the map.
    //
    // Only the worst MAX_INTERACTIONS can ever be the answer: INP indexes this list at
    // floor(count / 50) capped to its own length, so an entry past the cap is unreachable however
    // long the session runs. Keeping every interaction of a long-lived document would grow without
    // bound - and make every read sort a list that only its first ten entries are read from.
    const MAX_INTERACTIONS = 10;
    let _interactionList: { id: number, duration: number }[] = [];
    let _interactions: { [id: string]: { id: number, duration: number } } = {};

    // The observers behind the Web Vitals accumulator: webVitals() starts them, no caller holds a
    // handle to them, and the performance module's stopRetained() is the only thing that stops them.
    const _vitalObservers: PerformanceObserver[] = [];

    butil.performance.onStopRetained(stopWebVitals);

    butil.performanceVitals = {
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
        }
    };

    // observeVital plus the bookkeeping the accumulator needs: an observer started for a Web Vital
    // has no other owner, so this list is the only way stopWebVitals() can find it again.
    function observeAccumulator(type: string, handler: (list: PerformanceObserverEntryList) => void, options: any = {}): PerformanceObserver | null {
        const observer = butil.performance.observeVital(type, handler, options);
        if (observer) _vitalObservers.push(observer);
        return observer;
    }

    function startWebVitals() {
        if (_vitals.started || !('PerformanceObserver' in window)) return;
        _vitals.started = true;

        // LCP reports a new, larger candidate each time one paints; the one that counts is the last.
        observeAccumulator('largest-contentful-paint', list => {
            const entries = list.getEntries() as any[];
            const last = entries[entries.length - 1];
            if (last) _vitals.lcp = last.renderTime || last.loadTime || last.startTime;
        });

        if (observeAccumulator('layout-shift', list => {
            for (const entry of list.getEntries() as any[]) addLayoutShift(entry);
        })) _vitals.cls = 0;

        // durationThreshold below the 104ms default so short interactions are counted too: INP is a
        // percentile over the interactions there were, and dropping the fast ones inflates it.
        const events = observeAccumulator('event', list => {
            for (const entry of list.getEntries() as any[]) addInteraction(entry);
        }, { durationThreshold: 16 });

        // first-input is reported by engines that have no 'event' support at all, so it is the
        // fallback rather than an addition.
        if (!events) {
            observeAccumulator('first-input', list => {
                for (const entry of list.getEntries() as any[]) addInteraction(entry);
            });
        }

        observeAccumulator('paint', list => {
            for (const entry of list.getEntries()) {
                if (entry.name === 'first-contentful-paint') _vitals.fcp = entry.startTime;
            }
        });

        const nav = performance.getEntriesByType('navigation')[0] as any;
        // activationStart is non-zero only for a prerendered document, where the other timestamps
        // are relative to the prerender rather than to the moment the user saw the page.
        if (nav) _vitals.ttfb = Math.max(0, nav.responseStart - (nav.activationStart || 0));
    }

    // Disconnects the accumulator's observers and puts it back to its pre-first-call state. Both
    // halves matter: metrics that are running totals of observers no longer running would report a
    // frozen score as a live one, and started:false is what lets a later webVitals() - a new circuit,
    // a new scope - start collecting again rather than reading the leftovers of the last one.
    // buffered:true backfills whatever the engine still holds when it does.
    function stopWebVitals() {
        for (const observer of _vitalObservers) observer.disconnect();
        _vitalObservers.length = 0;

        _vitals.started = false;
        _vitals.lcp = _vitals.cls = _vitals.inp = _vitals.fcp = _vitals.ttfb = null;
        _vitals.interactionCount = 0;
        _vitals.layoutShiftCount = 0;

        _clsWindowValue = 0;
        _clsWindowStart = 0;
        _clsWindowLast = 0;

        _interactionList = [];
        _interactions = {};
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

        // An id already on the list: the interaction's latency is its worst event, so this only ever
        // raises the record - and the list is at most MAX_INTERACTIONS long, so re-sorting it is free.
        const kept = _interactions[id];
        if (kept) {
            if (entry.duration > kept.duration) {
                kept.duration = entry.duration;
                _interactionList.sort((a, b) => b.duration - a.duration);
            }
            return;
        }

        // The count is every interaction there was, not every one kept - it is what INP's index is
        // derived from, and discarding the fast ones from it would move that index.
        _vitals.interactionCount++;

        const weakest = _interactionList[_interactionList.length - 1];
        if (_interactionList.length >= MAX_INTERACTIONS && entry.duration <= weakest.duration) return;

        const record = { id, duration: entry.duration };
        _interactions[id] = record;
        _interactionList.push(record);
        _interactionList.sort((a, b) => b.duration - a.duration);

        while (_interactionList.length > MAX_INTERACTIONS) {
            delete _interactions[_interactionList.pop()!.id];
        }
    }

    // INP is not the worst interaction: it is the worst discounted by one for every 50 interactions,
    // so a single outlier in a long session does not define the page. The list is kept sorted by
    // addInteraction, so this is an index rather than a sort.
    function computeInp() {
        if (_interactionList.length === 0) return null;

        const index = Math.min(_interactionList.length - 1, Math.floor(_vitals.interactionCount / 50));
        return _interactionList[index].duration;
    }
}(BitButil));
