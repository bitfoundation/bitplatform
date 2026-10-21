// Bmage browser bridge.
//
// This module does what the browser platform does better than .NET can on its own: format decoding and
// encoding (WebP, AVIF, JPEG, PNG), capability probing, clipboard, camera and pointer gestures, and the
// high quality resize that builds the editor's working copy of a large photo. The image processing
// itself lives in C#. Pixel data never crosses the boundary as base64: on WebAssembly it is copied
// straight between .NET memory and typed arrays (see pixelTransport), and over a Blazor Server circuit
// it travels as raw bytes through streams.

/** The DotNetObjectReference surface this module calls back into. */
interface DotNetObject {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

/**
 * A result holder returned to .NET as an object reference. Metadata is fetched as a tiny JSON call and the
 * payload is streamed as raw bytes, so pixel data never passes through base64.
 */
class ByteResult {
    private _bytes: Uint8Array | null;
    private _info: Record<string, unknown>;

    constructor(bytes: Uint8Array, info?: Record<string, unknown>) {
        this._bytes = bytes;
        this._info = info ?? {};
    }
    /** Small JSON descriptor: dimensions, mime type and so on. */
    info() {
        return { ...this._info, byteLength: this._bytes ? this._bytes.byteLength : 0 };
    }
    /** The payload. Blazor exposes a typed array as a stream reference. */
    bytes() {
        if (!this._bytes) throw new Error('This result has already been released.');
        return this._bytes;
    }
    /** Releases the payload so the browser can reclaim it without waiting for GC. */
    dispose() {
        this._bytes = null;
    }
}

const CANVAS_POOL_LIMIT = 3;
type AnyCanvas = HTMLCanvasElement | OffscreenCanvas;

const canvasPool: AnyCanvas[] = [];

/** Rents a canvas of at least the given size, reusing one when possible. */
function rentCanvas(width: number, height: number): AnyCanvas {
    const canvas = canvasPool.pop() ?? createCanvas(width, height);
    if (canvas.width !== width) canvas.width = width;
    if (canvas.height !== height) canvas.height = height;
    return canvas;
}

function returnCanvas(canvas: AnyCanvas) {
    // Releasing the backing store matters on mobile Safari, which is quick to kill pages over canvas memory.
    canvas.width = 0;
    canvas.height = 0;
    if (canvasPool.length < CANVAS_POOL_LIMIT) canvasPool.push(canvas);
}

function createCanvas(width: number, height: number): AnyCanvas {
    if (typeof OffscreenCanvas !== 'undefined') return new OffscreenCanvas(width, height);
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    return canvas;
}

function get2d(canvas: AnyCanvas, options?: CanvasRenderingContext2DSettings): CanvasRenderingContext2D {
    // Both canvas kinds hand out a context with the same drawing API; one type keeps the callers simple.
    const ctx = (canvas as HTMLCanvasElement).getContext('2d', options ?? { willReadFrequently: true });
    if (!ctx) throw new Error('The browser refused a 2D canvas context. The image may exceed the platform canvas limit.');
    return ctx;
}

/** Blob -> ArrayBuffer, tolerating older browsers without Blob.arrayBuffer. */
async function blobToArrayBuffer(blob: Blob): Promise<ArrayBuffer> {
    if (blob.arrayBuffer) return await blob.arrayBuffer();
    return await new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = () => reject(reader.error ?? new Error('Failed to read blob.'));
        reader.readAsArrayBuffer(blob);
    });
}

/**
 * Chooses decode-time resize hints that preserve the aspect ratio.
 *
 * createImageBitmap stretches the image when both resizeWidth and resizeHeight are given, and keeps the aspect
 * ratio when only one is. Which axis constrains a fit-inside-the-box request depends on the source aspect ratio,
 * so the caller passes the source dimensions (read from the file header in managed code, which costs nothing) and
 * this picks the single constraining axis. Without them there is no safe hint and the full-size bitmap is decoded,
 * then scaled correctly on the canvas.
 */
function resizeHint(options?: DecodeOptions): ImageBitmapOptions | null {
    const maxW = options?.maxWidth;
    const maxH = options?.maxHeight;
    if (!maxW || !maxH) return maxW ? { resizeWidth: maxW } : maxH ? { resizeHeight: maxH } : null;

    const sw = options?.sourceWidth;
    const sh = options?.sourceHeight;
    if (!sw || !sh) return null;
    if (sw <= maxW && sh <= maxH) return null;
    // Fit inside the box: the axis needing the larger reduction is the one that constrains.
    return (maxW / sw) <= (maxH / sh) ? { resizeWidth: maxW } : { resizeHeight: maxH };
}

/** The decode options .NET passes in; every member is optional. */
interface DecodeOptions {
    mimeType?: string;
    maxWidth?: number;
    maxHeight?: number;
    sourceWidth?: number;
    sourceHeight?: number;
    maxPixels?: number;
    autoOrient?: boolean;
}

