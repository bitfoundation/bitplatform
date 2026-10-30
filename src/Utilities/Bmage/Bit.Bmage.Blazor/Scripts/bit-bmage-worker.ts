// Bmage codec worker.
//
// Decoding, encoding and resizing with the browser's own codecs, off the page's thread. bit-bmage.js starts a few of
// these and hands them work; each message carries its buffers as transferables, so a 12 megapixel image moves between
// the page and a worker without being copied. A classic script rather than a module, so it starts in every browser
// that has workers at all, and wrapped in a function so nothing it declares reaches the global scope of the page's
// own script when TypeScript checks both together.

(() => {
    // The page's typings are the DOM's, not a worker's; the few worker globals used here are typed loosely.
    const scope: any = self;

    interface DecodeOptions {
        mimeType?: string;
        maxWidth?: number;
        maxHeight?: number;
        sourceWidth?: number;
        sourceHeight?: number;
        maxPixels?: number;
        autoOrient?: boolean;
    }

    /** What the page asks for. The input buffer is transferred in and, when the work fails, transferred back. */
    type Request =
        | { id: number; op: 'probe' }
        | { id: number; op: 'decode'; input: ArrayBuffer; options: DecodeOptions }
        | { id: number; op: 'encode'; input: ArrayBuffer; width: number; height: number; mimeType: string; quality?: number; background?: string }
        | { id: number; op: 'scale'; input: ArrayBuffer; width: number; height: number; targetWidth: number; targetHeight: number };

    function context(canvas: OffscreenCanvas, read: boolean): OffscreenCanvasRenderingContext2D {
        const ctx = canvas.getContext('2d', { willReadFrequently: read }) as OffscreenCanvasRenderingContext2D | null;
        if (!ctx) throw new Error('The browser refused a 2D canvas context. The image may exceed the platform canvas limit.');
        return ctx;
    }

    /** The same decode-time resize hint the page's own path uses: one axis only, so the aspect ratio is kept. */
    function resizeHint(options: DecodeOptions): ImageBitmapOptions | null {
        const maxW = options.maxWidth;
        const maxH = options.maxHeight;
        if (!maxW || !maxH) return maxW ? { resizeWidth: maxW } : maxH ? { resizeHeight: maxH } : null;
        const sw = options.sourceWidth;
        const sh = options.sourceHeight;
        if (!sw || !sh) return null;
        if (sw <= maxW && sh <= maxH) return null;
        return (maxW / sw) <= (maxH / sh) ? { resizeWidth: maxW } : { resizeHeight: maxH };
    }

    async function decode(input: ArrayBuffer, options: DecodeOptions) {
        const blob = new Blob([input], options.mimeType ? { type: options.mimeType } : undefined);
        const init: ImageBitmapOptions = { imageOrientation: options.autoOrient === false ? 'none' : 'from-image' };
        const hint = resizeHint(options);
        if (hint) {
            Object.assign(init, hint);
            init.resizeQuality = 'high';
        }
        let bitmap: ImageBitmap;
        try {
            bitmap = await createImageBitmap(blob, init);
        } catch {
            // Some browsers reject the options bag; a failure here too is reported, and the page decodes instead.
            bitmap = await createImageBitmap(blob);
        }
        try {
            let { width, height } = bitmap;
            if (!width || !height) throw new Error('The decoded image has no dimensions.');
            if (options.maxWidth && options.maxHeight) {
                const scale = Math.min(options.maxWidth / width, options.maxHeight / height, 1);
                if (scale < 1) {
                    width = Math.max(1, Math.round(width * scale));
                    height = Math.max(1, Math.round(height * scale));
                }
            }
            if (options.maxPixels && width * height > options.maxPixels) {
                const scale = Math.sqrt(options.maxPixels / (width * height));
                width = Math.max(1, Math.floor(width * scale));
                height = Math.max(1, Math.floor(height * scale));
            }
            const canvas = new OffscreenCanvas(width, height);
            try {
                const ctx = context(canvas, true);
                ctx.imageSmoothingQuality = 'high';
                ctx.drawImage(bitmap, 0, 0, width, height);
                const data = ctx.getImageData(0, 0, width, height).data;
                return { data: data.buffer as ArrayBuffer, width, height };
            } finally {
                canvas.width = 0;
                canvas.height = 0;
            }
        } finally {
            bitmap.close();
        }
    }

    async function encode(input: ArrayBuffer, width: number, height: number, mimeType: string, quality?: number, background?: string) {
        const canvas = new OffscreenCanvas(width, height);
        try {
            const ctx = context(canvas, false);
            ctx.putImageData(new ImageData(new Uint8ClampedArray(input), width, height), 0, 0);
            if (background) {
                ctx.globalCompositeOperation = 'destination-over';
                ctx.fillStyle = background;
                ctx.fillRect(0, 0, width, height);
                ctx.globalCompositeOperation = 'source-over';
            }
            const blob = await canvas.convertToBlob({ type: mimeType, quality });
            if (blob.type && mimeType && blob.type !== mimeType) throw new Error(`unsupported-format:${mimeType}:${blob.type}`);
            return { data: await blob.arrayBuffer(), width, height, type: blob.type };
        } finally {
            canvas.width = 0;
            canvas.height = 0;
        }
    }

    async function scaleImage(input: ArrayBuffer, width: number, height: number, targetWidth: number, targetHeight: number) {
        const source = new ImageData(new Uint8ClampedArray(input), width, height);
        // Premultiplied explicitly, as on the page: Firefox's high quality resize gets half transparent pixels wrong otherwise.
        const bitmap = await createImageBitmap(source, { resizeWidth: targetWidth, resizeHeight: targetHeight, resizeQuality: 'high', premultiplyAlpha: 'premultiply' });
        try {
            const canvas = new OffscreenCanvas(targetWidth, targetHeight);
            try {
                const ctx = context(canvas, true);
                ctx.imageSmoothingQuality = 'high';
                ctx.drawImage(bitmap, 0, 0, targetWidth, targetHeight);
                const data = ctx.getImageData(0, 0, targetWidth, targetHeight).data;
                return { data: data.buffer as ArrayBuffer, width: targetWidth, height: targetHeight };
            } finally {
                canvas.width = 0;
                canvas.height = 0;
            }
        } finally {
            bitmap.close();
        }
    }

    /** True when this worker has what the codecs need: a 2D OffscreenCanvas that can encode, and createImageBitmap. */
    function probe() {
        try {
            if (typeof OffscreenCanvas === 'undefined' || typeof createImageBitmap !== 'function') return false;
            const canvas = new OffscreenCanvas(1, 1);
            const ok = !!canvas.getContext('2d') && typeof canvas.convertToBlob === 'function';
            canvas.width = 0;
            canvas.height = 0;
            return ok;
        } catch {
            return false;
        }
    }

    scope.onmessage = async (event: MessageEvent<Request>) => {
        const request = event.data;
        if (request.op === 'probe') {
            scope.postMessage({ id: request.id, ok: probe() });
            return;
        }
        try {
            const result = request.op === 'decode' ? await decode(request.input, request.options)
                : request.op === 'encode' ? await encode(request.input, request.width, request.height, request.mimeType, request.quality, request.background)
                : await scaleImage(request.input, request.width, request.height, request.targetWidth, request.targetHeight);
            scope.postMessage({ id: request.id, ok: true, ...result }, [result.data]);
        } catch (e: any) {
            // The input is still whole: hand it back, so the page can do the work itself without a copy kept aside.
            const input = request.input;
            const transfer = input && input.byteLength > 0 ? [input] : [];
            scope.postMessage({ id: request.id, ok: false, error: String(e?.message ?? e), input }, transfer);
        }
    };
})();
