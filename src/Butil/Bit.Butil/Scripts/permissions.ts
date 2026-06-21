var BitButil = BitButil || {};

(function (butil: any) {
    butil.permissions = {
        isSupported() { return !!(window.navigator as any).permissions?.query; },
        async query(name: string) {
            const perms: any = (window.navigator as any).permissions;
            if (!perms || typeof perms.query !== 'function') return 'unknown';
            try {
                const status = await perms.query({ name });
                return status?.state ?? 'unknown';
            } catch {
                // The browser doesn't recognize this descriptor name.
                return 'unknown';
            }
        }
    };
}(BitButil));
