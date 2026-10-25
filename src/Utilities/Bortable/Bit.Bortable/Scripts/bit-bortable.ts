/*!
 * Bit.Bortable browser layer.
 *
 * The sorting engine - which lists take a drag, what a position means, every change to the bound lists and every
 * event - lives in C#. This module does what has to happen where the pointer is, without a round trip:
 *   1. it routes the input: it owns the native drag events, finds the list under the pointer that takes the drag,
 *      and tells .NET about a drop position only when it changes,
 *   2. drag handles (an item is draggable only while its handle element is pressed),
 *   3. drop-position hit testing, using the SortableJS swap-threshold model,
 *   4. FLIP animations, so items glide to their new positions instead of snapping.
 */

// The attributes Bortable.razor renders on its lists, its item wrappers and the drop placeholder.
const LIST = 'data-bortable-list';
const ITEM = 'data-bortable-item';
const KEY = 'data-bortable-key';
const PLACEHOLDER = 'data-bortable-placeholder';
const NO_DRAG = 'data-bortable-disabled';
const EASING = 'cubic-bezier(0.22, 1, 0.36, 1)';

// The data type that marks a drag as a list's own. Any other drag (a file, selected text) is left alone.
const DRAG_TYPE = 'application/x-bit-bortable';

/** The direction values BortableDirection is sent as. */
type Direction = 'auto' | 'horizontal' | 'vertical';

/** 1 = insert after the target, -1 = insert before it, 0 = do not move. */
type SwapDir = -1 | 0 | 1;

/** How a drag ended, as the engine is told: see EndFromBrowserAsync in Bortable.razor.cs. */
type EndKind = 'drop' | 'release' | 'cancel' | 'abandon';

/** The part of a .NET DotNetObjectReference this module calls. */
interface DotNetRef {
    invokeMethodAsync<T>(method: string, ...args: unknown[]): Promise<T>;
}

/** What a list tells the module about itself (BortableJsOptions in C#). */
interface ListOptions {
    id: string;
    handle: string | null;
    swapThreshold: number;
    invertSwap: boolean;
    invertedSwapThreshold: number;
    direction: Direction;
    animation: number;
    swap: boolean;
    multiDrag: boolean;
    keyboard: boolean;
    /** How long (ms) a mouse or pen press must be held before a drag starts. */
    delay: number;
    /** How long (ms) a touch must be held before a drag starts. */
    touchDelay: number;
    /** How far (px) the pointer may move while the delay runs before the press is taken for a scroll or a click. */
    delayTolerance: number;
    /** Drag with pointer events, a preview following the pointer, for the mouse too (always so for touch and pen). */
    forceFallback: boolean;
    /** The class of that preview. */
    dragClass: string;
    /** A pointer drag near an edge scrolls what is there; how close (px), and how fast (px per frame, at most). */
    scroll: boolean;
    scrollSensitivity: number;
    scrollSpeed: number;
}

/** A list on the page, as registered by its component. */
interface ListState {
    el: HTMLElement;
    ref: DotNetRef;
    opts: ListOptions;
}

/** What .NET answers when a drag starts (BortableStartResult in C#). */
interface StartResult {
    lists: string[];
    announce?: string | null;
    clone?: boolean;
    drag: number;
}

/** What .NET answers for a key (BortableKeyResult in C#). */
interface KeyResult {
    dragging: boolean;
    announce?: string | null;
    list?: string | null;
    key?: string | null;
    lists?: string[] | null;
    target?: string | null;
    drag?: number;
}

/** A visible item of a list, with its index among all of the list's items (hidden ones included). */
interface ItemEntry {
    el: Element;
    index: number;
    rect: DOMRect;
}

/** The drop placeholder of a list: the gap (an index) it fills, rather than an item index. */
interface PlaceholderEntry {
    el: Element;
    gap: number;
    rect: DOMRect;
}

/** Where the dragged item currently sits in a list: either as one of its items or as its placeholder. */
type Slot = ItemEntry | PlaceholderEntry;

/** The offset a running FLIP animation started from. */
interface FlipToken {
    dx: number;
    dy: number;
}

/** An element's layout box, ignoring the transforms of the FLIP animations. */
interface LayoutBox {
    left: number;
    top: number;
    width: number;
    height: number;
}

/** The last drop position sent to .NET, so the same one is not sent again while the pointer rests. */
interface Sent {
    list: ListState;
    index: number;
}

/**
 * One drag, from its start to the moment .NET has been told how it ended. There is at most one at a time: a
 * pointer drags one thing.
 */
interface Session {
    /**
     * A native drag (the mouse: the browser drags the item), a pointer drag (touch, pen, or the mouse with
     * forceFallback: this module drags a preview of the item), or a keyboard drag.
     */
    mode: 'native' | 'pointer' | 'keyboard';
    /** The drag's id, as .NET answered its start; sent back with every call about it, so a late one is never taken for another drag's. */
    dragId: number;
    /** Pointer: the pointer that drags. */
    pointerId: number;
    /** Undoes what the drag set up in the page (the preview, listeners); run as soon as it ends. */
    cleanup: (() => void) | null;
    /** Pointer: moves the preview to the pointer, on the next frame. */
    onMove: (() => void) | null;
    /** The drag puts a copy of the item into other lists (a clone pull). */
    clone: boolean;
    /** The list elements this drag marked with a state attribute (accepts, over, refused), to unmark at its end. */
    marked: Set<HTMLElement>;
    /** The list marked as the one under the pointer. */
    overEl: HTMLElement | null;
    /** The list the drag started in; its bridge is the one told how the drag ended. */
    home: ListState;
    /** Keyboard: the id of the list the item is in now, and the key it is rendered with there. */
    listId: string;
    key: string;
    /** Keyboard, swap mode: the key of the item highlighted as the swap target, and the id of its list. */
    swapKey: string | null;
    swapListId: string | null;
    /** The item element being dragged (the original, which stays in the page while it is dragged). */
    itemEl: HTMLElement;
    /** The node the browser drags (the item, or a link or image inside it), which receives its dragend. */
    dragNode: EventTarget | null;
    /** The pointer type the drag was started with, reported to .NET. */
    input: string;
    state: 'starting' | 'active' | 'ending' | 'ended' | 'refused';
    /** Ids of the lists that take this drag, as .NET answered at its start. */
    accepting: Set<string>;
    x: number;
    y: number;
    /** The list under the pointer that takes the drag, if any. */
    target: ListState | null;
    /** The list that refused the last position it was offered: the pointer shows it cannot drop there. */
    refused: ListState | null;
    /** The lists a change of this drag has touched, whose layout the next change may move again. */
    touched: Set<ListState>;
    /** A call to .NET is in flight; `dirty` asks for another hit test once it completes. */
    busy: Promise<void> | null;
    dirty: boolean;
    sent: Sent | null;
    /** Resolves once .NET has answered the start. */
    started: Promise<void>;
    /** When the last dragover was seen: a mouse move long after it means the browser lost the dragend. */
    lastOver: number;
}

// --- Registration ----------------------------------------------------------------------------------------

const lists = new Map<HTMLElement, ListState>();

let session: Session | null = null;

// --- Diagnostics -----------------------------------------------------------------------------------------

/** What the router has sent .NET, and how long the drop-position calls took: the hot path's own measure. */
const stats = { starts: 0, positions: 0, changes: 0, refusals: 0, keys: 0, ends: 0, totalMs: 0, slowestMs: 0 };

/**
 * The calls this module has made into .NET since the page loaded (or since `resetDiagnostics`): drags started,
 * drop positions sent (and how many changed a list, or were refused), keyboard steps, ends, and the average and
 * slowest round trip of a drop position, in milliseconds - the latency a user feels as the item follows the pointer.
 */
export function diagnostics(): Record<string, number> {
    return { ...stats, averageMs: stats.positions ? stats.totalMs / stats.positions : 0 };
}

export function resetDiagnostics(): void {
    for (const key of Object.keys(stats) as (keyof typeof stats)[]) stats[key] = 0;
}

// The pointer type of the last press, which is what a native drag that follows it is made with.
let lastPointerType = 'mouse';

/** Registers a list: called by its component after its first interactive render. */
export function register(listEl: HTMLElement | null, ref: DotNetRef, opts: ListOptions): void {
    if (!listEl || !ref) return;
    // Lists whose elements left the page without unregistering (a dropped circuit) are let go of here.
    for (const el of Array.from(lists.keys())) {
        if (!el.isConnected) lists.delete(el);
    }
    lists.set(listEl, { el: listEl, ref, opts });
    liveRegionOf();
    listEl.addEventListener('dragover', stopAtTarget);
    listEl.addEventListener('drop', stopAtTarget);
    // Non-passive on the list itself, so that a finger's moves over a list can still be kept from scrolling the page
    // once they drag an item (a browser decides at the touch whether its moves may be cancelled at all).
    listEl.addEventListener('touchmove', holdTouchScroll, { passive: false });
}

/** A finger that drags an item does not scroll the page. */
function holdTouchScroll(e: TouchEvent): void {
    const s = session;
    if (s && s.mode === 'pointer' && s.state !== 'ended' && e.cancelable) e.preventDefault();
}

/** Updates the options of a registered list (a parameter changed). */
export function configure(listEl: HTMLElement | null, opts: ListOptions): void {
    const list = listEl ? lists.get(listEl) : undefined;
    if (list) list.opts = opts;
}

