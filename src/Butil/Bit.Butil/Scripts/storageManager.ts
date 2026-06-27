var BitButil = BitButil || {};

(function (butil: any) {
    butil.storageManager = {
        isSupported() { return !!(window.navigator as any).storage; },
        async estimate() {
            const sm: any = (window.navigator as any).storage;
            if (!sm?.estimate) return { quota: null, usage: null };
            try {
                const e = await sm.estimate();
                return {
                    quota: typeof e.quota === 'number' ? e.quota : null,
                    usage: typeof e.usage === 'number' ? e.usage : null
                };
            } catch {
                return { quota: null, usage: null };
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
