/**
 * bit-bmotion.ts - slim browser-API bridge
 *
 * All animation math (spring, tween, inertia, keyframes, easing, colour
 * interpolation, gesture state, transform composition) now lives in the
 * C# AnimationEngine / ElementAnimationState classes running as WebAssembly.
 *
 * This file only touches browser-native APIs:
 *    requestAnimationFrame   drives the C# animation engine each tick
 *    element.style           applies CSS updates returned by ComputeFrame
 *    Pointer / Focus events  gesture input forwarded to the C# component
 *    IntersectionObserver    viewport visibility forwarded to C#
 *    Scroll events           scroll progress forwarded to C#
 *    getBoundingClientRect   FLIP layout snapshot
 *    Web Animations API      FLIP playback
 *
 * The exported surface is compiled to an ES module (wwwroot/bit-bmotion.js) that the C#
 * interop loads via dynamic `import`; every exported function name is an interop entry point.
 */

// A rectangle with the fields this module reads off getBoundingClientRect (and the test rects).
interface BmRect { left: number; top: number; right: number; bottom: number; width: number; height: number; }
// Per-edge transform bounds; a null edge is unconstrained on that side.
interface BmBounds { left: number | null; right: number | null; top: number | null; bottom: number | null; }
// Per-edge elastic factors (0 = rigid, 1 = fully elastic).
interface BmElastic { left: number; right: number; top: number; bottom: number; }
interface BmXY { x: number; y: number; }
// A CSS property/value map (also carries SVG geometry attrs like `d`/`points`).
type BmStyleMap = { [prop: string]: string };

//
// Pure drag / scroll math - no DOM access, so it is unit-tested directly (Tests/bit-bmotion-js).
// The exports are an internal testing surface; the C# interop only calls the named bridge functions.
//

/** Elastic give past a constraint edge: 0 = rigid, 1 = fully elastic. */
export function applyElastic(overflow: number, edge: number): number {
    return edge > 0 ? overflow * edge : 0;
}

/**
 * Turns a container rect + element rect (and the element's current transform x/y) into the allowed
 * transform bounds. An element larger than its container collapses to the centered offset so the
 * drag pins in place instead of oscillating between contradictory bounds.
 */
export function resolveConstraintBounds(cRect: BmRect, eRect: BmRect, curX: number, curY: number): BmBounds {
    const baseLeft = eRect.left - curX;
    const baseTop  = eRect.top  - curY;
    let left   = cRect.left   - baseLeft;
    let right  = cRect.right  - (baseLeft + eRect.width);
    let top    = cRect.top    - baseTop;
    let bottom = cRect.bottom - (baseTop + eRect.height);
    if (right < left) { const mid = (left + right) / 2; left = right = mid; }
    if (bottom < top) { const mid = (top + bottom) / 2; top = bottom = mid; }
    return { left, right, top, bottom };
}

/** Clamps a drag position to its constraints, applying per-edge elastic overflow. */
export function clampToConstraints(x: number, y: number, c: BmBounds | null, elastic: BmElastic): BmXY {
    if (!c) return { x, y };
    if (c.left   != null && x < c.left)   x = c.left   - applyElastic(c.left   - x, elastic.left);
    if (c.right  != null && x > c.right)  x = c.right  + applyElastic(x - c.right,  elastic.right);
    if (c.top    != null && y < c.top)    y = c.top    - applyElastic(c.top    - y, elastic.top);
    if (c.bottom != null && y > c.bottom) y = c.bottom + applyElastic(y - c.bottom, elastic.bottom);
    return { x, y };
}

/**
 * Whether Space on a focused element of this kind does something of its own - activates a button, toggles a
 * checkbox, opens a picker, types a space - rather than scrolling the page. A tap's keyboard handling must leave
 * those defaults alone; only elsewhere is Space's page scroll suppressed. (Every `<input>` type either activates
 * on Space or types it, so none of them scrolls.)
 */
export function nativeSpaceActivation(tagName: string, editable: boolean): boolean {
    if (editable) return true;
    switch (tagName.toUpperCase()) {
        case 'BUTTON': case 'INPUT': case 'SUMMARY': case 'SELECT': case 'TEXTAREA': return true;
        default: return false;
    }
}

function hasNativeSpaceActivation(el: HTMLElement): boolean {
    return nativeSpaceActivation(el.tagName, el.isContentEditable);
}

// How many drags are in progress anywhere on the page: a pointer carrying a dragged element over another one
// is not hovering it.
let _activeDrags = 0;

// While a drag is under way the browser must not run a gesture of its own off the same pointer. WebKit starts a text
// selection from a press on a draggable (user-select: none on the element doesn't stop one anchored around it) and
// extends it over the page as the pointer travels; a later press inside that selection then starts a native
// drag-and-drop of it, which takes the pointer over - no more moves, no pointerup, and the drag never ends.
let _nativeGesturesGuarded = false;

function _guardNativeGestures(): void {
    if (_nativeGesturesGuarded) return;
    _nativeGesturesGuarded = true;
    const block = (e: Event) => { if (_activeDrags > 0) e.preventDefault(); };
    document.addEventListener('selectstart', block, true);
    document.addEventListener('dragstart', block, true);
}

/** Normalised 0→1 scroll progress along one axis (0 when the content doesn't overflow). */
export function scrollFraction(scroll: number, size: number, client: number): number {
    return size > client ? scroll / (size - client) : 0;
}

/**
 * A subject's 0→1 progress through a scrollport, the fallback for `ViewTimeline`'s default range:
 * 0 the moment its leading edge enters at the far side, 1 the moment its trailing edge leaves at
 * the near side. `near`/`far` are the scrollport's edges along the tracked axis and `start`/`size`
 * the subject's position and extent in the same coordinate space.
 *
 * The travelled span is the scrollport plus the subject (the subject is off-screen at both ends),
 * so a subject taller than the scrollport still maps its whole journey onto 0→1 instead of
 * saturating. A zero span (both degenerate) yields 0 rather than a division by zero.
 */
export function viewFraction(start: number, size: number, near: number, far: number): number {
    const span = (far - near) + size;
    if (span <= 0) return 0;
    const travelled = (far - start) / span;
    return travelled < 0 ? 0 : travelled > 1 ? 1 : travelled;
}

/** The timeline-range keywords a CSS `animation-range` part can start with. */
const _RANGE_NAMES = /^(normal|cover|contain|entry|exit|entry-crossing|exit-crossing)$/i;

/**
 * Splits a CSS `animation-range` shorthand into its start and end parts, because the Web Animations
 * API takes them as two separate `rangeStart` / `rangeEnd` values rather than as one string.
 *
 * A part is either a keyword with an optional offset ("entry 0%") or a bare length/percentage
 * ("40%"), so a token begins a new part unless it is an offset following a keyword that does not
 * have one yet. "entry 0% cover 50%" → ["entry 0%", "cover 50%"]; "0% 100%" → ["0%", "100%"].
 */
export function splitAnimationRange(range: string): [string | undefined, string | undefined] {
    const parts: string[] = [];
    for (const token of (range ?? '').trim().split(/\s+/).filter(Boolean)) {
        const attaches = parts.length > 0 && !_RANGE_NAMES.test(token) && _awaitsOffset(parts[parts.length - 1]);
        if (attaches) parts[parts.length - 1] += ' ' + token;
        else parts.push(token);
    }
    return [parts[0], parts[1]];
}

/** True when a part so far is a bare keyword, i.e. the next offset token belongs to it. */
function _awaitsOffset(part: string): boolean {
    return part.indexOf(' ') < 0 && _RANGE_NAMES.test(part);
}

/**
 * Layout FLIP corrections (plan item 1.2). The box FLIPs with `translate(d) scale(s)` about its anchor, eased
 * back to identity; a direct child would ride along - stretched by the scale and shifted by the translate - and
 * its corners would scale with it. The child is given the exact inverse of the box's transform about the same
 * physical point, and the corners a radius that renders constant, at every step of the way (flipChildFrames,
 * flipRadiusFrames). A correction keyframed only at the two ends would be exact there and wrong in between.
 */
// FLIP scale factors come from width/height ratios, so they are non-negative; clamp them away
// from zero so a collapsed (0-sized) start rect can't turn the inverse math into Infinity/NaN.
const MIN_FLIP_SCALE = 1e-4;

/**
 * The progress offsets the FLIP's corrections are keyframed at. The parent's scale runs linearly from s to 1,
 * so a correction 1/scale is a curve, not a line: two keyframes would interpolate 1/s → 1 linearly and the
 * product would leave 1 in between (12.5% at the midpoint of a 2× grow). Keyframing it closely enough keeps it
 * exact to the eye; the samples are spaced evenly in log(scale), which is where 1/scale bends, plus evenly in
 * progress for the translate. Both animations share the timing easing, so the offsets are in the same
 * eased-progress space as the parent's two keyframes.
 */
export function flipCorrectionOffsets(sx: number, sy: number, segments = 12): number[] {
    const offsets = new Set<number>();
    for (let i = 0; i <= segments; i++) offsets.add(i / segments);
    for (const s of [Math.max(sx, MIN_FLIP_SCALE), Math.max(sy, MIN_FLIP_SCALE)]) {
        if (Math.abs(Math.log(s)) < 0.01) continue;
        for (let i = 1; i < segments; i++) {
            // The scale at this sample, geometric between s and 1, and the progress the parent reaches it at.
            const at = Math.exp(Math.log(s) * (1 - i / segments));
            offsets.add((at - s) / (1 - s));
        }
    }
    return Array.from(new Set(Array.from(offsets, o => Math.round(o * 1e6) / 1e6))).sort((a, b) => a - b);
}

/**
 * The keyframes that keep a direct child of a FLIPping box where it is and the size it is. The box plays
 * `translate(d·(1-q)) scale(s + (1-s)·q)` about its anchor at progress q; each keyframe is that transform's exact
 * inverse about the same physical point, written in the child's own coordinates.
 */
export function flipChildFrames(
    parentRect: BmRect, childRect: BmRect, sx: number, sy: number, dx: number, dy: number,
    anchorX = 0, anchorY = 0): { originX: number; originY: number; frames: Keyframe[] } {
    sx = Math.max(sx, MIN_FLIP_SCALE);
    sy = Math.max(sy, MIN_FLIP_SCALE);
    const frames = flipCorrectionOffsets(sx, sy).map(q => {
        const scaleX = sx + (1 - sx) * q, scaleY = sy + (1 - sy) * q;
        const tx = -dx * (1 - q) / scaleX, ty = -dy * (1 - q) / scaleY;
        return { offset: q, transform: `translate(${tx}px,${ty}px) scaleX(${1 / scaleX}) scaleY(${1 / scaleY})` };
    });
    return {
        originX: parentRect.left + anchorX * parentRect.width - childRect.left,
        originY: parentRect.top + anchorY * parentRect.height - childRect.top,
        frames,
    };
}

/** The border-radius keyframes that render as `radius` throughout a FLIP's scale (see flipChildFrames). */
export function flipRadiusFrames(radius: number, sx: number, sy: number): Keyframe[] {
    sx = Math.max(sx, MIN_FLIP_SCALE);
    sy = Math.max(sy, MIN_FLIP_SCALE);
    return flipCorrectionOffsets(sx, sy).map(q => ({
        offset: q,
        borderRadius: `${radius / (sx + (1 - sx) * q)}px / ${radius / (sy + (1 - sy) * q)}px`,
    }));
}

//
// rAF loop  C# ComputeFrame is called synchronously each tick (Blazor WASM)
//

let _rafId: number | null = null;
// Set of engine DotNetObjectReferences. Using a set (rather than a single global) means
// multiple Blazor roots / engine instances sharing this module each get ticked, instead of a
// second startRafLoop silently hijacking the loop from the first.
const _engines = new Set<DotNet.DotNetObject>();

export function startRafLoop(dotnetRef: DotNet.DotNetObject): void {
    _engines.add(dotnetRef);
    if (_rafId === null) _rafId = requestAnimationFrame(_tick);
}

export function stopRafLoop(dotnetRef?: DotNet.DotNetObject): void {
    // With an argument, stop only that engine; without one, stop everything (back-compat).
    if (dotnetRef) _engines.delete(dotnetRef);
    else _engines.clear();
    if (_engines.size === 0 && _rafId !== null) {
        cancelAnimationFrame(_rafId);
        _rafId = null;
    }
}

// Work for the start of the next frame whose result that frame's styles must include - on WebAssembly run in the
// tick before ComputeFrame (a drag's auto-scroll step, and the drag position it leads to), otherwise in a frame of
// its own. Each callback runs once; one that should run again asks again.
const _beforeFrame = new Set<() => void>();
let _ownFrame: number | null = null;

function _requestBeforeFrame(callback: () => void): void {
    _beforeFrame.add(callback);
    if (_rafId === null) _scheduleOwnFrame();
}

function _scheduleOwnFrame(): void {
    _ownFrame ??= requestAnimationFrame(() => { _ownFrame = null; _runBeforeFrame(); });
}

function _runBeforeFrame(): void {
    if (_beforeFrame.size === 0) return;
    const due = [..._beforeFrame];
    _beforeFrame.clear();
    for (const callback of due) {
        try { callback(); } catch (e) { console.error('bmotion: frame callback failed', e); }
    }
}

function _tick(timestamp: number): void {
    _runBeforeFrame();
    if (_engines.size === 0) {
        _rafId = null;
        if (_beforeFrame.size > 0) _scheduleOwnFrame();
        return;
    }
    for (const ref of _engines) {
        try {
            // invokeMethod is synchronous in Blazor WASM  C# does all animation math here
            const updates = ref.invokeMethod<{ [elementId: string]: BmStyleMap } | null>('ComputeFrame', timestamp);
            if (updates) {
                for (const elementId in updates) {
                    const el = document.getElementById(elementId);
                    if (!el) continue;
                    _applyStyles(el, updates[elementId]);
                }
            }
        } catch (e) {
            // Never let a fault from one engine's synchronous tick stop the shared rAF loop.
            console.error('bmotion: ComputeFrame tick failed', e);
        }
    }
    _rafId = requestAnimationFrame(_tick);
}

