// Wires a capture-phase click listener on the given anchor that calls preventDefault
// ONLY for unmodified primary clicks. Modified clicks (Ctrl/Cmd/Shift/Alt) and non-
// primary buttons keep their native browser behavior (e.g., "open in new tab").
//
// Blazor's render-time `onclick:preventDefault` attribute can't be toggled per click,
// so it would otherwise suppress the default action even on modified clicks. With
// this listener installed, Blazor's own onclick handler still fires (and the C# side
// applies the same modifier checks before performing the replace navigation), but
// the browser default is left alone for modified clicks.
export function wireConditionalPreventDefault(element: HTMLElement | null) {
    if (!element) return null;

    const handler = (e: MouseEvent) => {
        if (e.defaultPrevented) return;
        if (e.button !== 0) return;
        if (e.ctrlKey || e.shiftKey || e.altKey || e.metaKey) return;
        e.preventDefault();
    };

    // Capture phase so we run before Blazor's bubble-phase onclick handler.
    element.addEventListener('click', handler, { capture: true });

    return {
        dispose: () => element.removeEventListener('click', handler, { capture: true })
    };
}

// ---------------------------------------------------------------------------------------------
// Link preloading (BrouterLink.Preload). Wires the DOM triggers for the two JS-driven modes and
// calls back into the BrouterLink instance, which resolves the target route and runs its loaders
// into the cache. 'intent' fires on pointer hover / touchstart / keyboard focus after a small
// debounce (leaving cancels a pending fire); 'viewport' fires once when the link first becomes
// visible. Repeated fires are cheap: the C# side short-circuits on a still-fresh cache entry.

export function wirePreload(element: HTMLElement | null, mode: string, delayMs: number, dotnetRef: any) {
    if (!element || !dotnetRef) return null;

    const trigger = () => { try { dotnetRef.invokeMethodAsync('OnPreloadTriggered'); } catch { /* disposed */ } };

    if (mode === 'intent') {
        let timer: number | null = null;
        const arm = () => {
            if (timer !== null) return;
            timer = window.setTimeout(() => { timer = null; trigger(); }, delayMs);
        };
        const disarm = () => {
            if (timer !== null) { window.clearTimeout(timer); timer = null; }
        };
        element.addEventListener('pointerenter', arm);
        element.addEventListener('pointerleave', disarm);
        element.addEventListener('touchstart', arm, { passive: true });
        element.addEventListener('focus', arm);
        element.addEventListener('blur', disarm);
        return {
            dispose: () => {
                disarm();
                element.removeEventListener('pointerenter', arm);
                element.removeEventListener('pointerleave', disarm);
                element.removeEventListener('touchstart', arm);
                element.removeEventListener('focus', arm);
                element.removeEventListener('blur', disarm);
            }
        };
    }

    if (mode === 'viewport') {
        if (typeof IntersectionObserver !== 'function') return null;
        const observer = new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    observer.disconnect();
                    trigger();
                    break;
                }
            }
        });
        observer.observe(element);
        return { dispose: () => observer.disconnect() };
    }

    return null;
}

// ---------------------------------------------------------------------------------------------
// View Transitions API integration (BrouterOptions.ViewTransitions).
//
// Blazor renders asynchronously, so the classic synchronous startViewTransition(update) shape
// doesn't fit: the DOM mutation happens whenever the render batch lands, not inside a callback we
// control. The handshake is therefore split: beginViewTransition() is called by the C# pipeline
// right BEFORE it triggers the new route's render - startViewTransition snapshots the old page and
// receives an update promise we hold open - and completeViewTransition() is called from
// OnAfterRenderAsync once the new DOM is committed, resolving that promise so the browser
// snapshots the new state and runs the crossfade (customizable per-element with the standard
// view-transition-name CSS).
//
// TWO TIMING SUBTLETIES (each was a real bug):
//
// 1. CAPTURE ORDERING. startViewTransition captures the OLD page state at the next rendering
//    opportunity (~one frame later) - NOT synchronously. If beginViewTransition responded to C#
//    as soon as startViewTransition returned, a fast circuit would apply the new route's render
//    batch BEFORE that capture, so the "old" snapshot would picture the NEW page: old == new,
//    shared-element morphs have nothing to morph between, and every navigation degenerates to an
//    identical fade. The fix: beginViewTransition returns a Promise that resolves from INSIDE the
//    update callback - the spec guarantees the callback is invoked only after the old state is
//    captured - and JSInterop awaits JS promises, so C# doesn't start rendering until the capture
//    is truly done. (The spec also guarantees the callback is always invoked, even for skipped
//    transitions; the begin watchdog below is belt-and-braces for broken implementations.)
//
// 2. COMPLETION RACING THE CALLBACK. A completion that arrives before the update callback has
//    parked its resolver would silently no-op, orphaning the promise - the transition then holds
//    the page frozen (pointer blocked, rendering suppressed) until the browser's ~4s timeout
//    aborts it with "Transition was aborted because of timeout in DOM update". With the promise-
//    based begin above, C# can't normally complete before the callback runs, but the
//    completedEarly flag keeps the degraded paths (begin watchdog fired, duplicate completes)
//    correct, and the update watchdog caps how long an open transition can ever hold the page
//    (a lost completion - circuit drop, error path - degrades to a skipped animation, not a freeze).