/** Forgets a list: called by its component when it is disposed. */
export function unregister(listEl: HTMLElement | null): void {
    if (!listEl) return;
    listEl.removeEventListener('dragover', stopAtTarget);
    listEl.removeEventListener('drop', stopAtTarget);
    listEl.removeEventListener('touchmove', holdTouchScroll);
    const list = lists.get(listEl);
    lists.delete(listEl);
    const s = session;
    if (list && s) {
        if (s.target === list) s.target = null;
        s.touched.delete(list);
    }
}

/** The registered list an element is, if it is one. */
function listOf(el: Element | null): ListState | null {
    return el instanceof HTMLElement ? lists.get(el) ?? null : null;
}

/** The registered list whose own item `item` is (a direct child, not the drop placeholder). */
function listOfItem(item: Element): ListState | null {
    if (!item.hasAttribute(ITEM) || item.hasAttribute(PLACEHOLDER)) return null;
    return listOf(item.parentElement);
}

/** The items between `node` and the document that belong to a registered list, innermost first. */
function ownItemsAround(node: Element | null): { item: HTMLElement; list: ListState }[] {
    const found: { item: HTMLElement; list: ListState }[] = [];
    for (let el = node; el; el = el.parentElement) {
        if (!el.hasAttribute(ITEM)) continue;
        const list = listOfItem(el);
        if (list) found.push({ item: el as HTMLElement, list });
    }
    return found;
}

// --- Handles ---------------------------------------------------------------------------------------------

const warnedSelectors = new Set<string>();

/** `el.closest(selector)`, or null when the selector is not valid CSS (reported once, instead of on every press). */
function closestSafe(el: Element, selector: string): Element | null {
    try {
        return el.closest(selector);
    } catch {
        if (!warnedSelectors.has(selector)) {
            warnedSelectors.add(selector);
            console.warn(`Bit.Bortable: the Handle "${selector}" is not a valid CSS selector; no drag can start from it.`);
        }
        return null;
    }
}

/**
 * A press decides how a drag may start. Items behind a handle render as non-draggable, and pressing a handle makes
 * its item draggable for the drag that follows; every list between the press and the document decides for its own
 * item, so a handle of a nested list's item never drags the outer item. Then the innermost item's list takes the
 * press: a mouse drags natively (after the list's delay, if it has one), a finger or a pen - or the mouse, with
 * forceFallback - through the pointer engine below.
 */
document.addEventListener('pointerdown', (e: PointerEvent) => {
    lastPointerType = e.pointerType || 'mouse';

    // A press anywhere ends a keyboard drag where the item is, leaving the focus to the press.
    const s = session;
    if (s && s.mode === 'keyboard' && (s.state === 'active' || s.state === 'starting')) void keyStep(s, { action: 'drop', list: null, index: -1, refocus: false });

    const target = e.target instanceof Element ? e.target : null;
    if (!target) return;

    let innermost: { item: HTMLElement; list: ListState; onHandle: boolean } | null = null;
    for (const { item, list } of ownItemsAround(target)) {
        const selector = list.opts.handle;
        let onHandle = !selector;
        if (selector) {
            const handle = closestSafe(target, selector);
            onHandle = !!handle && handle.closest(`[${ITEM}]`) === item && !item.hasAttribute(NO_DRAG);
            item.setAttribute('draggable', onHandle ? 'true' : 'false');
        }
        innermost ??= { item, list, onHandle };
    }

    if (!innermost || !innermost.onHandle || innermost.item.hasAttribute(NO_DRAG)) return;
    if (e.pointerType === 'mouse' && e.button !== 0) return;
    const { item, list } = innermost;

    if (e.pointerType === 'mouse' && !list.opts.forceFallback) {
        if (list.opts.delay > 0) holdNativeDrag(e, item, list);
        return;
    }

    // A control of the app's own inside the item keeps its press (a handle is the one control that drags).
    const control = target.closest(INTERACTIVE);
    if (!list.opts.handle && control && control !== item && item.contains(control)) return;
    startPress(e, list, item);
}, true);

// --- Delayed native drags --------------------------------------------------------------------------------

/**
 * A mouse press on a list with a delay: the item stays undraggable until the press has been held long enough
 * without moving, and only then may the browser start its drag (the SortableJS `delay`).
 */
function holdNativeDrag(e: PointerEvent, item: HTMLElement, list: ListState): void {
    const rendered = item.getAttribute('draggable');
    item.setAttribute('draggable', 'false');
    const x0 = e.clientX, y0 = e.clientY;
    const restore = (): void => {
        if (rendered !== null) item.setAttribute('draggable', rendered);
    };
    // Held long enough: the item may be dragged now. Moved too far first: it stays undraggable for this press.
    const timer = window.setTimeout(() => {
        window.removeEventListener('pointermove', move, true);
        restore();
    }, list.opts.delay);
    const move = (m: PointerEvent): void => {
        if (m.pointerId !== e.pointerId || Math.hypot(m.clientX - x0, m.clientY - y0) <= list.opts.delayTolerance) return;
        clearTimeout(timer);
        window.removeEventListener('pointermove', move, true);
    };
    // The press ends - released, or taken over by the native drag it started (pointercancel) - and the item gets its
    // rendered draggable back in every case, so the next press starts from it.
    const end = (u: PointerEvent): void => {
        if (u.pointerId !== e.pointerId) return;
        clearTimeout(timer);
        window.removeEventListener('pointermove', move, true);
        window.removeEventListener('pointerup', end, true);
        window.removeEventListener('pointercancel', end, true);
        restore();
    };
    window.addEventListener('pointermove', move, true);
    window.addEventListener('pointerup', end, true);
    window.addEventListener('pointercancel', end, true);
}

// --- Pointer drags ---------------------------------------------------------------------------------------
// A finger or a pen (and the mouse, with forceFallback) drags with pointer events instead of the browser's own drag:
// the item stays in its list, dimmed, while a preview of it follows the pointer, and the list under the pointer is
// found by hit testing. Everything after that - the routing, the hit test, the calls to .NET - is the same router a
// native drag goes through.

// What a press on an item does not take over: the app's own controls inside it.
const INTERACTIVE = 'input, textarea, select, button, a[href], label, summary, [contenteditable=""], [contenteditable="true"]';

// How far (px) the pointer must move before a press without a delay becomes a drag, as the browser's own drag does.
const DRAG_THRESHOLD = 4;

/** A press on an item that may become a pointer drag. */
interface Press {
    pointerId: number;
    pointerType: string;
    list: ListState;
    item: HTMLElement;
    x0: number;
    y0: number;
    /** The press has been held long enough; with no delay, it is ready at once. */
    ready: boolean;
    timer: number;
    /** The draggable attribute the item was rendered with, put back once the press is over. */
    draggable: string | null;
}

let press: Press | null = null;

function startPress(e: PointerEvent, list: ListState, item: HTMLElement): void {
    endPress();
    const delay = e.pointerType === 'touch' ? list.opts.touchDelay : list.opts.delay;
    const p: Press = {
        pointerId: e.pointerId,
        pointerType: e.pointerType || 'mouse',
        list, item,
        x0: e.clientX,
        y0: e.clientY,
        ready: !(delay > 0),
        timer: 0,
        draggable: item.getAttribute('draggable'),
    };
    press = p;

    // The browser must not start a drag of its own from this press (a long press drags natively on mobile Safari).
    item.setAttribute('draggable', 'false');
    // A long press opens the context menu on touch screens.
    document.addEventListener('contextmenu', preventDefault, true);
    if (p.pointerType === 'mouse') document.addEventListener('selectstart', preventDefault, true);

    if (!p.ready) {
        p.timer = window.setTimeout(() => {
            if (press !== p) return;
            p.ready = true;
            activate(p, p.x0, p.y0);
        }, delay);
    }
}

/** Forgets the press: it never became a drag, or the drag that came of it is over. */
function endPress(): void {
    const p = press;
    if (!p) return;
    press = null;
    clearTimeout(p.timer);
    if (p.draggable !== null) p.item.setAttribute('draggable', p.draggable);
    resetHandle(p.item);
    document.removeEventListener('contextmenu', preventDefault, true);
    document.removeEventListener('selectstart', preventDefault, true);
}

function preventDefault(e: Event): void {
    if (e.cancelable) e.preventDefault();
}

/** The press becomes a drag: the session starts, the page stops scrolling under the finger, the preview appears. */
function activate(p: Press, x: number, y: number): void {
    if (!p.item.isConnected || !p.list.el.isConnected) {
        endPress();
        return;
    }
    const key = p.item.getAttribute(KEY);
    if (!key) {
        endPress();
        return;
    }

    const s = begin(p.list, p.item, null, key, p.pointerType, 'pointer', p.pointerId);
    s.x = x;
    s.y = y;

    // A pointer the browser captured for the element it went down on would keep its events there.
    try { p.item.releasePointerCapture(p.pointerId); } catch { /* not captured */ }

    const preview = makePreview(p.item, p.list.opts.dragClass);
    const rect = p.item.getBoundingClientRect();
    const dx = x - rect.left, dy = y - rect.top;
    let frame = 0;
    const place = (): void => {
        frame = 0;
        preview.style.transform = `translate3d(${s.x - dx - rect.left}px, ${s.y - dy - rect.top}px, 0)`;
    };
    s.onMove = () => { if (!frame) frame = requestAnimationFrame(place); };
    place();

    // Also outside the lists, as far as the browser lets it (see holdTouchScroll).
    document.addEventListener('touchmove', holdTouchScroll, { passive: false, capture: true });
    const userSelect = document.documentElement.style.userSelect;
    document.documentElement.style.userSelect = 'none';

    s.cleanup = () => {
        cancelAnimationFrame(frame);
        preview.remove();
        document.removeEventListener('touchmove', holdTouchScroll, { capture: true });
        document.documentElement.style.userSelect = userSelect;
        endPress();
    };

    routePointer(s, x, y);
}