/** Decodes bytes into an ImageBitmap, preferring createImageBitmap and falling back to an <img> element. */
async function decodeToBitmap(blob: Blob, options?: DecodeOptions): Promise<ImageBitmap | HTMLImageElement> {
    if (typeof createImageBitmap === 'function') {
        try {
            // imageOrientation 'from-image' makes the browser honour EXIF orientation for us.
            const init: ImageBitmapOptions = { imageOrientation: options?.autoOrient === false ? 'none' : 'from-image' };
            const hint = resizeHint(options);
            if (hint) {
                Object.assign(init, hint);
                init.resizeQuality = 'high';
            }
            return await createImageBitmap(blob, init);
        } catch (e) {
            // Some browsers reject the options bag; retry bare before falling back.
            try { return await createImageBitmap(blob); } catch { /* fall through */ }
        }
    }
    const url = URL.createObjectURL(blob);
    try {
        const img = new Image();
        img.decoding = 'async';
        await new Promise<void>((resolve, reject) => {
            img.onload = () => resolve();
            img.onerror = () => reject(new Error('The browser could not decode this image.'));
            img.src = url;
        });
        if (img.decode) { try { await img.decode(); } catch { /* already loaded */ } }
        return img;
    } finally {
        URL.revokeObjectURL(url);
    }
}

function bitmapSize(bitmap: ImageBitmap | HTMLImageElement) {
    return {
        width: bitmap.width ?? (bitmap as HTMLImageElement).naturalWidth ?? 0,
        height: bitmap.height ?? (bitmap as HTMLImageElement).naturalHeight ?? 0,
    };
}

/**
 * Decodes an encoded image into raw RGBA pixels.
 * Returns a ByteResult whose info() gives the dimensions and bytes() streams the pixels.
 */
export async function decode(bytes: any, options?: DecodeOptions) {
    const { data, width, height } = await decodePixels(bytes, options);
    return new ByteResult(new Uint8Array(data.buffer, data.byteOffset, data.byteLength), { width, height });
}

async function decodePixels(bytes: Uint8Array, options?: DecodeOptions) {
    const blob = new Blob([bytes as BlobPart], options?.mimeType ? { type: options.mimeType } : undefined);
    const bitmap = await decodeToBitmap(blob, options);
    try {
        let { width, height } = bitmapSize(bitmap);
        if (!width || !height) throw new Error('The decoded image has no dimensions.');

        // Downscale during decode when the caller asked for a smaller working copy.
        if (options?.maxWidth && options?.maxHeight) {
            const scale = Math.min(options.maxWidth / width, options.maxHeight / height, 1);
            if (scale < 1) {
                width = Math.max(1, Math.round(width * scale));
                height = Math.max(1, Math.round(height * scale));
            }
        }
        if (options?.maxPixels && width * height > options.maxPixels) {
            const scale = Math.sqrt(options.maxPixels / (width * height));
            width = Math.max(1, Math.floor(width * scale));
            height = Math.max(1, Math.floor(height * scale));
        }

        const canvas = rentCanvas(width, height);
        try {
            const ctx = get2d(canvas);
            // A canvas shrinking the bitmap itself, when the browser could not resize while decoding, must not use
            // the default low quality smoothing.
            ctx.imageSmoothingQuality = 'high';
            ctx.drawImage(bitmap, 0, 0, width, height);
            return { data: ctx.getImageData(0, 0, width, height).data, width, height };
        } finally {
            returnCanvas(canvas);
        }
    } finally {
        closeBitmap(bitmap);
    }
}

function closeBitmap(bitmap: ImageBitmap | HTMLImageElement) {
    if ('close' in bitmap) bitmap.close();
}

/**
 * Reads dimensions using the browser's own decoder. Only used for formats with no managed header reader (AVIF),
 * because it really does decode the image: there is no cheaper way to ask the browser for a size.
 */
export async function identify(bytes: any, mimeType?: string) {
    const blob = new Blob([bytes], mimeType ? { type: mimeType } : undefined);
    const bitmap = await decodeToBitmap(blob, { autoOrient: true });
    try {
        const { width, height } = bitmapSize(bitmap);
        return { width, height };
    } finally {
        closeBitmap(bitmap);
    }
}

/**
 * Encodes raw RGBA pixels into the requested format.
 * Returns a ByteResult, or throws when the browser cannot produce that format.
 */
export async function encode(pixels: any, width: number, height: number, mimeType: string, quality?: number, background?: string) {
    const { data, type } = await encodePixels(pixels, width, height, mimeType, quality, background);
    return new ByteResult(data, { mimeType: type, width, height });
}

/** A view of a byte array as canvas pixels, without copying it. */
function asClamped(bytes: Uint8Array | Uint8ClampedArray): Uint8ClampedArray<ArrayBuffer> {
    return new Uint8ClampedArray(bytes.buffer as ArrayBuffer, bytes.byteOffset, bytes.byteLength);
}

