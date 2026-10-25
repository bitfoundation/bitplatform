/*!
 * Bit.Bonboarding browser layer.
 *
 * This module does only what the DOM alone can do: find elements, measure them, watch them for
 * movement, scroll them into view, and route keyboard events. Every decision -- placement,
 * step selection, waiting policy, persistence -- stays in C#.
 *
 * Measurements are batched onto a single animation frame and de-duplicated, so a resize storm or a
 * momentum scroll produces at most one interop call per frame, and none at all while nothing moves.
 */

// ---------------------------------------------------------------------------
// Contracts shared with the C# interop (camelCase JSON of the Bonboarding* records)
// ---------------------------------------------------------------------------

interface DotNetObjectReference {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface BitBonboardingRect { x: number; y: number; width: number; height: number; }

interface BitBonboardingSize { width: number; height: number; }

interface TargetDescriptor {
    kind: 'None' | 'Selector' | 'Element';
    selector?: string | null;
    element?: Element | null;
}

interface AdvanceTriggerDescriptor {
    eventName: string;
    selector?: string | null;
    matchSelector?: string | null;
    delayMs: number;
}

interface StepActivation {
    sessionId: string;
    // Echoed back with every step-scoped callback so .NET can drop ones that outlived their step.
    activation: number;
    target: TargetDescriptor;
    additionalTargets?: TargetDescriptor[] | null;
    waitForTarget: boolean;
    waitTimeoutMs: number;
    scroll: 'smooth' | 'instant' | 'auto' | 'none';
    scrollPadding: number;
    interaction: 'blocked' | 'target' | 'free';
    closeOnEscape: boolean;
    keyboardNavigation: boolean;
    // Per step: a step may switch the trap off that its tour switched on, and the other way round.
    trapFocus: boolean;
    advanceOn?: AdvanceTriggerDescriptor | null;
}

interface StepGeometry {
    found: boolean;
    target: BitBonboardingRect;
    viewport: BitBonboardingRect;
    popover: BitBonboardingSize;
    targetVisible: boolean;
    sequence: number;
    activation: number;
    popoverRadius: number | null;
    // A target selector the browser cannot parse. Reported at once rather than waited on.
    invalidSelector?: string | null;
}

interface BrowserEnvironment {
    reducedMotion: boolean;
    rightToLeft: boolean;
    viewport: BitBonboardingSize;
    coarsePointer: boolean;
}

interface Session {
    id: string;
    ref: DotNetObjectReference | null;
    targets: Element[];
    // The ancestors of the (first) target that clip its overflow, found when the targets are bound rather than on
    // every frame: overflow styles rarely change, and walking the tree costs a style lookup per level.
    clips: Element[];
    popover: HTMLElement | null;
    spec: StepActivation | null;
    // Observers and listeners, all torn down together in clearStep().
    resizeObserver: ResizeObserver | null;
    intersectionObserver: IntersectionObserver | null;
    mutationObserver: MutationObserver | null;
    scrollListener: (() => void) | null;
    resizeListener: (() => void) | null;
    keyListener: ((event: KeyboardEvent) => void) | null;
    advanceListener: ((event: Event) => void) | null;
    advanceElement: Element | null;
    advanceEventName: string | null;
    // While waiting: re-checks once a CSS transition or animation ends, for an element that is in the DOM
    // but only becomes visible as it animates in, which no mutation reports.
    settleListener: (() => void) | null;
    // While a step shows: DOM changes and the end of transitions, which move or hide a target without
    // resizing it or the document (a sibling growing inside a fixed-height pane, a visibility change).
    layoutObserver: MutationObserver | null;
    layoutListener: (() => void) | null;
    waitTimer: number | null;
    advanceTimer: number | null;
    frameHandle: number;
    lastFrame: StepGeometry | null;
    previousFocus: HTMLElement | null;
    trapFocus: boolean;
    disposed: boolean;
}

const EPSILON = 0.5;
const ANCHOR_ATTRIBUTE = 'data-bonboarding-anchor';
const AUTOFOCUS_ATTRIBUTE = 'data-bonboarding-autofocus';
const sessions = new Map<string, Session>();

let sequence = 0;
let diagnostics = false;

// ---------------------------------------------------------------------------
// Environment
// ---------------------------------------------------------------------------

export function initialize(options?: { enableJsDiagnostics?: boolean }): BrowserEnvironment {
    diagnostics = !!(options && options.enableJsDiagnostics);

    return {
        reducedMotion: matchMediaSafe('(prefers-reduced-motion: reduce)'),
        rightToLeft: documentDirection() === 'rtl',
        viewport: { width: viewportWidth(), height: viewportHeight() },
        coarsePointer: matchMediaSafe('(pointer: coarse)')
    };
}

function matchMediaSafe(query: string): boolean {
    try {
        return typeof window.matchMedia === 'function' && window.matchMedia(query).matches;
    } catch {
        return false;
    }
}

function documentDirection(): string {
    try {
        return getComputedStyle(document.documentElement).direction;
    } catch {
        return 'ltr';
    }
}

function viewportWidth(): number {
    return document.documentElement.clientWidth || window.innerWidth || 0;
}

function viewportHeight(): number {
    return document.documentElement.clientHeight || window.innerHeight || 0;
}

function warn(message: string, error?: unknown) {
    if (diagnostics) console.warn('[Bit.Bonboarding] ' + message, error || '');
}

// ---------------------------------------------------------------------------
// Session lifecycle
// ---------------------------------------------------------------------------

export function attach(sessionId: string, dotNetRef: DotNetObjectReference) {
    detach(sessionId);

    sessions.set(sessionId, {
        id: sessionId,
        ref: dotNetRef,
        targets: [],
        clips: [],
        popover: null,
        spec: null,
        resizeObserver: null,
        intersectionObserver: null,
        mutationObserver: null,
        scrollListener: null,
        resizeListener: null,
        keyListener: null,
        advanceListener: null,
        advanceElement: null,
        advanceEventName: null,
        settleListener: null,
        layoutObserver: null,
        layoutListener: null,
        waitTimer: null,
        advanceTimer: null,
        frameHandle: 0,
        lastFrame: null,
        previousFocus: null,
        trapFocus: false,
        disposed: false
    });
}

export function detach(sessionId: string) {
    const session = sessions.get(sessionId);
    if (!session) return;

    session.disposed = true;
    clearStep(session);
    session.popover = null;
    session.previousFocus = null;
    session.ref = null;
    sessions.delete(sessionId);
}

// ---------------------------------------------------------------------------
// Step activation
// ---------------------------------------------------------------------------

export async function activateStep(spec: StepActivation): Promise<StepGeometry> {
    const session = sessions.get(spec.sessionId);
    if (!session) return emptyGeometry(spec.activation);

    clearStep(session);
    session.spec = spec;
    session.trapFocus = !!spec.trapFocus;

    // A selector the browser cannot parse will never match, so waiting for it would only delay the
    // inevitable by the whole timeout. It is reported with the first measurement instead, and .NET names
    // the step and applies the timeout behaviour straight away.
    const invalid = invalidSelectorOf(spec);
    const elements = invalid === null ? resolveAll(spec) : [];

    // Nothing found and the caller is willing to wait: watch the DOM instead of polling it. The
    // keyboard is wired up straight away so Escape can still leave a tour stuck on a missing target.
    if (elements.length === 0 && wantsTarget(spec) && spec.waitForTarget && invalid === null) {
        // Nothing is on screen to protect while the step waits, and the user may be the one who has to make
        // the element appear (open the menu it lives in), so the page keeps its keyboard: no trap, and keys
        // pressed in the application stay the application's. The step's own settings return with the target.
        session.trapFocus = false;
        attachKeyboard(session, { ...spec, interaction: 'free', trapFocus: false });
        startWaiting(session, spec);
        return emptyGeometry(spec.activation);
    }

    session.targets = elements;

    if (elements.length > 0) {
        await scrollIntoView(elements, spec.scroll, spec.scrollPadding);
    }

    // The step may have been replaced or the session detached while the smooth scroll settled.
    if (session.disposed || session.spec !== spec) return emptyGeometry(spec.activation);

    observe(session);
    attachKeyboard(session, spec);
    attachAdvanceTrigger(session, spec);

    const geometry = measureSession(session);
    geometry.invalidSelector = invalid;
    session.lastFrame = geometry;
    return geometry;
}

export function deactivateStep(sessionId: string) {
    const session = sessions.get(sessionId);
    if (session) clearStep(session);
}

export function measure(sessionId: string): StepGeometry {
    const session = sessions.get(sessionId);
    if (!session) return emptyGeometry();

    // Re-resolve first: selector targets are routinely replaced by a Blazor re-render. When nothing is found
    // the old targets are kept and measure as missing, which .NET handles as a lost target.
    if (session.spec) {
        const elements = resolveAll(session.spec);
        if (elements.length > 0) {
            const changed = elements.length !== session.targets.length
                || elements.some((el, i) => el !== session.targets[i]);

            if (changed) rebind(session, elements);
        }
    }

    const geometry = measureSession(session);

    // This reply reports the loss, so the targets go as they do when a frame finds them gone: otherwise the next
    // frame would find them missing too and report the same loss a second time. As there, a target the user just
    // acted on is not lost while its step is about to move on.
    if (!geometry.found && session.targets.length > 0 && !session.advanceTimer) session.targets = [];

    session.lastFrame = geometry;
    return geometry;
}

export function setPopover(sessionId: string, element: HTMLElement | null, trapFocus: boolean) {
    const session = sessions.get(sessionId);
    if (!session) return;

    session.popover = element || null;

    // The popover is registered once per tour, so the value sent here is only right for the step
    // that was current at the time. Each activation brings its own; this one is kept for the gap
    // before the first activation lands.
    if (!session.spec) session.trapFocus = !!trapFocus;

    if (session.resizeObserver && session.popover) {
        session.resizeObserver.observe(session.popover);
        // The popover has a size now, so the placement C# is waiting for can be computed.
        scheduleFrame(session);
    }
}

// ---------------------------------------------------------------------------
// Target resolution
// ---------------------------------------------------------------------------

function wantsTarget(spec: StepActivation): boolean {
    return !!spec.target && spec.target.kind !== 'None';
}

/**
 * Finds the element a descriptor points at, but only one that is actually rendered: an element that is
 * display:none, inside a closed details element, visibility:hidden or has no size cannot be pointed at, so
 * it counts as missing and the step's missing-target behaviour decides (wait for it to show, skip, centre).
 * A selector matching several elements picks the first rendered one, which is what a responsive layout
 * with a desktop and a mobile copy of the same control needs.
 */
function resolveOne(descriptor: TargetDescriptor | null | undefined): Element | null {
    if (!descriptor) return null;

    try {
        if (descriptor.kind === 'Element') {
            // Blazor revives an ElementReference into the real element for us.
            const element = descriptor.element;
            return element instanceof Element && isRendered(element) ? element : null;
        }

        if (descriptor.kind === 'Selector' && descriptor.selector) {
            // The first match is nearly always the one, and costs a single lookup.
            const first = document.querySelector(descriptor.selector);
            if (!first) return null;
            if (isRendered(first)) return first;

            const all = document.querySelectorAll(descriptor.selector);
            for (let i = 1; i < all.length; i++) {
                if (isRendered(all[i])) return all[i];
            }
        }
    } catch (error) {
        warn('Could not resolve a target.', error);
    }

    return null;
}

/** True when an element has a box on screen that a spotlight can be drawn around. */
function isRendered(element: Element): boolean {
    if (!element.isConnected) return false;

    const rects = element.getClientRects();
    let sized = false;
    for (let i = 0; i < rects.length; i++) {
        if (rects[i].width > 0 || rects[i].height > 0) {
            sized = true;
            break;
        }
    }

    if (!sized) return false;

    try {
        const visibility = getComputedStyle(element).visibility;
        return visibility !== 'hidden' && visibility !== 'collapse';
    } catch {
        return true;
    }
}

/** The first target selector of a step that the browser cannot parse, or null when every one is valid. */
function invalidSelectorOf(spec: StepActivation): string | null {
    const descriptors = [spec.target, ...(spec.additionalTargets || [])];

    for (const descriptor of descriptors) {
        if (!descriptor || descriptor.kind !== 'Selector' || !descriptor.selector) continue;

        try {
            document.querySelector(descriptor.selector);
        } catch {
            return descriptor.selector;
        }
    }

    return null;
}

/** Points the step at new target elements: observers, and the advance trigger, move to them. */
function rebind(session: Session, elements: Element[]) {
    session.targets = elements;
    observe(session);

    // The trigger listened on the old node. A pending advance is kept: it was heard, and is on its way.
    if (session.spec) attachAdvanceTrigger(session, session.spec);
}

function resolveAll(spec: StepActivation): Element[] {
    const found: Element[] = [];

    const primary = resolveOne(spec.target);
    if (primary) found.push(primary);

    if (spec.additionalTargets) {
        for (const descriptor of spec.additionalTargets) {
            const element = resolveOne(descriptor);
            if (element && !found.includes(element)) found.push(element);
        }
    }

    return found;
}

// ---------------------------------------------------------------------------
// Waiting for an element that does not exist yet
// ---------------------------------------------------------------------------

function startWaiting(session: Session, spec: StepActivation) {
    let settled = false;

    const finish = (elements: Element[]) => {
        if (settled || session.disposed) return;
        settled = true;

        stopWaiting(session);

        session.targets = elements;

        scrollIntoView(elements, spec.scroll, spec.scrollPadding)
            .then(() => {
                if (session.disposed || session.spec !== spec) return;
                observe(session);
                session.trapFocus = !!spec.trapFocus;
                attachKeyboard(session, spec);
                attachAdvanceTrigger(session, spec);
                push(session, measureSession(session));
            })
            .catch((error) => warn('Scrolling a late target into view failed.', error));
    };

    const check = () => {
        const elements = resolveAll(spec);
        if (elements.length > 0) finish(elements);
    };

    // Mutation callbacks fire in bursts; coalescing onto a frame keeps querySelector cheap.
    const scheduleCheck = () => {
        if (settled || session.frameHandle) return;
        session.frameHandle = requestAnimationFrame(() => {
            session.frameHandle = 0;
            check();
        });
    };

    session.mutationObserver = new MutationObserver(scheduleCheck);

    session.mutationObserver.observe(document.documentElement, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: [ANCHOR_ATTRIBUTE, 'id', 'class', 'hidden', 'style', 'open']
    });