let activeViewTransitionResolve: (() => void) | null = null;
// Set when completeViewTransition() arrives before the browser has invoked the current
// transition's update callback (see timing note 2). Consumed by that callback.
let viewTransitionCompletedEarly = false;
// Upper bound on how long the update promise may stay open. The C# side completes right after the
// route's render lands (loaders run BEFORE the transition begins), so legitimate completions
// arrive within a few frames; well under a second even on a slow connection.
const viewTransitionWatchdogMs = 1500;
// Upper bound on waiting for the old-state capture (the update callback's invocation). Normally
// one frame; if it never comes (pathological implementation), navigation must not hang.
const viewTransitionBeginWatchdogMs = 500;

// Brouter's out-of-the-box navigation animations (BrouterOptions.ViewTransitionDefaultAnimations).
// Direction-aware: a push glides the new page in, pop (Back/Forward) mirrors the motion, replace
// does a quick in-place fade; shared-element morphs get a springy glide. prefers-reduced-motion
// (which Windows/macOS accessibility settings propagate into the browser - "Animation effects"
// off on Windows means EVERY user of that machine reports reduce) swaps the slides for gentle
// opacity-only crossfades and disables the morph motion, so navigation still gives visual feedback
// instead of going dead. Injected once, inside the CSS layer "bit-brouter",
// so any UNLAYERED ::view-transition-* rule in application CSS overrides these automatically -
// zero specificity fights. The current direction is exposed as data-brouter-nav on <html>.
const viewTransitionDefaultCss = `
::view-transition-old(root) { animation: 170ms cubic-bezier(.4,0,1,1) both bit-brouter-vt-out-fwd; }
::view-transition-new(root) { animation: 300ms cubic-bezier(.22,1,.36,1) 30ms both bit-brouter-vt-in-fwd; }
html[data-brouter-nav="pop"]::view-transition-old(root) { animation-name: bit-brouter-vt-out-back; }
html[data-brouter-nav="pop"]::view-transition-new(root) { animation-name: bit-brouter-vt-in-back; }
html[data-brouter-nav="replace"]::view-transition-old(root) { animation: 120ms ease-out both bit-brouter-vt-fade-out; }
html[data-brouter-nav="replace"]::view-transition-new(root) { animation: 170ms ease-in both bit-brouter-vt-fade-in; }
::view-transition-group(*) { animation-duration: 320ms; animation-timing-function: cubic-bezier(.22,1,.36,1); }
@keyframes bit-brouter-vt-out-fwd { to { opacity: 0; transform: translateX(-28px); } }
@keyframes bit-brouter-vt-in-fwd { from { opacity: 0; transform: translateX(34px); } }
@keyframes bit-brouter-vt-out-back { to { opacity: 0; transform: translateX(28px); } }
@keyframes bit-brouter-vt-in-back { from { opacity: 0; transform: translateX(-34px); } }
@keyframes bit-brouter-vt-fade-out { to { opacity: 0; } }
@keyframes bit-brouter-vt-fade-in { from { opacity: 0; } }`;

// Included only when BrouterOptions.ViewTransitionRespectReducedMotion is true (the default).
// Bypassing it is legitimate when the OS reports reduce for non-accessibility reasons (Windows
// "Animation effects" off on VMs / remote desktops / perf-tuned machines) - the C# option holds
// the rationale; here we just honor the flag.
const viewTransitionReducedMotionCss = `
@media (prefers-reduced-motion: reduce) {
    ::view-transition-old(root),
    html[data-brouter-nav]::view-transition-old(root) { animation: 120ms ease-out both bit-brouter-vt-fade-out; }
    ::view-transition-new(root),
    html[data-brouter-nav]::view-transition-new(root) { animation: 160ms ease-in both bit-brouter-vt-fade-in; }
    ::view-transition-group(*) { animation-duration: 1ms; animation-delay: 0ms; }
}`;