//
// Style helpers
//

// SVG geometry presentation attributes are set on the element, not via inline style: the CSS `d`
// property needs a path() wrapper and isn't universally supported, so setAttribute is the reliable
// path for shape morphing (`d`) and polygon/polyline morphing (`points`).
const _svgGeomAttrs = new Set(['d', 'points']);

function _applyStyles(el: HTMLElement, styles: BmStyleMap): void {
    for (const prop in styles) {
        if (prop.startsWith('--')) el.style.setProperty(prop, styles[prop]);
        else if (_svgGeomAttrs.has(prop)) el.setAttribute(prop, styles[prop]);
        else el.style[prop as any] = styles[prop];
    }
    // A new transform from .NET moves the base a drag being shown locally is measured from.
    if ('transform' in styles && _localDrags.has(el)) _showLocalDrag(el, true);
}

// ── Drag shown locally (Blazor Server) ──────────────────────────────────────

// Without synchronous interop, .NET writes a dragged element's transform a round trip after each move. Meanwhile the
// bridge shows the element where the drag has it, with the CSS translate property - which the browser applies on top
// of the transform: the difference between the drag's position and what the transform shows. Recomputed on every move
// and whenever .NET writes the transform. After the release it goes once the transform has caught up - or, once .NET
// has the final position (settled), at the next transform it writes, wherever the release takes the element.
const _localDrags = new Map<HTMLElement, { x: number; y: number; dragging: boolean; settled?: boolean }>();

function _showLocalDrag(el: HTMLElement, transformWritten = false): void {
    const drag = _localDrags.get(el);
    if (!drag) return;
    const shown = _currentTranslation(el); // the transform alone: translate is a separate property
    const dx = drag.x - shown.x, dy = drag.y - shown.y;
    const caughtUp = Math.abs(dx) < 0.5 && Math.abs(dy) < 0.5;
    if (!drag.dragging && (caughtUp || (drag.settled && transformWritten))) {
        el.style.translate = '';
        _localDrags.delete(el);
        return;
    }
    el.style.translate = `${dx}px ${dy}px`;
}

// The offset a local drag still shows on top of the transform (a drag picked up again before .NET caught up).
function _localOffset(el: HTMLElement): BmXY {
    if (!_localDrags.has(el) || !el.style.translate) return { x: 0, y: 0 };
    const [x, y] = el.style.translate.split(/\s+/).map(v => parseFloat(v) || 0);
    return { x: x ?? 0, y: y ?? 0 };
}

// Where the element shows: its transform, plus what a drag shown locally adds.
function _shownTranslation(el: HTMLElement): BmXY {
    const t = _currentTranslation(el), o = _localOffset(el);
    return { x: t.x + o.x, y: t.y + o.y };
}

// Something else takes the element over (a browser animation .NET started - the throw after the release): .NET has
// the final position by then, so the local offset goes.
function _endLocalDrag(el: HTMLElement): void {
    if (!_localDrags.get(el) || _localDrags.get(el)!.dragging) return;
    el.style.translate = '';
    _localDrags.delete(el);
}

/** Apply a styles object to an element by ID (used for instant set() calls). */
export function applyStyles(elementId: string, styles: BmStyleMap): void {
    const el = document.getElementById(elementId);
    if (el) _applyStyles(el, styles);
}

// One property's value as the page shows it: the computed style (camelCase name or custom property), or the SVG
// geometry attribute.
function _readStyle(el: HTMLElement, cs: CSSStyleDeclaration, prop: string): string {
    if (_svgGeomAttrs.has(prop)) return el.getAttribute(prop) ?? '';
    if (prop.startsWith('--')) return cs.getPropertyValue(prop).trim();
    return String((cs as any)[prop] ?? '');
}

function _inline(el: HTMLElement, prop: string): string {
    return prop.startsWith('--') ? el.style.getPropertyValue(prop) : String((el.style as any)[prop] ?? '');
}

function _setInline(el: HTMLElement, prop: string, value: string): void {
    if (prop.startsWith('--')) el.style.setProperty(prop, value);
    else (el.style as any)[prop] = value;
}

/**
 * Reads what the engine needs to know before it animates: the computed values of `current` (starting values it
 * has never seen), and for `stylesheet` the value the stylesheet gives each property once its inline declaration
 * is lifted - read with the declaration removed for an instant and put straight back, so nothing is ever painted
 * without it. Null when the element is gone.
 */
export function readStyles(elementId: string, current: string[], stylesheet: string[], auto?: string[]):
    { current: BmStyleMap; stylesheet: BmStyleMap; auto: BmStyleMap } | null {
    const el = document.getElementById(elementId);
    if (!el) return null;
    const out = { current: {} as BmStyleMap, stylesheet: {} as BmStyleMap, auto: {} as BmStyleMap };
    // The natural size an animation to "auto" plays towards: the computed size with the inline size set to auto for
    // an instant (an animation of ours on it was cancelled by the engine first). Read before the current values,
    // which must see the element as it is.
    for (const prop of auto ?? []) {
        const inline = _inline(el, prop);
        _setInline(el, prop, 'auto');
        out.auto[prop] = _readStyle(el, getComputedStyle(el), prop);
        _setInline(el, prop, inline);
    }
    if (current.length) {
        const cs = getComputedStyle(el);
        for (const prop of current) out.current[prop] = _readStyle(el, cs, prop);
    }
    for (const prop of stylesheet) {
        if (_svgGeomAttrs.has(prop)) continue; // an attribute has no stylesheet value behind it
        const inline = _inline(el, prop);
        _setInline(el, prop, '');
        out.stylesheet[prop] = _readStyle(el, getComputedStyle(el), prop);
        _setInline(el, prop, inline);
    }
    return out;
}

//
// Accessibility - prefers-reduced-motion
//

/** Returns true when the user has requested reduced motion at the OS/browser level. */
export function prefersReducedMotion(): boolean {
    return typeof matchMedia === 'function' &&
        matchMedia('(prefers-reduced-motion: reduce)').matches;
}

// Live prefers-reduced-motion change notifications. Keyed by engine DotNetObjectReference so
// each engine can subscribe/unsubscribe independently and we only keep one media-query listener.
const _reducedMotionRefs = new Set<DotNet.DotNetObject>();
let _reducedMotionMql: MediaQueryList | null = null;
let _reducedMotionListener: ((e: MediaQueryListEvent) => void) | null = null;

function _ensureReducedMotionListener(): void {
    if (_reducedMotionMql || typeof matchMedia !== 'function') return;
    _reducedMotionMql = matchMedia('(prefers-reduced-motion: reduce)');
    _reducedMotionListener = (e: MediaQueryListEvent) => {
        for (const ref of _reducedMotionRefs) {
            try { ref.invokeMethodAsync('OnReducedMotionChanged', e.matches); }
            catch { /* a disposed/faulted engine ref must not break the others */ }
        }
    };
    // addEventListener is the modern API; addListener is the deprecated Safari fallback.
    if (_reducedMotionMql.addEventListener) _reducedMotionMql.addEventListener('change', _reducedMotionListener);
    else if (_reducedMotionMql.addListener) _reducedMotionMql.addListener(_reducedMotionListener);
}

export function watchReducedMotion(dotnetRef: DotNet.DotNetObject): void {
    _reducedMotionRefs.add(dotnetRef);
    _ensureReducedMotionListener();
}

export function unwatchReducedMotion(dotnetRef: DotNet.DotNetObject): void {
    _reducedMotionRefs.delete(dotnetRef);
    if (_reducedMotionRefs.size === 0 && _reducedMotionMql && _reducedMotionListener) {
        if (_reducedMotionMql.removeEventListener) _reducedMotionMql.removeEventListener('change', _reducedMotionListener);
        else if (_reducedMotionMql.removeListener) _reducedMotionMql.removeListener(_reducedMotionListener);
        _reducedMotionMql = null;
        _reducedMotionListener = null;
    }
}

//
// Element registration
//

const _eventCleanup = new Map<string, Array<(unregistering: boolean) => void>>(); // elementId  Array<cleanup>

export function registerElement(elementId: string): boolean {
    const el = document.getElementById(elementId);
    if (el) el.setAttribute('data-bmid', elementId);
    return !!el;
}

//
// Programmatic animate() API - resolve elements by CSS selector or ElementReference
// Assigns a stable id + data-bmid so the engine can address them via getElementById.
//

let _programmaticSeq = 0;

function _ensureElementId(el: Element): string {
    const existing = el.getAttribute('data-bmid');
    if (existing) return existing;
    let id = el.id;
    if (!id) {
        // Skip any generated id that already exists in the DOM so we never collide
        // with an element that has the same id assigned elsewhere.
        do {
            id = 'bm-p' + (++_programmaticSeq);
        } while (document.getElementById(id));
    }
    el.id = id;
    el.setAttribute('data-bmid', id);
    return id;
}

/** Resolve all elements matching a CSS selector and return their element IDs. */
export function resolveOrRegisterBySelector(selector: string): string[] {
    try {
        return Array.from(document.querySelectorAll(selector)).map(el => _ensureElementId(el));
    } catch {
        return [];
    }
}

/** Resolve the element for a Blazor ElementReference and return its element ID. */
export function resolveOrRegisterByRef(element: Element): string {
    return _ensureElementId(element);
}

export function unregisterElement(elementId: string): void {
    const el = document.getElementById(elementId);
    if (el) el.removeAttribute('data-bmid');
    _runCleanup(elementId, true);
    // Release scroll-scrub listeners before cancelling, so a fallback timeline can't keep firing
    // against an animation that is about to go away.
    _releaseScrubsForElement(elementId);
    _cancelWaapiForElement(elementId, false);
    // Detach from every viewport observer (drops membership and evicts empty observers).
    _detachFromObservers(el, elementId);
    _vpRefs.delete(elementId);
    _popped.delete(elementId);
    _flips.delete(elementId);
    _interruptedAt.delete(elementId);
}

function _runCleanup(elementId: string, unregistering = false): void {
    const fns = _eventCleanup.get(elementId);
    if (fns) { fns.forEach(fn => fn(unregistering)); _eventCleanup.delete(elementId); }
}

//
// Gesture event listeners (hover / tap / focus / drag)
// C# handles all state-machine logic; JS only forwards raw browser events.
//

/**
 * Attach event listeners to an element.
 * @param {string} elementId
 * @param {{ hover?: bool, tap?: bool, focus?: bool, drag?: bool,
 *            dragAxis?: string, dragConstraints?: object,
 *            dragElastic?: number }} events
 * @param dotnetRef  DotNetObjectReference<Motion>
 */
export function attachEventListeners(elementId: string, events: any, dotnetRef: DotNet.DotNetObject): void {
    const el = document.getElementById(elementId);
    if (!el) return;
    _runCleanup(elementId);
    const cleanups: Array<(unregistering: boolean) => void> = [];
    _eventCleanup.set(elementId, cleanups);

    // gesturePropagation:false stops this element's bubbling pointer gestures from also reaching
    // gesture-enabled ancestors - the tap/pan counterpart of dragPropagation. Only pointerdown
    // needs stopping: it is the event that starts both tap and pan, and pointerenter/leave don't
    // bubble at all. Default true (motion.dev's default for tap/hover, unlike drag).
    const gesturePropagation = events.gesturePropagation !== false;
    const stopIfIsolated = (e: PointerEvent) => { if (!gesturePropagation) e.stopPropagation(); };

    //  Hover
    if (events.hover) {
        // A finger has no hover: touch fires pointerenter/leave around every tap, which would flash (or, on some
        // browsers, stick) the hover state on phones - motion.dev ignores touch for hover too. So does a pointer
        // passing over while it drags something else. Only a hover this element started is ended by it.
        let hovering = false;
        const onEnter = (e: PointerEvent) => {
            if (e.pointerType === 'touch' || _activeDrags > 0 || hovering) return;
            hovering = true;
            dotnetRef.invokeMethodAsync('OnPointerEnter');
        };
        const onLeave = (e: PointerEvent) => {
            if (!hovering || e.pointerType === 'touch') return;
            hovering = false;
            dotnetRef.invokeMethodAsync('OnPointerLeave');
        };
        el.addEventListener('pointerenter', onEnter);
        el.addEventListener('pointerleave', onLeave);
        cleanups.push(() => { el.removeEventListener('pointerenter', onEnter); el.removeEventListener('pointerleave', onLeave); });
    }

    //  Tap
    if (events.tap) {
        let pressing = false;
        const onDown = (e: PointerEvent) => {
            if (e.button !== 0 && e.pointerType !== 'touch') return; // primary button / touch only
            stopIfIsolated(e);
            pressing = true; dotnetRef.invokeMethodAsync('OnPointerDown');
        };
        const onUp   = (e: PointerEvent) => {
            if (e.button !== 0 && e.pointerType !== 'touch') return; // ignore non-primary releases
            if (!pressing) return; pressing = false;
            dotnetRef.invokeMethodAsync('OnPointerUp', el.contains(e.target as Node) || el === e.target);
        };
        const onCancel = () => { if (!pressing) return; pressing = false; dotnetRef.invokeMethodAsync('OnPointerCancel'); };

        // Keyboard accessibility (matches motion.dev): when the element is focused, Enter and
        // Space press and release it like a tap. Key state is tracked separately from pointer
        // state so a keyboard press can't cancel an in-flight pointer tap (and vice versa).
        // Only a key pressed on the element itself presses it: keys typed into a field inside a
        // tappable card, or pressed on a nested button, belong to that control.
        let keyPressing = false;
        const isTapKey = (e: KeyboardEvent) => e.key === 'Enter' || e.key === ' ';
        const onKeyDown = (e: KeyboardEvent) => {
            if (!isTapKey(e) || e.target !== el) return;
            // Space's default action scrolls the page on an element with no activation of its own;
            // suppress it there (repeats included, so a held press can't scroll either). A native
            // control activates on Space - a button clicks on its keyup - so its default stays.
            if (e.key === ' ' && !hasNativeSpaceActivation(el)) e.preventDefault();
            if (e.repeat || keyPressing) return;
            keyPressing = true;
            dotnetRef.invokeMethodAsync('OnPointerDown');
        };
        const onKeyUp = (e: KeyboardEvent) => {
            if (!isTapKey(e) || !keyPressing || e.target !== el) return;
            keyPressing = false;
            dotnetRef.invokeMethodAsync('OnPointerUp', true);
        };
        const onBlur = () => {
            if (!keyPressing) return; // focus lost mid-press ⇒ tap cancelled
            keyPressing = false;
            dotnetRef.invokeMethodAsync('OnPointerCancel');
        };

        el.addEventListener('pointerdown', onDown);
        window.addEventListener('pointerup',     onUp);
        window.addEventListener('pointercancel', onCancel);
        el.addEventListener('keydown', onKeyDown);
        el.addEventListener('keyup',   onKeyUp);
        el.addEventListener('blur',    onBlur);
        cleanups.push(() => {
            el.removeEventListener('pointerdown', onDown);
            window.removeEventListener('pointerup',     onUp);
            window.removeEventListener('pointercancel', onCancel);
            el.removeEventListener('keydown', onKeyDown);
            el.removeEventListener('keyup',   onKeyUp);
            el.removeEventListener('blur',    onBlur);
        });
    }

    //  Focus
    if (events.focus) {
        // motion.dev semantics: the focus gesture is keyboard focus - what the browser draws a focus ring for
        // (:focus-visible). A button clicked with the mouse takes focus too, but shows no ring, and plays no focus
        // overlay either.
        let focused = false;
        const onIn = (e: FocusEvent) => {
            if (!focusVisible(e.target as Element)) return;
            focused = true;
            dotnetRef.invokeMethodAsync('OnFocusIn');
        };
        const onOut = () => {
            if (!focused) return;
            focused = false;
            dotnetRef.invokeMethodAsync('OnFocusOut');
        };
        el.addEventListener('focusin',  onIn);
        el.addEventListener('focusout', onOut);
        cleanups.push(() => { el.removeEventListener('focusin', onIn); el.removeEventListener('focusout', onOut); });
    }

    //  Pan (detects movement ≥ 3px without moving the element)
    if (events.pan) {
        // When drag is also active it already calls setPointerCapture; let pan reuse that capture
        // instead of grabbing the pointer a second time for the same element.
        _attachPan(el, dotnetRef, cleanups, !!events.drag, stopIfIsolated);
    }

    //  Drag
    if (events.drag) {
        _attachDrag(elementId, el, events, dotnetRef, cleanups);
    }
}