    // An element that exists but is still hidden may become visible by animating in (a menu that scales up,
    // a drawer that slides out), which changes no attribute. The end of the transition says it is done.
    session.settleListener = scheduleCheck;
    document.addEventListener('transitionend', scheduleCheck, true);
    document.addEventListener('animationend', scheduleCheck, true);

    if (spec.waitTimeoutMs > 0) {
        session.waitTimer = window.setTimeout(() => {
            if (settled || session.disposed) return;
            settled = true;
            stopWaiting(session);
            invoke(session, 'OnWaitTimedOut', spec.activation);
        }, spec.waitTimeoutMs);
    }

    // The element may already be there by the time the observer is wired up.
    check();
}

function stopWaiting(session: Session) {
    if (session.mutationObserver) {
        session.mutationObserver.disconnect();
        session.mutationObserver = null;
    }

    if (session.settleListener) {
        document.removeEventListener('transitionend', session.settleListener, true);
        document.removeEventListener('animationend', session.settleListener, true);
        session.settleListener = null;
    }

    if (session.waitTimer) {
        clearTimeout(session.waitTimer);
        session.waitTimer = null;
    }

    if (session.frameHandle) {
        cancelAnimationFrame(session.frameHandle);
        session.frameHandle = 0;
    }
}

// ---------------------------------------------------------------------------
// Observation
// ---------------------------------------------------------------------------

