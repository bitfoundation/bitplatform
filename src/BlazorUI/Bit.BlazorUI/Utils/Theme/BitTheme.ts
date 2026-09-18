// Attribute / storage names - kept aligned with BitThemeAttributeNames.cs and BitThemeSsr.cs in
// C#. If you rename a constant here, mirror the change there (the contract test under
// Bit.BlazorUI.Tests.Utils.Theme will catch a mismatch).
namespace BitBlazorUI {
    const ATTR_THEME = 'bit-theme';
    const ATTR_THEME_DEFAULT = 'bit-theme-default';
    const ATTR_THEME_SYSTEM = 'bit-theme-system';
    const ATTR_THEME_PERSIST = 'bit-theme-persist';
    const ATTR_THEME_PERSIST_COOKIE = 'bit-theme-persist-cookie';
    const ATTR_THEME_DARK = 'bit-theme-dark';
    const ATTR_THEME_LIGHT = 'bit-theme-light';
    // Opt-in marker: animate theme swaps with the View Transitions API (see swapThemeAttribute).
    const ATTR_THEME_VIEW_TRANSITION = 'bit-theme-view-transition';
    // Opt-in marker: keep <meta name="theme-color"> equal to a palette color of the live page (see
    // ThemeColorMeta). Its value names the custom property to read; empty means the default below.
    const ATTR_THEME_COLOR_META = 'bit-theme-color-meta';
    // The page's own background - what a status bar sits above in nearly every app.
    const THEME_COLOR_VARIABLE = '--bit-clr-bg-pri';
    const STORAGE_KEY = 'bit-current-theme';
    // Kept aligned with BitThemeCookie.PreferenceCookieName in C#. When cookie persistence is
    // enabled, the client mirrors the persisted preference into this cookie so the server can read
    // the user's current choice (via BitThemeSsr.BuildRootThemeAttributes) and paint the matching
    // theme on first server render - keeping the client (localStorage) and server (cookie) stores
    // in sync instead of drifting apart.
    const COOKIE_NAME = 'bit-theme-preference';
    // ~400 days, the upper bound modern browsers clamp persistent cookies to.
    const COOKIE_MAX_AGE_SECONDS = 34560000;

    type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

    export interface ThemeOptions {
        system?: boolean;
        persist?: boolean;
        /** Mirror the persisted preference into the COOKIE_NAME cookie so SSR stays in sync with client choices. */
        persistCookie?: boolean;
        theme?: string | null;
        default?: string | null;
        darkTheme?: string | null;
        lightTheme?: string | null;
        onChange?: onThemeChangeType;
        /**
         * Keep `<meta name="theme-color">` equal to a palette color of the live page: `true` for the
         * default custom property, a `--bit-*` name to read another one, `false` to turn it off
         * again. Same feature as the bit-theme-color-meta attribute (see ThemeColorMeta).
         */
        themeColorMeta?: boolean | string | null;
    }

    interface ThemeSetOptions {
        /** When true, startup init so resolved light/dark does not disable OS sync from bit-theme-system. */
        fromInit?: boolean;
        /** Reserved for internal OS refresh paths (same as normal set without touching follow-system flags). */
        internalOsRefresh?: boolean;
    }

    export class Theme {
        private static SYSTEM_THEME = 'system';
        private static THEME_ATTRIBUTE = ATTR_THEME;
        private static THEME_STORAGE_KEY = STORAGE_KEY;

        /** DOM event dispatched on `document` after every theme change, so non-CSS consumers can react. */
        public static readonly THEME_CHANGE_EVENT = 'bit-theme-change';

        private static _persist = false;
        private static _persistCookie = false;
        private static _darkTheme: string = 'dark';
        private static _lightTheme: string = 'light';
        private static _initOptions: ThemeOptions = {};
        private static _currentTheme = Theme._lightTheme;
        private static _onThemeChange: onThemeChangeType = () => { };

        /** When true, user pinned an explicit theme via set (not system); disables following OS until set('system'). */
        private static _stopFollowingSystem = false;

