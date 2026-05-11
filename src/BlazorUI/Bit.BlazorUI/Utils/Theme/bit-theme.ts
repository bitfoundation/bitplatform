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

class BitTheme {
    private static SYSTEM_THEME = 'system';
    private static THEME_ATTRIBUTE = 'bit-theme';
    private static THEME_STORAGE_KEY = 'bit-current-theme';

    private static _persist = false;
    private static _darkTheme: string = 'dark';
    private static _lightTheme: string = 'light';
    private static _initOptions: BitThemeOptions = {};
    private static _currentTheme = BitTheme._lightTheme;
    private static _onThemeChange: onThemeChangeType = () => { };

    /** When true, user pinned an explicit theme via set (not system); disables following OS until set('system'). */
    private static _stopFollowingSystem = false;

    private static _schemeMediaQuery: MediaQueryList | null = null;
    private static _onSchemeChange = () => BitTheme.applyResolvedSystemThemeFromOs();

    private static _dotnetNotifier: DotNetObject | null = null;

    private static _appliedVarKeys = new WeakMap<HTMLElement, string[]>();

    public static init(options: BitThemeOptions) {
        Object.assign(BitTheme._initOptions, options);

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
            theme = BitTheme.getPersisted() || theme;
        }

        BitTheme.set(theme, { fromInit: true });
        BitTheme.syncSystemThemeListener();
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
        const fromInit = options?.fromInit === true;
        const internalOs = options?.internalOsRefresh === true;

        if (!fromInit && !internalOs) {
            if (themeName === BitTheme.SYSTEM_THEME) {
                BitTheme._stopFollowingSystem = false;
            } else {
                BitTheme._stopFollowingSystem = true;
            }
        }

        BitTheme._currentTheme = BitTheme.getActualTheme(themeName)!;

        if (BitTheme._persist) {
            localStorage.setItem(BitTheme.THEME_STORAGE_KEY, themeName);
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

        return localStorage.getItem(BitTheme.THEME_STORAGE_KEY);
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
        if (BitTheme._persist && BitTheme.getPersisted() === BitTheme.SYSTEM_THEME) return true;
        if (document.documentElement.hasAttribute('bit-theme-system')) return true;
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
        BitTheme._schemeMediaQuery.addEventListener('change', BitTheme._onSchemeChange);
        const legacy = BitTheme._schemeMediaQuery as unknown as { addListener?: (cb: () => void) => void };
        legacy.addListener?.(BitTheme._onSchemeChange);
    }

    private static detachSystemThemeListener() {
        if (!BitTheme._schemeMediaQuery) return;
        BitTheme._schemeMediaQuery.removeEventListener('change', BitTheme._onSchemeChange);
        const legacy = BitTheme._schemeMediaQuery as unknown as { removeListener?: (cb: () => void) => void };
        legacy.removeListener?.(BitTheme._onSchemeChange);
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
            void n.invokeMethodAsync('NotifyThemeChangedFromJs', newTheme, oldTheme);
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
        let el = document.getElementById(linkId) as HTMLLinkElement | null;
        if (!el) {
            el = document.createElement('link');
            el.id = linkId;
            el.rel = 'stylesheet';
            document.head.appendChild(el);
        }
        el.href = href;
    }

    public static detach(linkId: string) {
        document.getElementById(linkId)?.remove();
    }
}

(function () {
    const options = {
        system: document.documentElement.hasAttribute('bit-theme-system'),
        persist: document.documentElement.hasAttribute('bit-theme-persist'),
        theme: document.documentElement.getAttribute('bit-theme'),
        default: document.documentElement.getAttribute('bit-theme-default'),
        darkTheme: document.documentElement.getAttribute('bit-theme-dark'),
        lightTheme: document.documentElement.getAttribute('bit-theme-light'),
    };

    BitTheme.init(options);
}());

(window as any).BitTheme = BitTheme;
(window as any).BitExternalTheme = BitExternalTheme;