function observe(session: Session) {
    disconnectObservers(session);

    session.clips = session.targets.length > 0 ? clippingAncestors(session.targets[0]) : [];

    const schedule = () => scheduleFrame(session);

    if (typeof ResizeObserver === 'function') {
        session.resizeObserver = new ResizeObserver(schedule);
        session.resizeObserver.observe(document.documentElement);
        for (const element of session.targets) session.resizeObserver.observe(element);
        if (session.popover) session.resizeObserver.observe(session.popover);
    }

    if (typeof IntersectionObserver === 'function' && session.targets.length > 0) {
        // Thresholds at both ends catch a target sliding in and out of a scroll container.
        session.intersectionObserver = new IntersectionObserver(schedule, { threshold: [0, 0.01, 0.99, 1] });
        for (const element of session.targets) session.intersectionObserver.observe(element);
    }

    // Capture-phase scroll catches every scrollable ancestor without having to find them.
    session.scrollListener = schedule;
    window.addEventListener('scroll', schedule, { passive: true, capture: true });

    session.resizeListener = schedule;
    window.addEventListener('resize', schedule, { passive: true });

    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', schedule, { passive: true });
        window.visualViewport.addEventListener('scroll', schedule, { passive: true });
    }

    // A target can move or disappear with nothing resized: a sibling growing inside a fixed-height pane,
    // a class that sets visibility: hidden. Mutations and finished transitions catch those. The tour's own
    // DOM is ignored, or every placement it renders would schedule a frame of its own.
    if (typeof MutationObserver === 'function') {
        session.layoutObserver = new MutationObserver((records) => {
            const host = session.popover ? session.popover.parentElement : null;
            for (const record of records) {
                if (!host || !host.contains(record.target)) {
                    schedule();
                    return;
                }
            }
        });

        session.layoutObserver.observe(document.documentElement, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style', 'hidden', 'open']
        });
    }

    session.layoutListener = schedule;
    document.addEventListener('transitionend', schedule, true);
    document.addEventListener('animationend', schedule, true);
}

function disconnectObservers(session: Session) {
    if (session.resizeObserver) {
        session.resizeObserver.disconnect();
        session.resizeObserver = null;
    }

    if (session.intersectionObserver) {
        session.intersectionObserver.disconnect();
        session.intersectionObserver = null;
    }

    if (session.scrollListener) {
        window.removeEventListener('scroll', session.scrollListener, { capture: true });
        if (window.visualViewport) window.visualViewport.removeEventListener('scroll', session.scrollListener);
        session.scrollListener = null;
    }

    if (session.resizeListener) {
        window.removeEventListener('resize', session.resizeListener);
        if (window.visualViewport) window.visualViewport.removeEventListener('resize', session.resizeListener);
        session.resizeListener = null;
    }

    if (session.layoutObserver) {
        session.layoutObserver.disconnect();
        session.layoutObserver = null;
    }

    if (session.layoutListener) {
        document.removeEventListener('transitionend', session.layoutListener, true);
        document.removeEventListener('animationend', session.layoutListener, true);
        session.layoutListener = null;
    }
}

function scheduleFrame(session: Session) {
    if (session.disposed || session.frameHandle) return;

    session.frameHandle = requestAnimationFrame(() => {
        session.frameHandle = 0;
        if (session.disposed) return;

        let geometry = measureSession(session);

        // A target can be removed by a re-render, or hidden (a panel collapsed, a layout switched to its
        // mobile form), at any moment.
        if (!geometry.found && session.targets.length > 0) {
            const recovered = session.spec ? resolveAll(session.spec) : [];

            if (recovered.length > 0) {
                // Same element on a new DOM node, or its responsive twin: rebind rather than report it lost.
                rebind(session, recovered);
                geometry = measureSession(session);
            } else if (session.advanceTimer) {
                // The user just acted on the target and the step is about to move on. The action that
                // advances it is often the very thing that hid the target; that is not a loss.
                return;
            } else {
                session.targets = [];
                invoke(session, 'OnTargetLost', activationOf(session));
                return;
            }
        }

        if (sameFrame(session.lastFrame, geometry)) return;

        push(session, geometry);
    });
}

function push(session: Session, geometry: StepGeometry) {
    session.lastFrame = geometry;
    invoke(session, 'OnGeometryChanged', geometry);
}