        /**
         * When true, the user explicitly opted into following the OS at runtime (e.g. via useSystem()),
         * so we follow OS changes even without a persisted "system" preference or the bit-theme-system attribute.
         * Cleared automatically when the user pins a concrete theme via set(...).
         */
        private static _runtimeFollowSystem = false;

        private static _schemeMediaQuery: MediaQueryList | null = null;
        private static _onSchemeChange = () => Theme.applyResolvedSystemThemeFromOs();

        private static _storageListenerAttached = false;
        // Mirror a persisted-theme change made in ANOTHER tab into this one, so all tabs of the same
        // app stay in sync. The `storage` event fires only in tabs OTHER than the one that wrote, so
        // re-applying here can never loop back with our own set(). Gated on _persist because only the
        // localStorage-backed store raises this event (cookie-only persistence does not).
        private static _onStorage = (e: StorageEvent) => {
            if (!Theme._persist) return;
            // e.key is null when storage was cleared; otherwise it must be our key.
            if (e.key !== null && e.key !== Theme.THEME_STORAGE_KEY) return;
            const next = e.newValue;
            if (!next) return;
            // Skip when the other tab's concrete choice is already applied here: avoids a redundant
            // set() (re-persist + re-dispatch) rippling across 3+ tabs. "system" is always processed
            // because the OS scheme may differ from the currently painted attribute.
            if (next !== Theme.SYSTEM_THEME &&
                Theme.getActualTheme(next) === (document.documentElement.getAttribute(ATTR_THEME) || '')) {
                return;
            }
            // Mirror the other tab's intent (including a switch to "system"); set() resolves and
            // re-cascades. storage events never fire in the tab that wrote, so this cannot loop.
            Theme.set(next);
        };

        private static _dotnetNotifier: DotNetObject | null = null;

        private static _appliedVarKeys = new WeakMap<HTMLElement, string[]>();

