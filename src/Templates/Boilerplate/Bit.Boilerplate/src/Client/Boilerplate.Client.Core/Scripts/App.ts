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

    private static readonly erudaUrl = 'https://cdn.jsdelivr.net/npm/eruda@3.4.3/eruda.min.js';
    private static readonly erudaIntegrity = 'sha384-F7xQBvh3l6dG/mMD6QPIeVmXtzWT4Ce3ZDu8ysPuzMWMx9bFOIMGnRPUhLuQipss';
    private static readonly erudaScriptId = 'app-eruda-script';

    public static openDevTools() {
        const eruda = (window as any).eruda;

        if (eruda) {
            eruda.show();
            return;
        }

        // window.eruda only exists once the CDN script has run, so a second press while the first request is still
        // in flight would append a second copy and init it twice. The element is the marker for that in-flight load.
        if (document.getElementById(App.erudaScriptId)) return;

        const script = document.createElement('script');
        script.id = App.erudaScriptId;
        script.src = App.erudaUrl;
        script.integrity = App.erudaIntegrity;
        script.crossOrigin = 'anonymous';
        script.onload = () => {
            (window as any).eruda.init();
            (window as any).eruda.show();
        };
        script.onerror = () => {
            script.remove();
            console.error(`Failed to load the dev tools from ${App.erudaUrl}.`);
        };
        document.body.append(script);
    }

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

