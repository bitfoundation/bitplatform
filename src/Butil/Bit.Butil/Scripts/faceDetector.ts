var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One detector per option-set: constructing one loads the platform's face model, which is far
    // more expensive than the detection itself.
    const _detectors: { [key: string]: any } = {};

    function detectorFor(maxFaces: number, fastMode: boolean) {
        const FD = (window as any).FaceDetector;
        if (typeof FD !== 'function') return null;

        const key = `${maxFaces}|${fastMode}`;
        if (_detectors[key]) return _detectors[key];

        try {
            const options: any = {};
            if (maxFaces > 0) options.maxDetectedFaces = maxFaces;
            if (fastMode) options.fastMode = true;
            _detectors[key] = new FD(options);
            return _detectors[key];
        } catch {
            return null;
        }
    }

    function read(result: any) {
        const box = result.boundingBox ?? {};
        return {
            x: box.x ?? 0,
            y: box.y ?? 0,
            width: box.width ?? 0,
            height: box.height ?? 0,
            // Landmarks are optional and platform-dependent - Android reports eyes and mouth, macOS
            // often reports none at all - so they are flattened to a name plus its first point,
            // which is what a caller can actually draw without knowing the platform.
            landmarks: (result.landmarks ?? []).map((landmark: any) => ({
                type: landmark.type ?? '',
                x: landmark.locations?.[0]?.x ?? 0,
                y: landmark.locations?.[0]?.y ?? 0,
                pointCount: landmark.locations?.length ?? 0
            }))
        };
    }

    async function detectIn(source: any, maxFaces: number, fastMode: boolean) {
        const detector = detectorFor(maxFaces, fastMode);
        if (!detector || !source) return [];
        try {
            const results = await detector.detect(source);
            return results.map(read);
        } catch {
            // A video with no frame yet, or a detached element - not worth distinguishing.
            return [];
        }
    }

    butil.faceDetector = {
        isSupported() { return typeof (window as any).FaceDetector === 'function'; },
        detect(element: any, maxFaces: number, fastMode: boolean) { return detectIn(element, maxFaces, fastMode); },
        async detectBytes(bytes: Uint8Array, mimeType: string, maxFaces: number, fastMode: boolean) {
            let bitmap: any;
            try {
                const blob = new Blob([butil.utils.arrayToBuffer(bytes)], { type: mimeType || 'image/png' });
                bitmap = await createImageBitmap(blob);
            } catch {
                return [];
            }

            try {
                return await detectIn(bitmap, maxFaces, fastMode);
            } finally {
                // An ImageBitmap holds decoded pixels until it is closed.
                try { bitmap.close?.(); } catch { /* already closed */ }
            }
        }
    };
}(BitButil));