async function encodePixels(pixels: Uint8Array | Uint8ClampedArray, width: number, height: number, mimeType: string, quality?: number, background?: string) {
    // Not pooled: a context's attributes are fixed by its first getContext call, and this one opts out of
    // willReadFrequently. Returned to the pool, it would make later pixel reads on that canvas slow.
    const canvas = createCanvas(width, height);
    try {
        const ctx = get2d(canvas, { willReadFrequently: false });
        ctx.putImageData(new ImageData(asClamped(pixels), width, height), 0, 0);
        // Formats without an alpha channel need an explicit backdrop, otherwise the browser uses black. Drawing it
        // behind the pixels already there composites them over it, and leaves opaque pixels exactly as they are.
        if (background) {
            ctx.globalCompositeOperation = 'destination-over';
            ctx.fillStyle = background;
            ctx.fillRect(0, 0, width, height);
            ctx.globalCompositeOperation = 'source-over';
        }

        const blob = await canvasToBlob(canvas, mimeType, quality);
        if (!blob) throw new Error(`The browser produced no output for ${mimeType}.`);
        if (blob.type && mimeType && blob.type !== mimeType) {
            // Browsers silently fall back to PNG for formats they cannot write. Report it instead of lying.
            throw new Error(`unsupported-format:${mimeType}:${blob.type}`);
        }
        return { data: new Uint8Array(await blobToArrayBuffer(blob)), type: blob.type };
    } finally {
        canvas.width = 0;
        canvas.height = 0;
    }
}

function canvasToBlob(canvas: AnyCanvas, mimeType: string, quality?: number): Promise<Blob | null> {
    if ('convertToBlob' in canvas) return canvas.convertToBlob({ type: mimeType, quality });
    return new Promise((resolve) => canvas.toBlob(resolve, mimeType, quality));
}

/** Probes what this browser can actually do. Encoding support is verified by really encoding a pixel. */
let capabilities: Promise<unknown> | null = null;

/**
 * Probes once per page and hands every later caller the same answer: the probes encode and decode real images and
 * allocate a large canvas, which is worth doing once and only once.
 */
export function getCapabilities() {
    // A probe that failed outright is not remembered, so the next caller tries again.
    return capabilities ??= probeCapabilities().catch((e) => { capabilities = null; throw e; });
}

async function probeCapabilities() {
    const probe = createCanvas(1, 1);
    const encodable: string[] = [];
    const decodable: string[] = [];

    // A canvas must have a rendering context before it can be encoded; without one, OffscreenCanvas throws
    // InvalidStateError and every format would look unsupported.
    const probeCtx = (probe as HTMLCanvasElement).getContext('2d');
    if (probeCtx) {
        probeCtx.fillStyle = 'rgba(255,0,0,0.5)';
        probeCtx.fillRect(0, 0, 1, 1);
        // All at once: each encode snapshots the canvas when it starts.
        const types = ['image/png', 'image/jpeg', 'image/webp', 'image/avif'];
        const blobs = await Promise.all(types.map((type) => canvasToBlob(probe, type, 0.9).catch(() => null)));
        // Browsers substitute PNG for formats they cannot write, so compare the type rather than trusting success.
        types.forEach((type, i) => { if (blobs[i]?.type === type) encodable.push(type); });
    }
    // Not returned to the pool: its context was created without willReadFrequently, and a pooled canvas keeps the
    // attributes of its first context, which would make every later pixel read on it slow.
    probe.width = 0;
    probe.height = 0;

    const decoded = await Promise.all(Object.entries(PROBE_IMAGES).map(async ([type, data]) => {
        try {
            const bytes = Uint8Array.from(atob(data), (c) => c.charCodeAt(0));
            const bitmap = await decodeToBitmap(new Blob([bytes], { type }), { autoOrient: false });
            const size = bitmapSize(bitmap);
            closeBitmap(bitmap);
            return size.width > 0 ? type : null;
        } catch {
            return null;
        }
    }));
    for (const type of decoded) if (type) decodable.push(type);

    // deviceMemory is not in the DOM typings, and a worker-like host may have no navigator at all.
    const nav: any = typeof navigator !== 'undefined' ? navigator : {};
    return {
        encodableMimeTypes: encodable,
        decodableMimeTypes: decodable,
        supportsOffscreenCanvas: typeof OffscreenCanvas !== 'undefined',
        supportsWebWorkers: typeof Worker !== 'undefined',
        supportsImageBitmap: typeof createImageBitmap === 'function',
        supportsWebCodecs: typeof (globalThis as any).ImageDecoder !== 'undefined',
        supportsClipboardRead: !!(nav.clipboard && nav.clipboard.read),
        supportsClipboardWrite: !!(nav.clipboard && nav.clipboard.write) && typeof ClipboardItem !== 'undefined',
        supportsFileSystemAccess: typeof window !== 'undefined' && 'showSaveFilePicker' in window,
        supportsCamera: !!(nav.mediaDevices && nav.mediaDevices.getUserMedia),
        devicePixelRatio: (typeof window !== 'undefined' && window.devicePixelRatio) || 1,
        hardwareConcurrency: nav.hardwareConcurrency ?? null,
        deviceMemoryGb: nav.deviceMemory ?? null,
        maxCanvasDimension: probeMaxCanvasDimension(),
        maxCanvasArea: probeMaxCanvasArea(),
    };
}

