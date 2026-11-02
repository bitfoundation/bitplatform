var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer,
        viewToBytes,
        dispatch,
        throttle,
        handleRegistry,
        encodeMessage,
        messageJson,
        boundMessage,
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
        // A non-zero timer is the one signal that a trailing send is queued; `pending` is only the
        // payload it will carry, and may legitimately be undefined for payload-less callers.
        let timer: any = 0;
        let pending: any;

        const gate: any = (e: any) => {
            const now = performance.now();
            const remaining = minInterval - (now - lastSentAt);

            if (remaining <= 0) {
                // A leading send makes a queued trailing one redundant - it would deliver an older
                // payload than the one going out now.
                if (timer) { clearTimeout(timer); timer = 0; }
                pending = undefined;
                lastSentAt = now;
                send(e);
                return;
            }

            if (!trailing) return;

            // Only the newest suppressed payload is kept: a queue of them would deliver the whole
            // burst late, which is the flooding this exists to prevent.
            pending = e;
            if (timer) return;

            timer = setTimeout(() => {
                timer = 0;
                const last = pending;
                pending = undefined;
                lastSentAt = performance.now();
                send(last);
            }, remaining);
        };

        gate.cancel = () => {
            if (timer) { clearTimeout(timer); timer = 0; }
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
    //
    // The JSON is capped, because a structured clone keeps shared references and JSON cannot: a
    // message of a few kilobytes built as x = [x, x], forty times over, spells out to 2^40 leaves
    // here. See jsonFits for how the cap is counted.
    const MESSAGE_JSON_LIMIT = 32 * 1024 * 1024;
    const TOO_LARGE = {};

    function encodeMessage(data: any): [boolean, string | null, Uint8Array | null] {
        if (data instanceof ArrayBuffer) return [true, null, new Uint8Array(data)];
        if (ArrayBuffer.isView(data)) {
            const view = data as ArrayBufferView;
            return [true, null, new Uint8Array(view.buffer, view.byteOffset, view.byteLength)];
        }
        return [false, messageJson(data), null];
    }

    // The JSON of a message, capped. Never throws: a message JSON cannot write at all arrives as
    // its type name rather than not at all.
    function messageJson(data: any): string {
        try {
            return JSON.stringify(boundMessage(data) ?? null);
        } catch {
            return JSON.stringify(typeName(data));
        }
    }

    // The message itself when its JSON stays within the cap, and its type name in its place when it
    // does not. On its own, this is for the channels whose data .NET receives as a JsonElement,
    // where Blazor's interop writes the JSON rather than this file.
    function boundMessage(data: any): any {
        let fits: boolean;
        try {
            fits = jsonFits(data);
        } catch {
            // A cyclic graph, or a value JSON cannot represent (a BigInt). Losing the message entirely
            // would be worse than losing its shape.
            return typeName(data);
        }
        if (fits) return data;

        // The message can no longer say why it arrived empty, so the console does.
        console.warn(`BitButil: a message whose JSON passes ${MESSAGE_JSON_LIMIT / 1024 / 1024} MB was delivered as its type name only.`);
        return typeName(data);
    }

    // "[object Object]", "[object Array]" and so on. Not String(data), which calls the message's own
    // toString - and a cloned object can carry a property of that name that is no function at all.
    function typeName(data: any) {
        return Object.prototype.toString.call(data);
    }

    // Whether data's JSON stays within the cap, counted the way JSON.stringify writes it but without
    // writing any of it. Throws where JSON.stringify would: on a cycle, and on a BigInt.
    //
    // Counting is kept apart from writing so that it costs what the message cost to clone, and no
    // more. An object is counted once and remembered, so one that is shared a million times costs a
    // million lookups rather than a million walks - which a replacer passed to JSON.stringify cannot
    // do, since it sees every reference and can skip none. And the count stops as soon as it passes
    // the cap, so an array that is long but holds nothing is refused before it is walked.
    //
    // A number that is not a whole one is first counted at its longest: its exact length is that of
    // the string it turns into, and making that string per number would make counting cost as much as
    // writing. Only a message which that puts over the cap is counted again, exactly.
    function jsonFits(data: any) {
        return (countJson(data, false) ?? countJson(data, true)) === true;
    }

    // "-0.0000012345678901234567": no number is written longer.
    const LONGEST_NUMBER = 25;

    // JSON.stringify escapes a quote, a backslash and a control character, and a surrogate that is
    // not half of a pair.
    const ESCAPED = /["\\\u0000-\u001f\ud800-\udfff]/;

    // One count for jsonFits: true when the JSON fits, false when it does not, and null when it only
    // might - when the count passed the cap after taking a number at its longest.
    function countJson(data: any, exact: boolean): boolean | null {
        let budget = MESSAGE_JSON_LIMIT;
        let estimated = false;
        const sizes = new Map<object, number>();
        const open = new Set<object>();

        const spend = (length: number) => {
            if ((budget -= length) < 0) throw TOO_LARGE;
        };

        const numberLength = (value: number) => {
            if (!isFinite(value)) return 4;   // NaN and the infinities are written as null
            const magnitude = Math.abs(value);
            if (magnitude < 1e21 && Math.floor(magnitude) === magnitude) {
                let digits = 1;
                for (let power = 10; power <= magnitude; power *= 10) digits++;
                return value < 0 ? digits + 1 : digits;
            }
            if (exact) return String(value).length;
            estimated = true;
            return LONGEST_NUMBER;
        };

        // Spends what one value adds to the JSON - not the key or the comma before it - and returns
        // false for a value JSON leaves out.
        function write(value: any, key: string | number): boolean {
            const isObject = typeof value === 'object' && value !== null;
            if (isObject) {
                const known = sizes.get(value);
                if (known !== undefined) {
                    spend(known);
                    return true;
                }
            }

            const start = budget;
            let json = value;
            if ((isObject || typeof json === 'bigint') && typeof json.toJSON === 'function') json = json.toJSON(String(key));
            switch (typeof json) {
                case 'string': spend(quotedLength(json)); break;
                case 'number': spend(numberLength(json)); break;
                case 'boolean': spend(json ? 4 : 5); break;
                case 'bigint': throw new TypeError('BigInt');
                case 'object':
                    if (json === null) spend(4);
                    // A Number, String or Boolean object is written as the primitive it wraps.
                    else if (json instanceof Number) spend(numberLength(Number(json)));
                    else if (json instanceof String) spend(quotedLength(String(json)));
                    else if (json instanceof Boolean) spend(json.valueOf() ? 4 : 5);
                    else if (json instanceof BigInt) throw new TypeError('BigInt');
                    else writeMembers(json);
                    break;
                default: return false;   // undefined, a function or a symbol
            }

            if (isObject) sizes.set(value, start - budget);
            return true;
        }

        function writeMembers(value: any) {
            if (open.has(value)) throw new TypeError('cyclic');
            open.add(value);

            if (Array.isArray(value)) {
                // The brackets and the commas, up front: an array that is long but holds nothing - a
                // sparse one - is refused here, not after a walk through its holes.
                const length = value.length;
                spend(length ? length + 1 : 2);
                for (let i = 0; i < length; i++) {
                    if (!write(value[i], i)) spend(4);   // written as null
                }
            } else if (ArrayBuffer.isView(value) && !(value instanceof DataView)) {
                // A typed array is written as an object keyed by index - {"0":1,"1":2} - and listing
                // those keys would cost more than the JSON they make. So they are counted instead: the
                // braces, and per element two quotes, a colon and a comma, up front, then the digits.
                const typed = value as any;
                const length = typed.length;
                spend(length ? length * 4 + 1 : 2);
                for (let i = 0; i < length; i++) {
                    spend(numberLength(i));
                    write(typed[i], i);
                }
            } else {
                spend(2);
                let first = true;
                for (const key of Object.keys(value)) {
                    if (!write(value[key], key)) continue;
                    // The key, quoted, with its colon - and the comma before it.
                    spend(quotedLength(key) + (first ? 1 : 2));
                    first = false;
                }
            }

            open.delete(value);
        }

        try {
            write(data ?? null, '');
            return true;
        } catch (e) {
            if (e !== TOO_LARGE) throw e;
            return estimated ? null : false;
        }
    }

    function quotedLength(text: string) {
        let length = text.length + 2;
        const first = text.search(ESCAPED);
        if (first < 0) return length;

        for (let i = first; i < text.length; i++) {
            const c = text.charCodeAt(i);
            if (c === 0x22 || c === 0x5c) length += 1;   // \" and \\
            else if (c < 0x20) length += c === 0x08 || c === 0x09 || c === 0x0a || c === 0x0c || c === 0x0d ? 1 : 5;   // \n, or \u0001
            else if (c >= 0xd800 && c <= 0xdfff) {
                if (c <= 0xdbff && (text.charCodeAt(i + 1) & 0xfc00) === 0xdc00) i++;   // a pair, written as it is
                else length += 5;
            }
        }
        return length;
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
