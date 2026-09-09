var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Reads off an AnalyserNode. Its own module because a visualizer polls these on every frame
    // while most graphs never call them at all.
    butil.webAudioAnalyser = {
        byteFrequencyData,
        byteTimeDomainData,
        floatFrequencyData
    };

    function byteFrequencyData(id: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node?.getByteFrequencyData) return null;
        const data = new Uint8Array(node.frequencyBinCount);
        node.getByteFrequencyData(data);
        return data;
    }

    function byteTimeDomainData(id: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node?.getByteTimeDomainData) return null;
        const data = new Uint8Array(node.fftSize);
        node.getByteTimeDomainData(data);
        return data;
    }

    function floatFrequencyData(id: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node?.getFloatFrequencyData) return null;
        const data = new Float32Array(node.frequencyBinCount);
        node.getFloatFrequencyData(data);
        // A Float32Array does not serialize as numbers, and the values are decibels, so precision
        // matters more here than the extra bytes.
        return Array.from(data);
    }
}(BitButil));
