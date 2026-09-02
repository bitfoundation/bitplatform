var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer,
        dispatch,
        encodeMessage
    };

    function arrayToBuffer(array: Uint8Array) {
        if (!array) return undefined;
        // Slice covers exactly the [byteOffset, byteOffset + byteLength) range so that
        // a Uint8Array view over a larger buffer doesn't leak extra bytes.
        return array.buffer.slice(array.byteOffset, array.byteOffset + array.byteLength);
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