// 1x1 images used to probe decode support.
const PROBE_IMAGES: Record<string, string> = {
    'image/webp': 'UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAwA0JaQAA3AA/vuUAAA=',
    'image/avif': 'AAAAIGZ0eXBhdmlmAAAAAGF2aWZtaWYxbWlhZk1BMUIAAADybWV0YQAAAAAAAAAoaGRscgAAAAAAAAAAcGljdAAAAAAAAAAAAAAAAGxpYmF2aWYAAAAADnBpdG0AAAAAAAEAAAAeaWxvYwAAAABEAAABAAEAAAABAAABGgAAAB0AAAAoaWluZgAAAAAAAQAAABppbmZlAgAAAAABAABhdjAxQ29sb3IAAAAAamlwcnAAAABLaXBjbwAAABRpc3BlAAAAAAAAAAEAAAABAAAAEHBpeGkAAAAAAwgICAAAAAxhdjFDgQ0MAAAAABNjb2xybmNseAACAAIABoAAAAAXaXBtYQAAAAAAAAABAAEEAQKDBAAAACVtZGF0EgAKCBgABogQEDQgMgkQAAAAB8dSLfI=',
    'image/gif': 'R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7',
};

/**
 * Finds the largest square canvas the platform will actually allocate. Browsers do not expose this,
 * and exceeding it yields a silently blank canvas, which is far worse than refusing up front.
 */
function probeMaxCanvasDimension() {
    const sizes = [32767, 16384, 11180, 8192, 4096];
    for (const size of sizes) {
        try {
            const canvas = createCanvas(size, 1);
            const ctx = (canvas as HTMLCanvasElement).getContext('2d');
            if (!ctx) continue;
            ctx.fillStyle = '#fff';
            ctx.fillRect(size - 1, 0, 1, 1);
            const ok = ctx.getImageData(size - 1, 0, 1, 1).data[3] === 255;
            canvas.width = 0;
            canvas.height = 0;
            if (ok) return size;
        } catch { /* try smaller */ }
    }
    return 4096;
}

/**
 * Finds whether the platform caps canvas area below its edge limit. iOS Safari allows a 32767 pixel edge but only
 * 16,777,216 pixels in total, so a canvas that passes the edge probe can still come out blank. Probing the real area
 * limit would allocate gigabytes, so this checks the one well-known cap and otherwise reports 0 (unknown).
 */
function probeMaxCanvasArea() {
    const iosLimit = 4096 * 4096;
    try {
        const canvas = createCanvas(4097, 4097);
        const ctx = (canvas as HTMLCanvasElement).getContext('2d');
        let ok = false;
        if (ctx) {
            ctx.fillStyle = '#fff';
            ctx.fillRect(4096, 4096, 1, 1);
            ok = ctx.getImageData(4096, 4096, 1, 1).data[3] === 255;
        }
        canvas.width = 0;
        canvas.height = 0;
        return ok ? 0 : iosLimit;
    } catch {
        return iosLimit;
    }
}

/** Reads the first image on the clipboard, or null when there is none. */
export async function readClipboardImage() {
    if (!navigator.clipboard || !navigator.clipboard.read) throw new Error('This browser does not support reading images from the clipboard.');
    const items = await navigator.clipboard.read();
    for (const item of items) {
        const type = item.types.find((t) => t.startsWith('image/'));
        if (!type) continue;
        const blob = await item.getType(type);
        return new ByteResult(new Uint8Array(await blobToArrayBuffer(blob)), { mimeType: type });
    }
    return null;
}

let pasteListenerId = 0;
const pasteListeners = new Map<number, (e: ClipboardEvent) => Promise<void>>();

/**
 * Forwards images pasted anywhere in the document to .NET. A paste event carries its data, so unlike
 * navigator.clipboard.read this needs no permission. Pastes without an image are left for the page to handle.
 */
export function listenForPaste(dotNetRef: DotNetObject, maxBytes: number) {
    const id = ++pasteListenerId;
    const handler = async (e: ClipboardEvent) => {
        const items: DataTransferItem[] = e.clipboardData ? Array.from(e.clipboardData.items) : [];
        const item = items.find((i) => i.kind === 'file' && i.type.startsWith('image/'));
        const file = item?.getAsFile();
        if (!file) return;
        e.preventDefault();
        if (maxBytes > 0 && file.size > maxBytes) {
            await dotNetRef.invokeMethodAsync('OnPasteRejected', file.size);
            return;
        }
        const bytes = new Uint8Array(await blobToArrayBuffer(file));
        await dotNetRef.invokeMethodAsync('OnPaste', bytes, file.type || null);
    };
    document.addEventListener('paste', handler);
    pasteListeners.set(id, handler);
    return id;
}

export function stopListeningForPaste(id: number) {
    const handler = pasteListeners.get(id);
    if (!handler) return;
    document.removeEventListener('paste', handler);
    pasteListeners.delete(id);
}

