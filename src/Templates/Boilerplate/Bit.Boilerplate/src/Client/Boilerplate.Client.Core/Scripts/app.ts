class App {
    public static applyBodyElementClasses(cssClasses: string[], cssVariables: any): void {
        cssClasses?.forEach(c => document.body.classList.add(c));
        Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
    }
}

declare class BitTheme { static init(options: any): void; };

BitTheme.init({
    system: true,
    onChange: (newTheme: string, oldThem: string) => {
        if (newTheme === 'dark') {
            document.body.classList.add('theme-dark');
            document.body.classList.remove('theme-light');
            document.querySelector("meta[name=theme-color]")!.setAttribute('content', '#0d1117');
        } else {
            document.body.classList.add('theme-light');
            document.body.classList.remove('theme-dark');
            document.querySelector("meta[name=theme-color]")!.setAttribute('content', '#ffffff');
        }
    }
});