/**
 * A copy of the item that follows the pointer, outside the lists and above everything. It carries the item's own
 * look, read off its computed style, and the drag class, which may add a shadow, a tilt or an opacity of its own.
 */
function makePreview(item: HTMLElement, dragClass: string): HTMLElement {
    const rect = item.getBoundingClientRect();
    const preview = item.cloneNode(true) as HTMLElement;
    for (const el of [preview, ...Array.from(preview.querySelectorAll('[id]'))]) el.removeAttribute('id');
    for (const attribute of [ITEM, KEY, NO_DRAG, 'tabindex', 'role', 'aria-describedby', 'aria-roledescription', 'draggable']) {
        preview.removeAttribute(attribute);
    }
    preview.setAttribute('aria-hidden', 'true');
    preview.setAttribute('inert', '');

    const computed = getComputedStyle(item);
    for (let i = 0; i < computed.length; i++) {
        const name = computed[i];
        if (!PREVIEW_OWN.test(name)) preview.style.setProperty(name, computed.getPropertyValue(name));
    }
    if (dragClass) preview.classList.add(...dragClass.split(/\s+/).filter(Boolean));
    const style = preview.style;
    style.position = 'fixed';
    style.left = `${rect.left}px`;
    style.top = `${rect.top}px`;
    style.width = `${rect.width}px`;
    style.height = `${rect.height}px`;
    style.margin = '0';
    style.boxSizing = 'border-box';
    style.pointerEvents = 'none';
    style.zIndex = '2147483647';
    style.transition = 'none';
    style.willChange = 'transform';
    document.body.appendChild(preview);
    return preview;
}

// The properties the preview does not copy from the item: its placement (set above), and what the drag class gives it.
const PREVIEW_OWN = /^(position|inset|top|right|bottom|left|margin.*|transform.*|translate|rotate|scale|transition.*|animation.*|opacity|box-shadow|filter|z-index|pointer-events|cursor|outline.*|will-change|visibility)$/;

/** Routes the pointer at (x, y) to the list under it that takes the drag. */
function routePointer(s: Session, x: number, y: number): void {
    s.x = x;
    s.y = y;
    s.lastOver = performance.now();
    s.onMove?.();
    const under = document.elementFromPoint(x, y);
    route(s, s.state === 'starting' || s.state === 'active' ? targetAt(s, under) : null);
    if (s.home.opts.scroll && !scrollFrame) scrollFrame = requestAnimationFrame(autoScroll);
}

// --- Auto-scroll -----------------------------------------------------------------------------------------
// A pointer drag near an edge scrolls what is there, once per frame while the pointer stays: the innermost element
// under the pointer that can still scroll that way, then the ones around it, then the page - so a long list in a
// panel scrolls first, and the page once the panel is at its end. Each step measures the pointer again, as the
// items under it moved. A native drag is left to the browser, which scrolls for it.

let scrollFrame = 0;

function autoScroll(): void {
    scrollFrame = 0;
    const s = session;
    if (!s || s.mode !== 'pointer' || (s.state !== 'active' && s.state !== 'starting')) return;
    const { scrollSensitivity, scrollSpeed } = s.home.opts;
    if (!scrollEdge(s.x, s.y, scrollSensitivity, scrollSpeed)) return;
    routePointer(s, s.x, s.y);
    scrollFrame ||= requestAnimationFrame(autoScroll);
}

/** Scrolls the innermost scroller the pointer is near an edge of, toward that edge; whether anything scrolled. */
function scrollEdge(x: number, y: number, sensitivity: number, speed: number): boolean {
    if (!(sensitivity > 0) || !(speed > 0)) return false;
    const root = document.scrollingElement ?? document.documentElement;
    for (let el = document.elementFromPoint(x, y); el; el = el.parentElement) {
        if (el === root || el === document.body) break;
        const style = getComputedStyle(el);
        const canX = /(auto|scroll|overlay)/.test(style.overflowX) && el.scrollWidth > el.clientWidth;
        const canY = /(auto|scroll|overlay)/.test(style.overflowY) && el.scrollHeight > el.clientHeight;
        if ((canX || canY) && nudge(el, el.getBoundingClientRect(), x, y, sensitivity, speed, canX, canY)) return true;
    }
    return nudge(root, new DOMRect(0, 0, window.innerWidth, window.innerHeight), x, y, sensitivity, speed, true, true);
}

/** Scrolls `el` by up to `speed` pixels toward the edge of `box` the pointer is within `sensitivity` of. */
function nudge(el: Element, box: DOMRect, x: number, y: number, sensitivity: number, speed: number, canX: boolean, canY: boolean): boolean {
    const step = (distance: number): number => distance < sensitivity ? Math.max(1, Math.round(speed * (1 - Math.max(0, distance) / sensitivity))) : 0;
    const dy = canY ? step(box.bottom - y) - step(y - box.top) : 0;
    const dx = canX ? step(box.right - x) - step(x - box.left) : 0;
    if (!dx && !dy) return false;
    const left = el.scrollLeft, top = el.scrollTop;
    el.scrollBy(dx, dy);
    return el.scrollLeft !== left || el.scrollTop !== top;
}

window.addEventListener('pointermove', (e: PointerEvent) => {
    const p = press;
    const s = session;
    if (s && s.mode === 'pointer' && s.pointerId === e.pointerId && (s.state === 'active' || s.state === 'starting')) {
        routePointer(s, e.clientX, e.clientY);
        return;
    }
    if (!p || p.pointerId !== e.pointerId) return;

    const moved = Math.hypot(e.clientX - p.x0, e.clientY - p.y0);
    if (!p.ready) {
        // Moved before the delay was over: a scroll, a swipe or a click, not a drag.
        if (moved > p.list.opts.delayTolerance) endPress();
        return;
    }
    if (moved >= DRAG_THRESHOLD) activate(p, e.clientX, e.clientY);
}, true);

window.addEventListener('pointerup', (e: PointerEvent) => {
    const s = session;
    if (s && s.mode === 'pointer' && s.pointerId === e.pointerId && s.state !== 'ended') {
        const target = s.state === 'refused' ? null : targetAt(s, document.elementFromPoint(e.clientX, e.clientY));
        s.x = e.clientX;
        s.y = e.clientY;
        if (target) route(s, target);
        // The click the browser follows a press with is not the app's: the press was a drag.
        suppressNextClick();
        void finish(s, target ? 'drop' : 'release');
        return;
    }
    if (press?.pointerId === e.pointerId) endPress();
}, true);

window.addEventListener('pointercancel', (e: PointerEvent) => {
    const s = session;
    if (s && s.mode === 'pointer' && s.pointerId === e.pointerId) void finish(s, 'cancel');
    else if (press?.pointerId === e.pointerId) endPress();
}, true);

// Leaving the page (another tab, another window) cancels a pointer drag: its pointer is gone.
function abandonPointer(): void {
    const s = session;
    if (s && s.mode === 'pointer' && s.state !== 'ended') void finish(s, 'cancel');
    else endPress();
}
window.addEventListener('blur', abandonPointer);
document.addEventListener('visibilitychange', () => { if (document.hidden) abandonPointer(); });

function suppressNextClick(): void {
    const swallow = (e: Event): void => {
        e.stopPropagation();
        e.preventDefault();
    };
    window.addEventListener('click', swallow, { capture: true, once: true });
    setTimeout(() => window.removeEventListener('click', swallow, { capture: true }), 0);
}

/** After a drag from a handle, the item waits for its handle again. */
function resetHandle(itemEl: Element): void {
    const list = listOfItem(itemEl);
    if (list?.opts.handle) itemEl.setAttribute('draggable', 'false');
}

// --- Native drag routing ---------------------------------------------------------------------------------

/** Whether a native drag event belongs to the drag in progress. */
function isSessionDrag(e: DragEvent): Session | null {
    const s = session;
    if (!s || s.state === 'ended' || s.state === 'refused') return null;
    return e.dataTransfer?.types.includes(DRAG_TYPE) ? s : null;
}

document.addEventListener('dragstart', (e: DragEvent) => {
    const item = e.target instanceof Element ? e.target.closest(`[${ITEM}]`) : null;

    // A native drag of something inside an item that is not itself draggable right now (a link or an image
    // outside the handle, an element of the app's own, or anything in a filtered item) stays a plain browser
    // drag: left unmarked, it reaches every listener as usual, and no list treats it as a sort.
    if (!(item instanceof HTMLElement) || item.getAttribute('draggable') !== 'true' || !e.dataTransfer) return;
    const list = listOfItem(item);
    const key = item.getAttribute(KEY);
    if (!list || !key) return;

    // The mark the lists recognise the drag by. Setting data also makes older browsers (Firefox) start the drag.
    e.dataTransfer.setData(DRAG_TYPE, list.opts.id);
    e.dataTransfer.effectAllowed = 'copyMove';
    begin(list, item, e.target, key, lastPointerType);
}, true);

/**
 * Lets a list's own drag be dropped on the lists that take it (the browser allows a drop only where dragenter and
 * dragover are cancelled), and hands the pointer position to the router. A drag the lists do not own keeps its
 * default, so it does not show as droppable and is dropped where the browser drops it.
 */
