var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.history = {
        length,
        scrollRestoration,
        setScrollRestoration,
        state,
        back,
        forward,
        go,
        pushState,
        replaceState,
        addPopState,
        removePopState
    };

    function length() {
        return window.history.length;
    }

    function scrollRestoration() {
        return window.history.scrollRestoration;
    }

    function setScrollRestoration(value) {
        window.history.scrollRestoration = value;
    }

    function state() {
        return window.history.state;
    }

    function back() {
        window.history.back();
    }

    function forward() {
        window.history.forward();
    }

    function go(delta) {
        window.history.go(delta);
    }

    function pushState(state, unused, url) {
        window.history.pushState(state, unused, url);
    }

    function replaceState(state, unused, url) {
        window.history.replaceState(state, unused, url);
    }

    function addPopState(methodName, listenerId) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId, e.state);
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