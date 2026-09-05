var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Listener { handler: (e: MessageEvent) => void; origins: string[]; }

    const _listeners: { [id: string]: Listener } = {};

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
            const origins = allowedOrigins ?? [];

            const handler = (e: MessageEvent) => {
                // The origin check is the whole security model of cross-document messaging: anyone
                // can post to a window they have a reference to, so a listener that does not check
                // is a listener that trusts every page on the internet. An empty list means the
                // caller opted out explicitly.
                if (origins.length > 0 && origins.indexOf(e.origin) < 0) return;

                // Ports that arrived with the message are adopted into the messageChannel registry
                // so .NET gets handles it can use, rather than a boolean saying some arrived.
                const portIds: string[] = [];
                for (const port of e.ports ?? []) {
                    const portId = butil.utils.randomUUID();
                    butil.messageChannel.adopt(portId, port);
                    portIds.push(portId);
                }

                butil.utils.dispatch(dotNetRef, 'InvokeWindowMessage', listenerId, e.origin ?? '',
                    ...butil.utils.encodeMessage(e.data), portIds);
            };

            _listeners[listenerId] = { handler, origins };
            window.addEventListener('message', handler);
            return true;
        },

        removeListener(listenerId: string) {
            const listener = _listeners[listenerId];
            if (!listener) return;
            delete _listeners[listenerId];
            window.removeEventListener('message', listener.handler);
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
