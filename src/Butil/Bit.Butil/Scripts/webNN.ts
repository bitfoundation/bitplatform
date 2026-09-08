var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // A created context, kept so its limits can be inspected without creating another one - context
    // creation can spin up a real accelerator backend and is not cheap.
    const _contexts: { [id: string]: any } = {};

    function ml() { return (window.navigator as any).ml; }

    butil.webNN = {
        isSupported() { return typeof ml()?.createContext === 'function'; },
        async createContext(id: string, deviceType: string, powerPreference: string) {
            const neural = ml();
            if (typeof neural?.createContext !== 'function') return null;

            const options: any = {};
            if (deviceType) options.deviceType = deviceType;
            if (powerPreference) options.powerPreference = powerPreference;

            try {
                const context = await neural.createContext(options);
                butil.webNN.destroy(id);
                _contexts[id] = context;
                return {
                    deviceType: context?.deviceType ?? deviceType ?? '',
                    powerPreference: context?.powerPreference ?? powerPreference ?? '',
                    // A graph builder is what the rest of WebNN is reached through; reporting whether
                    // one can be constructed is the honest measure of "is this usable".
                    canBuildGraph: typeof (window as any).MLGraphBuilder === 'function'
                };
            } catch {
                // No backend for the requested device, or the API is behind a flag.
                return null;
            }
        },
        async opSupportLimits(id: string) {
            const context = _contexts[id];
            if (typeof context?.opSupportLimits !== 'function') return [];
            try {
                const limits = await context.opSupportLimits();
                if (!limits) return [];
                // The shape is one entry per operator plus a few top-level ones; flattened to
                // name/detail pairs, because the detail differs per operator and per backend.
                return Object.keys(limits).map(name => ({
                    name,
                    detail: JSON.stringify(limits[name])
                }));
            } catch {
                return [];
            }
        },
        destroy(id: string) {
            const context = _contexts[id];
            if (!context) return;
            delete _contexts[id];
            try { context.destroy?.(); } catch { /* already gone, or no destroy in this build */ }
        },
        disposeAll() {
            for (const id of Object.keys(_contexts)) butil.webNN.destroy(id);
        }
    };
}(BitButil));
