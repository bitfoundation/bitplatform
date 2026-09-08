var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _observers: { [id: string]: any } = {};

    butil.computePressure = {
        isSupported() { return typeof (window as any).PressureObserver === 'function'; },
        getKnownSources,
        observe,
        disconnect
    };

    function getKnownSources() {
        const Observer = (window as any).PressureObserver;
        // knownSources is a static on the constructor, and the only way to tell whether an engine
        // exposes anything beyond 'cpu' without attempting an observe().
        return typeof Observer === 'function' && Observer.knownSources ? Array.prototype.slice.call(Observer.knownSources) : [];
    }

    async function observe(subscriptionId: string, dotNetRef: any, source: string, sampleIntervalMs: number) {
        const Observer = (window as any).PressureObserver;
        if (typeof Observer !== 'function') return false;

        const observer = new Observer((records: any[]) => {
            butil.utils.dispatch(dotNetRef, 'InvokePressureRecords', subscriptionId, records.map((record: any) => ({
                source: record.source,
                state: record.state,
                time: record.time ?? 0
            })));
        });

        _observers[subscriptionId] = observer;
        try {
            // sampleInterval is a hint, in milliseconds; 0 lets the engine pick. The call rejects
            // with NotSupportedError for an unknown source and NotAllowedError behind a policy.
            await observer.observe(source, sampleIntervalMs > 0 ? { sampleInterval: sampleIntervalMs } : undefined);
            return true;
        } catch {
            disconnect(subscriptionId);
            return false;
        }
    }

    function disconnect(subscriptionId: string) {
        const observer = _observers[subscriptionId];
        if (!observer) return;
        delete _observers[subscriptionId];
        try { observer.disconnect(); } catch { /* already disconnected */ }
    }
}(BitButil));
