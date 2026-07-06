var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.history = {
        length() { return window.history.length },
        scrollRestoration() { return window.history.scrollRestoration },
        setScrollRestoration(value: ScrollRestoration) { window.history.scrollRestoration = value },
        state() { return window.history.state },
        back() { window.history.back() },
        forward() { window.history.forward() },
        go(delta: number) { window.history.go(delta) },
        pushState(state: any, unused: string, url?: string | null) { window.history.pushState(state, unused, url) },
        replaceState(state: any, unused: string, url?: string | null) { window.history.replaceState(state, unused, url) },
        addPopState,
        removePopState
    };

    function addPopState(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler = (e: PopStateEvent) => {
            butil.utils.dispatch(dotNetRef, 'InvokeHistoryPopState', listenerId, e.state);
        };

        _handlers[listenerId] = handler;
        window.addEventListener('popstate', handler);
    }

    function removePopState(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.removeEventListener('popstate', handler);
        });
    }
}(BitButil));