function _attachPan(
    el: HTMLElement, dotnetRef: DotNet.DotNetObject, cleanups: Array<(unregistering: boolean) => void>, skipCapture: boolean,
    stopIfIsolated: (e: PointerEvent) => void): void {
    const PAN_THRESHOLD = 3; // pixels before pan is detected
    let down = false;        // whether a pointer is currently pressed on this element
    let panning = false;
    let startX = 0, startY = 0, lastX = 0, lastY = 0;
    const velocity = pointerVelocity();
    // One OnPanMove on its way at a time (a round trip on Blazor Server): the moves meanwhile arrive as one, their
    // deltas added up and the rest (position, offset, velocity) the latest - nothing queues behind the circuit.
    type PanMove = { x: number; y: number; deltaX: number; deltaY: number; offsetX: number; offsetY: number; velX: number; velY: number };
    const moves = coalesce<PanMove>(
        m => dotnetRef.invokeMethodAsync('OnPanMove', m.x, m.y, m.deltaX, m.deltaY, m.offsetX, m.offsetY, m.velX, m.velY),
        (a, b) => ({ ...b, deltaX: a.deltaX + b.deltaX, deltaY: a.deltaY + b.deltaY }));

    const onDown = (e: PointerEvent) => {
        if (e.button !== 0 && e.pointerType !== 'touch') return;
        stopIfIsolated(e);
        down = true;
        startX = lastX = e.clientX; startY = lastY = e.clientY;
        velocity.reset(e.clientX, e.clientY, _eventTime(e)); panning = false;
        // Skip when drag already owns the pointer capture for this element.
        if (!skipCapture) el.setPointerCapture(e.pointerId);
    };

    const onMove = (e: PointerEvent) => {
        // Ignore moves when no pointer is pressed (e.g. plain hover) so stale start
        // coordinates from a previous gesture can't trigger a phantom pan.
        if (!down) return;
        const dx = e.clientX - startX, dy = e.clientY - startY;
        const deltaX = e.clientX - lastX, deltaY = e.clientY - lastY;
        const t = _eventTime(e);
        velocity.add(e.clientX, e.clientY, t);
        const { x: velX, y: velY } = velocity.read(t, 1000);

        if (!panning && Math.sqrt(dx * dx + dy * dy) >= PAN_THRESHOLD) {
            panning = true;
            dotnetRef.invokeMethodAsync('OnPanStart_');
        }
        if (panning) moves.push({ x: e.clientX, y: e.clientY, deltaX, deltaY, offsetX: dx, offsetY: dy, velX, velY });

        lastX = e.clientX; lastY = e.clientY;
    };

    const onUp = () => {
        down = false;
        if (!panning) return;
        panning = false;
        moves.flush(); // the last movement arrives before the end
        dotnetRef.invokeMethodAsync('OnPanEnd_');
    };

    el.addEventListener('pointerdown',   onDown);
    el.addEventListener('pointermove',   onMove);
    el.addEventListener('pointerup',     onUp);
    el.addEventListener('pointercancel', onUp);
    cleanups.push(() => {
        el.removeEventListener('pointerdown',   onDown);
        el.removeEventListener('pointermove',   onMove);
        el.removeEventListener('pointerup',     onUp);
        el.removeEventListener('pointercancel', onUp);
    });
}

function _attachDrag(
    elementId: string, el: HTMLElement, opts: any, dotnetRef: DotNet.DotNetObject, cleanups: Array<(unregistering: boolean) => void>): void {
    // The release velocity goes to .NET in "px per frame" (~16ms), the figure the C# inertia driver's
    // release-velocity math works in.
    const FRAME_MS = 16;
    const axis           = opts.dragAxis        ?? null;
    const constraintsCfg = opts.dragConstraints ?? null;
    const dirLock        = !!opts.dragDirectionLock;
    const handleSel      = opts.dragHandle ?? null;
    // Default false (motion.dev semantics): a nested drag stops propagation so it doesn't also
    // start a draggable ancestor. Set dragPropagation to let both move together.
    const propagate      = !!opts.dragPropagation;
    // Without synchronous interop (Blazor Server) the start position is read off the element and
    // moves are reported through async calls, at most one per animation frame - the circuit can't
    // take a message per pointer move, and only the latest position matters anyway. The OnDragMove
    // callback rides in the same frame, after the position, so a handler always reads the position
    // it was raised for.
    const asyncMode      = !!opts.dragAsync;
    let pendingX = 0, pendingY = 0, reportFrame: number | null = null;
    // The last position sent this drag: once that call completes .NET has it, and every transform it writes after
    // follows from it.
    let lastSent: Promise<unknown> | null = null;
    const sendPendingMove = () => {
        lastSent = dotnetRef.invokeMethodAsync('SetDragPosition', pendingX, pendingY);
        dotnetRef.invokeMethodAsync('OnDragMove');
    };
    const reportMove = (x: number, y: number) => {
        if (!asyncMode) {
            // Synchronously, so ComputeFrame picks the position up this frame.
            dotnetRef.invokeMethod('SetDragPosition', x, y);
            dotnetRef.invokeMethodAsync('OnDragMove');
            return;
        }
        pendingX = x; pendingY = y;
        // Shown now, not a round trip from now (see _localDrags).
        const local = _localDrags.get(el);
        if (local) {
            local.x = x;
            local.y = y;
            _showLocalDrag(el);
        }
        if (reportFrame !== null) return;
        reportFrame = requestAnimationFrame(() => { reportFrame = null; sendPendingMove(); });
    };
    // Sends a move still waiting for its frame now, so the release is never processed before it.
    const flushPosition = () => {
        if (reportFrame === null) return;
        cancelAnimationFrame(reportFrame);
        reportFrame = null;
        sendPendingMove();
    };

    // Elasticity is per-edge ({ left, right, top, bottom }); a plain number still means uniform.
    const elasticCfg = opts.dragElastic;
    const elastic: BmElastic = typeof elasticCfg === 'number'
        ? { left: elasticCfg, right: elasticCfg, top: elasticCfg, bottom: elasticCfg }
        : (elasticCfg ?? { left: 0.35, right: 0.35, top: 0.35, bottom: 0.35 });

    // Static pixel bounds pass through unchanged; element-bounds configs (parent / selector) are
    // measured fresh at each drag start so layout changes between drags are picked up.
    let constraints: BmBounds | null = null;

    let dragging = false;
    let lockedAxis: 'x' | 'y' | null = null; // null = not yet locked, 'x' or 'y' once detected
    let startPX = 0, startPY = 0, startElX = 0, startElY = 0;
    const velocity = pointerVelocity();

    // Shared drag-start body: called by the element's own pointerdown listener and by the
    // external startDrag entry point (BmDragControls).
    const begin = (pointerId: number, clientX: number, clientY: number, time?: number) => {
        // A compositor (WAAPI) animation may own the transform; commit its current values inline
        // and cancel it so the drag takes over seamlessly. C# resolves its own state from the
        // mirrored plan inside GetCurrentXY below.
        _cancelWaapiForElement(elementId, true);
        // So does a layout (FLIP) animation in flight - a reordered row grabbed as it slides into its new place:
        // settled where it is heading, it stops projecting the old layout over the drag.
        const flip = _flips.get(elementId);
        if (flip) {
            flip.end();
            flip.anim.cancel();
        }
        // Retrieve starting transform position from C# state synchronously - or, without synchronous
        // interop, from the element itself (just committed above, so it is what's on screen), letting
        // C# fold the cancelled compositor animations back into its state in the background.
        let pos: BmXY | null;
        if (asyncMode) {
            // Where it shows: the transform, plus what a drag shown locally still adds before .NET has caught up.
            pos = _shownTranslation(el);
            _localDrags.set(el, { x: pos.x, y: pos.y, dragging: true });
            lastSent = null;
            dotnetRef.invokeMethodAsync('GetCurrentXY');
        } else {
            pos = dotnetRef.invokeMethod<BmXY | null>('GetCurrentXY');
        }
        startElX = pos ? pos.x : 0;
        startElY = pos ? pos.y : 0;
        startPX = lastCX = clientX; startPY = lastCY = clientY;
        velocity.reset(clientX, clientY, time ?? performance.now());
        scrollShift = { x: 0, y: 0 };
        layoutStart = layoutCentre();
        scrollLimits = _scrollLimits(el);
        window.addEventListener('scroll', onScroll, { capture: true, passive: true });
        if (!dragging) _activeDrags++;
        dragging = true;
        _guardNativeGestures();
        lockedAxis = null;
        constraints = _resolveDragConstraints(constraintsCfg, el, startElX, startElY);
        // Re-measure element-bounds constraints if the layout changes mid-drag (container resize
        // or scroll). Static pixel bounds have nothing to re-measure, so skip the observers.
        _observeConstraintChanges();
        // Capture on the target element so move/up events route here even when the drag was
        // started from another element. May throw for an already-released pointer.
        try { el.setPointerCapture(pointerId); } catch { /* stale pointer - drag still tracks moves */ }
        dotnetRef.invokeMethodAsync('OnPointerDown_Drag');
    };

    // Live constraint re-measurement (element-bounds only), active only while dragging.
    let _resizeObserver: ResizeObserver | null = null;
    // The rect measured now includes the translation the drag has applied so far, so back out that
    // one - as rendered, read off the element, a drag shown locally included - not the one the drag
    // started from. (The ResizeObserver reports once as soon as it observes, so this runs on every
    // element-bounds drag.)
    const _remeasure = () => {
        if (!dragging) return;
        const cur = _shownTranslation(el);
        constraints = _resolveDragConstraints(constraintsCfg, el, cur.x, cur.y);
    };
    const _needsRemeasure = () =>
        !!constraintsCfg && (!!constraintsCfg.parent || typeof constraintsCfg.selector === 'string');
    function _observeConstraintChanges(): void {
        if (!_needsRemeasure() || typeof ResizeObserver !== 'function') return;
        // A programmatic begin() can re-arm the observers mid-drag (no intervening onUp); tear the
        // previous ones down first so repeated begins don't leak observers or duplicate listeners.
        _stopObservingConstraintChanges();
        _resizeObserver = new ResizeObserver(_remeasure);
        _resizeObserver.observe(el);
        // Observe the SAME container _resolveDragConstraints measures against - the parent element
        // for { parent: true }, or the selector target (which may be a distant ancestor, not
        // el.parentElement) for { selector }. Otherwise a mid-drag resize of a selector container
        // wouldn't re-measure the bounds.
        const container = constraintsCfg.parent ? el.parentElement : document.querySelector(constraintsCfg.selector);
        if (container) _resizeObserver.observe(container);
        window.addEventListener('scroll', _remeasure, true);
        window.addEventListener('resize', _remeasure);
    }
    function _stopObservingConstraintChanges(): void {
        if (_resizeObserver) { _resizeObserver.disconnect(); _resizeObserver = null; }
        window.removeEventListener('scroll', _remeasure, true);
        window.removeEventListener('resize', _remeasure);
    }

    const onDown = (e: PointerEvent) => {
        if (e.button !== 0 && e.pointerType !== 'touch') return;
        // A press in a form field inside the draggable is for the field - placing a caret, selecting text, opening a
        // select - not the start of a drag (motion.dev semantics). Buttons and links still drag.
        if (e.target instanceof Element) {
            const field = e.target.closest('input, select, textarea, [contenteditable]:not([contenteditable="false"])');
            if (field && el.contains(field)) return;
        }
        // Handle mode: only start when the press lands on (or inside) a matching descendant.
        if (handleSel && !(e.target instanceof Element && e.target.closest(handleSel))) return;
        // Isolate nested draggables unless propagation is opted in (motion.dev's dragPropagation).
        if (!propagate) e.stopPropagation();
        begin(e.pointerId, e.clientX, e.clientY, _eventTime(e));
    };

    const onMove = (e: PointerEvent) => {
        if (!dragging) return;
        velocity.add(e.clientX, e.clientY, _eventTime(e));
        lastCX = e.clientX; lastCY = e.clientY;

        // Direction lock detection
        if (dirLock && !lockedAxis) {
            const dx = Math.abs(e.clientX - startPX), dy = Math.abs(e.clientY - startPY);
            if (dx > 3 || dy > 3) {
                lockedAxis = dx >= dy ? 'x' : 'y';
                // Report the decision once, the moment it is made (motion.dev's onDirectionLock),
                // so the consumer can e.g. hand the other axis back to the page scroll.
                dotnetRef.invokeMethodAsync('OnDirectionLocked', lockedAxis);
            }
        }

        moveTo(e.clientX, e.clientY);
        startAutoScroll();
    };

    // Where the pointer at (clientX, clientY) puts the element, constrained, and on to .NET.
    const moveTo = (clientX: number, clientY: number) => {
        const effectiveAxis = dirLock && lockedAxis ? lockedAxis : axis;
        let x = startElX + (effectiveAxis === 'y' ? 0 : clientX - startPX + scrollShift.x);
        let y = startElY + (effectiveAxis === 'x' ? 0 : clientY - startPY + scrollShift.y);

        if (constraints) {
            const clamped = clampToConstraints(x, y, constraints, elastic);
            x = clamped.x; y = clamped.y;
        }

        reportMove(x, y);
    };

    // A scroll during the drag - of the page, of any container the element is in - carries its layout box away
    // from a pointer that hasn't moved. The drag adds the shift back, measured off the box itself (so whatever
    // scrolled, and an element a scroll doesn't move, come out right): the element stays under the pointer.
    let lastCX = 0, lastCY = 0;
    let layoutStart: BmXY = { x: 0, y: 0 }, scrollShift: BmXY = { x: 0, y: 0 };
    const layoutCentre = (): BmXY => {
        const r = el.getBoundingClientRect(), t = _shownTranslation(el);
        return { x: r.left + (r.width / 2) - t.x, y: r.top + (r.height / 2) - t.y };
    };
    const onScroll = () => {
        if (!dragging) return;
        _holdWithinScrollLimits(scrollLimits);
        const now = layoutCentre();
        scrollShift = { x: layoutStart.x - now.x, y: layoutStart.y - now.y };
        moveTo(lastCX, lastCY);
    };

    // With dragAutoScroll the nearest container that can scroll on scrolls while the pointer is near its edge, frame
    // after frame - a pointer held there keeps it going - carrying the element (see onScroll) along with it.
    const autoScroll = !!opts.dragAutoScroll;
    let autoScrolling = false;
    let scrollLimits = new Map<Element, BmXY>();
    const autoScrollTick = () => {
        if (!dragging || !_autoScrollStep(el, lastCX, lastCY, dirLock && lockedAxis ? lockedAxis : axis, scrollLimits)) {
            autoScrolling = false;
            return;
        }
        // At once, not on the scroll event a frame later: the element would trail a step behind all the way. Before
        // the frame's ComputeFrame, which moves it there in the same frame.
        onScroll();
        _requestBeforeFrame(autoScrollTick);
    };
    const startAutoScroll = () => {
        if (!autoScroll || autoScrolling) return;
        autoScrolling = true;
        _requestBeforeFrame(autoScrollTick);
    };
    const stopAutoScroll = () => {
        autoScrolling = false;
        _beforeFrame.delete(autoScrollTick);
    };

    const onUp = (e?: PointerEvent) => {
        if (!dragging) return;
        dragging = false;
        _activeDrags = Math.max(0, _activeDrags - 1);
        // Shown where it was let go until .NET's transform gets there - or, once .NET has the final
        // position, until whatever it writes next (the release may take the element elsewhere at once).
        const local = _localDrags.get(el);
        if (local) {
            local.dragging = false;
            flushPosition();
            const settle = () => {
                if (_localDrags.get(el) !== local) return; // picked up again meanwhile
                local.settled = true;
                _showLocalDrag(el);
            };
            (lastSent ?? Promise.resolve()).then(settle, settle);
        }
        else flushPosition();
        _stopObservingConstraintChanges();
        window.removeEventListener('scroll', onScroll, true);
        stopAutoScroll();
        // Report the constraints actually used for this drag so C#'s release logic (momentum
        // clamping / snap-back) uses the same bounds - essential for element-bounds constraints,
        // which only exist in resolved form here.
        const c = constraints;
        const vel = velocity.read(e ? _eventTime(e) : performance.now(), FRAME_MS);
        dotnetRef.invokeMethodAsync('OnPointerUp_Drag', vel.x, vel.y,
            c?.left ?? null, c?.right ?? null, c?.top ?? null, c?.bottom ?? null);
    };

    // With a drag handle the grab cursor belongs on the handle (styled by the consumer), not
    // the whole element; with the listener disabled the element isn't grabbable at all.
    if (!handleSel && opts.dragListener !== false) el.style.cursor = 'grab';
    el.style.userSelect = 'none';
    el.style.touchAction = axis === 'x' ? 'pan-y' : axis === 'y' ? 'pan-x' : 'none';
    // A draggable element moves by transform on purpose; scroll anchoring must not follow it. Firefox anchors on the
    // dragged row itself and scrolls after it, which the drag (keeping the row under the pointer) then answers.
    el.style.overflowAnchor = 'none';

    if (opts.dragListener !== false) el.addEventListener('pointerdown', onDown);
    el.addEventListener('pointermove',   onMove);
    el.addEventListener('pointerup',     onUp);
    el.addEventListener('pointercancel', onUp);
    _dragStarters.set(elementId, begin);
    cleanups.push(unregistering => {
        // Listeners are re-attached whenever the gesture configuration changes, which can happen
        // mid-drag: end that drag first, or C# would be left dragging (and WhileDrag applied) forever.
        // On unregistration the component is going away, so there is no drag end to report.
        if (unregistering) {
            if (dragging) _activeDrags = Math.max(0, _activeDrags - 1);
            dragging = false;
        }
        else onUp();
        el.removeEventListener('pointerdown',   onDown);
        el.removeEventListener('pointermove',   onMove);
        el.removeEventListener('pointerup',     onUp);
        el.removeEventListener('pointercancel', onUp);
        if (reportFrame !== null) { cancelAnimationFrame(reportFrame); reportFrame = null; }
        if (unregistering && _localDrags.delete(el)) el.style.translate = '';
        _stopObservingConstraintChanges();
        window.removeEventListener('scroll', onScroll, true);
        stopAutoScroll();
        _dragStarters.delete(elementId);
        el.style.cursor = el.style.userSelect = el.style.touchAction = el.style.overflowAnchor = '';
    });
}

