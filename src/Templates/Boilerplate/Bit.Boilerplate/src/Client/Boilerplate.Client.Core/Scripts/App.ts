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

