var BitButil = BitButil || {};

(function (butil: any) {
    function connection() {
        const nav = window.navigator as any;
        return nav.connection || nav.mozConnection || nav.webkitConnection || null;
    }

    butil.networkInformation = {
        // navigator.onLine is everywhere; navigator.connection - the part that carries the
        // effective type, downlink and save-data - is Chromium only. GetStatus works either way
        // and reports nulls for the missing half, so this says which half you are getting.
        isSupported() { return !!connection(); },
        getStatus() {
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
    };
}(BitButil));
