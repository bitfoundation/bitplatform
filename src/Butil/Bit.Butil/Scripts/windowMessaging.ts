var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Listener { dotNetRef: any; origins: string[]; }

    const _listeners: { [id: string]: Listener } = {};

    // One window listener for the whole module, fanned out in here. A listener each would mean
    // adopting the ports on a message once per listener - several handles over one MessagePort,
    // where closing any of them closes it under all the others.
    let _windowListener: ((e: MessageEvent) => void) | null = null;

    function onWindowMessage(e: MessageEvent) {
        // The origin check is the whole security model of cross-document messaging: anyone can post
        // to a window they have a reference to, so a listener that does not check is a listener that
        // trusts every page on the internet. An empty list means the caller opted out explicitly.
        const targets = Object.keys(_listeners)
            .filter(id => _listeners[id].origins.length === 0 || _listeners[id].origins.indexOf(e.origin) >= 0);

        // Nothing to deliver to, so nothing to adopt either: a port taken into the registry here
        // would have no handle to reach it and would sit there until the service is disposed.
        if (targets.length === 0) return;

        // Ports that arrived with the message are adopted into the messageChannel registry so .NET
        // gets handles it can use, rather than a boolean saying some arrived. Once each, whatever
        // the number of listeners, because it is one port.
        const portIds: string[] = [];
        for (const port of e.ports ?? []) {
            const portId = butil.utils.randomUUID();
            butil.messageChannel.adopt(portId, port);
            portIds.push(portId);
        }

        // A port has one owner. Handing the same id to every listener would build a handle each over
        // one port, where the first disposal closes and releases it under all the others - the very
        // thing the single window listener above exists to prevent. So the ports go to the first
        // listener that accepts the origin, in registration order, and everyone else is told the
        // message arrived without them.
        const noPorts: string[] = [];
        const encoded = butil.utils.encodeMessage(e.data);
        for (let i = 0; i < targets.length; i++) {
            const id = targets[i];
            butil.utils.dispatch(_listeners[id].dotNetRef, 'InvokeWindowMessage',
                id, e.origin ?? '', ...encoded, i === 0 ? portIds : noPorts);
        }
    }

    function syncWindowListener() {
        const wanted = Object.keys(_listeners).length > 0;
        if (wanted && !_windowListener) {
            _windowListener = onWindowMessage;
            window.addEventListener('message', _windowListener);
        } else if (!wanted && _windowListener) {
            window.removeEventListener('message', _windowListener);
            _windowListener = null;
        }
    }

    // Resolves the window a message is being sent to. Kept as (kind, target) rather than a stored
    // reference because an iframe's contentWindow is replaced on every navigation - looking it up
    // per call is the only way to be talking to the document that is there now.
    function windowFor(kind: string, element: HTMLIFrameElement | null, windowId: string | null): Window | null {
        switch (kind) {
            case 'frame': return element?.contentWindow ?? null;
            case 'opened': return butil.window.refOf(windowId) ?? null;
            case 'opener': return window.opener ?? null;
            case 'parent': return window.parent ?? null;
            case 'top': return window.top ?? null;
            default: return null;
        }
    }

    butil.windowMessaging = {
        // window.postMessage has existed since forever; the check exists so callers can use the
        // same shape they use for everything else.
        isSupported() { return typeof window.postMessage === 'function'; },

        listen(dotNetRef: any, listenerId: string, allowedOrigins: string[]) {
            _listeners[listenerId] = { dotNetRef, origins: allowedOrigins ?? [] };
            syncWindowListener();
            return true;
        },

        removeListener(listenerId: string) {
            const listener = _listeners[listenerId];
            if (!listener) return;
            delete _listeners[listenerId];
            syncWindowListener();
        },

        postJson(kind: string, element: HTMLIFrameElement | null, windowId: string | null, targetOrigin: string, json: string | null) {
            const target = windowFor(kind, element, windowId);
            if (!target) return false;
            try {
                target.postMessage(json === null ? null : JSON.parse(json), targetOrigin);
                return true;
            } catch { return false; }
        },

        postBytes(kind: string, element: HTMLIFrameElement | null, windowId: string | null, targetOrigin: string, bytes: Uint8Array, transfer: boolean) {
            const target = windowFor(kind, element, windowId);
            if (!target) return false;
            const buffer = butil.utils.arrayToBuffer(bytes);
            try {
                target.postMessage(buffer, targetOrigin, transfer ? [buffer] : []);
                return true;
            } catch { return false; }
        },

        postWithPorts(kind: string, element: HTMLIFrameElement | null, windowId: string | null, targetOrigin: string, json: string | null, transferredPortIds: string[]) {
            const target = windowFor(kind, element, windowId);
            if (!target) return false;

            const ports = (transferredPortIds ?? []).map(id => butil.messageChannel.portOf(id)).filter(Boolean) as MessagePort[];
            if (ports.length !== (transferredPortIds ?? []).length) return false;

            try {
                target.postMessage(json === null ? null : JSON.parse(json), targetOrigin, ports);
                // The ports belong to the receiving document now.
                for (const id of transferredPortIds) butil.messageChannel.release(id);
                return true;
            } catch { return false; }
        },

        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.windowMessaging.removeListener(id);
        }
    };
}(BitButil));
