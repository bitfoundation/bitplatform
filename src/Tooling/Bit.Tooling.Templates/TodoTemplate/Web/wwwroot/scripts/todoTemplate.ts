var todoTemplate = (function () {

    return {
        setCookie: setCookie,
        getCookie: getCookie,
        removeCookie: removeCookie,
        goBack: goBack,
        reloadPage: reloadPage,
    };

    function setCookie(name: string, value: string, seconds: number): void {
        const date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/`;
    }

    function getCookie(name: string): string {
        const cookies = document.cookie.split(';');
        for (let i = 0; i < cookies.length; i++) {
            const cookie = cookies[i].split('=');
            if (trim(cookie[0]) == escape(name)) {
                return unescape(trim(cookie[1]));
            }
        }
        return "";
    }

    function removeCookie(name: string): void {
        document.cookie = `${name}=; Max-Age=0`;
    }

    function trim(value: string): string {
        return value.replace(/^\s+|\s+$/g, '');
    };

    function goBack(): void {
        window.history.back();
    }

    function reloadPage(): void {
        location.reload();
    }

}());