function invoke(session: Session, method: string, arg?: unknown) {
    if (session.disposed || !session.ref) return;

    try {
        const promise = arg === undefined
            ? session.ref.invokeMethodAsync(method, session.id)
            : session.ref.invokeMethodAsync(method, session.id, arg);

        if (promise && typeof promise.catch === 'function') {
            // The circuit can go away between scheduling and delivery; that is not an error.
            promise.catch((error) => warn(method + ' could not be delivered.', error));
        }
    } catch (error) {
        warn(method + ' could not be invoked.', error);
    }
}

// ---------------------------------------------------------------------------
// Measurement
// ---------------------------------------------------------------------------

function activationOf(session: Session): number {
    return session.spec ? session.spec.activation : 0;
}

function measureSession(session: Session): StepGeometry {
    const connectedPopover = session.popover && session.popover.isConnected ? session.popover : null;
    const host = connectedPopover ? connectedPopover.parentElement : null;

    // Everything is reported in the coordinates the host's position: fixed children are laid out in. That is the
    // viewport, unless an ancestor of the host is transformed, filtered or contained, which makes it the containing
    // block instead: the host fills its containing block, so its own box says where that is.
    const hostBox = host ? host.getBoundingClientRect() : null;
    const originX = hostBox ? hostBox.left : 0;
    const originY = hostBox ? hostBox.top : 0;

    const viewport = shift(visibleViewport(host), originX, originY);
    const popover = connectedPopover
        ? { width: connectedPopover.offsetWidth, height: connectedPopover.offsetHeight }
        : { width: 0, height: 0 };
    const popoverRadius = connectedPopover ? cornerRadius(connectedPopover) : null;
    const activation = activationOf(session);

    const connected = session.targets.filter(isRendered);

    if (connected.length === 0) {
        return {
            found: false,
            target: { x: 0, y: 0, width: 0, height: 0 },
            viewport,
            popover,
            targetVisible: false,
            sequence: ++sequence,
            activation,
            popoverRadius
        };
    }

    // Only the part of the target its scrolling ancestors show is pointed at: a long table in a pane is spotlit
    // where it can be seen, not across the rest of the page. A target scrolled entirely out of its pane is pointed
    // at on the edge it went past, and is reported as not visible: its own box is over whatever is next to the pane.
    const shown = clipToAncestors(unionRect(connected), session.clips);
    const target = shift(shown.rect, originX, originY);
    const visible = !shown.hidden
        && target.width > 0
        && target.height > 0
        && target.x < viewport.x + viewport.width
        && target.y < viewport.y + viewport.height
        && target.x + target.width > viewport.x
        && target.y + target.height > viewport.y;

    return {
        found: true,
        target,
        viewport,
        popover,
        targetVisible: visible,
        sequence: ++sequence,
        activation,
        popoverRadius
    };
}

/**
 * The largest corner radius the stylesheet gave the popover, in pixels, so .NET can keep the arrow
 * clear of the rounded corners whatever the theme sets. Null when it cannot be expressed in pixels.
 */
function cornerRadius(element: HTMLElement): number | null {
    try {
        const style = getComputedStyle(element);
        let largest = 0;

        for (const value of [style.borderTopLeftRadius, style.borderTopRightRadius, style.borderBottomRightRadius, style.borderBottomLeftRadius]) {
            // An elliptical corner reads "12px 8px"; the first length is the horizontal radius.
            const first = (value || '').trim().split(/\s+/)[0];
            if (!first) continue;
            if (!first.endsWith('px')) return null;

            const radius = parseFloat(first);
            if (isFinite(radius)) largest = Math.max(largest, radius);
        }

        return largest;
    } catch {
        return null;
    }
}

function shift(rect: BitBonboardingRect, dx: number, dy: number): BitBonboardingRect {
    return dx === 0 && dy === 0 ? rect : { x: rect.x - dx, y: rect.y - dy, width: rect.width, height: rect.height };
}

/**
 * The part of the layout viewport the user can actually see, in viewport coordinates: pinch zoom and the on-screen
 * keyboard shrink it (the visual viewport), and so do the notch and the rounded corners of a phone when the page
 * asks for the whole screen (viewport-fit=cover; the host carries the safe-area insets as its padding).
 */
function visibleViewport(host: Element | null): BitBonboardingRect {
    const width = viewportWidth();
    const height = viewportHeight();
    let left = 0;
    let top = 0;
    let right = width;
    let bottom = height;

    const visual = window.visualViewport;
    if (visual && visual.width > 0 && visual.height > 0) {
        left = Math.max(left, visual.offsetLeft);
        top = Math.max(top, visual.offsetTop);
        right = Math.min(right, visual.offsetLeft + visual.width);
        bottom = Math.min(bottom, visual.offsetTop + visual.height);
    }

    if (host) {
        try {
            const style = getComputedStyle(host);
            left += parseFloat(style.paddingLeft) || 0;
            top += parseFloat(style.paddingTop) || 0;
            right -= parseFloat(style.paddingRight) || 0;
            bottom -= parseFloat(style.paddingBottom) || 0;
        } catch {
            // Detached while measuring; the visual viewport alone is still right.
        }
    }

    // A degenerate area (a keyboard taller than the screen, a measurement mid-rotation) would leave nowhere to place
    // the popover at all; the whole viewport is the better answer then.
    return right - left > 0 && bottom - top > 0
        ? { x: left, y: top, width: right - left, height: bottom - top }
        : { x: 0, y: 0, width, height };
}

/**
 * The part of a rectangle every clipping ancestor shows. One that an ancestor hides entirely is pressed flat against the
 * edge of that ancestor it went past, which is where the shown part shrinks to as it is scrolled out, and is reported
 * hidden. It keeps a pixel of thickness across that edge: .NET reads a rectangle with no area as no target at all.
 */
function clipToAncestors(rect: BitBonboardingRect, clips: Element[]): { rect: BitBonboardingRect; hidden: boolean } {
    let left = rect.x;
    let top = rect.y;
    let right = rect.x + rect.width;
    let bottom = rect.y + rect.height;
    let hidden = false;

    for (const clip of clips) {
        if (!clip.isConnected) continue;

        // The client area: inside the borders, without the scrollbars.
        const box = clip.getBoundingClientRect();
        const clipLeft = box.left + clip.clientLeft;
        const clipTop = box.top + clip.clientTop;
        const clipRight = clipLeft + clip.clientWidth;
        const clipBottom = clipTop + clip.clientHeight;

        hidden = hidden || right <= clipLeft || left >= clipRight || bottom <= clipTop || top >= clipBottom;

        left = clamp(left, clipLeft, clipRight);
        top = clamp(top, clipTop, clipBottom);
        right = clamp(right, clipLeft, clipRight);
        bottom = clamp(bottom, clipTop, clipBottom);
    }

    if (hidden) {
        if (right - left < 1) {
            left -= 0.5;
            right = left + 1;
        }

        if (bottom - top < 1) {
            top -= 0.5;
            bottom = top + 1;
        }
    }

    return { rect: { x: left, y: top, width: right - left, height: bottom - top }, hidden };
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max);
}

