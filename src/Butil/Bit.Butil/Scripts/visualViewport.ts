var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.visualViewport = {
        isSupported() { return !!window.visualViewport; },
        offsetLeft() { return window.visualViewport.offsetLeft; },
        offsetTop() { return window.visualViewport.offsetTop; },
        pageLeft() { return window.visualViewport.pageLeft; },
        pageTop() { return window.visualViewport.pageTop; },
        width() { return window.visualViewport.width; },
        height() { return window.visualViewport.height; },
        scale() { return window.visualViewport.scale; },
        addResize, removeResize,
        addScroll, removeScroll,
        addScrollEnd, removeScrollEnd
    };

    // resize and scroll both fire once a frame for as long as a pinch-zoom pan lasts, and the
    // notification carries no payload - the .NET handler reads the viewport afterwards, so one
    // event is a round trip to say "look again" plus a round trip per property looked at. Both
    // take a minimum interval for that reason, gated trailing so the settled viewport is always
    // the last thing .NET is told about. A zero interval leaves the dispatch untouched.
    function addResize(dotNetRef: DotNet.DotNetObject, listenerId: string, minInterval: number) {
        const notify = () => butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        const handler: EventListener = butil.utils.throttle(minInterval, notify, true);

        _handlers[listenerId] = handler;
        // Passive: none of these three listeners cancels anything, and visualViewport events are
        // not passive by default the way a window scroll listener is.
        window.visualViewport.addEventListener('resize', handler, { passive: true });
    }
    function removeResize(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('resize', handler);
        });
    }

    function addScroll(dotNetRef: DotNet.DotNetObject, listenerId: string, minInterval: number) {
        const notify = () => butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        const handler: EventListener = butil.utils.throttle(minInterval, notify, true);

        _handlers[listenerId] = handler;
        window.visualViewport.addEventListener('scroll', handler, { passive: true });
    }
    function removeScroll(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('scroll', handler);
        });
    }

    // scroll fires continuously through a pinch-zoom pan; scrollend fires once when it settles,
    // which is the one you want for re-laying out or persisting a position.
    function addScrollEnd(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler: EventListener = () => {
            butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        };

        _handlers[listenerId] = handler;
        // Not gated: scrollend fires once, when the pan settles - there is nothing to rate-limit.
        window.visualViewport.addEventListener('scrollend', handler, { passive: true });
    }
    function removeScrollEnd(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('scrollend', handler);
        });
    }
}(BitButil));