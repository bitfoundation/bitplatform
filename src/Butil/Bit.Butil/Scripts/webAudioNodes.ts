var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Graph nodes .NET built explicitly, keyed by their handle's id.
    const _nodes: { [id: string]: any } = {};

    // What a module layered on this registry has to undo when a node is released - webAudioMedia
    // taking its stream back out of the mediaDevices registry. A hook rather than a call into that
    // module, so the dependency stays one-way and this module keeps mediaDevices out of its own
    // download.
    const _releaseHooks: ((id: string) => void)[] = [];

    butil.webAudio.onDispose(() => { for (const id of Object.keys(_nodes)) releaseNode(id); });

    butil.webAudioNodes = {
        createGain,
        createBiquadFilter,
        createAnalyser,
        createConvolver,
        createPanner,
        createStereoPanner,
        createDelay,
        createDynamicsCompressor,
        createWaveShaper,
        createOscillator,
        createBufferSource,
        createConstantSource,
        setListener,
        releaseNode,

        // For the modules layered on this one.
        register,
        nodeOf(id: string) { return _nodes[id]; },
        onRelease(hook: (id: string) => void) { _releaseHooks.push(hook); }
    };

    function register(id: string, node: any) {
        if (!node) return false;
        releaseNode(id);
        _nodes[id] = node;
        return true;
    }

    function createGain(id: string, gain: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createGain();
        node.gain.value = gain;
        return register(id, node);
    }

    function createBiquadFilter(id: string, type: string, frequency: number, q: number, gain: number, detune: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createBiquadFilter();
        node.type = type as BiquadFilterType;
        node.frequency.value = frequency;
        node.Q.value = q;
        node.gain.value = gain;
        node.detune.value = detune;
        return register(id, node);
    }

    function createAnalyser(id: string, fftSize: number, smoothing: number, minDecibels: number, maxDecibels: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createAnalyser();
        // fftSize has to be a power of two in [32, 32768]; an invalid one throws rather than being
        // clamped, and it is the most common thing to get wrong here.
        try { node.fftSize = fftSize; } catch { /* keep the default */ }
        node.smoothingTimeConstant = smoothing;
        node.minDecibels = minDecibels;
        node.maxDecibels = maxDecibels;
        return register(id, node);
    }

    function createConvolver(id: string, bufferId: string, normalize: boolean) {
        const ctx = butil.webAudio.context();
        const buffer = butil.webAudio.bufferOf(bufferId);
        if (!ctx || !buffer) return false;
        const node = ctx.createConvolver();
        // Normalization has to be set before the buffer: setting it afterwards does not re-scale
        // what has already been loaded.
        node.normalize = !!normalize;
        node.buffer = buffer;
        return register(id, node);
    }

    function createPanner(id: string, options: any) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createPanner();
        if (options?.panningModel) node.panningModel = options.panningModel;
        if (options?.distanceModel) node.distanceModel = options.distanceModel;
        for (const key of ['refDistance', 'maxDistance', 'rolloffFactor', 'coneInnerAngle', 'coneOuterAngle', 'coneOuterGain']) {
            if (typeof options?.[key] === 'number') (node as any)[key] = options[key];
        }
        for (const key of ['positionX', 'positionY', 'positionZ', 'orientationX', 'orientationY', 'orientationZ']) {
            const param = (node as any)[key];
            if (param && typeof options?.[key] === 'number') param.value = options[key];
        }
        return register(id, node);
    }

    function createStereoPanner(id: string, pan: number) {
        const ctx = butil.webAudio.context();
        if (!ctx?.createStereoPanner) return false;
        const node = ctx.createStereoPanner();
        node.pan.value = pan;
        return register(id, node);
    }

    function createDelay(id: string, maxDelayTime: number, delayTime: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createDelay(maxDelayTime > 0 ? maxDelayTime : 1);
        node.delayTime.value = delayTime;
        return register(id, node);
    }

    function createDynamicsCompressor(id: string, threshold: number, knee: number, ratio: number, attack: number, release: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createDynamicsCompressor();
        node.threshold.value = threshold;
        node.knee.value = knee;
        node.ratio.value = ratio;
        node.attack.value = attack;
        node.release.value = release;
        return register(id, node);
    }

    function createWaveShaper(id: string, curve: number[], oversample: string) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createWaveShaper();
        if (curve?.length) node.curve = new Float32Array(curve);
        node.oversample = (oversample || 'none') as OverSampleType;
        return register(id, node);
    }

    function createOscillator(id: string, type: string, frequency: number, detune: number) {
        const ctx = butil.webAudio.context();
        if (!ctx) return false;
        const node = ctx.createOscillator();
        node.type = (type || 'sine') as OscillatorType;
        node.frequency.value = frequency;
        node.detune.value = detune;
        return register(id, node);
    }

    function createBufferSource(id: string, bufferId: string, loop: boolean, loopStart: number, loopEnd: number, playbackRate: number, detune: number) {
        const ctx = butil.webAudio.context();
        const buffer = butil.webAudio.bufferOf(bufferId);
        if (!ctx || !buffer) return false;
        const node = ctx.createBufferSource();
        node.buffer = buffer;
        node.loop = !!loop;
        if (loopStart > 0) node.loopStart = loopStart;
        if (loopEnd > 0) node.loopEnd = loopEnd;
        node.playbackRate.value = playbackRate > 0 ? playbackRate : 1;
        try { node.detune.value = detune; } catch { /* not implemented everywhere */ }
        return register(id, node);
    }

    function createConstantSource(id: string, offset: number) {
        const ctx = butil.webAudio.context();
        if (!ctx?.createConstantSource) return false;
        const node = ctx.createConstantSource();
        node.offset.value = offset;
        return register(id, node);
    }

    function setListener(x: number, y: number, z: number, forwardX: number, forwardY: number, forwardZ: number, upX: number, upY: number, upZ: number) {
        const ctx = butil.webAudio.context();
        const listener: any = ctx?.listener;
        if (!listener) return false;
        try {
            if (listener.positionX) {
                listener.positionX.value = x;
                listener.positionY.value = y;
                listener.positionZ.value = z;
                listener.forwardX.value = forwardX;
                listener.forwardY.value = forwardY;
                listener.forwardZ.value = forwardZ;
                listener.upX.value = upX;
                listener.upY.value = upY;
                listener.upZ.value = upZ;
            } else {
                // The pre-AudioParam listener interface, still the only one on some engines.
                listener.setPosition(x, y, z);
                listener.setOrientation(forwardX, forwardY, forwardZ, upX, upY, upZ);
            }
            return true;
        } catch {
            return false;
        }
    }

    function releaseNode(id: string) {
        const node = _nodes[id];
        if (!node) return;
        delete _nodes[id];
        try { node.stop?.(); } catch { /* not a source, or never started */ }
        try { node.disconnect(); } catch { /* already disconnected */ }
        if (node.port) {
            try { node.port.onmessage = null; node.port.close(); } catch { /* already closed */ }
        }
        for (const hook of _releaseHooks) hook(id);
    }
}(BitButil));
