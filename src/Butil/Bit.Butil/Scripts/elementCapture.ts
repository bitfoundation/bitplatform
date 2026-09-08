var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function videoTrack(streamId: string) {
        // The stream lives in mediaDevices; .NET only ever holds its id.
        return butil.mediaDevices.getStream(streamId)?.getVideoTracks()[0] ?? null;
    }

    butil.elementCapture = {
        isSupported() { return typeof (window as any).RestrictionTarget?.fromElement === 'function'; },

        async restrictTo(streamId: string, element: Element) {
            const track: any = videoTrack(streamId);
            const RestrictionTarget = (window as any).RestrictionTarget;
            if (!track || !element || typeof RestrictionTarget?.fromElement !== 'function') return false;
            if (typeof track.restrictTo !== 'function') return false;

            try {
                const target = await RestrictionTarget.fromElement(element);
                await track.restrictTo(target);
                return true;
            } catch {
                // Restriction only applies to a capture of this very tab, and the element has to form
                // its own stacking context - the browser rejects it otherwise.
                return false;
            }
        },

        // Back to the whole captured surface. Passing null is the spec's own way of saying so.
        async clear(streamId: string) {
            const track: any = videoTrack(streamId);
            if (typeof track?.restrictTo !== 'function') return false;
            try { await track.restrictTo(null); return true; } catch { return false; }
        }
    };
}(BitButil));
