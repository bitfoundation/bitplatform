var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: any } = {};

    function vk() { return (window.navigator as any).virtualKeyboard; }

    function geometry() {
        const rect = vk()?.boundingRect;
        return {
            x: rect?.x ?? 0,
            y: rect?.y ?? 0,
            width: rect?.width ?? 0,
            height: rect?.height ?? 0
        };
    }

    butil.virtualKeyboard = {
        isSupported() { return !!vk(); },
        show() {
            // Only works from a handler for a user gesture on a focused editable element.
            try { vk()?.show?.(); } catch { /* no gesture, or nothing focused */ }
        },
        hide() {
            try { vk()?.hide?.(); } catch { /* not showing */ }
        },
        getOverlaysContent() { return vk()?.overlaysContent === true; },
        setOverlaysContent(value: boolean) {
            const keyboard = vk();
            if (keyboard) keyboard.overlaysContent = value;
        },
        getBoundingRect() { return geometry(); },
        onGeometryChange(dotNetRef: any, listenerId: string, method: string) {
            const keyboard = vk();
            if (!keyboard?.addEventListener) return false;

            // Re-registering the same listener id would otherwise orphan the previous handler.
            butil.virtualKeyboard.offGeometryChange(listenerId);

            const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, geometry());
            keyboard.addEventListener('geometrychange', handler);
            _listeners[listenerId] = handler;

            // Emit the current geometry so a subscriber can lay out before the keyboard moves.
            butil.utils.dispatch(dotNetRef, method, listenerId, geometry());
            return true;
        },
        offGeometryChange(listenerId: string) {
            const handler = _listeners[listenerId];
            if (!handler) return;
            delete _listeners[listenerId];
            try { vk()?.removeEventListener('geometrychange', handler); } catch { /* keyboard gone */ }
        }
    };
}(BitButil));
