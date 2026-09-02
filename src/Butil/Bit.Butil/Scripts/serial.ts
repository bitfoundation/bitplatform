var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // SerialPort objects carry the user's grant and stay on this side of the boundary; .NET works
    // through the Guid it minted for each one. See bluetooth.ts for the same shape.
    const _ports: { [id: string]: any } = {};
    // At most one read loop per port: the stream lock is exclusive, so a second reader would throw.
    const _readers: { [id: string]: { reader: any, subscriptionId: string, stopping: boolean } } = {};
    const _connectionListeners: { [id: string]: { connect: EventListener, disconnect: EventListener } } = {};

    // Handle ids are minted here rather than by .NET: getPorts() and the connect/disconnect events
    // both surface ports .NET has never seen. See bluetooth.ts.
    let _sequence = 0;
    function nextId() { return `sp${++_sequence}`; }

    butil.serial = {
        isSupported() { return !!(navigator as any).serial; },
        requestPort,
        getPorts,
        forget,
        release,
        open,
        close,
        isOpen,
        getInfo,
        write,
        writeText,
        startReading,
        stopReading,
        getSignals,
        setSignals,
        subscribeConnection,
        unsubscribeConnection
    };

    function serial() { return (navigator as any).serial; }

    function describe(id: string, port: any) {
        const info = typeof port.getInfo === 'function' ? (port.getInfo() ?? {}) : {};
        return {
            id,
            usbVendorId: info.usbVendorId ?? null,
            usbProductId: info.usbProductId ?? null,
            // readable/writable are non-null exactly while the port is open, which is the only
            // "is it open" signal the API gives.
            open: !!(port.readable || port.writable)
        };
    }

    async function requestPort(filters: any[]) {
        const api = serial();
        if (!api) return null;

        const requested = (filters ?? []).map((filter: any) => {
            const out: any = {};
            if (filter.usbVendorId != null) out.usbVendorId = filter.usbVendorId;
            if (filter.usbProductId != null) out.usbProductId = filter.usbProductId;
            return out;
        }).filter((filter: any) => Object.keys(filter).length > 0);

        let port: any;
        try {
            port = requested.length ? await api.requestPort({ filters: requested }) : await api.requestPort();
        } catch (e: any) {
            // NotFoundError is what dismissing the chooser looks like - "no port", not a failure.
            // Anything else is a mistake in the calling code and has to reach it.
            if (e?.name === 'NotFoundError') return null;
            throw e;
        }

        const id = nextId();
        _ports[id] = port;
        return describe(id, port);
    }

    async function getPorts() {
        const api = serial();
        if (!api) return [];
        try {
            const ports = await api.getPorts();
            return ports.map((port: any) => {
                const id = nextId();
                _ports[id] = port;
                return describe(id, port);
            });
        } catch { return []; }
    }

    async function forget(id: string) {
        const port = _ports[id];
        if (!port || typeof port.forget !== 'function') return false;
        try { await port.forget(); release(id); return true; } catch { return false; }
    }

    function release(id: string) {
        const port = _ports[id];
        delete _ports[id];
        if (!port) return;
        // Fire-and-forget: disposal must not wait on a port whose cable has already been pulled.
        stopReading(id);
        try { port.close(); } catch { /* not open, or already gone */ }
    }

    async function open(id: string, options: any) {
        const port = _ports[id];
        if (!port) return false;
        if (port.readable || port.writable) return true;

        await port.open({
            baudRate: options?.baudRate ?? 9600,
            dataBits: options?.dataBits ?? 8,
            stopBits: options?.stopBits ?? 1,
            parity: options?.parity ?? 'none',
            bufferSize: options?.bufferSize ?? 255,
            flowControl: options?.flowControl ?? 'none'
        });
        return true;
    }

    async function close(id: string) {
        const port = _ports[id];
        if (!port) return;
        // The read loop holds the readable stream locked; close() throws until it lets go.
        await stopReading(id);
        try { await port.close(); } catch { /* not open, or already gone */ }
    }

    function isOpen(id: string) {
        const port = _ports[id];
        return !!(port && (port.readable || port.writable));
    }

    function getInfo(id: string) {
        const port = _ports[id];
        return port ? describe(id, port) : null;
    }

    async function writeBytes(id: string, data: Uint8Array) {
        const port = _ports[id];
        if (!port?.writable) return false;
        const writer = port.writable.getWriter();
        try { await writer.write(data); return true; }
        finally { writer.releaseLock(); }
    }

    function write(id: string, data: Uint8Array) { return writeBytes(id, data); }

    function writeText(id: string, text: string) {
        return writeBytes(id, new TextEncoder().encode(text));
    }

    // One chunk per dispatch, exactly as the device framed it - the API gives no message
    // boundaries, so re-assembling is the caller's decision and is left to .NET.
    function startReading(subscriptionId: string, dotNetRef: any, id: string) {
        const port = _ports[id];
        if (!port?.readable || _readers[id]) return false;

        const reader = port.readable.getReader();
        const entry = { reader, subscriptionId, stopping: false };
        _readers[id] = entry;

        (async () => {
            try {
                while (true) {
                    const { value, done } = await reader.read();
                    if (done) break;
                    if (value && value.length) butil.utils.dispatch(dotNetRef, 'InvokeSerialData', subscriptionId, value);
                }
            } catch (e: any) {
                // A cancel() during teardown rejects the pending read - not an error worth reporting.
                if (!entry.stopping) butil.utils.dispatch(dotNetRef, 'InvokeSerialError', subscriptionId, e?.message ?? String(e));
            } finally {
                try { reader.releaseLock(); } catch { /* stream already errored */ }
                if (_readers[id] === entry) delete _readers[id];
            }
        })();

        return true;
    }

    async function stopReading(id: string) {
        const entry = _readers[id];
        if (!entry) return;
        entry.stopping = true;
        delete _readers[id];
        try { await entry.reader.cancel(); } catch { /* already cancelled */ }
        try { entry.reader.releaseLock(); } catch { /* released by the loop */ }
    }

    async function getSignals(id: string) {
        const port = _ports[id];
        if (!port || typeof port.getSignals !== 'function') return null;
        try {
            const signals = await port.getSignals();
            return {
                clearToSend: !!signals.clearToSend,
                dataCarrierDetect: !!signals.dataCarrierDetect,
                dataSetReady: !!signals.dataSetReady,
                ringIndicator: !!signals.ringIndicator
            };
        } catch { return null; }
    }

    async function setSignals(id: string, dataTerminalReady: boolean | null, requestToSend: boolean | null, brk: boolean | null) {
        const port = _ports[id];
        if (!port || typeof port.setSignals !== 'function') return false;
        const signals: any = {};
        // Omitted signals keep their current value; sending null would clear them.
        if (dataTerminalReady != null) signals.dataTerminalReady = dataTerminalReady;
        if (requestToSend != null) signals.requestToSend = requestToSend;
        if (brk != null) signals.break = brk;
        try { await port.setSignals(signals); return true; } catch { return false; }
    }

    // As in usb.ts: the event carries a port .NET has no handle for, so one is minted here.
    function subscribeConnection(subscriptionId: string, dotNetRef: any) {
        const api = serial();
        if (!api) return false;

        const relay = (method: string) => ((event: any) => {
            const port = event.target ?? event.port;
            const id = nextId();
            _ports[id] = port;
            butil.utils.dispatch(dotNetRef, method, subscriptionId, describe(id, port));
        }) as EventListener;

        const connect = relay('InvokeSerialConnected');
        const disconnect = relay('InvokeSerialDisconnected');
        api.addEventListener('connect', connect);
        api.addEventListener('disconnect', disconnect);
        _connectionListeners[subscriptionId] = { connect, disconnect };
        return true;
    }

    function unsubscribeConnection(subscriptionId: string) {
        const entry = _connectionListeners[subscriptionId];
        if (!entry) return;
        delete _connectionListeners[subscriptionId];
        const api = serial();
        if (!api) return;
        api.removeEventListener('connect', entry.connect);
        api.removeEventListener('disconnect', entry.disconnect);
    }
}(BitButil));
