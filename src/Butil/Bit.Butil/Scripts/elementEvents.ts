var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Element-scoped event handlers, indexed by listenerId so element teardown can find them.
    const _elementHandlers: { [listenerId: string]: { element: HTMLElement, eventName: string, handler: any, options: any } } = {};

    // Its own module because this is the only part of the element surface that needs the event
    // mapper: keeping it here leaves the events module out of every other element call.
    butil.elementEvents = {
        subscribeEvent,
        unsubscribeEvent,
    };

    function subscribeEvent(element: HTMLElement, elementId: string, eventName: string, methodName: string,
        dotNetRef: any, listenerId: string, argsMembers: string[], options: AddEventListenerOptions | boolean,
        preventDefault: boolean, stopPropagation: boolean, minInterval: number) {
        if (!element) return;
        // When { once: true } is set the browser auto-detaches after the first call; mirror that by
        // dropping our tracking entry so the listenerId doesn't linger after it fires.
        const once = typeof options === 'object' && options.once === true;
        // Rate-limited around the dispatch, not around the handler - see the same gate in events.ts
        // for why preventDefault still runs on every event and why the payload is mapped first.
        const send = (payload: any) => butil.utils.dispatch(dotNetRef, methodName, listenerId, payload);
        const gated = butil.utils.throttle(minInterval, send, true);
        const handler = (e: any) => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            if (once) delete _elementHandlers[listenerId];
            gated(butil.events.mapEvent(e, argsMembers));
        };
        _elementHandlers[listenerId] = { element, eventName, handler, options };
        element.addEventListener(eventName, handler, options);
    }

    function unsubscribeEvent(elementId: string, eventName: string, listenerId: string, options: AddEventListenerOptions | boolean) {
        const entry = _elementHandlers[listenerId];
        if (!entry) return;
        delete _elementHandlers[listenerId];
        try {
            entry.element.removeEventListener(entry.eventName, entry.handler, entry.options);
        } catch { /* element may already be detached */ }
    }
}(BitButil));