let viewTransitionDefaultsInjected = false;

function ensureViewTransitionDefaults(useDefaults: boolean, respectReducedMotion: boolean) {
    if (!useDefaults || viewTransitionDefaultsInjected) return;
    viewTransitionDefaultsInjected = true;
    const style = document.createElement('style');
    style.id = 'bit-brouter-view-transitions';
    style.textContent = '@layer bit-brouter {' + viewTransitionDefaultCss
        + (respectReducedMotion ? viewTransitionReducedMotionCss : '') + '\n}';
    document.head.appendChild(style);
}

// History-traversal detection for the animation direction. The C# side classifies a navigation
// from what the framework reports, but interactive Blazor flags Back/Forward history traversals
// as "intercepted", which reads as a push. The browser knows the truth: popstate fires exactly on
// history traversals (never on pushState link navigations), and it fires BEFORE the navigation
// pipeline's beginViewTransition interop arrives - so a module-scope listener can correct the
// direction reliably for both the browser buttons and programmatic history.go/back/forward.
let historyTraversalPending = false;
if (typeof window !== 'undefined') {
    window.addEventListener('popstate', () => { historyTraversalPending = true; });
}

// Resolves true once a transition is started AND its old-state capture is complete (the C# side
// awaits this before rendering the new route - see timing note 1); false lets the C# side skip
// the completion round-trip entirely on unsupported browsers.
//   navKind              - 'push' | 'replace' | 'pop'; drives the direction-aware default
//                          animations and is exposed as data-brouter-nav on the root element.
//   useDefaults          - whether to inject Brouter's default animation stylesheet (once).
//   respectReducedMotion - whether the injected defaults include the prefers-reduced-motion
//                          fallback (BrouterOptions.ViewTransitionRespectReducedMotion).
export function beginViewTransition(navKind?: string, useDefaults?: boolean, respectReducedMotion?: boolean): Promise<boolean> {
    const doc = document as any;
    if (typeof doc.startViewTransition !== 'function') return Promise.resolve(false);

    ensureViewTransitionDefaults(useDefaults !== false, respectReducedMotion !== false);
    // Stamp the direction BEFORE the transition starts so the pseudo-element animations resolve
    // against it. It persists (harmlessly) until the next navigation overwrites it. A pending
    // popstate overrides the reported kind: the framework cannot distinguish Back/Forward from a
    // push in interactive mode (see historyTraversalPending above), but the browser can.
    const kind = historyTraversalPending ? 'pop' : (navKind || 'push');
    historyTraversalPending = false;
    document.documentElement.setAttribute('data-brouter-nav', kind);

    // A still-open previous transition (its navigation was superseded mid-flight) must be released
    // first: the browser skips/settles it and lets the new one start cleanly.
    if (activeViewTransitionResolve) {
        activeViewTransitionResolve();
        activeViewTransitionResolve = null;
    }
    viewTransitionCompletedEarly = false;

    return new Promise<boolean>(beginResolve => {
        let beginResolved = false;
        const resolveBegin = (started: boolean) => {
            if (beginResolved) return;
            beginResolved = true;
            beginResolve(started);
        };

        try {
            const transition = doc.startViewTransition(() => {
                // Invoked by the browser once the old state is captured: NOW C# may render the new
                // route (the render batch lands while rendering is paused, exactly as the API
                // intends), then complete to trigger the new-state capture and the animation.
                resolveBegin(true);

                // The completion may already have arrived (only via the degraded begin-watchdog
                // path) - resolve immediately instead of parking a resolver nothing would call.
                if (viewTransitionCompletedEarly) {
                    viewTransitionCompletedEarly = false;
                    return Promise.resolve();
                }
                return new Promise<void>(resolve => {
                    activeViewTransitionResolve = resolve;
                    // Watchdog: never let an open transition hold the page hostage. setTimeout
                    // still fires while rendering is suppressed, so this releases even a fully
                    // frozen page.
                    window.setTimeout(() => {
                        if (activeViewTransitionResolve === resolve) activeViewTransitionResolve = null;
                        resolve(); // idempotent if the normal completion already ran
                    }, viewTransitionWatchdogMs);
                });
            });
            // We deliberately discard the transition, so its promises reject unobserved when a
            // rapid follow-up navigation skips it (AbortError: "Transition was skipped") - attach
            // no-op handlers to keep that expected outcome out of the console.
            transition?.finished?.catch?.(() => { /* skipped/aborted transitions are expected */ });
            transition?.updateCallbackDone?.catch?.(() => { /* ditto */ });
            transition?.ready?.catch?.(() => { /* ditto */ });

            // Belt-and-braces: the spec guarantees the update callback always runs, but a broken
            // host must not leave the navigation pipeline awaiting forever. Reporting true keeps
            // the completion handshake alive so the transition (if any) is still released.
            window.setTimeout(() => resolveBegin(true), viewTransitionBeginWatchdogMs);
        } catch {
            // Defensive: a host with a broken/partial implementation must not break navigation.
            activeViewTransitionResolve = null;
            resolveBegin(false);
        }
    });
}