/** Writes an image to the clipboard. */
export async function writeClipboardImage(bytes: any, mimeType: string) {
    if (!navigator.clipboard || !navigator.clipboard.write || typeof ClipboardItem === 'undefined')
        throw new Error('This browser does not support writing images to the clipboard.');
    const blob = new Blob([bytes], { type: mimeType });
    await navigator.clipboard.write([new ClipboardItem({ [mimeType]: blob })]);
}

/** Fetches a remote image. Subject to CORS; the caller decides whether credentials are sent. */
export async function fetchImage(url: string, withCredentials: boolean) {
    const response = await fetch(url, { credentials: withCredentials ? 'include' : 'omit', mode: 'cors' });
    if (!response.ok) throw new Error(`Fetching the image failed with HTTP ${response.status}.`);
    const blob = await response.blob();
    return new ByteResult(new Uint8Array(await blobToArrayBuffer(blob)), { mimeType: blob.type });
}

/** Triggers a browser download of the given bytes. */
export function download(bytes: any, fileName: string, mimeType: string) {
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    // Give the browser a moment to start the download before releasing the URL.
    setTimeout(() => URL.revokeObjectURL(url), 30000);
}

/** Saves bytes through the File System Access API when available. Returns false when the user cancelled. */
export async function saveFile(bytes: any, fileName: string, mimeType: string, extension: string) {
    if (!('showSaveFilePicker' in window)) {
        download(bytes, fileName, mimeType);
        return true;
    }
    try {
        // The File System Access API is not in the DOM typings of every TypeScript release.
        const handle = await (window as any).showSaveFilePicker({
            suggestedName: fileName,
            types: [{ description: mimeType, accept: { [mimeType]: [extension] } }],
        });
        const writable = await handle.createWritable();
        await writable.write(new Blob([bytes], { type: mimeType }));
        await writable.close();
        return true;
    } catch (e) {
        if (e && e.name === 'AbortError') return false;
        throw e;
    }
}

/** Creates an object URL for bytes. The caller must release it with revokeObjectUrl. */
export function createObjectUrl(bytes: any, mimeType: string) {
    return URL.createObjectURL(new Blob([bytes], { type: mimeType }));
}

export function revokeObjectUrl(url: string) {
    URL.revokeObjectURL(url);
}

/**
 * Paints raw RGBA pixels into a canvas element. The backing store is sized to the pixel buffer, not to the CSS box:
 * callers that want a crisp result on a high-DPI display render their buffer at devicePixelRatio times the CSS size
 * and let CSS scale the element back down.
 */
export function paint(canvas: HTMLCanvasElement, pixels: any, width: number, height: number) {
    if (!canvas) return;
    if (canvas.width !== width) canvas.width = width;
    if (canvas.height !== height) canvas.height = height;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.putImageData(new ImageData(asClamped(pixels), width, height), 0, 0);
}

/** Measures text with the platform text engine, which handles fonts and scripts .NET cannot resolve alone. */
export function measureText(text: string, font: string, letterSpacing?: number) {
    const canvas = rentCanvas(1, 1);
    try {
        const ctx = get2d(canvas);
        ctx.font = font;
        if (letterSpacing && 'letterSpacing' in ctx) ctx.letterSpacing = `${letterSpacing}px`;
        const m = ctx.measureText(text);
        return {
            width: m.width,
            ascent: m.actualBoundingBoxAscent ?? m.fontBoundingBoxAscent ?? 0,
            descent: m.actualBoundingBoxDescent ?? m.fontBoundingBoxDescent ?? 0,
        };
    } finally {
        returnCanvas(canvas);
    }
}

/**
 * Renders text to RGBA pixels using the platform text engine. Used by the browser text rasteriser so
 * system fonts, emoji and complex scripts render correctly without shipping a font.
 */
export function renderText(text: string, font: string, color: string, maxWidth: number, letterSpacing: number, lineHeight: number, align?: CanvasTextAlign) {
    const probe = rentCanvas(1, 1);
    let metrics: ReturnType<typeof measureLines>;
    try {
        const ctx = get2d(probe);
        ctx.font = font;
        if (letterSpacing && 'letterSpacing' in ctx) ctx.letterSpacing = `${letterSpacing}px`;
        metrics = measureLines(ctx, text, maxWidth, lineHeight);
    } finally {
        returnCanvas(probe);
    }

    const pad = Math.ceil(metrics.fontSize * 0.35) + 2;
    const width = Math.max(1, Math.ceil(metrics.width) + pad * 2);
    const height = Math.max(1, Math.ceil(metrics.lines.length * metrics.lineHeight) + pad * 2);
    const canvas = rentCanvas(width, height);
    try {
        const ctx = get2d(canvas);
        ctx.clearRect(0, 0, width, height);
        ctx.font = font;
        if (letterSpacing && 'letterSpacing' in ctx) ctx.letterSpacing = `${letterSpacing}px`;
        ctx.fillStyle = color;
        ctx.textBaseline = 'alphabetic';
        ctx.textAlign = align || 'left';
        const originX = align === 'center' ? width / 2 : align === 'right' ? width - pad : pad;
        for (let i = 0; i < metrics.lines.length; i++) {
            ctx.fillText(metrics.lines[i], originX, pad + metrics.ascent + i * metrics.lineHeight);
        }
        const data = ctx.getImageData(0, 0, width, height);
        return new ByteResult(new Uint8Array(data.data.buffer), {
            width,
            height,
            originX,
            originY: pad + metrics.ascent,
            lineCount: metrics.lines.length,
            textWidth: metrics.width,
            ascent: metrics.ascent,
            descent: metrics.descent,
        });
    } finally {
        returnCanvas(canvas);
    }
}

