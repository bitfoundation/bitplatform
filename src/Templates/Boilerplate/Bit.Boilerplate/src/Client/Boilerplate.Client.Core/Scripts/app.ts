//+:cnd:noEmit
class App {
    private static jsBridgeObj: DotNetObject;

    public static registerJsBridge(dotnetObj: DotNetObject) {
        // For additional details, see the JsBridge.cs file.
        App.jsBridgeObj = dotnetObj;
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync("ShowDiagnostic");
    }

    public static applyBodyElementClasses(cssClasses: string[], cssVariables: any): void {
        cssClasses?.forEach(c => document.body.classList.add(c));
        Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
    }

    public static getPlatform(): string {
        return (navigator as any).userAgentData?.platform || navigator?.platform;
    }

    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }

    //#if (notification == true)
    public static async getDeviceInstallation(vapidPublicKey: string) {
        if (!("Notification" in window)) return null;

        if (await Notification.requestPermission() != "granted") return null;

        const registration = await navigator.serviceWorker.ready;
        if (!registration) return null;

        const pushManager = registration.pushManager;
        if (pushManager == null) return null;

        let subscription = await pushManager.getSubscription();
        if (subscription == null) {
            subscription = await pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: vapidPublicKey
            });
        }
        const pushChannel = subscription.toJSON();
        const p256dh = pushChannel.keys!["p256dh"];
        const auth = pushChannel.keys!["auth"];
        return { installationId: `${p256dh}-${auth}`, platform: "browser", p256dh: p256dh, auth: auth, endpoint: pushChannel.endpoint };
    };
    //#endif
}

declare class BitTheme { static init(options: any): void; };

interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

(function () {
    setCssWindowSizes();

    window.addEventListener('resize', setCssWindowSizes);

    function setCssWindowSizes() {
        document.documentElement.style.setProperty('--win-width', `${window.innerWidth}px`);
        document.documentElement.style.setProperty('--win-height', `${window.innerHeight}px`);
    }
}());

BitTheme.init({
    system: true,
    onChange: (newTheme: string, oldThem: string) => {
        if (newTheme === 'dark') {
            document.body.classList.add('theme-dark');
            document.body.classList.remove('theme-light');
        } else {
            document.body.classList.add('theme-light');
            document.body.classList.remove('theme-dark');
        }
        const primaryBgColor = getComputedStyle(document.documentElement).getPropertyValue('--bit-clr-bg-pri');
        document.querySelector("meta[name=theme-color]")!.setAttribute('content', primaryBgColor);
    }
});
