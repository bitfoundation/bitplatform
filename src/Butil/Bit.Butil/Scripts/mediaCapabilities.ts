var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const VIDEO_KEYS = ['contentType', 'width', 'height', 'bitrate', 'framerate', 'hasAlphaChannel', 'hdrMetadataType', 'colorGamut', 'transferFunction', 'scalabilityMode', 'spatialScalability'];
    const AUDIO_KEYS = ['contentType', 'channels', 'bitrate', 'samplerate', 'spatialRendering'];

    butil.mediaCapabilities = {
        isSupported() { return !!(navigator as any).mediaCapabilities?.decodingInfo; },
        decodingInfo(config: any) { return query('decodingInfo', config); },
        encodingInfo(config: any) { return query('encodingInfo', config); }
    };

    async function query(method: string, config: any) {
        const capabilities = (navigator as any).mediaCapabilities;
        if (!capabilities?.[method]) return null;

        const request: any = { type: config?.type };
        // Only the members the caller actually set are sent - the spec requires at least one of
        // them. A member that is present but missing its contentType is still forwarded: dropping it
        // would silently turn a malformed query into a valid audio- or video-only one and answer a
        // question nobody asked, where forwarding it gets the TypeError the caller should see.
        if (config?.video) request.video = butil.utils.pick(config.video, VIDEO_KEYS);
        if (config?.audio) request.audio = butil.utils.pick(config.audio, AUDIO_KEYS);
        if (config?.keySystemConfiguration?.keySystem) {
            request.keySystemConfiguration = butil.utils.pick(config.keySystemConfiguration,
                ['keySystem', 'initDataType', 'distinctiveIdentifier', 'persistentState', 'sessionTypes']);
            if (config.keySystemConfiguration.audioRobustness) request.keySystemConfiguration.audio = { robustness: config.keySystemConfiguration.audioRobustness };
            if (config.keySystemConfiguration.videoRobustness) request.keySystemConfiguration.video = { robustness: config.keySystemConfiguration.videoRobustness };
        }

        try {
            const info = await capabilities[method](request);
            return {
                supported: !!info.supported,
                smooth: !!info.smooth,
                powerEfficient: !!info.powerEfficient,
                // The access object itself can't cross the interop boundary; whether the browser
                // handed one back is the part a caller can act on (it means the DRM configuration
                // in the query is satisfiable, so EME can be set up for this exact codec).
                keySystemAccessible: !!info.keySystemAccess
            };
        } catch {
            // A configuration the spec rejects (unknown type, malformed contentType, no video and
            // no audio) throws TypeError rather than reporting `supported: false`.
            return null;
        }
    }
}(BitButil));