function measureLines(ctx: CanvasRenderingContext2D, text: string, maxWidth: number, lineHeight: number) {
    const sample = ctx.measureText('Mg');
    const ascent = sample.fontBoundingBoxAscent ?? sample.actualBoundingBoxAscent ?? 0;
    const descent = sample.fontBoundingBoxDescent ?? sample.actualBoundingBoxDescent ?? 0;
    const fontSize = ascent + descent;
    // lineHeight is a multiple of the font size, as in TextStyle.LineHeight.
    const lineAdvance = fontSize * (lineHeight > 0 ? lineHeight : 1.2);
    const raw = String(text).split('\n');
    const lines: string[] = [];
    for (const paragraph of raw) {
        if (!maxWidth || maxWidth <= 0) { lines.push(paragraph); continue; }
        let current = '';
        for (const word of paragraph.split(/(\s+)/)) {
            const candidate = current + word;
            if (current && ctx.measureText(candidate).width > maxWidth) {
                lines.push(current);
                current = word.trimStart();
            } else {
                current = candidate;
            }
        }
        lines.push(current);
    }
    const width = lines.reduce((max, line) => Math.max(max, ctx.measureText(line).width), 0);
    return { lines, width, ascent, descent, fontSize, lineHeight: lineAdvance };
}

/** Starts a camera stream into a video element and returns the stream so it can be stopped. */
export async function startCamera(video: HTMLVideoElement, facingMode?: string) {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) throw new Error('This browser does not support camera capture.');
    const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: facingMode || 'environment', width: { ideal: 1920 }, height: { ideal: 1080 } },
        audio: false,
    });
    video.srcObject = stream;
    await video.play();
    return stream;
}

/** Grabs the current camera frame as raw RGBA pixels. */
export function captureFrame(video: HTMLVideoElement) {
    const width = video.videoWidth;
    const height = video.videoHeight;
    if (!width || !height) throw new Error('The camera has not produced a frame yet.');
    const canvas = rentCanvas(width, height);
    try {
        const ctx = get2d(canvas);
        ctx.drawImage(video, 0, 0);
        const data = ctx.getImageData(0, 0, width, height);
        return new ByteResult(new Uint8Array(data.data.buffer), { width, height });
    } finally {
        returnCanvas(canvas);
    }
}

export function stopCamera(stream: MediaStream | null) {
    if (!stream) return;
    for (const track of stream.getTracks()) track.stop();
}

/** Reports the CSS size and device pixel ratio of an element, so previews can be rendered crisply. */
export function getElementMetrics(element: Element | null) {
    if (!element) return null;
    const rect = element.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height,
        devicePixelRatio: window.devicePixelRatio || 1,
    };
}

const observers = new WeakMap<Element, ResizeObserver>();

/** Watches an element for size changes and calls back into .NET. Returns a token used to stop observing. */
export function observeResize(element: Element | null, dotNetRef: DotNetObject) {
    if (!element || typeof ResizeObserver === 'undefined') return false;
    stopObservingResize(element);
    const observer = new ResizeObserver((entries) => {
        const entry = entries[0];
        if (!entry) return;
        const box = entry.contentRect;
        dotNetRef.invokeMethodAsync('OnElementResized', box.width, box.height, window.devicePixelRatio || 1);
    });
    observer.observe(element);
    observers.set(element, observer);
    return true;
}

export function stopObservingResize(element: Element) {
    const existing = observers.get(element);
    if (existing) {
        existing.disconnect();
        observers.delete(element);
    }
}

/**
 * Prevents the page from scrolling or zooming while the user drags on the editor surface.
 * Registered as a non-passive listener, which Blazor's own event handling cannot do.
 */
const scrollKeys = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', ' ', 'Delete', 'Backspace']);

/** Wheel delta that zooms by the same 1.1 a mouse notch (100 pixels of delta) always has. */
const zoomPerPixel = Math.log(1.1) / 100;

/**
 * Stops the page scrolling or zooming while the user works on an element: touch drags, pinch gestures, the mouse
 * wheel (which the editor uses for zoom), and the keys the editor binds while the element itself has focus. These
 * need non-passive listeners on the element, which Blazor's own event handling cannot register.
 *
 * With a .NET reference it also takes over the parts of pointer handling that cost a round trip per event: it
 * captures the pointer the moment a drag starts, passes .NET only the pointer moves of a drag (Blazor itself does not
 * listen for moves, so hovering costs nothing, and the page's own listeners still see every move), and turns wheel
 * and trackpad pinch input into one zoom call per frame, with a factor proportional to the distance scrolled so a
 * trackpad zooms smoothly instead of a fixed step per event.
 */
