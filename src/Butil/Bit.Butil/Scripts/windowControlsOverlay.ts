var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: any } = {};

    function overlay() { return (window.navigator as any).windowControlsOverlay; }

    function geometry() {
        const wco = overlay();
        const rect = wco?.getTitlebarAreaRect?.();
        return {
            visible: wco?.visible === true,
            x: rect?.x ?? 0,
            y: rect?.y ?? 0,
            width: rect?.width ?? 0,
            height: rect?.height ?? 0
        };
    }

    butil.windowControlsOverlay = {
        isSupported() { return !!overlay(); },
        isVisible() { return overlay()?.visible === true; },
        getTitlebarAreaRect() { return geometry(); },
        onGeometryChange(dotNetRef: any, listenerId: string, method: string) {
            const wco = overlay();
            if (!wco?.addEventListener) return false;

            const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, geometry());
            wco.addEventListener('geometrychange', handler);
            _listeners[listenerId] = handler;

            // Emit the current geometry so a subscriber can lay out before the first change.
            butil.utils.dispatch(dotNetRef, method, listenerId, geometry());
            return true;
        },
        offGeometryChange(listenerId: string) {
            const handler = _listeners[listenerId];
            if (!handler) return;
            delete _listeners[listenerId];
            try { overlay()?.removeEventListener('geometrychange', handler); } catch { /* overlay gone */ }
        }
    };
}(BitButil));
