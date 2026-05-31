var BitButil = BitButil || {};

(function (butil: any) {
    let _refs = {};
    const _mediaQueryHandlers: { [id: string]: { mql: MediaQueryList, handler: (e: MediaQueryListEvent) => void } } = {};

    butil.window = {
        addBeforeUnload,
        removeBeforeUnload,
        innerHeight() { return window.innerHeight },
        innerWidth() { return window.innerWidth },
        isSecureContext() { return window.isSecureContext },
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
        close,
        confirm(message?: string) { return window.confirm(message) },
        find,
        focus() { window.focus() },
        getSelection,
        getSelectionText() { return window.getSelection()?.toString() ?? '' },
        clearSelection() { window.getSelection()?.removeAllRanges(); },
        selectElement(element: HTMLElement) {
            if (!element) return;
            // Inputs/textareas have their own select(), and trying to wrap them in a Range fails.
            if (typeof (element as any).select === 'function' && (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement)) {
                (element as HTMLInputElement).select();
                return;
            }
            const sel = window.getSelection();
            if (!sel) return;
            sel.removeAllRanges();
            const range = document.createRange();
            try { range.selectNodeContents(element); sel.addRange(range); }
            catch { /* element may not be in the DOM */ }
        },
        async copySelection() {
            const text = window.getSelection()?.toString() ?? '';
            if (!text) return false;
            try { await navigator.clipboard.writeText(text); return true; }
            catch { return false; }
        },
        matchMedia,
        subscribeMatchMedia,
        unsubscribeMatchMedia,
        open,
        print() { window.print() },
        prompt(message?: string, defaultValue?: string) { return window.prompt(message, defaultValue) },
        scroll,
        scrollBy,
        stop() { window.stop() },
        dispose
    };

    function addBeforeUnload(message?: string) {
        window.onbeforeunload = e => {
            e.preventDefault();
            // Modern browsers ignore the returnValue/message text and show their own copy,
            // but legacy and some embedded webviews still honor it.
            const msg = typeof message === 'string' && message.length > 0 ? message : true;
            (e as any).returnValue = msg;
            return msg;
        };
    }

    function removeBeforeUnload() {
        window.onbeforeunload = null;
    }

    function close(id: string | undefined) {
        if (!id) return window.close();

        const ref = _refs[id];
        if (!ref) return;
        delete _refs[id];
        return ref.close();
    }

    function find(text?: string,
        caseSensitive?: boolean,
        backward?: boolean,
        wrapAround?: boolean,
        wholeWord?: boolean,
        searchInFrame?: boolean) {
        return (window as any).find(text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);
    }

    function getSelection() {
        const sel = window.getSelection();
        if (!sel) return null;
        return {
            text: sel.toString(),
            isCollapsed: sel.isCollapsed,
            rangeCount: sel.rangeCount,
            type: (sel as any).type ?? null,
            anchorOffset: sel.anchorOffset,
            focusOffset: sel.focusOffset
        };
    }

    function matchMedia(query: string) {
        const media = window.matchMedia(query);
        return {
            matches: media.matches,
            media: media.media
        };
    }

    function subscribeMatchMedia(methodName: string, listenerId: string, query: string) {
        const mql = window.matchMedia(query);
        const handler = (e: MediaQueryListEvent) => {
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId, { matches: e.matches, media: e.media });
        };

        // addEventListener is supported on MediaQueryList in all evergreen browsers; older
        // Safari only exposes the legacy addListener variant.
        if (typeof mql.addEventListener === 'function') {
            mql.addEventListener('change', handler);
        } else {
            (mql as any).addListener(handler);
        }
        _mediaQueryHandlers[listenerId] = { mql, handler };
    }

    function unsubscribeMatchMedia(ids: string[]) {
        ids.forEach(id => {
            const entry = _mediaQueryHandlers[id];
            if (!entry) return;
            delete _mediaQueryHandlers[id];
            if (typeof entry.mql.removeEventListener === 'function') {
                entry.mql.removeEventListener('change', entry.handler);
            } else {
                (entry.mql as any).removeListener(entry.handler);
            }
        });
    }

    function open(id: string, url?: string, target?: string, windowFeatures?: string) {
        const ref = window.open(url, target, windowFeatures);
        if (!ref) return;
        _refs[id] = ref;
        return id;
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

    function dispose() {
        _refs = {};
        for (const id of Object.keys(_mediaQueryHandlers)) {
            const entry = _mediaQueryHandlers[id];
            if (typeof entry.mql.removeEventListener === 'function') {
                entry.mql.removeEventListener('change', entry.handler);
            } else {
                (entry.mql as any).removeListener(entry.handler);
            }
            delete _mediaQueryHandlers[id];
        }
    }
}(BitButil));