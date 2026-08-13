var BitButil = BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: { connection: any, handler: () => void } } = {};

    function connection() {
        const nav = window.navigator as any;
        return nav.connection || nav.mozConnection || nav.webkitConnection || null;
    }

    function snapshot() {
        const nav = window.navigator as any;
        const c = connection();
        return {
            online: !!nav.onLine,
            effectiveType: c?.effectiveType ?? null,
            type: c?.type ?? null,
            downlink: c?.downlink ?? null,
            downlinkMax: c?.downlinkMax ?? null,
            rtt: c?.rtt ?? null,
            saveData: typeof c?.saveData === 'boolean' ? c.saveData : null
        };
    }

    butil.networkInformation = {
        // navigator.onLine is everywhere; navigator.connection - the part that carries the
        // effective type, downlink and save-data - is Chromium only. GetStatus works either way
        // and reports nulls for the missing half, so this says which half you are getting.
        isSupported() { return !!connection(); },
        getStatus: snapshot,
        subscribe(dotNetRef: any, listenerId: string) {
            const handler = () => butil.utils.dispatch(dotNetRef, 'InvokeNetworkChange', listenerId, snapshot());
            const c = connection();

            // window's online/offline fire everywhere, so the subscription is useful even without
            // navigator.connection - it just won't report quality changes on those browsers.
            window.addEventListener('online', handler);
            window.addEventListener('offline', handler);
            c?.addEventListener('change', handler);

            _listeners[listenerId] = { connection: c, handler };
        },
        unsubscribe(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            window.removeEventListener('online', entry.handler);
            window.removeEventListener('offline', entry.handler);
            entry.connection?.removeEventListener('change', entry.handler);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.networkInformation.unsubscribe(id);
        }
    };
}(BitButil));
