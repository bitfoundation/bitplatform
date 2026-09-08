var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function videoTrack(streamId: string) {
        // The stream lives in mediaDevices; .NET only ever holds its id.
        return butil.mediaDevices.getStream(streamId)?.getVideoTracks()[0] ?? null;
    }

    butil.regionCapture = {
        isSupported() { return typeof (window as any).CropTarget?.fromElement === 'function'; },

        async cropTo(streamId: string, element: Element) {
            const track: any = videoTrack(streamId);
            const CropTarget = (window as any).CropTarget;
            if (!track || !element || typeof CropTarget?.fromElement !== 'function') return false;
            if (typeof track.cropTo !== 'function') return false;

            try {
                const target = await CropTarget.fromElement(element);
                await track.cropTo(target);
                return true;
            } catch {
                // Cropping only applies to a capture of this very tab; a screen or window share
                // rejects it, as does an element that is not rendered.
                return false;
            }
        },

        // Back to the whole captured surface. Passing null is the spec's own way of saying so.
        async clear(streamId: string) {
            const track: any = videoTrack(streamId);
            if (typeof track?.cropTo !== 'function') return false;
            try { await track.cropTo(null); return true; } catch { return false; }
        }
    };
}(BitButil));
