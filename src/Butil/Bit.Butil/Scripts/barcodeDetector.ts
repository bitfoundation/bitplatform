var BitButil = BitButil || {};

(function (butil: any) {
    // One detector per requested format-set, because constructing one is not free and a scan loop
    // would otherwise build a fresh one every frame.
    const _detectors: { [key: string]: any } = {};
    const _scans: { [id: string]: number } = {};

    function detectorFor(formats: string[]) {
        const BD = (window as any).BarcodeDetector;
        if (typeof BD !== 'function') return null;

        const key = (formats ?? []).join(',');
        if (_detectors[key]) return _detectors[key];

        try {
            _detectors[key] = formats?.length ? new BD({ formats }) : new BD();
            return _detectors[key];
        } catch {
            // An unsupported format in the list rejects the whole constructor.
            return null;
        }
    }

    function read(result: any) {
        const box = result.boundingBox ?? {};
        return {
            rawValue: result.rawValue ?? '',
            format: result.format ?? '',
            x: box.x ?? 0,
            y: box.y ?? 0,
            width: box.width ?? 0,
            height: box.height ?? 0
        };
    }

    async function detectIn(source: any, formats: string[]) {
        const detector = detectorFor(formats);
        if (!detector || !source) return [];
        try {
            const results = await detector.detect(source);
            return results.map(read);
        } catch {
            // A video with no frame yet, or a detached element - not worth distinguishing.
            return [];
        }
    }

    butil.barcodeDetector = {
        isSupported() { return typeof (window as any).BarcodeDetector === 'function'; },
        async supportedFormats() {
            const BD = (window as any).BarcodeDetector;
            if (typeof BD?.getSupportedFormats !== 'function') return [];
            try { return await BD.getSupportedFormats(); } catch { return []; }
        },
        detect(element: any, formats: string[]) { return detectIn(element, formats); },
        async detectBytes(bytes: Uint8Array, mimeType: string, formats: string[]) {
            // createImageBitmap wants a Blob; going through one also means the caller can pass any
            // image format the browser can decode rather than raw pixels.
            let bitmap: any;
            try {
                const blob = new Blob([butil.utils.arrayToBuffer(bytes)], { type: mimeType || 'image/png' });
                bitmap = await createImageBitmap(blob);
            } catch {
                return [];
            }

            try {
                return await detectIn(bitmap, formats);
            } finally {
                // An ImageBitmap holds decoded pixels until it is closed - leaking one per scan
                // would be a real memory problem in a loop.
                try { bitmap.close?.(); } catch { /* already closed */ }
            }
        },
        // Scanning is a poll: there is no "barcode appeared" event, so the video is sampled on a
        // timer. requestAnimationFrame would be wasteful here - detection is far slower than a
        // frame, and a few scans a second is plenty for a user holding up a code.
        startScan(dotNetRef: any, scanId: string, element: any, formats: string[], intervalMs: number) {
            if (!butil.barcodeDetector.isSupported() || !element) return false;

            butil.barcodeDetector.stopScan(scanId);

            let running = false;
            const tick = async () => {
                // Skip rather than queue when the previous detect is still in flight, so a slow
                // device degrades to a lower scan rate instead of building a backlog.
                if (running) return;
                running = true;
                try {
                    const results = await detectIn(element, formats);
                    if (results.length && _scans[scanId] !== undefined) {
                        butil.utils.dispatch(dotNetRef, 'InvokeBarcodesDetected', scanId, results);
                    }
                } finally {
                    running = false;
                }
            };

            _scans[scanId] = setInterval(tick, Math.max(50, intervalMs)) as unknown as number;
            return true;
        },
        stopScan(scanId: string) {
            const handle = _scans[scanId];
            if (handle === undefined) return;
            delete _scans[scanId];
            clearInterval(handle);
        },
        disposeAll() {
            for (const id of Object.keys(_scans)) butil.barcodeDetector.stopScan(id);
        }
    };
}(BitButil));
