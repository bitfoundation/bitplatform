var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    type Registration = { type: 'resize' | 'scroll' | 'scrollend', handler: EventListener };

    // One map for all three events, keyed by listener id, and each entry remembers which event it is
    // on: removal is by id, and an id has to be detached from the event it was attached to - not from
    // whichever one the caller happened to name.
    const _handlers: { [id: string]: Registration } = {};

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
        addScrollEnd, removeScrollEnd,
        removeAll
    };

    // resize and scroll both fire once a frame for as long as a pinch-zoom pan lasts, and the
    // notification carries no payload - the .NET handler reads the viewport afterwards, so one
    // event is a round trip to say "look again" plus a round trip per property looked at. Both
    // take a minimum interval for that reason, gated trailing so the settled viewport is always
    // the last thing .NET is told about. A zero interval leaves the dispatch untouched.
    function addResize(dotNetRef: DotNet.DotNetObject, listenerId: string, minInterval: number) {
        add('resize', dotNetRef, listenerId, minInterval);
    }
    function removeResize(ids: string[]) { remove(ids, 'resize'); }

    function addScroll(dotNetRef: DotNet.DotNetObject, listenerId: string, minInterval: number) {
        add('scroll', dotNetRef, listenerId, minInterval);
    }
    function removeScroll(ids: string[]) { remove(ids, 'scroll'); }

    // scroll fires continuously through a pinch-zoom pan; scrollend fires once when it settles,
    // which is the one you want for re-laying out or persisting a position. Not gated: it fires
    // once, when the pan settles - there is nothing to rate-limit.
    function addScrollEnd(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        add('scrollend', dotNetRef, listenerId, 0);
    }
    function removeScrollEnd(ids: string[]) { remove(ids, 'scrollend'); }

    // Disposal: every id this instance registered, whatever event each is on.
    function removeAll(ids: string[]) { remove(ids); }

    function add(type: Registration['type'], dotNetRef: DotNet.DotNetObject, listenerId: string, minInterval: number) {
        const notify = () => butil.utils.dispatch(dotNetRef, 'InvokeVisualViewport', listenerId);
        const handler: EventListener = butil.utils.throttle(minInterval, notify, true);

        _handlers[listenerId] = { type, handler };
        // Passive: none of these three listeners cancels anything, and visualViewport events are
        // not passive by default the way a window scroll listener is.
        window.visualViewport.addEventListener(type, handler, { passive: true });
    }

    // With a type, only ids registered on that event are touched - an id of another event is left
    // for the call that names its event. Without one, every id given is detached from its own event.
    function remove(ids: string[], type?: Registration['type']) {
        ids.forEach(id => {
            const entry = _handlers[id];
            if (!entry || (type && entry.type !== type)) return;
            delete _handlers[id];
            // The handler *is* the gate here, so cancelling it drops a trailing send that would
            // otherwise fire into a DotNetObjectReference disposed right after this call.
            (entry.handler as any)?.cancel?.();
            window.visualViewport.removeEventListener(entry.type, entry.handler);
        });
    }
}(BitButil));