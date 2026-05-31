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
            try { stream.getTracks().forEach(t => t.stop()); } catch { /* ignore */ }
        }
    };
}(BitButil));
