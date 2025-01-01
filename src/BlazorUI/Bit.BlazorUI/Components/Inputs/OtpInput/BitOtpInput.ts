namespace BitBlazorUI {
    export class OtpInput {
        private static abortControllers: { [key: string]: AbortController } = {};

        public static setup(id: string, dotnetObj: DotNetObject, input: HTMLInputElement) {
            input.addEventListener('focus', (e: any) => {
                e.target?.select();
            });

            input.addEventListener('paste', async e => {
                e.preventDefault();
                let pastedValue = e.clipboardData?.getData('Text');
                await dotnetObj.invokeMethodAsync("SetValue", pastedValue);
            });

            OtpInput.setupSmsAutofill(id, dotnetObj);
        }

        public static dispose(id: string) {
            const ac = OtpInput.abortControllers[id];
            if (!ac) return;

            ac.abort();
            delete OtpInput.abortControllers[id];
        }

        private static setupSmsAutofill(id: string, dotnetObj: DotNetObject) {
            if (!('OTPCredential' in window)) return;

            const abortCtrl = new AbortController();
            OtpInput.abortControllers[id] = abortCtrl;

            navigator.credentials.get({
                otp: { transport: ['sms'] },
                signal: abortCtrl.signal
            } as any).then(async (otp: any) => {
                await dotnetObj.invokeMethodAsync("SetValue", otp.code);
                abortCtrl.abort();
                delete OtpInput.abortControllers[id];
            }).catch(async (err: any) => {
                abortCtrl.abort();
                delete OtpInput.abortControllers[id];
            })
        }
    }
}