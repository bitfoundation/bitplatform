// Attribute / storage names — kept aligned with BitThemeAttributeNames.cs and BitThemeSsr.cs in
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
    const STORAGE_KEY = 'bit-current-theme';
    // Kept aligned with BitThemeCookie.PreferenceCookieName in C#. When cookie persistence is
    // enabled, the client mirrors the persisted preference into this cookie so the server can read
    // the user's current choice (via BitThemeSsr.BuildRootThemeAttributes) and paint the matching
    // theme on first server render — keeping the client (localStorage) and server (cookie) stores
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

        private static _dotnetNotifier: DotNetObject | null = null;

        private static _appliedVarKeys = new WeakMap<HTMLElement, string[]>();

        public static init(options: ThemeOptions) {
            Object.assign(Theme._initOptions, options);

            let deferPersist = false;

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
            if (Theme._initOptions.persistCookie) {
                Theme._persistCookie = true;
            }

            let theme = Theme._initOptions.theme || Theme._initOptions.default || Theme._lightTheme;

            // Resolve the first-paint theme with the SAME precedence as the SSR inline script in
            // BitThemeSsr.cs (BuildInlineScriptBody): a `system` opt-in — the bit-theme-system
            // attribute, the JS `system: true` option, or an explicit bit-theme="system" — follows
            // the OS and takes precedence over an explicit bit-theme / bit-theme-default at first
            // paint. A persisted preference (handled below) still wins over this.
            //
            // Previously this only resolved the OS theme when there was NO explicit theme/default,
            // so a document with both bit-theme="..." and bit-theme-system painted the explicit
            // theme on hydration while the SSR script painted the OS-resolved one — a flash of the
            // wrong theme. Keeping the two code paths in lockstep avoids that.
            if (Theme._initOptions.system || Theme._initOptions.theme === Theme.SYSTEM_THEME) {
                theme = Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            }

            if (Theme._initOptions.persist) {
                Theme._persist = true;
                const persisted = Theme.getPersisted();                if (persisted) {
                    theme = persisted;
                    // An explicit persisted preset (anything other than "system") means the user pinned a theme;
                    // stop following the OS even when <html bit-theme-system> is present.
                    Theme._stopFollowingSystem = persisted !== Theme.SYSTEM_THEME;
                } else if (Theme._initOptions.system) {
                    // System mode is enabled but no value has been persisted yet. Avoid writing the
                    // resolved light/dark theme to storage during the initial set() — otherwise the next
                    // init would treat that concrete value as an explicit user choice and stop following
                    // the OS. Disable persistence for the initial set() and re-enable it afterwards so
                    // SYSTEM_THEME remains the effective persisted indicator until the user picks one.
                    Theme._persist = false;
                    deferPersist = true;
                }
            }

            // The JS `system: true` option must on its own enable OS follow, otherwise callers that
            // configure system mode purely from JS (no <html bit-theme-system> attribute, no persisted
            // "system" value) would never get the prefers-color-scheme listener attached because
            // shouldFollowSystem() only considers the HTML attribute and persisted value. Runtime
            // follow is still cleared when the user later pins a concrete theme via set(...).
            if (Theme._initOptions.system && !Theme._stopFollowingSystem) {
                Theme._runtimeFollowSystem = true;
            }

            Theme.set(theme, { fromInit: true });

            if (deferPersist) {
                Theme._persist = true;
            }
        }

        public static onChange(fn: onThemeChangeType) {
            Theme._onThemeChange = fn;
        }

        public static get() {
            // Report the theme that is actually applied: the bit-theme attribute is the source of
            // truth (it is what is painted, and set()/OS-follow keep _currentTheme in sync with it).
            // The persisted *preference* is a separate concept exposed via getPersisted(); the two
            // can legitimately diverge — e.g. runtime OS-follow updates the attribute without
            // rewriting storage — so get() must not substitute the persisted value here.
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
                // browsers). Theme persistence is best-effort — never let it break theme switching.
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

            document.documentElement.setAttribute(Theme.THEME_ATTRIBUTE, Theme._currentTheme);

            Theme.dispatchThemeChange(Theme._currentTheme, oldTheme);

            Theme.syncSystemThemeListener();

            return Theme._currentTheme;
        }

        public static toggleDarkLight() {
            // Toggle relative to the configured dark theme: when the dark theme is active switch to
            // light, otherwise switch to dark. Anchoring on the dark theme (rather than the light
            // one) means a configured pair such as bit-theme-light="fluent-light" /
            // bit-theme-dark="fluent-dark" toggles correctly in BOTH directions, and any other
            // current value (a custom or unrecognized theme) resolves to the dark theme instead of
            // silently collapsing to light.
            Theme._currentTheme = Theme._currentTheme === Theme._darkTheme
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
            if (Theme._persist) {
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
            document.documentElement.setAttribute(Theme.THEME_ATTRIBUTE, resolved);
            Theme.dispatchThemeChange(resolved, oldTheme);
        }

        private static dispatchThemeChange(newTheme: string, oldTheme: string) {
            Theme._onThemeChange?.(newTheme, oldTheme);
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
                link.rel = 'stylesheet';
                document.head.appendChild(link);
            }
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
    });
}
