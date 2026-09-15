var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.userAgent = {
        // Two names for one probe on purpose: isSupported is the spelling every other Butil class
        // uses, isClientHintsSupported is the one that says which of the two mechanisms - UA-CH or
        // the legacy string - the answer is about. UserAgent.Extract() works without either - it is
        // served by the userAgentParser module, which this one deliberately does not depend on.
        isSupported() { return !!(window.navigator as any).userAgentData; },
        isClientHintsSupported() { return !!(window.navigator as any).userAgentData; },
        getBrands() {
            const data = (window.navigator as any).userAgentData;
            if (!data?.brands) return [];
            return data.brands.map((b: any) => ({ brand: b.brand, version: b.version }));
        },
        isMobile() { return !!(window.navigator as any).userAgentData?.mobile; },
        getPlatform() { return (window.navigator as any).userAgentData?.platform ?? ''; },
        async getHighEntropyValues(hints: string[]) {
            const data = (window.navigator as any).userAgentData;
            const empty = {
                architecture: null, bitness: null, brands: null, fullVersionList: null,
                mobile: null, model: null, platform: null, platformVersion: null,
                uaFullVersion: null, wow64: null
            };
            if (!data?.getHighEntropyValues) return empty;
            try {
                const v = await data.getHighEntropyValues(hints || []);
                return {
                    architecture: v.architecture ?? null,
                    bitness: v.bitness ?? null,
                    brands: v.brands?.map((b: any) => ({ brand: b.brand, version: b.version })) ?? null,
                    fullVersionList: v.fullVersionList?.map((b: any) => ({ brand: b.brand, version: b.version })) ?? null,
                    mobile: typeof v.mobile === 'boolean' ? v.mobile : null,
                    model: v.model ?? null,
                    platform: v.platform ?? null,
                    platformVersion: v.platformVersion ?? null,
                    uaFullVersion: v.uaFullVersion ?? null,
                    wow64: typeof v.wow64 === 'boolean' ? v.wow64 : null
                };
            } catch {
                return empty;
            }
        }
    };

}(BitButil));