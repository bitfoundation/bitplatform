//+:cnd:noEmit

import { AndroidAppWebOtp } from './AndroidAppWebOtp';

export class App {
    // For additional details, see the JsBridge.cs file.
    private static jsBridgeObj: DotNetObject;

    public static registerJsBridge(dotnetObj: DotNetObject, hasAndroidAppWebOtp: boolean) {
        App.jsBridgeObj = dotnetObj;

        if (hasAndroidAppWebOtp) {
            AndroidAppWebOtp.provide(dotnetObj);
        }
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync('ShowDiagnostic');
    }

    public static publishMessage(message: string, payload: unknown) {
        return App.jsBridgeObj?.invokeMethodAsync('PublishMessage', message, payload);
    }

    /* Adds stylesheets and scripts a feature brings only once it's used, each once however often it's asked for.
       Called by `AppAiChatPanel.razor.cs`, which loads the AI chat when it first opens. */
    public static loadAssets(styles: string[], scripts: string[]) {
        const load = (tag: 'link' | 'script', attributes: Record<string, string>, selector: string) => document.querySelector(selector)
            ? Promise.resolve()
            : new Promise<void>((resolve, reject) => {
                const element = Object.assign(document.createElement(tag), attributes, { onload: () => resolve(), onerror: reject });
                (tag === 'link' ? document.head : document.body).appendChild(element);
            });

        return Promise.all([
            ...styles.map(href => load('link', { rel: 'stylesheet', href }, `link[href="${href}"]`)),
            ...scripts.map(src => load('script', { src }, `script[src="${src}"]`))
        ]);
    }

    /* Checks for and applies updates if available.
       Called by `WebAppUpdateService.cs` when the user clicks the app version in `AppShell.razor`
       or when `ForceUpdateSnackbar.razor` appears after a forced update. */
    public static async tryUpdatePwa(autoReload: boolean) {
        const bswup = window.BitBswup; // https://bitplatform.dev/bswup
        if (!bswup) return;

        if (autoReload) {
            if (await bswup.skipWaiting()) return; // Use new service worker if available and reload the page.
        }

        const bswupProgress = window.BitBswupProgress;
        if (!bswupProgress) return;

        bswupProgress.config({ autoReload });

        bswup.checkForUpdate();
    }
}

