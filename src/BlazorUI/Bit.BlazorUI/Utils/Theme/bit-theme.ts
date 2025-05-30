type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

class BitTheme {
    private static SYSTEM_THEME = 'system';
    private static THEME_ATTRIBUTE = 'bit-theme';
    private static THEME_STORAGE_KEY = 'bit-current-theme';

    private static _darkTheme: string = 'dark';
    private static _lightTheme: string = 'light';

    private static _persist = false;
    private static _currentTheme = BitTheme._lightTheme;
    private static _onThemeChange: onThemeChangeType = () => { };

    public static init(options: any) {
        if (options.onChange) {
            BitTheme._onThemeChange = options.onChange;
        }

        if (options.darkTheme) {
            BitTheme._darkTheme = options.darkTheme;
        }

        if (options.lightTheme) {
            BitTheme._lightTheme = options.lightTheme;
        }

        let theme = BitTheme._lightTheme;

        if (options.system) {
            theme = BitTheme.isSystemDark()
                ? BitTheme._darkTheme
                : BitTheme._lightTheme;
        } else if (options.default) {
            theme = options.default;
        }

        if (options.persist) {
            BitTheme._persist = true;
            theme = BitTheme.getPersisted() || theme;
        }

        BitTheme.set(theme);
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

    public static set(themeName: string) {
        BitTheme._currentTheme = BitTheme.getActualTheme(themeName)!;

        if (BitTheme._persist) {
            localStorage.setItem(BitTheme.THEME_STORAGE_KEY, themeName);
        }

        const oldTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        document.documentElement.setAttribute(BitTheme.THEME_ATTRIBUTE, BitTheme._currentTheme);

        BitTheme._onThemeChange?.(BitTheme._currentTheme, oldTheme);

        return BitTheme._currentTheme;
    }

    public static toggleDarkLight() {
        BitTheme._currentTheme = BitTheme._currentTheme === BitTheme._lightTheme
            ? BitTheme._darkTheme
            : BitTheme._lightTheme;

        BitTheme.set(BitTheme._currentTheme);

        return BitTheme._currentTheme;
    }

    public static applyBitTheme(theme: any, element?: HTMLElement) {
        const el = element || document.body;
        Object.keys(theme).forEach(key => el.style.setProperty(key, theme[key]));
    }

    public static isSystemDark() {
        return matchMedia('(prefers-color-scheme: dark)').matches;
    }

    public static getPersisted() {
        if (!BitTheme._persist) return null;

        return localStorage.getItem(BitTheme.THEME_STORAGE_KEY);
    }

    private static getActualTheme(theme: string | null) {
        if (theme === BitTheme.SYSTEM_THEME) {
            return BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        }

        return theme;
    }
}

(function () {
    const options = {
        darkTheme: document.documentElement.getAttribute('bit-theme-dark'),
        lightTheme: document.documentElement.getAttribute('bit-theme-light'),
        system: document.documentElement.hasAttribute('bit-theme-system'),
        default: document.documentElement.getAttribute('bit-theme-default'),
        persist: document.documentElement.hasAttribute('bit-theme-persist'),
    };

    BitTheme.init(options);
}());

(window as any).BitTheme = BitTheme;
