var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const VIDEO_ENCODER_KEYS = ['codec', 'width', 'height', 'displayWidth', 'displayHeight', 'bitrate', 'framerate',
        'hardwareAcceleration', 'alpha', 'scalabilityMode', 'bitrateMode', 'latencyMode', 'contentHint'];
    const VIDEO_DECODER_KEYS = ['codec', 'codedWidth', 'codedHeight', 'displayAspectWidth', 'displayAspectHeight',
        'hardwareAcceleration', 'optimizeForLatency'];
    const AUDIO_ENCODER_KEYS = ['codec', 'sampleRate', 'numberOfChannels', 'bitrate', 'bitrateMode'];
    const AUDIO_DECODER_KEYS = ['codec', 'sampleRate', 'numberOfChannels'];

    interface CodecEntry { codec: any; kind: string; }

    const _codecs: { [id: string]: CodecEntry } = {};
    const _frames: { [id: string]: any } = {};
    const _audio: { [id: string]: any } = {};

    butil.webCodecs = {
        isSupported() { return 'VideoEncoder' in window || 'VideoDecoder' in window || 'AudioEncoder' in window || 'AudioDecoder' in window; },
        isKindSupported(kind: string) { return !!constructorFor(kind); },
        isConfigSupported,
        create,
        configure,
        encodeFrame,
        encodeAudio,
        decode,
        flush,
        reset,
        state,
        queueSize,
        close,
        frameFromElement,
        frameFromBytes,
        frameInfo,
        drawFrame,
        copyFrame,
        closeFrame,
        createAudioData,
        audioInfo,
        copyAudio,
        closeAudio,
        disposeAll
    };

    function constructorFor(kind: string) {
        switch (kind) {
            case 'video-encoder': return (window as any).VideoEncoder;
            case 'video-decoder': return (window as any).VideoDecoder;
            case 'audio-encoder': return (window as any).AudioEncoder;
            case 'audio-decoder': return (window as any).AudioDecoder;
            default: return undefined;
        }
    }

    function toConfig(kind: string, config: any) {
        const keys = kind === 'video-encoder' ? VIDEO_ENCODER_KEYS
            : kind === 'video-decoder' ? VIDEO_DECODER_KEYS
                : kind === 'audio-encoder' ? AUDIO_ENCODER_KEYS
                    : AUDIO_DECODER_KEYS;
        const result = butil.utils.pick(config, keys);

        // A decoder's `description` is the codec's extradata (an avcC box, an Opus header). It is
        // binary rather than a plain member, so it is carried separately and rebuilt here.
        if (config?.description?.length) result.description = butil.utils.arrayToBuffer(config.description);
        return result;
    }

    async function isConfigSupported(kind: string, config: any) {
        const Ctor = constructorFor(kind);
        if (!Ctor?.isConfigSupported) return false;
        try {
            const result = await Ctor.isConfigSupported(toConfig(kind, config));
            return !!result?.supported;
        } catch {
            // A malformed configuration throws TypeError rather than answering "not supported".
            return false;
        }
    }

    function create(kind: string, id: string, config: any, dotNetRef: any, outputMethod: string, errorMethod: string) {
        const Ctor = constructorFor(kind);
        if (!Ctor) return false;

        close(id);

        try {
            const codec = new Ctor({
                output: (chunk: any, metadata: any) => onOutput(kind, id, chunk, metadata, dotNetRef, outputMethod),
                error: (e: any) => butil.utils.dispatch(dotNetRef, errorMethod, id, e?.message ?? String(e))
            });
            _codecs[id] = { codec, kind };
            // Configuring here rather than in a second call keeps the codec from ever being handed to
            // .NET in the unconfigured state, where every encode or decode would throw.
            codec.configure(toConfig(kind, config));
            return true;
        } catch {
            // An unsupported codec string, or a configuration the engine rejects outright.
            close(id);
            return false;
        }
    }

    function onOutput(kind: string, id: string, chunk: any, metadata: any, dotNetRef: any, method: string) {
        if (kind === 'video-encoder' || kind === 'audio-encoder') {
            const data = new Uint8Array(chunk.byteLength);
            chunk.copyTo(data);
            // The decoder configuration only ever accompanies the first chunk, and a decoder cannot
            // be built without its `description`, so it is forwarded with the chunk that carries it
            // rather than left for the caller to go looking for.
            const description = metadata?.decoderConfig?.description;
            const descriptionBytes = description
                ? (description instanceof Uint8Array ? description : new Uint8Array(description.buffer ?? description))
                : null;
            butil.utils.dispatch(dotNetRef, method, id, chunk.type, chunk.timestamp,
                typeof chunk.duration === 'number' ? chunk.duration : null, data, descriptionBytes);
            return;
        }

        // A decoder hands back a VideoFrame or an AudioData, both of which hold real memory (often a
        // GPU surface) until they are closed. They are parked here under an id so .NET can draw or
        // copy them, and the handle's disposal is what closes them.
        const outputId = newId();
        if (kind === 'video-decoder') {
            _frames[outputId] = chunk;
            butil.utils.dispatch(dotNetRef, method, id, outputId, chunk.timestamp,
                typeof chunk.duration === 'number' ? chunk.duration : null,
                chunk.displayWidth ?? chunk.codedWidth ?? 0, chunk.displayHeight ?? chunk.codedHeight ?? 0,
                chunk.format ?? '');
        } else {
            _audio[outputId] = chunk;
            butil.utils.dispatch(dotNetRef, method, id, outputId, chunk.timestamp,
                typeof chunk.duration === 'number' ? chunk.duration : null,
                chunk.sampleRate ?? 0, chunk.numberOfFrames ?? 0, chunk.numberOfChannels ?? 0,
                chunk.format ?? '');
        }
    }

    function configure(id: string, config: any) {
        const entry = _codecs[id];
        if (!entry) return false;
        try { entry.codec.configure(toConfig(entry.kind, config)); return true; } catch { return false; }
    }

    function encodeFrame(id: string, frameId: string, keyFrame: boolean) {
        const entry = _codecs[id];
        const frame = _frames[frameId];
        if (!entry || !frame) return false;
        try { entry.codec.encode(frame, keyFrame ? { keyFrame: true } : undefined); return true; }
        catch { return false; }
    }

    function encodeAudio(id: string, audioId: string) {
        const entry = _codecs[id];
        const data = _audio[audioId];
        if (!entry || !data) return false;
        try { entry.codec.encode(data); return true; } catch { return false; }
    }

    function decode(id: string, type: string, timestamp: number, duration: number | null, data: Uint8Array) {
        const entry = _codecs[id];
        if (!entry) return false;
        const ChunkCtor: any = entry.kind === 'video-decoder' ? (window as any).EncodedVideoChunk : (window as any).EncodedAudioChunk;
        if (!ChunkCtor) return false;
        try {
            const init: any = { type, timestamp, data: butil.utils.arrayToBuffer(data) };
            if (duration !== null && duration !== undefined) init.duration = duration;
            entry.codec.decode(new ChunkCtor(init));
            return true;
        } catch {
            // A delta chunk before the first key frame, or a codec that is closed or unconfigured.
            return false;
        }
    }

    async function flush(id: string) {
        const entry = _codecs[id];
        if (!entry) return false;
        try { await entry.codec.flush(); return true; }
        catch { return false; }  // reset() and close() reject a pending flush by design
    }

    function reset(id: string) {
        const entry = _codecs[id];
        if (!entry) return;
        try { entry.codec.reset(); } catch { /* already closed */ }
    }

    function state(id: string) { return _codecs[id]?.codec.state ?? 'closed'; }

    function queueSize(id: string) { return _codecs[id]?.codec.encodeQueueSize ?? _codecs[id]?.codec.decodeQueueSize ?? 0; }

    function close(id: string) {
        const entry = _codecs[id];
        if (!entry) return;
        delete _codecs[id];
        try { entry.codec.close(); } catch { /* already closed */ }
    }

    function frameFromElement(frameId: string, element: any, timestamp: number, duration: number | null) {
        const Ctor: any = (window as any).VideoFrame;
        if (!Ctor || !element) return null;
        try {
            const init: any = { timestamp };
            if (duration !== null && duration !== undefined) init.duration = duration;
            const frame = new Ctor(element, init);
            _frames[frameId] = frame;
            return describeFrame(frame);
        } catch {
            // A video element with no current frame, or a canvas of zero size.
            return null;
        }
    }

    function frameFromBytes(frameId: string, format: string, width: number, height: number, timestamp: number, duration: number | null, data: Uint8Array) {
        const Ctor: any = (window as any).VideoFrame;
        if (!Ctor) return null;
        try {
            const init: any = { format, codedWidth: width, codedHeight: height, timestamp };
            if (duration !== null && duration !== undefined) init.duration = duration;
            const frame = new Ctor(butil.utils.arrayToBuffer(data), init);
            _frames[frameId] = frame;
            return describeFrame(frame);
        } catch {
            // Wrong buffer size for the format and dimensions, or an unknown pixel format.
            return null;
        }
    }

    function describeFrame(frame: any) {
        return {
            timestamp: frame.timestamp ?? 0,
            duration: typeof frame.duration === 'number' ? frame.duration : null,
            width: frame.displayWidth ?? frame.codedWidth ?? 0,
            height: frame.displayHeight ?? frame.codedHeight ?? 0,
            format: frame.format ?? ''
        };
    }

    function frameInfo(frameId: string) {
        const frame = _frames[frameId];
        return frame ? describeFrame(frame) : null;
    }

    function drawFrame(frameId: string, canvas: any) {
        const frame = _frames[frameId];
        if (!frame || !canvas?.getContext) return false;
        try {
            const context = canvas.getContext('2d');
            if (!context) return false;
            // Sizing the canvas to the frame is what a caller almost always wants, and getting it
            // wrong silently crops - so it is done here rather than left to the page's CSS.
            if (canvas.width !== frame.displayWidth) canvas.width = frame.displayWidth;
            if (canvas.height !== frame.displayHeight) canvas.height = frame.displayHeight;
            context.drawImage(frame, 0, 0);
            return true;
        } catch {
            return false;
        }
    }

    async function copyFrame(frameId: string) {
        const frame = _frames[frameId];
        if (!frame?.copyTo) return null;
        try {
            const buffer = new Uint8Array(frame.allocationSize());
            await frame.copyTo(buffer);
            return buffer;
        } catch {
            return null;
        }
    }

    function closeFrame(frameId: string) {
        const frame = _frames[frameId];
        if (!frame) return;
        delete _frames[frameId];
        try { frame.close(); } catch { /* already closed */ }
    }

    function createAudioData(audioId: string, format: string, sampleRate: number, numberOfFrames: number, numberOfChannels: number, timestamp: number, data: Uint8Array) {
        const Ctor: any = (window as any).AudioData;
        if (!Ctor) return null;
        try {
            const audio = new Ctor({
                format, sampleRate, numberOfFrames, numberOfChannels, timestamp,
                data: butil.utils.arrayToBuffer(data)
            });
            _audio[audioId] = audio;
            return describeAudio(audio);
        } catch {
            // The buffer does not hold numberOfFrames * numberOfChannels samples of this format.
            return null;
        }
    }

    function describeAudio(audio: any) {
        return {
            timestamp: audio.timestamp ?? 0,
            duration: typeof audio.duration === 'number' ? audio.duration : null,
            sampleRate: audio.sampleRate ?? 0,
            numberOfFrames: audio.numberOfFrames ?? 0,
            numberOfChannels: audio.numberOfChannels ?? 0,
            format: audio.format ?? ''
        };
    }

    function audioInfo(audioId: string) {
        const audio = _audio[audioId];
        return audio ? describeAudio(audio) : null;
    }

    function copyAudio(audioId: string, planeIndex: number) {
        const audio = _audio[audioId];
        if (!audio?.copyTo) return null;
        try {
            const buffer = new Uint8Array(audio.allocationSize({ planeIndex }));
            audio.copyTo(buffer, { planeIndex });
            return buffer;
        } catch {
            // A plane index the format does not have - interleaved formats only have plane 0.
            return null;
        }
    }

    function closeAudio(audioId: string) {
        const audio = _audio[audioId];
        if (!audio) return;
        delete _audio[audioId];
        try { audio.close(); } catch { /* already closed */ }
    }

    function disposeAll() {
        for (const id of Object.keys(_codecs)) close(id);
        for (const id of Object.keys(_frames)) closeFrame(id);
        for (const id of Object.keys(_audio)) closeAudio(id);
    }

    function newId() {
        const uuid = (crypto as any).randomUUID?.();
        if (uuid) return uuid;
        const bytes = new Uint8Array(16);
        crypto.getRandomValues(bytes);
        const hex = Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('');
        return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
    }

    // Decoded frames and audio hold memory the garbage collector cannot reclaim on its own, so a
    // page that goes away mid-decode would otherwise leak them until the tab is closed.
    window.addEventListener('pagehide', () => butil.webCodecs.disposeAll());
}(BitButil));