function unionRect(elements: Element[]): BitBonboardingRect {
    let left = Infinity;
    let top = Infinity;
    let right = -Infinity;
    let bottom = -Infinity;

    for (const element of elements) {
        const rect = element.getBoundingClientRect();

        // A zero-sized element is usually display:none or not laid out yet; including it would
        // drag the union to the origin.
        if (rect.width === 0 && rect.height === 0) continue;

        left = Math.min(left, rect.left);
        top = Math.min(top, rect.top);
        right = Math.max(right, rect.right);
        bottom = Math.max(bottom, rect.bottom);
    }

    if (left === Infinity) {
        const first = elements[0].getBoundingClientRect();
        return { x: first.left, y: first.top, width: first.width, height: first.height };
    }

    return { x: left, y: top, width: right - left, height: bottom - top };
}

function sameFrame(previous: StepGeometry | null, next: StepGeometry): boolean {
    if (!previous) return false;

    return previous.found === next.found
        && previous.targetVisible === next.targetVisible
        && near(previous.target.x, next.target.x)
        && near(previous.target.y, next.target.y)
        && near(previous.target.width, next.target.width)
        && near(previous.target.height, next.target.height)
        && near(previous.viewport.width, next.viewport.width)
        && near(previous.viewport.height, next.viewport.height)
        && near(previous.popover.width, next.popover.width)
        && near(previous.popover.height, next.popover.height);
}

function near(a: number, b: number): boolean {
    return Math.abs(a - b) < EPSILON;
}

// ---------------------------------------------------------------------------
// Scrolling
// ---------------------------------------------------------------------------

function scrollIntoView(elements: Element[], mode: StepActivation['scroll'], padding: number): Promise<void> {
    if (mode === 'none' || elements.length === 0) return Promise.resolve();

    const element = elements[0];
    const rect = element.getBoundingClientRect();
    const pad = padding || 0;

    // The viewport is not all that can hide a target: a pane that scrolls can have it out of sight while its box
    // sits in the middle of the screen, which is where a tour started a second time over a pane left scrolled finds it.
    const fullyVisible = rect.top >= pad
        && rect.left >= pad
        && rect.bottom <= viewportHeight() - pad
        && rect.right <= viewportWidth() - pad
        && shownByScrollingAncestors(rect, clippingAncestors(element));

    // Already comfortably on screen: scrolling anyway is jarring and costs a frame.
    if (fullyVisible) return Promise.resolve();

    const behavior: ScrollBehavior = mode === 'smooth' ? 'smooth' : 'auto';

    try {
        // scrollIntoView walks every scrollable ancestor, which is exactly what nested layouts,
        // virtualized lists and dialogs need.
        element.scrollIntoView({ behavior, block: 'center', inline: 'center' });
    } catch {
        element.scrollIntoView();
    }

    if (behavior !== 'smooth') return Promise.resolve();

    // Give the smooth scroll a moment to settle so the first measurement is not mid-flight.
    return waitForScrollEnd(element);
}

/**
 * True when every ancestor that scrolls shows as much of a rectangle as it can: all of it along an axis it fits in,
 * and the pane's whole extent along an axis it is larger than. An ancestor that clips without scrolling is left out:
 * scrolling cannot show any more of the target there, so it is no reason to scroll the page.
 */
function shownByScrollingAncestors(rect: DOMRect, clips: Element[]): boolean {
    for (const clip of clips) {
        const scrollsX = clip.scrollWidth > clip.clientWidth;
        const scrollsY = clip.scrollHeight > clip.clientHeight;
        if (!scrollsX && !scrollsY) continue;

        // The client area: inside the borders, without the scrollbars.
        const box = clip.getBoundingClientRect();
        const left = box.left + clip.clientLeft;
        const top = box.top + clip.clientTop;

        if (scrollsX && !shownAlong(rect.left, rect.right, left, left + clip.clientWidth)) return false;
        if (scrollsY && !shownAlong(rect.top, rect.bottom, top, top + clip.clientHeight)) return false;
    }

    return true;
}

function shownAlong(start: number, end: number, paneStart: number, paneEnd: number): boolean {
    return end - start <= paneEnd - paneStart
        ? start >= paneStart - EPSILON && end <= paneEnd + EPSILON
        : start <= paneStart + EPSILON && end >= paneEnd - EPSILON;
}

function waitForScrollEnd(element: Element): Promise<void> {
    return new Promise((resolve) => {
        let last: { top: number; left: number } | null = null;
        let stable = 0;
        let frames = 0;

        const tick = () => {
            const rect = element.getBoundingClientRect();

            if (last && near(rect.top, last.top) && near(rect.left, last.left)) {
                stable += 1;
            } else {
                stable = 0;
            }

            last = { top: rect.top, left: rect.left };
            frames += 1;

            // Two identical frames means the scroll has settled; the frame cap stops a permanently
            // animating page from hanging the step.
            if (stable >= 2 || frames > 60) {
                resolve();
                return;
            }

            requestAnimationFrame(tick);
        };

        requestAnimationFrame(tick);
    });
}

// ---------------------------------------------------------------------------
// Keyboard, focus and the focus trap
// ---------------------------------------------------------------------------

const FOCUSABLE = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
    '[contenteditable="true"]'
].join(',');

function attachKeyboard(session: Session, spec: StepActivation) {
    if (session.keyListener) {
        document.removeEventListener('keydown', session.keyListener, true);
        session.keyListener = null;
    }

    const listener = (event: KeyboardEvent) => {
        if (session.disposed) return;

        // The page beyond the tour belongs to the application. While it is usable (free or
        // target-only interaction) and focus is out there, Tab, Escape and the arrow keys are left
        // for the application's own forms, menus, dialogs and widgets. The trap in particular must
        // not pull a user who is tabbing through a form on a free step into the popover.
        if (!ownsKeyboard(session, spec, event.target)) return;

        if (event.key === 'Tab') {
            if (session.trapFocus && session.popover) trapTab(session, spec, event);
            return;
        }

        if (event.key === 'Escape' && spec.closeOnEscape) {
            event.preventDefault();
            event.stopPropagation();
            invoke(session, 'OnKey', { key: 'Escape', shiftKey: event.shiftKey });
            return;
        }

        if (!spec.keyboardNavigation) return;

        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight' || event.key === 'Home' || event.key === 'End') {
            // Never steal arrow keys from a control the user is actually typing in.
            if (isTextEntry(event.target)) return;

            event.preventDefault();
            invoke(session, 'OnKey', { key: event.key, shiftKey: event.shiftKey });
        }
    };

    session.keyListener = listener;
    document.addEventListener('keydown', listener, true);
}

