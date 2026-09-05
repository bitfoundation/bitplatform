var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One detector for the whole page: TextDetector takes no options, and constructing one loads the
    // platform's OCR model.
    let _detector: any = null;

    function detector() {
        const TD = (window as any).TextDetector;
        if (typeof TD !== 'function') return null;
        if (_detector) return _detector;
        try { _detector = new TD(); return _detector; } catch { return null; }
    }

    function read(result: any) {
        const box = result.boundingBox ?? {};
        return {
            rawValue: result.rawValue ?? '',
            x: box.x ?? 0,
            y: box.y ?? 0,
            width: box.width ?? 0,
            height: box.height ?? 0
        };
    }

    async function detectIn(source: any) {
        const found = detector();
        if (!found || !source) return [];
        try {
            const results = await found.detect(source);
            return results.map(read);
        } catch {
            // A video with no frame yet, or a detached element - not worth distinguishing.
            return [];
        }
    }

    butil.textDetector = {
        isSupported() { return typeof (window as any).TextDetector === 'function'; },
        detect(element: any) { return detectIn(element); },
        async detectBytes(bytes: Uint8Array, mimeType: string) {
            let bitmap: any;
            try {
                const blob = new Blob([butil.utils.arrayToBuffer(bytes)], { type: mimeType || 'image/png' });
                bitmap = await createImageBitmap(blob);
            } catch {
                return [];
            }

            try {
                return await detectIn(bitmap);
            } finally {
                // An ImageBitmap holds decoded pixels until it is closed.
                try { bitmap.close?.(); } catch { /* already closed */ }
            }
        }
    };
}(BitButil));
