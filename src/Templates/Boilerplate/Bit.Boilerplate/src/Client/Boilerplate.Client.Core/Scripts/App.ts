//+:cnd:noEmit

export class App {
    // For additional details, see the JsBridge.cs file.
    private static jsBridgeObj: DotNetObject;
    private static credentialsGet?: CredentialsContainer['get'];
    private static smsOtpRequestId = 0;

    public static registerJsBridge(dotnetObj: DotNetObject, receivesSmsOtp: boolean) {
        App.jsBridgeObj = dotnetObj;

        if (receivesSmsOtp) {
            App.provideWebOtp();
        }
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync('ShowDiagnostic');
    }

    public static publishMessage(message: string, payload: unknown) {
        return App.jsBridgeObj?.invokeMethodAsync('PublishMessage', message, payload);
    }

    private static readonly erudaUrl = 'https://cdn.jsdelivr.net/npm/eruda@3.4.3/eruda.min.js';
    private static readonly erudaIntegrity = 'sha384-F7xQBvh3l6dG/mMD6QPIeVmXtzWT4Ce3ZDu8ysPuzMWMx9bFOIMGnRPUhLuQipss';
    private static readonly erudaScriptId = 'app-eruda-script';

    public static openDevTools() {
        const eruda = window.eruda;

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
            window.eruda?.init();
            window.eruda?.show();
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

    private static provideWebOtp() {
        const credentials = navigator.credentials;
        if (!credentials || App.credentialsGet) return;

        App.credentialsGet = credentials.get.bind(credentials);
        credentials.get = (options?: CredentialRequestOptions & { otp?: unknown }) =>
            options?.otp ? App.receiveSmsOtp(options.signal) : App.credentialsGet!(options);

        (window as any).OTPCredential ??= class { };
    }

    private static async receiveSmsOtp(signal?: AbortSignal) {
        if (signal?.aborted) throw new DOMException('The request was aborted.', 'AbortError');

        const requestId = ++App.smsOtpRequestId;
        const cancel = () => {
            if (requestId === App.smsOtpRequestId) {
                App.jsBridgeObj.invokeMethodAsync('CancelSmsOtp');
            }
        };

        signal?.addEventListener('abort', cancel);

        try {
            const code = await App.jsBridgeObj.invokeMethodAsync<string | null>('ReceiveSmsOtp');

            if (!code || signal?.aborted || requestId !== App.smsOtpRequestId) {
                throw new DOMException('The request was aborted.', 'AbortError');
            }

            return { id: '', type: 'otp', code } as Credential;
        } finally {
            signal?.removeEventListener('abort', cancel);
        }
    }
}