export function suppressTouchScrolling(element: any, dotNetRef?: DotNetObject) {
    if (!element || element.__bitBmageHandlers) return;
    const cancel = (e: Event) => { if (e.cancelable) e.preventDefault(); };
    const keys = (e: KeyboardEvent) => { if (e.target === element && scrollKeys.has(e.key)) e.preventDefault(); };
    const pointers = new Set<number>();
    const down = (e: PointerEvent) => {
        if (e.button > 0) return;
        pointers.add(e.pointerId);
        try { element.setPointerCapture(e.pointerId); } catch { /* the pointer is already gone */ }
    };
    const up = (e: PointerEvent) => { pointers.delete(e.pointerId); };
    const move = (e: PointerEvent) => {
        if (!dotNetRef || !pointers.has(e.pointerId)) return;
        // The component may be gone by the time the call arrives; the drag ended with it.
        dotNetRef.invokeMethodAsync('OnDragPointerMove', e.pointerId, e.clientX, e.clientY, e.shiftKey, e.altKey, e.ctrlKey, e.pointerType || 'mouse', e.pressure)
            .catch(() => { });
    };
    let pendingDelta = 0, pendingX = 0, pendingY = 0, frame = 0;
    const wheel = (e: WheelEvent) => {
        if (e.cancelable) e.preventDefault();
        if (!dotNetRef) return;
        // Firefox reports a mouse notch as 3 lines; at 33 pixels a line it zooms as much as a 100 pixel notch elsewhere.
        const unit = e.deltaMode === 1 ? 33 : e.deltaMode === 2 ? 800 : 1;
        const rect = element.getBoundingClientRect();
        pendingDelta += e.deltaY * unit;
        pendingX = e.clientX - rect.left;
        pendingY = e.clientY - rect.top;
        if (frame) return;
        frame = requestAnimationFrame(() => {
            frame = 0;
            const delta = pendingDelta;
            pendingDelta = 0;
            // The component may be gone by the time the frame fires; nothing is left to zoom then.
            if (delta !== 0) dotNetRef.invokeMethodAsync('OnWheelZoom', Math.exp(-delta * zoomPerPixel), pendingX, pendingY).catch(() => { });
        });
    };
    element.addEventListener('touchmove', cancel, { passive: false });
    element.addEventListener('gesturestart', cancel, { passive: false });
    element.addEventListener('wheel', wheel, { passive: false });
    element.addEventListener('keydown', keys);
    if (dotNetRef) {
        element.addEventListener('pointerdown', down);
        element.addEventListener('pointerup', up);
        element.addEventListener('pointercancel', up);
        element.addEventListener('lostpointercapture', up);
        element.addEventListener('pointermove', move);
    }
    element.__bitBmageHandlers = { cancel, keys, wheel, down, up, move, cancelFrame: () => { if (frame) cancelAnimationFrame(frame); } };
}

export function restoreTouchScrolling(element: any) {
    const handlers = element?.__bitBmageHandlers;
    if (!handlers) return;
    handlers.cancelFrame();
    element.removeEventListener('touchmove', handlers.cancel);
    element.removeEventListener('gesturestart', handlers.cancel);
    element.removeEventListener('wheel', handlers.wheel);
    element.removeEventListener('keydown', handlers.keys);
    element.removeEventListener('pointerdown', handlers.down);
    element.removeEventListener('pointerup', handlers.up);
    element.removeEventListener('pointercancel', handlers.up);
    element.removeEventListener('lostpointercapture', handlers.up);
    element.removeEventListener('pointermove', handlers.move);
    delete element.__bitBmageHandlers;
}

/** Captures the pointer so a drag keeps tracking outside the element. */
export function setPointerCapture(element: Element | null, pointerId: number) {
    try { element?.setPointerCapture(pointerId); } catch { /* pointer already released */ }
}

export function releasePointerCapture(element: Element | null, pointerId: number) {
    try { element?.releasePointerCapture(pointerId); } catch { /* already released */ }
}

/** Reads the bytes of a File or Blob held by the browser (used for drag & drop and paste). */
export async function readBlob(blob: Blob) {
    return new ByteResult(new Uint8Array(await blobToArrayBuffer(blob)), { mimeType: blob.type });
}

/** Frees pooled canvases. Call when leaving an image-heavy page on a memory constrained device. */
export function releaseResources() {
    canvasPool.length = 0;
}

// ---- In-process pixel transport ----
//
// On WebAssembly, .NET calls these through [JSImport] with views of its own memory, so pixels are copied once in each
// direction, straight between a .NET buffer and a typed array: no byte[] is allocated for the call, nothing is
// marshalled through JSON, and nothing is streamed in chunks. A view is only valid during the synchronous call it is
// passed to, so data that an asynchronous step needs is first copied into a staged entry, and results are copied
// back into a buffer .NET has sized from the entry's dimensions.

