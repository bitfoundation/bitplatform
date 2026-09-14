var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The captured `beforeinstallprompt` event, kept so `prompt()` can be called later from a user
    // gesture. The event fires once, very early - often before Blazor has started - so the listener
    // is attached while this module is being evaluated rather than when .NET first asks for it.
    //
    // In lazy-scripts mode the module is only imported on the first call into it, which can be after
    // the event has already fired. For that case an app can stash the event from a tiny inline
    // snippet in index.html onto `window.BitButilDeferredInstallPrompt`, and we adopt it here.
    let _deferred: any = (window as any).BitButilDeferredInstallPrompt ?? null;
    let _installed = false;
    const _installedListeners: { [id: string]: any } = {};
    const _availableListeners: { [id: string]: any } = {};

    window.addEventListener('beforeinstallprompt', (e: any) => {
        // Suppressing the browser's own mini-infobar is what makes a custom install button possible;
        // without it Chromium shows its banner and never lets us re-show the prompt.
        e.preventDefault();
        _deferred = e;
        (window as any).BitButilDeferredInstallPrompt = e;
        for (const id of Object.keys(_availableListeners)) {
            const entry = _availableListeners[id];
            butil.utils.dispatch(entry.dotNetRef, entry.method, id, platforms(e));
        }
    });

    window.addEventListener('appinstalled', () => {
        _installed = true;
        // The deferred event is spent once the app is installed - holding it would let a caller
        // prompt for an install that already happened.
        _deferred = null;
        (window as any).BitButilDeferredInstallPrompt = null;
        for (const id of Object.keys(_installedListeners)) {
            const entry = _installedListeners[id];
            butil.utils.dispatch(entry.dotNetRef, entry.method, id);
        }
    });

    function platforms(e: any): string[] {
        return Array.isArray(e?.platforms) ? e.platforms : [];
    }

    butil.installPrompt = {
        isSupported() { return 'onbeforeinstallprompt' in window; },
        isAvailable() { return !!_deferred; },
        getPlatforms() { return platforms(_deferred); },
        wasInstalled() { return _installed; },
        isStandalone() {
            // Two different signals for the same question: the display-mode media query is the
            // standard one, `navigator.standalone` is what iOS Safari implements instead.
            const standalone = (window.navigator as any).standalone;
            return standalone === true
                || window.matchMedia?.('(display-mode: standalone)').matches === true
                || window.matchMedia?.('(display-mode: minimal-ui)').matches === true
                || window.matchMedia?.('(display-mode: fullscreen)').matches === true
                || window.matchMedia?.('(display-mode: window-controls-overlay)').matches === true;
        },
        async prompt() {
            const e = _deferred;
            if (!e?.prompt) return { outcome: 'unavailable', platform: '' };

            try {
                await e.prompt();
                const choice = await e.userChoice;
                // The spec allows a beforeinstallprompt event to be prompted once, whatever the outcome.
                _deferred = null;
                (window as any).BitButilDeferredInstallPrompt = null;
                return { outcome: choice?.outcome ?? 'dismissed', platform: choice?.platform ?? '' };
            } catch {
                // Not called from a user gesture, or already consumed.
                return { outcome: 'unavailable', platform: '' };
            }
        },
        onAvailable(dotNetRef: any, listenerId: string, method: string) {
            _availableListeners[listenerId] = { dotNetRef, method };
            // Late subscriber: report the event we already hold so a handler attached after the
            // browser fired it still sees that an install is offerable.
            if (_deferred) butil.utils.dispatch(dotNetRef, method, listenerId, platforms(_deferred));
        },
        offAvailable(listenerId: string) { delete _availableListeners[listenerId]; },
        onInstalled(dotNetRef: any, listenerId: string, method: string) {
            _installedListeners[listenerId] = { dotNetRef, method };
        },
        offInstalled(listenerId: string) { delete _installedListeners[listenerId]; },
        disposeAll() {
            for (const id of Object.keys(_availableListeners)) delete _availableListeners[id];
            for (const id of Object.keys(_installedListeners)) delete _installedListeners[id];
        }
    };
}(BitButil));
