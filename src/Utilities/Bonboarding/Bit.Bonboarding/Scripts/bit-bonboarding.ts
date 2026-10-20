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

    const elements = resolveAll(spec);

    // Nothing found and the caller is willing to wait: watch the DOM instead of polling it. The
    // keyboard is wired up straight away so Escape can still leave a tour stuck on a missing target.
    if (elements.length === 0 && wantsTarget(spec) && spec.waitForTarget) {
        attachKeyboard(session, spec);
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

    // Re-resolve first: selector targets are routinely replaced by a Blazor re-render.
    if (session.spec) {
        const elements = resolveAll(session.spec);
        if (elements.length > 0) {
            const changed = elements.length !== session.targets.length
                || elements.some((el, i) => el !== session.targets[i]);

            if (changed) {
                session.targets = elements;
                observe(session);
                // The trigger listened on the old node; a pending advance has already fired.
                if (!session.advanceTimer) attachAdvanceTrigger(session, session.spec);
            }
        }
    }

    const geometry = measureSession(session);
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

function resolveOne(descriptor: TargetDescriptor | null | undefined): Element | null {
    if (!descriptor) return null;

    try {
        if (descriptor.kind === 'Element') {
            // Blazor revives an ElementReference into the real element for us.
            const element = descriptor.element;
            return element instanceof Element && element.isConnected ? element : null;
        }

        if (descriptor.kind === 'Selector' && descriptor.selector) {
            return document.querySelector(descriptor.selector);
        }
    } catch (error) {
        warn('Could not resolve a target.', error);
    }

    return null;
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

    session.mutationObserver = new MutationObserver(() => {
        // Mutation callbacks fire in bursts; coalescing onto a frame keeps querySelector cheap.
        if (session.frameHandle) return;
        session.frameHandle = requestAnimationFrame(() => {
            session.frameHandle = 0;
            check();
        });
    });

    session.mutationObserver.observe(document.documentElement, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: [ANCHOR_ATTRIBUTE, 'id', 'class', 'hidden', 'style']
    });

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
}

function scheduleFrame(session: Session) {
    if (session.disposed || session.frameHandle) return;

    session.frameHandle = requestAnimationFrame(() => {
        session.frameHandle = 0;
        if (session.disposed) return;

        // A target can be removed by a re-render at any moment.
        if (session.targets.length > 0 && session.targets.every((el) => !el.isConnected)) {
            const recovered = session.spec ? resolveAll(session.spec) : [];

            if (recovered.length > 0) {
                // Same element, new DOM node: rebind rather than reporting it as lost.
                session.targets = recovered;
                observe(session);
                // The trigger listened on the old node; a pending advance has already fired.
                if (session.spec && !session.advanceTimer) attachAdvanceTrigger(session, session.spec);
            } else {
                session.targets = [];
                invoke(session, 'OnTargetLost', activationOf(session));
                return;
            }
        }

        const geometry = measureSession(session);
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
    const viewport = { x: 0, y: 0, width: viewportWidth(), height: viewportHeight() };
    const connectedPopover = session.popover && session.popover.isConnected ? session.popover : null;
    const popover = connectedPopover
        ? { width: connectedPopover.offsetWidth, height: connectedPopover.offsetHeight }
        : { width: 0, height: 0 };
    const popoverRadius = connectedPopover ? cornerRadius(connectedPopover) : null;
    const activation = activationOf(session);

    const connected = session.targets.filter((element) => element.isConnected);

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

    const target = unionRect(connected);
    const visible = target.width > 0
        && target.height > 0
        && target.x < viewport.width
        && target.y < viewport.height
        && target.x + target.width > 0
        && target.y + target.height > 0;

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

    const fullyVisible = rect.top >= pad
        && rect.left >= pad
        && rect.bottom <= viewportHeight() - pad
        && rect.right <= viewportWidth() - pad;

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

    const tabbables: HTMLElement[] = [];
    for (const scope of scopes) {
        if (!scope || !scope.isConnected) continue;

        if (scope.matches(FOCUSABLE) && isVisible(scope)) tabbables.push(scope as HTMLElement);
        scope.querySelectorAll<HTMLElement>(FOCUSABLE).forEach((element) => {
            if (isVisible(element)) tabbables.push(element);
        });
    }

    if (tabbables.length === 0) {
        event.preventDefault();
        return;
    }

    const first = tabbables[0];
    const last = tabbables[tabbables.length - 1];
    const active = document.activeElement as HTMLElement | null;
    const inside = !!active && tabbables.includes(active);

    if (!inside) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus();
        return;
    }

    if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
    } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
    }
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
        if (session.disposed) return;

        if (trigger.matchSelector) {
            const target = event.target as Element | null;
            const match = target && typeof target.closest === 'function'
                ? target.closest(trigger.matchSelector)
                : null;
            if (!match) return;
        }

        // Fire once: the step is about to change anyway.
        detachAdvanceTrigger(session);

        session.advanceTimer = window.setTimeout(() => {
            session.advanceTimer = null;
            invoke(session, 'OnAdvanceTriggered', spec.activation);
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
        popoverRadius: null
    };
}

// ---------------------------------------------------------------------------
// Storage
// ---------------------------------------------------------------------------

function storage(): Storage | null {
    try {
        // Private browsing and blocked third-party storage both throw on access, not on use.
        const probe = window.localStorage;
        probe.getItem('__bit_bonboarding_probe__');
        return probe;
    } catch {
        return null;
    }
}

export function storageGet(key: string): string | null {
    const store = storage();
    if (!store) return null;

    try {
        return store.getItem(key);
    } catch (error) {
        warn('Reading storage failed.', error);
        return null;
    }
}

export function storageSet(key: string, value: string) {
    const store = storage();
    if (!store) return;

    try {
        store.setItem(key, value);
    } catch (error) {
        // Quota exceeded is not worth failing a tour over.
        warn('Writing storage failed.', error);
    }
}

export function storageRemove(key: string) {
    const store = storage();
    if (!store) return;

    try {
        store.removeItem(key);
    } catch (error) {
        warn('Removing from storage failed.', error);
    }
}

export function storageKeys(prefix: string): string[] {
    const store = storage();
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
