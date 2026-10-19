// Bit.Bdnd HTML5 backend interop.
// Blazor raises the managed drag events (ondragstart/ondragover/ondrop). This module covers what
// managed handlers cannot do, because they cannot touch the live DataTransfer and (in Blazor
// Server) do not run while the native event is being dispatched:
//   1) configure a drag at dragstart: drag data, effectAllowed, drag image and drag handles,
//   2) track the pointer during a drag (Firefox reports 0,0 in 'drag' events),
//   3) report the end of a drag, even when the source element was removed from the DOM,
//   4) track hover per drop target in a way that survives re-rendered/removed child nodes,
//   5) give drop-effect feedback and read native drop payloads (files/text/url/html),
//   6) read an element's bounding rectangle.

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
}

/** Mirrors BdndDropTargetOptions. */
interface DropTargetOptions {
    /** Whether the target can accept the in-app item currently being dragged. */
    canDrop?: boolean;
    /** The DataTransfer types of the native drags the target accepts. */
    nativeTypes?: string[];
}

interface DragSource {
    element: HTMLElement;
    dotNetRef: DotNetObject;
    options: DragSourceOptions;
    rect: Rect | null;
}

interface DropTarget {
    options: DropTargetOptions;
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
    onMouseMove: () => void;
}

interface DropEffectState {
    accepted: boolean;
    rejected: boolean;
}

// Marks drags started by a registered source; the handler id follows the prefix.
const SOURCE_TYPE_PREFIX = 'application/x-bit-bdnd-';
const HANDLE_ATTRIBUTE = 'data-bit-bdnd-handle';
const EMPTY_IMAGE_SRC =
    'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';

// Firefox can raise a mousemove right as a drag starts, so mouse movement is only taken as
// "the drag is over" after this delay (the same approach as React DnD's HTML5 backend).
const SOURCE_REMOVED_CHECK_DELAY = 1000;

