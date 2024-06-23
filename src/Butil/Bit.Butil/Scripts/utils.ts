var BitButil = BitButil || {};

(function (butil: any) {
    butil.utils = {
        arrayToBuffer
    };

    function arrayToBuffer(array: Uint8Array) {
        return array?.buffer.slice(array.byteOffset, array.byteLength + array.byteOffset)
    }
}(BitButil));