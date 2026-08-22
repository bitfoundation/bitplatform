var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _transitions: { [id: string]: any } = {};

    butil.viewTransition = {
        isSupported() { return typeof (document as any).startViewTransition === 'function'; },
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
        }
    };
}(BitButil));
