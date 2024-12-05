﻿//+:cnd:noEmit
class App {
    private static jsBridgeObj: DotNetObject;

    public static registerJsBridge(dotnetObj: DotNetObject) {
        // For additional details, see the JsBridge.cs file.
        App.jsBridgeObj = dotnetObj;
        window.addEventListener('message', e => {
            // Enable publishing messages from JavaScript's `window.postMessage` to the C# `PubSubService`.
            if (e.data.key === 'PUBLISH_MESSAGE') {
                App.jsBridgeObj?.invokeMethodAsync('PublishMessage', e.data.message, e.data.payload);
            }
        });
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync('ShowDiagnostic');
    }

    public static applyBodyElementClasses(cssClasses: string[], cssVariables: any): void {
        cssClasses?.forEach(c => document.body.classList.add(c));
        Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
    }

    public static getPlatform(): string {
        let data = [(navigator as any).userAgentData?.platform ?? navigator?.platform];

        if (navigator.userAgent.includes('Firefox'))
            data.push('Firefox browser');
        else if (navigator.userAgent.includes('Edg'))
            data.push('Edge browser');
        else if (navigator.userAgent.includes('OPR'))
            data.push('Opera browser');
        else if (navigator.userAgent.includes('Chrome'))
            data.push('Chrome browser');
        else if (navigator.userAgent.includes('Safari'))
            data.push('Safari browser');
        else
            data.push('Unknown browser');

        return data.filter(d => d != null).join(' ');
    }

    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }

    //#if (notification == true)
    public static async getPushNotificationSubscription(vapidPublicKey: string) {
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
        const p256dh = pushChannel.keys!['p256dh'];
        const auth = pushChannel.keys!['auth'];
        return { deviceId: `${p256dh}-${auth}`, platform: 'browser', p256dh: p256dh, auth: auth, endpoint: pushChannel.endpoint };
    };
    //#endif
}

declare class BitTheme { static init(options: any): void; };

interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

window.addEventListener('load', () => {
    setCssWindowSizes();
    if (window.opener != null) {
        // The IExternalNavigationService is responsible for opening pages in a new window,
        // such as during social sign-in flows. Once the external navigation is complete,
        // and the user is redirected back to the newly opened window,
        // the following code ensures that the original window is notified of where it should navigate next.
        window.opener.postMessage({ key: 'PUBLISH_MESSAGE', message: 'NAVIGATE_TO', payload: window.location.href });
        window.close();
    }
});
window.addEventListener('resize', setCssWindowSizes);

function setCssWindowSizes() {
    document.documentElement.style.setProperty('--win-width', `${window.innerWidth}px`);
    document.documentElement.style.setProperty('--win-height', `${window.innerHeight}px`);
}

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
        document.querySelector('meta[name=theme-color]')!.setAttribute('content', primaryBgColor);
    }
});