/**
 * True when a key press is the tour's to handle: the page is blocked, so nothing else can be
 * focused, or focus is inside the popover (or inside the target of a target-only step, which the
 * focus trap spans as well), or nothing in particular has focus.
 */
function ownsKeyboard(session: Session, spec: StepActivation, target: EventTarget | null): boolean {
    if (spec.interaction === 'blocked') return true;

    const node = target as Node | null;
    if (!node || node === document.body || node === document.documentElement || node === document) return true;

    if (!!session.popover && session.popover.contains(node)) return true;

    return spec.interaction === 'target' && session.targets.some((element) => element.contains(node));
}

function isTextEntry(target: EventTarget | null): boolean {
    const element = target as HTMLElement | null;
    if (!element || !element.tagName) return false;

    const tag = element.tagName.toLowerCase();
    if (tag === 'textarea' || tag === 'select') return true;
    if (element.isContentEditable) return true;

    if (tag === 'input') {
        const type = (element.getAttribute('type') || 'text').toLowerCase();
        return !['checkbox', 'radio', 'button', 'submit', 'reset', 'file'].includes(type);
    }

    return false;
}

function trapTab(session: Session, spec: StepActivation, event: KeyboardEvent) {
    const scopes: Element[] = session.popover ? [session.popover] : [];

    // An interactive step has to let Tab reach the element the user is being asked to use.
    if (spec.interaction === 'target') scopes.push(...session.targets);

    // The controls of each scope, scope by scope: the popover's, then each target's.
    const groups: HTMLElement[][] = [];
    const tabbables: HTMLElement[] = [];
    for (const scope of scopes) {
        if (!scope || !scope.isConnected) continue;

        const group: HTMLElement[] = [];
        const add = (element: HTMLElement) => {
            if (isVisible(element) && !tabbables.includes(element)) {
                group.push(element);
                tabbables.push(element);
            }
        };

        if (scope.matches(FOCUSABLE)) add(scope as HTMLElement);
        scope.querySelectorAll<HTMLElement>(FOCUSABLE).forEach(add);

        if (group.length > 0) groups.push(group);
    }

    if (tabbables.length === 0) {
        event.preventDefault();
        return;
    }

    const active = document.activeElement as HTMLElement | null;
    const index = active ? tabbables.indexOf(active) : -1;

    if (index < 0) {
        event.preventDefault();
        (event.shiftKey ? tabbables[tabbables.length - 1] : tabbables[0]).focus();
        return;
    }

    // Within one scope the browser's own order is the right one. Leaving a scope is not: the popover and a target are
    // in different parts of the document, so the browser would carry on into whatever follows the one being left. Focus
    // is moved to the next scope by hand instead, round from the last control to the first and back.
    const group = groups.find((candidate) => candidate.includes(active!))!;
    const leaving = event.shiftKey ? active === group[0] : active === group[group.length - 1];
    if (!leaving) return;

    event.preventDefault();
    tabbables[(index + (event.shiftKey ? tabbables.length - 1 : 1)) % tabbables.length].focus();
}

function isVisible(element: Element): boolean {
    if (!element.isConnected) return false;
    if ((element as HTMLElement).hidden) return false;

    // offsetParent is null for display:none subtrees, but also for position:fixed, so fall back to
    // the box size for fixed elements such as the popover itself.
    return (element as HTMLElement).offsetParent !== null || element.getClientRects().length > 0;
}

export function captureFocus(sessionId: string) {
    const session = sessions.get(sessionId);
    if (!session) return;

    const active = document.activeElement as HTMLElement | null;
    session.previousFocus = active && active !== document.body ? active : null;
}

export function restoreFocus(sessionId: string) {
    const session = sessions.get(sessionId);
    if (!session) return;

    const target = session.previousFocus;
    session.previousFocus = null;

    if (target && target.isConnected && typeof target.focus === 'function') {
        try {
            target.focus({ preventScroll: true });
        } catch {
            target.focus();
        }
    }
}

export function focusPopover(sessionId: string) {
    const session = sessions.get(sessionId);
    if (!session || !session.popover || !session.popover.isConnected) return;

    // An explicit autofocus wins, then the first real control, then the dialog itself so screen
    // readers announce it.
    const preferred = session.popover.querySelector<HTMLElement>(`[${AUTOFOCUS_ATTRIBUTE}]`)
        || session.popover.querySelector<HTMLElement>(FOCUSABLE)
        || session.popover;

    try {
        preferred.focus({ preventScroll: true });
    } catch {
        try { preferred.focus(); } catch (error) { warn('Focusing the popover failed.', error); }
    }
}

// ---------------------------------------------------------------------------
// Advance-on-interaction
// ---------------------------------------------------------------------------

function attachAdvanceTrigger(session: Session, spec: StepActivation) {
    detachAdvanceTrigger(session);

    const trigger = spec.advanceOn;
    if (!trigger) return;

    let host: Element | null = null;
    try {
        host = trigger.selector ? document.querySelector(trigger.selector) : session.targets[0] || null;
    } catch (error) {
        warn('Could not resolve the advance trigger.', error);
    }

    if (!host) return;

    const listener = (event: Event) => {
        // One advance at a time: events while one is pending (a double click, every keystroke of an
        // input event) belong to the advance already on its way.
        if (session.disposed || session.advanceTimer) return;

        if (trigger.matchSelector) {
            const target = event.target as Element | null;
            let match: Element | null = null;
            try {
                match = target && typeof target.closest === 'function' ? target.closest(trigger.matchSelector) : null;
            } catch (error) {
                warn('The advance trigger\'s match selector is not valid.', error);
            }
            if (!match) return;
        }

        // The listener stays armed: .NET may hold the user on the step (CanAdvance), and the next attempt
        // has to be heard. A step that does change tears the listener down with everything else it owns.
        session.advanceTimer = window.setTimeout(() => {
            session.advanceTimer = null;
            invoke(session, 'OnAdvanceTriggered', spec.activation);

            // A loss was held back while the advance was pending (see scheduleFrame). Should .NET keep the
            // user on the step, the target is measured again; should it move on, the new step cancels this.
            scheduleFrame(session);
        }, trigger.delayMs || 0);
    };

    session.advanceElement = host;
    session.advanceEventName = trigger.eventName;
    session.advanceListener = listener;
    host.addEventListener(trigger.eventName, listener, true);
}

function detachAdvanceTrigger(session: Session) {
    // The event name is remembered with the listener, so the listener can still be removed after
    // the step's spec has been replaced or cleared.
    if (session.advanceListener && session.advanceElement && session.advanceEventName) {
        session.advanceElement.removeEventListener(session.advanceEventName, session.advanceListener, true);
    }

    session.advanceListener = null;
    session.advanceElement = null;
    session.advanceEventName = null;
}

// ---------------------------------------------------------------------------
// Teardown
// ---------------------------------------------------------------------------

