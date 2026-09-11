var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const CSSNS = () => (window as any).CSS;

    // The Houdini worklets. Their own module: loading one is an opt-in that brings an extra script
    // file with it, and most pages never do.
    butil.cssWorklet = {
        supportsPaintWorklet() { return !!CSSNS()?.paintWorklet; },
        supportsLayoutWorklet() { return !!CSSNS()?.layoutWorklet; },

        // Houdini's paint worklet: a script that draws a custom paint() image the way a canvas does,
        // but as a live CSS value. It runs in its own global scope with no DOM, which is what makes
        // it fast and what makes it unable to reach anything in the page.
        async addPaintWorklet(url: string) {
            const worklet = CSSNS()?.paintWorklet;
            if (!worklet?.addModule) return false;
            try {
                await worklet.addModule(url);
                return true;
            } catch {
                // The module 404'd, or threw while registering its paint class.
                return false;
            }
        },

        async addLayoutWorklet(url: string) {
            const worklet = CSSNS()?.layoutWorklet;
            if (!worklet?.addModule) return false;
            try {
                await worklet.addModule(url);
                return true;
            } catch {
                return false;
            }
        },
    };
}(BitButil));
