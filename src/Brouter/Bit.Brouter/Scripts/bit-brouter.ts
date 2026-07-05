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
// view-transition-name CSS). If C# never completes (a crash path), the browser's own transition
// timeout aborts it - the page does not hang.

let activeViewTransitionResolve: (() => void) | null = null;

// Returns true when a transition was actually started (API present); false lets the C# side skip
// the completion round-trip entirely on unsupported browsers.
export function beginViewTransition(): boolean {
    const doc = document as any;
    if (typeof doc.startViewTransition !== 'function') return false;

    // A still-open previous transition (its navigation was superseded mid-flight) must be released
    // first: the browser skips/settles it and lets the new one start cleanly.
    if (activeViewTransitionResolve) {
        activeViewTransitionResolve();
        activeViewTransitionResolve = null;
    }

    try {
        doc.startViewTransition(() => new Promise<void>(resolve => {
            activeViewTransitionResolve = resolve;
        }));
        return true;
    } catch {
        // Defensive: a host with a broken/partial implementation must not break navigation.
        activeViewTransitionResolve = null;
        return false;
    }
}

// Resolves the pending transition's update promise; the browser then animates old -> new.
// Idempotent: completing with no pending transition is a no-op.
export function completeViewTransition() {
    if (activeViewTransitionResolve) {
        activeViewTransitionResolve();
        activeViewTransitionResolve = null;
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
