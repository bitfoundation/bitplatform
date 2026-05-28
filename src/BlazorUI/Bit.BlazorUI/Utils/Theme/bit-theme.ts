// Attribute / storage names — kept aligned with BitThemeAttributeNames.cs and BitThemeSsr.cs in
// C#. If you rename a constant here, mirror the change there (the contract test under
// Bit.BlazorUI.Tests.Utils.Theme will catch a mismatch).
namespace BitBlazorUI {
    const ATTR_THEME = 'bit-theme';
    const ATTR_THEME_DEFAULT = 'bit-theme-default';
    const ATTR_THEME_SYSTEM = 'bit-theme-system';
    const ATTR_THEME_PERSIST = 'bit-theme-persist';
    const ATTR_THEME_DARK = 'bit-theme-dark';
    const ATTR_THEME_LIGHT = 'bit-theme-light';
    const STORAGE_KEY = 'bit-current-theme';

    type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

    export interface ThemeOptions {
        system?: boolean;
        persist?: boolean;
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

            let theme = Theme._initOptions.theme || Theme._initOptions.default || Theme._lightTheme;

            if (Theme._initOptions.system) {
                theme = Theme.isSystemDark() ? Theme._darkTheme : Theme._lightTheme;
            }

            if (Theme._initOptions.persist) {
                Theme._persist = true;
                const persisted = Theme.getPersisted();
                if (persisted) {
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

            Theme.set(theme, { fromInit: true });

            if (deferPersist) {
                Theme._persist = true;
            }
        }

        public static onChange(fn: onThemeChangeType) {
            Theme._onThemeChange = fn;
        }

        public static get() {
            Theme._currentTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            if (Theme._persist) {
                var theme = Theme.getActualTheme(Theme.getPersisted());
                Theme._currentTheme = theme || Theme._currentTheme;
            }

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

            const oldTheme = document.documentElement.getAttribute(Theme.THEME_ATTRIBUTE) || '';

            document.documentElement.setAttribute(Theme.THEME_ATTRIBUTE, Theme._currentTheme);

            Theme.dispatchThemeChange(Theme._currentTheme, oldTheme);

            Theme.syncSystemThemeListener();

            return Theme._currentTheme;
        }

        public static toggleDarkLight() {
            Theme._currentTheme = Theme._currentTheme === Theme._lightTheme
                ? Theme._darkTheme
                : Theme._lightTheme;

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
            keys.forEach(key => el.style.setProperty(key, theme[key]));
            Theme._appliedVarKeys.set(el, [...new Set([...prev, ...keys])]);
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
            if (!Theme._persist) return null;

            // Mirror the write side: localStorage.getItem can throw under the same conditions as
            // setItem (Safari private mode, blocked storage, etc.). Treat failure as "no persisted
            // value" so the rest of the resolution chain (system / default / lightTheme) takes over.
            try {
                return localStorage.getItem(Theme.THEME_STORAGE_KEY);
            } catch {
                return null;
            }
        }

        public static registerDotNetNotifier(dotNetRef: DotNetObject) {
            Theme._dotnetNotifier = dotNetRef;
        }

        public static unregisterDotNetNotifier() {
            Theme._dotnetNotifier = null;
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
        public static attach(linkId: string, href: string) {
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
        theme: document.documentElement.getAttribute(ATTR_THEME),
        default: document.documentElement.getAttribute(ATTR_THEME_DEFAULT),
        darkTheme: document.documentElement.getAttribute(ATTR_THEME_DARK),
        lightTheme: document.documentElement.getAttribute(ATTR_THEME_LIGHT),
    });
}
