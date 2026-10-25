// Browsers fill the OTP from an SMS through their own WebOTP API, which this file leaves alone. It only helps the
// Android app, whose WebView has no WebOTP: there, BitOtpInput's request is answered natively (See IAndroidAppWebOtpService).
export class AndroidAppWebOtp {
    private static jsBridgeObj: DotNetObject;
    private static credentialsGet?: CredentialsContainer['get'];
    private static requestId = 0;

    public static provide(jsBridgeObj: DotNetObject) {
        AndroidAppWebOtp.jsBridgeObj = jsBridgeObj;

        const credentials = navigator.credentials;
        if (!credentials || AndroidAppWebOtp.credentialsGet) return;

        // Only OTP requests are taken over; every other credential request (passkeys etc.) still goes to the WebView.
        AndroidAppWebOtp.credentialsGet = credentials.get.bind(credentials);
        credentials.get = (options?: CredentialRequestOptions & { otp?: unknown }) =>
            options?.otp ? AndroidAppWebOtp.receive(options.signal) : AndroidAppWebOtp.credentialsGet!(options);

        // Callers (BitOtpInput, Butil's WebOtp) feature-detect WebOTP by this global before calling get.
        (window as any).OTPCredential ??= class { };
    }

    private static async receive(signal?: AbortSignal) {
        if (signal?.aborted) throw new DOMException('The request was aborted.', 'AbortError');

        // A newer request replaces the pending one (See AppJsBridge.ReceiveAndroidAppWebOtp), so only the newest may cancel the native wait.
        const requestId = ++AndroidAppWebOtp.requestId;
        const cancel = () => {
            if (requestId === AndroidAppWebOtp.requestId) {
                AndroidAppWebOtp.jsBridgeObj.invokeMethodAsync('CancelAndroidAppWebOtp');
            }
        };

        signal?.addEventListener('abort', cancel);

        try {
            const code = await AndroidAppWebOtp.jsBridgeObj.invokeMethodAsync<string | null>('ReceiveAndroidAppWebOtp');

            if (!code || signal?.aborted || requestId !== AndroidAppWebOtp.requestId) {
                throw new DOMException('The request was aborted.', 'AbortError');
            }

            return { id: '', type: 'otp', code } as Credential;
        } finally {
            signal?.removeEventListener('abort', cancel);
        }
    }
}
