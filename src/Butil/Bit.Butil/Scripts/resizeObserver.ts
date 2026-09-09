var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _observers: { [id: string]: ResizeObserver } = {};

    butil.resizeObserver = {
        observe,
        unobserve
    };

    function pickBox(entry: ResizeObserverEntry, prop: 'borderBoxSize' | 'contentBoxSize' | 'devicePixelContentBoxSize') {
        const box = (entry as any)[prop];
        if (!box) return { inlineSize: 0, blockSize: 0 };
        // Older Safari delivered a single object instead of an array.
        const first = Array.isArray(box) ? box[0] : box;
        return { inlineSize: first?.inlineSize ?? 0, blockSize: first?.blockSize ?? 0 };
    }

    function observe(dotNetRef: any, listenerId: string, element: HTMLElement, box: string, minInterval: number) {
        if (!element || !('ResizeObserver' in window)) return;

        // The most flood-prone observer of the three: a window drag or a flex reflow delivers an
        // entry every frame for as long as it lasts. Trailing is not optional here - the size the
        // element *settles* at is the one the layout depends on, and a leading-only gate would
        // leave .NET holding a mid-drag size until something else happened to resize it.
        const send = (payload: any) => butil.utils.dispatch(dotNetRef, 'InvokeResize', listenerId, payload);
        const gated = butil.utils.throttle(minInterval, send, true);

        const observer = new ResizeObserver(entries => {
            const payload = entries.map(e => {
                const r = e.contentRect;
                const content = pickBox(e, 'contentBoxSize');
                const device = pickBox(e, 'devicePixelContentBoxSize');
                return {
                    contentRect: r ? { x: r.x, y: r.y, width: r.width, height: r.height } : null,
                    inlineSize: content.inlineSize,
                    blockSize: content.blockSize,
                    devicePixelInlineSize: device.inlineSize,
                    devicePixelBlockSize: device.blockSize,
                };
            });
            gated(payload);
        });

        try {
            observer.observe(element, { box: box as ResizeObserverBoxOptions });
        } catch {
            observer.observe(element);
        }
        _observers[listenerId] = observer;
    }

    function unobserve(listenerId: string) {
        const observer = _observers[listenerId];
        if (!observer) return;
        delete _observers[listenerId];
        observer.disconnect();
    }
}(BitButil));
