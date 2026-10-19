/*!
 * Bit.Bortable browser layer.
 *
 * The sorting engine lives in C# and runs on the browser's native drag events. This module only does what
 * needs the DOM:
 *   1. drag handles (an item is draggable only while its handle element is pressed),
 *   2. drop-position hit testing, using the SortableJS swap-threshold model,
 *   3. FLIP animations, so items glide to their new positions instead of snapping.
 */

// The attributes Bortable.razor renders on its lists, its item wrappers and the drop placeholder.
const LIST = 'data-bortable-list';
const ITEM = 'data-bortable-item';
const PLACEHOLDER = 'data-bortable-placeholder';
const NO_DRAG = 'data-bortable-disabled';
const EASING = 'cubic-bezier(0.22, 1, 0.36, 1)';

// The data type that marks a drag as a list's own: Bortable.razor starts a drag only for a dragstart carrying it.
const DRAG_TYPE = 'application/x-bit-bortable';

/** The direction values BortableDirection is sent as. */
type Direction = 'auto' | 'horizontal' | 'vertical';

/** 1 = insert after the target, -1 = insert before it, 0 = do not move. */
type SwapDir = -1 | 0 | 1;

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

// listEl -> Map(item element -> DOMRect) captured just before a reorder.
const flipStore = new WeakMap<Element, Map<Element, DOMRect>>();

// item element -> the FLIP animation currently running on it.
const animations = new WeakMap<Element, FlipToken>();

// listEl -> pointerdown listener installed for its drag handle.
const handleStore = new WeakMap<Element, (e: PointerEvent) => void>();

// --- Drag tracking ---------------------------------------------------------------------------------------

// The item element currently being dragged, plus the SortableJS swap state. ES modules are evaluated once
// (the import is cached), so the document listener below is installed a single time no matter how many lists
// are on the page.
let draggedItemEl: Element | null = null;
let lastTarget: Element | null = null;
let lastDirection: SwapDir = 0;
let targetBeforeFirstSwap = 0;
let pastFirstInvertThresh = false;
let isCircumstantialInvert = false;
let ignoreWhileAnimating: Element | null = null;

function resetDragState(): void {
    draggedItemEl = null;
    lastTarget = null;
    lastDirection = 0;
    targetBeforeFirstSwap = 0;
    pastFirstInvertThresh = false;
    isCircumstantialInvert = false;
    ignoreWhileAnimating = null;
}

document.addEventListener('dragstart', (e: DragEvent) => {
    resetDragState();
    const item = e.target instanceof Element ? e.target.closest(`[${ITEM}]`) : null;

    // A native drag of something inside an item that is not itself draggable right now (a link or an image
    // outside the handle, an element of the app's own, or anything in a filtered item) stays a plain browser
    // drag: left unmarked, it reaches every listener as usual, and the list does not treat it as a sort.
    if (!item || item.getAttribute('draggable') !== 'true' || !e.dataTransfer) return;

    draggedItemEl = item;
    // The mark the list starts the drag on. Setting data also makes older browsers (Firefox) start the drag.
    e.dataTransfer.setData(DRAG_TYPE, item.parentElement?.id || 'item');
    e.dataTransfer.effectAllowed = 'move';
}, true);

// The state is deliberately kept after dragend and only reset by the next dragstart: with Blazor Server, the
// last dragover events of a drag can reach .NET (and call back into this module) after the browser has
// already fired dragend.

/**
 * Lets a list's own drag be dropped on any list (the browser allows a drop only where dragenter and dragover
 * are cancelled): which list takes it is for .NET to decide. Done here, at the event, rather than with the
 * lists' Blazor preventDefault flags, which are rendered ahead of it and cannot tell one drag from another: a
 * drag the lists do not own (a file, selected text) keeps its default, so it does not show as droppable and is
 * dropped where the browser drops it, such as into an input inside an item.
 */
function allowListDrop(e: DragEvent): void {
    if (!e.dataTransfer?.types.includes(DRAG_TYPE)) return;
    if (e.target instanceof Element && e.target.closest(`[${LIST}]`)) e.preventDefault();
}

document.addEventListener('dragenter', allowListDrop, true);
document.addEventListener('dragover', allowListDrop, true);
document.addEventListener('drop', allowListDrop, true);

/**
 * True when the given list must not receive the dragged item: it lives inside the element being dragged
 * (dropping there would nest an item inside itself and detach its subtree), or inside the drop placeholder
 * (a preview copy of the item).
 */
