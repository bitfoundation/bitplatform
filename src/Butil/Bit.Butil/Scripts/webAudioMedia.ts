var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The mediaDevices stream a MediaStreamAudioDestination node was registered under, so releasing
    // the node can take the stream back out of that registry too.
    const _nodeStreams: { [id: string]: string } = {};

    // Its own module because it is the only part of Web Audio that needs mediaDevices: an app
    // building a filter chain would otherwise download the whole media-device layer with it.
    butil.webAudioMedia = {
        createMediaElementSource,
        createMediaStreamSource,
        createMediaStreamDestination
    };

    butil.webAudioNodes.onRelease((id: string) => {
        const streamId = _nodeStreams[id];
        if (streamId === undefined) return;
        delete _nodeStreams[id];
        // The node's stream was parked in the mediaDevices registry; leaving it there after the
        // node is gone keeps a live stream nothing in .NET has a handle to stop any more.
        butil.mediaDevices.stop(streamId);
    });

    function createMediaElementSource(id: string, element: any) {
        const ctx = butil.webAudio.context();
        if (!ctx || !element) return false;
        try {
            // An element can only ever be the source of one node; a second attempt throws, so the
            // failure is reported rather than left to surface as an unhandled interop error.
            return butil.webAudioNodes.register(id, ctx.createMediaElementSource(element));
        } catch {
            return false;
        }
    }

    function createMediaStreamSource(id: string, streamId: string) {
        const ctx = butil.webAudio.context();
        const stream = butil.mediaDevices.getStream(streamId);
        if (!ctx || !stream) return false;
        try { return butil.webAudioNodes.register(id, ctx.createMediaStreamSource(stream)); }
        catch { return false; }
    }

    function createMediaStreamDestination(id: string, streamId: string) {
        const ctx = butil.webAudio.context();
        if (!ctx?.createMediaStreamDestination) return false;
        const node = ctx.createMediaStreamDestination();
        if (!butil.webAudioNodes.register(id, node)) return false;
        // Handing the stream to the mediaDevices registry is what lets .NET treat it like any other
        // stream - attach it to an element, or record it with MediaRecorder.
        butil.mediaDevices.registerStream(streamId, node.stream);
        _nodeStreams[id] = streamId;
        return true;
    }
}(BitButil));