        public static init(options: ThemeOptions) {
            Object.assign(Theme._initOptions, options);

            let deferPersist = false;
            let deferPersistCookie = false;

            if (Theme._initOptions.onChange) {
                Theme._onThemeChange = Theme._initOptions.onChange;
            }

            if (Theme._initOptions.darkTheme) {
                Theme._darkTheme = Theme._initOptions.darkTheme;
            }

            if (Theme._initOptions.lightTheme) {
                Theme._lightTheme = Theme._initOptions.lightTheme;
            }

            // Cookie mirroring is independent of localStorage persistence: an SSR app may want the
            // server to read the preference cookie without necessarily enabling localStorage.
            // Derive from the merged _initOptions on every call (not just when truthy) so a later
            // init({ persistCookie: false }) can clear the flag, while an init that omits the key
            // keeps the previously merged value.
            Theme._persistCookie = !!Theme._initOptions.persistCookie;

            let theme = Theme._initOptions.theme || Theme._initOptions.default || Theme._lightTheme;

            // A `system` opt-in comes from the JS `system: true` option / bit-theme-system attribute,
            // OR from the base theme itself resolving to the "system" keyword (bit-theme="system" or
            // bit-theme-default="system"). Capture it once so first-paint resolution, the persist
            // deferral below and runtime OS-follow all treat these spellings identically.
            const systemRequested = Theme._initOptions.system || theme === Theme.SYSTEM_THEME;

            // Resolve the first-paint theme with the SAME precedence as the SSR inline script in
            // BitThemeSsr.cs (BuildInlineScriptBody): a `system` opt-in follows the OS and takes
            // precedence over an explicit bit-theme / bit-theme-default at first paint. A persisted
            // preference (handled below) still wins over this.
            //
            // Previously this only resolved the OS theme when there was NO explicit theme/default,
            // so a document with both bit-theme="..." and bit-theme-system painted the explicit
            // theme on hydration while the SSR script painted the OS-resolved one - a flash of the
            // wrong theme. Keeping the two code paths in lockstep avoids that.
            if (systemRequested) {
                theme = Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            }

            // Derive on every call (see persistCookie note above) so a later init({ persist: false })
            // clears the flag, while an omitted key preserves the merged value.
            Theme._persist = !!Theme._initOptions.persist;

            // Restore a previously persisted preference. getPersisted() reads localStorage first and
            // falls back to the preference cookie, so this also covers a cookie-only setup
            // (persistCookie without persist) that a pure client-side render (no SSR inline script)
            // would otherwise ignore - previously this read was gated on persist alone.
            if (Theme._persist || Theme._persistCookie) {
                const persisted = Theme.getPersisted();
                if (persisted) {
                    theme = persisted;
                    // An explicit persisted preset (anything other than "system") means the user pinned a theme;
                    // stop following the OS even when <html bit-theme-system> is present.
                    Theme._stopFollowingSystem = persisted !== Theme.SYSTEM_THEME;
                } else if (systemRequested) {
                    // System mode is enabled but no value has been persisted yet. Avoid writing the
                    // resolved light/dark theme to storage during the initial set() - otherwise the next
                    // init would treat that concrete value as an explicit user choice and stop following
                    // the OS. Disable BOTH stores for the initial set() and re-enable them afterwards so
                    // SYSTEM_THEME remains the effective persisted indicator until the user picks one.
                    deferPersist = Theme._persist;
                    deferPersistCookie = Theme._persistCookie;
                    Theme._persist = false;
                    Theme._persistCookie = false;
                }
            }

            // Any `system` opt-in (system: true, bit-theme="system" or bit-theme-default="system")
            // must on its own enable OS follow, otherwise setups without the bit-theme-system
            // attribute and without a persisted "system" value would resolve the OS theme once at
            // init but never get the prefers-color-scheme listener attached, because
            // shouldFollowSystem() only considers the HTML attribute and persisted value. Runtime
            // follow is still cleared when the user later pins a concrete theme via set(...).
            if (systemRequested && !Theme._stopFollowingSystem) {
                Theme._runtimeFollowSystem = true;
            }

            Theme.set(theme, { fromInit: true });

            if (deferPersist) {
                Theme._persist = true;
            }

            if (deferPersistCookie) {
                Theme._persistCookie = true;
            }

            // After the first set(), so the tag is read from the theme this init resolved rather than
            // the one being replaced. An omitted key leaves the feature as it is (the self-init below
            // may already have turned it on from the attribute); false / null turns it back off.
            const themeColorMeta = Theme._initOptions.themeColorMeta;
            if (themeColorMeta === false || themeColorMeta === null) {
                ThemeColorMeta.disable();
            } else if (themeColorMeta) {
                ThemeColorMeta.enable(typeof themeColorMeta === 'string' ? themeColorMeta : null);
            }

            Theme.attachStorageSyncListener();
        }

        private static attachStorageSyncListener() {
            if (Theme._storageListenerAttached) return;
            if (typeof window === 'undefined' || !window.addEventListener) return;
            Theme._storageListenerAttached = true;
            window.addEventListener('storage', Theme._onStorage);
        }

        public static onChange(fn: onThemeChangeType) {
            Theme._onThemeChange = fn;
        }

        public static get() {
            // Report the theme that is actually applied: the bit-theme attribute is the source of
            // truth (it is what is painted, and set()/OS-follow keep _currentTheme in sync with it).
            // The persisted *preference* is a separate concept exposed via getPersisted(); the two
            // can legitimately diverge - e.g. runtime OS-follow updates the attribute without
            // rewriting storage - so get() must not substitute the persisted value here.
            Theme._currentTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            return Theme._currentTheme;
        }