const AUTO_SCROLL_EDGE = 48; // px inside a scroll container's edge where a drag starts scrolling it
const AUTO_SCROLL_MAX = 20;  // px per frame with the pointer on the edge, or past it

/**
 * How far a drag auto-scrolls this frame along one axis, for a pointer at <paramref name="pointer"/> in a container
 * spanning [<paramref name="start"/>, <paramref name="end"/>]: towards the nearer edge once within
 * <paramref name="edge"/> of it (a quarter of the container at most), faster the closer, at <paramref name="max"/>
 * on the edge and past it. Zero away from the edges.
 */
export function autoScrollSpeed(pointer: number, start: number, end: number, edge = AUTO_SCROLL_EDGE, max = AUTO_SCROLL_MAX): number {
    const zone = Math.min(edge, (end - start) / 4);
    if (!(zone > 0)) return 0;
    if (pointer - start < zone) return -max * Math.min(1, (zone - (pointer - start)) / zone);
    if (end - pointer < zone) return max * Math.min(1, (zone - (end - pointer)) / zone);
    return 0;
}

// How far each container around the element - and the page - can scroll, as the drag starts: the dragged element's
// transform counts towards its scroller's overflow, so carried past the end it would make room for itself and a
// scroll that followed it would never stop.
function _scrollLimits(el: HTMLElement): Map<Element, BmXY> {
    const limits = new Map<Element, BmXY>();
    const root = (document.scrollingElement ?? document.documentElement) as HTMLElement;
    for (let node: HTMLElement | null = el.parentElement; node; node = node.parentElement)
        limits.set(node, { x: node.scrollWidth - node.clientWidth, y: node.scrollHeight - node.clientHeight });
    limits.set(root, { x: root.scrollWidth - root.clientWidth, y: root.scrollHeight - root.clientHeight });
    return limits;
}

// During a drag, a container scrolled past where it could scroll as the drag started has only scrolled into room the
// dragged element's own transform made - and the drag, following the scroll, would make more. WebKit's own autoscroll
// under a held mouse does exactly that, all the way down an endless list. Held at the limit, before anything paints
// (scroll events come first in a frame).
function _holdWithinScrollLimits(limits: Map<Element, BmXY>): void {
    for (const [node, limit] of limits) {
        if (node.scrollTop > limit.y + 0.5) node.scrollTop = limit.y;
        if (node.scrollLeft > limit.x + 0.5) node.scrollLeft = limit.x;
    }
}

// One frame of a drag's auto-scroll: the nearest container around the element - an overflowing scroller, then the
// page - with the pointer near one of its edges and room to scroll that way (within limits) scrolls a step. Whether
// one did.
function _autoScrollStep(el: HTMLElement, cx: number, cy: number, axis: 'x' | 'y' | null, limits: Map<Element, BmXY>): boolean {
    const root = (document.scrollingElement ?? document.documentElement) as HTMLElement;
    for (let node = el.parentElement; node; node = node.parentElement) {
        const isRoot = node === root || node === document.body || node === document.documentElement;
        if (isRoot) node = root;
        const limit = limits.get(node) ?? { x: node.scrollWidth - node.clientWidth, y: node.scrollHeight - node.clientHeight };
        let left: number, top: number, right: number, bottom: number;
        if (isRoot) {
            left = 0; top = 0; right = window.innerWidth; bottom = window.innerHeight;
        } else {
            const cs = getComputedStyle(node);
            const scrolls = (o: string) => o === 'auto' || o === 'scroll' || o === 'overlay';
            if (!scrolls(cs.overflowX) && !scrolls(cs.overflowY)) continue;
            ({ left, top, right, bottom } = node.getBoundingClientRect());
        }
        let dx = axis === 'y' ? 0 : autoScrollSpeed(cx, left, right);
        let dy = axis === 'x' ? 0 : autoScrollSpeed(cy, top, bottom);
        // Only towards where there is more to see - and never back the other way, from past a limit.
        dx = dx > 0 ? Math.min(dx, Math.max(0, limit.x - node.scrollLeft)) : Math.max(dx, -node.scrollLeft);
        dy = dy > 0 ? Math.min(dy, Math.max(0, limit.y - node.scrollTop)) : Math.max(dy, -node.scrollTop);
        if (Math.abs(dx) < 0.5) dx = 0;
        if (Math.abs(dy) < 0.5) dy = 0;
        if (dx !== 0 || dy !== 0) {
            node.scrollBy(dx, dy);
            return true;
        }
        if (isRoot) break;
    }
    return false;
}

/** The translation an element shows right now, read off its computed transform. */
function _currentTranslation(el: HTMLElement): BmXY {
    const transform = getComputedStyle(el).transform;
    if (!transform || transform === 'none') return { x: 0, y: 0 };
    const m = new DOMMatrixReadOnly(transform);
    return { x: m.m41, y: m.m42 };
}

const _dragStarters = new Map<string, (pointerId: number, clientX: number, clientY: number) => void>(); // elementId → begin(pointerId, clientX, clientY)

/**
 * Starts a drag on a drag-enabled element from an external pointer event
 * (BmDragControls.StartAsync). No-op when the element has no drag attached.
 */
export function startDrag(elementId: string, pointerId: number, clientX: number, clientY: number): void {
    const begin = _dragStarters.get(elementId);
    if (begin) begin(pointerId, clientX, clientY);
}

/**
 * Pops an element out of the layout flow for a popLayout exit: pins it with
 * position:absolute at its current visual spot (relative to its offsetParent) so siblings
 * reflow immediately while the exit animation plays. curX/curY are the element's current
 * transform translation, backed out of the measured rect so the still-applied transform
 * doesn't double-offset the pinned position.
 */
const _popped = new Map<string, BmStyleMap>(); // elementId → prior inline style values (for un-popping on revival)

export function popLayout(elementId: string, curX: number, curY: number): void {
    const el = document.getElementById(elementId);
    if (!el || _popped.has(elementId)) return;
    const rect = el.getBoundingClientRect();
    const parent = el.offsetParent || document.body;
    const pRect = parent.getBoundingClientRect();
    // Save the prior inline values so a revival (item re-added mid-exit) can restore them.
    const s = el.style;
    _popped.set(elementId, {
        position: s.position, left: s.left, top: s.top,
        width: s.width, height: s.height, margin: s.margin, boxSizing: s.boxSizing,
    });
    s.width  = rect.width + 'px';
    s.height = rect.height + 'px';
    s.boxSizing = 'border-box';
    s.margin = '0';
    s.position = 'absolute';
    // Absolute positioning is relative to the offsetParent's padding box (border edge + clientLeft/Top).
    s.left = (rect.left - pRect.left - parent.clientLeft - curX) + 'px';
    s.top  = (rect.top  - pRect.top  - parent.clientTop  - curY) + 'px';
}

// ── Keyboard reordering (BmotionReorderGroup) ───────────────────────────────

/** An item's keyboard reordering: whether it is held, whether it was made focusable for it, and its listeners. */
interface BmReorderKeys {
    held: boolean;
    madeFocusable: boolean;
    unlisten(): void;
}

const _reorderKeys = new WeakMap<Element, BmReorderKeys>();

/**
 * Keyboard reordering for one item of a BmotionReorderGroup: Space or Enter picks it up and drops it, the arrow keys
 * along the list's axis move it while it is held, Escape puts it back, and focus leaving it drops it. Only keys on
 * the item itself (or its drag handle) count - a field or a button inside it keeps its own. The item is made
 * focusable when it isn't. Attaching again (the axis or the handle changed) replaces its listeners with ones on the
 * new settings, the item still held if it was.
 */
