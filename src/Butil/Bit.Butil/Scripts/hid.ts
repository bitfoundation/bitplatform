var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // HIDDevice objects carry the user's grant and stay on this side of the boundary; .NET works
    // through the Guid it minted for each one. See bluetooth.ts for the same shape.
    const _devices: { [id: string]: any } = {};
    const _inputListeners: { [id: string]: { device: any, handler: EventListener } } = {};
    const _connectionListeners: { [id: string]: { connect: EventListener, disconnect: EventListener } } = {};

    // Handle ids are minted here rather than by .NET, through the registry every device module
    // shares: the browser surfaces a device .NET has never seen, so there is nothing for it to key on
    // until the info comes back. Every entry point that hands one out hands out its id with it.
    const _registry = butil.utils.handleRegistry('hid', _devices);
    const idOf = _registry.idOf;


    butil.hid = {
        isSupported() { return !!(navigator as any).hid; },
        requestDevice,
        getDevices,
        forget,
        release,
        open,
        close,
        isOpened,
        getInfo,
        sendReport,
        sendFeatureReport,
        receiveFeatureReport,
        subscribeInputReports,
        unsubscribeInputReports,
        subscribeConnection,
        unsubscribeConnection
    };

    function hid() { return (navigator as any).hid; }

    function reports(list: any[]) {
        return (list ?? []).map((report: any) => ({
            reportId: report.reportId,
            // Only the item count is carried across: a full HID item descriptor is a deep tree
            // that nothing on the .NET side can act on without the raw report bytes anyway.
            itemCount: (report.items ?? []).length
        }));
    }

    function describe(id: string, device: any) {
        return {
            id,
            vendorId: device.vendorId,
            productId: device.productId,
            productName: device.productName ?? null,
            opened: !!device.opened,
            collections: (device.collections ?? []).map((collection: any) => ({
                usagePage: collection.usagePage,
                usage: collection.usage,
                inputReports: reports(collection.inputReports),
                outputReports: reports(collection.outputReports),
                featureReports: reports(collection.featureReports)
            }))
        };
    }

    async function requestDevice(filters: any[]) {
        const api = hid();
        if (!api) return [];

        const requested = (filters ?? []).map((filter: any) => {
            const out: any = {};
            if (filter.vendorId != null) out.vendorId = filter.vendorId;
            if (filter.productId != null) out.productId = filter.productId;
            if (filter.usagePage != null) out.usagePage = filter.usagePage;
            if (filter.usage != null) out.usage = filter.usage;
            return out;
        }).filter((filter: any) => Object.keys(filter).length > 0);

        // requestDevice returns an array - the chooser can hand back more than one device when the
        // request allowed it - so this returns a list where the other buses return a single device.
        const devices = await api.requestDevice({ filters: requested });
        return devices.map((device: any) => describe(idOf(device), device));
    }

    async function getDevices() {
        const api = hid();
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
        const device = _registry.remove(id);
        if (!device) return;

        for (const key of Object.keys(_inputListeners)) {
            if (_inputListeners[key].device === device) unsubscribeInputReports(key);
        }
        // Fire-and-forget: disposal must not wait on a device that has already been unplugged.
        // close() rejects asynchronously, so the catch has to be on the promise as well - a bare
        // try/catch here would leave an unhandled rejection behind.
        try { if (device.opened) void device.close()?.catch(() => { /* already closed or gone */ }); }
        catch { /* already closed or gone */ }
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

    async function sendReport(id: string, reportId: number, data: Uint8Array) {
        const device = _devices[id];
        if (!device) return false;
        await device.sendReport(reportId, butil.utils.arrayToBuffer(data));
        return true;
    }

    async function sendFeatureReport(id: string, reportId: number, data: Uint8Array) {
        const device = _devices[id];
        if (!device) return false;
        await device.sendFeatureReport(reportId, butil.utils.arrayToBuffer(data));
        return true;
    }

    async function receiveFeatureReport(id: string, reportId: number) {
        const device = _devices[id];
        if (!device) return null;
        const view: DataView = await device.receiveFeatureReport(reportId);
        return butil.utils.viewToBytes(view);
    }

    function subscribeInputReports(subscriptionId: string, dotNetRef: any, id: string) {
        const device = _devices[id];
        if (!device) return false;

        const handler: EventListener = (event: any) => {
            const view: DataView = event.data;
            butil.utils.dispatch(dotNetRef, 'InvokeHidInputReport', subscriptionId, {
                reportId: event.reportId,
                data: butil.utils.viewToBytes(view)
            });
        };
        device.addEventListener('inputreport', handler);
        _inputListeners[subscriptionId] = { device, handler };
        return true;
    }

    function unsubscribeInputReports(subscriptionId: string) {
        const entry = _inputListeners[subscriptionId];
        if (!entry) return;
        delete _inputListeners[subscriptionId];
        entry.device.removeEventListener('inputreport', entry.handler);
    }

    // As in usb.ts: the event carries a device .NET may have no handle for, so one is minted here -
    // or the handle it already has is reused.
    function subscribeConnection(subscriptionId: string, dotNetRef: any) {
        const api = hid();
        if (!api) return false;

        const relay = (method: string) => ((event: any) => {
            butil.utils.dispatch(dotNetRef, method, subscriptionId, describe(idOf(event.device), event.device));
        }) as EventListener;

        const connect = relay('InvokeHidConnected');
        const disconnect = relay('InvokeHidDisconnected');
        api.addEventListener('connect', connect);
        api.addEventListener('disconnect', disconnect);
        _connectionListeners[subscriptionId] = { connect, disconnect };
        return true;
    }

    function unsubscribeConnection(subscriptionId: string) {
        const entry = _connectionListeners[subscriptionId];
        if (!entry) return;
        delete _connectionListeners[subscriptionId];
        const api = hid();
        if (!api) return;
        api.removeEventListener('connect', entry.connect);
        api.removeEventListener('disconnect', entry.disconnect);
    }
}(BitButil));
