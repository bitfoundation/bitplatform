var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One MIDIAccess per page: it is the permission grant, and re-requesting it with the same
    // options resolves to an equivalent object, so caching it keeps port ids stable.
    let _access: any = null;
    // The options the cached access was granted under - asking again with the same pair reuses it.
    let _accessSysex = false;
    let _accessSoftware = false;
    // MIDI port ids are strings the browser assigns and they stay valid for the life of the
    // access object, so - unlike the other bus APIs - .NET can address ports by their own id
    // and nothing has to be minted here.
    const _messageListeners: { [id: string]: { inputId: string | null, ports: any[], handler: EventListener } } = {};
    const _stateListeners: { [id: string]: EventListener } = {};

    // MIDI has no per-port 'added' event, so the access object's statechange is the only hook for
    // binding a subscription to a controller plugged in after it was made. One module-owned listener
    // covers every message subscription; it stays attached while any of them exists.
    let _reattachHandler: EventListener | null = null;

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
        map?.forEach((port: any) => out.push(describePort(port)));
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

        // Re-requesting with the same options would resolve to an equivalent object, but a
        // different one - and every listener in _messageListeners/_stateListeners is attached to
        // the ports of the old one. Hand the cached access back instead.
        if (_access && _accessSysex === sysex && _accessSoftware === software) return snapshot();

        // Different options mean a different grant, so the old access object - and every port
        // listener attached through it - is replaced. The subscriptions themselves move across:
        // dropping them would leave .NET holding subscriptions that look live, never fire again, and
        // have nothing on either side able to say so.
        let access: any;
        try {
            access = await (navigator as any).requestMIDIAccess({ sysex, software });
        } catch {
            // A refused prompt - or a policy that blocks MIDI outright - rejects. That is "no access",
            // not a failure worth throwing across the boundary, and it must not disturb the access
            // already granted: every listener in _messageListeners is still attached to its ports.
            return null;
        }

        detachAll();
        _access = access;
        _accessSysex = sysex;
        _accessSoftware = software;
        attachAll();
        return snapshot();
    }

    // Detaches every listener from the outgoing access object without forgetting the subscriptions
    // behind them - attachAll() re-binds those to the incoming one.
    function detachAll() {
        if (!_access) return;

        stopWatchingPorts();
        for (const key of Object.keys(_messageListeners)) detachMessages(key);
        for (const key of Object.keys(_stateListeners)) _access.removeEventListener('statechange', _stateListeners[key]);
    }

    function attachAll() {
        if (!_access) return;

        for (const key of Object.keys(_stateListeners)) _access.addEventListener('statechange', _stateListeners[key]);
        if (Object.keys(_messageListeners).length) startWatchingPorts();
        for (const key of Object.keys(_messageListeners)) attachMessages(key);
    }

    function startWatchingPorts() {
        if (!_access || _reattachHandler) return;

        _reattachHandler = () => { for (const key of Object.keys(_messageListeners)) attachMessages(key); };
        _access.addEventListener('statechange', _reattachHandler);
    }

    function stopWatchingPorts() {
        if (!_reattachHandler) return;

        _access?.removeEventListener('statechange', _reattachHandler);
        _reattachHandler = null;
    }

    // The inputs a subscription covers right now: the one it named, or every input there is.
    function inputsFor(inputId: string | null) {
        if (!_access) return [];
        if (inputId) {
            const port = findPort(_access.inputs, inputId);
            return port ? [port] : [];
        }

        const ports: any[] = [];
        _access.inputs.forEach((port: any) => ports.push(port));
        return ports;
    }

    function detachMessages(subscriptionId: string) {
        const entry = _messageListeners[subscriptionId];
        if (!entry) return;

        for (const port of entry.ports) port.removeEventListener('midimessage', entry.handler);
        entry.ports = [];
    }

    // Binds a subscription to the inputs matching it at this moment. Safe to run again - the previous
    // bindings are dropped first, so a port that is still there ends up listened to exactly once.
    function attachMessages(subscriptionId: string) {
        const entry = _messageListeners[subscriptionId];
        if (!entry) return;

        detachMessages(subscriptionId);
        const ports = inputsFor(entry.inputId);
        for (const port of ports) {
            // The opens are independent, so they are started together rather than awaited one after
            // another - on a machine with a dozen virtual ports that is the difference between one
            // open latency and twelve. A midimessage cannot arrive before its port is open, so the
            // listener can go on straight away.
            try { if (port.connection !== 'open' && typeof port.open === 'function') void port.open()?.catch(() => { /* port went away */ }); }
            catch { /* port went away */ }
            port.addEventListener('midimessage', entry.handler);
        }
        entry.ports = ports;
    }

    // Null until requestAccess() has resolved - reading the port list is not itself a way to get
    // permission, and callers are expected to treat null as "ask first".
    function getPorts() { return snapshot(); }

    // MIDIAccess.inputs/outputs are maplike and keyed by exactly this id, so get() is the lookup -
    // there is no case where scanning the values would find a port that get() misses.
    function findPort(map: any, portId: string) {
        return map?.get(portId) ?? null;
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

    function subscribeMessages(subscriptionId: string, dotNetRef: any, inputId: string | null) {
        if (!_access) return false;
        // A named input has to exist. 'Every input' does not require one to be plugged in yet: the
        // subscription stands, and the statechange watch binds whatever turns up later.
        if (inputId && !findPort(_access.inputs, inputId)) return false;

        const handler: EventListener = (event: any) => {
            butil.utils.dispatch(dotNetRef, 'InvokeMidiMessage', subscriptionId, {
                portId: (event.target as any)?.id ?? '',
                data: event.data ? new Uint8Array(event.data) : new Uint8Array(0),
                timeStamp: event.timeStamp ?? 0
            });
        };

        // One entry per subscription regardless of how many ports it covers: the ports it is bound
        // to are recorded together, so unsubscribing detaches all of them and re-binding replaces
        // the whole set.
        _messageListeners[subscriptionId] = { inputId: inputId ?? null, ports: [], handler };
        startWatchingPorts();
        attachMessages(subscriptionId);
        return true;
    }

    function unsubscribeMessages(subscriptionId: string) {
        const entry = _messageListeners[subscriptionId];
        if (!entry) return;

        detachMessages(subscriptionId);
        delete _messageListeners[subscriptionId];
        if (Object.keys(_messageListeners).length === 0) stopWatchingPorts();
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
