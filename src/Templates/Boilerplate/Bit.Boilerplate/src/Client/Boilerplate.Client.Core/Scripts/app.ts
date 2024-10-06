//+:cnd:noEmit
class App {
    public static applyBodyElementClasses(cssClasses: string[], cssVariables: any): void {
        cssClasses?.forEach(c => document.body.classList.add(c));
        Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
    }

    public static getPlatform(): string {
        return (navigator as any).userAgentData?.platform || navigator?.platform;
    }

    //#if (notification == true)
    public static async getDeviceInstallation(vapidPublicKey: string) {
        if (await Notification.requestPermission() == "granted") {
            const registration = await navigator.serviceWorker.ready;
            if (!registration) return null;
            const pushManager = registration.pushManager;
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
            return { installationId: `${p256dh}-${auth}`, platform: "browser", endpoint: pushChannel.endpoint, auth, p256dh };
        }
        return null;
    }
    //#endif
}

declare class BitTheme { static init(options: any): void; };

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
