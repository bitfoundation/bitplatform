var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The gate is kept next to the observer so unobserve can cancel a queued trailing send -
    // see resizeObserver.ts for why disconnecting alone is not enough.
    const _observers: { [id: string]: { observer: MutationObserver, gated: any } } = {};

    butil.mutationObserver = {
        observe,
        unobserve
    };

    function observe(dotNetRef: any, listenerId: string, element: HTMLElement, options: any) {
        if (!element || !('MutationObserver' in window)) return;

        const init: MutationObserverInit = {
            childList: !!options?.childList,
            attributes: !!options?.attributes,
            characterData: !!options?.characterData,
            subtree: !!options?.subtree,
            attributeOldValue: !!options?.attributeOldValue,
            characterDataOldValue: !!options?.characterDataOldValue
        };
        if (options?.attributeFilter?.length) init.attributeFilter = options.attributeFilter;

        // Rate-limited around the dispatch. A subtree observer over a list Blazor is re-rendering
        // sees a record batch per render, and forwarding each one turns one render into one round
        // trip. Trailing, so the batch that describes the settled tree still arrives.
        //
        // Note what the gate drops: whole record batches, not individual records. A caller that has
        // to see every mutation - a change log, an undo stack - leaves the interval at zero; one
        // that reacts to the current state of the tree (which is nearly all of them) does not.
        const send = (payload: any) => butil.utils.dispatch(dotNetRef, 'InvokeMutation', listenerId, payload);
        const gated = butil.utils.throttle(options?.minInterval ?? 0, send, true);

        const observer = new MutationObserver(records => {
            const payload = records.map(r => ({
                type: r.type,
                targetTagName: (r.target as Element)?.tagName ?? '',
                targetId: (r.target as Element)?.id || null,
                attributeName: r.attributeName,
                attributeNamespace: r.attributeNamespace,
                oldValue: r.oldValue,
                addedCount: r.addedNodes?.length ?? 0,
                removedCount: r.removedNodes?.length ?? 0
            }));
            gated(payload);
        });

        try { observer.observe(element, init); }
        catch { /* invalid options combo - silently ignore so dotnet sees no records */ }
        _observers[listenerId] = { observer, gated };
    }

    function unobserve(listenerId: string) {
        const entry = _observers[listenerId];
        if (!entry) return;
        delete _observers[listenerId];
        entry.gated.cancel?.();
        entry.observer.disconnect();
    }
}(BitButil));
