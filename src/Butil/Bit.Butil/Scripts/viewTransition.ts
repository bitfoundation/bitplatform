var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _transitions: { [id: string]: any } = {};
    const _pageListeners: { [id: string]: { name: string, handler: any } } = {};

    // The stylesheet that opts this document into cross-document transitions. Level 2 has no
    // scripted switch - `@view-transition { navigation: auto }` is the whole opt-in - so enabling it
    // from C# means injecting the rule and removing it again.
    const OPT_IN_ID = 'bit-butil-view-transition-opt-in';

    function describePageEvent(e: any, name: string) {
        const activation = e?.activation;
        return {
            name,
            hasTransition: !!e?.viewTransition,
            fromUrl: activation?.from?.url ?? '',
            toUrl: activation?.entry?.url ?? '',
            navigationType: activation?.navigationType ?? ''
        };
    }

    // 'pagereveal' fires on the incoming document before it is first painted - before .NET has
    // started, let alone subscribed. So the listener goes on while this module is evaluated (which,
    // for a bundle on the page, is early enough) and the event is parked until a subscriber asks for
    // it, the way launchQueue parks a launch. Under lazy scripts the module is imported on the first
    // call and there is nothing to park - the event is simply gone by then.
    let _revealed: any = null;
    if ('onpagereveal' in window) {
        window.addEventListener('pagereveal', (e: any) => { _revealed = describePageEvent(e, 'pagereveal'); }, { once: true });
    }

    butil.viewTransition = {
        isSupported() { return typeof (document as any).startViewTransition === 'function'; },
        isCrossDocumentSupported() {
            // Both halves have to be there: the opt-in rule the browser understands, and the events
            // that let a page take part in the transition it triggers.
            return 'CSSViewTransitionRule' in window && 'onpageswap' in window;
        },
        isCrossDocumentEnabled() { return !!document.getElementById(OPT_IN_ID); },
        enableCrossDocument(types: string[] | null) {
            let style = document.getElementById(OPT_IN_ID) as HTMLStyleElement | null;
            if (!style) {
                style = document.createElement('style');
                style.id = OPT_IN_ID;
                document.head.appendChild(style);
            }
            const typeRule = types?.length ? `\n  types: ${types.join(' ')};` : '';
            style.textContent = `@view-transition {\n  navigation: auto;${typeRule}\n}`;
            return true;
        },
        disableCrossDocument() {
            document.getElementById(OPT_IN_ID)?.remove();
        },
        onPageEvent(dotNetRef: any, listenerId: string, method: string, name: string) {
            // 'pageswap' fires on the old document just before it is snapshotted; 'pagereveal' on
            // the new one just before it is painted. They are the two places a cross-document
            // transition can be customized - or opted out of, by skipping it.
            if (!(`on${name}` in window)) return false;

            // Re-registering the same listener id would otherwise orphan the previous handler.
            butil.viewTransition.offPageEvent(listenerId);

            const handler = (e: any) => butil.utils.dispatch(dotNetRef, method, listenerId, describePageEvent(e, name));

            window.addEventListener(name, handler);
            _pageListeners[listenerId] = { name, handler };

            // This document's own 'pagereveal' has already happened; replaying it is the only way a
            // subscriber can see how this document was navigated into.
            if (name === 'pagereveal' && _revealed) {
                const revealed = _revealed;
                _revealed = null;
                butil.utils.dispatch(dotNetRef, method, listenerId, revealed);
            }

            return true;
        },
        offPageEvent(listenerId: string) {
            const entry = _pageListeners[listenerId];
            if (!entry) return;
            delete _pageListeners[listenerId];
            try { window.removeEventListener(entry.name, entry.handler); } catch { /* window gone */ }
        },
        start(dotNetRef: any, id: string, types: string[] | null) {
            const start = (document as any).startViewTransition;
            if (typeof start !== 'function') return false;

            // The browser snapshots the "before" state synchronously here, calls the callback, and
            // snapshots "after" when the callback's promise resolves. Handing it the promise from
            // invokeMethodAsync is what lets a Blazor render happen between the two snapshots -
            // .NET decides when the DOM is settled, not us.
            const callback = () => butil.utils.dispatch(dotNetRef, 'InvokeViewTransitionUpdate', id);

            let transition: any;
            try {
                // Level 2 takes an options bag with `types`; level 1 takes the callback directly.
                // Passing an object to a level-1 implementation would call it as the callback, so
                // only take that path when types were actually asked for.
                transition = types?.length
                    ? start.call(document, { update: callback, types })
                    : start.call(document, callback);
            } catch {
                return false;
            }

            _transitions[id] = transition;

            // Every phase is a promise, and each is reported separately so .NET can await the one
            // it cares about. A skipped transition rejects `ready` while `finished` still resolves,
            // so the rejection is caught rather than left unhandled.
            transition.ready
                ?.then(() => butil.utils.dispatch(dotNetRef, 'InvokeViewTransitionPhase', id, 'ready', ''))
                .catch((e: any) => butil.utils.dispatch(dotNetRef, 'InvokeViewTransitionPhase', id, 'skipped', e?.message ?? ''));

            transition.finished
                ?.then(() => {
                    delete _transitions[id];
                    butil.utils.dispatch(dotNetRef, 'InvokeViewTransitionPhase', id, 'finished', '');
                })
                .catch((e: any) => {
                    delete _transitions[id];
                    butil.utils.dispatch(dotNetRef, 'InvokeViewTransitionPhase', id, 'failed', e?.message ?? '');
                });

            return true;
        },
        skip(id: string) {
            const transition = _transitions[id];
            if (!transition?.skipTransition) return;
            // Jumps straight to the end state; `finished` still resolves, so the entry is cleaned
            // up by that handler rather than here.
            try { transition.skipTransition(); } catch { /* already finished */ }
        },
        disposeAll() {
            for (const id of Object.keys(_transitions)) {
                butil.viewTransition.skip(id);
                delete _transitions[id];
            }
            for (const id of Object.keys(_pageListeners)) butil.viewTransition.offPageEvent(id);
        }
    };
}(BitButil));
