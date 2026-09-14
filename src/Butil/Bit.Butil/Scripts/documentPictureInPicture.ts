var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface MovedElement { element: any; parent: any; nextSibling: any; }

    interface WindowEntry {
        win: any;
        moved: MovedElement[];
        onClose: (() => void) | null;
    }

    const _windows: { [id: string]: WindowEntry } = {};
    const _enterListeners: { [listenerId: string]: (e: any) => void } = {};

    butil.documentPictureInPicture = {
        isSupported() { return !!(window as any).documentPictureInPicture?.requestWindow; },
        requestWindow,
        moveElement,
        restoreElements,
        addStyleSheet,
        size,
        isOpen,
        focus,
        close,
        subscribeEnter,
        unsubscribeEnter,
        disposeAll
    };

    async function requestWindow(id: string, options: any, dotNetRef: any, closeMethod: string) {
        const dpip = (window as any).documentPictureInPicture;
        if (!dpip?.requestWindow) return null;

        close(id);
        try {
            const win = await dpip.requestWindow(butil.utils.pick(options,
                ['width', 'height', 'disallowReturnToOpener', 'preferInitialWindowPlacement']));

            const entry: WindowEntry = { win, moved: [], onClose: null };
            _windows[id] = entry;

            if (options?.copyStyleSheets) copyStyleSheets(win);

            // The user can close the floating window at any time, and the elements that were moved
            // into it would go with it - they are children of a document that is being destroyed.
            // Putting them back first is what keeps the page whole (and, under Blazor, keeps the
            // component's nodes reachable by the renderer).
            entry.onClose = () => {
                restoreElements(id);
                delete _windows[id];
                butil.utils.dispatch(dotNetRef, closeMethod, id);
            };
            win.addEventListener('pagehide', entry.onClose, { once: true });

            return { width: win.innerWidth ?? 0, height: win.innerHeight ?? 0 };
        } catch {
            // No user gesture behind the call, a window already open, or the embedder blocks it.
            return null;
        }
    }

    // A picture-in-picture window is a separate document and inherits none of the page's CSS, so an
    // element moved into it renders unstyled unless the styles come along.
    function copyStyleSheets(win: any) {
        for (const sheet of Array.from(document.styleSheets) as any[]) {
            try {
                const css = Array.from(sheet.cssRules).map((rule: any) => rule.cssText).join('');
                const style = win.document.createElement('style');
                style.textContent = css;
                win.document.head.appendChild(style);
            } catch {
                // A cross-origin sheet cannot be read, so it is re-linked by URL instead.
                if (!sheet.href) continue;
                const link = win.document.createElement('link');
                link.rel = 'stylesheet';
                link.type = sheet.type ?? 'text/css';
                if (sheet.media?.mediaText) link.media = sheet.media.mediaText;
                link.href = sheet.href;
                win.document.head.appendChild(link);
            }
        }
    }

    function moveElement(id: string, element: any) {
        const entry = _windows[id];
        if (!entry || !element) return false;
        try {
            // Where the element came from is recorded now, while it still has a parent - after the
            // move there is no way to work it out.
            entry.moved.push({ element, parent: element.parentNode, nextSibling: element.nextSibling });
            entry.win.document.body.append(element);
            return true;
        } catch {
            return false;
        }
    }

    function restoreElements(id: string) {
        const entry = _windows[id];
        if (entry) restore(entry);
    }

    function restore(entry: WindowEntry) {
        // Last moved is restored first, so siblings moved out one after another land back in order.
        for (const moved of entry.moved.reverse()) {
            try {
                if (moved.parent) moved.parent.insertBefore(moved.element, moved.nextSibling);
                else moved.element.remove();
            } catch { /* the original parent is gone from the document */ }
        }
        entry.moved = [];
    }

    function addStyleSheet(id: string, css: string) {
        const entry = _windows[id];
        if (!entry) return false;
        try {
            const style = entry.win.document.createElement('style');
            style.textContent = css;
            entry.win.document.head.appendChild(style);
            return true;
        } catch {
            return false;
        }
    }

    function size(id: string) {
        const entry = _windows[id];
        if (!entry) return null;
        return { width: entry.win.innerWidth ?? 0, height: entry.win.innerHeight ?? 0 };
    }

    function isOpen(id: string) {
        const entry = _windows[id];
        return !!entry && !entry.win.closed;
    }

    function focus(id: string) {
        const entry = _windows[id];
        if (!entry) return false;
        try { entry.win.focus(); return true; } catch { return false; }
    }

    function close(id: string) {
        const entry = _windows[id];
        if (!entry) return;
        delete _windows[id];
        if (entry.onClose) {
            try { entry.win.removeEventListener('pagehide', entry.onClose); } catch { /* window already gone */ }
        }
        // Restore before closing: once the window is closed its document - and everything still
        // inside it - is gone.
        restore(entry);
        try { entry.win.close(); } catch { /* already closed */ }
    }

    // Fires whenever a picture-in-picture window opens, including one this page did not request.
    function subscribeEnter(listenerId: string, dotNetRef: any, method: string) {
        const dpip = (window as any).documentPictureInPicture;
        if (!dpip?.addEventListener) return false;
        const handler = (e: any) => butil.utils.dispatch(dotNetRef, method, listenerId,
            e?.window?.innerWidth ?? 0, e?.window?.innerHeight ?? 0);
        _enterListeners[listenerId] = handler;
        dpip.addEventListener('enter', handler);
        return true;
    }

    function unsubscribeEnter(listenerId: string) {
        const handler = _enterListeners[listenerId];
        if (!handler) return;
        delete _enterListeners[listenerId];
        try { (window as any).documentPictureInPicture.removeEventListener('enter', handler); } catch { /* already gone */ }
    }

    function disposeAll() {
        for (const id of Object.keys(_windows)) close(id);
        for (const listenerId of Object.keys(_enterListeners)) unsubscribeEnter(listenerId);
    }
}(BitButil));