// Resolves the pending transition's update promise; the browser then animates old -> new.
// Idempotent: completing with no pending transition is a no-op - EXCEPT that a completion racing
// ahead of the update callback must be remembered (completedEarly), or the transition freezes.
export function completeViewTransition() {
    if (activeViewTransitionResolve) {
        activeViewTransitionResolve();
        activeViewTransitionResolve = null;
    } else {
        viewTransitionCompletedEarly = true;
    }
}

// ---------------------------------------------------------------------------------------------
// External-navigation confirmation (BrouterOptions.ConfirmExternalNavigation /
// IBrouter.SetConfirmExternalNavigationAsync). While armed, leaving the SPA entirely - closing the
// tab, a full reload, or following a link to another origin/document - triggers the browser's
// generic "unsaved changes" dialog. Browsers only honor beforeunload after a user interaction with
// the page (sticky activation), and the dialog text is not customizable; both are platform rules.
// In-SPA navigations are unaffected (use leave guards / OnNavigating for those).

let confirmExternalArmed = false;

const beforeUnloadHandler = (e: BeforeUnloadEvent) => {
    e.preventDefault();
    // Chrome (and pre-standard browsers) require returnValue to be set for the dialog to appear.
    e.returnValue = '';
};

// Arms/disarms the beforeunload confirmation. Idempotent in both directions so C# callers can
// toggle freely (e.g. a dirty-form tracker flipping it on and off).
export function setConfirmExternalNavigation(enabled: boolean) {
    if (enabled && !confirmExternalArmed) {
        window.addEventListener('beforeunload', beforeUnloadHandler);
        confirmExternalArmed = true;
    } else if (!enabled && confirmExternalArmed) {
        window.removeEventListener('beforeunload', beforeUnloadHandler);
        confirmExternalArmed = false;
    }
}

// ---------------------------------------------------------------------------------------------
// Scroll restoration state (only used when BrouterOptions.RestoreScrollPosition is enabled).
//
// scrollPositions : absolute-URL -> { x, y } scroll offset the user was at when they left that URL.
//                   Kept in memory for the page's lifetime; does not survive a full reload.
// pendingIsPop    : whether the navigation currently being committed is a Back/Forward (history pop).
//                   Captured from the popstate flag at navigation start (see saveScrollPosition) so it
//                   is read before any render, then consumed by applyNavigationEffects post-render.
// popped          : set by the popstate listener the instant the browser fires a Back/Forward, before
//                   Blazor's async LocationChanged pipeline runs. Drained into pendingIsPop at save time.
type ScrollPosition = { x: number, y: number };
type ScrollStorageKind = 'session' | 'local' | null;

const scrollPositions = new Map<string, ScrollPosition>();
let pendingIsPop = false;
let popped = false;
let scrollRestorationInited = false;
// null -> in-memory only; 'session'/'local' -> mirrored to sessionStorage/localStorage so positions
// survive a full reload. Fixed for the module's lifetime (BrouterOptions are per-scope constants).
let scrollStorageKind: ScrollStorageKind = null;

// The single web-storage slot the whole position map is JSON-serialized into. One slot (rather than
// one per URL) keeps hydrate/persist trivial and easy to clear.
const SCROLL_STORAGE_KEY = 'bit-brouter:scrollPositions';

// Upper bound on how many URLs' scroll positions we retain. Without a cap, every distinct URL visited
// in a long-lived session adds an entry forever, growing memory and eventually overflowing Web Storage
// (which throws QuotaExceededError on persist). A few dozen is plenty for realistic Back/Forward depth;
// the oldest entries are evicted first (see saveScrollPosition).
const MAX_SCROLL_POSITIONS = 50;