        public static set(themeName: string, options?: ThemeSetOptions) {
            // Reject null / undefined / empty inputs up-front so we never call setAttribute(...) with
            // a value that coerces to the literal string "null" or "undefined". The non-null
            // assertion below was unsafe because getActualTheme can return null for null input.
            if (!themeName) return Theme._currentTheme;

            const fromInit = options?.fromInit === true;
            const internalOs = options?.internalOsRefresh === true;

            if (!fromInit && !internalOs) {
                if (themeName === Theme.SYSTEM_THEME) {
                    Theme._stopFollowingSystem = false;
                    Theme._runtimeFollowSystem = true;
                } else {
                    Theme._stopFollowingSystem = true;
                    Theme._runtimeFollowSystem = false;
                }
            }

            const resolved = Theme.getActualTheme(themeName);
            if (!resolved) return Theme._currentTheme;
            Theme._currentTheme = resolved;

            if (Theme._persist) {
                // localStorage can throw in Safari private mode, in iframes that block storage,
                // when over quota, or under restrictive document policies (e.g. file:// in some
                // browsers). Theme persistence is best-effort - never let it break theme switching.
                try {
                    localStorage.setItem(Theme.THEME_STORAGE_KEY, themeName);
                } catch { /* persistence unavailable; continue without storing */ }
            }

            // Mirror the preference into the cookie so the server (BitThemeSsr.BuildRootThemeAttributes)
            // can paint the matching theme on first render. Stored verbatim (the abstract key, e.g.
            // "system" / "dark" / "fluent-light"), matching what localStorage holds.
            if (Theme._persistCookie) {
                Theme.writePreferenceCookie(themeName);
            }

            const oldTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            // Never animate the startup application (there is nothing meaningful to transition at
            // boot, and a transition would add a frame capture to every page load).
            Theme.swapThemeAttribute(Theme._currentTheme, /* skipTransition: */ fromInit);

            Theme.dispatchThemeChange(Theme._currentTheme, oldTheme);

            Theme.syncSystemThemeListener();

            return Theme._currentTheme;
        }

        public static toggleDarkLight() {
            // Re-sync from the bit-theme attribute first (the source of truth, same as get()) so a
            // toggle issued after an external attribute write - a non-bit script or dev tooling
            // setting bit-theme directly - acts on what is actually painted instead of a stale
            // _currentTheme. Read the attribute directly (not via get(), which would overwrite
            // _currentTheme before the fallback could use it) and fall back to _currentTheme when
            // the attribute is absent - which also bridges the one-frame window in which a pending
            // view transition has not applied the new attribute yet, since set() updates
            // _currentTheme synchronously.
            const current = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || Theme._currentTheme;

            // Toggle relative to the configured dark theme: when the dark theme is active switch to
            // light, otherwise switch to dark. Anchoring on the dark theme (rather than the light
            // one) means a configured pair such as bit-theme-light="fluent-light" /
            // bit-theme-dark="fluent-dark" toggles correctly in BOTH directions, and any other
            // current value (a custom or unrecognized theme) resolves to the dark theme instead of
            // silently collapsing to light.
            Theme._currentTheme = current === Theme._darkTheme
                ? Theme._lightTheme
                : Theme._darkTheme;

            Theme.set(Theme._currentTheme);

            return Theme._currentTheme;
        }

        /** Pins storage (when persist is on) to <c>system</c> and follows OS light/dark until an explicit preset is set. */
        public static useSystem() {
            return Theme.set(Theme.SYSTEM_THEME);
        }

        public static applyTheme(theme: Record<string, string>, element?: HTMLElement) {
            const el = element || document.body;
            const keys = Object.keys(theme);
            const prev = Theme._appliedVarKeys.get(el) || [];
            prev.filter(key => !keys.includes(key)).forEach(key => el.style.removeProperty(key));
            keys.forEach(key => el.style.setProperty(key, theme[key]));
            Theme._appliedVarKeys.set(el, keys);
        }

        /** Removes --bit-* properties previously applied by applyTheme on the target (default document.body). */
        public static clearAppliedTheme(element?: HTMLElement) {
            const el = element || document.body;
            const keys = Theme._appliedVarKeys.get(el);
            if (!keys || keys.length === 0) return;
            keys.forEach(k => el.style.removeProperty(k));
            Theme._appliedVarKeys.delete(el);
        }

        public static isSystemDark() {
            return matchMedia('(prefers-color-scheme: dark)').matches;
        }

