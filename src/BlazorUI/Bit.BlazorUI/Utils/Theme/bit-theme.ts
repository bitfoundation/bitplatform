type onThemeChangeType = (newThemeName: string, oldThemeName: string) => void;

class BitTheme {
    private static THEME_ATTRIBUTE = 'bit-theme';
    private static THEME_STORAGE_KEY = 'bit-current-theme';

    private static currentTheme = 'light';
    private static onThemeChange: onThemeChangeType = () => { };

    public static init(options: any) {
        if (options.system) {
            BitTheme.currentTheme = BitTheme.isSystemDark() ? 'dark' : 'light';
        } else if (options.default) {
            BitTheme.currentTheme = options.default;
        }

        if (options.persist) {
            BitTheme.currentTheme = localStorage.getItem(BitTheme.THEME_STORAGE_KEY) || BitTheme.currentTheme;
        }

        BitTheme.onThemeChange = options.onChange;

        BitTheme.set(BitTheme.currentTheme);
    }

    public static onChange(fn: onThemeChangeType) {
        BitTheme.onThemeChange = fn;
    }

    public static useSystem() {
        BitTheme.currentTheme = BitTheme.isSystemDark() ? 'dark' : 'light';
        BitTheme.set(BitTheme.currentTheme);
    }

    public static get() {
        BitTheme.currentTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        return BitTheme.currentTheme;
    }

    public static set(themeName: string) {
        BitTheme.currentTheme = themeName;
        localStorage.setItem(BitTheme.THEME_STORAGE_KEY, themeName);
        const oldTheme = document.documentElement.getAttribute(BitTheme.THEME_ATTRIBUTE) || '';

        document.documentElement.setAttribute(BitTheme.THEME_ATTRIBUTE, themeName);

        BitTheme.onThemeChange?.(themeName, oldTheme);
    }

    public static toggleDarkLight() {
        BitTheme.currentTheme = BitTheme.currentTheme === 'light' ? 'dark' : 'light';

        BitTheme.set(BitTheme.currentTheme);

        return BitTheme.currentTheme;
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