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

function _applyStyles(el, styles) {
    for (const prop in styles) {
        if (prop.startsWith('--')) el.style.setProperty(prop, styles[prop]);
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
    // Detach from every viewport observer (drops membership and evicts empty observers).
    _detachFromObservers(el, elementId);
    _vpRefs.delete(elementId);
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
        el.addEventListener('pointerdown', onDown);
        window.addEventListener('pointerup',     onUp);
        window.addEventListener('pointercancel', onCancel);
        cleanups.push(() => {
            el.removeEventListener('pointerdown', onDown);
            window.removeEventListener('pointerup',     onUp);
            window.removeEventListener('pointercancel', onCancel);
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
    const axis        = opts.dragAxis        ?? null;
    const constraints = opts.dragConstraints ?? null;
    const elastic     = typeof opts.dragElastic === 'number' ? opts.dragElastic : 0.35;
    const dirLock     = !!opts.dragDirectionLock;

    let dragging = false;
    let lockedAxis = null; // null = not yet locked, 'x' or 'y' once detected
    let startPX, startPY, startElX, startElY;
    let lastPX, lastPY, lastT, velX = 0, velY = 0;

    function applyElastic(overflow) {
        return elastic > 0 ? overflow * elastic : 0;
    }

    const onDown = (e) => {
        if (e.button !== 0 && e.pointerType !== 'touch') return;
        // Retrieve starting transform position from C# state synchronously
        const pos = dotnetRef.invokeMethod('GetCurrentXY');
        startElX = pos ? pos.x : 0;
        startElY = pos ? pos.y : 0;
        startPX = e.clientX; startPY = e.clientY;
        lastPX = e.clientX; lastPY = e.clientY; lastT = performance.now();
        velX = velY = 0;
        dragging = true;
        lockedAxis = null;
        el.setPointerCapture(e.pointerId);
        dotnetRef.invokeMethodAsync('OnPointerDown_Drag');
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
            if (constraints.left   != null && x < constraints.left)   x = constraints.left   - applyElastic(constraints.left   - x);
            if (constraints.right  != null && x > constraints.right)  x = constraints.right  + applyElastic(x - constraints.right);
            if (constraints.top    != null && y < constraints.top)    y = constraints.top    - applyElastic(constraints.top    - y);
            if (constraints.bottom != null && y > constraints.bottom) y = constraints.bottom + applyElastic(y - constraints.bottom);
        }

        // Sync drag position into C# state synchronously so ComputeFrame picks it up
        dotnetRef.invokeMethod('SetDragPosition', x, y);
        dotnetRef.invokeMethodAsync('OnDragMove');
    };

    const onUp = (e) => {
        if (!dragging) return;
        dragging = false;
        dotnetRef.invokeMethodAsync('OnPointerUp_Drag', velX, velY);
    };

    el.style.cursor     = 'grab';
    el.style.userSelect = 'none';
    el.style.touchAction = axis === 'x' ? 'pan-y' : axis === 'y' ? 'pan-x' : 'none';

    el.addEventListener('pointerdown',   onDown);
    el.addEventListener('pointermove',   onMove);
    el.addEventListener('pointerup',     onUp);
    el.addEventListener('pointercancel', onUp);
    cleanups.push(() => {
        el.removeEventListener('pointerdown',   onDown);
        el.removeEventListener('pointermove',   onMove);
        el.removeEventListener('pointerup',     onUp);
        el.removeEventListener('pointercancel', onUp);
        el.style.cursor = el.style.userSelect = el.style.touchAction = '';
    });
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

/** Returns the element's DOMRect as a plain object for C# to snapshot. */
export function getBoundingRect(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, left: r.left };
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
    const anim = el.animate(
        [
            { transform: `translate(${dx}px,${dy}px) scaleX(${sx}) scaleY(${sy})` },
            { transform: 'translate(0px,0px) scaleX(1) scaleY(1)' },
        ],
        { duration: durationMs, easing: easingStr || 'ease', fill: 'forwards' }
    );
    anim.onfinish = () => {
        el.style.transform = finalTransform || '';
        el.style.transformOrigin = '';
    };
}

// 
// Scroll tracking
// 

let _scrollKeySeq = 0;
const _scrollSubs = new Map(); // key  cleanup fn

export function observeScroll(containerId, dotnetRef) {
    const el = containerId ? document.getElementById(containerId) : window;
    if (!el) return null;
    const key = `scroll_${++_scrollKeySeq}`;

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
        const pX = sW > cW ? sX / (sW - cW) : 0;
        const pY = sH > cH ? sY / (sH - cH) : 0;
        dotnetRef.invokeMethodAsync('OnScroll', {
            scrollX: sX, scrollY: sY,
            progressX: pX, progressY: pY,
            scrollWidth: sW, scrollHeight: sH,
            clientWidth: cW, clientHeight: cH,
        });
    };

    el.addEventListener('scroll', onScroll, { passive: true });
    _scrollSubs.set(key, () => el.removeEventListener('scroll', onScroll));
    onScroll(); // fire immediately with current position
    return key;
}

export function unobserveScroll(key) {
    _scrollSubs.get(key)?.();
    _scrollSubs.delete(key);
}
