class App {
    public static setCookie(name: string, value: string, seconds: number) {
        const date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = `${name}=${value};expires=${date.toUTCString()};path=/`;
    }

    public static getCookie(name: string): string | null {
        return document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)')?.pop() || null;
    }

    public static removeCookie(name: string): void {
        document.cookie = `${name}=; Max-Age=0`;
    }

    public static goBack(): void {
        window.history.back();
    }

    public static getPreferredCulture(args: { currentCulture: string, defaultCulture: string, supportedCultures: string[] }): string {
        let culture = args.currentCulture;
        const preferredCulture = this.getCookie(".AspNetCore.Culture");
        if (preferredCulture != null) {
            culture = preferredCulture.substring(preferredCulture.indexOf("|uic=") + 5);
        }
        if (args.supportedCultures.some(sc => sc == culture) == false) {
            culture = args.defaultCulture;
        }
        if (preferredCulture != `c=${culture}|uic=${culture}`) {
            this.setCookie(".AspNetCore.Culture", `c=${culture}|uic=${culture}`, 31 * 24 * 3600);
        }
        return culture;
    }
}