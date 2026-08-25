var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: { event: string; handler: EventListener } } = {};

    function nav() { return (window as any).navigation ?? null; }

    function toEntry(e: any) {
        if (!e) return null;
        return {
            key: e.key ?? '',
            id: e.id ?? '',
            url: e.url ?? '',
            index: typeof e.index === 'number' ? e.index : -1,
            sameDocument: !!e.sameDocument
        };
    }

    // navigate()/back()/traverseTo() hand back a NavigationResult holding two promises. `committed`
    // settles once the entry is in the list; `finished` settles when any interception handler is
    // done. Both reject on an aborted or refused navigation, and an unobserved rejection is an
    // "Uncaught (in promise)" in the console - so both are always attached, and the caller gets a
    // boolean instead of an exception for what is a normal outcome.
    async function settle(result: any) {
        if (!result) return false;
        try {
            result.finished?.catch?.(() => { });
            await result.committed;
            return true;
        } catch {
            return false;
        }
    }

    butil.navigation = {
        isSupported() { return !!nav(); },

        canGoBack() { return !!nav()?.canGoBack; },
        canGoForward() { return !!nav()?.canGoForward; },

        currentEntry() { return toEntry(nav()?.currentEntry); },
        entries() {
            const list = nav()?.entries?.() ?? [];
            return list.map(toEntry);
        },
        currentState() {
            const state = nav()?.currentEntry?.getState?.();
            return state === undefined ? null : state;
        },

        async back() {
            const n = nav();
            // Calling back() with nothing behind it throws InvalidStateError rather than resolving
            // false, so the guard is what makes this answerable instead of exceptional.
            if (!n?.canGoBack) return false;
            return await settle(n.back());
        },
        async forward() {
            const n = nav();
            if (!n?.canGoForward) return false;
            return await settle(n.forward());
        },
        async traverseTo(key: string) {
            const n = nav();
            if (!n || !key) return false;
            try { return await settle(n.traverseTo(key)); }
            catch { return false; }   // the key names an entry that has since been disposed
        },
        async navigate(url: string, state: any, history: string) {
            const n = nav();
            if (!n) return false;
            const options: any = {};
            if (state !== null && state !== undefined) options.state = state;
            if (history === 'push' || history === 'replace') options.history = history;
            try { return await settle(n.navigate(url, options)); }
            catch { return false; }   // a url the document isn't allowed to navigate to
        },
        async reload(state: any) {
            const n = nav();
            if (!n) return false;
            try {
                return await settle(n.reload(state === null || state === undefined ? undefined : { state }));
            } catch { return false; }
        },
        updateCurrentEntry(state: any) {
            const n = nav();
            // Before the document has committed its first entry there is nothing to update, and
            // the call throws rather than no-opping.
            if (!n?.currentEntry) return false;
            try { n.updateCurrentEntry({ state }); return true; }
            catch { return false; }
        },

        subscribe(dotNetRef: DotNet.DotNetObject, listenerId: string, event: string) {
            const n = nav();
            if (!n) return false;

            const handler: EventListener = (e: any) => {
                butil.utils.dispatch(dotNetRef, 'InvokeNavigation', listenerId, event, {
                    from: event === 'currententrychange' ? toEntry(e.from) : null,
                    navigationType: e.navigationType ?? null,
                    message: e.message ?? null
                });
            };

            _handlers[listenerId] = { event, handler };
            n.addEventListener(event, handler);
            return true;
        },
        unsubscribe(ids: string[]) {
            const n = nav();
            ids.forEach(id => {
                const entry = _handlers[id];
                if (!entry) return;
                delete _handlers[id];
                n?.removeEventListener(entry.event, entry.handler);
            });
        }
    };
}(BitButil));
