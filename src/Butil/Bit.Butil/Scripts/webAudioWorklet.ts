var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // AudioWorklet: loading a processor module and talking to its node. Separate because a worklet
    // is an opt-in, secure-context, extra-file feature that most graphs never use.
    butil.webAudioWorklet = {
        isWorkletSupported() { return !!(window as any).AudioWorkletNode; },
        addModule,
        createWorkletNode,
        postWorkletMessage
    };

    async function addModule(url: string) {
        const ctx = butil.webAudio.context();
        if (!ctx?.audioWorklet) return false;
        try { await ctx.audioWorklet.addModule(url); return true; }
        catch { return false; }  // the file 404'd, or the processor script threw while registering
    }

    function createWorkletNode(id: string, name: string, options: any, dotNetRef: any, method: string) {
        const ctx = butil.webAudio.context();
        const Ctor: any = (window as any).AudioWorkletNode;
        if (!ctx || !Ctor) return false;
        try {
            const init: any = butil.utils.pick(options, ['numberOfInputs', 'numberOfOutputs', 'outputChannelCount', 'parameterData']);
            // processorOptions travels as JSON text so that .NET can send an arbitrary payload
            // without a type that both sides have to agree on.
            if (options?.processorOptions) init.processorOptions = JSON.parse(options.processorOptions);
            const node = new Ctor(ctx, name, init);
            if (dotNetRef) {
                node.port.onmessage = (e: any) => butil.utils.dispatch(dotNetRef, method, id,
                    typeof e.data === 'string' ? e.data : JSON.stringify(e.data));
            }
            return butil.webAudioNodes.register(id, node);
        } catch {
            // No processor registered under that name - addModule either failed or was never called.
            return false;
        }
    }

    function postWorkletMessage(id: string, message: string) {
        const node = butil.webAudioNodes.nodeOf(id);
        if (!node?.port) return false;
        try { node.port.postMessage(message); return true; } catch { return false; }
    }
}(BitButil));