// Resolves the configured Web Storage object, or null when persistence is off or the store is
// unavailable (private mode, disabled by policy). Accessing window.sessionStorage/localStorage can
// itself throw, so it's guarded.
function scrollStore(): Storage | null {
    try {
        if (scrollStorageKind === 'session') return window.sessionStorage;
        if (scrollStorageKind === 'local') return window.localStorage;
    } catch { /* storage access denied -> behave as in-memory */ }
    return null;
}

// Loads any previously-persisted positions into the in-memory map. Best-effort: corrupt or
// unreadable storage simply leaves the map as-is so restoration degrades to in-memory.
function hydrateScrollPositions() {
    const store = scrollStore();
    if (!store) return;
    try {
        const raw = store.getItem(SCROLL_STORAGE_KEY);
        if (!raw) return;
        const obj = JSON.parse(raw);
        if (!obj || typeof obj !== 'object') return;
        for (const k of Object.keys(obj)) {
            const v = obj[k];
            if (v && typeof v.x === 'number' && typeof v.y === 'number') {
                scrollPositions.set(k, { x: v.x, y: v.y });
            }
        }
    } catch { /* corrupt/unavailable -> keep whatever is already in memory */ }
}

// Write-through of the in-memory map to the configured store. Best-effort: a quota error or an
// unavailable store is swallowed so navigation (and in-memory restoration) keep working.
function persistScrollPositions() {
    const store = scrollStore();
    if (!store) return;
    try {
        const obj: Record<string, ScrollPosition> = {};
        for (const [k, v] of scrollPositions) obj[k] = v;
        store.setItem(SCROLL_STORAGE_KEY, JSON.stringify(obj));
    } catch { /* quota exceeded / storage unavailable -> in-memory still holds the positions */ }
}

// Idempotently arms scroll restoration: records the storage mode, takes over the browser's automatic
// restoration (so it can't fight ours), starts tracking Back/Forward, and hydrates any persisted
// positions. Called lazily the first time a restoration-enabled navigation touches the module, so a
// consumer that never opts in pays nothing and the browser's native restoration is left exactly as it
// was. `storageKind` is honored on the first call only (options are constant per scope).
//
//   storageKind - 'session' | 'local' to persist positions in the matching Web Storage, else in-memory.
function ensureScrollRestoration(storageKind: string | null) {
    if (scrollRestorationInited) return;
    scrollRestorationInited = true;
    scrollStorageKind = (storageKind === 'session' || storageKind === 'local') ? storageKind : null;

    if ('scrollRestoration' in history) {
        try { history.scrollRestoration = 'manual'; } catch { /* some hosts forbid setting it */ }
    }
    // Fires synchronously on a Back/Forward, ahead of Blazor's async LocationChanged handling, so the
    // flag is already set when the ensuing saveScrollPosition call reads it.
    window.addEventListener('popstate', () => { popped = true; });

    // Seed the in-memory map from persisted storage so a reload can still restore positions.
    hydrateScrollPositions();
}

function currentScroll(): ScrollPosition {
    return {
        x: window.scrollX ?? window.pageXOffset ?? 0,
        y: window.scrollY ?? window.pageYOffset ?? 0
    };
}

// Records the scroll offset of the page being navigated away from, keyed by its absolute URL, so a
// later Back/Forward to that URL can restore it. Invoked by the C# commit pipeline BEFORE the new
// route renders, so `currentScroll()` still reflects the outgoing page. Also drains the popstate flag
// into pendingIsPop here (pre-render) because applyNavigationEffects, which needs the direction, only
// runs post-render by which point a fresh popstate could have arrived.
//
//   key         - the absolute URL of the page being left, or null/empty to skip recording (e.g. initial load).
//   storageKind - persistence mode, honored on the first call (see ensureScrollRestoration).
export function saveScrollPosition(key: string | null, storageKind: string | null) {
    ensureScrollRestoration(storageKind);
    pendingIsPop = popped;
    popped = false;
    if (key) {
        // Bound the cache. Map preserves insertion order, so deleting the first key evicts the oldest
        // entry. Delete-then-set also re-inserts an updated key at the newest position, so recently
        // visited URLs survive eviction (oldest-first / LRU-ish) rather than being dropped by age of
        // first visit.
        scrollPositions.delete(key);
        while (scrollPositions.size >= MAX_SCROLL_POSITIONS) {
            const oldest = scrollPositions.keys().next().value;
            if (oldest === undefined) break;
            scrollPositions.delete(oldest);
        }
        scrollPositions.set(key, currentScroll());
        persistScrollPositions();
    }
}