function onDragOver(e: DragEvent): void {
    const s = isSessionDrag(e);
    if (!s) return;
    s.lastOver = performance.now();
    s.x = e.clientX;
    s.y = e.clientY;

    const target = targetAt(s, e.target);
    if (target) {
        e.preventDefault();
        // Over a position the list refused, the pointer shows that the item cannot be dropped there; over another
        // list of a clone drag, that a copy goes there.
        if (e.dataTransfer) e.dataTransfer.dropEffect = target === s.refused ? 'none' : s.clone && target !== s.home ? 'copy' : 'move';
    }
    if (e.type === 'dragover') route(s, target);
}

document.addEventListener('dragenter', onDragOver, true);
document.addEventListener('dragover', onDragOver, true);

document.addEventListener('drop', (e: DragEvent) => {
    const s = isSessionDrag(e);
    if (!s) return;
    s.x = e.clientX;
    s.y = e.clientY;
    const target = targetAt(s, e.target);
    if (target) {
        e.preventDefault();
        route(s, target);
    }
    void finish(s, target ? 'drop' : 'release');
}, true);

document.addEventListener('dragend', (e: DragEvent) => {
    if (e.target instanceof Element) resetHandle(e.target);
    const s = session;
    if (!s || (e.target !== s.dragNode && e.target !== s.itemEl)) return;
    // Released outside every list: on another drop target of the page, or nowhere. A native drag cancelled with
    // Escape ends exactly the same way (dropEffect "none"), so both are a release: the list decides, with its
    // RevertOnSpill, whether that keeps the move.
    void finish(s, 'release');
}, true);

// Some browsers never fire dragend for an item whose element was removed during the drag. Mouse events do not fire
// while a native drag runs and dragover fires every 50 ms while it does, so a mouse move well after the last
// dragover means the drag is over and its end was lost.
document.addEventListener('mousemove', () => {
    const s = session;
    if (s && s.mode === 'native' && (s.state === 'active' || s.state === 'starting') && performance.now() - s.lastOver > 500) void finish(s, 'abandon');
}, true);

/** The `dragover` and `drop` of a list's drag stop at the list that takes them, as the app around it has no part in them. */
function stopAtTarget(e: Event): void {
    const s = session;
    if (s && s.target && s.target.el === e.currentTarget && e instanceof DragEvent && isSessionDrag(e)) e.stopPropagation();
}

// --- The session -----------------------------------------------------------------------------------------

/** What makes one drag different from another when it starts: everything else starts out the same. */
type SessionStart = Pick<Session, 'mode' | 'pointerId' | 'home' | 'itemEl' | 'dragNode' | 'key' | 'input'>;

/** Makes a new drag the one in progress, ending the one before it, whose end was lost (.NET ends it at the next start). */
function startSession(start: SessionStart): Session {
    resetHitState();
    const previous = session;
    if (previous && previous.state !== 'ended') {
        previous.state = 'ended';
        previous.cleanup?.();
        clearMarks(previous);
    }

    const s: Session = {
        ...start,
        dragId: 0, cleanup: null, onMove: null, clone: false, marked: new Set(), overEl: null,
        listId: start.home.opts.id, swapKey: null, swapListId: null,
        state: 'starting',
        accepting: new Set(),
        x: 0, y: 0,
        target: null,
        refused: null,
        touched: new Set([start.home]),
        busy: null,
        dirty: false,
        sent: null,
        started: Promise.resolve(),
        lastOver: performance.now(),
    };
    session = s;
    return s;
}

/**
 * .NET will not start the drag (the item cannot be dragged now, or a handler of the start failed): whatever the drag
 * set up in the page goes - the preview, and the press it came of, so that the next pointer move does not start it
 * all over again. It ends for good when it is released.
 */
function refuse(s: Session): void {
    s.state = 'refused';
    s.cleanup?.();
    s.cleanup = null;
}

function begin(home: ListState, itemEl: HTMLElement, dragNode: EventTarget | null, key: string, input: string,
    mode: 'native' | 'pointer' = 'native', pointerId = -1): Session {
    const s = startSession({ mode, pointerId, home, itemEl, dragNode, key, input });

    stats.starts++;
    // Started once the end of the previous drag has reached .NET - a drop just made must not be taken for a lost drag
    // - and on the next task, so the browser takes its drag image of the item as it looks before .NET dims it.
    s.started = endsInFlight
        .then(() => new Promise<void>(resolve => setTimeout(resolve, 0)))
        .then(() => home.ref.invokeMethodAsync<StartResult | null>('Start', key, input))
        .then(result => {
            if (!result) {
                if (s.state === 'starting') refuse(s);
                return;
            }
            announce(result.announce);
            s.dragId = result.drag;
            s.accepting = new Set(result.lists);
            s.clone = !!result.clone;
            if (s.state !== 'starting') return; // it ended before .NET answered
            s.state = 'active';
            // Until the answer, any list was taken for the target; one that does not take the drag is not.
            if (s.target && (!s.accepting.has(s.target.opts.id) || wouldNest(s.target.el))) s.target = null;
            markAccepting(s);
            markOver(s, s.target);
            pump(s);
        }, () => {
            if (s.state === 'starting') refuse(s);
        });
    return s;
}

/**
 * The innermost list around `node` that takes the drag. A list that does not (another group, another item type,
 * a full one) leaves it to the list around it, so the columns of a board still sort while the pointer crosses the
 * card lists inside them. Before .NET has answered the start, any list will do: the drop is allowed, and the
 * position is routed once the answer is in.
 */
function targetAt(s: Session, node: EventTarget | null): ListState | null {
    for (let el = node instanceof Element ? node : null; el; el = el.parentElement) {
        if (!el.hasAttribute(LIST)) continue;
        const list = listOf(el);
        if (!list || wouldNest(el)) continue;
        if (s.state === 'starting' || s.accepting.has(list.opts.id)) return list;
    }
    return null;
}

/** Hands a new pointer position over `target` to the router; with no target, the preview stays where it is. */
function route(s: Session, target: ListState | null): void {
    if (s.target !== target) {
        s.target = target;
        if (s.state === 'active') markOver(s, target);
    }
    if (target) pump(s);
}

// --- Drop-target state -----------------------------------------------------------------------------------
// While a drag runs, the lists that take it carry data-bortable-accepts, the one under the pointer (or, from the
// keyboard, the one the item is in) data-bortable-over, and that one data-bortable-refused when it refused the last
// position; the page carries data-bortable-dragging. They are attributes Blazor never renders, so they cost no
// render, and the stylesheet (or the app's own) can draw valid and invalid targets from them.

const ACCEPTS = 'data-bortable-accepts';
const OVER = 'data-bortable-over';
const REFUSED = 'data-bortable-refused';
const DRAGGING = 'data-bortable-dragging';

function mark(s: Session, el: HTMLElement, attribute: string, on = true): void {
    if (on) {
        el.setAttribute(attribute, '');
        s.marked.add(el);
    } else {
        el.removeAttribute(attribute);
    }
}

function markAccepting(s: Session): void {
    document.documentElement.setAttribute(DRAGGING, s.mode);
    for (const list of lists.values()) {
        if (s.accepting.has(list.opts.id)) mark(s, list.el, ACCEPTS);
    }
}

function markOver(s: Session, list: ListState | null): void {
    const el = list?.el ?? null;
    if (s.overEl && s.overEl !== el) {
        s.overEl.removeAttribute(OVER);
        s.overEl.removeAttribute(REFUSED);
    }
    s.overEl = el;
    if (list && el) {
        mark(s, el, OVER);
        mark(s, el, REFUSED, s.refused === list);
    }
}

function clearMarks(s: Session): void {
    for (const el of s.marked) {
        el.removeAttribute(ACCEPTS);
        el.removeAttribute(OVER);
        el.removeAttribute(REFUSED);
    }
    s.marked.clear();
    s.overEl = null;
    if (!session || session === s) document.documentElement.removeAttribute(DRAGGING);
}

/** Hit-tests where the pointer is now, unless a call to .NET is in flight: then once it has completed. */
function pump(s: Session): void {
    if (s.state !== 'active' && s.state !== 'ending') return;
    if (s.busy) {
        s.dirty = true;
        return;
    }
    s.dirty = false;
    s.busy = step(s).finally(() => {
        s.busy = null;
        if (s.dirty) pump(s);
    });
}

/** One hit test, and the call to .NET when it found a new position. */
async function step(s: Session): Promise<void> {
    const list = s.target;
    if (!list || !list.el.isConnected || !lists.has(list.el)) return;

    const opts = list.opts;
    const index = opts.swap
        ? itemIndexAt(list.el, s.x, s.y)
        : dropIndex(list.el, s.x, s.y, opts.swapThreshold, opts.invertSwap, opts.invertedSwapThreshold, opts.direction);
    // -1 keeps the order (a dead zone, or already there). In swap mode it means "over no item", which clears the
    // highlight, so it is sent like any other answer.
    if (index < 0 && !opts.swap) return;
    if (s.sent && s.sent.list === list && s.sent.index === index) return;
    s.sent = { list, index };

    s.touched.add(list);
    const flip = armFlip(flipLists(s));
    let result = 0;
    const sentAt = performance.now();
    try {
        // 1: a list changed, 0: nothing did, -1: the list refused the position (its CanDrop said no).
        result = await list.ref.invokeMethodAsync<number>('Over', index, s.dragId);
    } catch {
        // The list left the page (or the browser lost .NET) while the call was on its way.
    } finally {
        flip.settle(result > 0);
    }
    const took = performance.now() - sentAt;
    stats.positions++;
    stats.totalMs += took;
    stats.slowestMs = Math.max(stats.slowestMs, took);
    if (result > 0) stats.changes++;
    if (result < 0) stats.refusals++;
    s.refused = result < 0 ? list : null;
    if (s.overEl === list.el) mark(s, list.el, REFUSED, result < 0);
    // The hit test took the move for done (it remembers the item it passed); nothing moved, so it starts afresh.
    if (result <= 0 && !opts.swap) resetHitState();
}

