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
