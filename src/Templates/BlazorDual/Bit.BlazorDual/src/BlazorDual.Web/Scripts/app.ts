class App {
    public static setCookie(name: string, value: string, seconds: number) {
        const date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/`;
    }

    public static getCookie(name: string): string | null {
        // https://stackoverflow.com/a/25490531/2720104
        return document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)')?.pop() || null;
    }

    public static removeCookie(name: string): void {
        document.cookie = `${name}=; Max-Age=0`;
    }

    public static goBack(): void {
        window.history.back();
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