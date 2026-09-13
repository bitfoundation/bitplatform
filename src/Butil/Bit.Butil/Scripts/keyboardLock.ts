var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function keyboard() { return (window.navigator as any).keyboard; }

    butil.keyboardLock = {
        isSupported() { return typeof keyboard()?.lock === 'function'; },
        async lock(codes: string[]) {
            const kb = keyboard();
            if (typeof kb?.lock !== 'function') return false;
            try {
                // No codes at all means "capture every key the platform lets us have".
                await kb.lock(codes?.length ? codes : undefined);
                return true;
            } catch {
                // Not fullscreen, not a top-level browsing context, or the platform refused.
                return false;
            }
        },
        unlock() {
            try { keyboard()?.unlock?.(); } catch { /* nothing was locked */ }
        }
    };
}(BitButil));