const claimedEvents = new WeakSet<Event>(); // dragstart events already handled by an inner source
const dropEffectStates = new WeakMap<Event, DropEffectState>(); // dragenter/dragover event -> nested targets' decision
const targets = new Set<DropTarget>();
let emptyImage: HTMLImageElement | null = null;
let session: Session | null = null;
let cancelRequested = false;

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
    const source: DragSource = { element, dotNetRef, options: {}, rect: null };

    const configure = (o: DragSourceOptions | null) => {
        source.options = o || {};
        // decode the transparent image before the first drag needs it
        if (source.options.useEmptyPreview) getEmptyImage();
    };

    const onDragStart = (e: DragEvent) => {
        // dragstart bubbles: the innermost registered source decides, its ancestors stay out
        // of it (the managed handler stops propagation the same way).
        if (claimedEvents.has(e)) return;
        claimedEvents.add(e);

        const o = source.options;
        const dataTransfer = e.dataTransfer;
        if (!dataTransfer || !o.canDrag || !o.id) return;
        const handle = o.useHandle ? findHandle(e.target as Node | null, element) : null;
        if (o.useHandle && !handle) return;

        // measured before anything re-renders; read by .NET as the source's initial offset
        const r = toRect(element);
        source.rect = r;
        try {
            // marks the drag as ours (Firefox also requires some data to start a drag)
            dataTransfer.setData(SOURCE_TYPE_PREFIX + o.id, o.id);
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

    configure(options);
    // Element-level listeners still fire after the element was removed from the DOM, whereas
    // Blazor's document-level listener never sees events of detached elements.
    element.addEventListener('dragstart', onDragStart);
    element.addEventListener('dragend', onDragEnd);

    return {
        update: configure,
        getDragStartRect: () => source.rect,
        cancelDrag: () => {
            if (!session || session.source !== source) return;
            cancelRequested = true;
            setTimeout(() => (cancelRequested = false), 0);
        },
        dispose: () => {
            element.removeEventListener('dragstart', onDragStart);
            element.removeEventListener('dragend', onDragEnd);
            if (session && session.source === source) endSession(false);
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
        onDragOver: (e: DragEvent) => {
            s.x = e.clientX;
            s.y = e.clientY;
            if (!s.frame) {
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
        onMouseMove: () => endIfSourceRemoved(s),
    };

    document.addEventListener('dragover', s.onDragOver, true);
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
    document.removeEventListener('drop', s.onDrop, true);
    window.removeEventListener('mousemove', s.onMouseMove, true);
    clearTimeout(s.timer);
    if (s.frame) cancelAnimationFrame(s.frame);
    for (const target of targets) target.reset();
    if (notify) invoke(s.source.dotNetRef, 'HandleDragEnd');
}

// ---- drop targets -------------------------------------------------------------------------

// Native drags over a text field or editable content are left to the browser, so text can still
// be dropped into inputs placed inside a drop target.
function isNativeDragOverEditable(e: DragEvent): boolean {
    if (isInAppDrag(e.dataTransfer)) return false;
    let node = e.target as Node | null;
    if (node && node.nodeType !== Node.ELEMENT_NODE) node = node.parentElement;
    const el = node as HTMLElement | null;
    return !!el && (el.isContentEditable || el.tagName === 'INPUT' || el.tagName === 'TEXTAREA');
}

// dragenter/dragover bubble through nested drop targets. The innermost target that can drop
// accepts the event; outer targets must not undo that decision.
function applyDropEffect(e: DragEvent, canDrop: boolean): void {
    const dataTransfer = e.dataTransfer;
    if (!dataTransfer) return;
    let state = dropEffectStates.get(e);
    if (!state) dropEffectStates.set(e, (state = { accepted: false, rejected: false }));
    if (state.accepted) return;
    if (isNativeDragOverEditable(e)) {
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
        options: options || {},
        entered: new Set<EventTarget>(),
        over: false,
        reset: () => {
            target.entered.clear();
            target.over = false;
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
            invoke(dotNetRef, 'HandleHoverChanged', over, typesOf(dataTransfer));
        }
    };

    const canDrop = (dataTransfer: DataTransfer | null): boolean => {
        if (isInAppDrag(dataTransfer)) return !!session && !!target.options.canDrop;
        const types = typesOf(dataTransfer);
        return (target.options.nativeTypes || []).some((t) => types.includes(t));
    };

    const onDragEnter = (e: DragEvent) => {
        if (e.target) target.entered.add(e.target);
        refresh(e.dataTransfer);
        applyDropEffect(e, canDrop(e.dataTransfer));
    };

    const onDragOver = (e: DragEvent) => {
        // the hover may have started before this target was registered
        if (target.entered.size === 0 && e.target) target.entered.add(e.target);
        refresh(e.dataTransfer);
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
        if (isNativeDragOverEditable(e)) {
            // The field takes the drop and no HandleNativeDrop follows, so nothing else would clear
            // the native hover reported for this target: report the leave.
            if (wasOver) invoke(dotNetRef, 'HandleHoverChanged', false, typesOf(dataTransfer));
            return;
        }
        e.preventDefault(); // never let the browser open or navigate to dropped content
        // in-app drops are handled by the managed ondrop handler
        if (!dataTransfer || isInAppDrag(dataTransfer) || !canDrop(dataTransfer)) return;

        // getData only works during the drop event, hence it is read here
        const types = typesOf(dataTransfer);
        const data: Record<string, string> = {};
        for (const t of types) {
            if (t === 'Files') continue;
            try {
                data[t] = dataTransfer.getData(t);
            } catch {
                /* ignore */
            }
        }
        const files = Array.from(dataTransfer.files || [], (f) => ({
            name: f.name,
            size: f.size,
            type: f.type,
        }));
        invoke(dotNetRef, 'HandleNativeDrop', { types, data, files });
    };

    element.addEventListener('dragenter', onDragEnter);
    element.addEventListener('dragover', onDragOver);
    element.addEventListener('dragleave', onDragLeave);
    element.addEventListener('drop', onDrop);
    targets.add(target);

    return {
        update: (o: DropTargetOptions | null) => {
            target.options = o || {};
        },
        dispose: () => {
            element.removeEventListener('dragenter', onDragEnter);
            element.removeEventListener('dragover', onDragOver);
            element.removeEventListener('dragleave', onDragLeave);
            element.removeEventListener('drop', onDrop);
            targets.delete(target);
        },
    };
}
