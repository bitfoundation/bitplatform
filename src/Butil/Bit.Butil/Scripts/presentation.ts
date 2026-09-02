var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface ConnectionEntry {
        connection: any;
        message: (e: any) => void;
        change: () => void;
    }

    interface AvailabilityEntry { availability: any; handler: () => void; }

    const _connections: { [id: string]: ConnectionEntry } = {};
    const _availability: { [listenerId: string]: AvailabilityEntry } = {};
    let _receiverWatch: { list: any, handler: () => void } | null = null;

    butil.presentation = {
        isSupported() { return !!(navigator as any).presentation && !!(window as any).PresentationRequest; },
        isReceiver() { return !!(navigator as any).presentation?.receiver; },
        watchAvailability,
        cancelWatch,
        setDefaultRequest,
        start,
        reconnect,
        send,
        sendBytes,
        state,
        close,
        terminate,
        watchReceiver,
        disposeAll
    };

    function newRequest(urls: string[]) {
        const Ctor: any = (window as any).PresentationRequest;
        if (!Ctor || !urls?.length) return null;
        try { return new Ctor(urls.length === 1 ? urls[0] : urls); } catch { return null; }
    }

    async function watchAvailability(listenerId: string, urls: string[], dotNetRef: any, method: string) {
        const request = newRequest(urls);
        if (!request?.getAvailability) return false;

        cancelWatch(listenerId);
        try {
            const availability = await request.getAvailability();
            const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, !!availability.value);
            _availability[listenerId] = { availability, handler };
            availability.addEventListener('change', handler);
            // The object holds the current answer as soon as it exists, and only reports changes
            // afterwards - so the first value has to be pushed by hand or .NET waits for a change
            // that may never come.
            handler();
            return true;
        } catch {
            // NotSupportedError where the engine cannot monitor availability continuously; the
            // documented fallback is to call start() and let the picker report the truth.
            return false;
        }
    }

    function cancelWatch(listenerId: string) {
        const entry = _availability[listenerId];
        if (!entry) return;
        delete _availability[listenerId];
        try { entry.availability.removeEventListener('change', entry.handler); } catch { /* already gone */ }
    }

    function setDefaultRequest(urls: string[]) {
        const presentation = (navigator as any).presentation;
        if (!presentation) return false;
        try {
            // The browser's own "cast this page" menu item starts this request; without it that menu
            // item does nothing for the page.
            presentation.defaultRequest = urls?.length ? newRequest(urls) : null;
            return true;
        } catch {
            return false;
        }
    }

    async function start(id: string, urls: string[], dotNetRef: any, messageMethod: string, changeMethod: string) {
        const request = newRequest(urls);
        if (!request?.start) return null;
        try {
            return attach(id, await request.start(), dotNetRef, messageMethod, changeMethod);
        } catch {
            // The user dismissed the picker, or no receiver accepted the URL.
            return null;
        }
    }

    async function reconnect(id: string, urls: string[], presentationId: string, dotNetRef: any, messageMethod: string, changeMethod: string) {
        const request = newRequest(urls);
        if (!request?.reconnect) return null;
        try {
            return attach(id, await request.reconnect(presentationId), dotNetRef, messageMethod, changeMethod);
        } catch {
            // No presentation with that id is still running.
            return null;
        }
    }

    function attach(id: string, connection: any, dotNetRef: any, messageMethod: string, changeMethod: string) {
        close(id);

        const message = (e: any) => {
            // A receiver can send either text or binary; the two are kept apart on the .NET side
            // rather than merged into a lossy string.
            if (typeof e.data === 'string') butil.utils.dispatch(dotNetRef, messageMethod, id, e.data, null);
            else if (e.data instanceof ArrayBuffer) butil.utils.dispatch(dotNetRef, messageMethod, id, null, new Uint8Array(e.data));
            else if (e.data?.arrayBuffer) e.data.arrayBuffer().then((buf: ArrayBuffer) => butil.utils.dispatch(dotNetRef, messageMethod, id, null, new Uint8Array(buf)));
        };
        const change = () => butil.utils.dispatch(dotNetRef, changeMethod, id, connection.state ?? 'terminated');

        _connections[id] = { connection, message, change };
        connection.addEventListener('message', message);
        connection.addEventListener('connect', change);
        connection.addEventListener('close', change);
        connection.addEventListener('terminate', change);

        return { connectionId: connection.id ?? '', url: connection.url ?? '', state: connection.state ?? 'connecting' };
    }

    function send(id: string, message: string) {
        const entry = _connections[id];
        if (!entry) return false;
        try { entry.connection.send(message); return true; }
        catch { return false; }  // the connection is closed or still connecting
    }

    function sendBytes(id: string, data: Uint8Array) {
        const entry = _connections[id];
        if (!entry) return false;
        try { entry.connection.send(butil.utils.arrayToBuffer(data)); return true; }
        catch { return false; }
    }

    function state(id: string) { return _connections[id]?.connection.state ?? 'terminated'; }

    function detach(entry: ConnectionEntry) {
        try {
            entry.connection.removeEventListener('message', entry.message);
            entry.connection.removeEventListener('connect', entry.change);
            entry.connection.removeEventListener('close', entry.change);
            entry.connection.removeEventListener('terminate', entry.change);
        } catch { /* the connection object is gone */ }
    }

    // Closing leaves the presentation running on the other screen and merely lets go of it, which is
    // what makes a later reconnect() possible. Terminating is what actually stops it.
    function close(id: string) {
        const entry = _connections[id];
        if (!entry) return;
        delete _connections[id];
        detach(entry);
        try { entry.connection.close(); } catch { /* already closed */ }
    }

    function terminate(id: string) {
        const entry = _connections[id];
        if (!entry) return false;
        delete _connections[id];
        detach(entry);
        try { entry.connection.terminate(); return true; } catch { return false; }
    }

    // The receiving half: a page opened on the second screen finds its controllers here rather than
    // creating connections of its own. Each one is registered like a controller-side connection, so
    // the receiver can talk back over it with the same send/close calls.
    async function watchReceiver(dotNetRef: any, connectionMethod: string, messageMethod: string, changeMethod: string) {
        const receiver = (navigator as any).presentation?.receiver;
        if (!receiver) return false;
        try {
            const list = await receiver.connectionList;
            const seen = new WeakSet<any>();
            const handler = () => {
                // The list holds every connection, not only the new one, so already-registered
                // connections are skipped rather than attached (and re-reported) twice.
                for (const connection of list.connections ?? []) {
                    if (seen.has(connection)) continue;
                    seen.add(connection);
                    const id = newId();
                    const info = attach(id, connection, dotNetRef, messageMethod, changeMethod);
                    butil.utils.dispatch(dotNetRef, connectionMethod, id, info.connectionId, info.url, info.state);
                }
            };
            _receiverWatch = { list, handler };
            list.addEventListener('connectionavailable', handler);
            handler();
            return true;
        } catch {
            return false;
        }
    }

    function newId() {
        const uuid = (crypto as any).randomUUID?.();
        if (uuid) return uuid;
        const bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
        return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
    }

    function disposeAll() {
        for (const id of Object.keys(_connections)) close(id);
        for (const listenerId of Object.keys(_availability)) cancelWatch(listenerId);
        if (_receiverWatch) {
            try { _receiverWatch.list.removeEventListener('connectionavailable', _receiverWatch.handler); } catch { /* already gone */ }
            _receiverWatch = null;
        }
    }
}(BitButil));
