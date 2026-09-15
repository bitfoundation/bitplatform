var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _mediaQueryHandlers: { [id: string]: { mql: MediaQueryList, handler: (e: MediaQueryListEvent) => void } } = {};

    // matchMedia and its change subscriptions.
    butil.windowMediaQuery = {
        matchMedia,
        subscribeMatchMedia,
        unsubscribeMatchMedia
    };

    function matchMedia(query: string) {
        const media = window.matchMedia(query);
        return {
            matches: media.matches,
            media: media.media
        };
    }

    function subscribeMatchMedia(dotNetRef: any, listenerId: string, query: string) {
        const mql = window.matchMedia(query);
        const handler = (e: MediaQueryListEvent) => {
            butil.utils.dispatch(dotNetRef, 'InvokeMediaQueryChange', listenerId, { matches: e.matches, media: e.media });
        };

        // addEventListener is supported on MediaQueryList in all evergreen browsers; older
        // Safari only exposes the legacy addListener variant.
        if (typeof mql.addEventListener === 'function') {
            mql.addEventListener('change', handler);
        } else {
            (mql as any).addListener(handler);
        }
        _mediaQueryHandlers[listenerId] = { mql, handler };
    }

    function unsubscribeMatchMedia(ids: string[]) {
        ids.forEach(id => {
            const entry = _mediaQueryHandlers[id];
            if (!entry) return;
            delete _mediaQueryHandlers[id];
            if (typeof entry.mql.removeEventListener === 'function') {
                entry.mql.removeEventListener('change', entry.handler);
            } else {
                (entry.mql as any).removeListener(entry.handler);
            }
        });
    }
}(BitButil));
