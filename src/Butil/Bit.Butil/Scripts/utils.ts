var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer,
        viewToBytes,
        dispatch,
        throttle,
        handleRegistry,
        encodeMessage,
        randomUUID,
        pick
    };

    // crypto.randomUUID is only exposed in a secure context, while crypto.getRandomValues is
    // available everywhere - so an id generated here works on plain http too. It lives in utils
    // rather than in the crypto module because every module that needs an id (dom, dataTransfer,
    // windowMessaging, ...) would otherwise drag the whole crypto module in as a dependency.
    function randomUUID(): string {
        if (typeof crypto?.randomUUID === 'function') return crypto.randomUUID();

        const bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        bytes[6] = (bytes[6] & 0x0f) | 0x40; // version 4
        bytes[8] = (bytes[8] & 0x3f) | 0x80; // variant 10
        const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
        return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
    }

    function arrayToBuffer(array: Uint8Array) {
        if (!array) return undefined;
        // Slice covers exactly the [byteOffset, byteOffset + byteLength) range so that
        // a Uint8Array view over a larger buffer doesn't leak extra bytes.
        return array.buffer.slice(array.byteOffset, array.byteOffset + array.byteLength);
    }

    // Builds a browser dictionary from a .NET-serialized options object, keeping only the named
    // members that carry a value. Blazor serializes every member of an options class, so a member
    // left unset arrives as an explicit `null` - and a browser dictionary that expects an enum, a
    // number or a sequence rejects `null` outright rather than treating it as "not specified".
    function pick(source: any, keys: string[]) {
        const result: any = {};
        if (!source) return result;
        for (const key of keys) {
            const value = source[key];
            if (value !== null && value !== undefined) result[key] = value;
        }
        return result;
    }

    // The inverse of arrayToBuffer, and it exists for the same reason: Web Bluetooth, WebHID and
    // WebUSB all hand their payloads back as a DataView over a larger buffer, so the offset and
    // length have to be carried through or the surrounding bytes leak into .NET.
    function viewToBytes(view: any) {
        if (!view) return new Uint8Array(0);
        if (view instanceof Uint8Array) return view;
        return new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
    }

    // Device streams, pointer moves, scrolls and observer callbacks all fire far faster than a
    // Blazor render can keep up with - 60 Hz is the usual platform default, and a resize drag or a
    // pinch-zoom is a frame every 16 ms - so a subscription that would otherwise flood the interop
    // channel is rate-limited here, before the cost of a round trip is paid. That cost is the whole
    // point: a round trip is a JSON serialization plus, on Blazor Server, a SignalR message and a
    // network hop, which is orders of magnitude more than anything this wrapper does.
    //
    // A non-positive interval means "every event", and returns the handler untouched rather than a
    // wrapper that always passes, so an unthrottled subscription pays nothing at all.
    //
    // `trailing` decides what happens to the events that are suppressed. Without it the last one is
    // simply dropped, which is what a sampling stream wants - the next reading is along shortly and
    // is worth more than the one just missed. With it the newest suppressed event is delivered once
    // the interval elapses, which is what every *state* stream wants: the final size of a resize,
    // the resting position of a scroll, the place the pointer stopped. Dropping those leaves .NET
    // holding a value that is permanently one sample out of date.
    //
    // Callers pass a payload already mapped out of the DOM event rather than the event itself, so a
    // trailing send cannot read an event object the browser has finished dispatching.
    //
    // The gate carries a `cancel` for teardown: a trailing send is a timer that outlives the
    // subscription that armed it, and firing it after an unsubscribe dispatches into a
    // DotNetObjectReference the .NET side has already disposed. Callers that unsubscribe must call
    // it - optionally, since an ungated subscription is handed the raw dispatch and has none.
    function throttle(minInterval: number, send: (e: any) => void, trailing?: boolean): any {
        if (!(minInterval > 0)) return send;

        // Never sent, rather than "sent at the time origin": a subscription created inside the
        // first `minInterval` ms of the page would otherwise lose its leading-edge send, which is
        // the one behaviour a caller setting an interval still expects to get immediately.
        let lastSentAt = -Infinity;
        let timer: any = 0;
        let pending: any;
        let hasPending = false;

        const gate: any = (e: any) => {
            const now = performance.now();
            const remaining = minInterval - (now - lastSentAt);

            if (remaining <= 0) {
                // A leading send makes a queued trailing one redundant - it would deliver an older
                // payload than the one going out now.
                if (timer) { clearTimeout(timer); timer = 0; }
                hasPending = false;
                pending = undefined;
                lastSentAt = now;
                send(e);
                return;
            }

            if (!trailing) return;

            // Only the newest suppressed payload is kept: a queue of them would deliver the whole
            // burst late, which is the flooding this exists to prevent.
            pending = e;
            hasPending = true;
            if (timer) return;

            timer = setTimeout(() => {
                timer = 0;
                if (!hasPending) return;
                const last = pending;
                hasPending = false;
                pending = undefined;
                lastSentAt = performance.now();
                send(last);
            }, remaining);
        };

        gate.cancel = () => {
            if (timer) { clearTimeout(timer); timer = 0; }
            hasPending = false;
            pending = undefined;
        };

        return gate;
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

    // Flattens whatever a structured-clone channel delivered - a worker, a MessagePort, another
    // window - into the three values .NET's message DTOs are made of: [isBinary, json, bytes].
    //
    // Structured clone carries far more than JSON does (Map, Set, Date, cyclic graphs, ArrayBuffers),
    // and none of the extra survives a trip through .NET's JSON interop. So the contract is drawn
    // here, in one place, and stated in the C# docs: binary payloads stay binary, and everything
    // else becomes JSON. Non-binary data is ALWAYS stringified, including a plain string, so that
    // the .NET side can deserialize any message without first asking what shape it is.
    function encodeMessage(data: any): [boolean, string | null, Uint8Array | null] {
        if (data instanceof ArrayBuffer) return [true, null, new Uint8Array(data)];
        if (ArrayBuffer.isView(data)) {
            const view = data as ArrayBufferView;
            return [true, null, new Uint8Array(view.buffer, view.byteOffset, view.byteLength)];
        }

        try {
            return [false, JSON.stringify(data ?? null), null];
        } catch {
            // A cyclic graph, or a value JSON cannot represent (a function, a BigInt). Losing the
            // message entirely would be worse than losing its shape.
            return [false, JSON.stringify(String(data)), null];
        }
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
