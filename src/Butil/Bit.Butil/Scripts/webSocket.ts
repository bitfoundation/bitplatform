var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _sockets: { [id: string]: WebSocket } = {};

    butil.webSocket = {
        isSupported() { return typeof (window as any).WebSocket === 'function'; },

        open(dotNetRef: any, id: string, url: string, protocols: string[]) {
            const WS = (window as any).WebSocket;
            if (typeof WS !== 'function') return false;

            let socket: WebSocket;
            try {
                socket = (protocols && protocols.length > 0) ? new WS(url, protocols) : new WS(url);
            } catch {
                // A malformed URL, a non-ws scheme, or a mixed-content block: the only synchronous
                // failures. Everything else arrives as an error/close event on the socket.
                return false;
            }

            // 'blob' is the default and is useless to .NET - it would need an extra async read per
            // frame. ArrayBuffer hands the bytes over directly.
            socket.binaryType = 'arraybuffer';
            _sockets[id] = socket;

            socket.addEventListener('open', () =>
                butil.utils.dispatch(dotNetRef, 'InvokeWebSocketOpen', id, socket.protocol ?? '', socket.extensions ?? ''));

            socket.addEventListener('message', (e: any) => {
                if (typeof e.data === 'string') {
                    butil.utils.dispatch(dotNetRef, 'InvokeWebSocketMessage', id, false, e.data, null);
                } else {
                    butil.utils.dispatch(dotNetRef, 'InvokeWebSocketMessage', id, true, null, new Uint8Array(e.data));
                }
            });

            // The error event carries nothing by design (it would leak cross-origin detail); the
            // close event that follows is where the code and reason are.
            socket.addEventListener('error', () =>
                butil.utils.dispatch(dotNetRef, 'InvokeWebSocketError', id));

            socket.addEventListener('close', (e: any) => {
                delete _sockets[id];
                butil.utils.dispatch(dotNetRef, 'InvokeWebSocketClose', id, e.code ?? 1006, e.reason ?? '', e.wasClean === true);
            });

            return true;
        },

        sendText(id: string, text: string) {
            const socket = _sockets[id];
            // Sending before 'open' throws InvalidStateError rather than queueing, which is a normal
            // race for a caller that did not wait - report it instead of letting it throw into .NET.
            if (!socket || socket.readyState !== 1) return false;
            try { socket.send(text); return true; } catch { return false; }
        },

        sendBytes(id: string, bytes: Uint8Array) {
            const socket = _sockets[id];
            if (!socket || socket.readyState !== 1) return false;
            try { socket.send(butil.utils.arrayToBuffer(bytes)); return true; } catch { return false; }
        },

        // 0 connecting, 1 open, 2 closing, 3 closed. 3 is also the answer for an id we no longer
        // know about - a socket that has closed is removed by its own close listener.
        readyState(id: string) { return _sockets[id]?.readyState ?? 3; },

        // Bytes queued by send() but not yet on the wire. The back-pressure signal: a caller that
        // never checks it can grow this without bound.
        bufferedAmount(id: string) { return _sockets[id]?.bufferedAmount ?? 0; },

        protocol(id: string) { return _sockets[id]?.protocol ?? ''; },
        extensions(id: string) { return _sockets[id]?.extensions ?? ''; },
        url(id: string) { return _sockets[id]?.url ?? ''; },

        close(id: string, code: number | null, reason: string | null) {
            const socket = _sockets[id];
            if (!socket) return;
            // The entry is dropped by the close listener, not here: between close() and the close
            // event the socket is genuinely CLOSING, and forgetting it now would report CLOSED.
            try {
                // Only 1000 and 3000-4999 are legal from script; anything else throws. Passing
                // nothing means 1005 "no status", which is the honest default for "just close".
                if (code) socket.close(code, reason ?? undefined);
                else socket.close();
            } catch {
                // An illegal code, or a socket already closing - neither is actionable here.
                try { socket.close(); } catch { /* already closed */ }
            }
        },

        disposeAll() {
            for (const id of Object.keys(_sockets)) butil.webSocket.close(id, null, null);
        }
    };
}(BitButil));
