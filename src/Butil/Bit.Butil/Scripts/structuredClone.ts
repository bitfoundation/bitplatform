var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.structuredClone = {
        isSupported() { return typeof (window as any).structuredClone === 'function'; },
        clone(value: any) {
            const fn = (window as any).structuredClone;
            if (typeof fn !== 'function') return null;
            try { return fn(value); } catch { return null; }
        },
        // Whether a value survives the structured clone algorithm at all - the same test that decides
        // whether postMessage, IndexedDB and history.pushState will accept it. The value arrives here
        // already marshalled, so this answers the question for the marshalled shape, which is the
        // shape those APIs would actually be handed from a Butil call.
        canClone(value: any) {
            const fn = (window as any).structuredClone;
            if (typeof fn !== 'function') return false;
            try { fn(value); return true; } catch { return false; }
        }
    };
}(BitButil));
