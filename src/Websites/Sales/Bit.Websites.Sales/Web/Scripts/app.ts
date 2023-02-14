class App {
    public static setCookie(name: string, value: string, seconds: number) {
        const date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/`;
    }

    public static getCookie(name: string) {
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].split('=');
            if (App.trim(cookie[0]) == escape(name)) {
                return unescape(App.trim(cookie[1]));
            }
        }
    }

    public static removeCookie(name: string) {
        document.cookie = `${name}=; Max-Age=0`;
    }

    public static goBack() {
        window.history.back();
    }

    public static reloadPage() {
        location.reload();
    }

    private static trim(value: string) {
        return value.replace(/^\s+|\s+$/g, '');
    }

    public static setBodyStyle(value: string) {
        document.body.setAttribute('style', value);
    }

    public static scrollToContactUs() {
        document.querySelector('#contactus')?.scrollIntoView();
    }

    public static scrollIntoView(id: string) {
        document.getElementById(id)?.scrollIntoView();
    }

    public static goToTop() {
        window.scrollTo({ top: 0 });
    }
}
