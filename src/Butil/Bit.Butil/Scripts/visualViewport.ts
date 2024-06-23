var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

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

    function addResize(methodName, listenerId) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId);
        };

        _handlers[listenerId] = handler;
        window.visualViewport.addEventListener('resize', handler);
    }
    function removeResize(ids) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('resize', handler);
        });
    }

    function addScroll(methodName, listenerId) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId);
        };

        _handlers[listenerId] = handler;
        window.visualViewport.addEventListener('scroll', handler);
    }
    function removeScroll(ids) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.visualViewport.removeEventListener('scroll', handler);
        });
    }
}(BitButil));