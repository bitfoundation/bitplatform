var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _streams: { [id: string]: MediaStream } = {};

    butil.mediaDevices = {
        isSupported() { return !!(window.navigator as any).mediaDevices; },
        // Cross-module accessor: mediaRecorder.ts records a stream this module owns, and the
        // handle .NET passes around is only ever the id. Not called from .NET.
        getStream(id: string) { return _streams[id]; },
        // The other direction: webAudio.ts produces a stream of its own (a MediaStreamAudioDestination)
        // and parks it here so that .NET can treat it like any other stream - attach it to an element,
        // or hand it to MediaRecorder. Not called from .NET.
        registerStream(id: string, stream: MediaStream) {
            stopStream(_streams[id]);
            _streams[id] = stream;
        },
        async enumerate() {
            const md = (window.navigator as any).mediaDevices;
            if (!md?.enumerateDevices) return [];
            try {
                const list = await md.enumerateDevices();
                return list.map((d: any) => ({
                    deviceId: d.deviceId,
                    kind: d.kind,
                    label: d.label,
                    groupId: d.groupId
                }));
            } catch {
                return [];
            }
        },
        async getUserMedia(id: string, audio: boolean, video: boolean, audioConstraints: any, videoConstraints: any) {
            const md = (window.navigator as any).mediaDevices;
            if (!md?.getUserMedia) return false;
            const constraints: MediaStreamConstraints = {};
            constraints.audio = audio ? (audioConstraints ?? true) : false;
            constraints.video = video ? (videoConstraints ?? true) : false;
            try {
                const stream = await md.getUserMedia(constraints);
                // Stop any prior stream stored under this id before overwriting, otherwise the old
                // camera/mic tracks stay live (hardware on, indicator lit) with no handle left to stop them.
                stopStream(_streams[id]);
                _streams[id] = stream;
                return true;
            } catch {
                return false;
            }
        },
        isDisplaySupported() { return !!(window.navigator as any).mediaDevices?.getDisplayMedia; },
        async getDisplayMedia(id: string, audio: boolean, videoConstraints: any, options: any) {
            const md = (window.navigator as any).mediaDevices;
            if (!md?.getDisplayMedia) return false;
            const constraints: any = { video: videoConstraints ?? true, audio };
            // The picker-shaping members live on the constraints object itself, not under video.
            // Undefined ones are dropped so a runtime that doesn't know a member isn't handed one.
            if (options) {
                for (const key of ['displaySurface', 'selfBrowserSurface', 'surfaceSwitching', 'systemAudio', 'monitorTypeSurfaces', 'preferCurrentTab']) {
                    if (options[key] !== null && options[key] !== undefined) constraints[key] = options[key];
                }
            }
            try {
                const stream = await md.getDisplayMedia(constraints);
                // Same reasoning as getUserMedia: never orphan a previous stream under this id.
                stopStream(_streams[id]);
                _streams[id] = stream;
                return true;
            } catch {
                // The user dismissed the picker, or the embedder blocks display-capture.
                return false;
            }
        },
        // The user can end a screen share from the browser's own "Stop sharing" bar, which we only
        // learn about through the track's 'ended' event - .NET has no other way to notice.
        onDisplayEnded(id: string, dotNetRef: any, method: string) {
            const stream = _streams[id];
            if (!stream) return;
            const notify = () => butil.utils.dispatch(dotNetRef, method, id);
            stream.getVideoTracks().forEach(t => t.addEventListener('ended', notify, { once: true }));
        },
        getDisplaySettings(id: string) {
            const track = _streams[id]?.getVideoTracks()[0];
            if (!track) return null;
            const s: any = track.getSettings?.() ?? {};
            return {
                label: track.label ?? '',
                displaySurface: s.displaySurface ?? '',
                width: s.width ?? 0,
                height: s.height ?? 0,
                frameRate: s.frameRate ?? 0
            };
        },
        attach(id: string, element: HTMLMediaElement) {
            const stream = _streams[id];
            if (!stream || !element) return;
            (element as any).srcObject = stream;
        },
        setEnabled(id: string, enabled: boolean) {
            const stream = _streams[id];
            if (!stream) return;
            stream.getTracks().forEach(t => { t.enabled = enabled; });
        },
        stop(id: string) {
            const stream = _streams[id];
            if (!stream) return;
            delete _streams[id];
            stopStream(stream);
        },
        disposeAll() {
            // Safety net for streams whose MediaStreamHandle was never disposed (e.g. circuit/page
            // teardown): stop every remaining track so the camera/mic can't stay live after we're gone.
            for (const id in _streams) {
                stopStream(_streams[id]);
            }
            for (const id in _streams) {
                delete _streams[id];
            }
        }
    };

    function stopStream(stream: MediaStream | undefined) {
        if (!stream) return;
        try { stream.getTracks().forEach(t => t.stop()); } catch { /* ignore */ }
    }

    // Last-chance teardown: pagehide fires on navigation/tab close/bfcache, covering the case where a
    // handle is leaked. Stops the hardware even if .NET never gets a chance to call stop/disposeAll.
    window.addEventListener('pagehide', () => butil.mediaDevices.disposeAll());
}(BitButil));
