var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // What a source element is intrinsically, which is not what CSS displays it at: a video laid
    // out at 320px wide is still 1920 pixels of picture, and a thumbnail wants the pixels.
    function intrinsicSize(source: any): { width: number; height: number } {
        if (!source) return { width: 0, height: 0 };
        if (typeof source.videoWidth === 'number') {
            // videoWidth is filled in at HAVE_METADATA, but drawing a video before HAVE_CURRENT_DATA
            // is a specified no-op - so a frame-less video reports no size rather than letting the
            // caller encode a blank picture and believe it captured something.
            if (source.videoWidth <= 0 || (source.readyState ?? 0) < 2) return { width: 0, height: 0 };
            return { width: source.videoWidth, height: source.videoHeight };
        }
        if (typeof source.naturalWidth === 'number' && source.naturalWidth > 0)
            return { width: source.naturalWidth, height: source.naturalHeight };
        return { width: source.width ?? 0, height: source.height ?? 0 };
    }

    function contextOf(canvas: any): CanvasRenderingContext2D | null {
        if (!canvas || typeof canvas.getContext !== 'function') return null;
        try { return canvas.getContext('2d'); } catch { return null; }
    }

    // A canvas the caller never sees, for capture-and-encode in one call. OffscreenCanvas keeps it
    // off the DOM entirely where it exists; the detached element is the same picture either way.
    function scratchCanvas(width: number, height: number) {
        if (typeof (window as any).OffscreenCanvas === 'function') return new (window as any).OffscreenCanvas(width, height);
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        return canvas;
    }

    async function encodeBlob(canvas: any, type: string, quality: number): Promise<Blob | null> {
        try {
            // Reading pixels back out of a canvas that drew a cross-origin image throws
            // SecurityError - the canvas is "tainted", and no amount of trying reads it. Null says
            // so, because it is a normal outcome of drawing someone else's picture.
            return typeof canvas.convertToBlob === 'function'
                ? await canvas.convertToBlob({ type, quality })
                : await new Promise(resolve => canvas.toBlob(resolve, type, quality));
        } catch {
            return null;
        }
    }

    async function encode(canvas: any, type: string, quality: number): Promise<Uint8Array | null> {
        const blob = await encodeBlob(canvas, type, quality);
        if (!blob) return null;
        try { return new Uint8Array(await blob.arrayBuffer()); } catch { return null; }
    }

    // Draws the source into a scratch canvas at the requested size, or null when there is nothing to
    // draw yet. Shared by capture() and captureToDataUrl() so the sizing rule lives in one place.
    function captureCanvas(source: any, width: number, height: number): any | null {
        const intrinsic = intrinsicSize(source);
        if (intrinsic.width <= 0 || intrinsic.height <= 0) return null;   // nothing to capture yet

        // A width or height of 0 means "keep the aspect ratio from the other one", and both
        // zero means the source's own size - so a thumbnail is one number, not two.
        let targetWidth = width > 0 ? width : 0;
        let targetHeight = height > 0 ? height : 0;
        if (targetWidth === 0 && targetHeight === 0) {
            targetWidth = intrinsic.width;
            targetHeight = intrinsic.height;
        } else if (targetWidth === 0) {
            targetWidth = Math.max(1, Math.round(intrinsic.width * (targetHeight / intrinsic.height)));
        } else if (targetHeight === 0) {
            targetHeight = Math.max(1, Math.round(intrinsic.height * (targetWidth / intrinsic.width)));
        }

        const canvas = scratchCanvas(targetWidth, targetHeight);
        const context = contextOf(canvas);
        if (!context) return null;

        try {
            context.drawImage(source, 0, 0, intrinsic.width, intrinsic.height, 0, 0, targetWidth, targetHeight);
        } catch {
            return null;
        }

        return canvas;
    }

    butil.canvas = {
        isSupported() { return typeof document.createElement('canvas').getContext === 'function'; },
        isOffscreenSupported() { return typeof (window as any).OffscreenCanvas === 'function'; },
        isWebGlSupported() {
            try { return document.createElement('canvas').getContext('webgl2') !== null; }
            catch { return false; }
        },
        isWebGpuSupported() { return typeof (navigator as any).gpu?.requestAdapter === 'function'; },

        // The pixel buffer's size, which is what drawing and exporting use. CSS may be displaying it
        // at some other size entirely, and the two disagreeing is the usual cause of a blurry canvas.
        size(canvas: any) {
            // getContext is the test for "is a canvas" everywhere in this module: the documented
            // answer for anything else is null, not a fabricated size.
            if (!canvas || typeof canvas.getContext !== 'function') return null;
            const rect = typeof canvas.getBoundingClientRect === 'function' ? canvas.getBoundingClientRect() : null;
            return {
                width: canvas.width ?? 0,
                height: canvas.height ?? 0,
                cssWidth: rect?.width ?? 0,
                cssHeight: rect?.height ?? 0,
                devicePixelRatio: window.devicePixelRatio || 1
            };
        },

        setSize(canvas: any, width: number, height: number) {
            if (!canvas || typeof canvas.getContext !== 'function') return false;
            // Assigning either dimension clears the canvas - the specification's rule, and a common
            // surprise, so it is worth knowing rather than working around.
            canvas.width = width;
            canvas.height = height;
            return true;
        },

        clear(canvas: any) {
            const context = contextOf(canvas);
            if (!context) return false;
            context.clearRect(0, 0, canvas.width, canvas.height);
            return true;
        },

        // The one drawing operation this slice provides: everything else here exists to get a
        // picture in and bytes out.
        drawImage(canvas: any, source: any, options: any) {
            const context = contextOf(canvas);
            if (!context || !source) return false;

            const intrinsic = intrinsicSize(source);
            const sourceX = options?.sourceX ?? 0;
            const sourceY = options?.sourceY ?? 0;
            const sourceWidth = options?.sourceWidth ?? intrinsic.width;
            const sourceHeight = options?.sourceHeight ?? intrinsic.height;
            const destinationX = options?.destinationX ?? 0;
            const destinationY = options?.destinationY ?? 0;
            const destinationWidth = options?.destinationWidth ?? canvas.width;
            const destinationHeight = options?.destinationHeight ?? canvas.height;

            if (sourceWidth <= 0 || sourceHeight <= 0) return false;   // a video with no frame yet

            try {
                context.drawImage(source, sourceX, sourceY, sourceWidth, sourceHeight,
                    destinationX, destinationY, destinationWidth, destinationHeight);
                return true;
            } catch {
                return false;
            }
        },

        toDataUrl(canvas: any, type: string, quality: number) {
            if (!canvas || typeof canvas.toDataURL !== 'function') return null;
            try { return canvas.toDataURL(type, quality); } catch { return null; } // tainted
        },

        toBytes(canvas: any, type: string, quality: number) {
            if (!canvas) return null;
            return encode(canvas, type, quality);
        },

        // Capture and encode in one step, with no canvas in the caller's markup: the frame is drawn
        // into a scratch canvas that is discarded afterwards.
        async capture(source: any, width: number, height: number, type: string, quality: number) {
            const canvas = captureCanvas(source, width, height);
            return canvas ? await encode(canvas, type, quality) : null;
        },

        async captureToDataUrl(source: any, width: number, height: number, type: string, quality: number) {
            // Drawing through the same helper as capture() keeps one path, so the two capture
            // methods can never disagree about the picture.
            const canvas = captureCanvas(source, width, height);
            if (!canvas) return null;

            const blob = await encodeBlob(canvas, type, quality);
            if (!blob) return null;

            // readAsDataURL encodes natively and stamps the blob's own type - not the requested one,
            // which matters because a format the engine cannot encode falls back to image/png
            // silently, and a data URL claiming otherwise is a lie a decoder acts on. Building the
            // base64 by hand would mean holding several megabytes of intermediate string per frame.
            try {
                return await new Promise<string | null>(resolve => {
                    const reader = new FileReader();
                    reader.onload = () => resolve(typeof reader.result === 'string' ? reader.result : null);
                    reader.onerror = () => resolve(null);
                    reader.readAsDataURL(blob);
                });
            } catch { return null; }
        }
    };
}(BitButil));
