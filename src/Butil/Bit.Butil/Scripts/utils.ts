var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer,
        viewToBytes,
        dispatch,
        throttle,
        handleRegistry
    };

    function arrayToBuffer(array: Uint8Array) {
        if (!array) return undefined;
        // Slice covers exactly the [byteOffset, byteOffset + byteLength) range so that
        // a Uint8Array view over a larger buffer doesn't leak extra bytes.
        return array.buffer.slice(array.byteOffset, array.byteOffset + array.byteLength);
    }

    // The inverse of arrayToBuffer, and it exists for the same reason: Web Bluetooth, WebHID and
    // WebUSB all hand their payloads back as a DataView over a larger buffer, so the offset and
    // length have to be carried through or the surrounding bytes leak into .NET.
    function viewToBytes(view: any) {
        if (!view) return new Uint8Array(0);
        if (view instanceof Uint8Array) return view;
        return new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
    }

    // Device and sensor streams fire far faster than a Blazor render can keep up with - 60 Hz is
    // the usual platform default - so a subscription that would otherwise flood the interop channel
    // is rate-limited here, before the cost of a round-trip is paid. A non-positive interval means
    // "every event", and returns the handler untouched rather than a wrapper that always passes.
    function throttle(minInterval: number, send: (e: any) => void) {
        if (!(minInterval > 0)) return send;

        let lastSentAt = 0;
        return (e: any) => {
            const now = performance.now();
            if (now - lastSentAt < minInterval) return;
            lastSentAt = now;
            send(e);
        };
    }

    // The handle registry the device modules share. A browser hands back the same SerialPort,
    // USBDevice, HIDDevice or BluetoothDevice object for a given device on every call, so an id has
    // to be minted once and reused: a fresh one per call would pile up registry entries for one
    // device and leave an open handle's id pointing at a device the caller thinks it released. The
    // reverse index is a WeakMap, so idOf is O(1) rather than a scan of the store, and a released
    // device is not kept alive by it.
    function handleRegistry(prefix: string, store: { [id: string]: any }) {
        const ids = new WeakMap<object, string>();
        let sequence = 0;

        return {
            idOf(handle: any) {
                if (!handle) return null;

                // The store is the authority: an id whose entry has been released is stale even if
                // the WeakMap still remembers it.
                const existing = ids.get(handle);
                if (existing !== undefined && store[existing] === handle) return existing;

                const id = `${prefix}${++sequence}`;
                store[id] = handle;
                ids.set(handle, id);
                return id;
            },
            // Drops the entry and its reverse index, so the next idOf() for the same object mints a
            // new id - which is what "the caller released this handle" has to mean.
            remove(id: string) {
                const handle = store[id];
                delete store[id];
                if (handle) ids.delete(handle);
                return handle;
            }
        };
    }

    // Fire-and-forget dispatch into a .NET [JSInvokable] callback. The returned promise is not
    // awaited by event-source callbacks, so without a catch a throwing .NET handler surfaces only as
    // an unobserved promise rejection with no link to the listener. Centralizing it here logs the
    // failing method name and swallows the rejection so it can't crash the dispatching event loop.
    function dispatch(dotNetRef: any, method: string, ...args: any[]) {
        if (!dotNetRef) return;
        try {
            const p = dotNetRef.invokeMethodAsync(method, ...args);
            if (p && typeof p.catch === 'function') {
                p.catch((e: any) => console.error(`BitButil: .NET callback '${method}' failed.`, e));
            }
            return p;
        } catch (e) {
            // Synchronous throw (e.g. the DotNetObjectReference was already disposed).
            console.error(`BitButil: dispatching .NET callback '${method}' failed.`, e);
        }
    }
}(BitButil));
