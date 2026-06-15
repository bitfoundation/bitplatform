var BitButil = BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer,
        dispatch
    };

    function arrayToBuffer(array: Uint8Array) {
        if (!array) return undefined;
        // Slice covers exactly the [byteOffset, byteOffset + byteLength) range so that
        // a Uint8Array view over a larger buffer doesn't leak extra bytes.
        return array.buffer.slice(array.byteOffset, array.byteOffset + array.byteLength);
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
