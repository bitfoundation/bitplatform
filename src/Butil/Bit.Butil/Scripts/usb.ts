var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // USBDevice objects carry the user's grant and stay on this side of the boundary; .NET works
    // through the Guid it minted for each one. See bluetooth.ts for the same shape.
    const _devices: { [id: string]: any } = {};
    const _connectionListeners: { [id: string]: { connect: EventListener, disconnect: EventListener } } = {};

    // Handle ids are minted here rather than by .NET: getDevices() and the connect/disconnect
    // events both surface devices .NET has never seen. See bluetooth.ts.
    let _sequence = 0;
    function nextId() { return `usb${++_sequence}`; }

    // The USB API hands back the same USBDevice object for a given device on every call, so
    // minting a fresh id each time would pile up registry entries for one device - and leave an
    // open handle's id pointing at a device the caller thinks it already released.
    function idOf(device: any) {
        for (const key of Object.keys(_devices)) {
            if (_devices[key] === device) return key;
        }

        const id = nextId();
        _devices[id] = device;
        return id;
    }

    butil.usb = {
        isSupported() { return !!(navigator as any).usb; },
        requestDevice,
        getDevices,
        forget,
        release,
        open,
        close,
        isOpened,
        getInfo,
        selectConfiguration,
        claimInterface,
        releaseInterface,
        selectAlternateInterface,
        controlTransferIn,
        controlTransferOut,
        transferIn,
        transferOut,
        clearHalt,
        reset,
        subscribeConnection,
        unsubscribeConnection
    };

    function usb() { return (navigator as any).usb; }

    function endpoints(alternate: any) {
        return (alternate.endpoints ?? []).map((endpoint: any) => ({
            endpointNumber: endpoint.endpointNumber,
            direction: endpoint.direction,
            type: endpoint.type,
            packetSize: endpoint.packetSize
        }));
    }

    function describe(id: string, device: any) {
        return {
            id,
            vendorId: device.vendorId,
            productId: device.productId,
            deviceClass: device.deviceClass,
            deviceSubclass: device.deviceSubclass,
            deviceProtocol: device.deviceProtocol,
            manufacturerName: device.manufacturerName ?? null,
            productName: device.productName ?? null,
            serialNumber: device.serialNumber ?? null,
            opened: !!device.opened,
            configurationValue: device.configuration?.configurationValue ?? null,
            configurations: (device.configurations ?? []).map((configuration: any) => ({
                configurationValue: configuration.configurationValue,
                configurationName: configuration.configurationName ?? null,
                interfaces: (configuration.interfaces ?? []).map((usbInterface: any) => ({
                    interfaceNumber: usbInterface.interfaceNumber,
                    claimed: !!usbInterface.claimed,
                    alternates: (usbInterface.alternates ?? []).map((alternate: any) => ({
                        alternateSetting: alternate.alternateSetting,
                        interfaceClass: alternate.interfaceClass,
                        interfaceSubclass: alternate.interfaceSubclass,
                        interfaceProtocol: alternate.interfaceProtocol,
                        interfaceName: alternate.interfaceName ?? null,
                        endpoints: endpoints(alternate)
                    }))
                }))
            }))
        };
    }

    async function requestDevice(filters: any[]) {
        const api = usb();
        if (!api) return null;

        // An empty filter object matches everything, which is what the spec wants for "any device";
        // undefined properties in a filter are a TypeError, so they are stripped rather than passed.
        const requested = (filters ?? []).map((filter: any) => {
            const out: any = {};
            if (filter.vendorId != null) out.vendorId = filter.vendorId;
            if (filter.productId != null) out.productId = filter.productId;
            if (filter.classCode != null) out.classCode = filter.classCode;
            if (filter.subclassCode != null) out.subclassCode = filter.subclassCode;
            if (filter.protocolCode != null) out.protocolCode = filter.protocolCode;
            if (filter.serialNumber) out.serialNumber = filter.serialNumber;
            return out;
        });

        let device: any;
        try {
            device = await api.requestDevice({ filters: requested.length ? requested : [{}] });
        } catch (e: any) {
            // NotFoundError is what dismissing the chooser looks like - "no device", not a failure.
            // Anything else is a mistake in the calling code and has to reach it.
            if (e?.name === 'NotFoundError') return null;
            throw e;
        }

        return describe(idOf(device), device);
    }

    async function getDevices() {
        const api = usb();
        if (!api) return [];
        try {
            const devices = await api.getDevices();
            return devices.map((device: any) => describe(idOf(device), device));
        } catch { return []; }
    }

    async function forget(id: string) {
        const device = _devices[id];
        if (!device || typeof device.forget !== 'function') return false;
        try { await device.forget(); release(id); return true; } catch { return false; }
    }

    function release(id: string) {
        const device = _devices[id];
        delete _devices[id];
        if (!device) return;
        // Fire-and-forget: disposal must not wait on a device that has already been unplugged.
        try { if (device.opened) device.close(); } catch { /* already closed or gone */ }
    }

    async function open(id: string) {
        const device = _devices[id];
        if (!device) return false;
        if (device.opened) return true;
        await device.open();
        return true;
    }

    async function close(id: string) {
        const device = _devices[id];
        if (!device?.opened) return;
        try { await device.close(); } catch { /* already closed or gone */ }
    }

    function isOpened(id: string) { return !!_devices[id]?.opened; }

    function getInfo(id: string) {
        const device = _devices[id];
        return device ? describe(id, device) : null;
    }

    async function selectConfiguration(id: string, configurationValue: number) {
        const device = _devices[id];
        if (!device) return false;
        await device.selectConfiguration(configurationValue);
        return true;
    }

    async function claimInterface(id: string, interfaceNumber: number) {
        const device = _devices[id];
        if (!device) return false;
        await device.claimInterface(interfaceNumber);
        return true;
    }

    async function releaseInterface(id: string, interfaceNumber: number) {
        const device = _devices[id];
        if (!device) return false;
        try { await device.releaseInterface(interfaceNumber); return true; } catch { return false; }
    }

    async function selectAlternateInterface(id: string, interfaceNumber: number, alternateSetting: number) {
        const device = _devices[id];
        if (!device) return false;
        await device.selectAlternateInterface(interfaceNumber, alternateSetting);
        return true;
    }

    function setup(parameters: any) {
        return {
            requestType: parameters.requestType,
            recipient: parameters.recipient,
            request: parameters.request,
            value: parameters.value,
            index: parameters.index
        };
    }

    // A transfer result is { status, data } for IN and { status, bytesWritten } for OUT. Both are
    // flattened into one shape so .NET has a single type for every transfer.
    async function controlTransferIn(id: string, parameters: any, length: number) {
        const device = _devices[id];
        if (!device) return null;
        const result = await device.controlTransferIn(setup(parameters), length);
        return {
            status: result.status,
            bytesWritten: 0,
            data: result.data ? new Uint8Array(result.data.buffer, result.data.byteOffset, result.data.byteLength) : null
        };
    }

    async function controlTransferOut(id: string, parameters: any, data: Uint8Array | null) {
        const device = _devices[id];
        if (!device) return null;
        const result = data && data.length
            ? await device.controlTransferOut(setup(parameters), butil.utils.arrayToBuffer(data))
            : await device.controlTransferOut(setup(parameters));
        return { status: result.status, bytesWritten: result.bytesWritten ?? 0, data: null };
    }

    async function transferIn(id: string, endpointNumber: number, length: number) {
        const device = _devices[id];
        if (!device) return null;
        const result = await device.transferIn(endpointNumber, length);
        return {
            status: result.status,
            bytesWritten: 0,
            data: result.data ? new Uint8Array(result.data.buffer, result.data.byteOffset, result.data.byteLength) : null
        };
    }

    async function transferOut(id: string, endpointNumber: number, data: Uint8Array) {
        const device = _devices[id];
        if (!device) return null;
        const result = await device.transferOut(endpointNumber, butil.utils.arrayToBuffer(data));
        return { status: result.status, bytesWritten: result.bytesWritten ?? 0, data: null };
    }

    async function clearHalt(id: string, direction: string, endpointNumber: number) {
        const device = _devices[id];
        if (!device) return false;
        try { await device.clearHalt(direction, endpointNumber); return true; } catch { return false; }
    }

    async function reset(id: string) {
        const device = _devices[id];
        if (!device) return false;
        try { await device.reset(); return true; } catch { return false; }
    }

    // The connect/disconnect events carry a USBDevice that .NET may have no handle for yet, so each
    // one is registered (or matched to the handle it already has) before being dispatched - the
    // caller can act on it straight away without a second requestDevice().
    function subscribeConnection(subscriptionId: string, dotNetRef: any) {
        const api = usb();
        if (!api) return false;

        const relay = (method: string) => ((event: any) => {
            butil.utils.dispatch(dotNetRef, method, subscriptionId, describe(idOf(event.device), event.device));
        }) as EventListener;

        const connect = relay('InvokeUsbConnected');
        const disconnect = relay('InvokeUsbDisconnected');
        api.addEventListener('connect', connect);
        api.addEventListener('disconnect', disconnect);
        _connectionListeners[subscriptionId] = { connect, disconnect };
        return true;
    }

    function unsubscribeConnection(subscriptionId: string) {
        const entry = _connectionListeners[subscriptionId];
        if (!entry) return;
        delete _connectionListeners[subscriptionId];
        const api = usb();
        if (!api) return;
        api.removeEventListener('connect', entry.connect);
        api.removeEventListener('disconnect', entry.disconnect);
    }
}(BitButil));
