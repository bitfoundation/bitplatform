var BitButil = BitButil || {};

(function (butil: any) {
    const _streams: { [id: string]: MediaStream } = {};

    butil.mediaDevices = {
        isSupported() { return !!(window.navigator as any).mediaDevices; },
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
