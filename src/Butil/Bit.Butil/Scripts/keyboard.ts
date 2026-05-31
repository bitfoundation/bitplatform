var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: { target: EventTarget, handler: any } } = {};

    butil.keyboard = {
        add,
        addOn,
        remove
    };

    function makeHandler(methodName: string, listenerId: string, code: string, alt: boolean, ctrl: boolean,
        meta: boolean, shift: boolean, preventDefault: boolean, stopPropagation: boolean, repeat: boolean) {
        return (e: KeyboardEvent) => {
            if (e.code !== code) return;

            if ((!alt && e.altKey) || (alt && !e.altKey)) return;
            if ((!ctrl && e.ctrlKey) || (ctrl && !e.ctrlKey)) return;
            if ((!meta && e.metaKey) || (meta && !e.metaKey)) return;
            if ((!shift && e.shiftKey) || (shift && !e.shiftKey)) return;

            if (!repeat && e.repeat) return;

            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();

            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId);
        };
    }

    function add(methodName: string, listenerId: string, code: string, alt: boolean, ctrl: boolean,
        meta: boolean, shift: boolean, preventDefault: boolean, stopPropagation: boolean, repeat: boolean) {
        const handler = makeHandler(methodName, listenerId, code, alt, ctrl, meta, shift, preventDefault, stopPropagation, repeat);
        _handlers[listenerId] = { target: document, handler };
        document.addEventListener('keydown', handler);
    }

    function addOn(methodName: string, listenerId: string, element: HTMLElement, code: string,
        alt: boolean, ctrl: boolean, meta: boolean, shift: boolean,
        preventDefault: boolean, stopPropagation: boolean, repeat: boolean) {
        if (!element) {
            // Fall back to document so callers don't lose the listener silently when the
            // element ref isn't ready yet.
            return add(methodName, listenerId, code, alt, ctrl, meta, shift, preventDefault, stopPropagation, repeat);
        }
        const handler = makeHandler(methodName, listenerId, code, alt, ctrl, meta, shift, preventDefault, stopPropagation, repeat);
        _handlers[listenerId] = { target: element, handler };
        element.addEventListener('keydown', handler);
    }

    function remove(ids: string[]) {
        ids.forEach(id => {
            const entry = _handlers[id];
            if (!entry) return;
            delete _handlers[id];
            try { entry.target.removeEventListener('keydown', entry.handler); } catch { /* detached */ }
        });
    }
}(BitButil));