export function attachReorderKeys(elementId: string, dotnetRef: DotNet.DotNetObject, axis: 'x' | 'y' | 'both', handleSelector: string | null): void {
    const el = document.getElementById(elementId);
    if (!el) return;
    const previous = _reorderKeys.get(el);
    previous?.unlisten();
    const madeFocusable = previous ? previous.madeFocusable : !el.hasAttribute('tabindex') && el.tabIndex < 0;
    if (madeFocusable && !previous) el.setAttribute('tabindex', '0');

    // Along the list; in a grid left and right move a cell, and up and down a row.
    const back = axis === 'y' ? 'ArrowUp' : 'ArrowLeft';
    const forward = axis === 'y' ? 'ArrowDown' : 'ArrowRight';
    const ours = (target: EventTarget | null) =>
        target === el || (!!handleSelector && target instanceof Element && target.matches(handleSelector) && el.contains(target));

    const onKeyDown = (e: KeyboardEvent) => {
        if (e.altKey || e.ctrlKey || e.metaKey || !ours(e.target)) return;
        if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            if (keys.held) {
                keys.held = false;
                dotnetRef.invokeMethodAsync('OnReorderDrop', elementId);
            } else {
                // Held until .NET says otherwise (another item, or a pointer drag, owns the list).
                keys.held = true;
                dotnetRef.invokeMethodAsync('OnReorderPickUp', elementId).then((ok: boolean) => { if (!ok) keys.held = false; }, () => { keys.held = false; });
            }
            return;
        }
        if (!keys.held) return;
        if (e.key === back || e.key === forward) {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnReorderMove', elementId, e.key === forward ? 1 : -1);
        } else if (axis === 'both' && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnReorderMoveRow', elementId, e.key === 'ArrowDown' ? 1 : -1);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            keys.held = false;
            dotnetRef.invokeMethodAsync('OnReorderCancel', elementId);
        }
    };
    const onFocusOut = (e: FocusEvent) => {
        if (!keys.held || el.contains(e.relatedTarget as Node | null)) return;
        keys.held = false;
        dotnetRef.invokeMethodAsync('OnReorderDrop', elementId);
    };
    const keys: BmReorderKeys = {
        held: previous?.held ?? false,
        madeFocusable,
        unlisten: () => {
            el.removeEventListener('keydown', onKeyDown);
            el.removeEventListener('focusout', onFocusOut);
        },
    };
    el.addEventListener('keydown', onKeyDown);
    el.addEventListener('focusout', onFocusOut);
    _reorderKeys.set(el, keys);
}

/** Takes keyboard reordering off the item again (attachReorderKeys), and the focusability it gave the item. */
export function detachReorderKeys(elementId: string): void {
    const el = document.getElementById(elementId);
    const keys = el ? _reorderKeys.get(el) : undefined;
    if (!el || !keys) return;
    keys.unlisten();
    _reorderKeys.delete(el);
    if (keys.madeFocusable) el.removeAttribute('tabindex');
}

/** Moves focus to the element without scrolling it into view. */
export function focusElement(elementId: string): void {
    document.getElementById(elementId)?.focus({ preventScroll: true });
}

let _liveRegion: HTMLElement | null = null;

/**
 * Has screen readers announce <paramref name="text"/>: one polite, visually hidden live region at the end of the
 * page, made on first use. Cleared and refilled on the next frame, so the same words twice are announced twice.
 */
export function announce(text: string): void {
    if (!_liveRegion || !_liveRegion.isConnected) {
        _liveRegion = document.createElement('div');
        _liveRegion.setAttribute('role', 'status');
        _liveRegion.setAttribute('aria-live', 'polite');
        _liveRegion.setAttribute('aria-atomic', 'true');
        _liveRegion.setAttribute('data-bm-announcer', '');
        Object.assign(_liveRegion.style, {
            position: 'absolute', width: '1px', height: '1px', margin: '-1px', padding: '0', border: '0',
            overflow: 'hidden', clip: 'rect(0 0 0 0)', whiteSpace: 'nowrap',
        });
        document.body.appendChild(_liveRegion);
    }
    const region = _liveRegion;
    region.textContent = '';
    requestAnimationFrame(() => { region.textContent = text; });
}

/** Whether <paramref name="el"/> shows keyboard focus (:focus-visible); true where the browser can't tell. */
export function focusVisible(el: Element | null): boolean {
    if (!el || typeof el.matches !== 'function') return true;
    try { return el.matches(':focus-visible'); }
    catch { return true; /* a browser without :focus-visible: every focus counts, as it always did */ }
}

/**
 * Makes an exiting element inert - nothing in it can be focused, clicked or read out, and pointer events reach what
 * is behind it - or, when its exit is cancelled, interactive again. Only an inert this function added is removed, so
 * one the app set itself stays.
 */
export function setInert(elementId: string, inert: boolean): void {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (inert) {
        if (el.hasAttribute('inert')) return;
        el.setAttribute('inert', '');
        el.setAttribute('data-bm-inert', '');
    } else if (el.hasAttribute('data-bm-inert')) {
        el.removeAttribute('inert');
        el.removeAttribute('data-bm-inert');
    }
}

/** Restores the inline styles replaced by popLayout (used when an exit is cancelled). */
export function unpopLayout(elementId: string): void {
    const el = document.getElementById(elementId);
    const prev = _popped.get(elementId);
    _popped.delete(elementId);
    if (!el || !prev) return;
    for (const [prop, value] of Object.entries(prev)) el.style[prop as any] = value;
}

/**
 * Resolves a drag-constraints config into pixel bounds on the element's transform x/y.
 * Static pixel configs pass through; element-bounds configs ({ parent: true } or
 * { selector: "..." }) are measured now: the allowed transform range is the container rect
 * minus the element's untransformed rect (its current rect with the active x/y backed out).
 */
function _resolveDragConstraints(cfg: any, el: HTMLElement, curX: number, curY: number): BmBounds | null {
    if (!cfg) return null;
    if (!cfg.parent && !cfg.selector) return cfg;

    const container = cfg.parent ? el.parentElement : document.querySelector(cfg.selector);
    if (!container) return null;

    const cRect = container.getBoundingClientRect();
    const eRect = el.getBoundingClientRect();
    return resolveConstraintBounds(cRect, eRect, curX, curY);
}

//
// WAAPI compositor offload
// The C# engine pre-samples eligible curves (tweens, zero-velocity springs on transform/opacity)
// and hands the browser a ready-made Web Animation, so playback runs off the main thread with
// no per-frame interop. The engine keeps a mirror "plan" so it can compute current values for
// interruption without reading the DOM.
//

const _waapiAnims = new Map<string, Map<string, Animation>>(); // elementId → Map<token, Animation>

/** True when the browser supports the linear() easing function (needed for spring offload). */
export function supportsLinearEasing(): boolean {
    try {
        return typeof CSS !== 'undefined' &&
            CSS.supports?.('animation-timing-function', 'linear(0, 1)') === true;
    } catch {
        return false;
    }
}

/**
 * True when the browser animates between oklab() colours in OKLab (an OKLab colour transition then plays in the
 * browser). Parsing oklab() is not enough: Firefox parses it but still interpolates the keyframes in sRGB, so
 * this plays red to blue on a probe and looks at the midpoint - OKLab's has green in it, sRGB's has none.
 */
export function supportsOklabColor(): boolean {
    let probe: HTMLElement | undefined;
    try {
        if (typeof CSS === 'undefined' || CSS.supports?.('color', 'oklab(0.5 0 0 / 0.5)') !== true) return false;
        probe = document.createElement('div');
        probe.style.cssText = 'position:fixed;width:0;height:0;visibility:hidden;pointer-events:none';
        document.body.appendChild(probe);
        const anim = probe.animate(
            [{ backgroundColor: 'oklab(0.628 0.2249 0.1258)' }, { backgroundColor: 'oklab(0.452 -0.0325 -0.3115)' }],
            { duration: 1000, fill: 'both' });
        anim.pause();
        anim.currentTime = 500;
        const mid = getComputedStyle(probe).backgroundColor;
        anim.cancel();
        if (mid.startsWith('oklab(')) return true;
        // Reported in sRGB: rgb() channels are 0-255, color(srgb ...) ones 0-1.
        const channels = mid.match(/-?\d*\.?\d+(?:e-?\d+)?/gi)?.map(Number) ?? [];
        if (channels.length < 3) return false;
        const green = mid.startsWith('color(') ? channels[1] : channels[1] / 255;
        return green > 0.1;
    } catch {
        return false;
    } finally {
        probe?.remove();
    }
}

/**
 * Plays a pre-sampled animation via element.animate(). Resolves true when the animation
 * finishes naturally (styles committed inline), false when it is cancelled or fails to start.
 * timing.iterations: -1 means Infinity.
 */
export function playWaapiAnimation(elementId: string, token: string, keyframes: any, timing: any): Promise<boolean> {
    const el = document.getElementById(elementId);
    if (!el || typeof el.animate !== 'function') return Promise.resolve(false);
    _endLocalDrag(el);

    let anim: Animation;
    try {
        anim = el.animate(keyframes, {
            duration: timing.duration,
            delay: timing.delay ?? 0,
            easing: timing.easing || 'linear',
            iterations: timing.iterations < 0 ? Infinity : (timing.iterations ?? 1),
            direction: timing.direction || 'normal',
            fill: 'both',
        });
    } catch (e) {
        // Unsupported easing/keyframes on this browser - the C# engine falls back to its rAF path.
        return Promise.resolve(false);
    }
    // Started on an element that is paused or re-timed (Blazor Server, where C# can't move it to
    // its frame loop instead).
    if (typeof timing.playbackRate === 'number') anim.playbackRate = timing.playbackRate;
    // Started the moment the engine sampled its starting point rather than on the next frame - unless this task has
    // run so long that the moment is gone (waapiStartTime), and then on its first frame, with the rest of the task's.
    const frame = document.timeline?.currentTime;
    const frameTime = frame == null ? null : Number(frame);
    const start = waapiStartTime(performance.now(), frameTime, !!timing.continues, _interruptedAt.get(elementId));
    _interruptedAt.delete(elementId);
    try {
        if (start !== null && frameTime !== null) anim.startTime = start;
    } catch { /* an inactive timeline: the animation starts on the next frame as usual */ }
    if (!timing.continues && frameTime !== null) _startWithTask(anim, frameTime, start === null);

    let map = _waapiAnims.get(elementId);
    if (!map) { map = new Map(); _waapiAnims.set(elementId, map); }
    map.set(token, anim);

    const cleanup = () => {
        const m = _waapiAnims.get(elementId);
        if (m) { m.delete(token); if (m.size === 0) _waapiAnims.delete(elementId); }
    };

    return anim.finished.then(
        () => {
            cleanup();
            // Persist the final values inline before releasing the effect, so nothing flashes.
            try { commitStylesChecked(anim); } catch { /* detached element */ }
            anim.cancel();
            return true;
        },
        () => { cleanup(); return false; } // cancelled (interrupted) - engine already reseeded state
    );
}

/**
 * anim.commitStyles(), checked. WebKit can return from it without writing anything (Playwright's WebKit 26, build
 * 2370: one letter of a staggered split text, every time) - and the animation, cancelled next, takes what it showed
 * with it. When the inline style is unchanged afterwards, the animated properties are written by hand: a finished
 * animation's from the keyframe it ended on, as the engine wrote them; one stopped part-way from what it shows.
 * Throws as commitStyles does (a detached element).
 */
export function commitStylesChecked(anim: Animation): void {
    const effect = anim.effect as KeyframeEffect | null;
    const el = (effect?.target ?? null) as (Element & ElementCSSInlineStyle) | null;
    const before = el?.getAttribute('style') ?? null;
    anim.commitStyles();
    if (!effect || !el || el.getAttribute('style') !== before) return;
    const keyframes = effect.getKeyframes();
    const progress = effect.getComputedTiming().progress;
    const ended = keyframes.find(k => k.computedOffset === progress);
    let shown: CSSStyleDeclaration | null = null;
    for (const key of new Set(keyframes.flatMap(k => Object.keys(k)))) {
        if (key === 'offset' || key === 'computedOffset' || key === 'easing' || key === 'composite') continue;
        const prop = key.startsWith('--') ? key : key === 'cssFloat' ? 'float' : key.replace(/[A-Z]/g, c => '-' + c.toLowerCase());
        let value = ended?.[key];
        if (typeof value !== 'string' || !value) value = (shown ??= getComputedStyle(el)).getPropertyValue(prop);
        if (value) el.style.setProperty(prop, value);
    }
}

/** Sets the playback rate of every offloaded animation on the element (1 = realtime, 0 = paused). */
/** Moves every animation playing on the element to playedMs of its own time (delay included): a seek. */
export function seekWaapiAnimations(elementId: string, playedMs: number): void {
    const map = _waapiAnims.get(elementId);
    if (!map) return;
    for (const anim of map.values()) anim.currentTime = playedMs;
}

export function setWaapiPlaybackRate(elementId: string, rate: number): void {
    const map = _waapiAnims.get(elementId);
    if (!map) return;
    for (const anim of map.values()) anim.playbackRate = rate;
}

// When each element's animations were last interrupted to hand over to a new one (performance.now()): the instant
// the engine sampled the values that one starts from.
const _interruptedAt = new Map<string, number>();

// How far past the last frame a task has to be before a new animation waits for the next one (waapiStartTime): past
// a click's own few milliseconds, short of any batch long enough to eat into a short animation.
const LATE_START_MS = 50;

/**
 * When a Web Animation created at <c>now</c> starts on the document timeline, or null for the frame that first shows
 * it. Normally at once - the moment the engine sampled its starting point, not on the next frame: a new animation is
 * otherwise held on its first keyframe for a whole frame, and when it takes over from one in flight that frame of
 * standing still is a visible kink. A continuation starts from the instant the animation it carries on was
 * interrupted (when the engine read the values it starts from), however long building it took.
 * <para>
 * But a task that has run long by <c>now</c> - building a whole batch, a staggered ripple of dozens of elements -
 * shows nothing until it ends, while performance.now() runs on (the timeline's clock only moves with a frame).
 * Backdated to then, a new animation would have played that long before its first frame, and a short one created
 * early in the batch would never be seen at all: those start on their first frame instead. A continuation keeps its
 * instant whatever the wait - it carries on a motion that was on screen.
 * </para>
 */
