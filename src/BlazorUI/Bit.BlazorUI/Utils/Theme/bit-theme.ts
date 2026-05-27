// Attribute / storage names — kept aligned with BitThemeAttributeNames.cs and BitThemeSsr.cs in
// C#. If you rename a constant here, mirror the change there (the contract test under
// Bit.BlazorUI.Tests.Utils.Theme will catch a mismatch).
const ATTR_THEME = 'bit-theme';
const ATTR_THEME_DEFAULT = 'bit-theme-default';
const ATTR_THEME_SYSTEM = 'bit-theme-system';
const ATTR_THEME_PERSIST = 'bit-theme-persist';
const ATTR_THEME_DARK = 'bit-theme-dark';
const ATTR_THEME_LIGHT = 'bit-theme-light';
const STORAGE_KEY = 'bit-current-theme';

type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

interface BitThemeOptions {
    system?: boolean;
    persist?: boolean;
    theme?: string | null;
    default?: string | null;
    darkTheme?: string | null;
    lightTheme?: string | null;
    onChange?: onThemeChangeType;
}

interface BitThemeSetOptions {
    /** When true, startup init so resolved light/dark does not disable OS sync from bit-theme-system. */
    fromInit?: boolean;
    /** Reserved for internal OS refresh paths (same as normal set without touching follow-system flags). */
    internalOsRefresh?: boolean;
}

// Type-safe global handle. Avoids `(window as any)` plumbing at the bottom of the file and gives
// downstream TypeScript users a real surface to consume.
declare global {
    interface Window {
        BitTheme: typeof BitTheme;
        BitExternalTheme: typeof BitExternalTheme;
    }
}

class BitTheme {
    private static SYSTEM_THEME = 'system';
    private static THEME_ATTRIBUTE = ATTR_THEME;
    private static THEME_STORAGE_KEY = STORAGE_KEY;

    private static _persist = false;
    private static _darkTheme: string = 'dark';
    private static _lightTheme: string = 'light';
    private static _initOptions: BitThemeOptions = {};
    private static _currentTheme = BitTheme._lightTheme;
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
    private static _onSchemeChange = () => BitTheme.applyResolvedSystemThemeFromOs();

    private static _dotnetNotifier: DotNetObject | null = null;

    private static _appliedVarKeys = new WeakMap<HTMLElement, string[]>();

    /** Tracks whether init() has already run to make a duplicate script load (HMR, double inclusion) idempotent. */
    private static _initialized = false;

    public static init(options: BitThemeOptions) {
        // Idempotent: a duplicate script load (e.g. two host pages including the bundle, or HMR
        // restarting the IIFE) must not blow away listeners or replay setAttribute side-effects.
        // Subsequent init() calls are accepted only when called explicitly with a different
        // option set — the auto-IIFE at the bottom guards itself with this flag.
        if (BitTheme._initialized) {
            return;
        }
        BitTheme._initialized = true;

        Object.assign(BitTheme._initOptions, options);

        let deferPersist = false;

        if (BitTheme._initOptions.onChange) {
            BitTheme._onThemeChange = BitTheme._initOptions.onChange;
        }

        if (BitTheme._initOptions.darkTheme) {
            BitTheme._darkTheme = BitTheme._initOptions.darkTheme;
        }

        if (BitTheme._initOptions.lightTheme) {
            BitTheme._lightTheme = BitTheme._initOptions.lightTheme;
        }

        let theme = BitTheme._initOptions.theme || BitTheme._initOptions.default || BitTheme._lightTheme;

        if (BitTheme._initOptions.system) {
            theme = BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        }

        if (BitTheme._initOptions.persist) {
            BitTheme._persist = true;
            const persisted = BitTheme.getPersisted();
            if (persisted) {
                theme = persisted;
                // An explicit persisted preset (anything other than "system") means the user pinned a theme;
                // stop following the OS even when <html bit-theme-system> is present.
                BitTheme._stopFollowingSystem = persisted !== BitTheme.SYSTEM_THEME;
            } else if (BitTheme._initOptions.system) {
                // System mode is enabled but no value has been persisted yet. Avoid writing the
                // resolved light/dark theme to storage during the initial set() — otherwise the next
                // init would treat that concrete value as an explicit user choice and stop following
                // the OS. Disable persistence for the initial set() and re-enable it afterwards so
                // SYSTEM_THEME remains the effective persisted indicator until the user picks one.
                BitTheme._persist = false;
                deferPersist = true;
            }
        }

        BitTheme.set(theme, { fromInit: true });

        if (deferPersist) {
            BitTheme._persist = true;
        }
    }

