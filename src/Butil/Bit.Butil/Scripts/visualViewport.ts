var BitButil = BitButil || {};

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

    function addResize(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler: EventListener = () => {
            butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        };

        _handlers[listenerId] = handler;
        window.visualViewport.addEventListener('resize', handler);
    }
    function removeResize(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('resize', handler);
        });
    }

    function addScroll(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler: EventListener = () => {
            butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        };

        _handlers[listenerId] = handler;
        window.visualViewport.addEventListener('scroll', handler);
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
        window.visualViewport.addEventListener('scrollend', handler);
    }
    function removeScrollEnd(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('scrollend', handler);
        });
    }
}(BitButil));