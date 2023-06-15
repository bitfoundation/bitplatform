class BitTheme {

    static changeTheme(themeName: string) {
        document.documentElement.setAttribute('bit-theme', themeName);
    }

    static applyBitTheme(theme: any, element?: HTMLElement) {
        const el = element || document.body;
        Object.keys(theme).forEach(key => el.style.setProperty(key, theme[key]));
    }

}