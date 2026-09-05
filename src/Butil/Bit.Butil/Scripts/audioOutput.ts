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

    // The outputs among everything mediaDevices.ts enumerates - the same objects, in the same shape,
    // so a device from either service can be handed to setSinkId without being re-fetched. Labels
    // stay empty until the origin has been granted a device permission at least once:
    // enumerateDevices() never leaks hardware names to a page the user has not engaged with.
    async function getDevices() {
        const devices = await butil.mediaDevices.enumerate();
        return devices.filter((device: any) => device.kind === 'audiooutput');
    }

    // The chooser: one device, picked by the user, and the only way to learn an output's label
    // without first asking for a microphone. Must run inside a user gesture.
    async function selectDevice() {
        const api = navigator.mediaDevices as any;
        if (typeof api?.selectAudioOutput !== 'function') return null;
        try {
            const device = await api.selectAudioOutput();
            // kind is filled in rather than read back: selectAudioOutput only ever returns an
            // output, and the caller gets the same MediaDeviceInfo shape enumerating produces.
            return { deviceId: device.deviceId, kind: device.kind ?? 'audiooutput', label: device.label ?? '', groupId: device.groupId ?? '' };
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
