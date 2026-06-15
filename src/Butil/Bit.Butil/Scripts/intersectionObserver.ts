var BitButil = BitButil || {};

(function (butil: any) {
    const _observers: { [id: string]: IntersectionObserver } = {};

    butil.intersectionObserver = {
        observe,
        unobserve
    };

    function toRect(r: DOMRectReadOnly | null) {
        if (!r) return null;
        return { x: r.x, y: r.y, width: r.width, height: r.height };
    }

    function observe(dotNetRef: any, listenerId: string, element: HTMLElement, options: any) {
        if (!element || !('IntersectionObserver' in window)) return;

        const init: IntersectionObserverInit = {
            rootMargin: options?.rootMargin ?? undefined,
            threshold: options?.thresholds && options.thresholds.length > 0 ? options.thresholds : 0
        };

        const observer = new IntersectionObserver(entries => {
            const payload = entries.map(e => ({
                isIntersecting: e.isIntersecting,
                intersectionRatio: e.intersectionRatio,
                time: e.time,
                boundingClientRect: toRect(e.boundingClientRect),
                intersectionRect: toRect(e.intersectionRect),
                rootBounds: toRect(e.rootBounds)
            }));
            butil.utils.dispatch(dotNetRef, 'InvokeIntersection', listenerId, payload);
        }, init);

        observer.observe(element);
        _observers[listenerId] = observer;
    }

    function unobserve(listenerId: string) {
        const observer = _observers[listenerId];
        if (!observer) return;
        delete _observers[listenerId];
        observer.disconnect();
    }
}(BitButil));