        public static getPersisted() {
            if (Theme._persist) {
                // Mirror the write side: localStorage.getItem can throw under the same conditions as
                // setItem (Safari private mode, blocked storage, etc.). Treat failure as "no persisted
                // value" so the rest of the resolution chain (system / default / lightTheme) takes over.
                try {
                    const stored = localStorage.getItem(Theme.THEME_STORAGE_KEY);
                    if (stored) return stored;
                } catch { /* fall through to cookie / null */ }
            }

            // Cookie-only persistence (or localStorage unavailable): read the same preference the
            // server uses so the client and server agree on the stored choice.
            if (Theme._persistCookie) {
                return Theme.readPreferenceCookie();
            }

            return null;
        }

        public static registerDotNetNotifier(dotNetRef: DotNetObject) {
            Theme._dotnetNotifier = dotNetRef;
        }

        public static unregisterDotNetNotifier() {
            Theme._dotnetNotifier = null;
        }

        private static writePreferenceCookie(value: string) {
            try {
                if (typeof document === 'undefined' || !value) return;
                const secure = location.protocol === 'https:' ? '; Secure' : '';
                document.cookie =
                    `${COOKIE_NAME}=${encodeURIComponent(value)}; path=/; max-age=${COOKIE_MAX_AGE_SECONDS}; SameSite=Lax${secure}`;
            } catch { /* cookies unavailable / blocked; best-effort like localStorage */ }
        }

        private static readPreferenceCookie(): string | null {
            try {
                if (typeof document === 'undefined' || !document.cookie) return null;
                const prefix = `${COOKIE_NAME}=`;
                const match = document.cookie
                    .split(';')
                    .map(c => c.trim())
                    .find(c => c.startsWith(prefix));
                return match ? decodeURIComponent(match.substring(prefix.length)) : null;
            } catch {
                return null;
            }
        }

        private static shouldFollowSystem(): boolean {
            if (typeof document === 'undefined') return false;
            if (Theme._stopFollowingSystem) return false;
            // An explicitly persisted theme (anything other than SYSTEM_THEME) wins over the
            // bit-theme-system attribute, otherwise a stale attribute could override the user's choice.
            // Consider the persisted preference whenever any persistence source is active - including
            // the cookie-only path (persistCookie without persist) that getPersisted() falls back to -
            // so a persisted SYSTEM_THEME still attaches the OS-follow listener in a cookie-backed setup.
            if (Theme._persist || Theme._persistCookie) {
                const persisted = Theme.getPersisted();
                if (persisted && persisted !== Theme.SYSTEM_THEME) return false;
                if (persisted === Theme.SYSTEM_THEME) return true;
            }
            if (Theme._runtimeFollowSystem) return true;
            if (document.documentElement.hasAttribute(ATTR_THEME_SYSTEM)) return true;
            return false;
        }

        private static syncSystemThemeListener() {
            Theme.detachSystemThemeListener();
            if (!Theme.shouldFollowSystem()) return;
            Theme.attachSystemThemeListener();
        }

        private static attachSystemThemeListener() {
            if (!window.matchMedia) return;
            Theme._schemeMediaQuery = matchMedia('(prefers-color-scheme: dark)');
            const mq = Theme._schemeMediaQuery as MediaQueryList & { addListener?: (cb: () => void) => void };
            if (typeof mq.addEventListener === 'function') {
                mq.addEventListener('change', Theme._onSchemeChange);
            } else {
                mq.addListener?.(Theme._onSchemeChange);
            }
        }

        private static detachSystemThemeListener() {
            if (!Theme._schemeMediaQuery) return;
            const mq = Theme._schemeMediaQuery as MediaQueryList & { removeListener?: (cb: () => void) => void };
            if (typeof mq.removeEventListener === 'function') {
                mq.removeEventListener('change', Theme._onSchemeChange);
            } else {
                mq.removeListener?.(Theme._onSchemeChange);
            }
            Theme._schemeMediaQuery = null;
        }

        private static applyResolvedSystemThemeFromOs() {
            if (!Theme.shouldFollowSystem()) return;

            const resolved = Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            const oldTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            if (resolved === oldTheme) return;

            Theme._currentTheme = resolved;
            Theme.swapThemeAttribute(resolved);
            Theme.dispatchThemeChange(resolved, oldTheme);
        }