/** The lists the next change may move: those the drag has touched, and the one the dragged item is in. */
function flipLists(s: Session): HTMLElement[] {
    const els = new Set<HTMLElement>();
    for (const list of s.touched) if (list.el.isConnected) els.add(list.el);
    const holder = s.itemEl.isConnected ? s.itemEl.parentElement : null;
    if (holder && lists.has(holder)) els.add(holder);
    return Array.from(els);
}

/**
 * Ends the drag: lets the last pointer position be acted on, then tells .NET how the drag ended. Called once per
 * drag, from whichever of drop, dragend and the lost-end check comes first.
 */
function finish(s: Session, how: EndKind): Promise<void> {
    if (s.state === 'ending' || s.state === 'ended') return Promise.resolve();
    const wasRefused = s.state === 'refused';
    s.state = 'ending';
    s.cleanup?.();
    s.cleanup = null;
    clearMarks(s);
    resetHandle(s.itemEl);
    return trackEnd(sendEnd(s, how, wasRefused));
}

// The ends on their way to .NET. The next drag starts after them: its start must not reach .NET before the end of
// the drag before it, or .NET would take that one for a drag whose end was lost.
let endsInFlight: Promise<void> = Promise.resolve();

function trackEnd(ending: Promise<void>): Promise<void> {
    const settled = ending.catch(() => { /* reported where it happened */ });
    endsInFlight = Promise.all([endsInFlight, settled]).then(() => undefined);
    return ending;
}

async function sendEnd(s: Session, how: EndKind, wasRefused: boolean): Promise<void> {
    await s.started;
    if (wasRefused || s.accepting.size === 0) {
        s.state = 'ended';
        if (session === s) session = null;
        return;
    }

    // The position the pointer was released at, if it was never acted on.
    if (how === 'drop') pump(s);
    while (s.busy) await s.busy;

    const flip = armFlip(flipLists(s));
    stats.ends++;
    try {
        announce(await invokeAny<string | null>(s.home, 'End', how, s.dragId));
    } finally {
        flip.settle(true);
        s.state = 'ended';
        if (session === s) session = null;
    }
}

/**
 * Calls the bridge of `preferred`, or, when that list has left the page, of any other list: .NET reads the drag
 * from the coordinator every list of the page shares.
 */
async function invokeAny<T>(preferred: ListState, method: string, ...args: unknown[]): Promise<T | undefined> {
    const candidates = [preferred, ...Array.from(lists.values()).filter(l => l !== preferred && l.el.isConnected)];
    for (const list of candidates) {
        try {
            return await list.ref.invokeMethodAsync<T>(method, ...args);
        } catch {
            // Disposed: try the next one.
        }
    }
    return undefined;
}

// --- Keyboard --------------------------------------------------------------------------------------------

/** A step of a keyboard drag that waits for the one in flight. */
interface KeyStep {
    action: 'to' | 'drop' | 'cancel';
    list: string | null;
    index: number;
    /** Whether the focus follows the item afterwards: not when the step was made by moving the focus elsewhere. */
    refocus?: boolean;
}

/** A registered list by its id, if it is on the page. */
function listById(id: string | null | undefined): ListState | null {
    if (!id) return null;
    for (const list of lists.values()) {
        if (list.opts.id === id && list.el.isConnected) return list;
    }
    return null;
}

/** The item of `listEl` rendered with `key`. */
function itemByKey(listEl: Element, key: string | null | undefined): HTMLElement | null {
    if (!key) return null;
    for (const child of Array.from(listEl.children)) {
        if (child.getAttribute(KEY) === key && !child.hasAttribute(PLACEHOLDER)) return child as HTMLElement;
    }
    return null;
}

/** The items of a list, in order (the drop placeholder left out), and whether each is on screen. */
function itemsOf(listEl: Element): HTMLElement[] {
    return Array.from(listEl.children).filter(c => c.hasAttribute(ITEM) && !c.hasAttribute(PLACEHOLDER)) as HTMLElement[];
}

function visible(el: Element): boolean {
    const r = el.getBoundingClientRect();
    return r.width > 0 || r.height > 0;
}

const ARROWS: Record<string, [number, number]> = {
    ArrowUp: [0, -1], ArrowDown: [0, 1], ArrowLeft: [-1, 0], ArrowRight: [1, 0],
};

/**
 * Of `candidates`, the one that lies nearest `from` in the direction `dir`: ahead of it along the arrow, and as close
 * to its line as possible. A vertical list, a horizontal one, a grid and the lists around them all answer the same
 * question, so the arrow keys go where they point, right to left included.
 */
function nearestInDirection<T>(from: DOMRect, candidates: { value: T; rect: DOMRect }[], [dx, dy]: [number, number]): T | null {
    const cx = from.left + from.width / 2, cy = from.top + from.height / 2;
    let best: T | null = null;
    let bestScore = Infinity;
    for (const { value, rect } of candidates) {
        const ox = rect.left + rect.width / 2 - cx, oy = rect.top + rect.height / 2 - cy;
        const ahead = ox * dx + oy * dy;
        if (ahead < 1) continue;
        // Off the line counts double, so the next item of the same row or column wins over a nearer one beside it.
        const aside = Math.abs(ox * dy) + Math.abs(oy * dx);
        const score = ahead + aside * 2;
        if (score < bestScore) { bestScore = score; best = value; }
    }
    return best;
}

/** The item a key was pressed on: the item wrapper itself, or the handle of the innermost item around it. */
function keyTarget(node: EventTarget | null): { item: HTMLElement; list: ListState; onItem: boolean } | null {
    if (!(node instanceof Element)) return null;
    const own = ownItemsAround(node)[0];
    if (!own) return null;
    if (own.item === node) return { ...own, onItem: true };
    const selector = own.list.opts.handle;
    const handle = selector ? closestSafe(node, selector) : null;
    return handle && handle.closest(`[${ITEM}]`) === own.item ? { ...own, onItem: false } : null;
}

document.addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

    const s = session;
    const running = !!s && (s.state === 'active' || s.state === 'starting');
    if (s && running && s.mode === 'keyboard') {
        // Only the keys pressed on the dragged item are the drag's: a dialog a ConfirmDrop opened, or a field the
        // user moved to, keeps its own.
        if (e.target instanceof Node && s.itemEl.contains(e.target)) keyDuringDrag(s, e);
        return;
    }
    if (s && running) {
        // During a pointer drag, Escape cancels it (a native drag never sees the key: the browser handles it), and no
        // other key starts anything.
        if (s.mode === 'pointer' && e.key === 'Escape') {
            e.preventDefault();
            void finish(s, 'cancel');
        }
        return;
    }

    const found = keyTarget(e.target);
    if (!found || !found.list.opts.keyboard) return;
    const { item, list, onItem } = found;

    // Shift+Space selects or deselects the item of a multi-drag list.
    if (e.key === ' ' && e.shiftKey && list.opts.multiDrag) {
        e.preventDefault();
        const key = item.getAttribute(KEY);
        if (key) {
            list.ref.invokeMethodAsync<KeyResult>('Key', key, 'select', null, -1, 0)
                .then(result => announce(result?.announce), () => { /* the list is gone */ });
        }
        return;
    }

    if (e.key === ' ' || e.key === 'Enter') {
        e.preventDefault();
        // A held key repeats: the press that dropped the item must not pick it up again at once.
        if (!e.repeat) void pickUp(list, item);
        return;
    }

    // Moving between the items of a list; a handle keeps the keys to itself.
    if (!onItem) return;
    const items = itemsOf(list.el).filter(visible);
    let next: HTMLElement | null = null;
    if (e.key in ARROWS) {
        next = nearestInDirection(item.getBoundingClientRect(),
            items.filter(i => i !== item).map(i => ({ value: i, rect: i.getBoundingClientRect() })), ARROWS[e.key]);
    } else if (e.key === 'Home' || e.key === 'End') {
        next = e.key === 'Home' ? items[0] : items[items.length - 1];
    } else {
        return;
    }
    e.preventDefault();
    if (next && next !== item) next.focus();
});

/** An item got the focus: it becomes its list's tab stop, so Tab comes back to it. */
document.addEventListener('focusin', (e: FocusEvent) => {
    const item = e.target;
    if (!(item instanceof HTMLElement) || item.getAttribute('tabindex') !== '-1') return;
    const list = listOfItem(item);
    const key = item.getAttribute(KEY);
    if (list?.opts.keyboard && key) list.ref.invokeMethodAsync('Focus', key).catch(() => { /* the list is gone */ });
});

