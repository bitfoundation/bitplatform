var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One MIDIAccess per page: it is the permission grant, and re-requesting it with the same
    // options resolves to an equivalent object, so caching it keeps port ids stable.
    let _access: any = null;
    // MIDI port ids are strings the browser assigns and they stay valid for the life of the
    // access object, so - unlike the other bus APIs - .NET can address ports by their own id
    // and nothing has to be minted here.
    const _messageListeners: { [id: string]: { ports: any[], handler: EventListener } } = {};
    const _stateListeners: { [id: string]: EventListener } = {};

    butil.midi = {
        isSupported() { return typeof (navigator as any).requestMIDIAccess === 'function'; },
        requestAccess,
        getPorts,
        send,
        clear,
        subscribeMessages,
        unsubscribeMessages,
        subscribeStateChange,
        unsubscribeStateChange
    };

    function describePort(port: any) {
        return {
            id: port.id,
            name: port.name ?? null,
            manufacturer: port.manufacturer ?? null,
            version: port.version ?? null,
            type: port.type,
            state: port.state,
            connection: port.connection
        };
    }

    function list(map: any) {
        const out: any[] = [];
        // MIDIAccess.inputs/outputs are Maps; forEach is the only iteration ES2019 output can rely on.
        if (map && typeof map.forEach === 'function') map.forEach((port: any) => out.push(describePort(port)));
        return out;
    }

    function snapshot() {
        if (!_access) return null;
        return {
            sysexEnabled: !!_access.sysexEnabled,
            inputs: list(_access.inputs),
            outputs: list(_access.outputs)
        };
    }

    async function requestAccess(sysex: boolean, software: boolean) {
        if (typeof (navigator as any).requestMIDIAccess !== 'function') return null;
        _access = await (navigator as any).requestMIDIAccess({ sysex, software });
        return snapshot();
    }

    // Null until requestAccess() has resolved - reading the port list is not itself a way to get
    // permission, and callers are expected to treat null as "ask first".
    function getPorts() { return snapshot(); }

    function findPort(map: any, portId: string) {
        if (!map) return null;
        if (typeof map.get === 'function') {
            const port = map.get(portId);
            if (port) return port;
        }
        let match: any = null;
        if (typeof map.forEach === 'function') map.forEach((port: any) => { if (port.id === portId) match = port; });
        return match;
    }

    async function send(outputId: string, data: Uint8Array, timestamp: number | null) {
        const output = findPort(_access?.outputs, outputId);
        if (!output) return false;
        try {
            // A closed port throws on send(); open() is idempotent and resolves immediately for
            // one that is already open.
            if (output.connection !== 'open' && typeof output.open === 'function') await output.open();
            // performance.now()-based timestamps only: 0/null means "as soon as possible".
            if (timestamp) output.send(data, timestamp);
            else output.send(data);
            return true;
        } catch { return false; }
    }

    // Drops every queued message that has not been sent yet - the panic button for a stuck note.
    function clear(outputId: string) {
        const output = findPort(_access?.outputs, outputId);
        if (!output || typeof output.clear !== 'function') return false;
        try { output.clear(); return true; } catch { return false; }
    }

    async function subscribeMessages(subscriptionId: string, dotNetRef: any, inputId: string | null) {
        if (!_access) return false;

        const ports: any[] = [];
        if (inputId) {
            const port = findPort(_access.inputs, inputId);
            if (port) ports.push(port);
        } else {
            _access.inputs.forEach((port: any) => ports.push(port));
        }
        if (!ports.length) return false;

        const handler: EventListener = (event: any) => {
            butil.utils.dispatch(dotNetRef, 'InvokeMidiMessage', subscriptionId, {
                portId: (event.target as any)?.id ?? '',
                data: event.data ? new Uint8Array(event.data) : new Uint8Array(0),
                timeStamp: event.timeStamp ?? 0
            });
        };

        for (const port of ports) {
            try { if (port.connection !== 'open' && typeof port.open === 'function') await port.open(); } catch { /* port went away */ }
            port.addEventListener('midimessage', handler);
        }
        // One entry per subscription regardless of how many ports it covers: the ports are
        // recorded together so unsubscribing detaches all of them.
        _messageListeners[subscriptionId] = { ports, handler };
        return true;
    }

    function unsubscribeMessages(subscriptionId: string) {
        const entry = _messageListeners[subscriptionId];
        if (!entry) return;
        delete _messageListeners[subscriptionId];
        for (const port of entry.ports) port.removeEventListener('midimessage', entry.handler);
    }

    function subscribeStateChange(subscriptionId: string, dotNetRef: any) {
        if (!_access) return false;
        const handler: EventListener = (event: any) => {
            const port = event.port;
            butil.utils.dispatch(dotNetRef, 'InvokeMidiStateChange', subscriptionId, port ? describePort(port) : null);
        };
        _access.addEventListener('statechange', handler);
        _stateListeners[subscriptionId] = handler;
        return true;
    }

    function unsubscribeStateChange(subscriptionId: string) {
        const handler = _stateListeners[subscriptionId];
        if (!handler) return;
        delete _stateListeners[subscriptionId];
        _access?.removeEventListener('statechange', handler);
    }
}(BitButil));