        /**
         * Writes the bit-theme attribute, optionally inside a View Transition so the whole page
         * cross-fades to the new palette instead of hard-swapping. Opt-in via the
         * bit-theme-view-transition attribute on the document element (checked live, so it can be
         * toggled at runtime). The transition is skipped - falling back to a plain synchronous
         * attribute write - when the API is unavailable or the user prefers reduced motion.
         *
         * Note: startViewTransition invokes its callback asynchronously (after capturing the old
         * frame), so with the transition active the attribute updates a frame later than the
         * Theme.set call. _currentTheme is always updated synchronously by the callers, and the
         * change notifications carry the explicit old/new names, so observers are unaffected.
         */
        private static swapThemeAttribute(themeName: string, skipTransition?: boolean) {
            const apply = () => document.documentElement.setAttribute(Theme.THEME_ATTRIBUTE, themeName);

            // A no-op swap (re-setting the already-applied theme, e.g. pinning the currently
            // resolved preset) must not run a transition: startViewTransition captures and
            // cross-fades even when the callback changes nothing visible.
            if (skipTransition || document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) === themeName) {
                apply();
                return;
            }

            try {
                const doc = document as Document & { startViewTransition?: (callback: () => void) => unknown };
                if (document.documentElement.hasAttribute(ATTR_THEME_VIEW_TRANSITION) &&
                    typeof doc.startViewTransition === 'function' &&
                    !(typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches)) {
                    doc.startViewTransition(apply);
                    return;
                }
            } catch { /* View Transition unavailable or failed to start; fall through to the direct swap */ }

            apply();
        }

        private static dispatchThemeChange(newTheme: string, oldTheme: string) {
            // Isolate the application callback: a throwing _onThemeChange must not abort the
            // _dotnetNotifier notification below (nor the Theme.set flow into syncSystemThemeListener()).
            try {
                Theme._onThemeChange?.(newTheme, oldTheme);
            } catch { /* application callback failed; not actionable here */ }

            // Broadcast a DOM event so non-CSS consumers (charts, canvas, iframes, third-party
            // widgets) can react without owning the single _onThemeChange callback. Best-effort:
            // a missing CustomEvent / document (non-browser host) must not break theme switching.
            try {
                if (typeof document !== 'undefined' && typeof CustomEvent === 'function') {
                    document.dispatchEvent(new CustomEvent(Theme.THEME_CHANGE_EVENT, {
                        detail: { newTheme, oldTheme },
                    }));
                }
            } catch { /* event dispatch unavailable; not actionable here */ }
            const n = Theme._dotnetNotifier;
            if (n) {
                // Swallow rejections so a disposed circuit / receiver does not surface as an
                // unhandled promise rejection. Theme dispatch is fire-and-forget by design.
                n.invokeMethodAsync('NotifyThemeChangedFromJs', newTheme, oldTheme)
                    .catch(() => { /* receiver gone or invocation failed; nothing actionable here */ });
            }
        }

