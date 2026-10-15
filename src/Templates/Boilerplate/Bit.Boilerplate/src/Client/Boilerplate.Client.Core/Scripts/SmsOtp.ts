// Browsers fill the OTP from an SMS through their own WebOTP API, which this file leaves alone. It only helps the
// Android app, whose WebView has no WebOTP: there, BitOtpInput's request is answered natively (See ISmsOtpReceiver).
export class SmsOtp {
    private static jsBridgeObj: DotNetObject;
    private static credentialsGet?: CredentialsContainer['get'];
    private static requestId = 0;

    public static provideWebOtp(jsBridgeObj: DotNetObject) {
        SmsOtp.jsBridgeObj = jsBridgeObj;

        const credentials = navigator.credentials;
        if (!credentials || SmsOtp.credentialsGet) return;

        // Only OTP requests are taken over; every other credential request (passkeys etc.) still goes to the WebView.
        SmsOtp.credentialsGet = credentials.get.bind(credentials);
        credentials.get = (options?: CredentialRequestOptions & { otp?: unknown }) =>
            options?.otp ? SmsOtp.receive(options.signal) : SmsOtp.credentialsGet!(options);

        // Callers (BitOtpInput, Butil's WebOtp) feature-detect WebOTP by this global before calling get.
        (window as any).OTPCredential ??= class { };
    }

    private static async receive(signal?: AbortSignal) {
        if (signal?.aborted) throw new DOMException('The request was aborted.', 'AbortError');

        // A newer request replaces the pending one (See AppJsBridge.ReceiveSmsOtp), so only the newest may cancel the native wait.
        const requestId = ++SmsOtp.requestId;
        const cancel = () => {
            if (requestId === SmsOtp.requestId) {
                SmsOtp.jsBridgeObj.invokeMethodAsync('CancelSmsOtp');
            }
        };

        signal?.addEventListener('abort', cancel);

        try {
            const code = await SmsOtp.jsBridgeObj.invokeMethodAsync<string | null>('ReceiveSmsOtp');

            if (!code || signal?.aborted || requestId !== SmsOtp.requestId) {
                throw new DOMException('The request was aborted.', 'AbortError');
            }

            return { id: '', type: 'otp', code } as Credential;
        } finally {
            signal?.removeEventListener('abort', cancel);
        }
    }
}
