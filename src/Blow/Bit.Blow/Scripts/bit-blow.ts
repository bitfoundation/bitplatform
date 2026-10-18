/*!
 * Bit.Blow browser layer.
 *
 * Kept intentionally small: Blazor handles pointer and wheel events natively, so this module only
 * does what C# cannot do on its own - measure DOM rectangles, watch them for size changes, find the
 * handle under a point, hold pointer capture, and cancel the browser's own handling of an event
 * while it is still being dispatched.
 */

// ---------------------------------------------------------------------------
// Contracts shared with the C# interop (camelCase JSON of the Blow* records)
// ---------------------------------------------------------------------------

interface DotNetObjectReference {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

// BlowDomRect
interface BlowDomRect { left: number; top: number; width: number; height: number; }

// BlowMeasuredHandle
interface BlowMeasuredHandle {
    handleId: string;
    type: string;
    position: string;
    offsetX: number;
    offsetY: number;
    connectableEnd: boolean;
}

// BlowMeasuredNode
interface BlowMeasuredNode {
    id: string | null;
    width: number;
    height: number;
    handles: BlowMeasuredHandle[];
}

// BlowHandleHit
interface BlowHandleHit {
    nodeId: string | null;
    handleId: string;
    handleType: string;
    position: string;
    connectableEnd: boolean;
}

// BlowInputPolicy
interface BlowInputPolicy {
    preventScrolling?: boolean;
    panOnScroll?: boolean;
    zoomOnScroll?: boolean;
    zoomKey?: 'ctrlKey' | 'metaKey' | 'shiftKey' | 'altKey' | null;
    panKey?: string | null;
    keyboardA11y?: boolean;
    selectAllKey?: boolean;
    clipboardKeys?: boolean;
    historyKeys?: boolean;
}

// What observeResize and installInteractionGuards hand back; .NET disposes it as a JS object reference.
interface BlowDisposable {
    dispose(): void;
}

const ROOT_CLASS = 'bit-blow';
const VIEWPORT_CLASS = 'bit-blow-viewport';
const NODE_CLASS = 'bit-blow-node';
const HANDLE_CLASS = 'bit-blow-handle';

export function getRect(el: Element | null): BlowDomRect | null {
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { left: r.left, top: r.top, width: r.width, height: r.height };
}

// The scale the viewport element is actually drawn at on screen. Measured rather than taken from
// .NET: the zoom .NET knows can be a step ahead of the transform the browser has painted (a
// re-render mid-zoom), and an ancestor's CSS transform scales the flow too. The viewport is laid
// out at the pane's size and only scaled by its transform, so its on-screen width over its layout
// width is exactly that scale.
function screenScale(viewportEl: HTMLElement, fallback: number): number {
    const layoutWidth = viewportEl.offsetWidth;
    if (layoutWidth > 0) {
        const scale = viewportEl.getBoundingClientRect().width / layoutWidth;
        if (scale > 0 && isFinite(scale)) return scale;
    }
    return fallback > 0 ? fallback : 1;
}

// Measures every node and its handles inside the (transformed) viewport element.
// Sizes/offsets are divided by the on-screen scale so they are expressed in flow units.
export function measureNodes(viewportEl: HTMLElement | null, zoom: number): BlowMeasuredNode[] {
    const result: BlowMeasuredNode[] = [];
    if (!viewportEl) return result;
    const z = screenScale(viewportEl, zoom);

    viewportEl.querySelectorAll<HTMLElement>(`.${NODE_CLASS}`).forEach(nodeEl => {
        // Only this flow's own nodes: a flow nested inside a node has nodes of its own.
        if (nodeEl.closest(`.${VIEWPORT_CLASS}`) !== viewportEl) return;

        const rect = nodeEl.getBoundingClientRect();
        const id = nodeEl.getAttribute('data-id');
        const handles: BlowMeasuredHandle[] = [];

        nodeEl.querySelectorAll<HTMLElement>(`.${HANDLE_CLASS}`).forEach(h => {
            // Likewise only this node's handles, not those of a nested node.
            if (h.closest(`.${NODE_CLASS}`) !== nodeEl) return;

            const hr = h.getBoundingClientRect();
            const cx = hr.left + hr.width / 2;
            const cy = hr.top + hr.height / 2;
            handles.push({
                handleId: h.getAttribute('data-handle-id') || '',
                type: h.getAttribute('data-handle-type') || 'source',
                position: h.getAttribute('data-handle-pos') || 'bottom',
                offsetX: (cx - rect.left) / z,
                offsetY: (cy - rect.top) / z,
                connectableEnd: h.getAttribute('data-connectable-end') !== 'false'
            });
        });

        result.push({
            id,
            width: rect.width / z,
            height: rect.height / z,
            handles
        });
    });

    return result;
}

// Returns the element under the given client point that matches `selector`, walking up from the
// topmost element. Used to resolve connection drop targets. Only elements inside `paneEl` count:
// another flow on the page may use the very same node and handle ids.
export function elementDataAtPoint(x: number, y: number, selector: string, paneEl: HTMLElement | null): BlowHandleHit | null {
    let el = document.elementFromPoint(x, y);
    if (paneEl && (!el || !paneEl.contains(el))) return null;

    while (el && el !== paneEl) {
        if (el.matches && el.matches(selector)) {
            // A handle of a flow nested inside this one belongs to that flow, not this one.
            if (paneEl && el.closest(`.${ROOT_CLASS}`) !== paneEl) return null;
            return {
                nodeId: el.getAttribute('data-id'),
                handleId: el.getAttribute('data-handle-id') || '',
                handleType: el.getAttribute('data-handle-type') || '',
                position: el.getAttribute('data-handle-pos') || '',
                connectableEnd: el.getAttribute('data-connectable-end') !== 'false'
            };
        }
        el = el.parentElement;
    }
    return null;
}

// Watches the flow for size changes that C# cannot see: the pane being resized by its
// layout, and node elements growing or shrinking as their content changes. Both invalidate
// the cached rectangles the canvas uses for hit-testing and edge anchoring.
//
// Returns a handle whose dispose() tears everything down.
export function observeResize(paneEl: HTMLElement | null, viewportEl: HTMLElement | null, dotNetRef: DotNetObjectReference): BlowDisposable | null {
    if (!paneEl || !viewportEl || typeof ResizeObserver === 'undefined') return null;

    let frame = 0;
    let pendingMeasure = false;
    let disposed = false;

    // Coalesce bursts of observations into one callback per frame: a single layout pass can
    // resize the pane and every node at once, and each would otherwise round-trip to .NET.
    // `measureNodes` is false for events that only move the pane (page scroll), where
    // re-measuring every node would be wasted work.
    const notify = (measureNodes: boolean) => {
        if (disposed) return;
        pendingMeasure = pendingMeasure || measureNodes;
        if (frame) return;
        frame = requestAnimationFrame(() => {
            frame = 0;
            const measure = pendingMeasure;
            pendingMeasure = false;
            if (disposed) return;
            // The component can be torn down (or its circuit dropped) between a frame being
            // scheduled and it running; that is an expected end, not an error worth reporting.
            Promise.resolve(dotNetRef.invokeMethodAsync('OnResizeObserved', measure)).catch(() => { });
        });
    };

    const onSizeChanged = () => notify(true);

    // Scrolling an ancestor moves the pane on screen without resizing anything, which still
    // invalidates the cached rectangle used for screen <-> flow conversions. Scrolling inside the
    // flow (a scrollable node) or in some unrelated part of the page does not.
    const onScroll = (e: Event) => {
        const t = e.target;
        if (t === document || t === document.documentElement || t === document.body
            || (t instanceof Node && t.contains(paneEl))) {
            notify(false);
        }
    };
    const onWindowResize = () => notify(false);

    const resizeObserver = new ResizeObserver(onSizeChanged);
    resizeObserver.observe(paneEl);

    const isElement = (n: Node): n is Element => n.nodeType === 1;
    const isNode = (el: Element) => el.classList && el.classList.contains(NODE_CLASS);
    const forEachNode = (el: Element, action: (node: Element) => void) => {
        if (isNode(el)) action(el);
        el.querySelectorAll(`.${NODE_CLASS}`).forEach(action);
    };
    const hasHandle = (el: Element) => el.matches(`.${HANDLE_CLASS}`) || el.querySelector(`.${HANDLE_CLASS}`) !== null;
    viewportEl.querySelectorAll(`.${NODE_CLASS}`).forEach(el => resizeObserver.observe(el));

    // Nodes (and the handles inside them) are added and removed by Blazor. Only those changes
    // matter here: the selection box, alignment guides, edges and the connection line come and
    // go constantly during a gesture, and re-measuring every node for each of them would cost a
    // round trip to .NET plus a forced layout of the whole graph.
    const mutationObserver = new MutationObserver(records => {
        let relevant = false;
        for (const record of records) {
            record.addedNodes.forEach(added => {
                if (!isElement(added)) return;
                forEachNode(added, el => { resizeObserver.observe(el); relevant = true; });
                if (!relevant && hasHandle(added)) relevant = true;
            });
            record.removedNodes.forEach(removed => {
                if (!isElement(removed)) return;
                forEachNode(removed, el => { resizeObserver.unobserve(el); relevant = true; });
                if (!relevant && hasHandle(removed)) relevant = true;
            });
        }
        if (relevant) onSizeChanged();
    });
    mutationObserver.observe(viewportEl, { childList: true, subtree: true });

    // Capture phase so scrolling in any ancestor container is seen, not just the window.
    window.addEventListener('scroll', onScroll, true);
    window.addEventListener('resize', onWindowResize);

    return {
        dispose: () => {
            disposed = true;
            if (frame) cancelAnimationFrame(frame);
            resizeObserver.disconnect();
            mutationObserver.disconnect();
            window.removeEventListener('scroll', onScroll, true);
            window.removeEventListener('resize', onWindowResize);
        }
    };
}

// True for an element the user types into, where keys belong to the text rather than the flow.
function isEditable(el: Element | null): boolean {
    if (!el) return false;
    if ((el as HTMLElement).isContentEditable) return true;
    const tag = el.tagName;
    if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
    if (tag !== 'INPUT') return false;
    // Buttons/checkboxes don't swallow text keys; text-ish inputs do.
    const type = (el.getAttribute('type') || 'text').toLowerCase();
    return !['button', 'submit', 'reset', 'checkbox', 'radio', 'range', 'color', 'file'].includes(type);
}

// Installs the nodrag / nopan / nowheel escape hatches.
//
// Blazor delegates DOM events to a single listener at the document root, so a listener on the
// pane that calls stopPropagation() prevents the event from ever reaching Blazor's dispatcher -
// for every handler on the flow at once. That is exactly the semantics we want: an element
// marked `nodrag` should neither drag its node nor pan the canvas, and one marked `nowheel`
// should keep its own scrolling instead of zooming the flow. Only elements inside the pane
// count: a page that happens to wrap the whole flow in an element with one of these classes must
// not switch the flow off.
//
// Only the DOM knows which element an event started on, which is why this cannot live in C#.
//
// The same listeners also apply the parts of the input policy that must be decided while the
// event is still being dispatched, since a DOM event can only be cancelled synchronously and so
// cannot wait for a round-trip to .NET. `policy` carries them:
//   preventScrolling, panOnScroll, zoomOnScroll, zoomKey - whether a wheel over the flow may
//     scroll the page. A plain scroll that the flow ignores, because zooming on scroll is off or
//     its zoom activation key is not held, is left to scroll the page; so is a wheel over a
//     panel, the controls or a non-zoomable minimap, which keep the behaviour of ordinary page
//     content.
//   panKey, keyboardA11y, selectAllKey, clipboardKeys, historyKeys - which keys the flow uses
//     while it (or a node or edge in it) has focus, so they do not also reach the browser: Space/
//     Enter activate a focused node or edge, the arrow keys move the selection, the pan key held
//     over the pane pans, and Ctrl/Cmd+A, C, X, V, D, Z and Y select, copy, paste, duplicate, undo
//     and redo - which would otherwise also select the page's text or open the bookmark dialog.
//
// Returns a handle whose dispose() removes the listeners.
export function installInteractionGuards(paneEl: HTMLElement | null, noDragClass: string | null, noPanClass: string | null,
    noWheelClass: string | null, policy: BlowInputPolicy | null): BlowDisposable | null {
    if (!paneEl) return null;
    const pane = paneEl;

    // Class names are user input; escaped, a name such as "md:nodrag" is a valid selector.
    const escape = (c: string) => (window.CSS && typeof CSS.escape === 'function') ? CSS.escape(c) : c;
    const pointerSelector = [noDragClass, noPanClass]
        .filter((c): c is string => !!c)
        .map(c => '.' + escape(c))
        .join(',');
    const wheelSelector = noWheelClass ? '.' + escape(noWheelClass) : '';

    // The nearest element matching `selector` between the event target and the pane, or null.
    // `closest` is only defined on Elements; text nodes and the document are not our concern.
    const closestInPane = (target: EventTarget | null, selector: string): Element | null => {
        if (!selector || !(target instanceof Element)) return null;
        const match = target.closest(selector);
        return match && match !== pane && pane.contains(match) ? match : null;
    };

    // True for an event that started inside another flow nested in this one. That flow applies its
    // own guards and policy and keeps the event from reaching this one; acting on it here as well
    // would switch the nested flow off (stopPropagation) or impose this flow's wheel policy on it.
    const inNestedFlow = (target: EventTarget | null): boolean =>
        target instanceof Element && target.closest(`.${ROOT_CLASS}`) !== pane;

    const onPointerDown = (e: PointerEvent) => {
        if (inNestedFlow(e.target)) return;
        if (closestInPane(e.target, pointerSelector)) e.stopPropagation();
    };

    const preventScrolling = !!(policy && policy.preventScrolling);
    const panOnScroll = !!(policy && policy.panOnScroll);
    const zoomOnScroll = !(policy && policy.zoomOnScroll === false);
    const zoomKey = (policy && policy.zoomKey) || null;
    const panKey = (policy && policy.panKey) || null;
    const keyboardA11y = !!(policy && policy.keyboardA11y);
    const selectAllKey = !!(policy && policy.selectAllKey);
    const clipboardKeys = !!(policy && policy.clipboardKeys);
    const historyKeys = !!(policy && policy.historyKeys);
    const overlaySelector = '.bit-blow-panel, .bit-blow-controls, .bit-blow-minimap';

    // The Ctrl/Cmd shortcuts the flow has been told to handle itself.
    const isFlowShortcut = (e: KeyboardEvent): boolean => {
        if (!e.ctrlKey && !e.metaKey) return false;
        switch ((e.key || '').toLowerCase()) {
            case 'a': return selectAllKey;
            case 'c': case 'x': case 'v': case 'd': return clipboardKeys;
            case 'z': case 'y': return historyKeys;
            default: return false;
        }
    };

    const onWheel = (e: WheelEvent) => {
        if (inNestedFlow(e.target)) return;
        if (closestInPane(e.target, wheelSelector)) {
            // The element keeps its own scrolling: neither the flow nor the page policy applies.
            // Ctrl+wheel is still a pinch, though, and left alone it zooms the whole page.
            e.stopPropagation();
            if (e.ctrlKey) e.preventDefault();
            return;
        }
        if (!preventScrolling) return;

        const overlay = closestInPane(e.target, overlaySelector);
        if (overlay) {
            // A zoomable minimap uses the wheel itself; anything else scrolls as it always did.
            if (overlay.matches('.bit-blow-minimap.zoomable') || e.ctrlKey) e.preventDefault();
            return;
        }

        // Ctrl+wheel is the browser's pinch-zoom and always zooms the flow; any wheel while
        // pan-on-scroll owns it pans the flow. Otherwise zooming on scroll being off, or a required
        // zoom key that is not held, means the flow does nothing with this wheel, and the page
        // should get to scroll.
        const passesThrough = !panOnScroll && !e.ctrlKey
            && (!zoomOnScroll || (!!zoomKey && !e[zoomKey]));
        if (!passesThrough) e.preventDefault();
    };

    const onKeyDown = (e: KeyboardEvent) => {
        const t = e.target;
        if (!(t instanceof Element) || isEditable(t)) return;

        // The pane itself, or one of this flow's own nodes or edges - not a button or an input
        // inside a node, which keep their own keys, and not the elements of a nested flow.
        const onElement = t !== pane
            && t.matches('.bit-blow-node, .bit-blow-edge')
            && t.closest(`.${ROOT_CLASS}`) === pane;
        if (t !== pane && !onElement) return;

        if (onElement && (e.key === ' ' || e.key === 'Enter')) e.preventDefault();
        else if (panKey && e.key === panKey) e.preventDefault();
        else if (keyboardA11y && e.key && e.key.startsWith('Arrow')) e.preventDefault();
        else if (isFlowShortcut(e)) e.preventDefault();
    };

    if (pointerSelector) pane.addEventListener('pointerdown', onPointerDown);
    // Not passive: preventDefault() has to be able to stop the page from scrolling.
    pane.addEventListener('wheel', onWheel, { passive: false });
    pane.addEventListener('keydown', onKeyDown);

    return {
        dispose: () => {
            pane.removeEventListener('pointerdown', onPointerDown);
            pane.removeEventListener('wheel', onWheel);
            pane.removeEventListener('keydown', onKeyDown);
        }
    };
}

// Sets pointer capture so dragging continues even when the cursor leaves the element.
export function setPointerCapture(el: Element | null, pointerId: number): void {
    try { el?.setPointerCapture(pointerId); } catch { /* the pointer is already gone */ }
}

export function releasePointerCapture(el: Element | null, pointerId: number): void {
    try {
        if (el?.hasPointerCapture(pointerId)) el.releasePointerCapture(pointerId);
    } catch { /* the pointer is already gone */ }
}

// True when focus sits in something the user is typing into, so the flow can ignore
// keys like Delete/Backspace/arrows that would otherwise mangle the graph.
export function isEditableFocused(): boolean {
    return isEditable(document.activeElement);
}
