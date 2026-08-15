var BitButil = BitButil || {};

(function (butil: any) {
    interface Entry { source: EventSource; named: { [name: string]: (e: any) => void }; }

    const _sources: { [id: string]: Entry } = {};

    butil.eventSource = {
        isSupported() { return typeof (window as any).EventSource === 'function'; },
        open(dotNetRef: any, id: string, url: string, withCredentials: boolean, eventNames: string[]) {
            const ES = (window as any).EventSource;
            if (typeof ES !== 'function') return false;

            let source: EventSource;
            try {
                source = new ES(url, { withCredentials });
            } catch {
                // A malformed URL is the only synchronous failure; everything else surfaces as an
                // error event on the stream.
                return false;
            }

            const entry: Entry = { source, named: {} };
            _sources[id] = entry;

            source.addEventListener('open', () =>
                butil.utils.dispatch(dotNetRef, 'InvokeEventSourceOpen', id));

            // 'message' carries every event the server didn't name.
            source.addEventListener('message', (e: any) =>
                butil.utils.dispatch(dotNetRef, 'InvokeEventSourceMessage', id, 'message', e.data ?? '', e.lastEventId ?? ''));

            for (const name of eventNames ?? []) {
                // Named events don't reach 'message' at all, so each one needs its own listener -
                // there is no wildcard in the API.
                const handler = (e: any) =>
                    butil.utils.dispatch(dotNetRef, 'InvokeEventSourceMessage', id, name, e.data ?? '', e.lastEventId ?? '');
                entry.named[name] = handler;
                source.addEventListener(name, handler);
            }

            source.addEventListener('error', () => {
                // The error event carries nothing useful; readyState is what distinguishes "the
                // browser is reconnecting" (CONNECTING) from "this stream is over" (CLOSED).
                const fatal = source.readyState === 2;
                butil.utils.dispatch(dotNetRef, 'InvokeEventSourceError', id, fatal);
            });

            return true;
        },
        // 0 connecting, 1 open, 2 closed. 2 is also the answer for an id we no longer know about.
        readyState(id: string) { return _sources[id]?.source.readyState ?? 2; },
        close(id: string) {
            const entry = _sources[id];
            if (!entry) return;
            delete _sources[id];
            for (const name of Object.keys(entry.named)) {
                entry.source.removeEventListener(name, entry.named[name]);
            }
            // Without close() the browser keeps reconnecting on its own schedule for as long as
            // the page lives, which is exactly the leak an abandoned stream would be.
            try { entry.source.close(); } catch { /* already closed */ }
        },
        disposeAll() {
            for (const id of Object.keys(_sources)) butil.eventSource.close(id);
        }
    };
}(BitButil));
