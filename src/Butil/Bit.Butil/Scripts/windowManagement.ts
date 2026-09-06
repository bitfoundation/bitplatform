var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // getScreenDetails() prompts on its first call and the resolved object is live - it keeps
    // updating as monitors come and go - so it is cached rather than re-requested per read.
    let _details: any = null;
    const _handlers: { [id: string]: { screens: EventListener, current: EventListener } } = {};

    // No isExtended or queryPermission here: screen.ts already reads window.screen.isExtended and
    // permissions.ts already runs navigator.permissions.query, so the .NET side of this service
    // calls those modules rather than carrying a second copy of either.
    butil.windowManagement = {
        isSupported() { return typeof (window as any).getScreenDetails === 'function'; },
        getScreenDetails,
        openOnScreen,
        requestFullscreenOnScreen,
        addChange,
        removeChange
    };

    function describe(screen: any, isCurrent: boolean) {
        return {
            label: screen.label ?? '',
            left: screen.left ?? 0,
            top: screen.top ?? 0,
            width: screen.width ?? 0,
            height: screen.height ?? 0,
            availLeft: screen.availLeft ?? 0,
            availTop: screen.availTop ?? 0,
            availWidth: screen.availWidth ?? 0,
            availHeight: screen.availHeight ?? 0,
            colorDepth: screen.colorDepth ?? 0,
            pixelDepth: screen.pixelDepth ?? 0,
            devicePixelRatio: screen.devicePixelRatio ?? 1,
            isPrimary: !!screen.isPrimary,
            isInternal: !!screen.isInternal,
            orientationType: screen.orientation?.type ?? null,
            orientationAngle: screen.orientation?.angle ?? 0,
            isCurrent
        };
    }

    function snapshot() {
        if (!_details) return null;
        const screens = _details.screens ?? [];
        const current = _details.currentScreen;
        return {
            isExtended: !!(window.screen as any).isExtended,
            // -1 is passed through rather than clamped to 0: a currentScreen that isn't in the list
            // means "unknown", and reporting index 0 would name a screen no entry marks as current.
            currentScreenIndex: Array.prototype.indexOf.call(screens, current),
            screens: Array.prototype.map.call(screens, (screen: any) => describe(screen, screen === current))
        };
    }

    async function getScreenDetails() {
        if (typeof (window as any).getScreenDetails !== 'function') return null;
        if (!_details) {
            try { _details = await (window as any).getScreenDetails(); }
            catch { return null; } // the user dismissed the window-management prompt
        }
        return snapshot();
    }

    // Places a new window on the chosen screen. The Window Management API adds no opener of its
    // own: what it adds is the screen geometry, which is what makes left/top meaningful across
    // monitors instead of relative to the current one.
    async function openOnScreen(url: string, screenIndex: number, features: string | null, fullSize: boolean) {
        const details = await getScreenDetails();
        if (!details) return false;
        const screen = _details.screens[screenIndex];
        if (!screen) return false;

        const parts = [
            `left=${screen.availLeft}`,
            `top=${screen.availTop}`,
            `width=${fullSize ? screen.availWidth : Math.min(800, screen.availWidth)}`,
            `height=${fullSize ? screen.availHeight : Math.min(600, screen.availHeight)}`
        ];
        if (features) parts.push(features);

        const opened = window.open(url, '_blank', parts.join(','));
        return !!opened;
    }

    async function requestFullscreenOnScreen(element: any, screenIndex: number) {
        const details = await getScreenDetails();
        if (!details) return false;
        const screen = _details.screens[screenIndex];
        // An unset ElementReference (default(ElementReference)) does not arrive as null: Blazor
        // marshals it as a `{ __internalId: null }` object that no ?? can see through, so the test
        // is whether an actual element came across.
        const target: any = element instanceof Element ? element : document.documentElement;
        if (!screen || typeof target.requestFullscreen !== 'function') return false;
        try { await target.requestFullscreen({ screen }); return true; } catch { return false; }
    }

    // 'screenschange' fires when a monitor is attached or removed, 'currentscreenchange' when this
    // window is dragged onto another one. Both hand back the whole snapshot, because either can
    // change every index the caller is holding.
    async function addChange(dotNetRef: any, listenerId: string) {
        const details = await getScreenDetails();
        if (!details) return false;

        // Both events carry the same payload, so one handler serves both - it is stored twice only
        // because removeEventListener needs the same reference it was added with.
        const relay: EventListener = () => butil.utils.dispatch(dotNetRef, 'InvokeScreensChange', listenerId, snapshot());
        _details.addEventListener('screenschange', relay);
        _details.addEventListener('currentscreenchange', relay);
        _handlers[listenerId] = { screens: relay, current: relay };
        return true;
    }

    function removeChange(ids: string[]) {
        ids.forEach(id => {
            const entry = _handlers[id];
            delete _handlers[id];
            if (!entry || !_details) return;
            _details.removeEventListener('screenschange', entry.screens);
            _details.removeEventListener('currentscreenchange', entry.current);
        });
    }
}(BitButil));