export function waapiStartTime(now: number, frameTime: number | null, continues: boolean, interruptedAt?: number): number | null {
    if (continues) return interruptedAt !== undefined && interruptedAt <= now ? interruptedAt : now;
    return frameTime !== null && now - frameTime > LATE_START_MS ? null : now;
}

/**
 * How far an offloaded animation has played, in ms of its own time (its delay included), as of <c>now</c>
 * (performance.now()) - not as of the frame the timeline last stood at, which inside a long task is well behind: the
 * moment the engine reads it is the moment the animation taking over starts from (waapiStartTime). One that is held on
 * its first keyframe (a late start), paused, playing at rate 0 or not started yet is where its current time says.
 */
export function waapiPlayedAt(now: number, startTime: number | null, paused: boolean, currentTime: number | null, rate: number): number {
    if (paused || startTime === null || rate === 0) return currentTime ?? 0;
    return Math.max(0, (now - startTime) * rate);
}

/**
 * How far the browser has really played an offloaded animation (waapiPlayedAt), or null when there is no such
 * animation. The engine's own clock for it starts when it built it, which a late start (waapiStartTime) can leave
 * well ahead of what the element shows - an element's first animation after the page loads usually lands in a task
 * that long - so before it reads the values an animation taking over starts from, it asks here (synchronously, so on
 * WebAssembly only).
 */
export function waapiPlayedMs(elementId: string, token: string): number | null {
    const anim = _waapiAnims.get(elementId)?.get(token);
    if (!anim) return null;
    const start = anim.startTime, current = anim.currentTime;
    return waapiPlayedAt(performance.now(), start === null ? null : Number(start), anim.playState === 'paused',
        current === null ? null : Number(current), anim.playbackRate);
}

// The new animations the current task has started - one task as long as the timeline's time hasn't moved - and
// whether it has turned out long.
let _taskFrame: number | null = null;
let _taskAnims: Animation[] = [];
let _taskLate = false;

// The late animations held on their first keyframe until the frame that shows them (_startWithTask).
const _heldLate = new WeakSet<Animation>();

/**
 * A task that turns out long (waapiStartTime) starts every new animation it created on the frame that first shows
 * them, the early ones included: those were started at once, before the task was known to be long, and left so they
 * would run that far ahead of the rest - a stagger's first delays eaten, its first elements arriving together with the
 * next. A late one is held (paused) on its first keyframe until then and played from there: left pending instead,
 * WebKit would resolve its start to the time the task began in and show that first frame far along. An early one is
 * moved to the real time of that frame - not the timeline's, which Chromium leaves at a vsync from inside the task.
 */
function _startWithTask(anim: Animation, frameTime: number, late: boolean): void {
    if (frameTime !== _taskFrame) {
        _taskFrame = frameTime;
        _taskAnims = [];
        _taskLate = false;
    }
    _taskAnims.push(anim);
    if (!late) return;
    try {
        anim.pause();
        _heldLate.add(anim);
    } catch { /* not pausable: it starts when the browser starts it */ }
    if (_taskLate) return;
    _taskLate = true;
    const anims = _taskAnims;
    requestAnimationFrame(() => {
        const at = performance.now();
        for (const a of anims) {
            try {
                if (_heldLate.delete(a)) { if (a.playState === 'paused') a.play(); }
                else if (a.playState === 'running') a.startTime = at;
            } catch { /* gone */ }
        }
    });
}

/** Cancels one offloaded animation; commit=true snapshots current values inline first. */
export function cancelWaapiAnimation(elementId: string, token: string, commit: boolean): void {
    const anim = _waapiAnims.get(elementId)?.get(token);
    if (!anim) return;
    if (commit) _interruptedAt.set(elementId, performance.now());
    if (commit) { try { commitStylesChecked(anim); } catch { /* detached element */ } }
    anim.cancel(); // rejects anim.finished → cleanup runs in playWaapiAnimation's handler
}

function _cancelWaapiForElement(elementId: string, commit: boolean): void {
    const map = _waapiAnims.get(elementId);
    if (!map) return;
    for (const anim of [...map.values()]) {
        if (commit) { try { commitStylesChecked(anim); } catch { } }
        anim.cancel();
    }
}

//
// Scroll-driven animations (ScrollTimeline / ViewTimeline)
//
// Same pre-sampled keyframes as the compositor offload above, but progressed by scroll position
// rather than by time. Where the browser has native scroll timelines this runs entirely off the
// main thread - no scroll handler at all. Where it doesn't, we scrub the same Web Animation from
// one passive scroll listener: still the browser interpolating the values, and still no .NET
// interop per frame.
//

// Scrub cleanups for timeline animations that fell back, keyed the same way as _waapiAnims so
// cancellation and element teardown release both together.
const _timelineScrubs = new Map<string, Map<string, () => void>>(); // elementId → Map<token, cleanup>

/** The scroll container a spec points at: an element by selector, or the document scroller. */
function _timelineSource(spec: any): HTMLElement | null {
    if (spec?.selector) return document.querySelector(spec.selector) as HTMLElement | null;
    return document.scrollingElement as HTMLElement | null ?? document.documentElement;
}

/** The subject a view timeline tracks: an element by selector, or the animated element itself. */
function _timelineSubject(spec: any, el: HTMLElement): HTMLElement | null {
    if (spec?.selector) return document.querySelector(spec.selector) as HTMLElement | null;
    return el;
}

/** Constructs the native timeline for a spec, or null when the browser has no native support. */
function _nativeTimeline(spec: any, el: HTMLElement): any | null {
    const axis = spec?.axis === 'inline' ? 'inline' : 'block';
    try {
        if (spec?.view) {
            const ViewTl = (window as any).ViewTimeline;
            if (typeof ViewTl !== 'function') return null;
            const subject = _timelineSubject(spec, el);
            return subject ? new ViewTl({ subject, axis }) : null;
        }
        const ScrollTl = (window as any).ScrollTimeline;
        if (typeof ScrollTl !== 'function') return null;
        const source = _timelineSource(spec);
        return source ? new ScrollTl({ source, axis }) : null;
    } catch {
        // A constructor that exists but rejects the arguments (older prefixed builds) is treated
        // exactly like no support at all, so the caller falls back to scrubbing.
        return null;
    }
}

/**
 * Plays a pre-sampled animation driven by scroll position. Returns true when a native scroll
 * timeline drove it, false when the scrub fallback did (and false is not a failure - the caller
 * only uses it to report which path is live). Returns null when the element is gone or the
 * browser has no Web Animations API at all.
 */
export function playScrollTimelineAnimation(
    elementId: string, token: string, keyframes: any, spec: any): boolean | null {
    const el = document.getElementById(elementId);
    if (!el || typeof el.animate !== 'function') return null;

    // A smoothed timeline is always scrubbed: a native one follows the scroll exactly.
    const timeline = spec?.smooth ? null : _nativeTimeline(spec, el);
    let anim: Animation;
    try {
        if (timeline) {
            anim = (el as any).animate(keyframes, { timeline, fill: 'both' });
            // The range is applied after the fact, and non-fatally: a browser that has scroll
            // timelines but not ranges should narrow to the default full journey rather than lose
            // the animation entirely. (The scrub fallback below always covers the full journey.)
            if (spec?.range) {
                const [rangeStart, rangeEnd] = splitAnimationRange(spec.range);
                try {
                    if (rangeStart) (anim as any).rangeStart = rangeStart;
                    if (rangeEnd) (anim as any).rangeEnd = rangeEnd;
                } catch { /* ranges unsupported - keep the default range */ }
            }
        } else {
            // Fallback: a paused, time-based animation whose currentTime we drive from scroll.
            // SCRUB_MS is arbitrary - it is only the unit the progress fraction is scaled into.
            anim = el.animate(keyframes, { duration: SCRUB_MS, fill: 'both', easing: 'linear' });
            anim.pause();
        }
    } catch {
        return null;
    }

    let map = _waapiAnims.get(elementId);
    if (!map) { map = new Map(); _waapiAnims.set(elementId, map); }
    map.set(token, anim);

    if (timeline) return true;

    _attachScrub(elementId, token, el, anim, spec);
    return false;
}

const SCRUB_MS = 1000;

/** Drives a paused animation's currentTime from the scroll position described by `spec`. */
function _attachScrub(
    elementId: string, token: string, el: HTMLElement, anim: Animation, spec: any): void {
    const inline = spec?.axis === 'inline';
    const source = spec?.view ? null : _timelineSource(spec);
    const subject = spec?.view ? _timelineSubject(spec, el) : null;
    // The document scroller reports its scroll on window, not on the element itself.
    const scrollHost: HTMLElement | Window =
        source && source !== document.scrollingElement && source !== document.documentElement
            ? source
            : window;

    const read = (): number => {
        let progress = 0;
        if (spec?.view) {
            if (subject) {
                const r = subject.getBoundingClientRect();
                progress = inline
                    ? viewFraction(r.left, r.width, 0, window.innerWidth)
                    : viewFraction(r.top, r.height, 0, window.innerHeight);
            }
        } else if (scrollHost === window) {
            const d = document.documentElement;
            progress = inline
                ? scrollFraction(window.scrollX, d.scrollWidth, window.innerWidth)
                : scrollFraction(window.scrollY, d.scrollHeight, window.innerHeight);
        } else {
            const c = scrollHost as HTMLElement;
            progress = inline
                ? scrollFraction(c.scrollLeft, c.scrollWidth, c.clientWidth)
                : scrollFraction(c.scrollTop, c.scrollHeight, c.clientHeight);
        }
        return progress;
    };
    const show = (progress: number) => {
        try { anim.currentTime = progress * SCRUB_MS; } catch { /* animation cancelled */ }
    };

    // Smoothed: the animation follows the progress through a spring, stepped once a frame until it is at rest -
    // starting where the scroll is now, so nothing glides in as it attaches.
    let update: () => void = () => show(read());
    let stopSmoothing = () => { };
    const spring = spec?.smooth;
    if (spring) {
        const state = { value: read(), velocity: 0 };
        let target = state.value, frame: number | null = null, last = 0;
        const step = (ts: number) => {
            frame = null;
            const atRest = smoothStep(state, target, last ? ts - last : 16, spring);
            last = atRest ? 0 : ts;
            show(state.value);
            if (!atRest) frame = requestAnimationFrame(step);
        };
        update = () => {
            target = read();
            frame ??= requestAnimationFrame(step);
        };
        stopSmoothing = () => { if (frame !== null) cancelAnimationFrame(frame); frame = null; };
        show(state.value);
    }

    // A view timeline's progress changes with any scroll on the page, not only with the tracked
    // container's, so it listens on window; resize changes the rects without any scroll at all.
    const target: HTMLElement | Window = spec?.view ? window : scrollHost;
    target.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update, { passive: true });
    update();

    let scrubs = _timelineScrubs.get(elementId);
    if (!scrubs) { scrubs = new Map(); _timelineScrubs.set(elementId, scrubs); }
    scrubs.set(token, () => {
        target.removeEventListener('scroll', update);
        window.removeEventListener('resize', update);
        stopSmoothing();
    });
}

/**
 * One frame of a smoothed scroll timeline: <paramref name="state"/> - progress and its velocity per second - moved
 * <paramref name="dtMs"/> on towards <paramref name="target"/> by a spring (stiffness, damping and mass as for a
 * Bmotion spring). Stepped in pieces of at most 4 ms, and at most 64 ms in all (a tab coming back from the
 * background doesn't fling it). Returns whether it is at rest: within 0.05% of the target and all but still, where it
 * lands on the target exactly.
 */
export function smoothStep(
    state: { value: number; velocity: number }, target: number, dtMs: number,
    spring: { stiffness: number; damping: number; mass: number }): boolean {
    let remaining = Math.min(Math.max(dtMs, 0), 64) / 1000;
    while (remaining > 0) {
        const h = Math.min(remaining, 0.004);
        const accel = (-spring.stiffness * (state.value - target) - spring.damping * state.velocity) / spring.mass;
        state.velocity += accel * h;
        state.value += state.velocity * h;
        remaining -= h;
    }
    if (Math.abs(target - state.value) < 0.0005 && Math.abs(state.velocity) < 0.005) {
        state.value = target;
        state.velocity = 0;
        return true;
    }
    return false;
}

/** Stops a scroll-driven animation and releases its scroll listener (when it fell back). */
export function cancelScrollTimelineAnimation(elementId: string, token: string): void {
    _releaseScrub(elementId, token);
    const anim = _waapiAnims.get(elementId)?.get(token);
    if (!anim) return;
    try { commitStylesChecked(anim); } catch { /* detached element */ }
    anim.cancel();
    const map = _waapiAnims.get(elementId);
    if (map) { map.delete(token); if (map.size === 0) _waapiAnims.delete(elementId); }
}

function _releaseScrub(elementId: string, token: string): void {
    const scrubs = _timelineScrubs.get(elementId);
    if (!scrubs) return;
    scrubs.get(token)?.();
    scrubs.delete(token);
    if (scrubs.size === 0) _timelineScrubs.delete(elementId);
}

function _releaseScrubsForElement(elementId: string): void {
    const scrubs = _timelineScrubs.get(elementId);
    if (!scrubs) return;
    for (const cleanup of [...scrubs.values()]) cleanup();
    _timelineScrubs.delete(elementId);
}

//
// Viewport observation (whileInView)
//

