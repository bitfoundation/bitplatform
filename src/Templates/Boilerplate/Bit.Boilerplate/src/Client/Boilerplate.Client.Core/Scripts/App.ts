//+:cnd:noEmit

export class App {
    // For additional details, see the JsBridge.cs file.
    private static jsBridgeObj: DotNetObject;

    public static registerJsBridge(dotnetObj: DotNetObject) {
        App.jsBridgeObj = dotnetObj;
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync('ShowDiagnostic');
    }

    public static publishMessage(message: string, payload: any) {
        return App.jsBridgeObj?.invokeMethodAsync('PublishMessage', message, payload);
    }

    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }

    public static openDevTools() {
        const allScripts = Array.from(document.scripts).map(s => s.src);
        const scriptAppended = allScripts.find(as => as.includes('npm/eruda'));

        if (scriptAppended) {
            (window as any).eruda.show();
            return;
        }

        const script = document.createElement('script');
        script.src = "https://cdn.jsdelivr.net/npm/eruda";
        document.body.append(script);
        script.onload = function () {
            (window as any).eruda.init();
            (window as any).eruda.show();
        }
    }

    //#if (notification == true)
    public static async getPushNotificationSubscription(vapidPublicKey: string) {
        const registration = await navigator.serviceWorker.ready;
        if (!registration) return null;

        const pushManager = registration.pushManager;
        if (!pushManager) return null;

        let subscription = await pushManager.getSubscription();

        if (!subscription) {
            subscription = await pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: vapidPublicKey
            });
        }

        const pushChannel = subscription.toJSON();
        const p256dh = pushChannel.keys!['p256dh'];
        const auth = pushChannel.keys!['auth'];

        return {
            deviceId: `${p256dh}-${auth}`,
            platform: 'browser',
            p256dh: p256dh,
            auth: auth,
            endpoint: pushChannel.endpoint
        };
    };
    //#endif

    /* Checks for and applies updates if available.
       Called by `WebAppUpdateService.cs` when the user clicks the app version in `AppShell.razor`
       or when `ForceUpdateSnackbar.razor` appears after a forced update. */
    public static async tryUpdatePwa(autoReload: boolean) {
        const bswup = (window as any).BitBswup; // https://bitplatform.dev/bswup
        if (!bswup) return;

        if (autoReload) {
            if (await bswup.skipWaiting()) return; // Use new service worker if available and reload the page.
        }

        const bswupProgress = (window as any).BitBswupProgress;
        if (!bswupProgress) return;

        bswupProgress.config({ autoReload });

        bswup.checkForUpdate();
    }
}

