var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // SerialPort objects carry the user's grant and stay on this side of the boundary; .NET works
    // through the Guid it minted for each one. See bluetooth.ts for the same shape.
    const _ports: { [id: string]: any } = {};
    // At most one read loop per port: the stream lock is exclusive, so a second reader would throw.
    const _readers: { [id: string]: { reader: any, subscriptionId: string, stopping: boolean } } = {};
    const _connectionListeners: { [id: string]: { connect: EventListener, disconnect: EventListener } } = {};
    // The line settings each open port was opened with. The API exposes no way to read them back and
    // no way to change them in place, so open() keeps them here to tell a redundant re-open from one
    // that is asking for different settings.
    const _openOptions: { [id: string]: any } = {};

    // Handle ids are minted here rather than by .NET, through the registry every device module
    // shares: the browser surfaces a port .NET has never seen, so there is nothing for it to key on
    // until the info comes back. Every entry point that hands one out hands out its id with it.
    const _registry = butil.utils.handleRegistry('sp', _ports);
    const idOf = _registry.idOf;

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

        return describe(idOf(port), port);
    }

    async function getPorts() {
        const api = serial();
        if (!api) return [];
        try {
            const ports = await api.getPorts();
            return ports.map((port: any) => describe(idOf(port), port));
        } catch { return []; }
    }


    async function forget(id: string) {
        const port = _ports[id];
        if (!port || typeof port.forget !== 'function') return false;
        try { await port.forget(); release(id); return true; } catch { return false; }
    }

    function release(id: string) {
        const port = _registry.remove(id);
        if (!port) return;
        delete _openOptions[id];
        // Fire-and-forget: disposal must not wait on a port whose cable has already been pulled.
        // The read loop still holds the readable stream locked, though, and close() rejects until
        // it lets go - so the close is chained behind stopReading rather than raced with it.
        stopReading(id).then(() => port.close()).catch(() => { /* not open, or already gone */ });
    }

    async function open(id: string, options: any) {
        const port = _ports[id];
        if (!port) return false;

        const settings = {
            baudRate: options?.baudRate ?? 9600,
            dataBits: options?.dataBits ?? 8,
            stopBits: options?.stopBits ?? 1,
            parity: options?.parity ?? 'none',
            bufferSize: options?.bufferSize ?? 255,
            flowControl: options?.flowControl ?? 'none'
        };

        if (port.readable || port.writable) {
            // Re-opening with the settings already in force is the documented no-op. Different
            // settings are a real request, and the API cannot apply them to an open port - so the
            // port is cycled instead of reporting a success it did not deliver. That ends the read
            // loop exactly as close() does, and any subscription over it stops delivering.
            if (sameSettings(_openOptions[id], settings)) return true;
            await close(id);
        }

        await port.open(settings);
        _openOptions[id] = settings;
        return true;
    }

    function sameSettings(a: any, b: any) {
        if (!a) return false;
        return a.baudRate === b.baudRate && a.dataBits === b.dataBits && a.stopBits === b.stopBits
            && a.parity === b.parity && a.bufferSize === b.bufferSize && a.flowControl === b.flowControl;
    }

    async function close(id: string) {
        const port = _ports[id];
        if (!port) return;
        delete _openOptions[id];
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

    // A port carries at most one read loop, but that loop can end on its own - the device closed the
    // stream, or close() was called - and a later subscription can start a new one on the same port.
    // So a stop that names a subscription only stops the loop that subscription started: without the
    // check, disposing a stale subscription would cancel a reader it never began. Passing no
    // subscription id means "whatever is reading this port", which is what close() and release() want.
    async function stopReading(id: string, subscriptionId: string | null = null) {
        const entry = _readers[id];
        if (!entry) return;
        if (subscriptionId && entry.subscriptionId !== subscriptionId) return;
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

    // As in usb.ts: the event carries a port .NET may have no handle for, so one is minted here -
    // or the handle it already has is reused.
    function subscribeConnection(subscriptionId: string, dotNetRef: any) {
        const api = serial();
        if (!api) return false;

        const relay = (method: string) => ((event: any) => {
            const port = event.target ?? event.port;
            butil.utils.dispatch(dotNetRef, method, subscriptionId, describe(idOf(port), port));
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
