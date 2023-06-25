class BitTheme {
    private static THEME_ATTRIBUTE = 'bit-theme';

    private static currentTheme = 'light';
    private static onChange: (newThemeName: string, oldThemeName: string) => void = () => { };

    static init(options: any) {
        if (options.system) {
            this.currentTheme = this.isSystemThemeDark() ? 'dark' : 'light';
        } else if (options.default) {
            this.currentTheme = options.default;
        }

        this.onChange = options.onChange;

        this.setTheme(this.currentTheme);
    }

    static getCurrentTheme() {
        this.currentTheme = document.documentElement.getAttribute(this.THEME_ATTRIBUTE) || '';

        return this.currentTheme;
    }

    static toggleThemeDarkLight() {
        this.currentTheme = this.currentTheme === 'light' ? 'dark' : 'light';

        this.setTheme(this.currentTheme);

        return this.currentTheme;
    }

    static setTheme(themeName: string) {
        const oldTheme = document.documentElement.getAttribute(this.THEME_ATTRIBUTE) || '';
        document.documentElement.setAttribute(this.THEME_ATTRIBUTE, themeName);

        this.onChange(themeName, oldTheme);
    }

    static applyBitTheme(theme: any, element?: HTMLElement) {
        const el = element || document.body;
        Object.keys(theme).forEach(key => el.style.setProperty(key, theme[key]));
    }

    static isSystemThemeDark() {
        return matchMedia('(prefers-color-scheme: dark)').matches;
    }
}