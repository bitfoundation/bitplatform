var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    let _ctx: AudioContext | null = null;
    let _master: GainNode | null = null;

    // One-shot playbacks started by playBuffer/playTone, keyed by the AudioPlaybackHandle's id.
    const _playbacks: { [id: string]: { source: AudioScheduledSourceNode, gain: GainNode } } = {};
    // Graph nodes .NET built explicitly, keyed by their handle's id.
    const _nodes: { [id: string]: any } = {};
    // The mediaDevices stream a MediaStreamAudioDestination node was registered under, so releasing
    // the node can take the stream back out of that registry too.
    const _nodeStreams: { [id: string]: string } = {};
    const _buffers: { [id: string]: AudioBuffer } = {};

    butil.webAudio = {
        isSupported() { return 'AudioContext' in window || 'webkitAudioContext' in (window as any); },
        isWorkletSupported() { return !!(window as any).AudioWorkletNode; },
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
        createMediaElementSource,
        createMediaStreamSource,
        createMediaStreamDestination,
        addModule,
        createWorkletNode,
        postWorkletMessage,
        connect,
        connectToDestination,
        disconnect,
        setParam,
        rampParam,
        cancelScheduledParam,
        setProperty,
        start,
        stopNode,
        byteFrequencyData,
        byteTimeDomainData,
        floatFrequencyData,
        setListener,
        releaseNode,
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

    // --- Node construction ---------------------------------------------------------------------

    function register(id: string, node: any) {
        if (!node) return false;
        releaseNode(id);
        _nodes[id] = node;
        return true;
    }

    function createGain(id: string, gain: number) {
        const ctx = ensureCtx();
        if (!ctx) return false;
        const node = ctx.createGain();
        node.gain.value = gain;
        return register(id, node);
    }

    function createBiquadFilter(id: string, type: string, frequency: number, q: number, gain: number, detune: number) {
        const ctx = ensureCtx();
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
        const ctx = ensureCtx();
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
        const ctx = ensureCtx();
        const buffer = _buffers[bufferId];
        if (!ctx || !buffer) return false;
        const node = ctx.createConvolver();
        // Normalization has to be set before the buffer: setting it afterwards does not re-scale
        // what has already been loaded.
        node.normalize = !!normalize;
        node.buffer = buffer;
        return register(id, node);
    }

    function createPanner(id: string, options: any) {
        const ctx = ensureCtx();
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
        const ctx = ensureCtx();
        if (!ctx?.createStereoPanner) return false;
        const node = ctx.createStereoPanner();
        node.pan.value = pan;
        return register(id, node);
    }

    function createDelay(id: string, maxDelayTime: number, delayTime: number) {
        const ctx = ensureCtx();
        if (!ctx) return false;
        const node = ctx.createDelay(maxDelayTime > 0 ? maxDelayTime : 1);
        node.delayTime.value = delayTime;
        return register(id, node);
    }

    function createDynamicsCompressor(id: string, threshold: number, knee: number, ratio: number, attack: number, release: number) {
        const ctx = ensureCtx();
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
        const ctx = ensureCtx();
        if (!ctx) return false;
        const node = ctx.createWaveShaper();
        if (curve?.length) node.curve = new Float32Array(curve);
        node.oversample = (oversample || 'none') as OverSampleType;
        return register(id, node);
    }

    function createOscillator(id: string, type: string, frequency: number, detune: number) {
        const ctx = ensureCtx();
        if (!ctx) return false;
        const node = ctx.createOscillator();
        node.type = (type || 'sine') as OscillatorType;
        node.frequency.value = frequency;
        node.detune.value = detune;
        return register(id, node);
    }

    function createBufferSource(id: string, bufferId: string, loop: boolean, loopStart: number, loopEnd: number, playbackRate: number, detune: number) {
        const ctx = ensureCtx();
        const buffer = _buffers[bufferId];
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
        const ctx = ensureCtx();
        if (!ctx?.createConstantSource) return false;
        const node = ctx.createConstantSource();
        node.offset.value = offset;
        return register(id, node);
    }

    function createMediaElementSource(id: string, element: any) {
        const ctx = ensureCtx();
        if (!ctx || !element) return false;
        try {
            // An element can only ever be the source of one node; a second attempt throws, so the
            // failure is reported rather than left to surface as an unhandled interop error.
            return register(id, ctx.createMediaElementSource(element));
        } catch {
            return false;
        }
    }

    function createMediaStreamSource(id: string, streamId: string) {
        const ctx = ensureCtx();
        const stream = butil.mediaDevices.getStream(streamId);
        if (!ctx || !stream) return false;
        try { return register(id, ctx.createMediaStreamSource(stream)); }
        catch { return false; }
    }

    function createMediaStreamDestination(id: string, streamId: string) {
        const ctx = ensureCtx();
        if (!ctx?.createMediaStreamDestination) return false;
        const node = ctx.createMediaStreamDestination();
        if (!register(id, node)) return false;
        // Handing the stream to the mediaDevices registry is what lets .NET treat it like any other
        // stream - attach it to an element, or record it with MediaRecorder.
        butil.mediaDevices.registerStream(streamId, node.stream);
        _nodeStreams[id] = streamId;
        return true;
    }

    async function addModule(url: string) {
        const ctx = ensureCtx();
        if (!ctx?.audioWorklet) return false;
        try { await ctx.audioWorklet.addModule(url); return true; }
        catch { return false; }  // the file 404'd, or the processor script threw while registering
    }

    function createWorkletNode(id: string, name: string, options: any, dotNetRef: any, method: string) {
        const ctx = ensureCtx();
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
            return register(id, node);
        } catch {
            // No processor registered under that name - addModule either failed or was never called.
            return false;
        }
    }

    function postWorkletMessage(id: string, message: string) {
        const node = _nodes[id];
        if (!node?.port) return false;
        try { node.port.postMessage(message); return true; } catch { return false; }
    }

    // --- Wiring --------------------------------------------------------------------------------

    function connect(fromId: string, toId: string) {
        const from = _nodes[fromId];
        const to = _nodes[toId];
        if (!from || !to) return false;
        try { from.connect(to); return true; } catch { return false; }
    }

    // Everything Butil-managed goes through the master gain rather than straight to the context's
    // destination, so one call can duck or mute the whole app.
    function connectToDestination(id: string) {
        const node = _nodes[id];
        ensureCtx();
        if (!node || !_master) return false;
        try { node.connect(_master); return true; } catch { return false; }
    }

    function disconnect(id: string) {
        const node = _nodes[id];
        if (!node) return false;
        try { node.disconnect(); return true; } catch { return false; }
    }

    function param(id: string, name: string) {
        const node = _nodes[id];
        const value = node?.[name];
        // An AudioParam is distinguished from a plain number property by having a setValueAtTime.
        return value && typeof value.setValueAtTime === 'function' ? value : null;
    }

    function setParam(id: string, name: string, value: number, atTime: number) {
        const target = param(id, name);
        const ctx = _ctx;
        if (!target || !ctx) return false;
        try {
            if (atTime > 0) target.setValueAtTime(value, ctx.currentTime + atTime);
            else target.value = value;
            return true;
        } catch {
            return false;
        }
    }

    function rampParam(id: string, name: string, value: number, seconds: number, exponential: boolean) {
        const target = param(id, name);
        const ctx = _ctx;
        if (!target || !ctx) return false;
        try {
            // Ramps start from the value at the last scheduled point, so anchoring "now" first is
            // what stops a ramp from starting at whatever was scheduled before it.
            target.cancelScheduledValues(ctx.currentTime);
            target.setValueAtTime(target.value, ctx.currentTime);
            // An exponential ramp cannot cross or reach zero, so a zero target is nudged to a value
            // below hearing rather than throwing.
            if (exponential) target.exponentialRampToValueAtTime(value === 0 ? 0.0001 : value, ctx.currentTime + seconds);
            else target.linearRampToValueAtTime(value, ctx.currentTime + seconds);
            return true;
        } catch {
            return false;
        }
    }

    function cancelScheduledParam(id: string, name: string) {
        const target = param(id, name);
        const ctx = _ctx;
        if (!target || !ctx) return false;
        try { target.cancelScheduledValues(ctx.currentTime); return true; } catch { return false; }
    }

    function setProperty(id: string, name: string, value: any) {
        const node = _nodes[id];
        if (!node) return false;
        try { node[name] = value; return true; } catch { return false; }
    }

    function start(id: string, when: number, offset: number, duration: number) {
        const node = _nodes[id];
        const ctx = _ctx;
        if (!node?.start || !ctx) return false;
        const at = ctx.currentTime + (when > 0 ? when : 0);
        try {
            if (duration > 0) node.start(at, offset > 0 ? offset : 0, duration);
            else if (offset > 0) node.start(at, offset);
            else node.start(at);
            return true;
        } catch {
            // A scheduled source can only be started once, ever.
            return false;
        }
    }

    function stopNode(id: string, when: number) {
        const node = _nodes[id];
        const ctx = _ctx;
        if (!node?.stop || !ctx) return false;
        try { node.stop(ctx.currentTime + (when > 0 ? when : 0)); return true; }
        catch { return false; }
    }

    // --- Analysis ------------------------------------------------------------------------------

    function byteFrequencyData(id: string) {
        const node = _nodes[id];
        if (!node?.getByteFrequencyData) return null;
        const data = new Uint8Array(node.frequencyBinCount);
        node.getByteFrequencyData(data);
        return data;
    }

    function byteTimeDomainData(id: string) {
        const node = _nodes[id];
        if (!node?.getByteTimeDomainData) return null;
        const data = new Uint8Array(node.fftSize);
        node.getByteTimeDomainData(data);
        return data;
    }

    function floatFrequencyData(id: string) {
        const node = _nodes[id];
        if (!node?.getFloatFrequencyData) return null;
        const data = new Float32Array(node.frequencyBinCount);
        node.getFloatFrequencyData(data);
        // A Float32Array does not serialize as numbers, and the values are decibels, so precision
        // matters more here than the extra bytes.
        return Array.from(data);
    }

    function setListener(x: number, y: number, z: number, forwardX: number, forwardY: number, forwardZ: number, upX: number, upY: number, upZ: number) {
        const ctx = ensureCtx();
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
        const streamId = _nodeStreams[id];
        if (streamId !== undefined) {
            delete _nodeStreams[id];
            // The node's stream was parked in the mediaDevices registry; leaving it there after the
            // node is gone keeps a live stream nothing in .NET has a handle to stop any more.
            butil.mediaDevices.stop(streamId);
        }
    }

    async function dispose() {
        for (const id of Object.keys(_playbacks)) stop(id);
        for (const id of Object.keys(_nodes)) releaseNode(id);
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