    public static onChange(fn: onThemeChangeType) {
        BitTheme._onThemeChange = fn;
    }

    public static get() {
        BitTheme._currentTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        if (BitTheme._persist) {
            var theme = BitTheme.getActualTheme(BitTheme.getPersisted());
            BitTheme._currentTheme = theme || BitTheme._currentTheme;
        }

        return BitTheme._currentTheme;
    }

    public static set(themeName: string, options?: BitThemeSetOptions) {
        // Reject null / undefined / empty inputs up-front so we never call setAttribute(...) with
        // a value that coerces to the literal string "null" or "undefined". The non-null
        // assertion below was unsafe because getActualTheme can return null for null input.
        if (!themeName) return BitTheme._currentTheme;

        const fromInit = options?.fromInit === true;
        const internalOs = options?.internalOsRefresh === true;

        if (!fromInit && !internalOs) {
            if (themeName === BitTheme.SYSTEM_THEME) {
                BitTheme._stopFollowingSystem = false;
                BitTheme._runtimeFollowSystem = true;
            } else {
                BitTheme._stopFollowingSystem = true;
                BitTheme._runtimeFollowSystem = false;
            }
        }

        const resolved = BitTheme.getActualTheme(themeName);
        if (!resolved) return BitTheme._currentTheme;
        BitTheme._currentTheme = resolved;

        if (BitTheme._persist) {
            // localStorage can throw in Safari private mode, in iframes that block storage,
            // when over quota, or under restrictive document policies (e.g. file:// in some
            // browsers). Theme persistence is best-effort — never let it break theme switching.
            try {
                localStorage.setItem(BitTheme.THEME_STORAGE_KEY, themeName);
            } catch { /* persistence unavailable; continue without storing */ }
        }

        const oldTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        document.documentElement.setAttribute(BitTheme.THEME_ATTRIBUTE, BitTheme._currentTheme);

        BitTheme.dispatchThemeChange(BitTheme._currentTheme, oldTheme);

        BitTheme.syncSystemThemeListener();

        return BitTheme._currentTheme;
    }

    public static toggleDarkLight() {
        BitTheme._currentTheme = BitTheme._currentTheme === BitTheme._lightTheme
            ? BitTheme._darkTheme
            : BitTheme._lightTheme;

        BitTheme.set(BitTheme._currentTheme);

        return BitTheme._currentTheme;
    }

    /** Pins storage (when persist is on) to <c>system</c> and follows OS light/dark until an explicit preset is set. */
    public static useSystem() {
        return BitTheme.set(BitTheme.SYSTEM_THEME);
    }

    public static applyBitTheme(theme: Record<string, string>, element?: HTMLElement) {
        const el = element || document.body;
        const keys = Object.keys(theme);
        const prev = BitTheme._appliedVarKeys.get(el) || [];
        keys.forEach(key => el.style.setProperty(key, theme[key]));
        BitTheme._appliedVarKeys.set(el, [...new Set([...prev, ...keys])]);
    }

    /** Removes --bit-* properties previously applied by applyBitTheme on the target (default document.body). */
    public static clearAppliedBitTheme(element?: HTMLElement) {
        const el = element || document.body;
        const keys = BitTheme._appliedVarKeys.get(el);
        if (!keys || keys.length === 0) return;
        keys.forEach(k => el.style.removeProperty(k));
        BitTheme._appliedVarKeys.delete(el);
    }

    public static isSystemDark() {
        return matchMedia('(prefers-color-scheme: dark)').matches;
    }

    public static getPersisted() {
        if (!BitTheme._persist) return null;

        // Mirror the write side: localStorage.getItem can throw under the same conditions as
        // setItem (Safari private mode, blocked storage, etc.). Treat failure as "no persisted
        // value" so the rest of the resolution chain (system / default / lightTheme) takes over.
        try {
            return localStorage.getItem(BitTheme.THEME_STORAGE_KEY);
        } catch {
            return null;
        }
    }

    public static registerDotNetNotifier(dotNetRef: DotNetObject) {
        BitTheme._dotnetNotifier = dotNetRef;
    }

