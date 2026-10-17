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
    lowLatency?: boolean;
    rejectTouchWhilePen?: boolean;
    suppressContextMenu?: boolean;
    trackHover?: boolean;
    preventDefaultOnDown?: boolean;
    wetInk?: boolean;
    handleKeyboard?: boolean;
}

// What the surface reports back. Either wired to a DotNetObjectReference by setDotNetCallbacks or supplied
// directly through setCallbacks.
interface BinkSurfaceCallbacks {
    samples(buffer: Float64Array, count: number): unknown;
    resized(width: number, height: number, dpr: number): unknown;
    key(key: string, ctrl: boolean, shift: boolean, alt: boolean): unknown;
    textKey(key: string, value: string | null | undefined): unknown;
    redraw?(): unknown;
}

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
    lowLatency: boolean;
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
};

const BLEND_NAMES: GlobalCompositeOperation[] = ['source-over', 'multiply', 'screen', 'destination-out'];
const CAP_NAMES: CanvasLineCap[] = ['butt', 'round', 'square'];
const JOIN_NAMES: CanvasLineJoin[] = ['miter', 'round', 'bevel'];
const ALIGN_NAMES: CanvasTextAlign[] = ['left', 'center', 'right'];

// The zero-copy WebAssembly path hands over a MemoryView, which exposes slice/set but not element access.
// One slice is a memcpy of the frame's commands, still far cheaper than serializing them.
function normalizeBuffer(buffer: CommandSource, count: number): ArrayLike<number> | null {
    if (!buffer) return null;
    if (Array.isArray(buffer) || ArrayBuffer.isView(buffer)) return buffer as ArrayLike<number>;
    if (typeof (buffer as MemoryViewLike).slice === 'function') return (buffer as MemoryViewLike).slice(0, count);
    return null;
}