        private static getActualTheme(theme: string | null) {
            if (theme === Theme.SYSTEM_THEME) {
                return Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            }

            return theme;
        }
    }

    /**
     * Keeps every `<meta name="theme-color">` tag equal to a palette color of the live page, so the
     * browser chrome an app cannot reach from CSS - an installed PWA's status bar, the address bar
     * on mobile - stays with the theme. Opted into with the bit-theme-color-meta attribute on
     * `<html>` (or `themeColorMeta` on init); off by default, since an app that hardcodes its tag
     * must keep the tag it wrote.
     *
     * The color is READ BACK from the page rather than mapped from the theme name, because a name
     * does not carry a color: the packaged design systems (Fluent 2, Material, Cupertino) each paint
     * their own surfaces, an app's stylesheet may re-declare the tokens, and an accent (see
     * BitAccentColor in Bit.BlazorUI.Extras) re-derives the whole palette - surfaces included - into
     * an applyTheme overlay on `<body>`. It is read off `<body>` for that last reason, and on a
     * schedule of its own rather than from the theme-change notification: with
     * bit-theme-view-transition the bit-theme attribute is only written a frame AFTER the change is
     * announced, so a listener would still see the outgoing palette. What it watches instead is the
     * DOM the color actually comes from.
     */
    export class ThemeColorMeta {
        private static _variable: string | null = null;
        private static _observer: MutationObserver | null = null;
        private static _pending = false;
        private static _loadListenerAttached = false;

        /**
         * Starts (or retargets) the sync. `variable` names the custom property to read and defaults
         * to THEME_COLOR_VARIABLE; a name without the leading `--` is accepted.
         */
        public static enable(variable?: string | null) {
            const name = (variable || '').trim() || THEME_COLOR_VARIABLE;
            ThemeColorMeta._variable = name.indexOf('--') === 0 ? name : `--${name}`;

            ThemeColorMeta.whenBodyReady(() => {
                ThemeColorMeta.observe();
                ThemeColorMeta.sync();
            });

            // A stylesheet still in flight leaves the custom property empty, and a pending stylesheet
            // changes no node, so nothing the observer watches would fire once it lands. One catch-up
            // after load covers the app that links its stylesheets with a script already running.
            if (!ThemeColorMeta._loadListenerAttached && typeof window !== 'undefined' && window.addEventListener) {
                ThemeColorMeta._loadListenerAttached = true;
                window.addEventListener('load', () => ThemeColorMeta.schedule(), { once: true } as any);
            }
        }

        /** Stops the sync and leaves the tags on whatever color they currently carry. */
        public static disable() {
            ThemeColorMeta._variable = null;
            ThemeColorMeta._observer?.disconnect();
            ThemeColorMeta._observer = null;
        }

        /**
         * Re-reads the color and rewrites the tags now. Public so an app can force a refresh after a
         * change none of the watched nodes shows - e.g. a stylesheet swapped through ExternalTheme.
         */
        public static sync() {
            ThemeColorMeta._pending = false;

            const variable = ThemeColorMeta._variable;
            if (!variable) return;

            // The overlay applyTheme writes lands on <body> (custom properties inherit, so a :root
            // declaration is read here just the same); before the body exists, nothing is painted yet.
            const source = document.body;
            if (!source) return;

            let color = '';
            try {
                color = getComputedStyle(source).getPropertyValue(variable).trim();
            } catch { return; /* computed styles unavailable (detached / hidden document) */ }

            // Empty means the stylesheet declaring it has not arrived (or the name is a typo): keep
            // whatever the document already carries rather than blanking a tag the app hand-wrote.
            if (!color) return;

            const tags = document.querySelectorAll('meta[name=theme-color]');

            // Nothing to keep in sync yet - an app that opted in without writing a tag gets one.
            if (tags.length === 0) {
                const meta = document.createElement('meta');
                meta.setAttribute('name', 'theme-color');
                meta.setAttribute('content', color);
                document.head.appendChild(meta);
                return;
            }

            // Every tag, so a document that splits them by media (a light one and a dark one, the
            // usual first-paint trick) has the pinned theme win over the OS on both. Writing only
            // what changed keeps this off the browser's chrome-repaint path on unrelated mutations.
            tags.forEach(tag => {
                if (tag.getAttribute('content') !== color) {
                    tag.setAttribute('content', color);
                }
            });
        }

        private static schedule = () => {
            if (ThemeColorMeta._pending || !ThemeColorMeta._variable) return;
            ThemeColorMeta._pending = true;

            // One read per frame at most: a theme swap moves an attribute, an accent moves the body
            // overlay and a <head> style within the same tick, and all of them want the same read.
            const run = () => ThemeColorMeta.sync();
            if (typeof requestAnimationFrame === 'function') {
                requestAnimationFrame(run);
            } else {
                setTimeout(run, 16);
            }
        };

        private static observe() {
            if (ThemeColorMeta._observer || typeof MutationObserver !== 'function') return;

            const observer = new MutationObserver(ThemeColorMeta.schedule);
            // <html> unfiltered: bit-theme and the accent's bit-accent are the two that matter here,
            // but an app's own attribute may select a palette too, and a coalesced read is cheap
            // enough that guessing the list would cost more than it saves.
            observer.observe(document.documentElement, { attributes: true });
            // The applyTheme overlay (an accent, or an app applying a theme object) as inline
            // custom properties, and a class that switches palettes.
            observer.observe(document.body, { attributes: true, attributeFilter: ['style', 'class'] });
            // The accent's first-paint <style> snapshot being injected or replaced. Also catches an
            // app's own late stylesheet. Appending the tag above is a childList change too, but the
            // read it schedules then finds the color unchanged and stops there.
            observer.observe(document.head, { childList: true });

            ThemeColorMeta._observer = observer;
        }

        private static whenBodyReady(action: () => void) {
            if (document.body) {
                action();
                return;
            }

            // The library script can be loaded from <head>; everything here needs the body element.
            document.addEventListener('DOMContentLoaded', action, { once: true } as any);
        }
    }

    /** Attach or swap alternate theme stylesheets at runtime (prefer same-origin / trusted URLs). */
    export class ExternalTheme {
        private static validateHref(href: string) {
            const trimmed = href?.trimStart();
            if (!trimmed) {
                throw new Error('Stylesheet href is required.');
            }
            if (trimmed.startsWith('//') || trimmed.startsWith('\\\\') ||
                trimmed.startsWith('/\\') || trimmed.startsWith('\\/')) {
                throw new Error('Stylesheet href must not be a protocol-relative URL.');
            }
            if (/^javascript:/i.test(trimmed) || /^data:/i.test(trimmed) || /^vbscript:/i.test(trimmed)) {
                throw new Error('Stylesheet href must not use a non-http scheme.');
            }
            let url: URL;
            try {
                url = new URL(href, document.baseURI);
            } catch {
                throw new Error('Stylesheet href is not a valid URL.');
            }
            if (url.protocol !== 'http:' && url.protocol !== 'https:') {
                throw new Error(`Stylesheet href scheme '${url.protocol}' is not allowed.`);
            }
            if (url.origin !== location.origin) {
                throw new Error('Stylesheet href must be same-origin.');
            }
        }

        public static attach(linkId: string, href: string) {
            ExternalTheme.validateHref(href);

            const existing = document.getElementById(linkId);
            let link: HTMLLinkElement;
            if (existing && existing.tagName === 'LINK') {
                link = existing as HTMLLinkElement;
            } else {
                // No element, or an element with the same id but a different tag (e.g. a stale
                // <style> or <meta>): replace/insert a fresh <link> rather than blindly mutating
                // an unrelated node which would break attach/detach invariants.
                existing?.remove();
                link = document.createElement('link');
                link.id = linkId;
                document.head.appendChild(link);
            }
            // Set rel on both the reuse and creation paths so a reused <link> that was created with a
            // different rel is corrected before we point it at the stylesheet href.
            link.rel = 'stylesheet';
            link.href = href;
        }

        public static detach(linkId: string) {
            const el = document.getElementById(linkId);
            // Only remove the element if it's actually a <link>; we should not garbage-collect
            // unrelated nodes that happen to share the id.
            if (el && el.tagName === 'LINK') {
                el.remove();
            }
        }
    }

    // Self-init from <html> attributes.
    Theme.init({
        system: document.documentElement.hasAttribute(ATTR_THEME_SYSTEM),
        persist: document.documentElement.hasAttribute(ATTR_THEME_PERSIST),
        persistCookie: document.documentElement.hasAttribute(ATTR_THEME_PERSIST_COOKIE),
        theme: document.documentElement.getAttribute(ATTR_THEME),
        default: document.documentElement.getAttribute(ATTR_THEME_DEFAULT),
        darkTheme: document.documentElement.getAttribute(ATTR_THEME_DARK),
        lightTheme: document.documentElement.getAttribute(ATTR_THEME_LIGHT),
        // Absent stays undefined rather than false: an app that turns the sync on through init()
        // instead of the attribute must not have it turned back off by this call.
        themeColorMeta: document.documentElement.hasAttribute(ATTR_THEME_COLOR_META)
            ? (document.documentElement.getAttribute(ATTR_THEME_COLOR_META) || true)
            : undefined,
    });
}
