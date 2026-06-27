var BitButil = BitButil || {};

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

    function observe(dotNetRef: any, listenerId: string, element: HTMLElement, box: string) {
        if (!element || !('ResizeObserver' in window)) return;

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
            butil.utils.dispatch(dotNetRef, 'InvokeResize', listenerId, payload);
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