function cssColor(packed: number): string {
    // Packed as 0xAARRGGBB.
    const a = (packed >>> 24) & 0xff;
    const r = (packed >>> 16) & 0xff;
    const g = (packed >>> 8) & 0xff;
    const b = packed & 0xff;
    return a === 255 ? `rgb(${r},${g},${b})` : `rgba(${r},${g},${b},${(a / 255).toFixed(4)})`;
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
    contexts: SurfaceContexts;
    resizeObserver: ResizeObserver;
    // Assigned after construction (or lazily) rather than in the constructor.
    callback?: BinkSurfaceCallbacks | null;
    wetInkStyle?: WetInkStyle | null;
    lowLatency?: boolean;

    onPointerDown: (event: PointerEvent) => void;
    onPointerMove: (event: PointerEvent) => void;
    onPointerUp: (event: PointerEvent) => void;
    onPointerCancel: (event: PointerEvent) => void;
    onContextMenu: (event: MouseEvent) => void;
    onKeyDown: (event: KeyboardEvent) => void;
    onFocusOut: (event: FocusEvent) => void;
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
        root.addEventListener('keydown', this.onKeyDown);
        root.addEventListener('focusout', this.onFocusOut);
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
        const attributes: CanvasRenderingContext2DSettings = { alpha: true, desynchronized: this.options.lowLatency !== false };
        const ctx = canvas.getContext('2d', attributes);
        if (ctx && typeof ctx.getContextAttributes === 'function') {
            const actual = ctx.getContextAttributes();
            this.lowLatency = !!(actual && actual.desynchronized);
        }
        return ctx;
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
                for (const sample of coalesced) this.push(sample, PHASE_MOVE, FLAG_COALESCED);
            } else {
                this.push(event, PHASE_MOVE, 0);
            }
        } else {
            this.push(event, isDown ? PHASE_MOVE : PHASE_HOVER, 0);
        }

        if (isDown) {
            if (this.options.wetInk) this.appendWetInk(event);
            event.preventDefault();
        }
        this.requestFlush();
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
        if (this.disposed || !this.callback || this.options.handleKeyboard === false) return;
        if (event.isComposing) return;

        // While the inline text editor is open it owns the keyboard. Forwarding these to the canvas would mean
        // typing "Delete" in a comment deleted the selection.
        const editor = this.textEditor();
        if (editor && event.target === editor) {
            if (event.key === 'Escape' || (event.key === 'Enter' && !event.shiftKey)) {
                event.preventDefault();
                this.callback.textKey(event.key, editor.value);
            }
            return;
        }

        // ChildContent renders inside the same element, so a caption field or a button in it would otherwise have
        // Ctrl+A, Delete and the arrow keys taken away by the canvas.
        if (event.target !== this.root && this.isInteractiveTarget(event.target as HTMLElement | null)) return;

        if (this.ownsKey(event)) event.preventDefault();
        this.callback.key(event.key, event.ctrlKey || event.metaKey, event.shiftKey, event.altKey);
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
        if ((event.ctrlKey || event.metaKey) && OWNED_SHORTCUTS.has(key.toLowerCase())) return true;
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
        if (pointerId === undefined) this.wetInk.clear();
        else this.wetInk.delete(pointerId);
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

        let ctx: CanvasRenderingContext2D = rootContext;
        const groups: IsolatedGroup[] = [];
        let i = 0;
        let dash: number[] | null = null;

        while (i < count) {
            const op = commands[i++];
            switch (op) {
                case OP.BEGIN_PATH:
                    ctx.beginPath();
                    break;
                case OP.MOVE_TO:
                    ctx.moveTo(commands[i++], commands[i++]);
                    break;
                case OP.LINE_TO:
                    ctx.lineTo(commands[i++], commands[i++]);
                    break;
                case OP.QUAD_TO:
                    ctx.quadraticCurveTo(commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.CUBIC_TO:
                    ctx.bezierCurveTo(commands[i++], commands[i++], commands[i++], commands[i++], commands[i++], commands[i++]);
                    break;
                case OP.CLOSE_PATH:
                    ctx.closePath();
                    break;
                case OP.FILL: {
                    const color = commands[i++];
                    const alpha = commands[i++];
                    const blend = commands[i++];
                    const evenOdd = commands[i++];
                    ctx.globalAlpha = alpha;
                    ctx.globalCompositeOperation = BLEND_NAMES[blend] || 'source-over';
                    ctx.fillStyle = cssColor(color);
                    ctx.fill(evenOdd ? 'evenodd' : 'nonzero');
                    ctx.globalAlpha = 1;
                    ctx.globalCompositeOperation = 'source-over';
                    break;
                }
                case OP.STROKE: {
                    const color = commands[i++];
                    const width = commands[i++];
                    const alpha = commands[i++];
                    const blend = commands[i++];
                    const cap = commands[i++];
                    const join = commands[i++];
                    ctx.globalAlpha = alpha;
                    ctx.globalCompositeOperation = BLEND_NAMES[blend] || 'source-over';
                    ctx.strokeStyle = cssColor(color);
                    ctx.lineWidth = width;
                    ctx.lineCap = CAP_NAMES[cap] || 'round';
                    ctx.lineJoin = JOIN_NAMES[join] || 'round';
                    if (dash) { ctx.setLineDash(dash); dash = null; } else { ctx.setLineDash([]); }
                    ctx.stroke();
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
                    break;
                }
                case OP.CLEAR: {
                    const color = commands[i++];
                    const transform = ctx.getTransform();
                    ctx.setTransform(1, 0, 0, 1, 0, 0);
                    if (color === 0) {
                        ctx.clearRect(0, 0, ctx.canvas.width, ctx.canvas.height);
                    } else {
                        ctx.globalCompositeOperation = 'copy';
                        ctx.fillStyle = cssColor(color);
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
                    const uri = strings[index];
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
                    const color = commands[i++];
                    const alpha = commands[i++];
                    const align = commands[i++];
                    ctx.globalAlpha = alpha;
                    ctx.fillStyle = cssColor(color);
                    ctx.font = strings[fontIndex];
                    ctx.textAlign = ALIGN_NAMES[align] || 'left';
                    ctx.textBaseline = 'alphabetic';
                    const lines = strings[textIndex].split('\n');
                    for (let l = 0; l < lines.length; l++) {
                        ctx.fillText(lines[l], x, y + size * 0.82 + l * lineHeight);
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
                    break;
                }
                case OP.POP_GROUP: {
                    const group = groups.pop();
                    if (!group) break;
                    const parent = group.parent;
                    ctx = parent;
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
                    // An unknown opcode means the buffer is out of sync; stopping is safer than misinterpreting it.
                    return;
            }
        }

        while (groups.length > 0) {
            const group = groups.pop() as IsolatedGroup;
            this.releaseScratch(group.canvas);
        }
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
        this.root.removeEventListener('keydown', this.onKeyDown);
        this.root.removeEventListener('focusout', this.onFocusOut);
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
        lowLatency: !!surface.lowLatency,
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
    surface.callback = {
        samples: (buffer: Float64Array, count: number) => {
            const view = buffer.subarray(0, count * SAMPLE_SIZE);
            reference.invokeMethodAsync('OnPointerSamples', Array.from(view), count).catch(report('pointer input'));
        },
        resized: (width: number, height: number, dpr: number) => reference.invokeMethodAsync('OnResized', width, height, dpr).catch(report('resize')),
        key: (key: string, ctrl: boolean, shift: boolean, alt: boolean) => reference.invokeMethodAsync('OnKeyDown', key, ctrl, shift, alt).catch(report('keyboard input')),
        textKey: (key: string, value: string | null | undefined) => reference.invokeMethodAsync('OnTextEditorKey', key, value ?? '').catch(report('text editing')),
        redraw: () => reference.invokeMethodAsync('OnRedrawRequested').catch(report('redraw')),
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

// Renders a command buffer into a detached canvas and returns encoded image bytes. Used by PNG export so the
// output uses the browser's real fonts and image decoders.
export async function renderToImage(id: string, pixelWidth: number, pixelHeight: number, commands: CommandSource, count: number,
    strings: string[] | null | undefined, mimeType: string | null | undefined, quality: number | null | undefined): Promise<Uint8Array> {
    const surface = surfaces.get(id);
    if (!surface) throw new Error('Unknown surface.');
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
    const buffer = await blob.arrayBuffer();
    return new Uint8Array(buffer);
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
