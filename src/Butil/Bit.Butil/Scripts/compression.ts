var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Round-trips bytes through a transform stream by wrapping them in a one-chunk Response, whose
    // arrayBuffer() drains the whole pipeline for us. Doing it by hand would mean a reader loop and
    // a manual concat of the output chunks, for the same result.
    async function pump(data: Uint8Array, stream: any) {
        const source = new Blob([butil.utils.arrayToBuffer(data)]).stream();
        const piped = source.pipeThrough(stream);
        const buffer = await new Response(piped).arrayBuffer();
        return new Uint8Array(buffer);
    }

    butil.compression = {
        isSupported() { return typeof (window as any).CompressionStream === 'function'; },
        async compress(data: Uint8Array, format: string) {
            const CS = (window as any).CompressionStream;
            if (typeof CS !== 'function') return null;
            try { return await pump(data, new CS(format)); }
            catch { return null; }   // unknown format, or the stream errored mid-pipe
        },
        async decompress(data: Uint8Array, format: string) {
            const DS = (window as any).DecompressionStream;
            if (typeof DS !== 'function') return null;
            try { return await pump(data, new DS(format)); }
            catch { return null; }   // corrupt input, or a format mismatch
        }
    };
}(BitButil));
