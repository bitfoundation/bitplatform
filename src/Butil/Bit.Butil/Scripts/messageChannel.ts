var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // closed is tracked here because the port itself will not say: postMessage on a closed port is
    // a silent no-op rather than a throw, so without this the post functions report success for a
    // message nothing will ever receive.
    interface PortEntry { port: MessagePort; listener?: (e: MessageEvent) => void; started: boolean; closed: boolean; }

    const _ports: { [id: string]: PortEntry } = {};
    // channel id -> its two port ids, so releasing a channel releases both halves.
    const _channels: { [id: string]: [string, string] } = {};

    function track(id: string, port: MessagePort) {
        _ports[id] = { port, started: false, closed: false };
        return _ports[id];
    }

    butil.messageChannel = {
        isSupported() { return typeof (window as any).MessageChannel === 'function'; },

        create(channelId: string, firstId: string, secondId: string) {
            if (typeof (window as any).MessageChannel !== 'function') return false;
            const channel = new MessageChannel();
            track(firstId, channel.port1);
            track(secondId, channel.port2);
            _channels[channelId] = [firstId, secondId];
            return true;
        },

        // Attaches the .NET listener. A port queues everything it receives until it is started, and
        // addEventListener (unlike the onmessage setter) does not start it - which is the useful
        // behaviour: a port can be handed over, listened to, and only then opened.
        listen(dotNetRef: any, portId: string) {
            const entry = _ports[portId];
            if (!entry) return false;
            if (entry.listener) entry.port.removeEventListener('message', entry.listener);

            entry.listener = (e: MessageEvent) =>
                butil.utils.dispatch(dotNetRef, 'InvokePortMessage', portId, ...butil.utils.encodeMessage(e.data));

            entry.port.addEventListener('message', entry.listener);
            return true;
        },

        start(portId: string) {
            const entry = _ports[portId];
            if (!entry || entry.started) return;
            entry.started = true;
            entry.port.start();
        },

        postJson(portId: string, json: string | null) {
            const entry = _ports[portId];
            if (!entry || entry.closed) return false;
            try { entry.port.postMessage(json === null ? null : JSON.parse(json)); return true; } catch { return false; }
        },

        postBytes(portId: string, bytes: Uint8Array, transfer: boolean) {
            const entry = _ports[portId];
            if (!entry || entry.closed) return false;
            const buffer = butil.utils.arrayToBuffer(bytes);
            try {
                // Transferring hands the buffer over instead of copying it - the sender's copy is
                // detached afterwards, which is the point and also the hazard.
                entry.port.postMessage(buffer, transfer ? [buffer] : []);
                return true;
            } catch { return false; }
        },

        // Sends a message that carries other ports with it. Ports are the one thing that has to be
        // transferred rather than copied: a port belongs to exactly one context at a time.
        postWithPorts(portId: string, json: string | null, transferredPortIds: string[]) {
            const entry = _ports[portId];
            if (!entry || entry.closed) return false;

            const ports = (transferredPortIds ?? [])
                .map(id => (_ports[id]?.closed ? undefined : _ports[id]?.port))
                .filter(Boolean) as MessagePort[];
            if (ports.length !== (transferredPortIds ?? []).length) return false;

            try {
                entry.port.postMessage(json === null ? null : JSON.parse(json), ports);
                // The ports now live in the receiver. Anything still holding them here would be
                // talking to a detached object, so drop them from the registry.
                for (const id of transferredPortIds) butil.messageChannel.release(id);
                return true;
            } catch { return false; }
        },

        close(portId: string) {
            const entry = _ports[portId];
            if (!entry) return;
            entry.closed = true;
            try { entry.port.close(); } catch { /* already closed */ }
        },

        release(portId: string) {
            const entry = _ports[portId];
            if (!entry) return;
            if (entry.listener) entry.port.removeEventListener('message', entry.listener);
            delete _ports[portId];
        },

        releaseChannel(channelId: string) {
            const ids = _channels[channelId];
            if (!ids) return;
            delete _channels[channelId];
            for (const id of ids) {
                butil.messageChannel.close(id);
                butil.messageChannel.release(id);
            }
        },

        disposeAll() {
            for (const id of Object.keys(_channels)) butil.messageChannel.releaseChannel(id);
            for (const id of Object.keys(_ports)) {
                butil.messageChannel.close(id);
                butil.messageChannel.release(id);
            }
        },

        // For other modules (worker): the live MessagePort behind an id, and a way to adopt a port
        // that was created somewhere else - a SharedWorker's port, for instance.
        portOf(id: string) { return _ports[id]?.port; },
        adopt(id: string, port: MessagePort) { track(id, port); }
    };
}(BitButil));
