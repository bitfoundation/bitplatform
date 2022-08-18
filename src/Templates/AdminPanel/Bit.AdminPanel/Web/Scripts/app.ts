class App {
    public static setCookie(name: string, value: string, seconds: number) {
        const date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/`;
    }

    public static getCookie(name: string): string | undefined {
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].split('=');
            if (App.trim(cookie[0]) == escape(name)) {
                return unescape(App.trim(cookie[1]));
            }
        }
    }

    public static removeCookie(name: string): void {
        document.cookie = `${name}=; Max-Age=0`;
    }

    public static goBack(): void {
        window.history.back();
    }

    public static reloadPage(): void {
        location.reload();
    }

    private static trim(value: string): string {
        return value.replace(/^\s+|\s+$/g, '');
    }
}

function toggleBodyOverflow(isOverflowHidden: boolean) {
    if (isOverflowHidden) {
        document.body.style.overflow = "hidden";
    } else {
        document.body.style.overflow = "auto";
    }
}

function navigateToPrevUrl() {
    history.back();
}