    public static unregisterDotNetNotifier() {
        BitTheme._dotnetNotifier = null;
    }

    private static shouldFollowSystem(): boolean {
        if (typeof document === 'undefined') return false;
        if (BitTheme._stopFollowingSystem) return false;
        // An explicitly persisted theme (anything other than SYSTEM_THEME) wins over the
        // bit-theme-system attribute, otherwise a stale attribute could override the user's choice.
        if (BitTheme._persist) {
            const persisted = BitTheme.getPersisted();
            if (persisted && persisted !== BitTheme.SYSTEM_THEME) return false;
            if (persisted === BitTheme.SYSTEM_THEME) return true;
        }
        if (BitTheme._runtimeFollowSystem) return true;
        if (document.documentElement.hasAttribute(ATTR_THEME_SYSTEM)) return true;
        return false;
    }

    private static syncSystemThemeListener() {
        BitTheme.detachSystemThemeListener();
        if (!BitTheme.shouldFollowSystem()) return;
        BitTheme.attachSystemThemeListener();
    }

    private static attachSystemThemeListener() {
        if (!window.matchMedia) return;
        BitTheme._schemeMediaQuery = matchMedia('(prefers-color-scheme: dark)');
        const mq = BitTheme._schemeMediaQuery as MediaQueryList & { addListener?: (cb: () => void) => void };
        if (typeof mq.addEventListener === 'function') {
            mq.addEventListener('change', BitTheme._onSchemeChange);
        } else {
            mq.addListener?.(BitTheme._onSchemeChange);
        }
    }

    private static detachSystemThemeListener() {
        if (!BitTheme._schemeMediaQuery) return;
        const mq = BitTheme._schemeMediaQuery as MediaQueryList & { removeListener?: (cb: () => void) => void };
        if (typeof mq.removeEventListener === 'function') {
            mq.removeEventListener('change', BitTheme._onSchemeChange);
        } else {
            mq.removeListener?.(BitTheme._onSchemeChange);
        }
        BitTheme._schemeMediaQuery = null;
    }

    private static applyResolvedSystemThemeFromOs() {
        if (!BitTheme.shouldFollowSystem()) return;

        const resolved = BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        const oldTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        if (resolved === oldTheme) return;

        BitTheme._currentTheme = resolved;
        document.documentElement.setAttribute(BitTheme.THEME_ATTRIBUTE, resolved);
        BitTheme.dispatchThemeChange(resolved, oldTheme);
    }

    private static dispatchThemeChange(newTheme: string, oldTheme: string) {
        BitTheme._onThemeChange?.(newTheme, oldTheme);
        const n = BitTheme._dotnetNotifier;
        if (n) {
            // Swallow rejections so a disposed circuit / receiver does not surface as an
            // unhandled promise rejection. Theme dispatch is fire-and-forget by design.
            n.invokeMethodAsync('NotifyThemeChangedFromJs', newTheme, oldTheme)
                .catch(() => { /* receiver gone or invocation failed; nothing actionable here */ });
        }
    }

    private static getActualTheme(theme: string | null) {
        if (theme === BitTheme.SYSTEM_THEME) {
            return BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        }

        return theme;
    }
}

/** Attach or swap alternate theme stylesheets at runtime (prefer same-origin / trusted URLs). */
class BitExternalTheme {
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

(function () {
    // If a previous load already published BitTheme on window, leave it alone. This makes the
    // bundle safe to include twice (HMR, multiple host pages) — the first invocation wins and the
    // second is a no-op rather than overwriting state and re-attaching event listeners.
    if ((window as any).BitTheme) return;

    const options: BitThemeOptions = {
        system: document.documentElement.hasAttribute(ATTR_THEME_SYSTEM),
        persist: document.documentElement.hasAttribute(ATTR_THEME_PERSIST),
        theme: document.documentElement.getAttribute(ATTR_THEME),
        default: document.documentElement.getAttribute(ATTR_THEME_DEFAULT),
        darkTheme: document.documentElement.getAttribute(ATTR_THEME_DARK),
        lightTheme: document.documentElement.getAttribute(ATTR_THEME_LIGHT),
    };

    BitTheme.init(options);

    window.BitTheme = BitTheme;
    window.BitExternalTheme = BitExternalTheme;
}());