export function wouldNest(listEl: Element | null): boolean {
    if (!listEl) return false;
    if (listEl.closest(`[${PLACEHOLDER}]`)) return true;
    return !!(draggedItemEl && draggedItemEl !== listEl && draggedItemEl.contains(listEl));
}

// --- Handles ---------------------------------------------------------------------------------------------

/**
 * Installs (or replaces, or with a null selector removes) the drag handle of a list. Items render as
 * non-draggable; pressing a handle makes its item draggable.
 */
export function setHandle(listEl: Element | null, handleSelector: string | null): void {
    if (!listEl) return;

    const previous = handleStore.get(listEl);
    if (previous) {
        listEl.removeEventListener('pointerdown', previous as EventListener, true);
        listEl.removeEventListener('dragend', resetHandle, true);
        handleStore.delete(listEl);
    }
    if (!handleSelector) return;

    const onPointerDown = (e: PointerEvent): void => {
        const target = e.target instanceof Element ? e.target : null;
        const item = ownItem(listEl, target);
        if (!item || !target) return;
        const handle = target.closest(handleSelector);
        const onHandle = !!handle && handle.closest(`[${ITEM}]`) === item && !item.hasAttribute(NO_DRAG);
        item.setAttribute('draggable', onHandle ? 'true' : 'false');
    };

    handleStore.set(listEl, onPointerDown);
    listEl.addEventListener('pointerdown', onPointerDown as EventListener, true);
    listEl.addEventListener('dragend', resetHandle, true);
}

function resetHandle(e: Event): void {
    const item = e.target instanceof Element ? e.target.closest(`[${ITEM}]`) : null;
    if (item) item.setAttribute('draggable', 'false');
}

/** The item of `listEl` (a direct child) that contains `node`, ignoring the items of lists nested inside it. */
function ownItem(listEl: Element, node: Element | null): Element | null {
    let el: Element | null = node;
    while (el && el !== listEl) {
        if (el.parentElement === listEl) return el.hasAttribute(ITEM) && !el.hasAttribute(PLACEHOLDER) ? el : null;
        el = el.parentElement;
    }
    return null;
}

// --- Drop position ---------------------------------------------------------------------------------------

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
 */
export function dropIndex(listEl: Element | null, x: number, y: number, swapThreshold: number,
    invertSwap: boolean, invertedSwapThreshold: number, direction: Direction): number {
    if (!listEl) return -1;

    swapThreshold = swapThreshold > 0 && swapThreshold <= 1 ? swapThreshold : 1;
    invertedSwapThreshold = invertedSwapThreshold > 0 && invertedSwapThreshold <= 1 ? invertedSwapThreshold : swapThreshold;

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
        if (child === draggedItemEl) slot = entry;
        entries.push(entry);
    }

    if (entries.length === 0) return count;

    const vertical = isVertical(listEl, direction, entries, slot);
    const over = (r: DOMRect): boolean => x >= r.left && x <= r.right && y >= r.top && y <= r.bottom;

    // Over the dragged item (or its placeholder) itself: nothing to do.
    if (slot && over(slot.rect)) {
        if (!animations.has(slot.el)) lastTarget = null;
        return -1;
    }

    const target = entries.find(e => e !== slot && over(e.rect));
    if (!target) {
        lastTarget = null;
        return nearestSlot(entries, slot, x, y, vertical);
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

    const dir = swapDirection(vertical ? y : x, target.rect, vertical,
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

/** SortableJS's _getSwapDirection: 1 = after the target, -1 = before it, 0 = no move. */
function swapDirection(mouse: number, rect: DOMRect, vertical: boolean, swapThreshold: number,
    invertedSwapThreshold: number, invertSwap: boolean, isLastTarget: boolean, targetMoveDistance: number,
    insertDirection: () => SwapDir): SwapDir {
    const length = vertical ? rect.height : rect.width;
    const s1 = vertical ? rect.top : rect.left;
    const s2 = vertical ? rect.bottom : rect.right;
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
function nearestSlot(entries: ItemEntry[], slot: Slot | null, x: number, y: number, vertical: boolean): number {
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
    const r = nearest.rect;
    const p = vertical ? y : x;
    const center = vertical ? r.top + r.height / 2 : r.left + r.width / 2;
    return p < center ? nearest.index : nearest.index + 1;
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

/** Forgets a snapshot that will not be played (called when a drag ends). */
export function clear(listEl: Element | null): void {
    if (listEl) flipStore.delete(listEl);
}
