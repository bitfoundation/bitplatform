var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.visualViewport = {
        offsetLeft() { return window.visualViewport.offsetLeft; },
        offsetTop() { return window.visualViewport.offsetTop; },
        pageLeft() { return window.visualViewport.pageLeft; },
        pageTop() { return window.visualViewport.pageTop; },
        width() { return window.visualViewport.width; },
        height() { return window.visualViewport.height; },
        scale() { return window.visualViewport.scale; },
        addResize, removeResize,
        addScroll, removeScroll
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
}(BitButil));