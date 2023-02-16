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

    public static setBodyOverflow(hidden: boolean) {
        document.body.style.overflow = hidden ? "hidden" : "auto";
    }
}

window.addEventListener('contextmenu', (event) => event.preventDefault());