function clearStep(session: Session) {
    stopWaiting(session);
    disconnectObservers(session);
    detachAdvanceTrigger(session);

    if (session.advanceTimer) {
        clearTimeout(session.advanceTimer);
        session.advanceTimer = null;
    }

    if (session.keyListener) {
        document.removeEventListener('keydown', session.keyListener, true);
        session.keyListener = null;
    }

    if (session.frameHandle) {
        cancelAnimationFrame(session.frameHandle);
        session.frameHandle = 0;
    }

    session.targets = [];
    session.clips = [];
    session.spec = null;
    session.lastFrame = null;
}

function emptyGeometry(activation = 0): StepGeometry {
    return {
        found: false,
        target: { x: 0, y: 0, width: 0, height: 0 },
        viewport: { x: 0, y: 0, width: viewportWidth(), height: viewportHeight() },
        popover: { width: 0, height: 0 },
        targetVisible: false,
        sequence: ++sequence,
        activation,
        popoverRadius: null,
        invalidSelector: null
    };
}

// ---------------------------------------------------------------------------
// Beacons
//
// Every beacon is positioned here, in the browser: .NET says once which element a beacon sits on, and the
// beacon follows it with no further interop. All beacons share one set of observers and one animation frame,
// so ten beacons cost what one does, and an idle page costs nothing.
// ---------------------------------------------------------------------------

interface BeaconActivation {
    beaconId: string;
    target: TargetDescriptor;
    position: string;
    offsetX: number;
    offsetY: number;
}

interface Beacon {
    spec: BeaconActivation;
    element: HTMLElement;
    target: Element | null;
    // The ancestors whose overflow can hide the target, so a beacon on a target scrolled out of its pane hides.
    clips: Element[];
    // The translation last written, from which the element's containing block is worked out: a beacon inside a
    // transformed ancestor is positioned relative to that ancestor, not to the viewport.
    tx: number;
    ty: number;
}

// Where a beacon goes, measured before any beacon is moved. Hidden when it has no target or its point is out of sight.
interface BeaconPlacement {
    beacon: Beacon;
    visible: boolean;
    tx: number;
    ty: number;
}

const beacons = new Map<string, Beacon>();
let beaconFrame = 0;
let beaconResizeObserver: ResizeObserver | null = null;
let beaconMutationObserver: MutationObserver | null = null;

export function attachBeacon(spec: BeaconActivation, element: HTMLElement) {
    if (!spec || !element) return;

    const previous = beacons.get(spec.beaconId);
    beacons.set(spec.beaconId, { spec, element, target: null, clips: [], tx: 0, ty: 0 });
    if (previous) releaseBeaconTarget(previous.target);

    startBeaconObservers();
    scheduleBeacons();
}

export function detachBeacon(beaconId: string) {
    const beacon = beacons.get(beaconId);
    beacons.delete(beaconId);

    if (beacons.size === 0) {
        stopBeaconObservers();
    } else if (beacon) {
        releaseBeaconTarget(beacon.target);
    }
}

/**
 * Stops watching the size of an element no beacon sits on any more, so a target a re-render replaced is neither kept
 * alive nor reported on for the rest of the page's life. Several beacons can share a target.
 */
function releaseBeaconTarget(target: Element | null) {
    if (!target || !beaconResizeObserver || target === document.documentElement) return;

    for (const beacon of beacons.values()) {
        if (beacon.target === target) return;
    }

    beaconResizeObserver.unobserve(target);
}

function startBeaconObservers() {
    if (beaconResizeObserver || beaconMutationObserver) return;

    if (typeof ResizeObserver === 'function') {
        beaconResizeObserver = new ResizeObserver(scheduleBeacons);
        beaconResizeObserver.observe(document.documentElement);
    }

    // Targets that appear, disappear, are replaced by a re-render or move with nothing resized. Mutations of the
    // beacons themselves (the translation written below) are ignored, or every placement would ask for another.
    if (typeof MutationObserver === 'function') {
        beaconMutationObserver = new MutationObserver((records) => {
            for (const record of records) {
                const node = record.target as Element;
                if (!(node instanceof Element) || !node.classList.contains('bit-bonboarding-beacon')) {
                    scheduleBeacons();
                    return;
                }
            }
        });

        beaconMutationObserver.observe(document.documentElement, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: [ANCHOR_ATTRIBUTE, 'id', 'class', 'style', 'hidden', 'open']
        });
    }

    window.addEventListener('scroll', scheduleBeacons, { passive: true, capture: true });
    window.addEventListener('resize', scheduleBeacons, { passive: true });
    document.addEventListener('transitionend', scheduleBeacons, true);
    document.addEventListener('animationend', scheduleBeacons, true);
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', scheduleBeacons, { passive: true });
        window.visualViewport.addEventListener('scroll', scheduleBeacons, { passive: true });
    }
}

function stopBeaconObservers() {
    if (beaconResizeObserver) {
        beaconResizeObserver.disconnect();
        beaconResizeObserver = null;
    }

    if (beaconMutationObserver) {
        beaconMutationObserver.disconnect();
        beaconMutationObserver = null;
    }

    window.removeEventListener('scroll', scheduleBeacons, { capture: true });
    window.removeEventListener('resize', scheduleBeacons);
    document.removeEventListener('transitionend', scheduleBeacons, true);
    document.removeEventListener('animationend', scheduleBeacons, true);
    if (window.visualViewport) {
        window.visualViewport.removeEventListener('resize', scheduleBeacons);
        window.visualViewport.removeEventListener('scroll', scheduleBeacons);
    }

    if (beaconFrame) {
        cancelAnimationFrame(beaconFrame);
        beaconFrame = 0;
    }
}

function scheduleBeacons() {
    if (beaconFrame || beacons.size === 0) return;

    beaconFrame = requestAnimationFrame(() => {
        beaconFrame = 0;

        // Every beacon is measured before any is moved. Measuring one after another was moved would make the browser
        // lay the page out again first, once per beacon on every frame.
        const placements: BeaconPlacement[] = [];
        beacons.forEach((beacon) => {
            const placement = measureBeacon(beacon);
            if (placement) placements.push(placement);
        });

        for (const placement of placements) applyBeacon(placement);
    });
}

