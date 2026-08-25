var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.storageManager = {
        isSupported() { return !!(window.navigator as any).storage; },
        async estimate() {
            const sm: any = (window.navigator as any).storage;
            if (!sm?.estimate) return { quota: null, usage: null, usageDetails: [] };
            try {
                const e = await sm.estimate();
                return {
                    quota: typeof e.quota === 'number' ? e.quota : null,
                    usage: typeof e.usage === 'number' ? e.usage : null,
                    // Chromium-only per-API breakdown ("indexedDB", "caches", "serviceWorkerRegistrations",
                    // ...). Flattened to a list so .NET sees one shape rather than an open-ended object.
                    usageDetails: Object.entries(e.usageDetails || {})
                        .map(([api, bytes]) => ({ api, bytes: typeof bytes === 'number' ? bytes : 0 }))
                };
            } catch {
                return { quota: null, usage: null, usageDetails: [] };
            }
        },
        async persisted() {
            const sm: any = (window.navigator as any).storage;
            if (!sm?.persisted) return false;
            try { return await sm.persisted(); } catch { return false; }
        },
        async persist() {
            const sm: any = (window.navigator as any).storage;
            if (!sm?.persist) return false;
            try { return await sm.persist(); } catch { return false; }
        }
    };
}(BitButil));
