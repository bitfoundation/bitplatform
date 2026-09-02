var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.audioOutput = {
        isSupported() { return typeof (HTMLMediaElement.prototype as any).setSinkId === 'function'; },
        isSelectionSupported() { return typeof (navigator.mediaDevices as any)?.selectAudioOutput === 'function'; },
        getDevices,
        selectDevice,
        setSinkId,
        getSinkId
    };

    function media(element: any): HTMLMediaElement | null {
        return element && typeof element.play === 'function' ? element as HTMLMediaElement : null;
    }

    // Labels stay empty until the origin has been granted a device permission at least once -
    // enumerateDevices() never leaks hardware names to a page the user has not engaged with.
    async function getDevices() {
        if (!navigator.mediaDevices?.enumerateDevices) return [];
        try {
            const devices = await navigator.mediaDevices.enumerateDevices();
            return devices
                .filter(device => device.kind === 'audiooutput')
                .map(device => ({
                    deviceId: device.deviceId,
                    label: device.label ?? '',
                    groupId: device.groupId ?? ''
                }));
        } catch { return []; }
    }

    // The chooser: one device, picked by the user, and the only way to learn an output's label
    // without first asking for a microphone. Must run inside a user gesture.
    async function selectDevice() {
        const api = navigator.mediaDevices as any;
        if (typeof api?.selectAudioOutput !== 'function') return null;
        try {
            const device = await api.selectAudioOutput();
            return { deviceId: device.deviceId, label: device.label ?? '', groupId: device.groupId ?? '' };
        } catch { return null; } // the user dismissed the chooser
    }

    async function setSinkId(element: any, deviceId: string) {
        const el = media(element) as any;
        if (!el || typeof el.setSinkId !== 'function') return false;
        try {
            // '' routes back to the system default; anything else must be a device the origin is
            // permitted to use, or this rejects with NotAllowedError.
            await el.setSinkId(deviceId ?? '');
            return true;
        } catch { return false; }
    }

    function getSinkId(element: any) {
        const el = media(element) as any;
        return el ? (el.sinkId ?? '') : '';
    }
}(BitButil));