// Cache observers keyed by their options signature so we can re-use them. Each entry tracks the
// element IDs it currently observes so the observer can be disconnected once it falls empty
// (otherwise distinct margin/threshold combinations would accumulate observers forever).
const _vpObservers = new Map<string, { observer: IntersectionObserver; members: Set<string> }>(); // sig → { observer, members: Set<elementId> }
// inView: whether the last report forwarded for the element was "in view". IntersectionObserver
// reports every element once as soon as it is observed, in view or not; a "not in view" is only
// forwarded after an "in view", so an element that mounts off-screen is never told it left a
// viewport it was never in. owesLeave: re-observed (options changed) while in view, so the new
// observer's first "not in view" is a real leave.
interface BmViewportRef { dotnetRef: DotNet.DotNetObject; once: boolean; inView: boolean; owesLeave: boolean; }
const _vpRefs      = new Map<string, BmViewportRef>(); // elementId → BmViewportRef

function _vpSig(margin: string, threshold: number, root: string | null): string {
    return `${root ?? ''}|${margin}|${threshold}`;
}

function _getVpEntry(margin: string, threshold: number, root: string | null): { observer: IntersectionObserver; members: Set<string> } {
    const sig = _vpSig(margin, threshold, root);
    let entry = _vpObservers.get(sig);
    if (entry) return entry;
    // A root selector that matches nothing degrades to the browser viewport (the API's default)
    // rather than silently never intersecting.
    const rootEl = root ? document.querySelector(root) : null;
    const observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
            const id  = entry.target.getAttribute('data-bmid');
            const ref = id ? _vpRefs.get(id) : undefined;
            if (!ref || !id) continue;
            if (entry.isIntersecting) {
                if (ref.inView) continue;
                ref.inView = true;
            } else {
                if (!ref.inView && !ref.owesLeave) continue;
                ref.inView = false;
            }
            ref.owesLeave = false;
            ref.dotnetRef.invokeMethodAsync('OnIntersect', entry.isIntersecting);
            if (ref.once && entry.isIntersecting) {
                _detachFromObservers(entry.target, id);
                _vpRefs.delete(id);
            }
        }
    }, { root: rootEl, rootMargin: margin || '0px', threshold: threshold ?? 0 });
    entry = { observer, members: new Set<string>() };
    _vpObservers.set(sig, entry);
    return entry;
}

// Unobserve an element from every observer that might track it, dropping membership and
// disconnecting (and evicting) any observer left with no members.
function _detachFromObservers(el: Element | null, elementId: string): void {
    for (const [sig, entry] of _vpObservers) {
        if (el) entry.observer.unobserve(el);
        entry.members.delete(elementId);
        if (entry.members.size === 0) {
            entry.observer.disconnect();
            _vpObservers.delete(sig);
        }
    }
}

export function observeViewport(elementId: string, dotnetRef: DotNet.DotNetObject, options: any): void {
    const el = document.getElementById(elementId);
    if (!el) return;
    const once      = options?.once      ?? false;
    const margin    = options?.margin    ?? '0px';
    const threshold = options?.threshold ?? 0;
    const root      = options?.root      ?? null;
    // Detach from any previously assigned observer first so re-observing with different options
    // doesn't stack duplicate subscriptions (which would fire OnIntersect multiple times and
    // break the "once" behaviour across option changes).
    _detachFromObservers(el, elementId);
    // A re-observe re-reports "in view" (C# re-observes to re-arm a WhileInView enabled late), and
    // owes a "leave" when the element was in view before and the new options say it isn't.
    const owesLeave = _vpRefs.get(elementId)?.inView ?? false;
    _vpRefs.set(elementId, { dotnetRef, once, inView: false, owesLeave });
    const entry = _getVpEntry(margin, threshold, root);
    entry.members.add(elementId);
    entry.observer.observe(el);
}

export function unobserveViewport(elementId: string): void {
    const el = document.getElementById(elementId);
    _detachFromObservers(el, elementId);
    _vpRefs.delete(elementId);
}

//
// FLIP layout animation support
//

/**
 * Returns the element's rect in DOCUMENT-relative coordinates (viewport rect + page scroll)
 * for C# to snapshot.
 *
 * FLIP deltas (snap - cur) and the shared-element (LayoutId) registry may capture a rect at
 * one scroll position and consume it at another - e.g. the user scrolls the page before
 * clicking a new tab. Raw getBoundingClientRect values are viewport-relative, so the recorded
 * position goes stale by exactly the scroll amount and the animation starts from the wrong
 * place. Document coordinates are scroll-invariant, which keeps the FLIP start position correct
 * across an intervening page scroll. (Widths/heights are unaffected by scrolling.)
 */
// The nearest ancestor that actually scrolls, for layoutScroll measurements. Walks up on computed
// overflow rather than scrollHeight, so a container that is currently short enough not to scroll is
// still recognised as the scroller it will be once its content grows.
function _scrollableAncestor(el: HTMLElement): HTMLElement | null {
    for (let p = el.parentElement; p; p = p.parentElement) {
        const style = getComputedStyle(p);
        if (/(auto|scroll|overlay)/.test(style.overflowY + style.overflowX)) return p;
    }
    return null;
}

/**
 * Measures an element for a FLIP comparison.
 *
 * Coordinates are document-relative by default (page scroll folded in), so scrolling the page
 * between the snapshot and the measurement isn't mistaken for the element having moved.
 * `options.fixedRoot` switches to viewport coordinates for a position:fixed element, which is the
 * one case where the page scrolling really does leave it where it was; `options.trackScroll` folds
 * in the nearest scrolling ancestor's offset for the same reason one level down.
 */
export function getBoundingRect(elementId: string, options?: { trackScroll?: boolean; fixedRoot?: boolean }):
    { x: number; y: number; width: number; height: number; top: number; left: number } | null {
    const el = document.getElementById(elementId);
    if (!el) return null;
    // A layout hold is the FLIP's inverse of where the layout put the element, not where the layout put it.
    return _withoutLayoutHold(el, () => _measure(el, options));
}

type BmMeasured = { x: number; y: number; width: number; height: number; top: number; left: number };

function _measure(el: HTMLElement, options?: { trackScroll?: boolean; fixedRoot?: boolean }): BmMeasured {
    const r = el.getBoundingClientRect();
    // A fixed element is positioned against the viewport, so its viewport rect is already the
    // stable one - adding page scroll would invent a delta on every scroll.
    let sx = options?.fixedRoot ? 0 : window.scrollX;
    let sy = options?.fixedRoot ? 0 : window.scrollY;
    if (options?.trackScroll) {
        const container = _scrollableAncestor(el);
        if (container) { sx += container.scrollLeft; sy += container.scrollTop; }
    }
    return {
        x: r.x + sx, y: r.y + sy, width: r.width, height: r.height,
        top: r.top + sy, left: r.left + sx,
    };
}

// ── Layout holds (Blazor Server) ─────────────────────────────────────────────
//
// Without synchronous interop a FLIP is a round trip behind the render that moved the element: the browser paints the
// new layout first, and the element flashes at its new place before the FLIP takes it back. So the snapshot taken
// before the render arms a hold: as soon as the render lands (a mutation, before anything paints) and the element's
// box has moved, it is held where it was - the FLIP's own first frame, a paused animation - until the FLIP arrives and
// takes over from there, or .NET lets go (no FLIP), or two seconds pass.

type BmMeasureOptions = { trackScroll?: boolean; fixedRoot?: boolean };
type BmArmedHold = { old: BmMeasured; options?: BmMeasureOptions; translate: boolean; scale: boolean; ax: number; ay: number };
const _armedHolds = new Map<HTMLElement, BmArmedHold>();
const _layoutHolds = new Map<HTMLElement, { anim: Animation; keyframes: Keyframe[]; origin: string }>();
const _holdTimers = new Map<HTMLElement, number>();
let _holdObserver: MutationObserver | null = null;
const HOLD_TIMING: KeyframeAnimationOptions = { duration: 1e6, fill: 'both' };

/** The snapshot before a re-render, arming a hold through that render (see above). */
export function measureAndHoldLayout(
    elementId: string, options: BmMeasureOptions | undefined, translate: boolean, scale: boolean,
    anchorX: number, anchorY: number): BmMeasured | null {
    const el = document.getElementById(elementId);
    if (!el) return null;
    // Where it shows now - a hold or a FLIP in flight included: the next FLIP starts from there.
    const old = _measure(el, options);
    _armedHolds.set(el, { old, options, translate, scale, ax: anchorX, ay: anchorY });
    const timer = _holdTimers.get(el);
    if (timer !== undefined) clearTimeout(timer);
    _holdTimers.set(el, window.setTimeout(() => releaseLayoutHold(elementId), 2000));
    if (!_holdObserver) {
        _holdObserver = new MutationObserver(_applyArmedHolds);
        _holdObserver.observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true });
    }
    return old;
}

/** Lets go of an element's layout hold - armed or applied - when no FLIP is coming. */
export function releaseLayoutHold(elementId: string): void {
    const el = document.getElementById(elementId);
    if (el) _releaseLayoutHold(el);
}

function _releaseLayoutHold(el: HTMLElement): void {
    _armedHolds.delete(el);
    const timer = _holdTimers.get(el);
    if (timer !== undefined) { clearTimeout(timer); _holdTimers.delete(el); }
    const hold = _layoutHolds.get(el);
    if (hold) {
        hold.anim.cancel();
        el.style.transformOrigin = hold.origin;
        _layoutHolds.delete(el);
    }
    if (_armedHolds.size === 0 && _holdObserver) { _holdObserver.disconnect(); _holdObserver = null; }
}

// After a mutation: every armed element whose box has moved is held where it was. One that hasn't stays armed - the
// render that moves it may still be on its way (the mutation was something else).
function _applyArmedHolds(): void {
    for (const [el, armed] of [..._armedHolds]) {
        if (!el.isConnected) { _releaseLayoutHold(el); continue; }
        const cur = _withoutLayoutHold(el, () => _measure(el, armed.options));
        // The FLIP's own deltas (Bmotion.PlayFlipFromAsync): from the anchor point, translation and scale as the
        // layout mode allows.
        const oldX = armed.old.left + armed.ax * armed.old.width, oldY = armed.old.top + armed.ay * armed.old.height;
        const curX = cur.left + armed.ax * cur.width, curY = cur.top + armed.ay * cur.height;
        const dx = armed.translate ? oldX - curX : 0, dy = armed.translate ? oldY - curY : 0;
        const sx = armed.scale && cur.width > 0 ? armed.old.width / cur.width : 1;
        const sy = armed.scale && cur.height > 0 ? armed.old.height / cur.height : 1;
        if (Math.abs(dx) < 0.5 && Math.abs(dy) < 0.5 && Math.abs(sx - 1) < 0.005 && Math.abs(sy - 1) < 0.005) continue;
        _armedHolds.delete(el);
        const previous = _layoutHolds.get(el);
        if (previous) previous.anim.cancel();
        const live = getComputedStyle(el).transform;
        const transform = `translate(${dx}px,${dy}px) scaleX(${sx}) scaleY(${sy}) ${live && live !== 'none' ? live : ''}`.trim();
        const keyframes: Keyframe[] = [{ transform }, { transform }];
        const origin = previous ? previous.origin : el.style.transformOrigin;
        el.style.transformOrigin = `${armed.ax * 100}% ${armed.ay * 100}%`;
        const anim = el.animate(keyframes, HOLD_TIMING);
        anim.pause();
        _layoutHolds.set(el, { anim, keyframes, origin });
    }
    if (_armedHolds.size === 0 && _holdObserver) { _holdObserver.disconnect(); _holdObserver = null; }
}

// fn's measurement of el as the layout has it, without the hold that keeps it where it was.
function _withoutLayoutHold<T>(el: HTMLElement, fn: () => T): T {
    const hold = _layoutHolds.get(el);
    if (!hold) return fn();
    hold.anim.cancel();
    try { return fn(); }
    finally {
        hold.anim = el.animate(hold.keyframes, HOLD_TIMING);
        hold.anim.pause();
    }
}

// The FLIP animation playing on each element, so a newer one can supersede it: end() settles and
// reports it at once - synchronously, before the newer one sets the element up, rather than from a
// cancel event that would arrive after and undo the newer one's transform-origin.
const _flips = new Map<string, { anim: Animation; end: () => void }>();

/**
 * Run a FLIP animation via the Web Animations API.
 * The element is currently at its NEW layout position; this animates it
 * from the OLD (inverted) position to identity.
 */
