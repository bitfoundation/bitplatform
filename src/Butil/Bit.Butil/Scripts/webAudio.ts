var BitButil = BitButil || {};

(function (butil: any) {
    let _ctx: AudioContext | null = null;
    let _master: GainNode | null = null;
    const _nodes: { [id: string]: { source: AudioScheduledSourceNode, gain: GainNode } } = {};

    butil.webAudio = {
        isSupported() { return 'AudioContext' in window || 'webkitAudioContext' in (window as any); },
        resume() { return ensureCtx()?.resume(); },
        suspend() { return _ctx?.suspend(); },
        setMasterGain,
        playBuffer,
        playTone,
        stop,
        setGain,
        dispose
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
        _nodes[id] = { source, gain };
        source.addEventListener('ended', () => { delete _nodes[id]; });
    }

    function stop(id: string) {
        const entry = _nodes[id];
        if (!entry) return;
        delete _nodes[id];
        try { entry.source.stop(); } catch { /* already stopped */ }
        try { entry.source.disconnect(); } catch { /* already disconnected */ }
        try { entry.gain.disconnect(); } catch { /* already disconnected */ }
    }

    function setGain(id: string, value: number) {
        const entry = _nodes[id];
        if (entry) entry.gain.gain.value = value;
    }

    async function dispose() {
        for (const id of Object.keys(_nodes)) stop(id);
        try { _master?.disconnect(); } catch { /* already disconnected */ }
        const ctx = _ctx;
        _ctx = null;
        _master = null;
        if (ctx && ctx.state !== 'closed') {
            try { await ctx.close(); } catch { /* invalid state */ }
        }
    }
}(BitButil));
