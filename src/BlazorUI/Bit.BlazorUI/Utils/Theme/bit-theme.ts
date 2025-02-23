type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

class BitTheme {
    private static THEME_ATTRIBUTE = 'bit-theme';
    private static THEME_STORAGE_KEY = 'bit-current-theme';

    private static _darkTheme: string = 'dark';
    private static _lightTheme: string = 'light';

    private static _currentTheme = BitTheme._lightTheme;
    private static _onThemeChange: onThemeChangeType = () => { };

    public static init(options: any) {
        if (options.system) {
            BitTheme._currentTheme = BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        } else if (options.default) {
            BitTheme._currentTheme = options.default;
        }

        if (options.persist) {
            BitTheme._currentTheme = localStorage.getItem(BitTheme.THEME_STORAGE_KEY) || BitTheme._currentTheme;
        }

        if (options.darkTheme) {
            BitTheme._darkTheme = options.darkTheme;
        }

        if (options.lightTheme) {
            BitTheme._lightTheme = options.lightTheme;
        }

        BitTheme._onThemeChange = options.onChange;

        BitTheme.set(BitTheme._currentTheme);
    }

    public static onChange(fn: onThemeChangeType) {
        BitTheme._onThemeChange = fn;
    }

    public static useSystem() {
        BitTheme._currentTheme = BitTheme.isSystemDark() ? BitTheme._darkTheme : BitTheme._lightTheme;
        BitTheme.set(BitTheme._currentTheme);
    }

    public static get() {
        BitTheme._currentTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        return BitTheme._currentTheme;
    }

    public static set(themeName: string) {
        BitTheme._currentTheme = themeName;
        localStorage.setItem(BitTheme.THEME_STORAGE_KEY, themeName);
        const oldTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        document.documentElement.setAttribute(BitTheme.THEME_ATTRIBUTE, themeName);

        BitTheme._onThemeChange?.(themeName, oldTheme);
    }

    public static toggleDarkLight() {
        BitTheme._currentTheme = BitTheme._currentTheme === BitTheme._lightTheme ? BitTheme._darkTheme : BitTheme._lightTheme;

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
}

if (document.documentElement.hasAttribute('use-system-theme')) {
    BitTheme.useSystem();
}