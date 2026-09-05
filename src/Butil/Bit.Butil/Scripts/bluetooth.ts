var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // A BluetoothDevice object *is* the permission grant the user made in the chooser, so it never
    // crosses the interop boundary - .NET holds the Guid it minted for the device and every call
    // comes back through this registry. Nothing here is reachable without a prior requestDevice()
    // or getDevices(), which is what keeps the grant scoped.
    const _devices: { [id: string]: any } = {};

    // 'characteristicvaluechanged' and 'gattserverdisconnected' listeners, keyed by the
    // subscription id .NET minted for each.
    const _notifications: { [id: string]: { device: any, characteristic: any, handler: EventListener } } = {};
    const _disconnects: { [id: string]: { device: any, handler: EventListener } } = {};

    // Handle ids are minted here rather than by .NET: getDevices() surfaces devices .NET has never
    // seen, so there is nothing for it to key them on until the info comes back. Every entry point
    // that hands a device out therefore hands out its id with it.
    let _sequence = 0;
    function nextId() { return `bt${++_sequence}`; }

    butil.bluetooth = {
        isSupported() { return !!(navigator as any).bluetooth; },
        getAvailability,
        requestDevice,
        getDevices,
        forget,
        release,
        connect,
        disconnect,
        isConnected,
        getPrimaryServices,
        getCharacteristics,
        readValue,
        writeValue,
        startNotifications,
        stopNotifications,
        subscribeDisconnect,
        unsubscribeDisconnect
    };

    function bluetooth() { return (navigator as any).bluetooth; }

    function describe(id: string, device: any) {
        return {
            id,
            deviceId: device.id ?? '',
            name: device.name ?? null,
            connected: !!device.gatt?.connected
        };
    }

    // A 16-bit alias ("0x180d"), a name ("heart_rate") and a full UUID are all legal service /
    // characteristic identifiers, and all three arrive as strings from .NET. Hex digits are passed
    // through as a number so the browser resolves the alias itself.
    function uuid(value: string): any {
        if (typeof value !== 'string') return value;
        const trimmed = value.trim();
        if (/^0x[0-9a-f]+$/i.test(trimmed)) return parseInt(trimmed, 16);
        return trimmed;
    }

    function bytes(view: DataView) {
        return new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
    }

    async function getAvailability() {
        const bt = bluetooth();
        if (!bt || typeof bt.getAvailability !== 'function') return false;
        try { return await bt.getAvailability(); } catch { return false; }
    }

    async function requestDevice(options: any) {
        const bt = bluetooth();
        if (!bt) return null;

        // acceptAllDevices and filters are mutually exclusive - passing both is a TypeError - and
        // acceptAllDevices is documented to win, so the filters aren't even looked at then.
        const acceptAll = options?.acceptAllDevices === true;
        const supplied = acceptAll ? [] : (options?.filters ?? []);

        const filters = supplied.map((filter: any) => {
            const out: any = {};
            if (filter.services?.length) out.services = filter.services.map(uuid);
            if (filter.name) out.name = filter.name;
            if (filter.namePrefix) out.namePrefix = filter.namePrefix;
            return out;
        }).filter((filter: any) => Object.keys(filter).length > 0);

        // Filters that were asked for but carry no criterion are a mistake in the caller's options.
        // Silently widening that to "every device nearby" is the opposite of what they asked for.
        if (supplied.length && filters.length === 0)
            throw new TypeError('Every supplied Bluetooth filter is empty - give each one a service, name or namePrefix, or set AcceptAllDevices.');

        const request: any = {};
        if (filters.length) request.filters = filters;
        else request.acceptAllDevices = true;
        if (options?.optionalServices?.length) request.optionalServices = options.optionalServices.map(uuid);

        let device: any;
        try {
            device = await bt.requestDevice(request);
        } catch (e: any) {
            // NotFoundError is what dismissing the chooser looks like - "no device", not a failure.
            // Everything else (a SecurityError for a service the request never named, a TypeError
            // for a malformed filter) is a mistake in the calling code and has to reach it.
            if (e?.name === 'NotFoundError') return null;
            throw e;
        }

        const id = nextId();
        _devices[id] = device;
        return describe(id, device);
    }

    // Devices the user has already permitted, each under a fresh handle id.
    async function getDevices() {
        const bt = bluetooth();
        if (!bt || typeof bt.getDevices !== 'function') return [];
        try {
            const devices = await bt.getDevices();
            return devices.map((device: any) => {
                const id = nextId();
                _devices[id] = device;
                return describe(id, device);
            });
        } catch { return []; }
    }

    async function forget(id: string) {
        const device = _devices[id];
        if (!device || typeof device.forget !== 'function') return false;
        try { await device.forget(); release(id); return true; } catch { return false; }
    }

    // Drops the registry entry and every listener attached through it. Called on handle disposal;
    // the device itself stays permitted - forget() is what revokes the grant.
    function release(id: string) {
        const device = _devices[id];
        delete _devices[id];
        if (!device) return;

        for (const key of Object.keys(_disconnects)) {
            if (_disconnects[key].device === device) unsubscribeDisconnect(key);
        }
        // Notifications are attached through the same handle, so they go with it. Fire-and-forget:
        // stopNotifications drops the entry and the listener synchronously, and only the device
        // round-trip is left to settle on its own.
        for (const key of Object.keys(_notifications)) {
            if (_notifications[key].device === device) void stopNotifications(key).catch(() => { /* device already gone */ });
        }
        try { device.gatt?.disconnect(); } catch { /* already gone */ }
    }

    async function connect(id: string) {
        const device = _devices[id];
        if (!device?.gatt) return false;
        try { await device.gatt.connect(); return true; } catch { return false; }
    }

    function disconnect(id: string) {
        try { _devices[id]?.gatt?.disconnect(); } catch { /* already gone */ }
    }

    function isConnected(id: string) { return !!_devices[id]?.gatt?.connected; }

    async function server(id: string) {
        const device = _devices[id];
        if (!device?.gatt) return null;
        // A GATT server drops the connection between operations more often than not, so every
        // operation re-connects rather than assuming an earlier connect() still holds.
        if (!device.gatt.connected) await device.gatt.connect();
        return device.gatt;
    }

    async function getPrimaryServices(id: string) {
        const gatt = await server(id);
        if (!gatt) return [];
        const services = await gatt.getPrimaryServices();
        return services.map((service: any) => ({ uuid: service.uuid, isPrimary: !!service.isPrimary }));
    }

    async function getCharacteristics(id: string, serviceUuid: string) {
        const gatt = await server(id);
        if (!gatt) return [];
        const service = await gatt.getPrimaryService(uuid(serviceUuid));
        const characteristics = await service.getCharacteristics();
        return characteristics.map((characteristic: any) => ({
            uuid: characteristic.uuid,
            serviceUuid: service.uuid,
            broadcast: !!characteristic.properties?.broadcast,
            read: !!characteristic.properties?.read,
            writeWithoutResponse: !!characteristic.properties?.writeWithoutResponse,
            write: !!characteristic.properties?.write,
            notify: !!characteristic.properties?.notify,
            indicate: !!characteristic.properties?.indicate
        }));
    }

    async function characteristic(id: string, serviceUuid: string, characteristicUuid: string) {
        const gatt = await server(id);
        if (!gatt) return null;
        const service = await gatt.getPrimaryService(uuid(serviceUuid));
        return await service.getCharacteristic(uuid(characteristicUuid));
    }

    async function readValue(id: string, serviceUuid: string, characteristicUuid: string) {
        const target = await characteristic(id, serviceUuid, characteristicUuid);
        if (!target) return null;
        const value = await target.readValue();
        return bytes(value);
    }

    async function writeValue(id: string, serviceUuid: string, characteristicUuid: string, data: Uint8Array, withResponse: boolean) {
        const target = await characteristic(id, serviceUuid, characteristicUuid);
        if (!target) return false;
        const buffer = butil.utils.arrayToBuffer(data);
        // writeValueWithResponse/WithoutResponse are the current spec; writeValue is the legacy
        // name still shipping in older Chromium, and the only one some builds expose.
        if (withResponse && typeof target.writeValueWithResponse === 'function') await target.writeValueWithResponse(buffer);
        else if (!withResponse && typeof target.writeValueWithoutResponse === 'function') await target.writeValueWithoutResponse(buffer);
        else await target.writeValue(buffer);
        return true;
    }

    async function startNotifications(subscriptionId: string, dotNetRef: any, id: string, serviceUuid: string, characteristicUuid: string) {
        const target = await characteristic(id, serviceUuid, characteristicUuid);
        if (!target) return false;

        const handler: EventListener = event => {
            const value = (event.target as any).value as DataView;
            butil.utils.dispatch(dotNetRef, 'InvokeBluetoothValueChanged', subscriptionId, bytes(value));
        };
        // Attached before starting so the first notification can't slip through, and taken back off
        // if the device refuses - a rejected start would otherwise leave a listener nothing tracks
        // and nothing can remove.
        target.addEventListener('characteristicvaluechanged', handler);
        try {
            await target.startNotifications();
        } catch (e) {
            target.removeEventListener('characteristicvaluechanged', handler);
            throw e;
        }
        _notifications[subscriptionId] = { device: _devices[id], characteristic: target, handler };
        return true;
    }

    async function stopNotifications(subscriptionId: string) {
        const entry = _notifications[subscriptionId];
        if (!entry) return;
        delete _notifications[subscriptionId];
        entry.characteristic.removeEventListener('characteristicvaluechanged', entry.handler);
        try { await entry.characteristic.stopNotifications(); } catch { /* device already gone */ }
    }

    function subscribeDisconnect(subscriptionId: string, dotNetRef: any, id: string) {
        const device = _devices[id];
        if (!device) return false;
        const handler: EventListener = () => butil.utils.dispatch(dotNetRef, 'InvokeBluetoothDisconnected', subscriptionId);
        device.addEventListener('gattserverdisconnected', handler);
        _disconnects[subscriptionId] = { device, handler };
        return true;
    }

    function unsubscribeDisconnect(subscriptionId: string) {
        const entry = _disconnects[subscriptionId];
        if (!entry) return;
        delete _disconnects[subscriptionId];
        entry.device.removeEventListener('gattserverdisconnected', entry.handler);
    }
}(BitButil));
