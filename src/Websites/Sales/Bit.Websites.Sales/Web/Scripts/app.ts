var currentUrlPath = window.location.pathname;
window.onscroll = () => handleHeaderScroll();

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

    public static setBodyStyle(value: string): void {
        document.body.setAttribute('style', value);
    }
}

//HeaderShadow======================================================================
function handleHeaderScroll() {
    const mainHeader = document.getElementById("mainHeader") as HTMLElement;
    if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
        if (mainHeader.className.indexOf("header-shadow") < 0) mainHeader.className += " header-shadow";
    } else {
        mainHeader.className = mainHeader.className.replace(" header-shadow", "");
    }

    if (currentUrlPath == "/process" || currentUrlPath == "/services" || currentUrlPath == "/about") {
        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
            mainHeader.className = mainHeader.className.replace(" transparent-header", "");
        } else {
            if (mainHeader.className.indexOf("transparent-header") < 0) mainHeader.className += " transparent-header";
        }
    }

    if (currentUrlPath == "/") {
        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
            mainHeader.className = mainHeader.className.replace(" home-page-header", "");
        } else {
            if (mainHeader.className.indexOf("home-page-header") < 0) mainHeader.className += " home-page-header";
        }
    }
}