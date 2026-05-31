var BitButil = BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer
    };

    function arrayToBuffer(array: Uint8Array) {
        if (!array) return undefined;
        // Slice covers exactly the [byteOffset, byteOffset + byteLength) range so that
        // a Uint8Array view over a larger buffer doesn't leak extra bytes.
        return array.buffer.slice(array.byteOffset, array.byteOffset + array.byteLength);
    }
}(BitButil));