export function playWaapiFlip(
    elementId: string, dx: number, dy: number, sx: number, sy: number,
    durationMs: number, easingStr: string, finalTransform: string,
    originX?: number, originY?: number, dotnetRef?: DotNet.DotNetObject): void {
    const el = document.getElementById(elementId);
    if (!el) {
        // Nothing will animate, so the caller's completion callback would never arrive; settle it
        // now rather than leaving an OnLayoutAnimationComplete permanently outstanding.
        dotnetRef?.invokeMethodAsync('OnLayoutAnimationCompleted');
        return;
    }
    // The hold keeping it where it was until now (Blazor Server) hands over: this FLIP starts from that same place.
    _releaseLayoutHold(el);
    // A newer layout change supersedes the FLIP still playing (ended - settled and reported - then
    // cancelled), instead of both running and the older one's finish resetting the newer one's origin.
    const superseded = _flips.get(elementId);
    if (superseded) {
        superseded.end();
        superseded.anim.cancel();
    }

    // Measure children relative to the parent before any transform takes hold.
    const scales = Math.abs(sx - 1) > 1e-4 || Math.abs(sy - 1) > 1e-4;
    const pRect = scales ? el.getBoundingClientRect() : null;
    const childRects = scales ? Array.from(el.children).map(c => c.getBoundingClientRect()) : [];

    // The projection anchor (layoutAnchor): the point of the box that appears to stay still. The
    // deltas C# computed are measured from this same point, so the two must agree.
    const ax = (originX ?? 0) * 100, ay = (originY ?? 0) * 100;
    el.style.transformOrigin = `${ax}% ${ay}%`;
    // The element's own transform (its animated x, scale, ...) stays on: the deltas were measured
    // between boxes that both carry it, and the FLIP only projects the layout change on top. It
    // starts from the transform showing right now (a compositor animation in flight included - the
    // rects were measured with it on) and ends on the one the element will be showing when the FLIP
    // ends (finalTransform, which C# samples with such animations advanced by the FLIP's duration),
    // so the animation underneath shows through at that same frame and nothing jumps. fill:'none' -
    // an animation filling forwards would override every transform the engine writes afterwards.
    const live = getComputedStyle(el).transform;
    const from = live && live !== 'none' ? live : '';
    const own = finalTransform || '';
    // What the inline transform is now: if something writes a newer one while the FLIP plays (a
    // compositor animation committing its end), settling must not put this older one back over it.
    const inlineAtStart = el.style.transform;
    const timing: KeyframeAnimationOptions = { duration: durationMs, easing: easingStr || 'ease', fill: 'none' };
    const anim = el.animate(
        [
            { transform: `translate(${dx}px,${dy}px) scaleX(${sx}) scaleY(${sy}) ${from}`.trim() },
            { transform: own || 'none' },
        ],
        timing
    );

    // Size-mode correction (plan item 1.2): while the box scales, counter-scale direct children and
    // correct the border-radius so text/children don't stretch and corners stay round. Position-mode
    // FLIP passes sx=sy=1, so this whole block is skipped and behaviour is unchanged there.
    // These end at their natural values, so they use fill:'none' and leave no persistent override.
    const cleanups: Array<() => void> = [];
    if (scales && pRect) {
        const correctTiming: KeyframeAnimationOptions = { duration: durationMs, easing: easingStr || 'ease', fill: 'none' };

        const radius = parseFloat(getComputedStyle(el).borderTopLeftRadius) || 0;
        if (radius > 0) el.animate(flipRadiusFrames(radius, sx, sy), correctTiming);

        const children = Array.from(el.children);
        for (let i = 0; i < children.length; i++) {
            const child = children[i];
            if (!(child instanceof HTMLElement)) continue;
            // Keyframed as the exact inverse of the parent's transform at every step, about the parent's own
            // transform origin (the anchor), so the child neither stretches nor jumps (flipChildFrames).
            const c = flipChildFrames(pRect, childRects[i], sx, sy, dx, dy, originX ?? 0, originY ?? 0);
            const prevOrigin = child.style.transformOrigin;
            child.style.transformOrigin = `${c.originX}px ${c.originY}px`;
            child.animate(c.frames, correctTiming);
            cleanups.push(() => { child.style.transformOrigin = prevOrigin; });
        }
    }

    // Run on finish AND cancel: a cancelled flip must not leave the '0 0' transform-origin (or
    // the children's counter-transform origins) stuck on the elements.
    const settle = () => {
        if (el.style.transform === inlineAtStart) el.style.transform = finalTransform || '';
        el.style.transformOrigin = '';
        for (const done of cleanups) done();
    };
    // Report completion on both outcomes. A cancelled FLIP is one that a newer layout change
    // superseded, and that newer animation reports its own completion - so firing here too keeps
    // every started layout animation matched by exactly one completion, which is what a consumer
    // re-enabling interaction on OnLayoutAnimationComplete depends on.
    let ended = false;
    const end = () => {
        if (ended) return;
        ended = true;
        if (_flips.get(elementId)?.anim === anim) _flips.delete(elementId);
        settle();
        dotnetRef?.invokeMethodAsync('OnLayoutAnimationCompleted');
    };
    anim.onfinish = end;
    anim.oncancel = end;
    _flips.set(elementId, { anim, end });
}

// A LayoutId's previous holder fading out onto the element that took the id over (playLayoutCrossfade), with the
// transform-origin it had before.
const _crossfadesOut = new WeakMap<HTMLElement, { anim: Animation; origin: string }>();

/**
 * Crossfades two holders of a LayoutId as the layout animation plays. The lead, taking it over, fades in; the follow,
 * on its way out, travels from its box onto the lead's - by (dx, dy) and (sx, sy) about the anchor (originX, originY)
 * - as it fades out, and stays there until it is removed. The fade is a filter: opacity() after any filter of the
 * element's own, so it multiplies with the elements' opacity: an enter or exit fade of their own plays on underneath.
 */
export function playLayoutCrossfade(
    leadId: string, followId: string, dx: number, dy: number, sx: number, sy: number,
    durationMs: number, easingStr: string, originX: number, originY: number): void {
    const timing = { duration: durationMs, easing: easingStr || 'ease' };
    const filterOf = (el: HTMLElement) => {
        const f = getComputedStyle(el).filter;
        return f && f !== 'none' ? `${f} ` : '';
    };

    const lead = document.getElementById(leadId);
    if (lead) {
        _revealCrossfadeLead(lead);
        const own = filterOf(lead);
        lead.animate([{ filter: `${own}opacity(0)` }, { filter: `${own}opacity(1)` }], { ...timing, fill: 'none' });
    }

    const follow = document.getElementById(followId);
    if (!follow) return;
    cancelLayoutCrossfade(followId);
    const origin = follow.style.transformOrigin;
    follow.style.transformOrigin = `${originX * 100}% ${originY * 100}%`;
    const live = getComputedStyle(follow).transform;
    const from = live && live !== 'none' ? live : '';
    const own = filterOf(follow);
    const anim = follow.animate([
        { transform: from || 'none', filter: `${own}opacity(1)` },
        { transform: `translate(${dx}px,${dy}px) scaleX(${sx}) scaleY(${sy}) ${from}`.trim(), filter: `${own}opacity(0)` },
    ], { ...timing, fill: 'forwards' });
    _crossfadesOut.set(follow, { anim, origin });
}

/**
 * Takes an element off its crossfade: one fading out that came back before it was removed, or one rendered hidden to
 * fade in whose crossfade didn't happen after all.
 */
export function cancelLayoutCrossfade(elementId: string): void {
    const el = document.getElementById(elementId);
    if (!el) return;
    _revealCrossfadeLead(el);
    const crossfade = _crossfadesOut.get(el);
    if (!crossfade) return;
    crossfade.anim.cancel();
    el.style.transformOrigin = crossfade.origin;
    _crossfadesOut.delete(el);
}

// An element about to fade in renders hidden (filter: opacity(0)), so it isn't shown at its new place for the round
// trips before its crossfade starts; the crossfade (or its cancellation) takes that over. Anything else in the
// element's filter is the app's own and stays.
function _revealCrossfadeLead(el: HTMLElement): void {
    if (el.style.filter === 'opacity(0)') el.style.removeProperty('filter');
}

//
// Scroll tracking
//

let _scrollKeySeq = 0;
const _scrollSubs = new Map<string, () => void>(); // key  cleanup fn

export function observeScroll(containerId: string | null, dotnetRef: DotNet.DotNetObject, options: any): string | null {
    const el: HTMLElement | Window | null = containerId ? document.getElementById(containerId) : window;
    if (!el) return null;
    const key = `scroll_${++_scrollKeySeq}`;
    const targetId = options?.targetId ?? null;
    const offsets = options?.offsets ?? null; // [[targetFrac, containerFrac], [targetFrac, containerFrac]]

    // Positions are absolute, so only the latest matters: one call on its way at a time (a round trip on Blazor
    // Server), and whatever scrolled meanwhile goes next as one.
    const report = latestOnly((progress: object) => dotnetRef.invokeMethodAsync('OnScroll', progress));

    const onScroll = () => {
        let sX: number, sY: number, sW: number, sH: number, cW: number, cH: number;
        if (el === window) {
            sX = window.scrollX; sY = window.scrollY;
            sW = document.documentElement.scrollWidth;
            sH = document.documentElement.scrollHeight;
            cW = window.innerWidth; cH = window.innerHeight;
        } else {
            const ce = el as HTMLElement;
            sX = ce.scrollLeft; sY = ce.scrollTop;
            sW = ce.scrollWidth; sH = ce.scrollHeight;
            cW = ce.clientWidth; cH = ce.clientHeight;
        }
        const pX = scrollFraction(sX, sW, cW);
        const pY = scrollFraction(sY, sH, cH);

        // Target progress: 0 when the target sits at the first configured alignment, 1 at the
        // second. Both alignment "distances" shift equally per scrolled pixel, so the current
        // fraction is d0 / (d0 - d1), computed straight from viewport-relative rects.
        let tp: number | null = null;
        if (targetId && offsets) {
            const target = document.getElementById(targetId);
            if (target) {
                const tr = target.getBoundingClientRect();
                let cTop: number, cHeight: number;
                if (el === window) { cTop = 0; cHeight = window.innerHeight; }
                else { cTop = (el as HTMLElement).getBoundingClientRect().top; cHeight = (el as HTMLElement).clientHeight; }
                const d0 = (tr.top + offsets[0][0] * tr.height) - (cTop + offsets[0][1] * cHeight);
                const d1 = (tr.top + offsets[1][0] * tr.height) - (cTop + offsets[1][1] * cHeight);
                const span = d0 - d1;
                tp = span !== 0 ? Math.min(1, Math.max(0, d0 / span)) : 0;
            }
        }

        report({
            scrollX: sX, scrollY: sY,
            progressX: pX, progressY: pY,
            scrollWidth: sW, scrollHeight: sH,
            clientWidth: cW, clientHeight: cH,
            targetProgress: tp,
        });
    };

    el.addEventListener('scroll', onScroll, { passive: true });
    // Target rects also change on viewport resize without a scroll event.
    if (targetId) window.addEventListener('resize', onScroll, { passive: true });
    _scrollSubs.set(key, () => {
        el.removeEventListener('scroll', onScroll);
        if (targetId) window.removeEventListener('resize', onScroll);
    });
    onScroll(); // fire immediately with current position
    return key;
}

/**
 * Turns a stream of values into calls to <paramref name="send"/>, one at a time: while one is on its way (to .NET, a
 * round trip on Blazor Server), newer values replace each other and only the last goes next. The stream never queues
 * up behind a slow connection, and what arrives is always the freshest value. A failed send doesn't stop it.
 */
export function latestOnly<T>(send: (value: T) => Promise<unknown>): (value: T) => void {
    return coalesce(send, (_, next) => next).push;
}

/**
 * <see cref="latestOnly"/> for values that add up: while a call is on its way, newer values are folded into one by
 * <paramref name="merge"/> (a pan's deltas summed, its position the latest). <c>flush</c> sends what is waiting at
 * once - before a message that must come after it (the end of the gesture).
 */
export function coalesce<T>(send: (value: T) => Promise<unknown>, merge: (pending: T, next: T) => T):
    { push: (value: T) => void; flush: () => void } {
    let inFlight = false;
    let pending: { value: T } | null = null;
    const pump = (value: T): void => {
        inFlight = true;
        let sent: Promise<unknown>;
        try { sent = send(value); } catch (e) { sent = Promise.reject(e); }
        sent.catch(() => { /* the circuit may be going; the next value still goes */ }).finally(() => {
            inFlight = false;
            if (pending) {
                const next = pending.value;
                pending = null;
                pump(next);
            }
        });
    };
    return {
        push: (value: T) => {
            if (!inFlight) pump(value);
            else pending = { value: pending ? merge(pending.value, value) : value };
        },
        flush: () => {
            if (!pending) return;
            const next = pending.value;
            pending = null;
            pump(next);
        },
    };
}

/**
 * A pointer's velocity from where it was over the last <paramref name="windowMs"/> (motion.dev's 100 ms): the
 * average over that window, which ends when it is read - not the last two moves', which spikes when moves arrive
 * back to back (a busy main thread, a browser that doesn't align input to frames) and stays behind after the
 * pointer stops. A pointer that stood still for the whole window has none. The span it divides by is at least
 * <paramref name="minSpanMs"/>, one frame: no screen shows a move faster than that.
 */
export function pointerVelocity(windowMs = 100, minSpanMs = 16): {
    reset: (x: number, y: number, t: number) => void;
    add: (x: number, y: number, t: number) => void;
    read: (t: number, perMs: number) => { x: number; y: number };
} {
    let points: Array<{ x: number; y: number; t: number }> = [];
    const prune = (t: number) => { while (points.length && t - points[0].t > windowMs) points.shift(); };
    return {
        reset: (x, y, t) => { points = [{ x, y, t }]; },
        add: (x, y, t) => { points.push({ x, y, t }); prune(t); },
        read: (t, perMs) => {
            prune(t);
            if (points.length < 2) return { x: 0, y: 0 };
            const first = points[0], last = points[points.length - 1];
            const span = Math.max(t - first.t, minSpanMs);
            return { x: (last.x - first.x) / span * perMs, y: (last.y - first.y) / span * perMs };
        },
    };
}

/**
 * When an input event happened: its own timestamp - right even when the main thread got to it late - unless the
 * browser stamps events on another clock than performance.now().
 */
function _eventTime(e: Event): number {
    const now = performance.now();
    return e.timeStamp > 0 && e.timeStamp <= now && now - e.timeStamp < 1000 ? e.timeStamp : now;
}

export function unobserveScroll(key: string): void {
    _scrollSubs.get(key)?.();
    _scrollSubs.delete(key);
}

// ── View Transitions API ────────────────────────────────────────────────────

// Wrap document.startViewTransition around a C# DOM-update callback. The callback (invoked by
// name on the dotnet ref) performs the Blazor state change; the browser snapshots before/after and
// cross-fades. Falls back to running the callback directly when the API is unsupported.
export async function startViewTransition(dotnetRef: DotNet.DotNetObject, callbackName: string, types?: string[]): Promise<boolean> {
    const runUpdate = () => dotnetRef.invokeMethodAsync(callbackName);
    const doc = document as any;
    if (typeof doc.startViewTransition !== 'function') {
        await runUpdate();
        return false;
    }
    // Typed transitions take an options object; a browser that only knows the callback form throws on it (it isn't
    // callable), and then gets the untyped transition - the update must run exactly once either way.
    let transition: any = null;
    if (types && types.length) {
        try { transition = doc.startViewTransition({ update: () => runUpdate(), types }); }
        catch { transition = null; }
    }
    transition ??= doc.startViewTransition(() => runUpdate());
    try { await transition.finished; }
    catch {
        // `finished` also rejects when the C# update callback threw - that IS an error and must
        // reach the caller. Re-awaiting updateCallbackDone rethrows the callback failure, while a
        // merely skipped/aborted transition (callback succeeded) resolves and stays swallowed.
        await transition.updateCallbackDone;
    }
    return true;
}