async function pickUp(list: ListState, item: HTMLElement): Promise<void> {
    const key = item.getAttribute(KEY);
    if (!key) return;

    const s = startSession({ mode: 'keyboard', pointerId: -1, home: list, itemEl: item, dragNode: null, key, input: 'keyboard' });

    let result: KeyResult | undefined;
    try {
        // After the end of the drag before it has reached .NET (see endsInFlight).
        await endsInFlight;
        result = await list.ref.invokeMethodAsync<KeyResult>('Key', key, 'pick', null, -1, 0);
    } catch {
        // The list left the page.
    }
    if (session !== s) return;
    announce(result?.announce);
    if (!result?.dragging || s.state !== 'starting') {
        pendingSteps.delete(s);
        s.state = 'ended';
        session = null;
        return;
    }
    s.state = 'active';
    s.dragId = result.drag ?? 0;
    s.accepting = new Set(result.lists ?? []);
    markAccepting(s);
    follow(s, result, true);

    // A key pressed before .NET had answered the pick waited for its id: without it, .NET would take a drop for one
    // about no drag at all, and this drag would never end.
    const waiting = pendingSteps.get(s);
    pendingSteps.delete(s);
    if (waiting) void keyStep(s, waiting);
}

// The step a keyboard drag waits to take once the one in flight returns: the latest arrow, or the end.
const pendingSteps = new WeakMap<Session, KeyStep>();

function keyDuringDrag(s: Session, e: KeyboardEvent): void {
    let step: KeyStep | null = null;
    switch (e.key) {
        case ' ':
        case 'Enter':
            // A held key repeats: the press that picked the item up must not drop it again at once.
            if (e.repeat) {
                e.preventDefault();
                return;
            }
            step = { action: 'drop', list: null, index: -1 };
            break;
        case 'Escape':
            step = { action: 'cancel', list: null, index: -1 };
            break;
        case 'Tab':
            // The focus moves on, and is not brought back; the item stays where it is now.
            void keyStep(s, { action: 'drop', list: null, index: -1, refocus: false });
            return;
        case 'Home':
        case 'End': {
            const list = listById(s.listId);
            if (list) step = { action: 'to', list: null, index: e.key === 'Home' ? 0 : Math.max(0, itemsOf(list.el).length - 1) };
            break;
        }
        default:
            if (e.key in ARROWS) step = arrowStep(s, ARROWS[e.key]);
            else return;
    }
    e.preventDefault();
    if (step) void keyStep(s, step);
}

/**
 * Where an arrow key takes the dragged item: to the next item of its list in that direction, or - past the end of
 * the list - into the nearest list in that direction that takes the drag, beside its nearest item. In swap mode the
 * arrows walk the swap highlight instead.
 */
function arrowStep(s: Session, dir: [number, number]): KeyStep | null {
    const list = listById(s.listId);
    if (!list) return null;
    const itemEl = itemByKey(list.el, s.key);
    if (!itemEl) return null;

    // In swap mode the arrows move the highlight, from wherever it is.
    const swapList = listById(s.swapListId);
    const cursorList = list.opts.swap && swapList ? swapList : list;
    const cursor = (list.opts.swap && swapList ? itemByKey(swapList.el, s.swapKey) : null) ?? itemEl;
    const from = cursor.getBoundingClientRect();

    const items = itemsOf(cursorList.el);
    const within = nearestInDirection(from,
        items.filter(i => i !== itemEl && visible(i)).map(i => ({ value: i, rect: i.getBoundingClientRect() })), dir);
    if (within) return { action: 'to', list: cursorList.opts.id, index: items.indexOf(within) };

    // Past the end of the list: the nearest list beyond it that takes the drag (never one inside the item itself).
    const others: { value: ListState; rect: DOMRect }[] = [];
    for (const other of lists.values()) {
        if (other === cursorList || !other.el.isConnected || !s.accepting.has(other.opts.id)) continue;
        if (itemEl.contains(other.el) || other.el.closest(`[${PLACEHOLDER}]`)) continue;
        others.push({ value: other, rect: other.el.getBoundingClientRect() });
    }
    const next = nearestInDirection(cursorList.el.getBoundingClientRect(), others, dir);
    if (!next) return null;

    const theirs = itemsOf(next.el).filter(visible);
    if (next.opts.swap) {
        // A swap list: the item nearest to where the highlight was is the one to trade with.
        const partner = nearestItem(theirs, from);
        return partner ? { action: 'to', list: next.opts.id, index: itemsOf(next.el).indexOf(partner) } : null;
    }
    return { action: 'to', list: next.opts.id, index: insertionIndex(next, theirs, from) };
}

/** Of `items`, the one nearest to the centre of `from`. */
function nearestItem(items: HTMLElement[], from: DOMRect): HTMLElement | null {
    const cx = from.left + from.width / 2, cy = from.top + from.height / 2;
    let nearest: HTMLElement | null = null;
    let best = Infinity;
    for (const item of items) {
        const d = distanceSquared(item.getBoundingClientRect(), cx, cy);
        if (d < best) { best = d; nearest = item; }
    }
    return nearest;
}

/** Where an item at `from` would enter `list`: beside the item nearest to it, on the side it is on. */
function insertionIndex(list: ListState, items: HTMLElement[], from: DOMRect): number {
    const nearest = nearestItem(items, from);
    if (!nearest) return 0;
    const cx = from.left + from.width / 2, cy = from.top + from.height / 2;
    const r = nearest.getBoundingClientRect();
    const entries = items.map((el, index) => ({ el, index, rect: el.getBoundingClientRect() }));
    const vertical = isVertical(list.el, list.opts.direction, entries, null);
    const reversed = isReversed(list.el, vertical, entries);
    const after = vertical ? cy > r.top + r.height / 2 : cx > r.left + r.width / 2;
    return itemsOf(list.el).indexOf(nearest) + (after !== reversed ? 1 : 0);
}

/** Takes one step of a keyboard drag, or, while one is in flight, keeps it for when that one returns. */
async function keyStep(s: Session, step: KeyStep): Promise<void> {
    if (s.busy || s.state === 'starting') {
        // The latest arrow replaces a waiting one; a drop or a cancel is never replaced by an arrow. Until the pick has
        // been answered (and the drag has its id) every step waits, as it would behind any other step in flight.
        const waiting = pendingSteps.get(s);
        if (!waiting || waiting.action === 'to' || step.action !== 'to') pendingSteps.set(s, step);
        return;
    }

    const list = listById(s.listId) ?? s.home;
    const target = listById(step.list) ?? list;
    const flip = armFlip(Array.from(new Set([list.el, target.el, s.home.el].filter(el => el.isConnected))));
    stats.keys++;
    s.busy = (async () => {
        let result: KeyResult | undefined;
        try {
            result = await list.ref.invokeMethodAsync<KeyResult>('Key', s.key, step.action, step.list, step.index, s.dragId);
        } catch {
            // The list left the page.
        } finally {
            flip.settle(true);
        }
        if (!result) {
            if (step.action !== 'to') end(s);
            return;
        }
        announce(result.announce);
        follow(s, result, step.refocus !== false);
        if (!result.dragging) end(s);
    })();
    // A drop or a cancel is an end on its way to .NET, which the next drag waits for.
    if (step.action !== 'to') void trackEnd(s.busy);

    try {
        await s.busy;
    } finally {
        s.busy = null;
    }

    const next = pendingSteps.get(s);
    pendingSteps.delete(s);
    if (next && s.state === 'active') await keyStep(s, next);
}

function end(s: Session): void {
    s.state = 'ended';
    clearMarks(s);
    if (session === s) session = null;
}

/** Keeps track of the dragged item after a step, and gives it the focus wherever it went (unless `refocus` is off). */
function follow(s: Session, result: KeyResult, refocus: boolean): void {
    if (result.list && result.key) {
        s.listId = result.list;
        s.key = result.key;
    }
    s.swapKey = result.target ?? null;
    s.swapListId = result.target ? (findListOfKey(result.target) ?? null) : null;

    const list = listById(s.listId);
    const item = list ? itemByKey(list.el, s.key) : null;
    if (item) {
        s.itemEl = item;
        if (refocus && document.activeElement !== item) item.focus();
    }
    if (s.state === 'active') markOver(s, list);
}

/** The id of the list that renders an item with `key` (keys are unique across the lists of a page). */
function findListOfKey(key: string): string | null {
    for (const list of lists.values()) {
        if (list.el.isConnected && itemByKey(list.el, key)) return list.opts.id;
    }
    return null;
}

// --- Announcements ---------------------------------------------------------------------------------------

let liveRegion: HTMLElement | null = null;
let announcement = 0;

/**
 * The visually hidden live region announcements go through, added to the page once - as soon as a list registers, so
 * it is already there, empty, when the first announcement fills it: screen readers read the changes of a live region,
 * and miss the text of one that appears with it.
 */
function liveRegionOf(): HTMLElement {
    if (liveRegion?.isConnected) return liveRegion;
    liveRegion = document.createElement('div');
    liveRegion.setAttribute('aria-live', 'assertive');
    liveRegion.setAttribute('aria-atomic', 'true');
    liveRegion.setAttribute('data-bortable-live', '');
    const style = liveRegion.style;
    style.position = 'absolute';
    style.width = '1px';
    style.height = '1px';
    style.margin = '-1px';
    style.padding = '0';
    style.border = '0';
    style.overflow = 'hidden';
    style.clip = 'rect(0 0 0 0)';
    style.whiteSpace = 'nowrap';
    document.body.appendChild(liveRegion);
    return liveRegion;
}