/** A view of .NET memory, as [JSImport] passes a Span. */
interface MemoryView {
    set(source: ArrayLike<number>, targetOffset?: number): void;
    copyTo(target: Uint8Array | Uint8ClampedArray, sourceOffset?: number): void;
    slice(start?: number, end?: number): Uint8Array;
    readonly byteLength: number;
}

interface Staged {
    data: Uint8Array | Uint8ClampedArray;
    width: number;
    height: number;
    type?: string;
}

const staged = new Map<number, Staged>();
let nextStaged = 1;
const paintData = new WeakMap<HTMLCanvasElement, { image: ImageData; bytes: Uint8Array }>();

function stagedEntry(id: number) {
    const entry = staged.get(id);
    if (!entry) throw new Error('This staged image has already been released.');
    return entry;
}

const pixelTransport = {
    /** Copies bytes (pixels, or an encoded file with a zero size) out of .NET memory and returns a handle to them. */
    stage(view: MemoryView, width: number, height: number) {
        const id = nextStaged++;
        staged.set(id, { data: view.slice(), width, height });
        return id;
    },

    /** Resizes staged pixels with the browser's high quality resampler. */
    async scale(id: number, width: number, height: number) {
        const entry = stagedEntry(id);
        const source = new ImageData(asClamped(entry.data), entry.width, entry.height);
        // Firefox's high quality resize returns the wrong alpha for half transparent pixels unless the
        // premultiplication is asked for explicitly.
        const bitmap = await createImageBitmap(source, { resizeWidth: width, resizeHeight: height, resizeQuality: 'high', premultiplyAlpha: 'premultiply' });
        try {
            const canvas = rentCanvas(width, height);
            try {
                const ctx = get2d(canvas);
                ctx.clearRect(0, 0, width, height);
                // Where createImageBitmap ignores the resize, this draw does it, and must not use low quality smoothing.
                ctx.imageSmoothingQuality = 'high';
                ctx.drawImage(bitmap, 0, 0, width, height);
                entry.data = ctx.getImageData(0, 0, width, height).data;
                entry.width = width;
                entry.height = height;
            } finally {
                returnCanvas(canvas);
            }
        } finally {
            bitmap.close();
        }
    },

    /** Decodes a staged file into pixels, with the same options and limits as decode(). */
    async decode(id: number, mimeType: string, autoOrient: boolean, maxWidth: number, maxHeight: number, maxPixels: number, sourceWidth: number, sourceHeight: number) {
        const entry = stagedEntry(id);
        const options: DecodeOptions = { mimeType, autoOrient };
        if (maxWidth > 0) options.maxWidth = maxWidth;
        if (maxHeight > 0) options.maxHeight = maxHeight;
        if (maxPixels > 0) options.maxPixels = maxPixels;
        if (sourceWidth > 0) options.sourceWidth = sourceWidth;
        if (sourceHeight > 0) options.sourceHeight = sourceHeight;
        const { data, width, height } = await decodePixels(entry.data as Uint8Array, options);
        Object.assign(entry, { data, width, height });
    },

    /** Encodes staged pixels, replacing them with the file; throws as encode() does when the format is unsupported. */
    async encode(id: number, mimeType: string, quality: number, background: string | null) {
        const entry = stagedEntry(id);
        const { data, type } = await encodePixels(entry.data, entry.width, entry.height, mimeType, quality, background ?? undefined);
        Object.assign(entry, { data, type });
    },

    width: (id: number) => stagedEntry(id).width,
    height: (id: number) => stagedEntry(id).height,
    length: (id: number) => stagedEntry(id).data.byteLength,

    /** Copies a staged entry into .NET memory and releases it. */
    read(id: number, view: MemoryView) {
        const entry = stagedEntry(id);
        staged.delete(id);
        // A view only accepts a Uint8Array, and canvas pixels come as a Uint8ClampedArray over the same kind of buffer.
        const data = entry.data;
        view.set(data instanceof Uint8Array ? data : new Uint8Array(data.buffer, data.byteOffset, data.byteLength));
    },

    release(id: number) {
        staged.delete(id);
    },

    /** Paints pixels straight from .NET memory into a canvas, reusing its ImageData while the size stays the same. */
    paint(canvasId: string, view: MemoryView, width: number, height: number) {
        const canvas = document.getElementById(canvasId) as HTMLCanvasElement | null;
        const ctx = canvas?.getContext('2d');
        if (!canvas || !ctx) return false;
        if (canvas.width !== width) canvas.width = width;
        if (canvas.height !== height) canvas.height = height;
        let entry = paintData.get(canvas);
        if (!entry || entry.image.width !== width || entry.image.height !== height) {
            const image = new ImageData(width, height);
            // A view only copies into a Uint8Array, so keep one over the ImageData's own buffer.
            entry = { image, bytes: new Uint8Array(image.data.buffer, image.data.byteOffset, image.data.byteLength) };
            paintData.set(canvas, entry);
        }
        view.copyTo(entry.bytes);
        ctx.putImageData(entry.image, 0, 0);
        return true;
    },
};

(globalThis as any).BitBmagePixels = pixelTransport;
