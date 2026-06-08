var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.history = {
        length() { return window.history.length },
        scrollRestoration() { return window.history.scrollRestoration },
        setScrollRestoration(value) { window.history.scrollRestoration = value },
        state() { return window.history.state },
        back() { window.history.back() },
        forward() { window.history.forward() },
        go(delta) { window.history.go(delta) },
        pushState(state, unused, url) { window.history.pushState(state, unused, url) },
        replaceState(state, unused, url) { window.history.replaceState(state, unused, url) },
        addPopState,
        removePopState
    };

    function addPopState(dotNetRef, listenerId) {
        const handler = e => {
            dotNetRef.invokeMethodAsync('InvokeHistoryPopState', listenerId, e.state);
        };

        _handlers[listenerId] = handler;
        window.addEventListener('popstate', handler);
    }

    function removePopState(ids) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.removeEventListener('popstate', handler);
        });
    }
}(BitButil));