/** Reads `text` out to screen readers. Also called by .NET, for a move made by code. */
export function announce(text: string | null | undefined): void {
    if (!text) return;
    const region = liveRegionOf();
    const turn = ++announcement;
    if (region.textContent !== text) {
        region.textContent = text;
        return;
    }
    // The same words again are not a change: empty the region, and say them again a moment later.
    region.textContent = '';
    setTimeout(() => {
        if (turn === announcement) region.textContent = text;
    }, 50);
}

// --- Drop position ---------------------------------------------------------------------------------------

// The SortableJS swap state of the drag in progress. Reset at every drag start.
let lastTarget: Element | null = null;
let lastDirection: SwapDir = 0;
let targetBeforeFirstSwap = 0;
let pastFirstInvertThresh = false;
let isCircumstantialInvert = false;
let ignoreWhileAnimating: Element | null = null;

function resetHitState(): void {
    lastTarget = null;
    lastDirection = 0;
    targetBeforeFirstSwap = 0;
    pastFirstInvertThresh = false;
    isCircumstantialInvert = false;
    ignoreWhileAnimating = null;
}

/**
 * True when the given list must not receive the dragged item: it lives inside the element being dragged
 * (dropping there would nest an item inside itself and detach its subtree), or inside the drop placeholder
 * (a preview copy of the item).
 */
export function wouldNest(listEl: Element | null): boolean {
    if (!listEl) return false;
    if (listEl.closest(`[${PLACEHOLDER}]`)) return true;
    const dragged = session?.itemEl;
    return !!(dragged && dragged !== listEl && dragged.contains(listEl));
}

/**
 * Given a pointer position, returns the index (0..count, counting the list's items but not its placeholder)
 * at which the dragged item should be inserted, or -1 when the current position should be kept.
 *
 * This is a port of SortableJS's swap logic (_getSwapDirection):
 *   - swapThreshold (0..1): the centred fraction of an item that is an active swap zone. Entering it moves
 *     the dragged item past that item.
 *   - invertSwap + invertedSwapThreshold: the active zones sit at the item's edges instead, and the pointer's
 *     side of the item decides before/after ("sort between items"). This mode is always used when the item
 *     enters a list it is not in yet.
 *   - after a swap, the area the dragged item just vacated (its "shadow") swaps it back, which keeps items of
 *     different sizes from oscillating.
 *
 * The target item is found by 2D hit testing, so grids work in every direction. When the pointer is not over
 * any item (gaps, trailing padding), it clamps to the nearest slot so the end of the list stays reachable.
 * A list laid out against the axis (right to left, or with a reversed flex direction) is measured in its own
 * order, so "after" is always the next item in the list.
 */
export function dropIndex(listEl: Element | null, x: number, y: number, swapThreshold: number,
    invertSwap: boolean, invertedSwapThreshold: number, direction: Direction): number {
    if (!listEl) return -1;

    swapThreshold = swapThreshold > 0 && swapThreshold <= 1 ? swapThreshold : 1;
    invertedSwapThreshold = invertedSwapThreshold > 0 && invertedSwapThreshold <= 1 ? invertedSwapThreshold : swapThreshold;

    const dragged = session?.itemEl ?? null;

    // Visible items in DOM order, with their index among all of the list's items (hidden ones included) so
    // the result maps straight onto the bound list.
    const entries: ItemEntry[] = [];
    let count = 0;
    let slot: Slot | null = null; // where the dragged item currently sits in this list, if it does
    for (const child of Array.from(listEl.children)) {
        if (!child.hasAttribute(ITEM)) continue;
        if (child.hasAttribute(PLACEHOLDER)) {
            slot = { el: child, gap: count, rect: child.getBoundingClientRect() };
            continue;
        }
        const index = count++;
        const rect = child.getBoundingClientRect();
        if (rect.width === 0 && rect.height === 0) continue; // hidden (the original of a cross-list preview)
        const entry: ItemEntry = { el: child, index, rect };
        if (child === dragged) slot = entry;
        entries.push(entry);
    }

    if (entries.length === 0) return count;

    const vertical = isVertical(listEl, direction, entries, slot);
    const reversed = isReversed(listEl, vertical, entries);
    const over = (r: DOMRect): boolean => x >= r.left && x <= r.right && y >= r.top && y <= r.bottom;

    // Over the dragged item (or its placeholder) itself: nothing to do.
    if (slot && over(slot.rect)) {
        if (!animations.has(slot.el)) lastTarget = null;
        return -1;
    }

    // Positions along the axis, in the list's own order: negated for a reversed list, so that a larger value is
    // always further down the list.
    const along = (v: number): number => reversed ? -v : v;
    const pointer = along(vertical ? y : x);
    const span = (r: DOMRect): [number, number] => {
        const a = along(vertical ? r.top : r.left);
        const b = along(vertical ? r.bottom : r.right);
        return a <= b ? [a, b] : [b, a];
    };

    const target = entries.find(e => e !== slot && over(e.rect));
    if (!target) {
        lastTarget = null;
        return nearestSlot(entries, slot, x, y, pointer, span);
    }

    const anim = animations.get(target.el);
    if (anim && ((anim.dx !== 0 && anim.dy !== 0) || target.el === ignoreWhileAnimating)) return -1;

    const differentLevel = !slot;
    const differentRowCol = !!slot && !inRowColumn(layoutBox(slot.el), layoutBox(target.el), vertical);

    if (lastTarget !== target.el) {
        targetBeforeFirstSwap = layoutStart(target.el, vertical);
        pastFirstInvertThresh = false;
        isCircumstantialInvert = (!differentRowCol && invertSwap) || differentLevel;
    }

    const isLastTarget = lastTarget === target.el;
    const targetMoveDistance = isLastTarget ? Math.abs(targetBeforeFirstSwap - layoutStart(target.el, vertical)) : 0;
    const current = slot;
    const insertDirection = (): SwapDir => {
        // Only reached with a slot: without one (a list the item is not in yet) the inverted zones decide.
        if (!current) return 1;
        const before = isPlaceholder(current) ? current.gap <= target.index : current.index < target.index;
        return before ? 1 : -1;
    };

    const [s1, s2] = span(target.rect);
    const dir = swapDirection(pointer, s1, s2,
        differentRowCol ? 1 : swapThreshold, invertedSwapThreshold,
        isCircumstantialInvert, isLastTarget, targetMoveDistance, insertDirection);
    if (dir === 0) return -1;

    // Already beside the target on that side: nothing to do.
    if (slot) {
        if (isPlaceholder(slot)) {
            if (slot.gap === (dir === 1 ? target.index + 1 : target.index)) return -1;
        } else {
            const at = entries.indexOf(slot);
            if (entries[at - dir] === target) return -1;
        }
    }

    lastTarget = target.el;
    lastDirection = dir;
    ignoreWhileAnimating = target.el;
    return dir === 1 ? target.index + 1 : target.index;
}

function isPlaceholder(slot: Slot): slot is PlaceholderEntry {
    return (slot as PlaceholderEntry).gap !== undefined;
}

/**
 * SortableJS's _getSwapDirection: 1 = after the target, -1 = before it, 0 = no move. `mouse`, `s1` and `s2` are
 * positions along the axis in the list's own order (s1 the start of the target, s2 its end).
 */
function swapDirection(mouse: number, s1: number, s2: number, swapThreshold: number,
    invertedSwapThreshold: number, invertSwap: boolean, isLastTarget: boolean, targetMoveDistance: number,
    insertDirection: () => SwapDir): SwapDir {
    const length = s2 - s1;
    let invert = false;

    if (!invertSwap) {
        if (isLastTarget && targetMoveDistance < length * swapThreshold) {
            // Past the first inverted threshold on the side opposite the last move?
            if (!pastFirstInvertThresh &&
                (lastDirection === 1
                    ? mouse > s1 + length * invertedSwapThreshold / 2
                    : mouse < s2 - length * invertedSwapThreshold / 2)) {
                pastFirstInvertThresh = true;
            }

            if (!pastFirstInvertThresh) {
                // Over the dragged item's shadow (where it was before the last move): move back.
                if (lastDirection === 1 ? mouse < s1 + targetMoveDistance : mouse > s2 - targetMoveDistance) {
                    return -lastDirection as SwapDir;
                }
            } else {
                invert = true;
            }
        } else if (mouse > s1 + length * (1 - swapThreshold) / 2 &&
                   mouse < s2 - length * (1 - swapThreshold) / 2) {
            return insertDirection();
        }
    }

    if (invert || invertSwap) {
        if (mouse < s1 + length * invertedSwapThreshold / 2 ||
            mouse > s2 - length * invertedSwapThreshold / 2) {
            return mouse > s1 + length / 2 ? 1 : -1;
        }
    }

    return 0;
}

/** Not over any item: insert next to the nearest one, on the pointer's side. */
function nearestSlot(entries: ItemEntry[], slot: Slot | null, x: number, y: number, pointer: number,
    span: (r: DOMRect) => [number, number]): number {
    let best = Infinity;
    let nearest: ItemEntry = entries[0];
    for (const e of entries) {
        const d = distanceSquared(e.rect, x, y);
        if (d < best) { best = d; nearest = e; }
    }
    if (slot && isPlaceholder(slot)) {
        if (distanceSquared(slot.rect, x, y) < best) return -1; // nearest to the placeholder: keep it
    }
    if (nearest === slot) return -1;
    const [s1, s2] = span(nearest.rect);
    return pointer < (s1 + s2) / 2 ? nearest.index : nearest.index + 1;
}

function distanceSquared(r: DOMRect, x: number, y: number): number {
    const dx = x - Math.max(r.left, Math.min(x, r.right));
    const dy = y - Math.max(r.top, Math.min(y, r.bottom));
    return dx * dx + dy * dy;
}

