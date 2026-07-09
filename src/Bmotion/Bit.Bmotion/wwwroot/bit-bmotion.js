/**
 * bit-bmotion.js - slim browser-API bridge
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
 */

//
// Pure drag / scroll math - no DOM access, so it is unit-tested directly (Tests/bit-bmotion-js).
// The exports are an internal testing surface; the C# interop only calls the named bridge functions.
//

/** Elastic give past a constraint edge: 0 = rigid, 1 = fully elastic. */
export function applyElastic(overflow, edge) {
    return edge > 0 ? overflow * edge : 0;
}

/**
 * Turns a container rect + element rect (and the element's current transform x/y) into the allowed
 * transform bounds. An element larger than its container collapses to the centered offset so the
 * drag pins in place instead of oscillating between contradictory bounds.
 */
export function resolveConstraintBounds(cRect, eRect, curX, curY) {
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
export function clampToConstraints(x, y, c, elastic) {
    if (!c) return { x, y };
    if (c.left   != null && x < c.left)   x = c.left   - applyElastic(c.left   - x, elastic.left);
    if (c.right  != null && x > c.right)  x = c.right  + applyElastic(x - c.right,  elastic.right);
    if (c.top    != null && y < c.top)    y = c.top    - applyElastic(c.top    - y, elastic.top);
    if (c.bottom != null && y > c.bottom) y = c.bottom + applyElastic(y - c.bottom, elastic.bottom);
    return { x, y };
}

/** Normalised 0→1 scroll progress along one axis (0 when the content doesn't overflow). */
export function scrollFraction(scroll, size, client) {
    return size > client ? scroll / (size - client) : 0;
}

/**
 * Layout FLIP child counter-transform (plan item 1.2). The parent element FLIPs with the transform
 * `translate(dx,dy) scale(sx,sy)` about its top-left (origin O). A direct child would ride along -
 * stretched by the scale AND shifted by the translate. To keep the child crisp and in place, apply
 * the exact inverse of the parent transform about the SAME physical point (the parent's top-left);
 * in the child's own box coordinates that point is (-offsetX, -offsetY).
 *
 * Deriving the inverse: the parent maps x → sx·x + (dx + O(1-sx)). Its inverse, written as a CSS
 * `translate(t) scale(k)` about O, is k = 1/sx and t = -dx/sx. Counter-scaling alone (t = 0) - the
 * previous behaviour - left a residual translation of exactly (dx,dy) on the child, so it visibly
 * jumped toward the FLIP corner at the start and slid back to centre. Including the -d/s translate
 * cancels that: net is identity at both endpoints, so children stay put and undistorted.
 */
// FLIP scale factors come from width/height ratios, so they are non-negative; clamp them away
// from zero so a collapsed (0-sized) start rect can't turn the inverse math into Infinity/NaN.
const MIN_FLIP_SCALE = 1e-4;

export function flipChildCorrection(parentRect, childRect, sx, sy, dx = 0, dy = 0) {
    sx = Math.max(sx, MIN_FLIP_SCALE);
    sy = Math.max(sy, MIN_FLIP_SCALE);
    const offsetX = childRect.left - parentRect.left;
    const offsetY = childRect.top - parentRect.top;
    return {
        originX: -offsetX, originY: -offsetY,
        fromScaleX: 1 / sx, fromScaleY: 1 / sy,
        fromTranslateX: -dx / sx, fromTranslateY: -dy / sy,
    };
}

/**
 * Border-radius that, once scaled by (sx,sy), renders as the target radius - so corners stay a
 * constant visual size while the box scales (Motion's border-radius correction). Uses the CSS
 * elliptical `h / v` form so non-uniform scale still yields round corners.
 */
export function correctedRadius(radius, sx, sy) {
    return {
        fromX: radius / Math.max(sx, MIN_FLIP_SCALE),
        fromY: radius / Math.max(sy, MIN_FLIP_SCALE),
    };
}

//
// rAF loop  C# ComputeFrame is called synchronously each tick (Blazor WASM)
//

let _rafId = null;
// Set of engine DotNetObjectReferences. Using a set (rather than a single global) means
// multiple Blazor roots / engine instances sharing this module each get ticked, instead of a
// second startRafLoop silently hijacking the loop from the first.
const _engines = new Set();

export function startRafLoop(dotnetRef) {
    _engines.add(dotnetRef);
    if (_rafId === null) _rafId = requestAnimationFrame(_tick);
}

export function stopRafLoop(dotnetRef) {
    // With an argument, stop only that engine; without one, stop everything (back-compat).
    if (dotnetRef) _engines.delete(dotnetRef);
    else _engines.clear();
    if (_engines.size === 0 && _rafId !== null) {
        cancelAnimationFrame(_rafId);
        _rafId = null;
    }
}

function _tick(timestamp) {
    if (_engines.size === 0) { _rafId = null; return; }
    for (const ref of _engines) {
        try {
            // invokeMethod is synchronous in Blazor WASM  C# does all animation math here
            const updates = ref.invokeMethod('ComputeFrame', timestamp);
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

function _applyStyles(el, styles) {
    for (const prop in styles) {
        if (prop.startsWith('--')) el.style.setProperty(prop, styles[prop]);
        else if (_svgGeomAttrs.has(prop)) el.setAttribute(prop, styles[prop]);
        else el.style[prop] = styles[prop];
    }
}

/** Apply a styles object to an element by ID (used for instant set() calls). */
export function applyStyles(elementId, styles) {
    const el = document.getElementById(elementId);
    if (el) _applyStyles(el, styles);
}

//
// Accessibility - prefers-reduced-motion
//

/** Returns true when the user has requested reduced motion at the OS/browser level. */
export function prefersReducedMotion() {
    return typeof matchMedia === 'function' &&
        matchMedia('(prefers-reduced-motion: reduce)').matches;
}

// Live prefers-reduced-motion change notifications. Keyed by engine DotNetObjectReference so
// each engine can subscribe/unsubscribe independently and we only keep one media-query listener.
const _reducedMotionRefs = new Set();
let _reducedMotionMql = null;
let _reducedMotionListener = null;

function _ensureReducedMotionListener() {
    if (_reducedMotionMql || typeof matchMedia !== 'function') return;
    _reducedMotionMql = matchMedia('(prefers-reduced-motion: reduce)');
    _reducedMotionListener = (e) => {
        for (const ref of _reducedMotionRefs) {
            try { ref.invokeMethodAsync('OnReducedMotionChanged', e.matches); }
            catch { /* a disposed/faulted engine ref must not break the others */ }
        }
    };
    // addEventListener is the modern API; addListener is the deprecated Safari fallback.
    if (_reducedMotionMql.addEventListener) _reducedMotionMql.addEventListener('change', _reducedMotionListener);
    else if (_reducedMotionMql.addListener) _reducedMotionMql.addListener(_reducedMotionListener);
}

export function watchReducedMotion(dotnetRef) {
    _reducedMotionRefs.add(dotnetRef);
    _ensureReducedMotionListener();
}

export function unwatchReducedMotion(dotnetRef) {
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

const _eventCleanup = new Map(); // elementId  Array<() => void>

export function registerElement(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.setAttribute('data-bmid', elementId);
    return !!el;
}

// 
// Programmatic animate() API - resolve elements by CSS selector or ElementReference
// Assigns a stable id + data-bmid so the engine can address them via getElementById.
// 

let _programmaticSeq = 0;

function _ensureElementId(el) {
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
export function resolveOrRegisterBySelector(selector) {
    try {
        return Array.from(document.querySelectorAll(selector)).map(el => _ensureElementId(el));
    } catch {
        return [];
    }
}

/** Resolve the element for a Blazor ElementReference and return its element ID. */
export function resolveOrRegisterByRef(element) {
    return _ensureElementId(element);
}

export function unregisterElement(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.removeAttribute('data-bmid');
    _runCleanup(elementId);
    _cancelWaapiForElement(elementId, false);
    // Detach from every viewport observer (drops membership and evicts empty observers).
    _detachFromObservers(el, elementId);
    _vpRefs.delete(elementId);
    _popped.delete(elementId);
}

function _runCleanup(elementId) {
    const fns = _eventCleanup.get(elementId);
    if (fns) { fns.forEach(fn => fn()); _eventCleanup.delete(elementId); }
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
export function attachEventListeners(elementId, events, dotnetRef) {
    const el = document.getElementById(elementId);
    if (!el) return;
    _runCleanup(elementId);
    const cleanups = [];
    _eventCleanup.set(elementId, cleanups);

    //  Hover 
    if (events.hover) {
        const onEnter = () => dotnetRef.invokeMethodAsync('OnPointerEnter');
        const onLeave = () => dotnetRef.invokeMethodAsync('OnPointerLeave');
        el.addEventListener('pointerenter', onEnter);
        el.addEventListener('pointerleave', onLeave);
        cleanups.push(() => { el.removeEventListener('pointerenter', onEnter); el.removeEventListener('pointerleave', onLeave); });
    }

    //  Tap 
    if (events.tap) {
        let pressing = false;
        const onDown = (e) => {
            if (e.button !== 0 && e.pointerType !== 'touch') return; // primary button / touch only
            pressing = true; dotnetRef.invokeMethodAsync('OnPointerDown');
        };
        const onUp   = (e) => {
            if (e.button !== 0 && e.pointerType !== 'touch') return; // ignore non-primary releases
            if (!pressing) return; pressing = false;
            dotnetRef.invokeMethodAsync('OnPointerUp', el.contains(e.target) || el === e.target);
        };
        const onCancel = () => { if (!pressing) return; pressing = false; dotnetRef.invokeMethodAsync('OnPointerCancel'); };

        // Keyboard accessibility (matches motion.dev): when the element is focused, Enter and
        // Space press and release it like a tap. Key state is tracked separately from pointer
        // state so a keyboard press can't cancel an in-flight pointer tap (and vice versa).
        let keyPressing = false;
        const isTapKey = (e) => e.key === 'Enter' || e.key === ' ';
        const onKeyDown = (e) => {
            if (!isTapKey(e)) return;
            // Space's default action scrolls the page on non-button elements; suppress it for
            // every tap-key keydown (including repeats) so a held press can't scroll either.
            if (e.key === ' ') e.preventDefault();
            if (e.repeat || keyPressing) return;
            keyPressing = true;
            dotnetRef.invokeMethodAsync('OnPointerDown');
        };
        const onKeyUp = (e) => {
            if (!isTapKey(e) || !keyPressing) return;
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
        const onIn  = () => dotnetRef.invokeMethodAsync('OnFocusIn');
        const onOut = () => dotnetRef.invokeMethodAsync('OnFocusOut');
        el.addEventListener('focusin',  onIn);
        el.addEventListener('focusout', onOut);
        cleanups.push(() => { el.removeEventListener('focusin', onIn); el.removeEventListener('focusout', onOut); });
    }

    //  Pan (detects movement ≥ 3px without moving the element) 
    if (events.pan) {
        // When drag is also active it already calls setPointerCapture; let pan reuse that capture
        // instead of grabbing the pointer a second time for the same element.
        _attachPan(el, dotnetRef, cleanups, !!events.drag);
    }

    //  Drag 
    if (events.drag) {
        _attachDrag(elementId, el, events, dotnetRef, cleanups);
    }
}

function _attachPan(el, dotnetRef, cleanups, skipCapture) {
    const PAN_THRESHOLD = 3; // pixels before pan is detected
    let down = false;        // whether a pointer is currently pressed on this element
    let panning = false;
    let startX, startY, lastX, lastY, lastT;
    let velX = 0, velY = 0;

    const onDown = (e) => {
        if (e.button !== 0 && e.pointerType !== 'touch') return;
        down = true;
        startX = lastX = e.clientX; startY = lastY = e.clientY;
        lastT = performance.now(); velX = velY = 0; panning = false;
        // Skip when drag already owns the pointer capture for this element.
        if (!skipCapture) el.setPointerCapture(e.pointerId);
    };

    const onMove = (e) => {
        // Ignore moves when no pointer is pressed (e.g. plain hover) so stale start
        // coordinates from a previous gesture can't trigger a phantom pan.
        if (!down) return;
        const dx = e.clientX - startX, dy = e.clientY - startY;
        const now = performance.now(), dt = now - lastT;
        const deltaX = e.clientX - lastX, deltaY = e.clientY - lastY;
        if (dt > 0) {
            velX = deltaX / dt * 1000;
            velY = deltaY / dt * 1000;
        }

        if (!panning && Math.sqrt(dx * dx + dy * dy) >= PAN_THRESHOLD) {
            panning = true;
            dotnetRef.invokeMethodAsync('OnPanStart_');
        }
        if (panning) {
            dotnetRef.invokeMethodAsync('OnPanMove',
                e.clientX, e.clientY,
                deltaX, deltaY,
                dx, dy,
                velX, velY);
        }

        lastX = e.clientX; lastY = e.clientY; lastT = now;
    };

    const onUp = () => { down = false; if (panning) { panning = false; dotnetRef.invokeMethodAsync('OnPanEnd_'); } };

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

function _attachDrag(elementId, el, opts, dotnetRef, cleanups) {
    // Velocity is sampled per pointer-move as px/ms and scaled to "px per frame" (~16ms) so the
    // C# inertia driver receives a frame-relative figure consistent with its release-velocity math.
    const FRAME_MS = 16;
    const axis           = opts.dragAxis        ?? null;
    const constraintsCfg = opts.dragConstraints ?? null;
    const dirLock        = !!opts.dragDirectionLock;
    const handleSel      = opts.dragHandle ?? null;
    // Default false (motion.dev semantics): a nested drag stops propagation so it doesn't also
    // start a draggable ancestor. Set dragPropagation to let both move together.
    const propagate      = !!opts.dragPropagation;

    // Elasticity is per-edge ({ left, right, top, bottom }); a plain number still means uniform.
    const elasticCfg = opts.dragElastic;
    const elastic = typeof elasticCfg === 'number'
        ? { left: elasticCfg, right: elasticCfg, top: elasticCfg, bottom: elasticCfg }
        : (elasticCfg ?? { left: 0.35, right: 0.35, top: 0.35, bottom: 0.35 });

    // Static pixel bounds pass through unchanged; element-bounds configs (parent / selector) are
    // measured fresh at each drag start so layout changes between drags are picked up.
    let constraints = null;

    let dragging = false;
    let lockedAxis = null; // null = not yet locked, 'x' or 'y' once detected
    let startPX, startPY, startElX, startElY;
    let lastPX, lastPY, lastT, velX = 0, velY = 0;

    // Shared drag-start body: called by the element's own pointerdown listener and by the
    // external startDrag entry point (BmDragControls).
    const begin = (pointerId, clientX, clientY) => {
        // A compositor (WAAPI) animation may own the transform; commit its current values inline
        // and cancel it so the drag takes over seamlessly. C# resolves its own state from the
        // mirrored plan inside GetCurrentXY below.
        _cancelWaapiForElement(elementId, true);
        // Retrieve starting transform position from C# state synchronously
        const pos = dotnetRef.invokeMethod('GetCurrentXY');
        startElX = pos ? pos.x : 0;
        startElY = pos ? pos.y : 0;
        startPX = clientX; startPY = clientY;
        lastPX = clientX; lastPY = clientY; lastT = performance.now();
        velX = velY = 0;
        dragging = true;
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
    let _resizeObserver = null;
    const _remeasure = () => {
        if (dragging) constraints = _resolveDragConstraints(constraintsCfg, el, startElX, startElY);
    };
    const _needsRemeasure = () =>
        !!constraintsCfg && (!!constraintsCfg.parent || typeof constraintsCfg.selector === 'string');
    function _observeConstraintChanges() {
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
    function _stopObservingConstraintChanges() {
        if (_resizeObserver) { _resizeObserver.disconnect(); _resizeObserver = null; }
        window.removeEventListener('scroll', _remeasure, true);
        window.removeEventListener('resize', _remeasure);
    }

    const onDown = (e) => {
        if (e.button !== 0 && e.pointerType !== 'touch') return;
        // Handle mode: only start when the press lands on (or inside) a matching descendant.
        if (handleSel && !(e.target instanceof Element && e.target.closest(handleSel))) return;
        // Isolate nested draggables unless propagation is opted in (motion.dev's dragPropagation).
        if (!propagate) e.stopPropagation();
        begin(e.pointerId, e.clientX, e.clientY);
    };

    const onMove = (e) => {
        if (!dragging) return;
        const now = performance.now(), dt = now - lastT;
        if (dt > 0) { velX = (e.clientX - lastPX) / dt * FRAME_MS; velY = (e.clientY - lastPY) / dt * FRAME_MS; }
        lastPX = e.clientX; lastPY = e.clientY; lastT = now;

        // Direction lock detection
        let effectiveAxis = axis;
        if (dirLock && !lockedAxis) {
            const dx = Math.abs(e.clientX - startPX), dy = Math.abs(e.clientY - startPY);
            if (dx > 3 || dy > 3) lockedAxis = dx >= dy ? 'x' : 'y';
        }
        if (dirLock && lockedAxis) effectiveAxis = lockedAxis;

        let x = startElX + (effectiveAxis === 'y' ? 0 : e.clientX - startPX);
        let y = startElY + (effectiveAxis === 'x' ? 0 : e.clientY - startPY);

        if (constraints) {
            const clamped = clampToConstraints(x, y, constraints, elastic);
            x = clamped.x; y = clamped.y;
        }

        // Sync drag position into C# state synchronously so ComputeFrame picks it up
        dotnetRef.invokeMethod('SetDragPosition', x, y);
        dotnetRef.invokeMethodAsync('OnDragMove');
    };

    const onUp = (e) => {
        if (!dragging) return;
        dragging = false;
        _stopObservingConstraintChanges();
        // Report the constraints actually used for this drag so C#'s release logic (momentum
        // clamping / snap-back) uses the same bounds - essential for element-bounds constraints,
        // which only exist in resolved form here.
        const c = constraints;
        dotnetRef.invokeMethodAsync('OnPointerUp_Drag', velX, velY,
            c?.left ?? null, c?.right ?? null, c?.top ?? null, c?.bottom ?? null);
    };

    // With a drag handle the grab cursor belongs on the handle (styled by the consumer), not
    // the whole element; with the listener disabled the element isn't grabbable at all.
    if (!handleSel && opts.dragListener !== false) el.style.cursor = 'grab';
    el.style.userSelect = 'none';
    el.style.touchAction = axis === 'x' ? 'pan-y' : axis === 'y' ? 'pan-x' : 'none';

    if (opts.dragListener !== false) el.addEventListener('pointerdown', onDown);
    el.addEventListener('pointermove',   onMove);
    el.addEventListener('pointerup',     onUp);
    el.addEventListener('pointercancel', onUp);
    _dragStarters.set(elementId, begin);
    cleanups.push(() => {
        el.removeEventListener('pointerdown',   onDown);
        el.removeEventListener('pointermove',   onMove);
        el.removeEventListener('pointerup',     onUp);
        el.removeEventListener('pointercancel', onUp);
        _stopObservingConstraintChanges();
        _dragStarters.delete(elementId);
        el.style.cursor = el.style.userSelect = el.style.touchAction = '';
    });
}

const _dragStarters = new Map(); // elementId → begin(pointerId, clientX, clientY)

/**
 * Starts a drag on a drag-enabled element from an external pointer event
 * (BmDragControls.StartAsync). No-op when the element has no drag attached.
 */
export function startDrag(elementId, pointerId, clientX, clientY) {
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
const _popped = new Map(); // elementId → prior inline style values (for un-popping on revival)

export function popLayout(elementId, curX, curY) {
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

/** Restores the inline styles replaced by popLayout (used when an exit is cancelled). */
export function unpopLayout(elementId) {
    const el = document.getElementById(elementId);
    const prev = _popped.get(elementId);
    _popped.delete(elementId);
    if (!el || !prev) return;
    for (const [prop, value] of Object.entries(prev)) el.style[prop] = value;
}

/**
 * Resolves a drag-constraints config into pixel bounds on the element's transform x/y.
 * Static pixel configs pass through; element-bounds configs ({ parent: true } or
 * { selector: "..." }) are measured now: the allowed transform range is the container rect
 * minus the element's untransformed rect (its current rect with the active x/y backed out).
 */
function _resolveDragConstraints(cfg, el, curX, curY) {
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

const _waapiAnims = new Map(); // elementId → Map<token, Animation>

/** True when the browser supports the linear() easing function (needed for spring offload). */
export function supportsLinearEasing() {
    try {
        return typeof CSS !== 'undefined' &&
            CSS.supports?.('animation-timing-function', 'linear(0, 1)') === true;
    } catch {
        return false;
    }
}

/**
 * Plays a pre-sampled animation via element.animate(). Resolves true when the animation
 * finishes naturally (styles committed inline), false when it is cancelled or fails to start.
 * timing.iterations: -1 means Infinity.
 */
export function playWaapiAnimation(elementId, token, keyframes, timing) {
    const el = document.getElementById(elementId);
    if (!el || typeof el.animate !== 'function') return Promise.resolve(false);

    let anim;
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
            try { anim.commitStyles(); } catch { /* detached element */ }
            anim.cancel();
            return true;
        },
        () => { cleanup(); return false; } // cancelled (interrupted) - engine already reseeded state
    );
}

/** Cancels one offloaded animation; commit=true snapshots current values inline first. */
export function cancelWaapiAnimation(elementId, token, commit) {
    const anim = _waapiAnims.get(elementId)?.get(token);
    if (!anim) return;
    if (commit) { try { anim.commitStyles(); } catch { /* detached element */ } }
    anim.cancel(); // rejects anim.finished → cleanup runs in playWaapiAnimation's handler
}

function _cancelWaapiForElement(elementId, commit) {
    const map = _waapiAnims.get(elementId);
    if (!map) return;
    for (const anim of [...map.values()]) {
        if (commit) { try { anim.commitStyles(); } catch { } }
        anim.cancel();
    }
}

//
// Viewport observation (whileInView)
//

// Cache observers keyed by their options signature so we can re-use them. Each entry tracks the
// element IDs it currently observes so the observer can be disconnected once it falls empty
// (otherwise distinct margin/threshold combinations would accumulate observers forever).
const _vpObservers = new Map(); // sig → { observer, members: Set<elementId> }
const _vpRefs      = new Map(); // elementId → { dotnetRef, once }

function _vpSig(margin, threshold) { return `${margin}|${threshold}`; }

function _getVpEntry(margin, threshold) {
    const sig = _vpSig(margin, threshold);
    let entry = _vpObservers.get(sig);
    if (entry) return entry;
    const observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
            const id  = entry.target.getAttribute('data-bmid');
            const ref = _vpRefs.get(id);
            if (!ref) continue;
            ref.dotnetRef.invokeMethodAsync('OnIntersect', entry.isIntersecting);
            if (ref.once && entry.isIntersecting) {
                _detachFromObservers(entry.target, id);
                _vpRefs.delete(id);
            }
        }
    }, { rootMargin: margin || '0px', threshold: threshold ?? 0 });
    entry = { observer, members: new Set() };
    _vpObservers.set(sig, entry);
    return entry;
}

// Unobserve an element from every observer that might track it, dropping membership and
// disconnecting (and evicting) any observer left with no members.
function _detachFromObservers(el, elementId) {
    for (const [sig, entry] of _vpObservers) {
        if (el) entry.observer.unobserve(el);
        entry.members.delete(elementId);
        if (entry.members.size === 0) {
            entry.observer.disconnect();
            _vpObservers.delete(sig);
        }
    }
}

export function observeViewport(elementId, dotnetRef, options) {
    const el = document.getElementById(elementId);
    if (!el) return;
    const once      = options?.once      ?? false;
    const margin    = options?.margin    ?? '0px';
    const threshold = options?.threshold ?? 0;
    // Detach from any previously assigned observer first so re-observing with different options
    // doesn't stack duplicate subscriptions (which would fire OnIntersect multiple times and
    // break the "once" behaviour across option changes).
    _detachFromObservers(el, elementId);
    _vpRefs.set(elementId, { dotnetRef, once });
    const entry = _getVpEntry(margin, threshold);
    entry.members.add(elementId);
    entry.observer.observe(el);
}

export function unobserveViewport(elementId) {
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
export function getBoundingRect(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    const sx = window.scrollX, sy = window.scrollY;
    return {
        x: r.x + sx, y: r.y + sy, width: r.width, height: r.height,
        top: r.top + sy, left: r.left + sx,
    };
}

/**
 * Run a FLIP animation via the Web Animations API.
 * The element is currently at its NEW layout position; this animates it
 * from the OLD (inverted) position to identity.
 */
export function playWaapiFlip(elementId, dx, dy, sx, sy, durationMs, easingStr, finalTransform) {
    const el = document.getElementById(elementId);
    if (!el) return;
    el.style.transformOrigin = '0 0';
    const timing = { duration: durationMs, easing: easingStr || 'ease', fill: 'forwards' };
    const anim = el.animate(
        [
            { transform: `translate(${dx}px,${dy}px) scaleX(${sx}) scaleY(${sy})` },
            { transform: 'translate(0px,0px) scaleX(1) scaleY(1)' },
        ],
        timing
    );

    // Size-mode correction (plan item 1.2): while the box scales, counter-scale direct children and
    // correct the border-radius so text/children don't stretch and corners stay round. Position-mode
    // FLIP passes sx=sy=1, so this whole block is skipped and behaviour is unchanged there.
    // These end at their natural values, so they use fill:'none' and leave no persistent override.
    const cleanups = [];
    if (Math.abs(sx - 1) > 1e-4 || Math.abs(sy - 1) > 1e-4) {
        const correctTiming = { duration: durationMs, easing: easingStr || 'ease', fill: 'none' };

        const radius = parseFloat(getComputedStyle(el).borderTopLeftRadius) || 0;
        if (radius > 0) {
            const r = correctedRadius(radius, sx, sy);
            el.animate(
                [
                    { borderRadius: `${r.fromX}px / ${r.fromY}px` },
                    { borderRadius: `${radius}px / ${radius}px` },
                ],
                correctTiming
            );
        }

        // Measure children relative to the (untransformed) parent before the transform takes hold.
        const pRect = el.getBoundingClientRect();
        for (const child of el.children) {
            if (!(child instanceof HTMLElement)) continue;
            const c = flipChildCorrection(pRect, child.getBoundingClientRect(), sx, sy, dx, dy);
            const prevOrigin = child.style.transformOrigin;
            child.style.transformOrigin = `${c.originX}px ${c.originY}px`;
            // translate first (outermost), then the inverse scale - the exact inverse of the
            // parent's `translate(dx,dy) scale(sx,sy)`, so the child neither stretches nor jumps.
            child.animate(
                [
                    { transform: `translate(${c.fromTranslateX}px,${c.fromTranslateY}px) scaleX(${c.fromScaleX}) scaleY(${c.fromScaleY})` },
                    { transform: 'translate(0px,0px) scaleX(1) scaleY(1)' },
                ],
                correctTiming
            );
            cleanups.push(() => { child.style.transformOrigin = prevOrigin; });
        }
    }

    // Run on finish AND cancel: a cancelled flip must not leave the '0 0' transform-origin (or
    // the children's counter-transform origins) stuck on the elements.
    const settle = () => {
        el.style.transform = finalTransform || '';
        el.style.transformOrigin = '';
        for (const done of cleanups) done();
    };
    anim.onfinish = settle;
    anim.oncancel = settle;
}

// 
// Scroll tracking
// 

let _scrollKeySeq = 0;
const _scrollSubs = new Map(); // key  cleanup fn

export function observeScroll(containerId, dotnetRef, options) {
    const el = containerId ? document.getElementById(containerId) : window;
    if (!el) return null;
    const key = `scroll_${++_scrollKeySeq}`;
    const targetId = options?.targetId ?? null;
    const offsets = options?.offsets ?? null; // [[targetFrac, containerFrac], [targetFrac, containerFrac]]

    const onScroll = () => {
        let sX, sY, sW, sH, cW, cH;
        if (el === window) {
            sX = window.scrollX; sY = window.scrollY;
            sW = document.documentElement.scrollWidth;
            sH = document.documentElement.scrollHeight;
            cW = window.innerWidth; cH = window.innerHeight;
        } else {
            sX = el.scrollLeft; sY = el.scrollTop;
            sW = el.scrollWidth; sH = el.scrollHeight;
            cW = el.clientWidth; cH = el.clientHeight;
        }
        const pX = scrollFraction(sX, sW, cW);
        const pY = scrollFraction(sY, sH, cH);

        // Target progress: 0 when the target sits at the first configured alignment, 1 at the
        // second. Both alignment "distances" shift equally per scrolled pixel, so the current
        // fraction is d0 / (d0 - d1), computed straight from viewport-relative rects.
        let tp = null;
        if (targetId && offsets) {
            const target = document.getElementById(targetId);
            if (target) {
                const tr = target.getBoundingClientRect();
                let cTop, cHeight;
                if (el === window) { cTop = 0; cHeight = window.innerHeight; }
                else { cTop = el.getBoundingClientRect().top; cHeight = el.clientHeight; }
                const d0 = (tr.top + offsets[0][0] * tr.height) - (cTop + offsets[0][1] * cHeight);
                const d1 = (tr.top + offsets[1][0] * tr.height) - (cTop + offsets[1][1] * cHeight);
                const span = d0 - d1;
                tp = span !== 0 ? Math.min(1, Math.max(0, d0 / span)) : 0;
            }
        }

        dotnetRef.invokeMethodAsync('OnScroll', {
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

export function unobserveScroll(key) {
    _scrollSubs.get(key)?.();
    _scrollSubs.delete(key);
}

// ── View Transitions API ────────────────────────────────────────────────────

// Wrap document.startViewTransition around a C# DOM-update callback. The callback (invoked by
// name on the dotnet ref) performs the Blazor state change; the browser snapshots before/after and
// cross-fades. Falls back to running the callback directly when the API is unsupported.
export async function startViewTransition(dotnetRef, callbackName) {
    const runUpdate = () => dotnetRef.invokeMethodAsync(callbackName);
    if (typeof document.startViewTransition !== 'function') {
        await runUpdate();
        return false;
    }
    const transition = document.startViewTransition(() => runUpdate());
    try { await transition.finished; }
    catch {
        // `finished` also rejects when the C# update callback threw - that IS an error and must
        // reach the caller. Re-awaiting updateCallbackDone rethrows the callback failure, while a
        // merely skipped/aborted transition (callback succeeded) resolves and stays swallowed.
        await transition.updateCallbackDone;
    }
    return true;
}
