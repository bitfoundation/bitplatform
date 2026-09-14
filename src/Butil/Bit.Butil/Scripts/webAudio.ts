var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    let _ctx: AudioContext | null = null;
    let _master: GainNode | null = null;

    // One-shot playbacks started by playBuffer/playTone, keyed by the AudioPlaybackHandle's id.
    const _playbacks: { [id: string]: { source: AudioScheduledSourceNode, gain: GainNode } } = {};
    const _buffers: { [id: string]: AudioBuffer } = {};

    // What a module built on top of this one has to run before the context goes away - the node
    // registry emptying itself, say. Hooks rather than direct calls because the dependency only
    // goes one way: webAudioNodes knows about webAudio, and this module must not know about it.
    const _disposeHooks: (() => void)[] = [];

    // The graph, the AudioParam scheduling, the analyser reads, the worklet and the media-stream
    // nodes are each their own module (webAudioNodes, webAudioParams, webAudioAnalyser,
    // webAudioWorklet, webAudioMedia). This one is the context itself plus the fire-and-forget
    // playback that most apps want, so a page that only plays a tone downloads nothing else -
    // notably not mediaDevices, which only the media-stream nodes need.
    butil.webAudio = {
        isSupported() { return 'AudioContext' in window || 'webkitAudioContext' in (window as any); },
        resume() { return ensureCtx()?.resume(); },
        suspend() { return _ctx?.suspend(); },
        state() { return _ctx?.state ?? 'suspended'; },
        currentTime() { return ensureCtx()?.currentTime ?? 0; },
        sampleRate() { return ensureCtx()?.sampleRate ?? 0; },
        setMasterGain,
        masterGain() { ensureCtx(); return _master?.gain.value ?? 1; },
        playBuffer,
        playTone,
        stop,
        setGain,
        decodeAudioData,
        releaseBuffer,
        dispose,

        // For the modules layered on this one.
        context: ensureCtx,
        currentContext() { return _ctx; },
        master() { ensureCtx(); return _master; },
        bufferOf(bufferId: string) { return _buffers[bufferId]; },
        onDispose(hook: () => void) { _disposeHooks.push(hook); }
    };

    function ensureCtx(): AudioContext | null {
        if (_ctx) return _ctx;
        const Ctor: any = (window as any).AudioContext || (window as any).webkitAudioContext;
        if (!Ctor) return null;
        _ctx = new Ctor();
        if (!_ctx) return null;
        _master = _ctx.createGain();
        _master.gain.value = 1;
        _master.connect(_ctx.destination);
        return _ctx;
    }

    function setMasterGain(value: number) {
        ensureCtx();
        if (_master) _master.gain.value = value;
    }

    async function playBuffer(id: string, data: Uint8Array, startGain: number, loop: boolean) {
        const ctx = ensureCtx();
        if (!ctx || !_master) return;
        const buf = await ctx.decodeAudioData(butil.utils.arrayToBuffer(data));
        const source = ctx.createBufferSource();
        source.buffer = buf;
        source.loop = !!loop;
        const gain = ctx.createGain();
        gain.gain.value = startGain ?? 1;
        source.connect(gain).connect(_master);
        attach(id, source, gain);
        try { source.start(); } catch { /* invalid state */ }
    }

    function playTone(id: string, frequency: number, durationMs: number, waveform: string, startGain: number) {
        const ctx = ensureCtx();
        if (!ctx || !_master) return;
        const osc = ctx.createOscillator();
        osc.type = (waveform || 'sine') as OscillatorType;
        osc.frequency.value = frequency;
        const gain = ctx.createGain();
        gain.gain.value = startGain ?? 0.5;
        osc.connect(gain).connect(_master);
        attach(id, osc, gain);
        try {
            osc.start();
            if (durationMs && durationMs > 0) osc.stop(ctx.currentTime + durationMs / 1000);
        } catch { /* invalid state */ }
    }

    function attach(id: string, source: AudioScheduledSourceNode, gain: GainNode) {
        _playbacks[id] = { source, gain };
        source.addEventListener('ended', () => { delete _playbacks[id]; });
    }

    function stop(id: string) {
        const entry = _playbacks[id];
        if (!entry) return;
        delete _playbacks[id];
        try { entry.source.stop(); } catch { /* already stopped */ }
        try { entry.source.disconnect(); } catch { /* already disconnected */ }
        try { entry.gain.disconnect(); } catch { /* already disconnected */ }
    }

    function setGain(id: string, value: number) {
        const entry = _playbacks[id];
        if (entry) entry.gain.gain.value = value;
    }

    // --- Buffers -------------------------------------------------------------------------------

    async function decodeAudioData(bufferId: string, data: Uint8Array) {
        const ctx = ensureCtx();
        if (!ctx) return null;
        try {
            const buffer = await ctx.decodeAudioData(butil.utils.arrayToBuffer(data));
            _buffers[bufferId] = buffer;
            return {
                duration: buffer.duration,
                sampleRate: buffer.sampleRate,
                numberOfChannels: buffer.numberOfChannels,
                length: buffer.length
            };
        } catch {
            // A container the engine cannot decode, or truncated bytes.
            return null;
        }
    }

    function releaseBuffer(bufferId: string) { delete _buffers[bufferId]; }

    async function dispose() {
        for (const id of Object.keys(_playbacks)) stop(id);
        for (const hook of _disposeHooks) hook();
        for (const id of Object.keys(_buffers)) delete _buffers[id];
        try { _master?.disconnect(); } catch { /* already disconnected */ }
        const ctx = _ctx;
        _ctx = null;
        _master = null;
        if (ctx && ctx.state !== 'closed') {
            try { await ctx.close(); } catch { /* invalid state */ }
        }
    }
}(BitButil));