/** Works out where a beacon goes, reading the page and never writing to it. Null when the beacon has left the page. */
function measureBeacon(beacon: Beacon): BeaconPlacement | null {
    const element = beacon.element;
    if (!element.isConnected) return null;

    // Selector targets are re-queried whenever the one held is gone or hidden: a re-render replaces the node, a
    // responsive layout swaps which copy is shown.
    if (!beacon.target || !isRendered(beacon.target)) {
        const found = resolveOne(beacon.spec.target);
        if (found !== beacon.target) {
            const previous = beacon.target;
            beacon.target = found;
            beacon.clips = found ? clippingAncestors(found) : [];
            if (found && beaconResizeObserver) beaconResizeObserver.observe(found);
            releaseBeaconTarget(previous);
        }
    }

    const hidden = { beacon, visible: false, tx: beacon.tx, ty: beacon.ty };

    const target = beacon.target;
    if (!target) return hidden;

    const rect = target.getBoundingClientRect();
    const rtl = directionOf(target) === 'rtl';
    const point = anchorPoint(rect, beacon.spec.position, rtl);
    point.x += (rtl ? -1 : 1) * (beacon.spec.offsetX || 0);
    point.y += beacon.spec.offsetY || 0;

    if (!isPointVisible(point.x, point.y, beacon.clips)) return hidden;

    const own = element.getBoundingClientRect();
    const originX = own.left - beacon.tx;
    const originY = own.top - beacon.ty;

    return {
        beacon,
        visible: true,
        tx: point.x - originX - (own.width / 2),
        ty: point.y - originY - (own.height / 2)
    };
}

/** Moves and shows, or hides, a measured beacon, writing to the page and never reading its layout. */
function applyBeacon(placement: BeaconPlacement) {
    const { beacon, visible, tx, ty } = placement;
    const element = beacon.element;

    if (visible && (!near(tx, beacon.tx) || !near(ty, beacon.ty))) {
        beacon.tx = tx;
        beacon.ty = ty;
        element.style.transform = `translate3d(${round(tx)}px, ${round(ty)}px, 0)`;
    }

    setBeaconState(element, visible);
}

/** Focuses a target, or the first control inside it; does nothing when there is nothing to focus. */
export function focusTarget(descriptor: TargetDescriptor) {
    const element = resolveOne(descriptor) as HTMLElement | null;
    if (!element) return;

    const focusable = element.matches(FOCUSABLE) ? element : element.querySelector<HTMLElement>(FOCUSABLE);
    if (!focusable || typeof focusable.focus !== 'function') return;

    try {
        focusable.focus({ preventScroll: true });
    } catch {
        focusable.focus();
    }
}

function setBeaconState(element: HTMLElement, visible: boolean) {
    const state = visible ? 'visible' : 'hidden';
    if (element.getAttribute('data-bonboarding-beacon-state') !== state) element.setAttribute('data-bonboarding-beacon-state', state);
}

function anchorPoint(rect: DOMRect, position: string, rtl: boolean): { x: number; y: number } {
    const start = rtl ? rect.right : rect.left;
    const end = rtl ? rect.left : rect.right;
    const middleX = rect.left + (rect.width / 2);
    const middleY = rect.top + (rect.height / 2);

    switch (position) {
        case 'top-start': return { x: start, y: rect.top };
        case 'top': return { x: middleX, y: rect.top };
        case 'start': return { x: start, y: middleY };
        case 'center': return { x: middleX, y: middleY };
        case 'end': return { x: end, y: middleY };
        case 'bottom-start': return { x: start, y: rect.bottom };
        case 'bottom': return { x: middleX, y: rect.bottom };
        case 'bottom-end': return { x: end, y: rect.bottom };
        default: return { x: end, y: rect.top };
    }
}

function directionOf(element: Element): string {
    try {
        return getComputedStyle(element).direction;
    } catch {
        return 'ltr';
    }
}

/** The ancestors that clip an element's overflow, nearest first. */
function clippingAncestors(element: Element): Element[] {
    const result: Element[] = [];

    for (let node = element.parentElement; node && node !== document.documentElement && node !== document.body; node = node.parentElement) {
        try {
            const style = getComputedStyle(node);
            if (style.overflowX !== 'visible' || style.overflowY !== 'visible') result.push(node);
        } catch {
            // Detached mid-walk; whatever was collected still applies.
        }
    }

    return result;
}

/** True when a viewport point is inside the viewport and inside every clipping ancestor. */
function isPointVisible(x: number, y: number, clips: Element[]): boolean {
    if (x < 0 || y < 0 || x > viewportWidth() || y > viewportHeight()) return false;

    for (const clip of clips) {
        if (!clip.isConnected) continue;
        const box = clip.getBoundingClientRect();
        if (x < box.left - EPSILON || x > box.right + EPSILON || y < box.top - EPSILON || y > box.bottom + EPSILON) return false;
    }

    return true;
}

function round(value: number): number {
    return Math.round(value * 100) / 100;
}

// ---------------------------------------------------------------------------
// Storage
// ---------------------------------------------------------------------------

type StorageArea = 'local' | 'session';

function storage(area?: StorageArea): Storage | null {
    try {
        // Private browsing and blocked third-party storage both throw on access, not on use.
        const probe = area === 'session' ? window.sessionStorage : window.localStorage;
        probe.getItem('__bit_bonboarding_probe__');
        return probe;
    } catch {
        return null;
    }
}

let storageListener: ((event: StorageEvent) => void) | null = null;

/**
 * Tells .NET about every entry another tab of the site changes, so the store can drop what it has cached. The
 * storage event never fires in the tab that made the change, which is exactly what is wanted here.
 */
export function watchStorage(dotNetRef: DotNetObjectReference, area?: StorageArea) {
    if (storageListener) window.removeEventListener('storage', storageListener);

    const watched = storage(area);
    storageListener = (event: StorageEvent) => {
        if (!watched || event.storageArea !== watched) return;

        try {
            const promise = dotNetRef.invokeMethodAsync('OnStorageChanged', event.key);
            if (promise && typeof promise.catch === 'function') promise.catch((error) => warn('A storage change could not be delivered.', error));
        } catch (error) {
            warn('A storage change could not be delivered.', error);
        }
    };

    window.addEventListener('storage', storageListener);
}

export function storageGet(key: string, area?: StorageArea): string | null {
    const store = storage(area);
    if (!store) return null;

    try {
        return store.getItem(key);
    } catch (error) {
        warn('Reading storage failed.', error);
        return null;
    }
}

export function storageSet(key: string, value: string, area?: StorageArea) {
    const store = storage(area);
    if (!store) return;

    try {
        store.setItem(key, value);
    } catch (error) {
        // Quota exceeded is not worth failing a tour over.
        warn('Writing storage failed.', error);
    }
}

export function storageRemove(key: string, area?: StorageArea) {
    const store = storage(area);
    if (!store) return;

    try {
        store.removeItem(key);
    } catch (error) {
        warn('Removing from storage failed.', error);
    }
}

export function storageKeys(prefix: string, area?: StorageArea): string[] {
    const store = storage(area);
    if (!store) return [];

    const keys: string[] = [];
    try {
        for (let i = 0; i < store.length; i++) {
            const key = store.key(i);
            if (key && key.startsWith(prefix)) keys.push(key);
        }
    } catch (error) {
        warn('Enumerating storage failed.', error);
    }

    return keys;
}
