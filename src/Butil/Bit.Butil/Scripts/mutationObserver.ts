var BitButil = BitButil || {};

(function (butil: any) {
    const _observers: { [id: string]: MutationObserver } = {};

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
            butil.utils.dispatch(dotNetRef, 'InvokeMutation', listenerId, payload);
        });

        try { observer.observe(element, init); }
        catch { /* invalid options combo - silently ignore so dotnet sees no records */ }
        _observers[listenerId] = observer;
    }

    function unobserve(listenerId: string) {
        const observer = _observers[listenerId];
        if (!observer) return;
        delete _observers[listenerId];
        observer.disconnect();
    }
}(BitButil));
