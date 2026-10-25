// Bit.Bdnd HTML5 backend interop.
// Blazor raises the managed drag events (ondragstart/ondragover/ondrop). This module covers what
// managed handlers cannot do, because they cannot touch the live DataTransfer and (in Blazor
// Server) do not run while the native event is being dispatched:
//   1) configure a drag at dragstart: drag data, effectAllowed, drag image and drag handles,
//   2) track the pointer during a drag (Firefox reports 0,0 in 'drag' events),
//   3) report the end of a drag, even when the source element was removed from the DOM,
//   4) track hover per drop target in a way that survives re-rendered/removed child nodes,
//   5) give drop-effect feedback and read native drop payloads (files/text/url/html),
//   6) read an element's bounding rectangle,
//   7) run keyboard drags: pick a source up with Space/Enter, send the keys that move, drop and cancel
//      it to .NET, keep the focus on it, and tell screen readers what happens through a live region,
//   8) run pointer drags for touch and pen (and, when asked, the mouse), which most touch browsers never
//      turn into native drags: start them from a press, move a preview, hit-test the targets, auto-scroll,
//      and report hover, drop and end to .NET.

/** The subset of DotNetObjectReference this module calls into. */
interface DotNetObject {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface Rect {
    left: number;
    top: number;
    width: number;
    height: number;
}

/** Mirrors BdndDragSourceOptions. */
interface DragSourceOptions {
    id?: string;
    canDrag?: boolean;
    useHandle?: boolean;
    useEmptyPreview?: boolean;
    effectAllowed?: DataTransfer['effectAllowed'];
    /** Whether .NET follows the pointer during a drag (a drag layer); otherwise moves are not sent. */
    trackPointer?: boolean;
    /** Whether Space/Enter on the source (or its handle) picks it up for a keyboard drag. */
    keyboard?: boolean;
    /** The id of the hidden element holding the keyboard instructions, and their text. */
    instructionsId?: string | null;
    instructions?: string | null;
    /** Which pointers drag through pointer events: 'touch' (touch and pen), 'all' or 'none'. */
    pointerDrag?: 'touch' | 'all' | 'none';
    /** How long a touch rests on the source before a pointer drag starts, in milliseconds. */
    touchDelay?: number;
    /** How far a pointer moves before a pointer drag starts, in CSS pixels. */
    dragThreshold?: number;
    /** Whether a pointer drag scrolls the containers it nears the edge of. */
    autoScroll?: boolean;
    /** The axis the copy following the pointer keeps to: 'x', 'y', or none. */
    previewAxis?: string | null;
    /** Data put on a native drag's DataTransfer at dragstart, by type, for destinations outside the page. */
    nativeData?: Record<string, string> | null;
}

/** Mirrors BdndDropTargetOptions. */
interface DropTargetOptions {
    /** The target's handler id, by which keyboard drags name it. */
    id?: string;
    /** Whether the target can accept the in-app item currently being dragged. */
    canDrop?: boolean;
    /** Whether the target has an OnHover, which pointer drags report their moves to. */
    hover?: boolean;
    /** The edges (BdndEdges flags) whose closest one is reported while a drag is over the target. */
    edges?: number;
    /** The DataTransfer types of the native drags the target accepts. */
    nativeTypes?: string[];
    /** The file types (MIME types, MIME groups such as image/*, extensions such as .png) a file drop is limited to. */
    fileTypes?: string[];
    /** Whether a dropped folder is expanded into the files inside it. */
    expandDirectories?: boolean;
    /** The most files a drop hands over; 0 for no limit. */
    maxFiles?: number;
}

interface DragSource {
    element: HTMLElement;
    dotNetRef: DotNetObject;
    options: DragSourceOptions;
    rect: Rect | null;
    /** Where the last press on the source began, for a native drag to tell whether it began in a no-drag region. */
    pressTarget: EventTarget | null;
}

interface DropTarget {
    element: HTMLElement;
    dotNetRef: DotNetObject;
    options: DropTargetOptions;
    /** The edge last reported for the drag over the target, or null. */
    edge: string | null;
    /** The id under which the files of the target's last native drop are kept for reading, or null. */
    lastDropId: string | null;
    /** Counts the target's native drops, so that a folder walk of an earlier one knows it is outdated. */
    dropSerial: number;
    entered: Set<EventTarget>;
    over: boolean;
    reset: () => void;
}

/** The in-app drag in progress, started by one of this document's sources. */
interface Session {
    source: DragSource;
    startEvent: DragEvent;
    x: number;
    y: number;
    sentX: number;
    sentY: number;
    frame: number;
    timer: number;
    onDragOver: (e: DragEvent) => void;
    onDrop: () => void;
    onMouseMove: (e: MouseEvent) => void;
    onDragEvent: () => void;
    /** When the last drag or dragover event of the drag came. */
    lastDragEvent: number;
}

interface DropEffectState {
    accepted: boolean;
    rejected: boolean;
}

/** The keyboard drag in progress: the keys that move, drop and cancel it go to its source's .NET object. */
interface KeyboardSession {
    source: DragSource;
    /** The element that has the focus: the source, its handle, or the element a re-render showed the item in since. */
    activator: HTMLElement;
    /** The element showing the dragged item: the source's, or the one a re-render showed the item in since. */
    element: HTMLElement;
    /**
     * How many candidates came before the item's element when the last move landed (-1 before the first move). A
     * hover that reorders a list moves the element; the next move then goes on from where the element is now.
     */
    position: number;
    /** Ended here: a drop or a cancel was pressed (or the focus left), and the keys are the page's again. */
    ended: boolean;
    /** Ended in .NET: it answered that the drag is over (or never began). */
    dotNetDone: boolean;
    /** When the current key started waiting for the rows of a virtualized list (0: not waiting). */
    pendingSince: number;
    observer: MutationObserver | null;
    onKeyDown: (e: KeyboardEvent) => void;
    onFocusIn: (e: FocusEvent) => void;
    onFocusOut: (e: FocusEvent) => void;
    onPointerDown: () => void;
}

/** A pointer drag: a press on a source, and - once the press qualifies - the drag it starts. */
interface PointerSession {
    source: DragSource;
    /** The element the press began on, which keeps getting a touch's events even once it is out of the document. */
    pressed: Element | null;
    pointerId: number;
    pointerType: string;
    startX: number;
    startY: number;
    x: number;
    y: number;
    /** How long the press must rest before the drag starts, and how far it may move meanwhile (or must, without a delay). */
    delay: number;
    threshold: number;
    /** The pointer was released (or its press cancelled): its click, if any, is past. */
    released: boolean;
    dragging: boolean;
    ended: boolean;
    /** .NET began the drag (true), refused it (false), or has not answered yet (null). */
    begun: boolean | null;
    timer: number;
    frame: number;
    interval: number;
    preview: HTMLElement | null;
    /** The targets under the pointer, innermost first. */
    hovered: DropTarget[];
    /** The OnHover report waiting in the queue for the targets hovered now, if any; it takes the latest position. */
    hoverSlot: { targets: DropTarget[]; x: number; y: number } | null;
    sentX: number;
    sentY: number;
    /** What the press changed on the page, undone when it ends. */
    restore: (() => void)[];
    /** The scroll axes of the elements the drag passed over, and when they were measured. */
    scrollAxes: Map<Element, { axes: { x: boolean; y: boolean } | null; at: number }>;
    onMove: (e: PointerEvent) => void;
    onUp: (e: PointerEvent) => void;
    onCancel: (e: PointerEvent) => void;
    onKeyDown: (e: KeyboardEvent) => void;
    onContextMenu: (e: Event) => void;
    onSelectStart: (e: Event) => void;
    onTouchMove: (e: TouchEvent) => void;
    onTouchEnd: (e: TouchEvent) => void;
    onAnotherPress: (e: PointerEvent) => void;
    onBlur: () => void;
    onVisibility: () => void;
}

/** Where a keyboard move landed, as BdndKeyboardTarget reads it. */
interface KeyboardPlace {
    id: string;
    edge: string | null;
    before: boolean | null;
    rect: Rect | null;
    label: string | null;
    position: number;
    count: number;
    moved: boolean;
    /** The list goes on that way with rows not rendered yet (a virtualized list), which were scrolled into view. */
    pending: boolean;
}

// Marks drags started by a registered source; the handler id follows the prefix.
const SOURCE_TYPE_PREFIX = 'application/x-bit-bdnd-';
const HANDLE_ATTRIBUTE = 'data-bit-bdnd-handle';
// a region of a source no drag starts from (a pointer press there is the region's own)
const NO_DRAG_ATTRIBUTE = 'data-bit-bdnd-no-drag';
// on the copy of the source a pointer drag moves with the pointer
const PREVIEW_ATTRIBUTE = 'data-bit-bdnd-preview';
const EMPTY_IMAGE_SRC =
    'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
// The DataTransfer types a text field can take, and the <input> types that take text.
const TEXT_DRAG_TYPES = ['text/plain', 'text/uri-list', 'text/html'];
const TEXT_INPUT_TYPES = ['text', 'search', 'url', 'tel', 'email', 'password', 'number'];

// Firefox can raise a mousemove right as a drag starts, so mouse movement is only taken as
// "the drag is over" after this delay (the same approach as React DnD's HTML5 backend): when the source was removed,
// or when no button is down any more.
const SOURCE_REMOVED_CHECK_DELAY = 1000;

// How long after a button-less mouse move a native drag must have raised no drag event to count as ended: longer than
// the gap between two dragover events of a drag held still (Firefox raises them every 350 ms or so).
const UNSEEN_END_WAIT = 500;

// How long a keyboard step waits for the rows a virtualized list renders before it goes on as if there were none.
const PENDING_ROWS_LIMIT = 1500;

const claimedEvents = new WeakSet<Event>(); // dragstart events already handled by an inner source
const dropEffectStates = new WeakMap<Event, DropEffectState>(); // dragenter/dragover event -> nested targets' decision
const targets = new Set<DropTarget>();
const targetsById = new Map<string, DropTarget>();
const targetsByElement = new WeakMap<Element, DropTarget>();
let pointerSession: PointerSession | null = null;
let emptyImage: HTMLImageElement | null = null;
let session: Session | null = null;
let cancelRequested = false;
let keyboardSession: KeyboardSession | null = null;
let liveRegion: HTMLElement | null = null;
// keys whose keyup must not reach the page (it would click a button the key picked up or dropped), with the time
// the keydown was taken
const suppressedKeyUps = new Map<string, number>();
const KEYUP_SUPPRESSION_MS = 1000;

window.addEventListener('keyup', (e) => {
    const since = suppressedKeyUps.get(e.key);
    if (since === undefined) return;
    suppressedKeyUps.delete(e.key);
    if (performance.now() - since > KEYUP_SUPPRESSION_MS) return;
    e.preventDefault();
    e.stopPropagation();
}, true);

// Runs after Blazor's document-level listener, i.e. after the managed dragstart handler.
window.addEventListener('dragstart', (e) => {
    // .NET calls cancelDrag() synchronously while dragstart is being dispatched (WebAssembly)
    if (cancelRequested) {
        cancelRequested = false;
        e.preventDefault();
        endSession(false);
        return;
    }
    // other code cancelled the drag: no dragend will follow
    if (e.defaultPrevented && session && session.startEvent === e) endSession(true);
});

// Delays a .NET call's answer until it arrives; a failed call (a disposed component, a dropped circuit) answers
// undefined.
function invokeForResult(dotNetRef: DotNetObject, method: string, ...args: unknown[]): Promise<unknown> {
    try {
        return dotNetRef.invokeMethodAsync(method, ...args).catch(() => undefined);
    } catch {
        return Promise.resolve(undefined);
    }
}

function getEmptyImage(): HTMLImageElement {
    if (!emptyImage) {
        emptyImage = new Image();
        emptyImage.src = EMPTY_IMAGE_SRC;
    }
    return emptyImage;
}

function typesOf(dataTransfer: DataTransfer | null): string[] {
    return dataTransfer && dataTransfer.types ? Array.from(dataTransfer.types) : [];
}

function isInAppDrag(dataTransfer: DataTransfer | null): boolean {
    return !!session || typesOf(dataTransfer).some((t) => t.startsWith(SOURCE_TYPE_PREFIX));
}

// .NET callbacks are fire-and-forget; a disposed component or a dropped circuit must not
// surface as an unhandled promise rejection.
function invoke(dotNetRef: DotNetObject, method: string, ...args: unknown[]): void {
    try {
        dotNetRef.invokeMethodAsync(method, ...args).catch(() => { });
    } catch {
        /* ignore */
    }
}

function toRect(element: Element): Rect {
    const r = element.getBoundingClientRect();
    return { left: r.left, top: r.top, width: r.width, height: r.height };
}

// An element with display:contents (a drop target wrapping a card, say) has no box of its own: what it covers on
// screen is the union of its children's boxes.
function boxRect(element: Element): Rect {
    const r = toRect(element);
    if (r.width > 0 || r.height > 0 || getComputedStyle(element).display !== 'contents') return r;
    let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;
    for (const child of Array.from(element.children)) {
        const c = boxRect(child);
        if (c.width === 0 && c.height === 0) continue;
        left = Math.min(left, c.left);
        top = Math.min(top, c.top);
        right = Math.max(right, c.left + c.width);
        bottom = Math.max(bottom, c.top + c.height);
    }
    return left === Infinity ? r : { left, top, width: right - left, height: bottom - top };
}

const EDGE_TOP = 1;
const EDGE_BOTTOM = 2;
const EDGE_LEFT = 4;
const EDGE_RIGHT = 8;

// The edge of an element's box nearest to a point, among the edges (BdndEdges flags) asked for.
function closestEdge(element: Element, x: number, y: number, edges: number): string | null {
    if (!edges) return null;
    const r = boxRect(element);
    let best: string | null = null;
    let distance = Infinity;
    const consider = (flag: number, name: string, d: number) => {
        if (edges & flag && d < distance) {
            distance = d;
            best = name;
        }
    };
    consider(EDGE_TOP, 'top', Math.abs(y - r.top));
    consider(EDGE_BOTTOM, 'bottom', Math.abs(r.top + r.height - y));
    consider(EDGE_LEFT, 'left', Math.abs(x - r.left));
    consider(EDGE_RIGHT, 'right', Math.abs(r.left + r.width - x));
    return best;
}

// The element of a display:contents one that the browser can scroll into view.
function boxElement(element: Element): Element {
    if (getComputedStyle(element).display !== 'contents') return element;
    for (const child of Array.from(element.children)) {
        const box = boxElement(child);
        if (box.getClientRects().length > 0) return box;
    }
    return element;
}

export function getBoundingClientRect(element: Element | null): Rect | null {
    return element ? toRect(element) : null;
}

// ---- drag sources -------------------------------------------------------------------------

function findHandle(node: Node | null, root: Element): Element | null {
    for (; node && node !== root; node = node.parentNode) {
        if (node.nodeType === Node.ELEMENT_NODE && (node as Element).hasAttribute(HANDLE_ATTRIBUTE)) {
            return node as Element;
        }
    }
    return null;
}

export function registerDragSource(element: HTMLElement | null, dotNetRef: DotNetObject, options: DragSourceOptions | null) {
    if (!element) return null;
    const source: DragSource = { element, dotNetRef, options: {}, rect: null, pressTarget: null };

    const configure = (o: DragSourceOptions | null) => {
        source.options = o || {};
        // decode the transparent image before the first drag needs it
        if (source.options.useEmptyPreview) getEmptyImage();
        // a live region only reads out what changes once it is in the document
        if (source.options.keyboard) {
            ensureLiveRegion();
            if (source.options.instructionsId) ensureInstructions(source.options.instructionsId, source.options.instructions || '');
        }
    };

    const onDragStart = (e: DragEvent) => {
        // dragstart bubbles: the innermost registered source decides, its ancestors stay out
        // of it (the managed handler stops propagation the same way).
        if (claimedEvents.has(e)) return;
        claimedEvents.add(e);

        // a pointer drag runs (a platform's own long-press drag of a touched source), or the press began in a region
        // no drag starts from: no native drag
        if (pointerSession || isNoDragPress(source.pressTarget, element)) {
            e.preventDefault();
            return;
        }

        const o = source.options;
        const dataTransfer = e.dataTransfer;
        if (!dataTransfer || !o.canDrag || !o.id) return;
        // a keyboard drag is still in progress (a pointer press cancels it, but .NET may not have ended it yet)
        if (keyboardSession) cancelKeyboardSession(keyboardSession);
        const handle = o.useHandle ? findHandle(e.target as Node | null, element) : null;
        if (o.useHandle && !handle) return;

        // measured before anything re-renders; read by .NET as the source's initial offset
        const r = toRect(element);
        source.rect = r;
        try {
            // marks the drag as ours (Firefox also requires some data to start a drag)
            dataTransfer.setData(SOURCE_TYPE_PREFIX + o.id, o.id);
            // what a destination outside the page reads: a text field, another application, another window
            if (o.nativeData) {
                for (const type of Object.keys(o.nativeData)) dataTransfer.setData(type, o.nativeData[type]);
            }
            dataTransfer.effectAllowed = o.effectAllowed || 'move';
            if (o.useEmptyPreview) {
                dataTransfer.setDragImage(getEmptyImage(), 0, 0);
            } else if (handle) {
                // the browser would picture just the handle; show the whole source instead
                dataTransfer.setDragImage(element, e.clientX - r.left, e.clientY - r.top);
            }
        } catch {
            /* ignore */
        }
        beginSession(source, e);
    };

    const onDragEnd = () => {
        if (session && session.source === source) endSession(true);
    };

    // Space or Enter on the source (or on its handle) picks it up. keydown bubbles: like dragstart, the innermost
    // registered source the key was pressed on decides.
    const onKeyDown = (e: KeyboardEvent) => {
        if (claimedEvents.has(e) || e.defaultPrevented || (e.key !== ' ' && e.key !== 'Enter')) return;
        const activator = keyboardActivator(source, e.target);
        if (!activator) return;
        claimedEvents.add(e);
        const o = source.options;
        if (!o.keyboard || !o.canDrag || !o.id || e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
        if (session || keyboardSession || pointerSession) return; // a drag is in progress already
        e.preventDefault(); // no scrolling (Space), no form submission or link (Enter)
        suppressKeyUp(e.key);
        startKeyboardSession(source, activator);
    };

    // A press may start a pointer drag: touch and pen (and with pointerDrag 'all' the mouse too). Like dragstart,
    // pointerdown bubbles and the innermost registered source that takes it decides.
    const onPointerDown = (e: PointerEvent) => {
        source.pressTarget = e.target;
        if (e.button === 0 && e.target instanceof Element
            && (pressesAField(e.target, element) || isNoDragPress(e.target, element))) holdNativeDrag(source, e.target);
        if (claimedEvents.has(e)) return;
        const o = source.options;
        const mode = o.pointerDrag || 'touch';
        if (mode === 'none' || (e.pointerType === 'mouse' && mode !== 'all')) return;
        if (!e.isPrimary || e.button !== 0 || !o.canDrag || !o.id) return;
        if (pointerSession || session) return;
        const target = e.target instanceof Element ? e.target : null;
        if (!target) return;
        const handle = o.useHandle ? findHandle(target, element) : null;
        if (o.useHandle && !handle) return;
        if (isNoDragPress(target, element) || pressesAField(target, element)) return;
        claimedEvents.add(e);
        startPointerPress(source, e, !!handle);
    };

    // WebKit decides at touchstart whether the moves of a touch may be cancelled, from the listeners there are at that
    // moment: one has to be on the source before its press, or the touch scrolls the page once the drag starts. It
    // only cancels the moves of this source's own drag, so other touches scroll as usual.
    const onTouchMove = (e: TouchEvent) => {
        const p = pointerSession;
        if (p && p.source === source && p.dragging && e.cancelable) e.preventDefault();
    };

    configure(options);
    // Element-level listeners still fire after the element was removed from the DOM, whereas
    // Blazor's document-level listener never sees events of detached elements.
    element.addEventListener('dragstart', onDragStart);
    element.addEventListener('dragend', onDragEnd);
    element.addEventListener('keydown', onKeyDown);
    element.addEventListener('pointerdown', onPointerDown);
    element.addEventListener('touchmove', onTouchMove, { passive: false });

    return {
        update: configure,
        getDragStartRect: () => source.rect,
        cancelDrag: () => {
            if (!session || session.source !== source) return;
            cancelRequested = true;
            setTimeout(() => (cancelRequested = false), 0);
        },
        keyboardNavigate: (candidates: KeyboardCandidate[] | null, sourceTargets: string[] | null, currentId: string | null,
            currentEdge: string | null, command: string, waiting: boolean) =>
            keyboardNavigate(source, candidates || [], sourceTargets || [], currentId, currentEdge, command, waiting === true),
        adoptFocus: () => adoptFocus(source),
        dispose: () => {
            element.removeEventListener('dragstart', onDragStart);
            element.removeEventListener('dragend', onDragEnd);
            element.removeEventListener('keydown', onKeyDown);
            element.removeEventListener('pointerdown', onPointerDown);
            element.removeEventListener('touchmove', onTouchMove);
            if (session && session.source === source) endSession(false);
            // a pointer drag goes on: it reports to .NET through the source's object, which outlives the registration
        },
    };
}

function beginSession(source: DragSource, startEvent: DragEvent): void {
    if (session) endSession(true); // the previous drag never reported its end

    const s: Session = {
        source,
        startEvent,
        x: 0,
        y: 0,
        sentX: NaN,
        sentY: NaN,
        frame: 0,
        timer: 0,
        lastDragEvent: performance.now(),
        onDragEvent: () => {
            s.lastDragEvent = performance.now();
        },
        onDragOver: (e: DragEvent) => {
            s.lastDragEvent = performance.now();
            s.x = e.clientX;
            s.y = e.clientY;
            // on Blazor Server every move is a message over the circuit: send them only when followed
            if (!s.frame && source.options.trackPointer) {
                s.frame = requestAnimationFrame(() => {
                    s.frame = 0;
                    if (session !== s || (s.x === s.sentX && s.y === s.sentY)) return;
                    s.sentX = s.x;
                    s.sentY = s.y;
                    invoke(source.dotNetRef, 'HandleDragMove', s.x, s.y);
                });
            }
        },
        // Browsers may never raise 'dragend' for a source that was removed from the DOM (e.g. by
        // the re-render a drop causes), so also end the drag after a drop or once the mouse moves.
        onDrop: () => { setTimeout(() => endIfSourceRemoved(s), 0); },
        // No mouse events reach the page while a native drag runs: one the browser raised, with no button down, means
        // the drag may be over with its end unseen (released outside the window, where some engines report no
        // dragend). It is a cancel once no drag event has come for a moment after it - a live drag keeps raising them.
        onMouseMove: (e: MouseEvent) => {
            if (!e.isTrusted || e.buttons !== 0) {
                endIfSourceRemoved(s);
                return;
            }
            const at = performance.now();
            setTimeout(() => {
                if (session === s && s.lastDragEvent < at) endSession(true);
            }, UNSEEN_END_WAIT);
        },
    };

    document.addEventListener('dragover', s.onDragOver, true);
    document.addEventListener('drag', s.onDragEvent, true);
    document.addEventListener('drop', s.onDrop, true);
    s.timer = window.setTimeout(
        () => window.addEventListener('mousemove', s.onMouseMove, true),
        SOURCE_REMOVED_CHECK_DELAY);
    session = s;
}

function endIfSourceRemoved(s: Session): void {
    if (session === s && !s.source.element.isConnected) endSession(true);
}

function endSession(notify: boolean): void {
    const s = session;
    if (!s) return;
    session = null;
    document.removeEventListener('dragover', s.onDragOver, true);
    document.removeEventListener('drag', s.onDragEvent, true);
    document.removeEventListener('drop', s.onDrop, true);
    window.removeEventListener('mousemove', s.onMouseMove, true);
    clearTimeout(s.timer);
    if (s.frame) cancelAnimationFrame(s.frame);
    for (const target of targets) target.reset();
    if (notify) invoke(s.source.dotNetRef, 'HandleDragEnd');
}

// ---- drop targets -------------------------------------------------------------------------

// Native text drags (text, links, HTML) over a text field or editable content are left to the
// browser, so text can still be dropped into inputs placed inside a drop target. A field cannot take
// a file: a drag carrying files stays the target's, or the browser would open the dropped file.
function isNativeTextDragOverEditable(e: DragEvent): boolean {
    if (isInAppDrag(e.dataTransfer)) return false;
    const types = typesOf(e.dataTransfer);
    if (types.includes('Files') || !types.some((t) => TEXT_DRAG_TYPES.includes(t))) return false;
    let node = e.target as Node | null;
    if (node && node.nodeType !== Node.ELEMENT_NODE) node = node.parentElement;
    return !!node && acceptsText(node as HTMLElement);
}

function acceptsText(el: HTMLElement): boolean {
    if (el.isContentEditable) return true;
    if (el instanceof HTMLTextAreaElement) return !el.disabled && !el.readOnly;
    // checkboxes, buttons, file and range inputs take no text
    return el instanceof HTMLInputElement && TEXT_INPUT_TYPES.includes(el.type) && !el.disabled && !el.readOnly;
}

// dragenter/dragover bubble through nested drop targets. The innermost target that can drop
// accepts the event; outer targets must not undo that decision.
function applyDropEffect(e: DragEvent, canDrop: boolean): void {
    const dataTransfer = e.dataTransfer;
    if (!dataTransfer) return;
    let state = dropEffectStates.get(e);
    if (!state) dropEffectStates.set(e, (state = { accepted: false, rejected: false }));
    if (state.accepted) return;
    if (isNativeTextDragOverEditable(e)) {
        state.accepted = true; // the editable element handles it, untouched
        return;
    }
    e.preventDefault();
    if (canDrop) {
        state.accepted = true;
        // an inner target rejected the event first: restore an effect the source allows
        if (state.rejected) dataTransfer.dropEffect = defaultDropEffect(dataTransfer.effectAllowed);
    } else {
        state.rejected = true;
        dataTransfer.dropEffect = 'none'; // no-drop cursor; the drop event will not fire
    }
}

function defaultDropEffect(effectAllowed: DataTransfer['effectAllowed']): DataTransfer['dropEffect'] {
    switch (effectAllowed) {
        case 'move':
        case 'linkMove':
            return 'move';
        case 'link':
            return 'link';
        default:
            return 'copy';
    }
}

export function registerDropTarget(element: HTMLElement | null, dotNetRef: DotNetObject, options: DropTargetOptions | null) {
    if (!element) return null;
    const target: DropTarget = {
        element,
        dotNetRef,
        options: options || {},
        edge: null,
        lastDropId: null,
        dropSerial: 0,
        entered: new Set<EventTarget>(),
        over: false,
        reset: () => {
            target.entered.clear();
            target.over = false;
            target.edge = null;
        },
    };

    // Hover is tracked per node that raised dragenter. Nodes removed or moved out of the target
    // since (e.g. re-rendered by Blazor) never raise dragleave, so they are pruned here.
    const refresh = (dataTransfer: DataTransfer | null) => {
        for (const node of target.entered) {
            if (!(node instanceof Node) || !node.isConnected || !element.contains(node)) target.entered.delete(node);
        }
        const over = target.entered.size > 0;
        if (over !== target.over) {
            target.over = over;
            if (!over) target.edge = null; // .NET forgets the edge with the hover
            invoke(dotNetRef, 'HandleHoverChanged', over, takenTypes(target, dataTransfer));
        }
    };

    // the edge the drag over the target is closest to, reported when it changes
    const reportEdge = (e: DragEvent) => {
        if (!target.over || !target.options.edges) return;
        const edge = closestEdge(element, e.clientX, e.clientY, target.options.edges);
        if (edge === target.edge) return;
        target.edge = edge;
        invoke(dotNetRef, 'HandleEdgeChanged', edge);
    };

    const canDrop = (dataTransfer: DataTransfer | null): boolean => {
        if (isInAppDrag(dataTransfer)) return !!session && !!target.options.canDrop;
        const types = takenTypes(target, dataTransfer);
        return (target.options.nativeTypes || []).some((t) => types.includes(t));
    };

    const onDragEnter = (e: DragEvent) => {
        if (e.target) target.entered.add(e.target);
        refresh(e.dataTransfer);
        reportEdge(e);
        applyDropEffect(e, canDrop(e.dataTransfer));
    };

    const onDragOver = (e: DragEvent) => {
        // the hover may have started before this target was registered
        if (target.entered.size === 0 && e.target) target.entered.add(e.target);
        refresh(e.dataTransfer);
        reportEdge(e);
        applyDropEffect(e, canDrop(e.dataTransfer));
    };

    const onDragLeave = (e: DragEvent) => {
        if (e.target) target.entered.delete(e.target);
        refresh(e.dataTransfer);
    };

    const onDrop = (e: DragEvent) => {
        const dataTransfer = e.dataTransfer;
        const wasOver = target.over;
        // No dragleave follows a drop. Reset silently: managed OnDrop handlers may still query
        // IsOver, and the managed state is reset when the drag ends.
        target.reset();
        if (isNativeTextDragOverEditable(e)) {
            // The field takes the drop and no HandleNativeDrop follows, so nothing else would clear
            // the native hover reported for this target: report the leave.
            if (wasOver) invoke(dotNetRef, 'HandleHoverChanged', false, typesOf(dataTransfer));
            return;
        }
        e.preventDefault(); // never let the browser open or navigate to dropped content
        // in-app drops are handled by the managed ondrop handler
        if (!dataTransfer || isInAppDrag(dataTransfer) || !canDrop(dataTransfer)) return;

        // getData only works during the drop event, hence it is read here. Only what the target
        // accepts is sent: a page's text/html can be far larger than a Blazor Server circuit
        // message may be, and a target taking text or links has no use for it.
        const types = takenTypes(target, dataTransfer);
        const accepted = target.options.nativeTypes || [];
        const data: Record<string, string> = {};
        for (const t of types) {
            if (t === 'Files' || !accepted.includes(t)) continue;
            try {
                data[t] = dataTransfer.getData(t);
            } catch {
                /* ignore */
            }
        }
        if (!accepted.includes('Files') || !types.includes('Files')) {
            ++target.dropSerial; // a walk still running for an earlier drop is outdated
            sendNativeDrop(target, types, data, [], 0, false);
            return;
        }
        takeNativeDrop(target, dataTransfer, types, data, wasOver);
    };

    element.addEventListener('dragenter', onDragEnter);
    element.addEventListener('dragover', onDragOver);
    element.addEventListener('dragleave', onDragLeave);
    element.addEventListener('drop', onDrop);
    targets.add(target);
    targetsByElement.set(element, target);
    if (target.options.id) targetsById.set(target.options.id, target);

    return {
        update: (o: DropTargetOptions | null) => {
            if (target.options.id && targetsById.get(target.options.id) === target) targetsById.delete(target.options.id);
            // .NET drops an edge the target no longer lists: report the next one afresh
            if ((o?.edges || 0) !== (target.options.edges || 0)) target.edge = null;
            target.options = o || {};
            if (target.options.id) targetsById.set(target.options.id, target);
        },
        dispose: () => {
            element.removeEventListener('dragenter', onDragEnter);
            element.removeEventListener('dragover', onDragOver);
            element.removeEventListener('dragleave', onDragLeave);
            element.removeEventListener('drop', onDrop);
            targets.delete(target);
            releaseNativeDrop(target);
            if (targetsByElement.get(element) === target) targetsByElement.delete(element);
            if (target.options.id && targetsById.get(target.options.id) === target) targetsById.delete(target.options.id);
        },
    };
}

// ---- native drops ------------------------------------------------------------------------
// A target keeps the files of its last native drop - for .NET to stream their contents - until its next drop, or
// until it is disposed. Their metadata goes to .NET in pages, so that a drop of many files never makes a message
// larger than a Blazor Server circuit takes (32 KB by default).

interface DroppedFile {
    file: File;
    /** The path inside a dropped folder, with / separators; empty for a file dropped by itself. */
    path: string;
    directory: boolean;
    entry?: FileSystemEntry | null;
}

interface FileMeta {
    name: string;
    size: number;
    type: string;
    lastModified: string | null;
    relativePath: string;
    isDirectory: boolean;
}

const nativeDrops = new Map<string, { files: File[]; meta: FileMeta[] }>();
let dropSequence = 0;
// how many file descriptions go to .NET in one message
const FILE_PAGE = 50;
// a folder walk stops after this many entries (files and folders)
const MAX_FOLDER_ENTRIES = 10000;

// The DataTransfer types of a drag the target takes: a file drag whose files are all of types the target refuses is
// not taken. While dragging, browsers tell the files' MIME types but not their names - so only a target that lists
// MIME types alone can refuse a drag before its drop; with an extension listed, the drop decides.
function takenTypes(target: DropTarget, dataTransfer: DataTransfer | null): string[] {
    const types = typesOf(dataTransfer);
    const patterns = target.options.fileTypes || [];
    if (patterns.length === 0 || !types.includes('Files') || !dataTransfer || isInAppDrag(dataTransfer)) return types;
    if (patterns.some((p) => p.startsWith('.') || p === '*' || p === '*/*')) return types;
    const items = Array.from(dataTransfer.items || []).filter((i) => i.kind === 'file');
    if (items.length === 0 || items.some((i) => !i.type || fileMatches(patterns, '', i.type))) return types;
    return types.filter((t) => t !== 'Files');
}

// Whether a file is of one of the types an accept list names: a MIME type, a MIME group (image/*), any (* or */*)
// or an extension.
function fileMatches(patterns: string[], name: string, type: string): boolean {
    if (patterns.length === 0) return true;
    const lowerName = name.toLowerCase();
    const lowerType = type.toLowerCase();
    return patterns.some((pattern) => {
        const p = pattern.toLowerCase();
        if (p === '*' || p === '*/*') return true;
        if (p.startsWith('.')) return lowerName.endsWith(p);
        if (p.endsWith('/*')) return lowerType.startsWith(p.slice(0, -1));
        return lowerType === p;
    });
}

function entryOf(item: DataTransferItem): FileSystemEntry | null {
    try {
        return typeof item.webkitGetAsEntry === 'function' ? item.webkitGetAsEntry() : null;
    } catch {
        return null;
    }
}

// A walk of dropped folders: the files it keeps (of the target's types, up to its limit), how many files it saw,
// and whether it saw them all.
interface FolderWalk {
    kept: DroppedFile[];
    patterns: string[];
    max: number;
    seen: number;
    entries: number;
    incomplete: boolean;
    stopped: boolean;
}

// The files of the dropped folders, walked recursively, each with its path inside the folder. An entry that cannot
// be read (no permission) is skipped and marks the walk incomplete; so does stopping early, at the target's file
// limit or after MAX_FOLDER_ENTRIES entries.
async function expandFolders(dropped: DroppedFile[], patterns: string[], max: number): Promise<FolderWalk> {
    const walk: FolderWalk = { kept: [], patterns, max, seen: 0, entries: 0, incomplete: false, stopped: false };
    for (const d of dropped) {
        if (walk.stopped) break;
        if (d.directory && d.entry) {
            await walkFolder(d.entry as FileSystemDirectoryEntry, walk);
        } else if (!d.directory) {
            walk.seen++;
            keepWalked(walk, d);
        }
    }
    return walk;
}

function keepWalked(walk: FolderWalk, d: DroppedFile): void {
    if (!fileMatches(walk.patterns, d.file.name, d.file.type)) return;
    walk.kept.push(d);
    if (walk.max > 0 && walk.kept.length >= walk.max) {
        walk.stopped = true;
        walk.incomplete = true; // the rest is not counted
    }
}

async function walkFolder(folder: FileSystemDirectoryEntry, walk: FolderWalk): Promise<void> {
    let reader: FileSystemDirectoryReader;
    try {
        reader = folder.createReader();
    } catch {
        walk.incomplete = true;
        return;
    }
    for (;;) {
        let batch: FileSystemEntry[];
        try {
            // readEntries hands the entries out in batches, and an empty one at the end
            batch = await new Promise<FileSystemEntry[]>((resolve, reject) => reader.readEntries(resolve, reject));
        } catch {
            walk.incomplete = true;
            return;
        }
        if (batch.length === 0) return;
        for (const entry of batch) {
            if (walk.stopped) return;
            if (++walk.entries > MAX_FOLDER_ENTRIES) {
                walk.stopped = true;
                walk.incomplete = true;
                return;
            }
            if (entry.isDirectory) {
                await walkFolder(entry as FileSystemDirectoryEntry, walk);
            } else if (entry.isFile) {
                let file: File;
                try {
                    file = await new Promise<File>((resolve, reject) => (entry as FileSystemFileEntry).file(resolve, reject));
                } catch {
                    walk.incomplete = true;
                    continue;
                }
                walk.seen++;
                keepWalked(walk, { file, path: entry.fullPath.replace(/^\//, ''), directory: false });
            }
        }
    }
}

function releaseNativeDrop(target: DropTarget): void {
    if (target.lastDropId) nativeDrops.delete(target.lastDropId);
    target.lastDropId = null;
}

// Takes the files of a drop event: synchronously - the browser hands them out during the event only - and, for the
// folders of a target expanding them, by walking them after it. A walk that ends after the target was disposed, or
// after a newer drop on it, is dropped.
function takeNativeDrop(target: DropTarget, dataTransfer: DataTransfer, types: string[], data: Record<string, string>, wasOver: boolean): void {
    const serial = ++target.dropSerial;
    const files = Array.from(dataTransfer.files || []);
    const items = Array.from(dataTransfer.items || []).filter((i) => i.kind === 'file');
    const entries = items.length === files.length ? items.map(entryOf) : files.map(() => null);
    const dropped: DroppedFile[] = files.map((file, i) => ({ file, path: '', directory: entries[i]?.isDirectory === true, entry: entries[i] }));
    const patterns = target.options.fileTypes || [];
    const max = target.options.maxFiles || 0;
    if (!target.options.expandDirectories || !dropped.some((d) => d.directory && d.entry)) {
        // a folder that is not expanded is one entry, unless the target limits the types (a folder is of none)
        let kept = dropped.filter((d) => patterns.length === 0 || (!d.directory && fileMatches(patterns, d.file.name, d.file.type)));
        if (max > 0 && kept.length > max) kept = kept.slice(0, max);
        sendNativeDrop(target, types, data, kept, dropped.length, false);
        return;
    }
    // the walk may take a while: the target is no longer hovered meanwhile
    if (wasOver) invoke(target.dotNetRef, 'HandleHoverChanged', false, types);
    expandFolders(dropped, patterns, max).then((walk) => {
        if (!targets.has(target) || target.dropSerial !== serial) return;
        sendNativeDrop(target, types, data, walk.kept, walk.seen, walk.incomplete);
    });
}

// Hands a native drop to .NET: the accepted data, and the kept files - which the target keeps (in place of those of
// its previous drop) for .NET to read - described in pages.
function sendNativeDrop(target: DropTarget, types: string[], data: Record<string, string>, kept: DroppedFile[],
    total: number, incomplete: boolean): void {
    releaseNativeDrop(target);
    const meta: FileMeta[] = kept.map((d) => ({
        name: d.file.name,
        size: d.file.size,
        type: d.file.type,
        lastModified: lastModifiedOf(d.file),
        relativePath: d.path,
        isDirectory: d.directory,
    }));
    let dropId: string | null = null;
    if (kept.length > 0) {
        dropId = String(++dropSequence);
        nativeDrops.set(dropId, { files: kept.map((d) => d.file), meta });
        target.lastDropId = dropId;
    }
    invoke(target.dotNetRef, 'HandleNativeDrop', {
        types,
        data,
        files: meta.slice(0, FILE_PAGE),
        fileCount: meta.length,
        dropId,
        totalFiles: Math.max(total, meta.length),
        incomplete,
    });
}

function lastModifiedOf(file: File): string | null {
    try {
        return file.lastModified ? new Date(file.lastModified).toISOString() : null;
    } catch {
        return null;
    }
}

// The descriptions of a kept drop's files from offset on, a page at a time.
export function nativeDropFiles(dropId: string, offset: number, count: number): FileMeta[] {
    const drop = nativeDrops.get(dropId);
    return drop ? drop.meta.slice(offset, offset + Math.min(count, FILE_PAGE)) : [];
}

// The contents of a kept file, which .NET streams (a File is a Blob).
export function readNativeFile(dropId: string, index: number): Blob {
    const drop = nativeDrops.get(dropId);
    const file = drop && drop.files[index];
    if (!file) throw new Error('The file is no longer available: a drop target keeps the files of its last drop only.');
    return file;
}

// ---- pointer drags ----------------------------------------------------------------------
// Most touch browsers raise no native drag events, so touch and pen (and with pointerDrag 'all' the mouse) are
// followed through pointer events. A press starts the drag once it has rested long enough without moving (a touch
// on the source itself, so that a swipe still scrolls) or moved far enough (a mouse, a pen, a touch on a handle,
// which has touch-action: none). From then on the module moves a copy of the source with the pointer, finds the
// targets under it, scrolls what it nears the edge of, and reports to .NET - in order, through the drag queue - the
// start, every hover change, the OnHover moves (coalesced to what .NET keeps up with), the drop and the end.

const SCROLL_EDGE = 48; // the band along a scroll container's edge where a pointer drag scrolls it, in CSS pixels
const SCROLL_SPEED = 18; // how far it scrolls per frame at the very edge
const SCROLL_AXES_TTL = 500; // how long a container's scroll axes are trusted (a list may start overflowing)
const REHIT_INTERVAL = 100; // how often a resting pointer is hit-tested again (the page may change under it)
const CLICK_SUPPRESSION_MS = 100;
const TOUCH_TOLERANCE = 4; // how far a touch may wander during its delay even with a DragThreshold of 0
// After a touch press ends, the native drag a platform lifts from a long press (iOS starts one after cancelling the
// pointer) is still refused for a moment.
const NATIVE_DRAG_GRACE_MS = 600;
let nativeDragRefusedUntil = 0;

// Native drags never start while a pointer drag runs, or right after a touch press - not from a source (handled in
// its dragstart listener), and not from an image, a link or any other draggable element either.
document.addEventListener('dragstart', (e) => {
    if (pointerSession || performance.now() < nativeDragRefusedUntil) e.preventDefault();
}, true);

// Pointer events a drag has handled already: the document and the pressed element both listen (see startPointerPress).
const handledPointerEvents = new WeakSet<Event>();

// Form fields take their own presses (selecting text, opening a list).
function pressesAField(target: Element, root: Element): boolean {
    for (let node: Element | null = target; node && node !== root; node = node.parentElement) {
        if (node instanceof HTMLElement && takesTyping(node)) return true;
    }
    return false;
}

// A press on a form field, or in a no-drag region, of a natively draggable source is that element's: the source (or
// its handle) is not draggable until the press ends, so the browser selects text in the field - Firefox selects none
// inside a draggable element - and starts no native drag from it.
function holdNativeDrag(source: DragSource, target: Element): void {
    const draggable = target.closest('[draggable="true"]');
    if (!draggable || !source.element.contains(draggable)) return;
    draggable.setAttribute('draggable', 'false');
    const release = () => {
        document.removeEventListener('pointerup', release, true);
        document.removeEventListener('pointercancel', release, true);
        window.removeEventListener('blur', release);
        // back to what the options say, in case a render changed them meanwhile
        const o = source.options;
        if (draggable.getAttribute('draggable') === 'false' && o.canDrag && o.pointerDrag !== 'all') draggable.setAttribute('draggable', 'true');
    };
    document.addEventListener('pointerup', release, true);
    document.addEventListener('pointercancel', release, true);
    window.addEventListener('blur', release);
}

function isNoDragPress(target: EventTarget | null, root: Element): boolean {
    if (!(target instanceof Element) || !root.contains(target)) return false;
    const region = target.closest('[' + NO_DRAG_ATTRIBUTE + ']');
    return !!region && root.contains(region);
}

function startPointerPress(source: DragSource, e: PointerEvent, onHandle: boolean): void {
    const o = source.options;
    const pressed = e.target instanceof Element ? e.target : null;
    const p: PointerSession = {
        source,
        pressed,
        pointerId: e.pointerId,
        pointerType: e.pointerType,
        startX: e.clientX,
        startY: e.clientY,
        x: e.clientX,
        y: e.clientY,
        delay: e.pointerType !== 'mouse' && !onHandle ? Math.max(0, o.touchDelay ?? 250) : 0,
        threshold: Math.max(0, o.dragThreshold ?? 5),
        released: false,
        dragging: false,
        ended: false,
        begun: null,
        timer: 0,
        frame: 0,
        interval: 0,
        preview: null,
        hovered: [],
        hoverSlot: null,
        sentX: NaN,
        sentY: NaN,
        restore: [],
        scrollAxes: new Map(),
        onMove: (ev) => {
            if (once(ev)) onPointerMove(p, ev);
        },
        onUp: (ev) => {
            if (ev.pointerId !== p.pointerId || !once(ev)) return;
            p.released = true;
            p.x = ev.clientX;
            p.y = ev.clientY;
            endPointerSession(p, true);
        },
        onCancel: (ev) => {
            if (ev.pointerId !== p.pointerId || !once(ev)) return;
            p.released = true;
            endPointerSession(p, false);
        },
        onKeyDown: (ev) => {
            if (ev.key !== 'Escape' && ev.key !== 'Esc') return;
            if (p.dragging) {
                ev.preventDefault();
                ev.stopPropagation();
            }
            endPointerSession(p, false);
        },
        // a long press opens no context menu while it may become, or is, a drag
        onContextMenu: (ev) => ev.preventDefault(),
        onSelectStart: (ev) => {
            if (p.dragging || p.pointerType === 'mouse') ev.preventDefault();
        },
        // once dragging, a touch moves the drag and not the page
        onTouchMove: (ev) => {
            if (p.dragging && ev.cancelable) ev.preventDefault();
        },
        onTouchEnd: (ev) => {
            if (!once(ev)) return;
            p.released = true;
            endPointerSession(p, ev.type === 'touchend');
        },
        // another press while a drag runs: the release of this one went unseen (its element was removed, say)
        onAnotherPress: (ev) => {
            if (ev.pointerId !== p.pointerId) endPointerSession(p, false);
        },
        onBlur: () => endPointerSession(p, false),
        onVisibility: () => {
            if (document.visibilityState === 'hidden') endPointerSession(p, false);
        },
    };
    pointerSession = p;
    document.addEventListener('pointermove', p.onMove, true);
    document.addEventListener('pointerup', p.onUp, true);
    document.addEventListener('pointercancel', p.onCancel, true);
    document.addEventListener('pointerdown', p.onAnotherPress, true);
    document.addEventListener('keydown', p.onKeyDown, true);
    document.addEventListener('contextmenu', p.onContextMenu, true);
    document.addEventListener('selectstart', p.onSelectStart, true);
    document.addEventListener('touchmove', p.onTouchMove, { capture: true, passive: false });
    window.addEventListener('blur', p.onBlur);
    document.addEventListener('visibilitychange', p.onVisibility);
    // Touch events, and on some engines a touch's pointer events, keep going to the element the finger pressed even
    // once a re-render has removed it - and an element out of the document no longer passes them on to it. The
    // pressed element listens too, so the moves, the release and the touchmove that must not scroll still arrive.
    if (pressed) {
        pressed.addEventListener('pointermove', p.onMove as EventListener);
        pressed.addEventListener('pointerup', p.onUp as EventListener);
        pressed.addEventListener('pointercancel', p.onCancel as EventListener);
        if (e.pointerType !== 'mouse') {
            pressed.addEventListener('touchmove', p.onTouchMove as EventListener, { passive: false });
            pressed.addEventListener('touchend', p.onTouchEnd as EventListener);
            pressed.addEventListener('touchcancel', p.onTouchEnd as EventListener);
        }
    }
    if (e.pointerType !== 'mouse') holdStill(p);
    if (p.delay > 0) {
        p.timer = window.setTimeout(() => {
            p.timer = 0;
            activatePointerDrag(p);
        }, p.delay);
    } else if (p.threshold === 0) {
        activatePointerDrag(p);
    }
}

// Whether a pointer or touch event is seen here for the first time (the document's and the pressed element's
// listeners both get it while the element is in the document).
function once(e: Event): boolean {
    if (handledPointerEvents.has(e)) return false;
    handledPointerEvents.add(e);
    return true;
}

// A touch press selects no text, shows no callout, and lifts none of the platform's own long-press drags (iOS,
// Android and Windows browsers start native drags of draggable elements, images and links on a long press).
function holdStill(p: PointerSession): void {
    const element = p.source.element;
    const styled = [element, ...Array.from(element.querySelectorAll<HTMLElement>('img, a[href], [draggable="true"]'))];
    const properties = ['user-select', '-webkit-user-select', '-webkit-touch-callout', '-webkit-user-drag'];
    const saved = styled.map((el) => properties.map((name) => [el.style.getPropertyValue(name), el.style.getPropertyPriority(name)]));
    for (const el of styled) for (const name of properties) el.style.setProperty(name, 'none');
    // Blink takes draggable="true" over -webkit-user-drag: the source (and its handles) are not natively draggable
    // for the press. They are put back to what their options say rather than to what they were, in case the
    // drag re-rendered them meanwhile.
    const handles = Array.from(element.querySelectorAll<HTMLElement>('[' + HANDLE_ATTRIBUTE + '][draggable="true"]'));
    const hostWasDraggable = element.getAttribute('draggable') === 'true';
    if (hostWasDraggable) element.setAttribute('draggable', 'false');
    for (const handle of handles) handle.setAttribute('draggable', 'false');
    p.restore.push(() => {
        styled.forEach((el, i) => properties.forEach((name, j) => {
            const [value, priority] = saved[i][j];
            if (value) el.style.setProperty(name, value, priority);
            else el.style.removeProperty(name);
        }));
        const o = p.source.options;
        const native = o.canDrag && o.pointerDrag !== 'all';
        if (hostWasDraggable && native && !o.useHandle) element.setAttribute('draggable', 'true');
        for (const handle of handles) if (native && o.useHandle && handle.getAttribute('draggable') === 'false') handle.setAttribute('draggable', 'true');
    });
}

function onPointerMove(p: PointerSession, e: PointerEvent): void {
    if (e.pointerId !== p.pointerId) return;
    p.x = e.clientX;
    p.y = e.clientY;
    if (!p.dragging) {
        const distance = Math.hypot(p.x - p.startX, p.y - p.startY);
        if (p.timer) {
            // moved before the press qualified: a swipe, the page's to scroll
            if (distance > Math.max(p.threshold, TOUCH_TOLERANCE)) endPointerSession(p, false);
        } else if (distance >= p.threshold) {
            activatePointerDrag(p);
        }
        return;
    }
    // the button was released outside the window, where the release went unseen: a release away from every target
    if (e.pointerType === 'mouse' && (e.buttons & 1) === 0) {
        p.released = true;
        endPointerSession(p, false);
        return;
    }
    if (e.cancelable) e.preventDefault();
    schedulePointerFrame(p);
}

function activatePointerDrag(p: PointerSession): void {
    if (p.dragging || p.ended) return;
    p.dragging = true;
    if (p.timer) {
        clearTimeout(p.timer);
        p.timer = 0;
    }
    const source = p.source;
    const element = source.element;
    try {
        element.setPointerCapture(p.pointerId); // the moves and the release keep coming outside the window too
    } catch {
        /* ignore */
    }
    try {
        const selection = window.getSelection();
        if (selection) selection.removeAllRanges();
    } catch {
        /* ignore */
    }
    const root = document.documentElement.style;
    const rootUserSelect = root.getPropertyValue('user-select');
    root.setProperty('user-select', 'none');
    p.restore.push(() => {
        if (rootUserSelect) root.setProperty('user-select', rootUserSelect);
        else root.removeProperty('user-select');
    });

    const rect = boxRect(element);
    source.rect = rect;
    if (!source.options.useEmptyPreview) {
        try {
            p.preview = createPreview(element, rect);
        } catch {
            p.preview = null; // a drag without its copy is still a drag
        }
    }
    p.interval = window.setInterval(() => schedulePointerFrame(p), REHIT_INTERVAL);

    const startX = p.startX, startY = p.startY;
    enqueueDrag(() => invokeForResult(source.dotNetRef, 'HandlePointerDragStart', startX, startY, rect).then((begun) => {
        p.begun = begun === true;
        if (!p.begun) endPointerSession(p, false); // refused (an ItemFactory answering null, say): no drag at all
        else schedulePointerFrame(p);
    }));
    schedulePointerFrame(p);
}

function schedulePointerFrame(p: PointerSession): void {
    if (!p.frame && p.dragging && !p.ended) p.frame = requestAnimationFrame(() => pointerFrame(p));
}

function pointerFrame(p: PointerSession): void {
    p.frame = 0;
    if (p.ended || !p.dragging) return;
    if (p.preview) {
        if (previewIsLost(p.preview)) reattachPreview(p.preview);
        const axis = p.source.options.previewAxis;
        const dx = axis === 'y' ? 0 : p.x - p.startX;
        const dy = axis === 'x' ? 0 : p.y - p.startY;
        p.preview.style.transform = `translate3d(${dx}px, ${dy}px, 0)`;
    }
    const scrolled = p.source.options.autoScroll !== false && autoScroll(p);
    updatePointerHover(p);
    if (p.begun === true && p.source.options.trackPointer && (p.x !== p.sentX || p.y !== p.sentY)) {
        p.sentX = p.x;
        p.sentY = p.y;
        invoke(p.source.dotNetRef, 'HandleDragMove', p.x, p.y);
    }
    // keep scrolling while the pointer rests near an edge
    if (scrolled) schedulePointerFrame(p);
}

// The registered targets under a point, innermost first. The preview is not hit: it takes no pointer events.
function targetsAt(x: number, y: number): DropTarget[] {
    const chain: DropTarget[] = [];
    for (let node = document.elementFromPoint(x, y); node; node = node.parentElement) {
        const target = targetsByElement.get(node);
        if (target) chain.push(target);
    }
    return chain;
}

function updatePointerHover(p: PointerSession): void {
    const chain = targetsAt(p.x, p.y);
    const previous = p.hovered;
    p.hovered = chain;
    const types = [SOURCE_TYPE_PREFIX + (p.source.options.id || '')];
    let changed = false;
    for (const target of previous) {
        if (chain.includes(target)) continue;
        target.edge = null; // .NET forgets the edge with the hover
        enqueueHoverChange(p, target, false, types);
        changed = true;
    }
    for (const target of chain) {
        if (previous.includes(target)) continue;
        enqueueHoverChange(p, target, true, types);
        changed = true;
    }
    for (const target of chain) {
        if (!target.options.edges) continue;
        const edge = closestEdge(target.element, p.x, p.y, target.options.edges);
        if (edge === target.edge) continue;
        target.edge = edge;
        enqueueDrag(() => (p.begun === true ? invokeForResult(target.dotNetRef, 'HandleEdgeChanged', edge) : undefined));
        changed = true;
    }
    // a report already waiting was for the targets (and edges) before this change: it keeps them, and this one gets
    // its own
    if (changed) p.hoverSlot = null;
    reportPointerHover(p, chain.filter((t) => t.options.hover));
}

function enqueueHoverChange(p: PointerSession, target: DropTarget, over: boolean, types: string[]): void {
    enqueueDrag(() => (p.begun === true ? invokeForResult(target.dotNetRef, 'HandleHoverChanged', over, types) : undefined));
}

// OnHover moves are coalesced: one report per set of hovered targets waits in the queue at a time, carrying the
// latest position, so a slow handler (or a Blazor Server round trip) is not flooded - and the report runs after the
// hover changes before it and before the ones after it, so a handler sees the hover state its report belongs to.
function reportPointerHover(p: PointerSession, targets: DropTarget[]): void {
    if (targets.length === 0) return;
    if (p.hoverSlot) {
        p.hoverSlot.x = p.x;
        p.hoverSlot.y = p.y;
        return;
    }
    const slot = { targets, x: p.x, y: p.y };
    p.hoverSlot = slot;
    enqueueDrag(() => {
        if (p.hoverSlot === slot) p.hoverSlot = null;
        if (p.begun !== true) return undefined;
        return Promise.all(slot.targets.map((t) => invokeForResult(t.dotNetRef, 'HandlePointerHover', slot.x, slot.y)));
    });
}

function endPointerSession(p: PointerSession, drop: boolean): void {
    if (p.ended) return;
    // the release point is the drop point: the hover and OnHover there come first (the last move may not have
    // reached a frame yet)
    if (drop && p.dragging) {
        try {
            updatePointerHover(p);
        } catch {
            /* ignore */
        }
    }
    p.ended = true;
    if (pointerSession === p) pointerSession = null;
    document.removeEventListener('pointermove', p.onMove, true);
    document.removeEventListener('pointerup', p.onUp, true);
    document.removeEventListener('pointercancel', p.onCancel, true);
    document.removeEventListener('pointerdown', p.onAnotherPress, true);
    document.removeEventListener('keydown', p.onKeyDown, true);
    document.removeEventListener('contextmenu', p.onContextMenu, true);
    document.removeEventListener('selectstart', p.onSelectStart, true);
    document.removeEventListener('touchmove', p.onTouchMove, true);
    window.removeEventListener('blur', p.onBlur);
    document.removeEventListener('visibilitychange', p.onVisibility);
    if (p.pressed) {
        p.pressed.removeEventListener('pointermove', p.onMove as EventListener);
        p.pressed.removeEventListener('pointerup', p.onUp as EventListener);
        p.pressed.removeEventListener('pointercancel', p.onCancel as EventListener);
        p.pressed.removeEventListener('touchmove', p.onTouchMove as EventListener);
        p.pressed.removeEventListener('touchend', p.onTouchEnd as EventListener);
        p.pressed.removeEventListener('touchcancel', p.onTouchEnd as EventListener);
    }
    if (p.timer) clearTimeout(p.timer);
    if (p.frame) cancelAnimationFrame(p.frame);
    if (p.interval) clearInterval(p.interval);
    for (const undo of p.restore.reverse()) undo();
    if (p.pointerType !== 'mouse') nativeDragRefusedUntil = performance.now() + NATIVE_DRAG_GRACE_MS;
    try {
        if (p.source.element.hasPointerCapture(p.pointerId)) p.source.element.releasePointerCapture(p.pointerId);
    } catch {
        /* ignore */
    }
    if (p.preview) {
        p.preview.remove();
        p.preview = null;
    }
    // .NET forgets the edges with the drag
    for (const target of p.hovered) target.edge = null;
    if (!p.dragging) return; // a tap, a click or a swipe: the page's own

    // the release raises a click on the element under it, which a drag must not trigger - also a release still to
    // come, after an Escape or a cancel
    suppressClick(p);
    const chain = drop ? targetsAt(p.x, p.y) : [];
    const x = p.x, y = p.y;
    enqueueDrag(async () => {
        if (p.begun !== true) return;
        // the innermost target under the pointer whose provider owns the drag runs the drop (on itself and on the
        // targets of that provider around it); the targets of other providers answer false
        for (const target of chain) {
            if ((await invokeForResult(target.dotNetRef, 'HandlePointerDrop', x, y)) === true) break;
        }
        await invokeForResult(p.source.dotNetRef, 'HandleDragEnd');
    });
}

// Swallows the click of the drag's release: right after it, or - when the drag ended before the pointer was
// released - after that pointer's release.
function suppressClick(p: PointerSession): void {
    const stop = (e: Event) => {
        e.preventDefault();
        e.stopPropagation();
        done();
    };
    let timer = 0;
    const done = () => {
        window.removeEventListener('click', stop, true);
        document.removeEventListener('pointerup', onRelease, true);
        clearTimeout(timer);
    };
    const onRelease = (e: PointerEvent) => {
        if (e.pointerId !== p.pointerId) return;
        document.removeEventListener('pointerup', onRelease, true);
        timer = window.setTimeout(done, CLICK_SUPPRESSION_MS);
    };
    window.addEventListener('click', stop, true);
    if (p.released) timer = window.setTimeout(done, CLICK_SUPPRESSION_MS);
    else document.addEventListener('pointerup', onRelease, true);
}

// The properties a popover's user-agent style sets, which the copy takes from the source instead.
const POPOVER_RESET = [
    'border-top-style', 'border-right-style', 'border-bottom-style', 'border-left-style',
    'border-top-width', 'border-right-width', 'border-bottom-width', 'border-left-width',
    'border-top-color', 'border-right-color', 'border-bottom-color', 'border-left-color',
    'padding-top', 'padding-right', 'padding-bottom', 'padding-left',
    'overflow-x', 'overflow-y', 'color', 'background-color',
];

// The text look a copy inherits from where the source is: kept for when it has to go on from the body.
const INHERITED_LOOK = ['font-family', 'font-size', 'font-weight', 'font-style', 'line-height', 'letter-spacing',
    'direction', 'color', 'white-space', 'text-align'];

// What a copy was made with: the source's place, and its inherited look there.
const previewOrigins = new WeakMap<HTMLElement, { rect: Rect; look: [string, string][] }>();

// A copy of the source, fixed over it, that the drag moves with the pointer. It goes next to the source, so the
// stylesheet rules that draw the source draw it too, and takes no pointer events, focus or accessibility tree.
// Where the browser has popovers it is shown in the top layer, above every stacking context and clipped by no
// ancestor; elsewhere it is placed against the ancestor that is its containing block.
function createPreview(element: HTMLElement, rect: Rect): HTMLElement {
    const preview = element.cloneNode(true) as HTMLElement;
    for (const node of [preview, ...Array.from(preview.querySelectorAll<HTMLElement>('*'))]) {
        node.removeAttribute('id'); // no duplicate ids
        node.removeAttribute('name'); // no radio button taken out of its group
        node.removeAttribute('tabindex');
        node.removeAttribute('draggable');
        node.removeAttribute('data-bit-bdnd-dragging');
        node.removeAttribute('autoplay'); // no second video or sound
        // Blazor finds the element of an ElementReference by this attribute: it must not find the copy
        for (const name of node.getAttributeNames()) if (name.startsWith('_bl_')) node.removeAttribute(name);
        if (node instanceof HTMLIFrameElement) node.removeAttribute('src'); // no second page loading
    }
    copyFieldValues(element, preview);
    preview.setAttribute(PREVIEW_ATTRIBUTE, '');
    preview.setAttribute('aria-hidden', 'true');
    preview.setAttribute('inert', '');
    const style = preview.style;
    const set = (name: string, value: string) => style.setProperty(name, value, 'important');
    set('position', 'fixed');
    set('left', rect.left + 'px');
    set('top', rect.top + 'px');
    set('right', 'auto');
    set('bottom', 'auto');
    set('width', rect.width + 'px');
    set('height', rect.height + 'px');
    set('margin', '0');
    set('box-sizing', 'border-box');
    set('pointer-events', 'none');
    set('z-index', '2147483647');
    set('transition', 'none');
    style.setProperty('transform', 'translate3d(0, 0, 0)');
    style.setProperty('will-change', 'transform');

    // In place first, as a plain element: its computed style is the source's rules and the page's
    // [data-bit-bdnd-preview] rules together - what the popover's own style must not replace.
    (element.parentElement || document.body).appendChild(preview);
    const computed = getComputedStyle(preview);
    previewOrigins.set(preview, { rect, look: INHERITED_LOOK.map((name) => [name, computed.getPropertyValue(name)]) });
    const topLayer = typeof preview.showPopover === 'function';
    if (topLayer) {
        for (const name of POPOVER_RESET) set(name, computed.getPropertyValue(name));
        preview.setAttribute('popover', 'manual');
        try {
            preview.showPopover();
            return preview;
        } catch {
            preview.removeAttribute('popover'); // shown below, in place
        }
    }
    // An ancestor with a transform (a filter, a containment) is the containing block of fixed elements: place the
    // copy where the source is all the same - or, when that ancestor also scales, from the body.
    const actual = preview.getBoundingClientRect();
    if (Math.abs(actual.width - rect.width) > 1 || Math.abs(actual.height - rect.height) > 1) {
        document.body.appendChild(preview);
    } else if (Math.abs(actual.left - rect.left) > 0.5 || Math.abs(actual.top - rect.top) > 0.5) {
        set('left', 2 * rect.left - actual.left + 'px');
        set('top', 2 * rect.top - actual.top + 'px');
    }
    return preview;
}

// Whether the copy is lost while the drag goes on: out of the document with the source's container (a virtualized
// row scrolled out of the rendered range, a list re-rendered under the drag), or hidden from the top layer because a
// re-render moved that container (moving an element closes the popovers in it).
function previewIsLost(preview: HTMLElement): boolean {
    if (!preview.isConnected) return true;
    if (!preview.hasAttribute('popover')) return false;
    try {
        return !preview.matches(':popover-open');
    } catch {
        return false;
    }
}

// Shows a lost copy again. One out of the document goes on from the body, where the rules that matched it through
// its ancestors no longer do: its own classes and the look it inherited, set on it now, still draw it.
function reattachPreview(preview: HTMLElement): void {
    const origin = previewOrigins.get(preview);
    if (!preview.isConnected) {
        if (origin) for (const [name, value] of origin.look) preview.style.setProperty(name, value);
        document.body.appendChild(preview);
    }
    if (preview.hasAttribute('popover')) {
        try {
            preview.showPopover();
            return;
        } catch {
            preview.removeAttribute('popover');
        }
    }
    // without the top layer the body is the containing block: the corner the source started at
    if (origin && preview.parentElement === document.body) {
        preview.style.setProperty('left', origin.rect.left + 'px', 'important');
        preview.style.setProperty('top', origin.rect.top + 'px', 'important');
    }
}

// cloneNode copies attributes, not what was typed into a field since
function copyFieldValues(from: HTMLElement, to: HTMLElement): void {
    const fields = 'input, textarea, select';
    const sources = from.querySelectorAll<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(fields);
    const copies = to.querySelectorAll<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(fields);
    sources.forEach((field, i) => {
        const copy = copies[i];
        if (!copy || (field instanceof HTMLInputElement && field.type === 'file')) return;
        try {
            copy.value = field.value;
            if (field instanceof HTMLInputElement && copy instanceof HTMLInputElement) copy.checked = field.checked;
        } catch {
            /* ignore */
        }
    });
}

// Scrolls the innermost scroll container under the pointer that can scroll towards the edge the pointer is near -
// else the page, along the axes the page scrolls on. Answers whether anything scrolled.
function autoScroll(p: PointerSession): boolean {
    for (let node = document.elementFromPoint(p.x, p.y); node; node = node.parentElement) {
        if (node === document.body || node === document.documentElement) break;
        const axes = scrollAxesOf(p, node);
        if (axes && scrollNearEdge(node, node.getBoundingClientRect(), axes, p)) return true;
    }
    const page = document.scrollingElement || document.documentElement;
    const axes = pageScrollAxes(p);
    if (!axes) return false;
    return scrollNearEdge(page, { left: 0, top: 0, right: window.innerWidth, bottom: window.innerHeight }, axes, p);
}

function scrolls(overflow: string): boolean {
    return overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay';
}

function scrollAxesOf(p: PointerSession, element: Element): { x: boolean; y: boolean } | null {
    const now = performance.now();
    const cached = p.scrollAxes.get(element);
    if (cached && now - cached.at < SCROLL_AXES_TTL) return cached.axes;
    const style = getComputedStyle(element);
    const x = scrolls(style.overflowX) && element.scrollWidth > element.clientWidth;
    const y = scrolls(style.overflowY) && element.scrollHeight > element.clientHeight;
    const axes = x || y ? { x, y } : null;
    p.scrollAxes.set(element, { axes, at: now });
    return axes;
}

// The page scrolls on an axis unless the root or the body hide its overflow there (which script could still scroll).
function pageScrollAxes(p: PointerSession): { x: boolean; y: boolean } | null {
    const now = performance.now();
    const cached = p.scrollAxes.get(document.documentElement);
    if (cached && now - cached.at < SCROLL_AXES_TTL) return cached.axes;
    const hides = (overflow: string) => overflow === 'hidden' || overflow === 'clip';
    const root = getComputedStyle(document.documentElement);
    const body = document.body ? getComputedStyle(document.body) : null;
    const x = !hides(root.overflowX) && !(body && hides(body.overflowX));
    const y = !hides(root.overflowY) && !(body && hides(body.overflowY));
    const axes = x || y ? { x, y } : null;
    p.scrollAxes.set(document.documentElement, { axes, at: now });
    return axes;
}

function scrollNearEdge(element: Element, r: { left: number; top: number; right: number; bottom: number },
    axes: { x: boolean; y: boolean }, p: PointerSession): boolean {
    let dx = 0, dy = 0;
    if (axes.y) {
        const band = Math.min(SCROLL_EDGE, (r.bottom - r.top) / 4);
        if (p.y < r.top + band && element.scrollTop > 0) dy = -scrollSpeed(r.top + band - p.y, band);
        else if (p.y > r.bottom - band && element.scrollTop + element.clientHeight < element.scrollHeight - 1) dy = scrollSpeed(p.y - (r.bottom - band), band);
    }
    if (axes.x) {
        const band = Math.min(SCROLL_EDGE, (r.right - r.left) / 4);
        if (p.x < r.left + band && element.scrollLeft > 0) dx = -scrollSpeed(r.left + band - p.x, band);
        else if (p.x > r.right - band && element.scrollLeft + element.clientWidth < element.scrollWidth - 1) dx = scrollSpeed(p.x - (r.right - band), band);
    }
    if (!dx && !dy) return false;
    element.scrollBy(dx, dy);
    return true;
}

function scrollSpeed(depth: number, band: number): number {
    return Math.ceil(SCROLL_SPEED * Math.min(1, Math.max(0, depth) / band));
}

// ---- keyboard drags ---------------------------------------------------------------------
// The source keeps the focus during a keyboard drag; the keys that move, drop and cancel it are taken at the
// document (capture phase), so they work even while a re-render has left the focus nowhere, and they go to .NET
// one at a time, in order. .NET decides what each one does: which targets the item can be dropped on, the hover
// state, the OnHover/OnDrop chain and the end of the drag. Here only the document order of the targets, the
// rectangles and the focus are worked out.

// The element a key picks the source up from: the source itself, or with useHandle one of its handles.
function keyboardActivator(source: DragSource, target: EventTarget | null): HTMLElement | null {
    if (!(target instanceof HTMLElement)) return null;
    if (source.options.useHandle) {
        // the handle, or a control inside it (a handle wrapping a button of its own) - not a field one types in
        const handle = findHandle(target, source.element);
        return handle && (handle === target || !takesTyping(target)) ? target : null;
    }
    return target === source.element ? target : null;
}

function takesTyping(element: HTMLElement): boolean {
    return element.isContentEditable || element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement
        || element instanceof HTMLSelectElement;
}

// What a source's activator is, for focusing it: the source, or its first handle.
function activatorOf(source: DragSource): HTMLElement | null {
    if (!source.options.useHandle) return source.element;
    return source.element.querySelector<HTMLElement>('[' + HANDLE_ATTRIBUTE + ']');
}

// A name for a source that has no Label: its activator's aria-label, else the source's text.
function labelOf(activator: HTMLElement, element: HTMLElement): string {
    const label = activator.getAttribute('aria-label') || element.getAttribute('aria-label');
    if (label) return label;
    const text = (element.textContent || '').replace(/\s+/g, ' ').trim();
    return text.length > 80 ? text.slice(0, 80).trim() + '\u2026' : text;
}

function suppressKeyUp(key: string): void {
    suppressedKeyUps.set(key, performance.now());
}

function focusElement(element: HTMLElement): void {
    try {
        element.focus({ preventScroll: true });
    } catch {
        /* ignore */
    }
}

function startKeyboardSession(source: DragSource, activator: HTMLElement): void {
    const s: KeyboardSession = {
        source,
        activator,
        element: source.element,
        position: -1,
        ended: false,
        dotNetDone: false,
        pendingSince: 0,
        observer: null,
        onKeyDown: (e) => onKeyboardSessionKey(s, e),
        // the focus moved to another element (Tab, a click, the page focusing something): the drag is over. Also
        // when the activator had already been removed, so that no focusout could tell.
        onFocusIn: (e) => {
            if (e.target !== s.activator && e.target instanceof Element && e.target !== document.body) cancelKeyboardSession(s);
        },
        // checked once the focus has settled: a re-render loses it, and it is put back
        onFocusOut: (e) => {
            if (e.target === s.activator) setTimeout(() => checkKeyboardFocus(s), 0);
        },
        onPointerDown: () => cancelKeyboardSession(s),
    };
    keyboardSession = s;
    document.addEventListener('keydown', s.onKeyDown, true);
    document.addEventListener('focusin', s.onFocusIn, true);
    document.addEventListener('focusout', s.onFocusOut, true);
    document.addEventListener('pointerdown', s.onPointerDown, true);
    // a re-render that moves the focused element drops its focus without any event
    if (typeof MutationObserver !== 'undefined') {
        s.observer = new MutationObserver(() => keepKeyboardFocus(s));
        s.observer.observe(document.body, { childList: true, subtree: true });
    }
    const rect = toRect(source.element);
    const label = labelOf(activator, source.element);
    enqueueDrag(() => invokeForResult(source.dotNetRef, 'HandleKeyboardDragStart', rect, label).then((started) => {
        if (started === true) return;
        s.dotNetDone = true;
        endKeyboardSession(s);
    }));
}

// The calls into .NET of every keyboard and pointer drag, chained: they reach .NET in the order things happened -
// the keys pressed, the pointer moved - and the start of a drag only once the drop of the drag before it has been
// handled.
let dragQueue: Promise<unknown> = Promise.resolve();

function enqueueDrag(call: () => Promise<unknown> | undefined): void {
    // a link that fails must not stop the ones after it
    dragQueue = dragQueue.then(call).catch(() => undefined);
}

function onKeyboardSessionKey(s: KeyboardSession, e: KeyboardEvent): void {
    let command: string;
    // in a right-to-left layout the next place is to the left
    const rtl = getComputedStyle(s.activator).direction === 'rtl';
    switch (e.key) {
        case 'ArrowDown':
            command = 'down';
            break;
        case 'ArrowUp':
            command = 'up';
            break;
        case 'ArrowRight':
            command = rtl ? 'previous' : 'next';
            break;
        case 'ArrowLeft':
            command = rtl ? 'next' : 'previous';
            break;
        case 'Home':
            command = 'first';
            break;
        case 'End':
            command = 'last';
            break;
        case ' ':
        case 'Enter':
            command = 'drop';
            break;
        case 'Escape':
        case 'Esc':
            command = 'cancel';
            break;
        default:
            return; // Tab moves the focus on, which cancels the drag
    }
    if (e.altKey || e.ctrlKey || e.metaKey) return;
    // the focus is on another element (moved there without a focus event this session saw): the key is that
    // element's, and the drag is over
    const active = document.activeElement;
    if (active && active !== s.activator && active !== document.body && active.isConnected) {
        cancelKeyboardSession(s);
        return;
    }
    // the keys belong to the drag: no scrolling, and no shortcut of the page (an Escape closing a dialog)
    e.preventDefault();
    e.stopPropagation();
    if (command === 'drop') suppressKeyUp(e.key);
    if (e.repeat && (command === 'drop' || command === 'cancel')) return;
    keepKeyboardFocus(s);
    sendKeyboardCommand(s, command);
}

function sendKeyboardCommand(s: KeyboardSession, command: string): void {
    enqueueDrag(() => {
        if (s.dotNetDone) return undefined;
        return invokeForResult(s.source.dotNetRef, 'HandleKeyboardCommand', command).then((dragging) => {
            if (dragging === true) {
                keepKeyboardFocus(s);
                return;
            }
            s.dotNetDone = true;
            endKeyboardSession(s);
        });
    });
    // A drop or a cancel ends the drag here at once: the keys pressed from now on are the page's again - another
    // pickup, say - even before .NET has handled this one. Ended after the call is queued, so the focus is watched
    // until .NET has handled it (and re-rendered what the drop changed).
    if (command === 'drop' || command === 'cancel') endKeyboardSession(s);
}

// A pointer press, or the focus moving away: the drag is cancelled here at once - a pointer press may start
// another drag right away - and in .NET.
function cancelKeyboardSession(s: KeyboardSession): void {
    if (!s.ended) sendKeyboardCommand(s, 'cancel');
}

const FOCUS_SETTLE_MS = 300;

function endKeyboardSession(s: KeyboardSession): void {
    if (s.ended) return;
    s.ended = true;
    if (keyboardSession === s) keyboardSession = null;
    document.removeEventListener('keydown', s.onKeyDown, true);
    document.removeEventListener('focusin', s.onFocusIn, true);
    document.removeEventListener('focusout', s.onFocusOut, true);
    document.removeEventListener('pointerdown', s.onPointerDown, true);
    // the focus stays on (or goes back to) the item - also through the re-render its drop is about to cause
    keepKeyboardFocus(s);
    dragQueue.then(() => setTimeout(() => {
        if (s.observer) s.observer.disconnect();
        s.observer = null;
        keepKeyboardFocus(s);
    }, FOCUS_SETTLE_MS));
}

// The focus is on nothing: on the body, or on an element a re-render removed.
function focusIsLost(): boolean {
    const active = document.activeElement;
    return !active || active === document.body || !active.isConnected;
}

function keepKeyboardFocus(s: KeyboardSession): void {
    if (keyboardSession !== null && keyboardSession !== s) return; // another keyboard drag has the focus now
    if (s.activator.isConnected && document.activeElement !== s.activator && focusIsLost()) focusElement(s.activator);
}

function checkKeyboardFocus(s: KeyboardSession): void {
    if (s.ended || document.activeElement === s.activator) return;
    if (focusIsLost()) {
        keepKeyboardFocus(s);
        return;
    }
    // the focus was moved to another element on purpose (Tab, a click): the drag is over
    cancelKeyboardSession(s);
}

// A draggable that shows the item of the keyboard drag (a re-render moved it to another list) takes the focus over
// from the source whose element is gone - during the drag, or right after a drop.
function adoptFocus(source: DragSource): boolean {
    const activator = activatorOf(source);
    if (!activator || !activator.isConnected) return false;
    const s = keyboardSession;
    if (s && !s.ended) {
        if (s.activator !== activator && s.activator.isConnected) return false;
        s.activator = activator;
        s.element = source.element;
        focusElement(activator);
        return true;
    }
    if (!focusIsLost()) return false;
    focusElement(activator);
    return true;
}

function documentOrder(a: Element, b: Element): number {
    if (a === b) return 0;
    return a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
}

// A drop target as .NET names it to a keyboard drag: its id, the target of the same provider around it, its edges.
interface KeyboardCandidate {
    id: string;
    parentId?: string | null;
    edges?: number;
}

// A place a keyboard drag can be at: a target, or an edge of a target with edges (before it or after it).
interface Place {
    id: string;
    element: HTMLElement;
    parentId: string | null;
    edge: string | null;
    /** The place is the end of its target: after it, and after everything inside it. */
    atEnd: boolean;
    /** Whether the place is before (true) or after (false) its target in the list's order; null for a target. */
    before: boolean | null;
    /** The 0-based place of its target among the candidate targets. */
    targetIndex: number;
}

// The edges a keyboard drag places the item against on a target, as [before, after] in the list's order: of one
// axis - vertical when the target has top or bottom, else horizontal, where right comes first in a right-to-left
// layout.
function keyboardEdges(element: Element, edges: number): [string | null, string | null] | null {
    if (edges & (EDGE_TOP | EDGE_BOTTOM)) return [edges & EDGE_TOP ? 'top' : null, edges & EDGE_BOTTOM ? 'bottom' : null];
    if (edges & (EDGE_LEFT | EDGE_RIGHT)) {
        const left = edges & EDGE_LEFT ? 'left' : null;
        const right = edges & EDGE_RIGHT ? 'right' : null;
        return getComputedStyle(element).direction === 'rtl' ? [right, left] : [left, right];
    }
    return null;
}

// Document order of places: a target's start (or the target itself) where it begins, its end after all it holds.
function placeOrder(a: Place, b: Place): number {
    if (a.element === b.element) return (a.atEnd ? 1 : 0) - (b.atEnd ? 1 : 0);
    if (a.element.contains(b.element)) return a.atEnd ? 1 : -1;
    if (b.element.contains(a.element)) return b.atEnd ? -1 : 1;
    return documentOrder(a.element, b.element);
}

// Whether a place comes before the item's element: a target before it, or the start of one around it.
function placePrecedes(place: Place, element: Element): boolean {
    if (place.element.contains(element)) return !place.atEnd;
    return precedes(place.element, element);
}

// Whether two targets of one list are neighbours: no registered target - a candidate or not (one that refuses the
// item, a hidden one) - lies between them, and, without a list target around them, they share their parent element.
function areNeighbours(a: { element: HTMLElement; parentId: string | null }, b: { element: HTMLElement; parentId: string | null }): boolean {
    if (a.parentId !== b.parentId || a.element.contains(b.element) || b.element.contains(a.element)) return false;
    if (a.parentId === null && a.element.parentElement !== b.element.parentElement) return false;
    for (const t of targets) {
        const e = t.element;
        if (e === a.element || e === b.element || !e.isConnected || e.contains(a.element) || e.contains(b.element)) continue;
        if (a.element.contains(e) || b.element.contains(e)) continue;
        if (precedes(a.element, e) && precedes(e, b.element)) return false;
    }
    return true;
}

// Works out where a keyboard command takes the drag among the places the candidates .NET names make (the targets
// the item can be dropped on, and the edges of those that have edges), in document order: 'next' and 'previous' go
// on from the current place - or from the item's own element, before the first move and whenever a hover moved that
// element (a list reordered under the drag) - 'down' and 'up' do the same except in a layout that wraps (see
// rowPlace), and 'first', 'last', or 'stay' only count them. The target landed on is scrolled into view.
function keyboardNavigate(source: DragSource, candidates: KeyboardCandidate[], sourceTargets: string[],
    currentId: string | null, currentEdge: string | null, command: string, waiting = false): KeyboardPlace | null {
    const s = keyboardSession && keyboardSession.source === source ? keyboardSession : null;
    // asked again for rows that were coming in, after the drag was cancelled or dropped in the browser: no move
    if (waiting && (!s || s.ended)) return { id: '', edge: null, before: null, rect: null, label: null, position: 0, count: 0, moved: false, pending: false };
    const element = s ? s.element : source.element;
    const all: { id: string; element: HTMLElement; parentId: string | null; edges: number }[] = [];
    for (const candidate of candidates) {
        const target = targetsById.get(candidate.id);
        if (!target || !isReachable(target.element)) continue;
        all.push({ id: candidate.id, element: target.element, parentId: candidate.parentId || null, edges: candidate.edges || 0 });
    }
    // The item's own targets: those inside the dragged item (an item onto itself, a node into its subtree), and those
    // around it that hold no other candidate (a target wrapping each draggable, a column holding just the card) -
    // the item is already there. One around it holding other candidates (the list, an enclosing tree node) stays:
    // dropping on it is a move of its own.
    const connected = element.isConnected;
    const inside = (c: { element: HTMLElement }) => connected && element.contains(c.element);
    const around = (c: { element: HTMLElement }) => connected && c.element !== element && c.element.contains(element);
    const holdsOthers = (c: { element: HTMLElement }) =>
        all.some((o) => o !== c && !inside(o) && !around(o) && c.element.contains(o.element));
    const own = new Set(all.filter((c) => inside(c) || (around(c) && !holdsOthers(c))).map((c) => c.id));
    const list = all.filter((c) => !own.has(c.id));
    if (list.length === 0) return null;
    list.sort((a, b) => documentOrder(a.element, b.element));

    // The places, in document order. A target holding other candidates and no edges of its own (a list) is placed
    // after them: dropping on it is dropping past its items.
    let places: Place[] = [];
    list.forEach((c, targetIndex) => {
        const edges = keyboardEdges(c.element, c.edges);
        const base = { id: c.id, element: c.element, parentId: c.parentId, targetIndex };
        if (!edges) {
            places.push({ ...base, edge: null, atEnd: holdsOthers(c), before: null });
            return;
        }
        if (edges[0]) places.push({ ...base, edge: edges[0], atEnd: false, before: true });
        if (edges[1]) places.push({ ...base, edge: edges[1], atEnd: true, before: false });
    });
    places.sort(placeOrder);
    // The gap between two neighbouring items of one list is one place: after the first of them - or, where a row of
    // a wrapping list breaks between them, before the second: at the start of its row, where its line shows.
    const merged: Place[] = [];
    for (const p of places) {
        const previous = merged[merged.length - 1];
        if (previous && p.before === true && previous.before === false && areNeighbours(previous, p)) {
            if ((p.edge === 'left' || p.edge === 'right') && !sameRow(rectOf(previous.element), rectOf(p.element))) {
                merged[merged.length - 1] = p;
            }
            continue;
        }
        merged.push(p);
    }
    places = merged;
    // The gap the item itself is in is where it already is: no place to go to. The item's list is the target around
    // it that is not its own; its neighbours are the nearest items of that list before and after it.
    // A target around the item is its own by where it is, whether or not it takes the item (a tree row refusing the
    // node it holds is still that node's row).
    const ownSource = (id: string) => {
        if (own.has(id)) return true;
        const target = targetsById.get(id);
        return !!target && around(target) && !holdsOthers({ element: target.element });
    };
    let containerId: string | null = null;
    let slot: HTMLElement = element;
    if (connected) {
        containerId = sourceTargets.find((id) => !ownSource(id)) ?? null;
        const slotId = [...sourceTargets].reverse().find((id) => ownSource(id));
        slot = (slotId && targetsById.get(slotId)?.element) || element;
        const siblings = list.filter((c) => c.parentId === containerId && !c.element.contains(element)
            && (containerId !== null || c.element.parentElement === slot.parentElement));
        const previous = [...siblings].reverse().find((c) => precedes(c.element, element));
        const following = siblings.find((c) => !precedes(c.element, element));
        places = places.filter((p) => !(p.edge
            && ((previous && p.id === previous.id && p.before === false) || (following && p.id === following.id && p.before === true))));
    }
    if (places.length === 0) return null;

    const count = places.length;
    const index = currentId ? places.findIndex((p) => p.id === currentId && p.edge === (currentEdge || null)) : -1;
    const position = connected ? places.filter((p) => placePrecedes(p, element)).length : -1;
    // the item's element is the place to go on from when there is no current place, or when it moved since
    const fromElement = index < 0 || (s !== null && position >= 0 && position !== s.position);
    let next = -1;
    switch (command) {
        case 'first':
            next = 0;
            break;
        case 'last':
            next = count - 1;
            break;
        case 'next':
        case 'down':
            // from the element: the first place after it; with no element to go from, the first place
            next = fromElement ? (position < 0 ? 0 : position < count ? position : -1) : index + 1 < count ? index + 1 : -1;
            break;
        case 'previous':
        case 'up':
            next = fromElement ? (position < 0 ? count - 1 : position > 0 ? position - 1 : -1) : index - 1;
            break;
        default:
            next = index;
            break;
    }
    if ((command === 'down' || command === 'up') && next >= 0 && (fromElement ? connected : true)) {
        const reference = fromElement
            ? { element: slot, parentId: containerId, x: sideX(element, places[next].edge, command === 'down') }
            : { element: places[index].element, parentId: places[index].parentId, x: placeX(places[index]) };
        next = rowPlace(places, next, command === 'down', reference);
    }

    // A step that would leave the current list - or go nowhere - while that list has rows that way it has not rendered
    // (a virtualized list): they come first. The place they stand in is scrolled into view, once per key, and .NET
    // asks again (waiting) as they render and register.
    let pending = false;
    const stepping = command === 'next' || command === 'previous' || command === 'down' || command === 'up';
    const reference = !fromElement && index >= 0 ? { element: places[index].element, parentId: places[index].parentId }
        : connected ? { element: slot, parentId: containerId } : null;
    if (!waiting && s) s.pendingSince = 0;
    if (stepping && s && !s.ended && reference && (next < 0 || next === index || !sameListAs(reference)(places[next]))
        && !(waiting && s.pendingSince > 0 && performance.now() - s.pendingSince > PENDING_ROWS_LIMIT)) {
        const spacer = unrenderedRows(places, reference, command === 'next' || command === 'down');
        if (spacer) {
            if (!waiting) {
                s.pendingSince = performance.now();
                try {
                    spacer.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' as ScrollBehavior });
                } catch {
                    /* ignore */
                }
            }
            pending = true;
            next = index;
        }
    }

    const navigating = command === 'next' || command === 'previous' || command === 'down' || command === 'up'
        || command === 'first' || command === 'last';
    const moved = !pending && navigating && next >= 0 && (next !== index || fromElement);
    if (!moved && navigating) next = index; // nothing further that way: stay where the drag is
    if (next < 0) return { id: '', edge: null, before: null, rect: null, label: null, position: 0, count: list.length, moved: false, pending };

    const chosen = places[next];
    if (moved) {
        try {
            boxElement(chosen.element).scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' as ScrollBehavior });
        } catch {
            /* ignore */
        }
    }
    if (s && moved) s.position = position;
    return {
        id: chosen.id,
        edge: chosen.edge,
        before: chosen.before,
        rect: boxRect(chosen.element),
        label: chosen.element.getAttribute('aria-label') || chosen.element.getAttribute('title'),
        position: chosen.targetIndex + 1,
        count: list.length,
        moved,
        pending,
    };
}

// The places of the reference's list: those of the target it is in - or, with no list target, those beside it in the
// same parent element.
function sameListAs(reference: { element: HTMLElement; parentId: string | null }): (p: Place) => boolean {
    return (p: Place) => p.parentId === reference.parentId
        && (reference.parentId !== null || p.element.parentElement === reference.element.parentElement);
}

// Virtualize renders the rows around what is in view, and an empty spacer element of the height (or width) of the
// rows it leaves out before and after them. One past the list's last rendered place that way (the first, going back) -
// beside it, or beside the element it sits in - means the list goes on there.
function unrenderedRows(places: Place[], reference: { element: HTMLElement; parentId: string | null }, forward: boolean): HTMLElement | null {
    const own = places.filter(sameListAs(reference));
    const edge = own.length > 0 ? (forward ? own[own.length - 1] : own[0]).element : reference.element;
    for (const start of [edge, edge.parentElement]) {
        for (let el = start && (forward ? start.nextElementSibling : start.previousElementSibling); el;
            el = forward ? el.nextElementSibling : el.previousElementSibling) {
            // its size is an inline style, as Virtualize sets it - not a stylesheet's, as a decorative element's is
            if (!(el instanceof HTMLElement) || el.childElementCount > 0 || (el.textContent || '').trim() !== ''
                || (!el.style.height && !el.style.width)) continue;
            const r = el.getBoundingClientRect();
            if (r.height > 1 || r.width > 1) return el;
        }
    }
    return null;
}

// Up and Down in a layout that wraps (a grid of cards, chips that break into lines): when the reference - the current
// place, or the item before its first move - has places of its own list beside it, on its row, they go to the nearest
// row below or above instead of on in the list's order, to the place of that list closest across. In a list that
// runs down the page, and where no row of the list lies that way, the place in the list's order stands.
function rowPlace(places: Place[], linear: number, down: boolean,
    reference: { element: HTMLElement; parentId: string | null; x: number }): number {
    const from = rectOf(reference.element);
    const sameList = sameListAs(reference);
    const list = places.map((p, i) => ({ i, place: p, rect: rectOf(p.element) }))
        .filter((c) => c.place.element !== reference.element && sameList(c.place));
    // beside the reference: on its row, and not above or below it
    const beside = (r: DOMRect) => sameRow(r, from) && (r.right <= from.left + 1 || r.left >= from.right - 1);
    // a list that wraps: places beside the reference, and places on other rows (a single row of targets - columns
    // side by side - is a list that does not wrap)
    if (!list.some((c) => beside(c.rect)) || !list.some((c) => !sameRow(c.rect, from))) return linear;
    // how far past the reference a place's row starts, that way; negative for a place not past it
    const gap = (r: DOMRect) => (down ? r.top - (from.bottom - 1) : (from.top + 1) - r.bottom);
    const past = list.filter((c) => gap(c.rect) >= 0);
    if (past.length === 0) {
        // no row that way: on in the list's order, past the places of this row - to another list, or nowhere
        const step = down ? 1 : -1;
        let next = linear;
        while (next >= 0 && next < places.length && sameList(places[next]) && sameRow(rectOf(places[next].element), from)) next += step;
        return next >= 0 && next < places.length ? next : -1;
    }
    // the nearest row: the place nearest that way, and the places that share its row
    const nearest = past.reduce((a, b) => (gap(b.rect) < gap(a.rect) ? b : a));
    let best = -1;
    let bestDistance = Infinity;
    for (const c of past) {
        if (c !== nearest && !overlapsVertically(c.rect, nearest.rect)) continue;
        const distance = Math.abs(placeX(c.place) - reference.x);
        if (distance < bestDistance) {
            best = c.i;
            bestDistance = distance;
        }
    }
    return best;
}

function rectOf(element: Element): DOMRect {
    return boxElement(element).getBoundingClientRect();
}

function overlapsVertically(a: DOMRect, b: DOMRect): boolean {
    return Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1;
}

// Two boxes on one row of a wrapping layout: they overlap vertically by half the shorter of them, at least.
function sameRow(a: DOMRect, b: DOMRect): boolean {
    return Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) >= Math.min(a.height, b.height) / 2;
}

// Where a place is across the page: the side of its target an edge is on, or the target's middle.
function placeX(place: Place): number {
    const r = boxElement(place.element).getBoundingClientRect();
    if (place.edge === 'left') return r.left;
    if (place.edge === 'right') return r.right;
    return r.left + r.width / 2;
}

// Where the item itself is across the page, for its first move by row: down lands after the item below it, up before
// the item above it - so the side after it, or before it, in a list with left and right edges; its middle otherwise.
function sideX(element: HTMLElement, edge: string | null, down: boolean): number {
    const r = boxElement(element).getBoundingClientRect();
    if (edge !== 'left' && edge !== 'right') return r.left + r.width / 2;
    const rtl = getComputedStyle(element).direction === 'rtl';
    return down !== rtl ? r.right : r.left;
}

// A target the keyboard can go to: in the document, shown (not display:none or hidden), and not inert (behind a
// modal dialog, say).
function isReachable(element: HTMLElement): boolean {
    if (!element.isConnected || element.closest('[inert]')) return false;
    const box = boxElement(element);
    if (box.getClientRects().length === 0) return false;
    return getComputedStyle(box).visibility !== 'hidden';
}

// Whether a candidate comes before the item's element in document order; one that contains the element comes
// before it.
function precedes(candidate: Element, element: Element): boolean {
    return !!(element.compareDocumentPosition(candidate) & Node.DOCUMENT_POSITION_PRECEDING);
}

// ---- screen readers ---------------------------------------------------------------------

function hideVisually(element: HTMLElement): void {
    const style = element.style;
    style.position = 'absolute';
    style.width = '1px';
    style.height = '1px';
    style.margin = '-1px';
    style.padding = '0';
    style.border = '0';
    style.overflow = 'hidden';
    style.clip = 'rect(0 0 0 0)';
    style.clipPath = 'inset(50%)';
    style.whiteSpace = 'nowrap';
}

// One live region per document, in the body, so a provider's markup is not changed by it.
function ensureLiveRegion(): HTMLElement | null {
    if (liveRegion && liveRegion.isConnected) return liveRegion;
    if (!document.body) return null;
    const region = document.createElement('div');
    region.id = 'bit-bdnd-live-region';
    region.setAttribute('role', 'status');
    region.setAttribute('aria-live', 'assertive');
    region.setAttribute('aria-atomic', 'true');
    hideVisually(region);
    document.body.appendChild(region);
    liveRegion = region;
    return region;
}

// The instructions keyboard-draggable sources reference through aria-describedby: one hidden element per text.
function ensureInstructions(id: string, text: string): void {
    if (!document.body) return;
    let element = document.getElementById(id);
    if (!element) {
        element = document.createElement('div');
        element.id = id;
        element.hidden = true;
        document.body.appendChild(element);
    }
    if (element.textContent !== text) element.textContent = text;
}

export function announce(text: string): void {
    const region = ensureLiveRegion();
    if (!region) return;
    // the same text twice in a row has to be a change too, or it is not read again
    region.textContent = region.textContent === text ? text + '\u00a0' : text;
}
