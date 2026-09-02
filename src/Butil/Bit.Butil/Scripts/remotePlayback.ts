var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface AvailabilityEntry { remote: any; callbackId: number | null; }
    interface StateEntry { remote: any; handler: () => void; }

    const _availability: { [listenerId: string]: AvailabilityEntry } = {};
    const _states: { [listenerId: string]: StateEntry } = {};

    butil.remotePlayback = {
        isSupported() { return 'remote' in HTMLMediaElement.prototype; },
        state(element: any) { return element?.remote?.state ?? 'disconnected'; },
        setDisabled(element: any, disabled: boolean) {
            if (!element) return false;
            // The attribute is what actually hides the cast affordance the browser draws inside the
            // element's own controls - there is no method for it.
            try { element.disableRemotePlayback = disabled; return true; } catch { return false; }
        },
        async prompt(element: any) {
            if (!element?.remote?.prompt) return false;
            try { await element.remote.prompt(); return true; }
            catch { return false; }  // dismissed, no device found, or no user gesture behind the call
        },
        async watchAvailability(listenerId: string, element: any, dotNetRef: any, method: string) {
            const remote = element?.remote;
            if (!remote?.watchAvailability) return false;

            butil.remotePlayback.cancelWatch(listenerId);
            const entry: AvailabilityEntry = { remote, callbackId: null };
            _availability[listenerId] = entry;
            try {
                // The callback fires immediately with the current answer as well as on every change,
                // so .NET never has to ask separately whether a device is around right now.
                entry.callbackId = await remote.watchAvailability((available: boolean) =>
                    butil.utils.dispatch(dotNetRef, method, listenerId, !!available));
                return true;
            } catch {
                // Blocked by permissions policy, or the element opted out of remote playback.
                delete _availability[listenerId];
                return false;
            }
        },
        cancelWatch(listenerId: string) {
            const entry = _availability[listenerId];
            if (!entry) return;
            delete _availability[listenerId];
            if (entry.callbackId === null) return;
            try { entry.remote.cancelWatchAvailability(entry.callbackId); } catch { /* element already gone */ }
        },
        subscribeState(listenerId: string, element: any, dotNetRef: any, method: string) {
            const remote = element?.remote;
            if (!remote) return false;
            const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, remote.state ?? 'disconnected');
            _states[listenerId] = { remote, handler };
            remote.addEventListener('connecting', handler);
            remote.addEventListener('connect', handler);
            remote.addEventListener('disconnect', handler);
            return true;
        },
        unsubscribeState(listenerId: string) {
            const entry = _states[listenerId];
            if (!entry) return;
            delete _states[listenerId];
            try {
                entry.remote.removeEventListener('connecting', entry.handler);
                entry.remote.removeEventListener('connect', entry.handler);
                entry.remote.removeEventListener('disconnect', entry.handler);
            } catch { /* element already gone */ }
        },
        disposeAll() {
            for (const id of Object.keys(_availability)) butil.remotePlayback.cancelWatch(id);
            for (const id of Object.keys(_states)) butil.remotePlayback.unsubscribeState(id);
        }
    };
}(BitButil));
