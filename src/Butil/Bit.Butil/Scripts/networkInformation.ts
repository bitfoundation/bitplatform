var BitButil = BitButil || {};

(function (butil: any) {
    butil.networkInformation = {
        getStatus() {
            const nav = window.navigator as any;
            const c = nav.connection || nav.mozConnection || nav.webkitConnection || null;
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
