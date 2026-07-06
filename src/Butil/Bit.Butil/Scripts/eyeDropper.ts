var BitButil = BitButil || {};

(function (butil: any) {
    butil.eyeDropper = {
        isSupported() { return 'EyeDropper' in window; },
        async open() {
            const W = window as any;
            if (typeof W.EyeDropper !== 'function') return null;
            try {
                const dropper = new W.EyeDropper();
                const result = await dropper.open();
                return result?.sRGBHex ?? null;
            } catch {
                // User canceled or the call wasn't tied to a user gesture.
                return null;
            }
        }
    };
}(BitButil));