function isVertical(listEl: Element, direction: Direction, entries: ItemEntry[], slot: Slot | null): boolean {
    if (direction === 'horizontal') return false;
    if (direction === 'vertical') return true;

    // Two boxes side by side make a row. The placeholder counts: it stands where the dragged item will.
    const second = entries.length > 1 ? entries[1].rect : slot && isPlaceholder(slot) ? slot.rect : null;
    if (second) {
        const first = entries[0].rect;
        return !(Math.abs(second.left - first.left) > Math.abs(second.top - first.top));
    }

    // A single box has nothing to line up with: read the layout instead, like SortableJS's _detectDirection.
    // The container's own (a flex row, a grid of several columns), else the item's (one that flows inline or
    // floats sits in a row).
    const list = getComputedStyle(listEl);
    if (list.display.endsWith('flex')) return list.flexDirection.startsWith('column');
    if (list.display.endsWith('grid')) return list.gridTemplateColumns.split(' ').length <= 1;
    const item = getComputedStyle(entries[0].el);
    return !item.display.startsWith('inline') && item.cssFloat === 'none';
}

/**
 * Whether the list runs against the axis: a row laid out right to left (an RTL page, or `row-reverse`), or a
 * `column-reverse`. Two items of the same row or column show it directly; a single one leaves it to the styles.
 */
function isReversed(listEl: Element, vertical: boolean, entries: ItemEntry[]): boolean {
    if (entries.length > 1) {
        const a = entries[0].rect, b = entries[1].rect;
        const sameLine = vertical
            ? b.left < a.right && b.right > a.left
            : b.top < a.bottom && b.bottom > a.top;
        if (sameLine) {
            const d = vertical ? b.top - a.top : b.left - a.left;
            if (Math.abs(d) >= 1) return d < 0;
        }
    }

    const style = getComputedStyle(listEl);
    if (style.display.endsWith('flex')) {
        const flexReversed = style.flexDirection.endsWith('reverse');
        return vertical ? flexReversed : flexReversed !== (style.direction === 'rtl');
    }
    return !vertical && style.direction === 'rtl';
}

/** Layout position and size (ignoring the transforms used by the animations). */
function layoutBox(el: Element): LayoutBox {
    const h = el as HTMLElement;
    return { left: h.offsetLeft, top: h.offsetTop, width: h.offsetWidth, height: h.offsetHeight };
}

function layoutStart(el: Element, vertical: boolean): number {
    const h = el as HTMLElement;
    return vertical ? h.offsetTop : h.offsetLeft;
}

/** SortableJS's _dragElInRowColumn: do the two boxes share a row (horizontal) or a column (vertical)? */
function inRowColumn(a: LayoutBox, b: LayoutBox, vertical: boolean): boolean {
    const near = (p: number, q: number): boolean => Math.abs(p - q) < 1;
    const a1 = vertical ? a.left : a.top, aLen = vertical ? a.width : a.height;
    const b1 = vertical ? b.left : b.top, bLen = vertical ? b.width : b.height;
    return near(a1, b1) || near(a1 + aLen, b1 + bLen) || near(a1 + aLen / 2, b1 + bLen / 2);
}

/**
 * Index (counting all of the list's items, hidden ones included) of the visible item whose box is directly
 * under the pointer, or -1 when the pointer is over no item. Used by swap mode to pick the item the dragged
 * one will trade places with.
 */
export function itemIndexAt(listEl: Element | null, x: number, y: number): number {
    if (!listEl) return -1;
    let i = 0;
    for (const child of Array.from(listEl.children)) {
        if (!child.hasAttribute(ITEM) || child.hasAttribute(PLACEHOLDER)) continue;
        const index = i++;
        const r = child.getBoundingClientRect();
        if (r.width === 0 && r.height === 0) continue;
        if (x >= r.left && x <= r.right && y >= r.top && y <= r.bottom) return index;
    }
    return -1;
}

// --- FLIP animation --------------------------------------------------------------------------------------

// listEl -> Map(item element -> DOMRect) captured just before a change.
const flipStore = new WeakMap<Element, Map<Element, DOMRect>>();

// item element -> the FLIP animation currently running on it.
const animations = new WeakMap<Element, FlipToken>();

const reducedMotion = typeof matchMedia === 'function' ? matchMedia('(prefers-reduced-motion: reduce)') : null;

/** The slide duration of a list: none when the user asked the system for reduced motion. */
function durationOf(listEl: Element): number {
    if (reducedMotion?.matches) return 0;
    return listOf(listEl)?.opts.animation ?? 0;
}

/** A set of snapshots taken before a call to .NET that may change the lists, played once the change arrives. */
interface ArmedFlip {
    settle(changed: boolean): void;
}

/**
 * Snapshots the lists, then plays each one's animation as soon as Blazor changes it: the mutation observer runs
 * right after the render batch is applied, before the browser paints, so the items never show in their new
 * places un-inverted. Whatever has not changed by the time the call returns is played (or dropped) then.
 */
function armFlip(listEls: HTMLElement[]): ArmedFlip {
    const pending = new Map<HTMLElement, MutationObserver>();
    for (const el of listEls) {
        const duration = durationOf(el);
        if (!(duration > 0)) continue;
        capture(el);
        const observer = new MutationObserver(records => {
            // Only the list's own children (added, removed, moved, or re-classed), not the lists nested in them.
            if (!records.some(r => r.target === el || (r.target as Node).parentNode === el)) return;
            observer.disconnect();
            pending.delete(el);
            play(el, duration);
        });
        observer.observe(el, { childList: true, subtree: true, attributes: true, attributeFilter: ['class'] });
        pending.set(el, observer);
    }

    return {
        settle(changed: boolean): void {
            for (const [el, observer] of pending) {
                observer.disconnect();
                if (changed) play(el, durationOf(el));
                else clear(el);
            }
            pending.clear();
        }
    };
}

/**
 * Snapshots the current on-screen position of every item. getBoundingClientRect includes any in-flight
 * transform, so animations chain smoothly when the user keeps dragging.
 */
export function capture(listEl: Element | null): void {
    if (!listEl) return;
    const map = new Map<Element, DOMRect>();
    for (const child of Array.from(listEl.children)) {
        if (!child.hasAttribute(ITEM)) continue;
        map.set(child, child.getBoundingClientRect());
    }
    flipStore.set(listEl, map);
}

/**
 * After Blazor has reordered the DOM, moves each item back to where it was (Invert), then transitions the
 * offset away to zero (Play).
 */
export function play(listEl: Element | null, duration: number): void {
    if (!listEl) return;
    const prev = flipStore.get(listEl);
    flipStore.delete(listEl);
    if (reducedMotion?.matches) duration = 0;
    if (!prev || !(duration > 0)) return;

    const items: HTMLElement[] = [];
    for (const child of Array.from(listEl.children)) {
        if (child.hasAttribute(ITEM) && prev.has(child)) items.push(child as HTMLElement); // new items are not animated
    }

    // Stop any animation still running so the measurement below sees pure layout. Its starting point was
    // captured mid-flight, so the new animation picks up exactly where the old one was.
    const interrupted: HTMLElement[] = [];
    for (const child of items) {
        if (!animations.has(child)) continue;
        animations.delete(child);
        child.style.transition = 'none';
        child.style.transform = '';
        interrupted.push(child);
    }

    const moves: { child: HTMLElement; dx: number; dy: number }[] = [];
    for (const child of items) {
        const first = prev.get(child)!;
        const last = child.getBoundingClientRect();
        // Skip items hidden before or after the change (the original item while it is previewed in another
        // list): animating those makes them fly in from a corner.
        if ((first.width === 0 && first.height === 0) || (last.width === 0 && last.height === 0)) continue;
        const dx = first.left - last.left;
        const dy = first.top - last.top;
        if (Math.abs(dx) < 0.5 && Math.abs(dy) < 0.5) continue;
        moves.push({ child, dx, dy });
    }

    for (const child of interrupted) {
        if (!moves.some(m => m.child === child)) child.style.transition = '';
    }
    if (moves.length === 0) return;

    for (const { child, dx, dy } of moves) {
        child.style.transition = 'none';
        child.style.transform = `translate(${dx}px, ${dy}px)`;
    }

    // Force a reflow so the inverted transforms are applied before the animation starts.
    void (listEl as HTMLElement).offsetWidth;

    for (const { child, dx, dy } of moves) {
        const token: FlipToken = { dx, dy };
        animations.set(child, token);
        child.style.transition = `transform ${duration}ms ${EASING}`;
        child.style.transform = '';

        let timer = 0;
        const done = (e?: Event): void => {
            // transitionend bubbles: ignore the ones from nested items.
            if (e && (e.target !== child || (e as TransitionEvent).propertyName !== 'transform')) return;
            child.removeEventListener('transitionend', done);
            child.removeEventListener('transitioncancel', done);
            clearTimeout(timer);
            if (animations.get(child) !== token) return; // superseded by a newer animation
            animations.delete(child);
            child.style.transition = '';
        };
        child.addEventListener('transitionend', done);
        child.addEventListener('transitioncancel', done);
        // Fallback for transitions that never report back (the element was removed, for one).
        timer = window.setTimeout(done, duration + 100);
    }
}

/** Forgets a snapshot that will not be played. */
export function clear(listEl: Element | null): void {
    if (listEl) flipStore.delete(listEl);
}
