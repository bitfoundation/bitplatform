namespace BitBlazorUI {
    export class OtpInput {
        private static abortControllers: { [key: string]: AbortController } = {};

        /**
         * The listeners are delegated to the root element instead of being attached to every input, so
         * that a component whose Length changes keeps working without re-registering anything, and so
         * that a single AbortController can detach all of them (plus the WebOTP request) on dispose.
         */
        public static setup(id: string, dotnetObj: DotNetObject, root: HTMLElement, smsAutoFill: boolean) {
            if (!root) return;

            OtpInput.dispose(id);

            const abortCtrl = new AbortController();
            OtpInput.abortControllers[id] = abortCtrl;
            const signal = abortCtrl.signal;

            // focusin instead of focus, since only the former bubbles up to the root element.
            root.addEventListener('focusin', (e: Event) => {
                const input = OtpInput.getInput(e);
                input?.select();
            }, { signal });

            root.addEventListener('paste', async (e: ClipboardEvent) => {
                const input = OtpInput.getInput(e);
                if (!input) return;

                e.preventDefault();

                const pastedValue = e.clipboardData?.getData('Text');
                if (!pastedValue) return;

                await OtpInput.setValue(dotnetObj, pastedValue, OtpInput.getIndex(root, input));
            }, { signal });

            if (smsAutoFill) {
                OtpInput.setupSmsAutoFill(dotnetObj, signal);
            }
        }

        /**
         * Blurs the input that currently has the focus inside the component, which is what dismisses the
         * virtual keyboard of a phone once the code is complete. Nothing happens when the focus is
         * somewhere else on the page, so a component that filled itself in the background never steals
         * the focus away from what the user is doing.
         */
        public static blur(root: HTMLElement) {
            if (!root) return;

            const active = document.activeElement as HTMLElement;

            if (!active || !root.contains(active)) return;

            active.blur?.();
        }

        public static dispose(id: string) {
            const ac = OtpInput.abortControllers[id];
            if (!ac) return;

            ac.abort();
            delete OtpInput.abortControllers[id];
        }

        private static getInput(e: Event): HTMLInputElement | null {
            const target = e.target as HTMLInputElement;

            return target?.tagName === 'INPUT' && target.classList.contains('bit-otp-inp') ? target : null;
        }

        private static getIndex(root: HTMLElement, input: HTMLInputElement): number {
            const inputs = Array.from(root.querySelectorAll('input.bit-otp-inp'));

            return Math.max(inputs.indexOf(input), 0);
        }

        private static async setValue(dotnetObj: DotNetObject, value: string, index: number) {
            try {
                await dotnetObj.invokeMethodAsync("SetValue", value, index);
            } catch (e) {
                // the component may already be disposed at this point, which is not an error here.
            }
        }

        private static setupSmsAutoFill(dotnetObj: DotNetObject, signal: AbortSignal) {
            // The WebOTP API is only available to a secure top level browsing context, and the credential
            // management API it hangs off of is missing altogether outside of one, so asking for the
            // feature alone is not enough to know that the call below can be made.
            if (!('OTPCredential' in window)) return;
            if (!navigator.credentials?.get) return;
            if (window.top !== window.self) return;

            try {
                navigator.credentials.get({
                    otp: { transport: ['sms'] },
                    signal: signal
                } as any).then(async (otp: any) => {
                    if (!otp?.code) return;

                    await OtpInput.setValue(dotnetObj, otp.code, 0);
                }).catch(() => {
                    // the request is aborted on dispose and rejected when the user dismisses it.
                });
            } catch (e) {
                // a browser that rejects the request synchronously (an insecure context above all) must
                // not take the rest of the component down with it.
            }
        }
    }
}
