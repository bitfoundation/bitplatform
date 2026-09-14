var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function keyboard() { return (window.navigator as any).keyboard; }

    butil.keyboardLayout = {
        isSupported() { return typeof keyboard()?.getLayoutMap === 'function'; },
        async getLayoutMap() {
            const kb = keyboard();
            if (typeof kb?.getLayoutMap !== 'function') return [];
            try {
                const map = await kb.getLayoutMap();
                // A KeyboardLayoutMap is a read-only Map of code -> the character that key produces
                // on this layout. Flattened to an array because a Map does not cross interop.
                return Array.from(map, ([code, key]: any) => ({ code, key }));
            } catch {
                return [];
            }
        },
        async get(code: string) {
            const kb = keyboard();
            if (typeof kb?.getLayoutMap !== 'function') return null;
            try {
                const map = await kb.getLayoutMap();
                return map.get(code) ?? null;
            } catch {
                return null;
            }
        }
    };
}(BitButil));
