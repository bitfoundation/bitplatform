// Bink browser layer.
//
// This file deliberately contains NO drawing logic. It does exactly three things that only the browser can do:
//
//   1. Collect pointer samples (including coalesced ones) and hand them to .NET once per animation frame.
//   2. Replay a command buffer produced by the C# renderer onto a 2D canvas context.
//   3. Report device capabilities, sizes and encoded image data back to .NET.
//
// Stroke shape, smoothing, pressure response, hit testing, history and serialization all live in C#.
// The one exception is the optional "wet ink" trail (see appendWetInk): a plain polyline drawn while a pointer
// batch is in flight, used only in Blazor Server to hide network latency. It is replaced by the real
// engine-generated geometry as soon as .NET responds.

// ---- Contracts shared with the C# interop ----------------------------------------------------------------

// The shape of a DotNetObjectReference as Blazor hands it to JavaScript.
interface DotNetObjectReference {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

// camelCase JSON of BinkInteropOptions. Every field is optional because setOptions merges partial updates.
interface BinkSurfaceOptions {
    rejectTouchWhilePen?: boolean;
    suppressContextMenu?: boolean;
    trackHover?: boolean;
    preventDefaultOnDown?: boolean;
    wetInk?: boolean;
    handleKeyboard?: boolean;
    // 0: the wheel scrolls the page. 1: the wheel pans and Ctrl+wheel or a trackpad pinch zooms. 2: the wheel zooms.
    wheelMode?: number;
    // Whether Ctrl+C, Ctrl+X and Ctrl+V reach the system clipboard.
    clipboard?: boolean;
    // Whether the browser's predicted pointer positions are delivered, flagged as predictions.
    predict?: boolean;
    // Whether image files and saved drawings dropped on the canvas are reported.
    dropFiles?: boolean;
    // Chords the application took for itself, in canonical form ("Ctrl+Shift+Z"): neither handled nor prevented.
    disabledShortcuts?: string[] | null;
}

// What a paste carried, reported to .NET.
type PasteKind = 'bink' | 'text' | 'image' | 'internal';

// What a dropped file is, reported to .NET with where it was dropped.
type DropKind = 'image' | 'bink' | 'svg';

// What the surface reports back. Either wired to a DotNetObjectReference by setDotNetCallbacks or supplied
// directly through setCallbacks.
interface BinkSurfaceCallbacks {
    samples(buffer: Float64Array, count: number): unknown;
    resized(width: number, height: number, dpr: number): unknown;
    key(key: string, ctrl: boolean, shift: boolean, alt: boolean): unknown;
    textKey(key: string, value: string | null | undefined): unknown;
    redraw?(): unknown;
    resourcesLost?(): unknown;
    wheel?(panX: number, panY: number, zoom: number, x: number, y: number): unknown;
    paste?(kind: PasteKind, text: string | null, data: Blob | null): unknown;
    drop?(kind: DropKind, data: Blob, x: number, y: number): unknown;
}

// The shape of Blazor's DotNet global, for passing a Blob to .NET as a stream rather than inside a message: a
// Blazor Server circuit refuses any single message from the browser above 32 KB by default.
interface DotNetGlobal {
    createJSStreamReference(data: Blob | ArrayBuffer | ArrayBufferView): unknown;
}

// A copy waits for .NET to serialize the selection. The clipboard item is created inside the key event, which is
// what browsers require, and filled when the payload arrives.
interface PendingClipboardWrite {
    resolve(blob: Blob): void;
    reject(reason: Error): void;
    // The picture of the selection, offered alongside the text so a copy pastes into other applications too.
    resolveImage(blob: Blob): void;
    rejectImage(reason: Error): void;
    timer: number;
    // Set when this layer settled the write with nothing, which is not a failure of the clipboard.
    withdrawn: boolean;
}

// A payload copied from a canvas starts with this, which is how a paste tells it from ordinary text.
const CLIPBOARD_MARKER = '{"format":"bink"';

// camelCase JSON of BinkAttachResult.
interface BinkAttachResult {
    width: number;
    height: number;
    devicePixelRatio: number;
    maxTouchPoints: number;
    coalescedEvents: boolean;
    predictedEvents: boolean;
    pointerEvents: boolean;
    tilt: boolean;
    twist: boolean;
    offscreenCanvas: boolean;
}

interface BinkSizeResult {
    width: number;
    height: number;
    devicePixelRatio: number;
}

interface BinkImageMetrics {
    width: number;
    height: number;
}

interface BinkTextMetrics {
    width: number;
    lines: number;
}

interface BinkFileResult {
    name: string;
    type: string;
    bytes: Uint8Array;
}

interface WetInkStyle {
    color: string;
    width: number;
    opacity: number;
}

// The zero-copy WebAssembly path hands over a .NET MemoryView rather than an array.
interface MemoryViewLike {
    slice(start?: number, end?: number): ArrayLike<number>;
}

type CommandSource = ArrayLike<number> | MemoryViewLike | null | undefined;

// The subset of a PointerEvent a sample is built from. A real PointerEvent satisfies it, and so does the synthetic
// cancel sample retractTouchContacts produces.
interface PointerSampleSource {
    pointerId: number;
    pointerType: string;
    clientX: number;
    clientY: number;
    pressure: number;
    tiltX: number;
    tiltY: number;
    twist: number;
    timeStamp: number;
    buttons: number;
    isPrimary: boolean;
    shiftKey: boolean;
    ctrlKey: boolean;
    metaKey: boolean;
    altKey: boolean;
}

interface SurfaceContexts {
    document: CanvasRenderingContext2D | null;
    overlay: CanvasRenderingContext2D | null;
    export?: CanvasRenderingContext2D | null;
    [layer: string]: CanvasRenderingContext2D | null | undefined;
}

interface IsolatedGroup {
    parent: CanvasRenderingContext2D;
    canvas: HTMLCanvasElement;
    opacity: number;
    blend: number;
}

interface PendingBlur {
    editor: HTMLTextAreaElement;
    value: string;
}

interface WetInkPoint {
    x: number;
    y: number;
}

// Kept on globalThis so the module can be imported twice (once through Blazor's IJSRuntime and once through
// the WebAssembly runtime's zero-copy import) while both views share the same surfaces.
const bitBinkGlobal = globalThis as typeof globalThis & { __bitBinkSurfaces?: Map<string, Surface> };
const surfaces = (bitBinkGlobal.__bitBinkSurfaces ??= new Map<string, Surface>());

// Packed pointer sample layout, matching BinkInputSample.ReadFrom in C#.
const SAMPLE_SIZE = 12;
// The most samples sent to .NET in one interop message when it goes through IJSRuntime: about 12 KB as JSON.
const SAMPLES_PER_MESSAGE = 128;
const FLAG_COALESCED = 1;
const FLAG_PREDICTED = 2;
const FLAG_PRIMARY = 4;
const FLAG_SHIFT = 8;
const FLAG_CTRL = 16;
const FLAG_ALT = 32;

const PHASE_DOWN = 0;
const PHASE_MOVE = 1;
const PHASE_UP = 2;
const PHASE_CANCEL = 3;
const PHASE_HOVER = 4;

const POINTER_KIND: Readonly<Record<string, number>> = { mouse: 1, touch: 2, pen: 3 };

// Decoded images are held so a redraw does not re-decode. The cache is bounded because a deleted or undone
// image element is never referenced again and would otherwise be retained for the life of the page. Eviction is
// least-recently-used and never touches an image the last rendered frame drew, so a document with more images
// than the cache nominally holds still renders all of them.
const MAX_CACHED_IMAGES = 128;

// Keys the canvas acts on. The browser default is suppressed for these so the page does not also scroll,
// select all its text, or navigate back.
const OWNED_KEYS = new Set(['Escape', 'Delete', 'Backspace', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown']);
// Of those, the ones that only do something when the selection is not empty.
const SELECTION_KEYS = new Set(['Delete', 'Backspace', 'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown']);
const OWNED_SHORTCUTS = new Set(['z', 'y', 'a', '0', '9', '+', '=', '-']);
// Shortcuts that act on the selection: duplicate, the four arrange commands (Shift turns ] and [ into } and {) and
// group/ungroup. Ctrl+D would otherwise bookmark the page and Ctrl+G find the next match.
const SELECTION_SHORTCUTS = new Set(['d', ']', '[', '}', '{', 'g']);
// What Shift makes of a key on a common layout, matching BinkShortcut.Unshifted in .NET.
const UNSHIFTED_KEYS: Record<string, string> = {
    '}': ']', '{': '[', '+': '=', '_': '-', '!': '1', '@': '2', '#': '3', '$': '4', '%': '5', '^': '6', '&': '7', '*': '8', '(': '9', ')': '0',
};

// Command opcodes, matching BinkCanvasCommand in C#.
const OP = {
    BEGIN_PATH: 0,
    MOVE_TO: 1,
    LINE_TO: 2,
    QUAD_TO: 3,
    CUBIC_TO: 4,
    CLOSE_PATH: 5,
    FILL: 6,
    STROKE: 7,
    SAVE: 8,
    RESTORE: 9,
    TRANSFORM: 10,
    CLIP_RECT: 11,
    DRAW_IMAGE: 12,
    DRAW_TEXT: 13,
    PUSH_GROUP: 14,
    POP_GROUP: 15,
    CLEAR: 16,
    SET_TRANSFORM: 17,
    SET_DASH: 18,
    CLEAR_RECT: 19,
    DEFINE_PATH: 20,
    END_PATH: 21,
    USE_PATH: 22,
    FREE_PATHS: 23,
    RETAIN_STRING: 24,
    FREE_STRINGS: 25,
};

const BLEND_NAMES: GlobalCompositeOperation[] = ['source-over', 'multiply', 'screen', 'destination-out'];
const CAP_NAMES: CanvasLineCap[] = ['butt', 'round', 'square'];
const JOIN_NAMES: CanvasLineJoin[] = ['miter', 'round', 'bevel'];
const ALIGN_NAMES: CanvasTextAlign[] = ['left', 'center', 'right'];

// The zero-copy WebAssembly path hands over a MemoryView, which exposes slice/set but not element access.
// One slice is a memcpy of the frame's commands, still far cheaper than serializing them. Every other host sends
// the buffer as bytes holding little-endian 32-bit floats, which Blazor transfers as binary.
function normalizeBuffer(buffer: CommandSource, count: number): ArrayLike<number> | null {
    if (!buffer) return null;
    if (buffer instanceof Uint8Array) {
        const length = Math.min(count, buffer.byteLength >> 2);
        // A Float32Array view needs a 4-byte aligned offset; a misaligned one is copied into a buffer of its own.
        return buffer.byteOffset % 4 === 0
            ? new Float32Array(buffer.buffer, buffer.byteOffset, length)
            : new Float32Array(buffer.slice(0, length * 4).buffer);
    }
    if (Array.isArray(buffer) || ArrayBuffer.isView(buffer)) return buffer as ArrayLike<number>;
    if (typeof (buffer as MemoryViewLike).slice === 'function') return (buffer as MemoryViewLike).slice(0, count);
    return null;
}

// Colours arrive as a 24-bit RGB value and an alpha byte. The CSS strings are cached because a page of strokes in a
// handful of colours would otherwise build the same string, and have the canvas parse it, once per element per frame.
const colorStrings = new Map<number, string>();

function cssColor(rgb: number, a: number): string {
    const key = rgb * 256 + a;
    let css = colorStrings.get(key);
    if (css !== undefined) return css;
    const r = (rgb >>> 16) & 0xff;
    const g = (rgb >>> 8) & 0xff;
    const b = rgb & 0xff;
    css = a >= 255 ? `rgb(${r},${g},${b})` : `rgba(${r},${g},${b},${(a / 255).toFixed(4)})`;
    if (colorStrings.size >= 512) colorStrings.clear();
    colorStrings.set(key, css);
    return css;
}

class Surface {
    id: string;
    root: HTMLElement;
    documentCanvas: HTMLCanvasElement | null;
    overlayCanvas: HTMLCanvasElement | null;
    hitTarget: HTMLElement;
    options: BinkSurfaceOptions;
    dpr: number;
    cssWidth: number;
    cssHeight: number;
    samples: Float64Array;
    sampleCount: number;
    frameRequested: boolean;
    frameHandle: number;
    activePointers: Map<number, string>;
    ownsSelectionKeys: boolean;
    penActive: boolean;
    disposed: boolean;
    wetInk: Map<number, WetInkPoint>;
    wetInkStopped: Set<number>;
    images: Map<string, HTMLImageElement>;
    imagesUsedThisFrame: Set<string>;
    pendingImages: Set<string>;
    failedImages: Set<string>;
    redrawRequested: boolean;
    redrawHandle: number;
    pendingBlur: PendingBlur | null;
    frameToken: number;
    rectFrame: number;
    cachedRect: DOMRect | null;
    pixelRatioQuery: MediaQueryList | null;
    onPixelRatioChange: (() => void) | null;
    groupStack: IsolatedGroup[];
    scratchCanvases: HTMLCanvasElement[];
    // Paths and long strings .NET asked this surface to keep, by slot. .NET decides what is kept and for how long;
    // this side only stores what it is told to and releases what it is told to.
    paths: Map<number, Path2D>;
    retainedStrings: Map<number, string>;
    // Wheel movement since the last frame, delivered with it: a trackpad fires far more wheel events than frames.
    wheelPanX: number;
    wheelPanY: number;
    wheelZoom: number;
    wheelX: number;
    wheelY: number;
    pendingClipboard: PendingClipboardWrite | null;
    // Set when writing the system clipboard failed, so the next paste uses the copy .NET kept instead of whatever
    // older content the system clipboard still holds. That paste consumes it and remembers the text the clipboard
    // held instead: while the clipboard still holds that text, .NET's copy keeps being pasted, and once the user has
    // copied something else anywhere, that is what a paste brings.
    clipboardWriteFailed: boolean;
    staleClipboardText: string | null;
    contexts: SurfaceContexts;
    resizeObserver: ResizeObserver;
    // Assigned after construction (or lazily) rather than in the constructor.
    callback?: BinkSurfaceCallbacks | null;
    wetInkStyle?: WetInkStyle | null;

    onPointerDown: (event: PointerEvent) => void;
    onPointerMove: (event: PointerEvent) => void;
    onPointerUp: (event: PointerEvent) => void;
    onPointerCancel: (event: PointerEvent) => void;
    onContextMenu: (event: MouseEvent) => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onFocusOut: (event: FocusEvent) => void;
    onWheel: (event: WheelEvent) => void;
    onPaste: (event: ClipboardEvent) => void;
    onClipboardElsewhere: (event: Event) => void;
    onDragOver: (event: DragEvent) => void;
    onDrop: (event: DragEvent) => void;
    onFrame: () => void;
    onLostPointerCapture: (event: PointerEvent) => void;
    onContextRestored: () => void;

    constructor(id: string, root: HTMLElement, options: BinkSurfaceOptions | null | undefined) {
        this.id = id;
        this.root = root;
        this.documentCanvas = root.querySelector<HTMLCanvasElement>('[data-bink-layer="document"]');
        this.overlayCanvas = root.querySelector<HTMLCanvasElement>('[data-bink-layer="overlay"]');
        this.hitTarget = root.querySelector<HTMLElement>('[data-bink-layer="input"]') || root;
        this.options = options || {};
        this.dpr = 1;
        this.cssWidth = 0;
        this.cssHeight = 0;
        this.samples = new Float64Array(SAMPLE_SIZE * 256);
        this.sampleCount = 0;
        this.frameRequested = false;
        this.frameHandle = 0;
        this.activePointers = new Map();
        this.ownsSelectionKeys = false;
        this.penActive = false;
        this.disposed = false;
        this.wetInk = new Map();
        this.wetInkStopped = new Set();
        this.images = new Map();
        this.imagesUsedThisFrame = new Set();
        this.pendingImages = new Set();
        this.failedImages = new Set();
        this.redrawRequested = false;
        this.redrawHandle = 0;
        this.pendingBlur = null;
        this.frameToken = 0;
        this.rectFrame = -1;
        this.cachedRect = null;
        this.pixelRatioQuery = null;
        this.onPixelRatioChange = null;
        this.groupStack = [];
        this.scratchCanvases = [];
        this.paths = new Map();
        this.retainedStrings = new Map();
        this.wheelPanX = 0;
        this.wheelPanY = 0;
        this.wheelZoom = 0;
        this.wheelX = 0;
        this.wheelY = 0;
        this.pendingClipboard = null;
        this.clipboardWriteFailed = false;
        this.staleClipboardText = null;

        this.contexts = {
            document: this.createContext(this.documentCanvas),
            overlay: this.createContext(this.overlayCanvas),
        };

        this.onPointerDown = this.handlePointerDown.bind(this);
        this.onPointerMove = this.handlePointerMove.bind(this);
        this.onPointerUp = this.handlePointerUp.bind(this);
        this.onPointerCancel = this.handlePointerCancel.bind(this);
        this.onContextMenu = (e) => { if (this.options.suppressContextMenu) e.preventDefault(); };
        this.onKeyDown = this.handleKeyDown.bind(this);
        this.onFocusOut = this.handleFocusOut.bind(this);
        this.onWheel = this.handleWheel.bind(this);
        this.onPaste = this.handlePaste.bind(this);
        this.onClipboardElsewhere = this.handleClipboardElsewhere.bind(this);
        this.onDragOver = this.handleDragOver.bind(this);
        this.onDrop = this.handleDrop.bind(this);
        this.onFrame = this.flush.bind(this);
        this.onLostPointerCapture = this.handleLostPointerCapture.bind(this);
        this.onContextRestored = () => this.requestRedraw();

        const target = this.hitTarget;
        target.addEventListener('pointerdown', this.onPointerDown);
        target.addEventListener('pointermove', this.onPointerMove);
        target.addEventListener('pointerup', this.onPointerUp);
        target.addEventListener('pointercancel', this.onPointerCancel);
        target.addEventListener('pointerleave', this.onPointerCancel);
        target.addEventListener('lostpointercapture', this.onLostPointerCapture);
        target.addEventListener('contextmenu', this.onContextMenu);
        // Not passive: a canvas that pans and zooms with the wheel must stop the page from scrolling at the same
        // time. With the wheel off the handler returns at once and the page scrolls as usual.
        target.addEventListener('wheel', this.onWheel, { passive: false });
        root.addEventListener('keydown', this.onKeyDown);
        root.addEventListener('focusout', this.onFocusOut);
        root.addEventListener('paste', this.onPaste);
        // Coming back from another window, or copying elsewhere on the page, may have put something newer on the
        // system clipboard than the copy that failed to reach it.
        window.addEventListener('focus', this.onClipboardElsewhere);
        document.addEventListener('copy', this.onClipboardElsewhere);
        document.addEventListener('cut', this.onClipboardElsewhere);
        root.addEventListener('dragover', this.onDragOver);
        root.addEventListener('drop', this.onDrop);
        // A browser may discard a canvas's backing store (GPU reset, a long-backgrounded tab on a phone) and later
        // restore it blank. Nothing in the document changed, so without this nothing would ever repaint it.
        for (const canvas of [this.documentCanvas, this.overlayCanvas]) canvas?.addEventListener('contextrestored', this.onContextRestored);

        this.resizeObserver = new ResizeObserver(() => this.notifyResize());
        this.resizeObserver.observe(root);

        // A change of device pixel ratio (moving the window to another display, browser zoom) must re-scale the
        // backing store or the drawing turns blurry.
        this.watchPixelRatio();
    }

    createContext(canvas: HTMLCanvasElement | null): CanvasRenderingContext2D | null {
        if (!canvas) return null;
        // Never `desynchronized`. Chromium (seen on Windows with GPU compositing) shows a desynchronized canvas whose
        // pixels are not all opaque - the overlay always, the document with a transparent background - as solid black
        // once it sits inside an ancestor that clips to rounded corners: overflow: hidden with a border-radius, an
        // ordinary card. The live stroke is drawn on the overlay, so that is also the only layer where the hint would
        // have shortened the latency a user feels.
        return canvas.getContext('2d', { alpha: true });
    }

    watchPixelRatio(): void {
        if (typeof matchMedia !== 'function') return;
        const listen = (): void => {
            if (this.disposed) return;
            this.pixelRatioQuery = matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`);
            // Kept in a field so dispose can detach it: a media query listener is registered on the document and
            // would keep this surface, its canvases and its image cache alive until the ratio happened to change.
            this.onPixelRatioChange = () => {
                this.notifyResize();
                listen();
            };
            this.pixelRatioQuery.addEventListener('change', this.onPixelRatioChange, { once: true });
        };
        listen();
    }

    stopWatchingPixelRatio(): void {
        if (this.pixelRatioQuery && this.onPixelRatioChange) {
            this.pixelRatioQuery.removeEventListener('change', this.onPixelRatioChange);
        }
        this.pixelRatioQuery = null;
        this.onPixelRatioChange = null;
    }

    setSize(cssWidth: number, cssHeight: number, dpr: number): void {
        this.cssWidth = cssWidth;
        this.cssHeight = cssHeight;
        this.dpr = dpr;
        const pixelWidth = Math.max(1, Math.round(cssWidth * dpr));
        const pixelHeight = Math.max(1, Math.round(cssHeight * dpr));
        for (const canvas of [this.documentCanvas, this.overlayCanvas]) {
            if (!canvas) continue;
            if (canvas.width !== pixelWidth || canvas.height !== pixelHeight) {
                canvas.width = pixelWidth;
                canvas.height = pixelHeight;
            }
            canvas.style.width = `${cssWidth}px`;
            canvas.style.height = `${cssHeight}px`;
        }
        this.scratchCanvases.length = 0;
        // The first size comes from the measurement taken at attach. The resize observer's first report arrives before
        // .NET has wired its callbacks, so a layout change in that window (a scrollbar appearing, a late stylesheet) was
        // seen by nobody and the backing store would keep the stale size. Reporting a mismatch lets .NET correct it,
        // and it settles at once because the corrected call carries the size measured here.
        // Reported from a task of its own rather than from inside the call .NET is making.
        const rect = this.root.getBoundingClientRect();
        if (Math.abs(rect.width - cssWidth) > 0.5 || Math.abs(rect.height - cssHeight) > 0.5
            || Math.abs((window.devicePixelRatio || 1) - dpr) > 0.001) {
            setTimeout(() => this.notifyResize(), 0);
        }
    }

    notifyResize(): void {
        if (this.disposed || !this.callback) return;
        const rect = this.root.getBoundingClientRect();
        this.callback.resized(rect.width, rect.height, window.devicePixelRatio || 1);
    }

    // ---- Input ------------------------------------------------------------------------------------------------

    shouldIgnore(event: PointerEvent): boolean {
        // Palm rejection: while a pen is in contact, touch contacts are almost always the hand resting on the screen.
        if (this.options.rejectTouchWhilePen !== false && this.penActive && event.pointerType === 'touch') return true;
        return false;
    }

    // The element rectangle is read once per event rather than once per sample: getBoundingClientRect forces a
    // layout, and a 240 Hz pen delivers dozens of coalesced samples per event.
    currentRect(): DOMRect {
        if (this.rectFrame !== this.frameToken) {
            this.cachedRect = this.hitTarget.getBoundingClientRect();
            this.rectFrame = this.frameToken;
        }
        // Always set by the branch above on the first call: rectFrame starts at -1 and frameToken at 0.
        return this.cachedRect as DOMRect;
    }

    push(event: PointerSampleSource, phase: number, flags: number): void {
        if (this.sampleCount * SAMPLE_SIZE + SAMPLE_SIZE > this.samples.length) {
            const grown = new Float64Array(this.samples.length * 2);
            grown.set(this.samples);
            this.samples = grown;
        }
        const rect = this.currentRect();
        const o = this.sampleCount * SAMPLE_SIZE;
        const s = this.samples;
        s[o] = event.pointerId;
        s[o + 1] = POINTER_KIND[event.pointerType] || 0;
        s[o + 2] = phase;
        s[o + 3] = event.clientX - rect.left;
        s[o + 4] = event.clientY - rect.top;
        s[o + 5] = event.pressure;
        s[o + 6] = event.tiltX || 0;
        s[o + 7] = event.tiltY || 0;
        s[o + 8] = event.twist || 0;
        s[o + 9] = event.timeStamp;
        s[o + 10] = event.buttons;
        s[o + 11] = flags
            | (event.isPrimary ? FLAG_PRIMARY : 0)
            | (event.shiftKey ? FLAG_SHIFT : 0)
            | ((event.ctrlKey || event.metaKey) ? FLAG_CTRL : 0)
            | (event.altKey ? FLAG_ALT : 0);
        this.sampleCount++;
    }

    handlePointerDown(event: PointerEvent): void {
        if (this.shouldIgnore(event)) return;
        if (event.pointerType === 'pen' && !this.penActive) {
            this.penActive = true;
            this.retractTouchContacts(event);
        }
        // Capturing on the element keeps the gesture alive when the pointer leaves the canvas.
        try { this.hitTarget.setPointerCapture(event.pointerId); } catch { /* capture is best effort */ }
        this.activePointers.set(event.pointerId, event.pointerType);
        // A second finger turns the touch into a pan or pinch. The provisional trail stops for every finger in it,
        // and stays stopped until each lifts: resumed when only one finger is left, it would draw a line from where
        // that finger was before the gesture to where it is now.
        if (event.pointerType === 'touch' && this.touchContacts() >= 2) {
            for (const [pointerId, kind] of this.activePointers) {
                if (kind !== 'touch') continue;
                this.wetInk.delete(pointerId);
                this.wetInkStopped.add(pointerId);
            }
        }
        this.push(event, PHASE_DOWN, 0);
        if (this.options.preventDefaultOnDown !== false) {
            // Preventing the default stops text selection and scrolling, but it also stops the browser from moving
            // focus. Without this the canvas would never be focused by drawing on it and its keyboard shortcuts
            // would be unreachable for anyone using a mouse or a pen.
            event.preventDefault();
            if (this.root.tabIndex >= 0 && document.activeElement !== this.root) {
                this.root.focus({ preventScroll: true });
            }
        }
        this.requestFlush();
    }

    // A hand usually lands before the nib does. Once a pen arrives, every touch contact already in progress was
    // the palm, so those gestures are cancelled rather than left to commit a blob when the hand lifts.
    retractTouchContacts(event: PointerEvent): void {
        if (this.options.rejectTouchWhilePen === false) return;
        for (const [pointerId, kind] of this.activePointers) {
            if (kind !== 'touch') continue;
            this.activePointers.delete(pointerId);
            this.endWetInk(pointerId);
            this.push({
                pointerId,
                pointerType: 'touch',
                clientX: event.clientX,
                clientY: event.clientY,
                pressure: 0,
                tiltX: 0,
                tiltY: 0,
                twist: 0,
                timeStamp: event.timeStamp,
                buttons: 0,
                isPrimary: false,
                shiftKey: false,
                ctrlKey: false,
                metaKey: false,
                altKey: false,
            }, PHASE_CANCEL, 0);
        }
    }

    handlePointerMove(event: PointerEvent): void {
        if (this.shouldIgnore(event)) return;
        const isDown = this.activePointers.has(event.pointerId);
        if (!isDown && !this.options.trackHover) return;

        if (isDown && typeof event.getCoalescedEvents === 'function') {
            // Coalesced events carry every sample the digitizer produced between frames, which is what makes
            // fast handwriting smooth on a 240 Hz pen.
            const coalesced = event.getCoalescedEvents();
            if (coalesced && coalesced.length > 1) {
                // The last coalesced sample is the dispatched event itself, so it is not flagged: a tool that skips
                // intermediate samples must still see where each pointer got to, including when predictions follow
                // or another pointer's samples come after it in the same batch.
                const last = coalesced.length - 1;
                for (let i = 0; i < coalesced.length; i++) this.push(coalesced[i], PHASE_MOVE, i < last ? FLAG_COALESCED : 0);
            } else {
                this.push(event, PHASE_MOVE, 0);
            }
            // Where the browser expects the pointer to be next, after the measured samples. .NET draws them as the tip
            // of the live stroke and drops them as soon as the next measured sample arrives.
            if (this.options.predict && typeof event.getPredictedEvents === 'function') {
                const predicted = event.getPredictedEvents();
                if (predicted) for (const sample of predicted) this.push(sample, PHASE_MOVE, FLAG_PREDICTED);
            }
        } else {
            this.push(event, isDown ? PHASE_MOVE : PHASE_HOVER, 0);
        }

        if (isDown) {
            // No provisional ink for the pen's eraser end, which erases rather than draws, or for a finger that is
            // part of a pan or pinch.
            if (this.options.wetInk && (event.buttons & 32) === 0 && !this.wetInkStopped.has(event.pointerId)) this.appendWetInk(event);
            event.preventDefault();
        }
        this.requestFlush();
    }

    touchContacts(): number {
        let count = 0;
        for (const kind of this.activePointers.values()) if (kind === 'touch') count++;
        return count;
    }

    handlePointerUp(event: PointerEvent): void {
        if (!this.activePointers.delete(event.pointerId)) return;
        if (event.pointerType === 'pen') this.penActive = false;
        try { this.hitTarget.releasePointerCapture(event.pointerId); } catch { /* already released */ }
        this.push(event, PHASE_UP, 0);
        this.endWetInk(event.pointerId);
        this.requestFlush();
    }

    handlePointerCancel(event: PointerEvent): void {
        if (!this.activePointers.delete(event.pointerId)) return;
        if (event.pointerType === 'pen') this.penActive = false;
        this.push(event, PHASE_CANCEL, 0);
        this.endWetInk(event.pointerId);
        this.requestFlush();
    }

    // Capture is released after pointerup and pointercancel, and by then the pointer is no longer active. Losing it
    // while the pointer is still down means no pointerup will reach this element, so the gesture is cancelled rather
    // than left active: a pen left marked active would otherwise reject every touch until the next pen contact.
    handleLostPointerCapture(event: PointerEvent): void {
        if (this.activePointers.has(event.pointerId)) this.handlePointerCancel(event);
    }

    // Keyboard shortcuts travel the same path as pointer input, so the component never depends on a framework
    // event and never re-renders per keystroke.
    handleKeyDown(event: KeyboardEvent): void {
        if (this.disposed || !this.callback) return;
        // An input method owns the keys while it composes. Safari ends the composition before the keydown of the
        // Enter that confirms it, so that keydown reports isComposing false and only its keyCode, 229, says it was
        // the input method's key; committing the text editor on it would cut a Japanese or Chinese word in half.
        if (event.isComposing || event.keyCode === 229) return;

        // While the inline text editor is open it owns the keyboard. Forwarding these to the canvas would mean
        // typing "Delete" in a comment deleted the selection. Enter and Escape commit and cancel it whether or not
        // the canvas's own shortcuts are on: they are how the editor works, not shortcuts.
        const editor = this.textEditor();
        if (editor && event.target === editor) {
            if (event.key === 'Escape' || (event.key === 'Enter' && !event.shiftKey)) {
                event.preventDefault();
                this.callback.textKey(event.key, editor.value);
            }
            return;
        }
        if (this.options.handleKeyboard === false) return;

        // ChildContent renders inside the same element, so a caption field or a button in it would otherwise have
        // Ctrl+A, Delete and the arrow keys taken away by the canvas.
        if (event.target !== this.root && this.isInteractiveTarget(event.target as HTMLElement | null)) return;
        // A shortcut the application took for itself is left entirely to the page.
        if (this.isDisabledShortcut(event)) return;

        if (this.ownsKey(event)) event.preventDefault();
        const ctrl = event.ctrlKey || event.metaKey;
        const lower = event.key.toLowerCase();
        if (ctrl && !event.altKey && (lower === 'c' || lower === 'x') && this.options.clipboard && this.ownsSelectionKeys) this.beginClipboardWrite();
        // Pointer samples wait for the next animation frame, a key does not. A shortcut pressed right after the mouse
        // was released would otherwise reach .NET before the release did: Ctrl+Z while the drag's undo group is still
        // open is refused, and Delete would act on the selection as it was before the click that changed it.
        if (this.sampleCount > 0) this.flush();
        this.callback.key(event.key, ctrl, event.shiftKey, event.altKey);
    }

    // The chord in the canonical form BinkShortcut gives in .NET: Ctrl (or Command), Alt, Shift, then the key with a
    // single character upper-cased. Shift turns ] into } on most layouts, so the unshifted spelling is tried as well.
    isDisabledShortcut(event: KeyboardEvent): boolean {
        const disabled = this.options.disabledShortcuts;
        if (!disabled || disabled.length === 0) return false;
        const modifiers = ((event.ctrlKey || event.metaKey) ? 'Ctrl+' : '') + (event.altKey ? 'Alt+' : '') + (event.shiftKey ? 'Shift+' : '');
        const key = event.key.length === 1 ? event.key.toUpperCase() : event.key;
        if (disabled.includes(modifiers + key)) return true;
        const unshifted = event.shiftKey && event.key.length === 1 ? UNSHIFTED_KEYS[event.key] : undefined;
        return unshifted !== undefined && disabled.includes(modifiers + unshifted);
    }

    isInteractiveTarget(target: HTMLElement | null): boolean {
        if (!target || typeof target.closest !== 'function') return false;
        if (target.isContentEditable) return true;
        return target.closest('input, textarea, select, button, a[href], [contenteditable="true"], .bit-bink-content') !== null;
    }

    // Delete, Backspace and the arrow keys do something only when there is a selection. Swallowing them when there
    // is not would leave a keyboard user unable to scroll the page while the canvas has focus.
    ownsKey(event: KeyboardEvent): boolean {
        const key = event.key;
        const ctrl = event.ctrlKey || event.metaKey;
        const lower = key.toLowerCase();
        if (ctrl && OWNED_SHORTCUTS.has(lower)) return true;
        // Copy and cut are the canvas's only while something is selected and the clipboard is on; with nothing
        // selected they belong to the page, so copying text next to the canvas still works. Paste is never taken
        // here: suppressing the key would also suppress the paste event, which is the only way to read the clipboard
        // without a permission prompt.
        if (ctrl && (lower === 'c' || lower === 'x')) return !!this.options.clipboard && this.ownsSelectionKeys;
        if (ctrl && SELECTION_SHORTCUTS.has(lower)) return this.ownsSelectionKeys;
        // Ctrl+Shift+L locks the selection. Only with Shift: Ctrl+L is the browser's address bar.
        if (ctrl && event.shiftKey && lower === 'l') return this.ownsSelectionKeys;
        if (key === 'Escape') return true;
        // Tab only belongs to the canvas while the user is stepping through elements. With nothing selected it moves
        // browser focus out as usual, so the canvas is never a keyboard trap: Escape clears the selection and leaves.
        if (key === 'Tab') return this.ownsSelectionKeys;
        if (key === 'Enter') return true;
        if (SELECTION_KEYS.has(key)) return this.ownsSelectionKeys;
        return false;
    }

    // Told by .NET whether anything is selected, because only .NET knows.
    setSelectionActive(active: boolean): void {
        this.ownsSelectionKeys = !!active;
    }

    // ---- Wheel ---------------------------------------------------------------------------------------------------

    // A mouse wheel and a trackpad both arrive as wheel events; a trackpad pinch arrives as a wheel event with
    // ctrlKey set, which is what Chrome, Edge and Firefox send. Movement is accumulated and delivered once per frame.
    handleWheel(event: WheelEvent): void {
        const mode = this.options.wheelMode || 0;
        if (!mode || this.disposed || !this.callback) return;
        event.preventDefault();
        // The view must not move under a stroke or a drag that is still in progress: its points are being mapped
        // through the viewport they started with.
        if (this.activePointers.size > 0) return;

        let dx = event.deltaX;
        let dy = event.deltaY;
        if (event.deltaMode === 1) {
            dx *= 16;
            dy *= 16;
        } else if (event.deltaMode === 2) {
            dx *= this.cssWidth || 800;
            dy *= this.cssHeight || 600;
        }
        const zoom = event.ctrlKey || event.metaKey || (mode === 2 && !event.shiftKey);
        if (zoom) {
            // Clamped per event so one notch of a coarse mouse wheel and the fine steps of a pinch both feel even.
            this.wheelZoom -= Math.max(-50, Math.min(50, dy)) * 0.006;
        } else {
            // Shift turns a vertical wheel into horizontal panning, for mice without a horizontal wheel.
            if (event.shiftKey && dx === 0) {
                dx = dy;
                dy = 0;
            }
            this.wheelPanX += dx;
            this.wheelPanY += dy;
        }
        const rect = this.currentRect();
        this.wheelX = event.clientX - rect.left;
        this.wheelY = event.clientY - rect.top;
        this.requestFlush();
    }

    // ---- Clipboard -----------------------------------------------------------------------------------------------

    // Browsers only allow writing the clipboard while handling the user's key press, and the payload is serialized in
    // .NET, a round trip away on Blazor Server. A clipboard item accepts a promise for its content, so the item is
    // created here, inside the key event, and the promise is resolved when .NET delivers the payload.
    beginClipboardWrite(): void {
        this.settleClipboard(null);
        const clipboard = typeof navigator !== 'undefined' ? navigator.clipboard : undefined;
        const Item = (globalThis as { ClipboardItem?: typeof ClipboardItem }).ClipboardItem;
        if (!clipboard || typeof clipboard.write !== 'function' || !Item) return;
        let resolve!: (blob: Blob) => void;
        let reject!: (reason: Error) => void;
        let resolveImage!: (blob: Blob) => void;
        let rejectImage!: (reason: Error) => void;
        const content = new Promise<Blob>((res, rej) => { resolve = res; reject = rej; });
        const image = new Promise<Blob>((res, rej) => { resolveImage = res; rejectImage = rej; });
        // An image whose write is never attempted must not be reported as an unhandled rejection.
        image.catch(() => undefined);
        // A payload that never comes (the circuit dropped) must not hold the clipboard write open for ever.
        const timer = setTimeout(() => this.settleClipboard(null), 10000) as unknown as number;
        const pending: PendingClipboardWrite = { resolve, reject, resolveImage, rejectImage, timer, withdrawn: false };
        this.pendingClipboard = pending;
        // The text is Bink's own format; the picture is what chat applications and documents take.
        const items: Record<string, Promise<Blob>> = { 'text/plain': content };
        const supports = (Item as unknown as { supports?: (type: string) => boolean }).supports;
        if (typeof supports !== 'function' || supports.call(Item, 'image/png')) items['image/png'] = image;
        try {
            // A write this layer withdrew itself, because .NET had nothing to copy or never answered, did not fail:
            // the system clipboard simply keeps what it held. Only a write the browser refused means the next paste
            // has to use the copy .NET kept instead.
            clipboard.write([new Item(items)]).then(
                () => this.noteClipboardWrite(false),
                () => { if (!pending.withdrawn) this.noteClipboardWrite(true); });
        } catch {
            this.noteClipboardWrite(true);
            this.settleClipboard(null);
        }
    }

    // Called by .NET with the serialized selection, or null when there was nothing to copy, and the picture of it.
    provideClipboard(text: string | null, image: Promise<Blob | null> | null = null): void {
        if (this.pendingClipboard) {
            this.settleClipboard(text, image);
            return;
        }
        // A copy started from the application rather than the keyboard has no clipboard item waiting. Writing it
        // directly works wherever the page still has the user activation of the click that started it.
        if (!text) return;
        const clipboard = typeof navigator !== 'undefined' ? navigator.clipboard : undefined;
        const Item = (globalThis as { ClipboardItem?: typeof ClipboardItem }).ClipboardItem;
        const writeText = () => {
            if (!clipboard || typeof clipboard.writeText !== 'function') {
                this.noteClipboardWrite(true);
                return;
            }
            clipboard.writeText(text).then(
                () => this.noteClipboardWrite(false),
                () => this.noteClipboardWrite(true));
        };
        if (!image || !clipboard || typeof clipboard.write !== 'function' || !Item) {
            writeText();
            return;
        }
        const picture = image.then((blob) => blob ?? Promise.reject(new Error('No picture of the selection.')));
        clipboard.write([new Item({ 'text/plain': new Blob([text], { type: 'text/plain' }), 'image/png': picture })]).then(
            () => this.noteClipboardWrite(false),
            // Text alone is still worth having when the picture could not be made or written.
            writeText);
    }

    noteClipboardWrite(failed: boolean): void {
        this.clipboardWriteFailed = failed;
        if (!failed) this.staleClipboardText = null;
    }

    // The user may have copied something else, so the next paste takes what the system clipboard holds rather than
    // the copy .NET kept. A copy or cut inside this canvas is its own and changes nothing.
    handleClipboardElsewhere(event: Event): void {
        if (event.type !== 'focus' && event.target instanceof Node && this.root.contains(event.target)) return;
        this.clipboardWriteFailed = false;
    }

    settleClipboard(text: string | null, image: Promise<Blob | null> | null = null): void {
        const pending = this.pendingClipboard;
        if (!pending) return;
        this.pendingClipboard = null;
        clearTimeout(pending.timer);
        if (text) {
            pending.resolve(new Blob([text], { type: 'text/plain' }));
            (image ?? Promise.resolve(null)).then(
                (blob) => (blob ? pending.resolveImage(blob) : pending.rejectImage(new Error('No picture of the selection.'))),
                (error) => pending.rejectImage(error instanceof Error ? error : new Error(String(error))));
        } else {
            pending.withdrawn = true;
            pending.reject(new Error('Nothing to copy.'));
            pending.rejectImage(new Error('Nothing to copy.'));
        }
    }

    // The paste event is the one way to read the clipboard without a permission prompt. A drawing copied from a
    // canvas, an image and plain text are each reported for .NET to decide what to do with.
    handlePaste(event: ClipboardEvent): void {
        if (!this.options.clipboard || this.disposed || !this.callback || !this.callback.paste) return;
        if (event.target !== this.root && this.isInteractiveTarget(event.target as HTMLElement | null)) return;
        const data = event.clipboardData;
        event.preventDefault();
        // As for keys: what the pointer did before the paste has to reach .NET before the paste does.
        if (this.sampleCount > 0) this.flush();
        const text = data ? data.getData('text/plain') : '';
        // Copying from this canvas failed to reach the system clipboard, so what it holds is older than what the user
        // just copied here; .NET kept its own copy for exactly this case. What the clipboard holds now is remembered
        // as the stale content: as long as it is still there .NET's copy is pasted, and anything the user copies
        // later, anywhere, is pasted as it should be. An empty clipboard, or one holding only a picture, is not
        // remembered, because it could not be told from a picture copied afterwards.
        if (this.clipboardWriteFailed) {
            this.clipboardWriteFailed = false;
            this.staleClipboardText = text || null;
            this.callback.paste('internal', null, null);
            return;
        }
        if (this.staleClipboardText !== null && text === this.staleClipboardText) {
            this.callback.paste('internal', null, null);
            return;
        }
        this.staleClipboardText = null;
        // A copy from a canvas carries a picture as well as the drawing itself; pasted back into a canvas, it is the
        // drawing that is wanted, as editable elements rather than a picture of them.
        if (text.startsWith(CLIPBOARD_MARKER)) {
            this.callback.paste('bink', text, null);
            return;
        }
        if (data) {
            for (const item of Array.from(data.items)) {
                if (item.kind !== 'file' || !item.type.startsWith('image/')) continue;
                const file = item.getAsFile();
                if (file) {
                    this.callback.paste('image', null, file);
                    return;
                }
            }
        }
        if (!text) {
            this.callback.paste('internal', null, null);
            return;
        }
        this.callback.paste('text', text, null);
    }

    // ---- Drag and drop -------------------------------------------------------------------------------------------

    // A file dragged over the canvas is accepted only when dropping is on and the drag carries files; anything else
    // (text dragged out of the page, a link) keeps the browser's own behaviour.
    handleDragOver(event: DragEvent): void {
        if (!this.options.dropFiles || this.disposed || !this.callback?.drop) return;
        if (!event.dataTransfer || !Array.from(event.dataTransfer.types).includes('Files')) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
    }

    // Each dropped image, and each saved drawing (a .bink or .json file), is reported with where it landed, in CSS
    // pixels of the canvas, a little apart from the one before so several files do not pile up exactly.
    handleDrop(event: DragEvent): void {
        if (!this.options.dropFiles || this.disposed || !this.callback?.drop) return;
        const files = event.dataTransfer ? Array.from(event.dataTransfer.files) : [];
        if (files.length === 0) return;
        event.preventDefault();
        if (this.sampleCount > 0) this.flush();
        const rect = this.hitTarget.getBoundingClientRect();
        let index = 0;
        for (const file of files.slice(0, 16)) {
            const name = file.name.toLowerCase();
            // An SVG is vector art to import, not a picture: .NET reads it into editable elements.
            const kind: DropKind | null = file.type === 'image/svg+xml' || name.endsWith('.svg') ? 'svg'
                : file.type.startsWith('image/') ? 'image'
                : name.endsWith('.bink') || name.endsWith('.json') || file.type === 'application/json' ? 'bink'
                : null;
            if (!kind) continue;
            this.callback.drop(kind, file, event.clientX - rect.left + index * 24, event.clientY - rect.top + index * 24);
            index++;
        }
    }

    textEditor(): HTMLTextAreaElement | null {
        return this.root.querySelector<HTMLTextAreaElement>('textarea.bit-bink-text-editor');
    }

    // Clicking away from an open editor commits what was typed, the way any text field behaves.
    //
    // The call into .NET is deferred to a microtask. Blazor's own DOM edits move focus (re-rendering or removing the
    // editor fires focusout synchronously inside the render batch), and calling .NET from there fails in WebAssembly
    // because the heap is locked for the batch. A microtask only runs once the whole call stack has unwound, which is
    // after the batch has released the lock. It must not wait any longer than that: pressing a toolbar button moves
    // focus on pointerdown, and its click, which may switch tools and close the editor, is the very next task, so a
    // timer would report the typed text only after the editor it belonged to was gone.
    handleFocusOut(event: FocusEvent): void {
        if (this.disposed || !this.callback) return;
        const editor = this.textEditor();
        if (editor && event.target === editor) {
            const first = !this.pendingBlur;
            this.pendingBlur = { editor, value: editor.value };
            if (first) queueMicrotask(() => this.sendPendingBlur());
        }
    }

    // Also called at the start of a sample flush, so a click that moved focus out of the editor is reported to .NET
    // before the pointer samples of that same click, the order the user performed them in.
    sendPendingBlur(): void {
        const pending = this.pendingBlur;
        this.pendingBlur = null;
        if (!pending || this.disposed || !this.callback) return;
        // An editor Blazor removed was closed by .NET itself (a commit, a cancel, a tool change) or replaced by the
        // editor for another element. Reporting its value now would commit it a second time, or onto the wrong element.
        if (!pending.editor.isConnected || this.textEditor() !== pending.editor) return;
        this.callback.textKey('Blur', pending.value);
    }

    requestFlush(): void {
        // A new frame invalidates the cached element rectangle: the page may have scrolled or resized.
        this.frameToken++;
        if (this.frameRequested || this.disposed) return;
        this.frameRequested = true;
        this.frameHandle = requestAnimationFrame(this.onFrame);
    }

    // Delivers one batch of samples per animation frame. Never one call per pointer event.
    flush(): void {
        this.frameRequested = false;
        const count = this.sampleCount;
        // Cleared unconditionally: samples collected before .NET attached its callback, or after a circuit dropped,
        // would otherwise accumulate for ever and grow the buffer without bound.
        this.sampleCount = 0;
        if (this.pendingBlur) this.sendPendingBlur();
        if (this.wheelPanX !== 0 || this.wheelPanY !== 0 || this.wheelZoom !== 0) {
            const panX = this.wheelPanX;
            const panY = this.wheelPanY;
            const zoom = this.wheelZoom;
            this.wheelPanX = 0;
            this.wheelPanY = 0;
            this.wheelZoom = 0;
            if (!this.disposed && this.callback && this.callback.wheel) this.callback.wheel(panX, panY, zoom, this.wheelX, this.wheelY);
        }
        if (this.disposed || !this.callback || count === 0) return;
        this.callback.samples(this.samples, count);
    }

    // ---- Wet ink (Blazor Server latency compensation) ----------------------------------------------------------

    // Draws a provisional trail so the pen feels responsive while the sample batch travels to the server.
    // It carries no stroke model: a constant-width polyline in the brush colour, cleared as soon as the engine
    // renders the real geometry.
    appendWetInk(event: PointerEvent): void {
        const ctx = this.contexts.overlay;
        if (!ctx || !this.wetInkStyle) return;
        const rect = this.currentRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        // Tracked per pointer, so a second contact does not draw a line from where the first one was.
        const previous = this.wetInk.get(event.pointerId);
        if (!previous) {
            this.wetInk.set(event.pointerId, { x, y });
            return;
        }
        ctx.save();
        ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
        ctx.strokeStyle = this.wetInkStyle.color;
        ctx.lineWidth = this.wetInkStyle.width;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        ctx.globalAlpha = this.wetInkStyle.opacity;
        ctx.beginPath();
        ctx.moveTo(previous.x, previous.y);
        ctx.lineTo(x, y);
        ctx.stroke();
        ctx.restore();
        this.wetInk.set(event.pointerId, { x, y });
    }

    endWetInk(pointerId?: number): void {
        if (pointerId === undefined) {
            this.wetInk.clear();
            this.wetInkStopped.clear();
        } else {
            this.wetInk.delete(pointerId);
            this.wetInkStopped.delete(pointerId);
        }
    }

    setWetInkStyle(style: WetInkStyle | null): void {
        this.wetInkStyle = style;
    }

    // ---- Command replay ----------------------------------------------------------------------------------------

    // The scratch canvas must be the size of whatever is being drawn into. Sizing it from the on-screen canvas
    // would crop every isolated layer when exporting at a different resolution.
    getScratch(width: number, height: number): HTMLCanvasElement {
        for (let i = this.scratchCanvases.length - 1; i >= 0; i--) {
            const candidate = this.scratchCanvases[i];
            if (candidate.width === width && candidate.height === height) {
                this.scratchCanvases.splice(i, 1);
                return candidate;
            }
        }
        const created = document.createElement('canvas');
        created.width = width;
        created.height = height;
        return created;
    }

    releaseScratch(canvas: HTMLCanvasElement): void {
        if (this.scratchCanvases.length < 4) this.scratchCanvases.push(canvas);
    }

    clearScratch(): void {
        this.scratchCanvases.length = 0;
    }

    execute(layer: string, buffer: CommandSource, count: number, strings: string[]): void {
        const rootContext = this.contexts[layer];
        if (!rootContext) return;

        // Images drawn by the document replay are protected from eviction until the next one: dropping an image the
        // current drawing still references would make it silently disappear from the canvas. An export adds to
        // that set rather than replacing it, or exporting a selection with few images would leave the on-screen
        // frame's images unprotected until the next redraw.
        if (layer === 'document') this.imagesUsedThisFrame = new Set();

        const commands = normalizeBuffer(buffer, count);
        if (!commands) return;
        count = Math.min(count, commands.length);

        try {
            this.replay(rootContext, commands, count, strings);
        } catch (error) {
            // An export has a caller waiting for its result, which should hear about the failure.
            if (layer === 'export') throw error;
            // A throw part way through would leave the retained paths after it undefined while .NET believes they
            // exist, so the next frames would draw nothing for them. Starting both sides over repairs that.
            console.error('[Bit.Bink] drawing failed', error);
            this.reportResourcesLost();
        }
    }

    reportResourcesLost(): void {
        // .NET starts its cache over, numbering slots from zero again, so what this side holds goes with it: a slot
        // .NET never defines again would otherwise keep its path or string until the surface is disposed.
        this.paths.clear();
        this.retainedStrings.clear();
        if (!this.disposed && this.callback && this.callback.resourcesLost) this.callback.resourcesLost();
    }

    replay(rootContext: CanvasRenderingContext2D, commands: ArrayLike<number>, count: number, strings: string[]): void {
        let ctx: CanvasRenderingContext2D = rootContext;
        const groups: IsolatedGroup[] = [];
        let i = 0;
        let dash: number[] | null = null;
        // Where path verbs go: the context's current path, or a retained Path2D being defined.
        let target: CanvasPath = ctx;
        let defining: Path2D | null = null;
        let definingSlot = 0;
        // What the next fill or stroke paints: null for the context's current path, otherwise a retained path.
        let current: Path2D | null = null;
        let missing = false;
        let lost = false;
        const paths = this.paths;
        const retained = this.retainedStrings;
        const text = (index: number): string | undefined => index >= 0 ? strings[index] : retained.get(-index - 1);

        while (i < count) {
            const op = commands[i++];
            switch (op) {
                case OP.BEGIN_PATH:
                    ctx.beginPath();
                    current = null;
                    missing = false;
                    break;
                case OP.MOVE_TO:
                    target.moveTo(commands[i++], commands[i++]);
                    break;
                case OP.LINE_TO:
                    target.lineTo(commands[i++], commands[i++]);
                    break;
                case OP.QUAD_TO:
                    target.quadraticCurveTo(commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.CUBIC_TO:
                    target.bezierCurveTo(commands[i++], commands[i++], commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.CLOSE_PATH:
                    target.closePath();
                    break;
                case OP.DEFINE_PATH:
                    definingSlot = commands[i++];
                    defining = new Path2D();
                    target = defining;
                    break;
                case OP.END_PATH:
                    if (defining) paths.set(definingSlot, defining);
                    defining = null;
                    target = ctx;
                    break;
                case OP.USE_PATH: {
                    const path = paths.get(commands[i++]);
                    current = path || null;
                    missing = !path;
                    if (missing) lost = true;
                    break;
                }
                case OP.FREE_PATHS: {
                    const n = commands[i++];
                    for (let k = 0; k < n; k++) paths.delete(commands[i++]);
                    break;
                }
                case OP.RETAIN_STRING: {
                    const slot = commands[i++];
                    retained.set(slot, strings[commands[i++]]);
                    break;
                }
                case OP.FREE_STRINGS: {
                    const n = commands[i++];
                    for (let k = 0; k < n; k++) retained.delete(commands[i++]);
                    break;
                }
                case OP.FILL: {
                    const rgb = commands[i++];
                    const a = commands[i++];
                    const alpha = commands[i++];
                    const blend = commands[i++];
                    const evenOdd = commands[i++];
                    if (missing) break;
                    ctx.globalAlpha = alpha;
                    ctx.globalCompositeOperation = BLEND_NAMES[blend] || 'source-over';
                    ctx.fillStyle = cssColor(rgb, a);
                    if (current) ctx.fill(current, evenOdd ? 'evenodd' : 'nonzero');
                    else ctx.fill(evenOdd ? 'evenodd' : 'nonzero');
                    ctx.globalAlpha = 1;
                    ctx.globalCompositeOperation = 'source-over';
                    break;
                }
                case OP.STROKE: {
                    const rgb = commands[i++];
                    const a = commands[i++];
                    const width = commands[i++];
                    const alpha = commands[i++];
                    const blend = commands[i++];
                    const cap = commands[i++];
                    const join = commands[i++];
                    if (missing) { dash = null; break; }
                    ctx.globalAlpha = alpha;
                    ctx.globalCompositeOperation = BLEND_NAMES[blend] || 'source-over';
                    ctx.strokeStyle = cssColor(rgb, a);
                    ctx.lineWidth = width;
                    ctx.lineCap = CAP_NAMES[cap] || 'round';
                    ctx.lineJoin = JOIN_NAMES[join] || 'round';
                    // The canvas default is 10. SVG export and the built-in rasterizer bevel beyond 4, and centerline
                    // bounds are padded for 4, so a sharper miter would draw past the element's bounds on screen only.
                    ctx.miterLimit = 4;
                    if (dash) { ctx.setLineDash(dash); dash = null; } else { ctx.setLineDash([]); }
                    if (current) ctx.stroke(current);
                    else ctx.stroke();
                    ctx.globalAlpha = 1;
                    ctx.globalCompositeOperation = 'source-over';
                    ctx.setLineDash([]);
                    break;
                }
                case OP.SET_DASH: {
                    const length = commands[i++];
                    dash = [];
                    for (let d = 0; d < length; d++) dash.push(commands[i++]);
                    break;
                }
                case OP.SAVE:
                    ctx.save();
                    break;
                case OP.RESTORE:
                    ctx.restore();
                    break;
                case OP.TRANSFORM:
                    ctx.transform(commands[i++], commands[i++], commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.SET_TRANSFORM:
                    ctx.setTransform(commands[i++], commands[i++], commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.CLIP_RECT: {
                    const x = commands[i++];
                    const y = commands[i++];
                    const w = commands[i++];
                    const h = commands[i++];
                    ctx.beginPath();
                    ctx.rect(x, y, w, h);
                    ctx.clip();
                    current = null;
                    missing = false;
                    break;
                }
                case OP.CLEAR: {
                    const rgb = commands[i++];
                    const a = commands[i++];
                    const transform = ctx.getTransform();
                    ctx.setTransform(1, 0, 0, 1, 0, 0);
                    if (a === 0) {
                        ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
                    } else {
                        ctx.globalCompositeOperation = 'copy';
                        ctx.fillStyle = cssColor(rgb, a);
                        ctx.fillRect(0, 0, ctx.canvas.width, ctx.canvas.height);
                        ctx.globalCompositeOperation = 'source-over';
                    }
                    ctx.setTransform(transform);
                    break;
                }
                case OP.CLEAR_RECT: {
                    const x = commands[i++];
                    const y = commands[i++];
                    const w = commands[i++];
                    const h = commands[i++];
                    ctx.clearRect(x, y, w, h);
                    break;
                }
                case OP.DRAW_IMAGE: {
                    const index = commands[i++];
                    const dx = commands[i++];
                    const dy = commands[i++];
                    const dw = commands[i++];
                    const dh = commands[i++];
                    const hasCrop = commands[i++];
                    const sx = commands[i++];
                    const sy = commands[i++];
                    const sw = commands[i++];
                    const sh = commands[i++];
                    const alpha = commands[i++];
                    const uri = text(index);
                    if (uri === undefined) { lost = true; break; }
                    const image = this.images.get(uri);
                    if (image) {
                        // Touching the entry moves it to the back of the Map's insertion order, which is the LRU position.
                        this.images.delete(uri);
                        this.images.set(uri, image);
                        this.imagesUsedThisFrame.add(uri);
                    }
                    else this.requestImage(uri);
                    if (image && image.complete && image.naturalWidth > 0) {
                        ctx.globalAlpha = alpha;
                        if (hasCrop) ctx.drawImage(image, sx, sy, sw, sh, dx, dy, dw, dh);
                        else ctx.drawImage(image, dx, dy, dw, dh);
                        ctx.globalAlpha = 1;
                    }
                    break;
                }
                case OP.DRAW_TEXT: {
                    const textIndex = commands[i++];
                    const fontIndex = commands[i++];
                    const x = commands[i++];
                    const y = commands[i++];
                    const size = commands[i++];
                    const lineHeight = commands[i++];
                    const rgb = commands[i++];
                    const a = commands[i++];
                    const alpha = commands[i++];
                    const align = commands[i++];
                    const content = text(textIndex);
                    const font = text(fontIndex);
                    if (content === undefined || font === undefined) { lost = true; break; }
                    ctx.globalAlpha = alpha;
                    ctx.fillStyle = cssColor(rgb, a);
                    ctx.font = font;
                    ctx.textAlign = ALIGN_NAMES[align] || 'left';
                    ctx.textBaseline = 'alphabetic';
                    const lines = content.split('\n');
                    for (let l = 0; l < lines.length; l++) {
                        // Text pasted from Windows ends its lines with \r\n. The \r would be drawn as a space and
                        // shift right- and centre-aligned lines, where SVG export trims it.
                        const line = lines[l];
                        ctx.fillText(line.endsWith('\r') ? line.slice(0, -1) : line, x, y + size * 0.82 + l * lineHeight);
                    }
                    ctx.globalAlpha = 1;
                    break;
                }
                case OP.PUSH_GROUP: {
                    const opacity = commands[i++];
                    const blend = commands[i++];
                    // An isolated group needs its own bitmap: canvas composite operations always apply to the whole
                    // surface, so an eraser inside a group would otherwise reach content drawn before it.
                    const scratch = this.getScratch(ctx.canvas.width, ctx.canvas.height);
                    const scratchCtx = scratch.getContext('2d') as CanvasRenderingContext2D;
                    // A pooled context can carry a clip region or an unbalanced save from an earlier group.
                    if (typeof scratchCtx.reset === 'function') {
                        scratchCtx.reset();
                    } else {
                        scratchCtx.setTransform(1, 0, 0, 1, 0, 0);
                        scratchCtx.clearRect(0, 0, scratch.width, scratch.height);
                    }
                    scratchCtx.setTransform(ctx.getTransform());
                    groups.push({ parent: ctx, canvas: scratch, opacity, blend });
                    ctx = scratchCtx;
                    if (!defining) target = ctx;
                    break;
                }
                case OP.POP_GROUP: {
                    const group = groups.pop();
                    if (!group) break;
                    const parent = group.parent;
                    ctx = parent;
                    if (!defining) target = ctx;
                    parent.save();
                    parent.setTransform(1, 0, 0, 1, 0, 0);
                    parent.globalAlpha = group.opacity;
                    parent.globalCompositeOperation = BLEND_NAMES[group.blend] || 'source-over';
                    parent.drawImage(group.canvas, 0, 0);
                    parent.restore();
                    this.releaseScratch(group.canvas);
                    break;
                }
                default:
                    // An unknown opcode means the buffer is out of sync; stopping is safer than misinterpreting it,
                    // and whatever it would have defined has to be defined again.
                    lost = true;
                    i = count;
                    break;
            }
        }

        while (groups.length > 0) {
            const group = groups.pop() as IsolatedGroup;
            this.releaseScratch(group.canvas);
        }
        if (lost) this.reportResourcesLost();
    }

    async loadImage(uri: string): Promise<BinkImageMetrics> {
        if (this.images.has(uri)) {
            const existing = this.images.get(uri) as HTMLImageElement;
            if (existing.complete && existing.naturalWidth > 0) return { width: existing.naturalWidth, height: existing.naturalHeight };
        }
        const image = new Image();
        image.decoding = 'async';
        if (!uri.startsWith('data:') && !uri.startsWith('blob:')) image.crossOrigin = 'anonymous';
        const loaded = new Promise<void>((resolve, reject) => {
            image.onload = () => resolve();
            image.onerror = () => reject(new Error('The image could not be loaded.'));
        });
        image.src = uri;
        await loaded;
        if (this.disposed) return { width: image.naturalWidth, height: image.naturalHeight };
        this.images.delete(uri);
        this.images.set(uri, image);
        this.evictImages();
        return { width: image.naturalWidth, height: image.naturalHeight };
    }

    // An image the document draws but this surface has not decoded: one added with a known size, or arriving with a
    // loaded document, a collaborator's edit or an undo, or one evicted from the cache. It is fetched once, and the
    // canvas is redrawn when it arrives. A failed URI is not retried on every frame.
    requestImage(uri: string): void {
        if (this.disposed || !uri || this.pendingImages.has(uri) || this.failedImages.has(uri)) return;
        this.pendingImages.add(uri);
        this.loadImage(uri).then(
            () => {
                this.pendingImages.delete(uri);
                this.requestRedraw();
            },
            () => {
                this.pendingImages.delete(uri);
                if (this.failedImages.size >= MAX_CACHED_IMAGES) this.failedImages.clear();
                this.failedImages.add(uri);
            });
    }

    // Only .NET can re-record the document, so it is asked to; coalesced to one request per frame, so a document
    // with many images is not redrawn once per image.
    requestRedraw(): void {
        if (this.disposed || this.redrawRequested) return;
        this.redrawRequested = true;
        this.redrawHandle = requestAnimationFrame(() => {
            this.redrawRequested = false;
            this.redrawHandle = 0;
            if (!this.disposed && this.callback && this.callback.redraw) this.callback.redraw();
        });
    }

    // Drops the least recently used decoded bitmaps. Anything the last frame drew is kept whatever the cache size
    // says: a document with more images than the limit would otherwise lose the ones drawn first on every redraw.
    evictImages(): void {
        if (this.images.size <= MAX_CACHED_IMAGES) return;
        for (const uri of [...this.images.keys()]) {
            if (this.images.size <= MAX_CACHED_IMAGES) break;
            if (this.imagesUsedThisFrame.has(uri)) continue;
            this.images.delete(uri);
        }
    }

    measureText(text: string, font: string): BinkTextMetrics {
        const ctx = this.contexts.document || this.contexts.overlay;
        if (!ctx) return { width: 0, lines: 1 };
        ctx.save();
        ctx.font = font;
        let width = 0;
        let lines = 0;
        for (const line of text.split('\n')) {
            width = Math.max(width, ctx.measureText(line).width);
            lines++;
        }
        ctx.restore();
        return { width, lines };
    }

    dispose(): void {
        if (this.disposed) return;
        this.disposed = true;
        const target = this.hitTarget;
        target.removeEventListener('pointerdown', this.onPointerDown);
        target.removeEventListener('pointermove', this.onPointerMove);
        target.removeEventListener('pointerup', this.onPointerUp);
        target.removeEventListener('pointercancel', this.onPointerCancel);
        target.removeEventListener('pointerleave', this.onPointerCancel);
        target.removeEventListener('lostpointercapture', this.onLostPointerCapture);
        target.removeEventListener('contextmenu', this.onContextMenu);
        target.removeEventListener('wheel', this.onWheel);
        this.root.removeEventListener('keydown', this.onKeyDown);
        this.root.removeEventListener('focusout', this.onFocusOut);
        this.root.removeEventListener('paste', this.onPaste);
        window.removeEventListener('focus', this.onClipboardElsewhere);
        document.removeEventListener('copy', this.onClipboardElsewhere);
        document.removeEventListener('cut', this.onClipboardElsewhere);
        this.root.removeEventListener('dragover', this.onDragOver);
        this.root.removeEventListener('drop', this.onDrop);
        this.settleClipboard(null);
        for (const canvas of [this.documentCanvas, this.overlayCanvas]) canvas?.removeEventListener('contextrestored', this.onContextRestored);
        if (this.frameHandle) cancelAnimationFrame(this.frameHandle);
        this.frameHandle = 0;
        if (this.redrawHandle) cancelAnimationFrame(this.redrawHandle);
        this.redrawHandle = 0;
        this.pendingImages.clear();
        this.failedImages.clear();
        this.pendingBlur = null;
        this.resizeObserver?.disconnect();
        this.stopWatchingPixelRatio();
        this.images.clear();
        this.imagesUsedThisFrame.clear();
        this.scratchCanvases.length = 0;
        this.paths.clear();
        this.retainedStrings.clear();
        this.callback = null;
    }
}

// ---- Module API ------------------------------------------------------------------------------------------

export function attach(id: string, root: HTMLElement, options?: BinkSurfaceOptions | null): BinkAttachResult {
    detach(id);
    reapDetachedSurfaces();
    let surface: Surface | undefined;
    try {
        surface = new Surface(id, root, options);
    } catch (error) {
        // The constructor registers listeners and observers before it can fail. Without this they would outlive a
        // surface that never made it into the map and could therefore never be detached.
        try { surface?.dispose(); } catch { /* nothing more to do */ }
        throw error;
    }
    surfaces.set(id, surface);
    const rect = root.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        devicePixelRatio: window.devicePixelRatio || 1,
        maxTouchPoints: navigator.maxTouchPoints || 0,
        coalescedEvents: typeof PointerEvent !== 'undefined' && 'getCoalescedEvents' in PointerEvent.prototype,
        predictedEvents: typeof PointerEvent !== 'undefined' && 'getPredictedEvents' in PointerEvent.prototype,
        pointerEvents: typeof PointerEvent !== 'undefined',
        tilt: typeof PointerEvent !== 'undefined' && 'tiltX' in PointerEvent.prototype,
        twist: typeof PointerEvent !== 'undefined' && 'twist' in PointerEvent.prototype,
        offscreenCanvas: typeof OffscreenCanvas !== 'undefined',
    };
}

export function detach(id: string): void {
    const surface = surfaces.get(id);
    if (surface) {
        surface.dispose();
        surfaces.delete(id);
    }
}

export function setCallbacks(id: string, callbacks: BinkSurfaceCallbacks | null): void {
    const surface = surfaces.get(id);
    if (surface) surface.callback = callbacks;
}

// Wires the surface to a .NET object reference. Sample batches are sent as a plain array once per animation
// frame, never per pointer event.
export function setDotNetCallbacks(id: string, reference: DotNetObjectReference): void {
    const surface = surfaces.get(id);
    if (!surface) return;
    // A rejected interop promise would otherwise vanish silently and the canvas would just stop responding.
    const report = (what: string) => (error: unknown) => console.error(`[Bit.Bink] ${what} failed`, error);
    // A Blazor Server circuit refuses any single message from the browser above 32 KB by default, and closes the
    // connection when it gets one. Everything that can grow is therefore bounded or streamed.
    const dotNet = (globalThis as { DotNet?: DotNetGlobal }).DotNet;
    surface.callback = {
        samples: (buffer: Float64Array, count: number) => {
            // A sample costs about 95 bytes as JSON. A frame normally holds a handful, but a long main-thread stall
            // with a fast pen or a 1000 Hz mouse queues hundreds, so the batch goes in chunks, which arrive in order.
            for (let start = 0; start < count; start += SAMPLES_PER_MESSAGE) {
                const n = Math.min(SAMPLES_PER_MESSAGE, count - start);
                const view = buffer.subarray(start * SAMPLE_SIZE, (start + n) * SAMPLE_SIZE);
                reference.invokeMethodAsync('OnPointerSamples', Array.from(view), n).catch(report('pointer input'));
            }
        },
        resized: (width: number, height: number, dpr: number) => reference.invokeMethodAsync('OnResized', width, height, dpr).catch(report('resize')),
        key: (key: string, ctrl: boolean, shift: boolean, alt: boolean) => reference.invokeMethodAsync('OnKeyDown', key, ctrl, shift, alt).catch(report('keyboard input')),
        textKey: (key: string, value: string | null | undefined) => {
            // The editor holds up to 100,000 characters; a long text travels as a stream, as a long paste does.
            const text = value ?? '';
            if (text.length > 8192 && dotNet) {
                const stream = dotNet.createJSStreamReference(new Blob([text], { type: 'text/plain' }));
                return reference.invokeMethodAsync('OnTextEditorKeyStream', key, stream).catch(report('text editing'));
            }
            return reference.invokeMethodAsync('OnTextEditorKey', key, text).catch(report('text editing'));
        },
        redraw: () => reference.invokeMethodAsync('OnRedrawRequested').catch(report('redraw')),
        resourcesLost: () => reference.invokeMethodAsync('OnResourcesLost').catch(report('redraw')),
        wheel: (panX: number, panY: number, zoom: number, x: number, y: number) => reference.invokeMethodAsync('OnWheel', panX, panY, zoom, x, y).catch(report('wheel input')),
        paste: (kind: PasteKind, text: string | null, data: Blob | null) => {
            // Anything sizeable travels as a stream: an image, or a long payload copied from a large selection.
            if (text && text.length > 8192) data = new Blob([text], { type: 'text/plain' });
            if (data && !dotNet) return;
            const stream = data && dotNet ? dotNet.createJSStreamReference(data) : null;
            reference.invokeMethodAsync('OnPaste', kind, stream ? null : text, stream, data ? data.type : null).catch(report('paste'));
        },
        drop: (kind: DropKind, data: Blob, x: number, y: number) => {
            // A file of any size travels as a stream, never inside a message.
            if (!dotNet) return;
            reference.invokeMethodAsync('OnDrop', kind, dotNet.createJSStreamReference(data), data.type, x, y).catch(report('drop'));
        },
    };
}

// Options that follow component parameters after attach. Without this, turning keyboard shortcuts off at runtime
// would leave the browser layer preventing the default action of the keys it used to own.
export function setOptions(id: string, options: BinkSurfaceOptions | null | undefined): void {
    const surface = surfaces.get(id);
    if (surface && options) surface.options = { ...surface.options, ...options };
}

export function setSize(id: string, cssWidth: number, cssHeight: number, dpr: number): void {
    surfaces.get(id)?.setSize(cssWidth, cssHeight, dpr);
}

export function execute(id: string, layer: string, commands: CommandSource, count: number, strings?: string[] | null): void {
    surfaces.get(id)?.execute(layer, commands, count, strings || []);
}

export function setWetInkStyle(id: string, color: string | null | undefined, width: number, opacity: number): void {
    surfaces.get(id)?.setWetInkStyle(color ? { color, width, opacity } : null);
}

export function setSelectionActive(id: string, active: boolean): void {
    surfaces.get(id)?.setSelectionActive(active);
}

// The serialized selection, and the commands that draw its picture at the given size, rendered here so the picture can
// go on the clipboard beside it.
export function provideClipboard(id: string, text: string | null | undefined, imageWidth?: number | null, imageHeight?: number | null,
    commands?: CommandSource | null, count?: number | null, strings?: string[] | null): void {
    const surface = surfaces.get(id);
    if (!surface) return;
    const image = text && commands && count && imageWidth && imageHeight
        ? renderBlob(surface, imageWidth, imageHeight, commands, count, strings, 'image/png', null).catch(() => null)
        : null;
    surface.provideClipboard(text || null, image);
}

// Reads the system clipboard for a paste the application started (a toolbar button rather than Ctrl+V). The browser
// may ask the user for permission; anything but text, or a refusal, reads as null and .NET falls back to its own copy.
// Returned as a stream because a Blazor Server circuit refuses a large payload inside a single message.
export async function readClipboard(): Promise<unknown> {
    try {
        const clipboard = typeof navigator !== 'undefined' ? navigator.clipboard : undefined;
        if (!clipboard || typeof clipboard.readText !== 'function') return null;
        const text = await clipboard.readText();
        if (!text) return null;
        // Returned as a Blob: Blazor turns it into the IJSStreamReference .NET asked for, and refuses a value that is
        // already a stream reference, which left every clipboard read failing and pasting only the canvas's own copy.
        return new Blob([text], { type: 'text/plain' });
    } catch {
        return null;
    }
}

// The browser's eyedropper (Chromium on desktop): picks the colour of any pixel on screen. It needs the user's
// activation, which a click that ends up here a moment later still carries. Null when there is no eyedropper or the
// user pressed Escape.
export async function pickColor(): Promise<string | null> {
    const EyeDropperType = (globalThis as { EyeDropper?: new () => { open(): Promise<{ sRGBHex: string }> } }).EyeDropper;
    if (!EyeDropperType) return null;
    try {
        const result = await new EyeDropperType().open();
        return result && typeof result.sRGBHex === 'string' ? result.sRGBHex : null;
    } catch {
        return null;
    }
}

export function supportsEyeDropper(): boolean {
    return typeof (globalThis as { EyeDropper?: unknown }).EyeDropper === 'function';
}

// ---- Document store (IndexedDB) -------------------------------------------------------------------------------------

// Saved drawings for BinkBrowserDocumentStore: one object store keyed by the application's key. IndexedDB rather
// than localStorage, which holds a few megabytes of strings at most and blocks the page while it writes.
const STORE_DATABASE = 'bit-bink';
const STORE_OBJECTS = 'documents';
let storeDatabase: Promise<IDBDatabase> | null = null;

function openStore(): Promise<IDBDatabase> {
    if (storeDatabase) return storeDatabase;
    const opening = new Promise<IDBDatabase>((resolve, reject) => {
        if (typeof indexedDB === 'undefined') {
            reject(new Error('IndexedDB is not available.'));
            return;
        }
        const request = indexedDB.open(STORE_DATABASE, 1);
        request.onupgradeneeded = () => {
            if (!request.result.objectStoreNames.contains(STORE_OBJECTS)) request.result.createObjectStore(STORE_OBJECTS);
        };
        request.onsuccess = () => {
            const database = request.result;
            // Another tab upgrading the database closes this connection; the next call opens a new one.
            database.onversionchange = () => {
                database.close();
                storeDatabase = null;
            };
            // The browser closes a connection itself when the user clears the site's data or the disk fails. Kept,
            // it would fail every later save until a reload.
            database.onclose = () => {
                storeDatabase = null;
            };
            resolve(database);
        };
        request.onerror = () => reject(request.error ?? new Error('The drawing store could not be opened.'));
        request.onblocked = () => reject(new Error('The drawing store is blocked by another tab.'));
    });
    // A failed open is not cached, so a later call can succeed once whatever blocked it is gone.
    storeDatabase = opening.catch((error) => {
        storeDatabase = null;
        throw error;
    });
    return storeDatabase;
}

function storeRequest<T>(mode: IDBTransactionMode, run: (objects: IDBObjectStore) => IDBRequest<T>): Promise<T> {
    return openStore().then((database) => new Promise<T>((resolve, reject) => {
        const transaction = database.transaction(STORE_OBJECTS, mode);
        const request = run(transaction.objectStore(STORE_OBJECTS));
        // Resolved when the transaction commits, not when the request succeeds: only then is a write durable.
        transaction.oncomplete = () => resolve(request.result);
        transaction.onerror = () => reject(transaction.error ?? request.error ?? new Error('The drawing store failed.'));
        transaction.onabort = () => reject(transaction.error ?? new Error('The drawing store write was aborted.'));
    }));
}

export function storeSave(key: string, data: Uint8Array): Promise<void> {
    // Kept as the bytes themselves, not a Blob: WebKit cannot store a Blob in IndexedDB outside a persistent session,
    // which is every Safari private window, and failed each save with "Error preparing Blob/File data".
    const record = { data, saved: Date.now() };
    return storeRequest('readwrite', (objects) => objects.put(record, key)).then(() => undefined);
}

// Returned as a Blob, which .NET reads as a stream, led by one byte that says whether anything was stored: 1 and the
// document, or 0 alone. Blazor can make a stream of neither null nor an empty Blob. A record saved by an earlier
// version holds a Blob rather than bytes, and is read the same way.
export function storeLoad(key: string): Promise<Blob> {
    return storeRequest<{ data?: unknown } | undefined>('readonly', (objects) => objects.get(key))
        .then((record) => {
            const data = record?.data;
            const stored = data instanceof Blob || data instanceof ArrayBuffer || ArrayBuffer.isView(data);
            return stored ? new Blob([new Uint8Array([1]), data as BlobPart]) : new Blob([new Uint8Array([0])]);
        });
}

export function storeDelete(key: string): Promise<boolean> {
    // IndexedDB does not say whether delete removed anything, so the key is counted first, in the same transaction.
    return openStore().then((database) => new Promise<boolean>((resolve, reject) => {
        const transaction = database.transaction(STORE_OBJECTS, 'readwrite');
        const objects = transaction.objectStore(STORE_OBJECTS);
        let existed = false;
        const count = objects.count(key);
        count.onsuccess = () => {
            existed = count.result > 0;
            if (existed) objects.delete(key);
        };
        transaction.oncomplete = () => resolve(existed);
        transaction.onerror = () => reject(transaction.error ?? new Error('The drawing store failed.'));
        transaction.onabort = () => reject(transaction.error ?? new Error('The drawing store delete was aborted.'));
    }));
}

export function setCursor(id: string, cursor: string): void {
    const surface = surfaces.get(id);
    if (surface) surface.hitTarget.style.cursor = cursor;
}

export function loadImage(id: string, uri: string): Promise<BinkImageMetrics> {
    const surface = surfaces.get(id);
    return surface ? surface.loadImage(uri) : Promise.reject(new Error('Unknown surface.'));
}

export function measureText(id: string, text: string, font: string): BinkTextMetrics {
    return surfaces.get(id)?.measureText(text, font) ?? { width: 0, lines: 1 };
}

// A Blazor Server circuit that drops without running detach leaves its surface behind, holding two canvases and
// an image cache. Reconnecting attaches a new one, so stale entries are swept whenever a surface is created.
function reapDetachedSurfaces(): void {
    for (const [key, surface] of surfaces) {
        if (surface.root && surface.root.isConnected) continue;
        surfaces.delete(key);
        try { surface.dispose(); } catch { /* the page is going away anyway */ }
    }
}

export function getSize(id: string): BinkSizeResult | null {
    const surface = surfaces.get(id);
    if (!surface) return null;
    const rect = surface.root.getBoundingClientRect();
    return { width: rect.width, height: rect.height, devicePixelRatio: window.devicePixelRatio || 1 };
}

// Renders a command buffer into a detached canvas and returns the encoded image, which .NET reads as a stream. Used
// by image export so the output uses the browser's real fonts and image decoders. A stream, not a byte array: on
// Blazor Server a returned byte array is one message to the server, and a circuit refuses any above 32 KB by default,
// which almost every exported image is. Blazor wraps a returned Blob in the stream reference itself when .NET asks for
// an IJSStreamReference, and refuses one that is already wrapped.
export async function renderToImage(id: string, pixelWidth: number, pixelHeight: number, commands: CommandSource, count: number,
    strings: string[] | null | undefined, mimeType: string | null | undefined, quality: number | null | undefined): Promise<Blob> {
    const surface = surfaces.get(id);
    if (!surface) throw new Error('Unknown surface.');
    return renderBlob(surface, pixelWidth, pixelHeight, commands, count, strings, mimeType, quality);
}

// Replays a command buffer into a detached canvas and encodes it: an export, or the picture a copy puts on the clipboard.
async function renderBlob(surface: Surface, pixelWidth: number, pixelHeight: number, commands: CommandSource, count: number,
    strings: string[] | null | undefined, mimeType: string | null | undefined, quality: number | null | undefined): Promise<Blob> {
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, pixelWidth);
    canvas.height = Math.max(1, pixelHeight);
    const ctx = canvas.getContext('2d');

    const previous = surface.contexts.export;
    surface.contexts.export = ctx;
    try {
        surface.execute('export', commands, count, strings || []);
    } finally {
        surface.contexts.export = previous;
    }

    // Isolated groups during export allocate scratch canvases the size of the export, not of the screen. Keeping
    // them pooled would retain several very large canvases that on-screen rendering can never reuse.
    surface.clearScratch();

    // A null quality (the C# default) is passed through as-is: toBlob treats any non-number as "use the default".
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, mimeType || 'image/png', quality as number | undefined));
    if (!blob) throw new Error('The canvas could not be encoded.');
    return blob;
}

export function saveFile(fileName: string, bytes: Uint8Array<ArrayBuffer>, mimeType?: string | null): void {
    const blob = new Blob([bytes], { type: mimeType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

export function readFile(input: HTMLInputElement | null | undefined): Promise<BinkFileResult | null> {
    const file = input?.files?.[0];
    if (!file) return Promise.resolve(null);
    return file.arrayBuffer().then((buffer) => ({
        name: file.name,
        type: file.type,
        bytes: new Uint8Array(buffer),
    }));
}
