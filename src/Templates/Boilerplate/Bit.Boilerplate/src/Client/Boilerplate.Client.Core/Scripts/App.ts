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

    /** The channel a sign-in or approval window hands its answer over when its opener was cut (See WebInteropApp.handOver). */
    public static readonly externalCallbackChannel = 'bit-external-callback';

    private static externalCallbackExpectedUntil = 0;

    /**
     * Called by `DefaultExternalNavigationService.cs` when it opens a sign-in or approval window, so this page, and no
     * other tab of the app, takes the answer that comes back over `externalCallbackChannel`.
     */
    public static expectExternalCallback() {
        App.externalCallbackExpectedUntil = Date.now() + 30 * 60 * 1000;
    }

    /** True once for the window this page opened: a second answer, or one half an hour later, is nobody's. */
    public static takeExternalCallback() {
        if (Date.now() > App.externalCallbackExpectedUntil) return false;
        App.externalCallbackExpectedUntil = 0;
        return true;
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

