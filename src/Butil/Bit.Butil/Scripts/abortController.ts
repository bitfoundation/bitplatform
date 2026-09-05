var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Entry {
        signal: AbortSignal;
        controller?: AbortController;   // absent for timeout()/any()-derived signals, which nobody can abort directly
        listeners: { [listenerId: string]: (e: any) => void };
        // Only for a hand-wired composite (the pre-Safari-17.4 any() fallback): the source signals
        // it is watching, with the listener each one is holding on its behalf.
        sources?: { source: AbortSignal; handler: () => void }[];
    }

    // Every signal Butil hands to .NET lives here under the id .NET knows it by. Other modules
    // (fetch) reach in through signalOf() rather than keeping registries of their own.
    const _signals: { [id: string]: Entry } = {};

    // AbortSignal.reason is any value at all - a DOMException for abort()/timeout(), whatever was
    // passed otherwise. .NET only ever sees a string, so flatten it here in one place.
    function reasonOf(signal: AbortSignal): string {
        const reason: any = (signal as any).reason;
        if (reason === undefined || reason === null) return '';
        if (reason instanceof DOMException) return reason.message || reason.name;
        if (typeof reason === 'string') return reason;
        try { return JSON.stringify(reason); } catch { return String(reason); }
    }

    function track(id: string, signal: AbortSignal, controller?: AbortController) {
        _signals[id] = { signal, controller, listeners: {} };
    }

    // A source signal can outlive the composite watching it by a long way - a timeout signal held
    // for the life of the page, say - and until its listener is removed it keeps that composite
    // alive with it. Detach them as a set: the first abort makes the rest pointless anyway.
    function detachSources(entry: Entry) {
        if (!entry.sources) return;
        for (const pair of entry.sources) pair.source.removeEventListener('abort', pair.handler);
        entry.sources = undefined;
    }

    butil.abortController = {
        isSupported() { return typeof (window as any).AbortController === 'function'; },

        // True only when AbortSignal.any exists - Any() falls back to a controller wired by hand,
        // which behaves the same but cannot be told apart from a real composite signal.
        isAnySupported() { return typeof (window as any).AbortSignal?.any === 'function'; },
        isTimeoutSupported() { return typeof (window as any).AbortSignal?.timeout === 'function'; },

        create(id: string) {
            if (typeof (window as any).AbortController !== 'function') return false;
            const controller = new AbortController();
            track(id, controller.signal, controller);
            return true;
        },

        timeout(id: string, milliseconds: number) {
            const AS: any = (window as any).AbortSignal;
            if (typeof AS?.timeout !== 'function') return false;
            track(id, AS.timeout(milliseconds));
            return true;
        },

        // Composite: aborts as soon as any source does, carrying that source's reason.
        any(id: string, sourceIds: string[]) {
            const sources = (sourceIds ?? []).map(sourceId => _signals[sourceId]?.signal).filter(Boolean) as AbortSignal[];
            if (sources.length !== (sourceIds ?? []).length) return false; // an unknown id would silently weaken the composite

            const AS: any = (window as any).AbortSignal;
            if (typeof AS?.any === 'function') {
                track(id, AS.any(sources));
                return true;
            }

            if (typeof (window as any).AbortController !== 'function') return false;

            // Pre-Safari-17.4 fallback. A hand-wired controller is observably the same thing: it
            // forwards the first source's reason and stops listening once it has fired.
            const controller = new AbortController();
            track(id, controller.signal, controller);

            const already = sources.find(source => source.aborted);
            if (already) {
                controller.abort((already as any).reason);
                return true;
            }

            const entry = _signals[id];
            entry.sources = sources.map(source => ({
                source,
                handler: () => {
                    detachSources(entry);
                    controller.abort((source as any).reason);
                }
            }));
            for (const pair of entry.sources) pair.source.addEventListener('abort', pair.handler, { once: true });
            return true;
        },

        abort(id: string, reason: string | null) {
            const entry = _signals[id];
            if (!entry?.controller) return false;
            // abort() with no argument produces the standard "AbortError" DOMException, which is
            // what callers who pass no reason expect to see; abort('') would replace it with ''.
            try { reason ? entry.controller.abort(reason) : entry.controller.abort(); } catch { /* already aborted */ }
            return true;
        },

        aborted(id: string) { return _signals[id]?.signal.aborted ?? false; },

        reason(id: string) {
            const entry = _signals[id];
            return entry ? reasonOf(entry.signal) : '';
        },

        addListener(dotNetRef: any, id: string, listenerId: string) {
            const entry = _signals[id];
            if (!entry) return false;

            const handler = () => butil.utils.dispatch(dotNetRef, 'InvokeAbort', listenerId, reasonOf(entry.signal));

            // An already-aborted signal never fires 'abort' again, so a late listener would never be
            // called at all. Dispatching once matches what a caller means by "tell me when this aborts".
            if (entry.signal.aborted) {
                handler();
                return true;
            }

            entry.listeners[listenerId] = handler;
            entry.signal.addEventListener('abort', handler, { once: true });
            return true;
        },

        removeListener(id: string, listenerId: string) {
            const entry = _signals[id];
            const handler = entry?.listeners[listenerId];
            if (!handler) return;
            delete entry.listeners[listenerId];
            entry.signal.removeEventListener('abort', handler);
        },

        // Drops the registry entry. The signal itself is not aborted - releasing a handle is not the
        // same as cancelling what it guards, and a signal already handed to fetch keeps working.
        release(id: string) {
            const entry = _signals[id];
            if (!entry) return;
            for (const listenerId of Object.keys(entry.listeners)) {
                entry.signal.removeEventListener('abort', entry.listeners[listenerId]);
            }
            detachSources(entry);
            delete _signals[id];
        },

        releaseAll() {
            for (const id of Object.keys(_signals)) butil.abortController.release(id);
        },

        // For other modules: the live AbortSignal behind an id, or undefined.
        signalOf(id: string) { return _signals[id]?.signal; }
    };
}(BitButil));