// Applies the post-navigation DOM effects that Blazor's declarative rendering can't express:
// scrolling a URL fragment into view, restoring a remembered scroll position on Back/Forward,
// moving focus for assistive technologies, and scroll-to-top. Called once per successful navigation,
// after the matched route has been committed to the DOM. Every step is best-effort: a missing target
// is silently ignored so navigation never breaks.
//
//   hash          - the URL fragment including its leading '#', or null/empty when the caller
//                   disabled fragment scrolling or the destination has no fragment.
//   focusSelector - a CSS selector for the element to focus (accessibility), or null to skip.
//   scrollToTop   - whether to scroll the window to the top when no fragment/restore claimed the scroll.
//   restoreKey    - the destination's absolute URL when scroll restoration is enabled, else null. On a
//                   Back/Forward to a URL with a remembered position, that position is restored instead
//                   of applying scrollToTop.
//   storageKind   - persistence mode ('session'/'local'/null), honored on first arm (see saveScrollPosition).
export function applyNavigationEffects(hash: string | null, focusSelector: string | null, scrollToTop: boolean, restoreKey: string | null, storageKind: string | null) {
    // Consume the direction captured at navigation start. Only meaningful when restoration is on.
    // ensureScrollRestoration here guarantees the position map is hydrated before the first restore,
    // even on the initial load where no saveScrollPosition call precedes this one.
    let isPop = false;
    if (restoreKey) {
        ensureScrollRestoration(storageKind);
        isPop = pendingIsPop;
        pendingIsPop = false;
    }

    // 1. Fragment scrolling: navigating to /docs#install should land on the #install element,
    //    and (for keyboard/AT users) continue focus from there rather than the top of the page.
    if (hash && hash.length > 1) {
        let id = hash.substring(1);
        try { id = decodeURIComponent(id); } catch { /* keep the raw, still-encoded fragment */ }

        const target = document.getElementById(id)
            || document.querySelector(`a[name="${cssEscape(id)}"]`);

        if (target) {
            target.scrollIntoView();
            // Fragment focus wins over focusSelector: the user asked to jump to this element.
            focusElement(target);
            return;
        }
        // Fragment target not found -> fall through to the restore / scroll-to-top / focus defaults.
    }

    // 2. Scroll restoration on Back/Forward: return the user to where they left this page. Wins over
    //    scroll-to-top (that's the "new navigation" behavior). Only acts on a history pop with a
    //    remembered position; a first visit or a forward push falls through to the defaults below.
    if (restoreKey && isPop && scrollPositions.has(restoreKey)) {
        const p = scrollPositions.get(restoreKey)!;
        window.scrollTo(p.x, p.y);
        // Still honor focus so assistive tech announces the page; focusElement preventScroll keeps
        // the restored position intact.
        if (focusSelector) {
            const el = document.querySelector<HTMLElement>(focusSelector);
            if (el) focusElement(el);
        }
        return;
    }

    // 3. Scroll to top (only when no fragment/restore claimed the scroll position above).
    if (scrollToTop) {
        window.scrollTo(0, 0);
    }

    // 4. Focus management: move focus to the configured landmark/heading so screen readers
    //    announce the new page instead of leaving focus on the activated link.
    if (focusSelector) {
        const el = document.querySelector<HTMLElement>(focusSelector);
        if (el) focusElement(el);
    }
}

// Focuses an element, making it programmatically focusable first if it isn't already. Uses
// preventScroll so focusing doesn't fight a scroll position already set by the caller (fragment
// scrollIntoView above, or window.scrollTo(0,0)).
function focusElement(el: HTMLElement) {
    // Non-interactive elements (h1, main, div, ...) have tabIndex -1 and no explicit tabindex;
    // they can't receive programmatic focus until one is added. Use -1 so they're script-focusable
    // but stay out of the sequential Tab order, matching Blazor's FocusOnNavigate behavior.
    if (el.tabIndex < 0 && !el.hasAttribute('tabindex')) {
        el.setAttribute('tabindex', '-1');
    }
    try { el.focus({ preventScroll: true }); } catch { /* element detached mid-navigation */ }
}

// CSS.escape isn't available in every host (older WebViews); fall back to a minimal escape so the
// a[name="..."] fragment fallback can't throw on ids containing quotes/backslashes.
function cssEscape(value: string): string {
    if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
    return value.replace(/["\\]/g, '\\$&');
}
