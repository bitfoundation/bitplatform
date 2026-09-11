var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // beforeunload handlers keyed by a per-registration id. We use addEventListener (not the
    // single window.onbeforeunload slot) so multiple subscribers - and the host app's own
    // handler - coexist instead of clobbering one another, and each can be removed individually.
    const _beforeUnloadHandlers: { [id: string]: (e: BeforeUnloadEvent) => any } = {};

    // The window itself: its geometry, its dialogs and its unload guard. Popups are windowRefs,
    // the selection windowSelection and the media queries windowMediaQuery - each of those keeps a
    // registry of its own, and none of them is wanted by an app that just reads innerWidth.
    butil.window = {
        addBeforeUnload,
        removeBeforeUnload,
        innerHeight() { return window.innerHeight },
        innerWidth() { return window.innerWidth },
        isSecureContext() { return window.isSecureContext },
        devicePixelRatio() { return window.devicePixelRatio },
        crossOriginIsolated() { return !!(window as any).crossOriginIsolated },
        frameCount() { return window.length },
        // Comparing against window.top is the standard iframe test; a cross-origin parent still
        // permits the identity check even though its properties are blocked. The catch covers the
        // engines that throw on the access itself.
        isInIframe() { try { return window.self !== window.top } catch { return true } },
        moveTo(x: number, y: number) { window.moveTo(x, y) },
        moveBy(x: number, y: number) { window.moveBy(x, y) },
        resizeTo(width: number, height: number) { window.resizeTo(width, height) },
        resizeBy(width: number, height: number) { window.resizeBy(width, height) },
        locationbar() { return window.locationbar },
        getName() { return window.name },
        setName(value: string) { window.name = value },
        origin() { return window.origin },
        outerHeight() { return window.outerHeight },
        outerWidth() { return window.outerWidth },
        screenX() { return window.screenX },
        screenY() { return window.screenY },
        scrollX() { return window.scrollX },
        scrollY() { return window.scrollY },
        atob(data: string) { return window.atob(data) },
        alert(message?: string) { window.alert(message) },
        blur() { window.blur() },
        btoa(data: string) { return window.btoa(data) },
        confirm(message?: string) { return window.confirm(message) },
        find,
        focus() { window.focus() },
        print() { window.print() },
        prompt(message?: string, defaultValue?: string) { return window.prompt(message, defaultValue) },
        scroll,
        scrollBy,
        stop() { window.stop() }
    };

    function addBeforeUnload(id: string, message?: string) {
        // Replace any prior handler registered under the same id so repeated calls stay idempotent.
        removeBeforeUnload([id]);
        const handler = (e: BeforeUnloadEvent) => {
            e.preventDefault();
            // Modern browsers ignore the returnValue/message text and show their own copy,
            // but legacy and some embedded webviews still honor it.
            const msg = typeof message === 'string' && message.length > 0 ? message : true;
            (e as any).returnValue = msg;
            return msg;
        };
        _beforeUnloadHandlers[id] = handler;
        window.addEventListener('beforeunload', handler);
    }

    function removeBeforeUnload(ids?: string[]) {
        // No ids => remove every handler this module registered (legacy "remove all" behavior).
        const targets = ids ?? Object.keys(_beforeUnloadHandlers);
        targets.forEach(id => {
            const handler = _beforeUnloadHandlers[id];
            if (!handler) return;
            delete _beforeUnloadHandlers[id];
            window.removeEventListener('beforeunload', handler);
        });
    }

    function find(text?: string,
        caseSensitive?: boolean,
        backward?: boolean,
        wrapAround?: boolean,
        wholeWord?: boolean,
        searchInFrame?: boolean) {
        return (window as any).find(text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);
    }

    function scroll(options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            window.scroll(options);
        } else {
            window.scroll(x, y);
        }
    }

    function scrollBy(options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            window.scrollBy(options);
        } else {
            window.scrollBy(x, y);
        }
    }
}(BitButil));
