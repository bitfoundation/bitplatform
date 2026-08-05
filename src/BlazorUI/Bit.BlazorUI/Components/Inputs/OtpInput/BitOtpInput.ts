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

            // The row of boxes reads as a single field, so the gaps between them (and the separators
            // sitting in those gaps) have to behave like a part of it rather than as dead space. A click
            // that misses a box lands on the input the typing is meant to carry on in, which is the first
            // one left to fill, or on the last one when the code is already complete.
            root.addEventListener('click', (e: Event) => {
                if (OtpInput.getInput(e)) return;

                const target = e.target as HTMLElement;
                if (!target?.closest?.('.bit-otp-iwr')) return;

                const inputs = Array.from(root.querySelectorAll<HTMLInputElement>('input.bit-otp-inp'))
                                    .filter(i => !i.disabled && !i.readOnly);
                if (!inputs.length) return;

                (inputs.find(i => !i.value) ?? inputs[inputs.length - 1]).focus();
            }, { signal });

            // A code is copied as often as it is pasted (out of one tab and into another, out of the page
            // and into an app), and each box being an input of its own is what would otherwise turn a copy
            // into the single character the focused box holds. The whole code goes onto the clipboard
            // instead, which is what a single input holding it would have handed over.
            root.addEventListener('copy', (e: ClipboardEvent) => {
                OtpInput.writeCodeToClipboard(e, root);
            }, { signal });

            // A cut is that same copy, with the code taken out of the inputs afterwards. The clipboard has
            // to be written synchronously here, so the clearing is what is left to the round trip.
            root.addEventListener('cut', (e: ClipboardEvent) => {
                if (!OtpInput.writeCodeToClipboard(e, root)) return;

                dotnetObj.invokeMethodAsync('ClearValue').catch(() => {
                    // the component may already be disposed at this point, which is not an error here.
                });
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

        /**
         * Puts the whole code on the clipboard of the event, and reports whether it did. A code that is
         * not shown is left alone (the boxes are holding a masking character rather than the code, and a
         * password input refuses to be copied for the very same reason), and so is an empty one.
         */
        private static writeCodeToClipboard(e: ClipboardEvent, root: HTMLElement): boolean {
            if (!OtpInput.getInput(e)) return false;
            if (!e.clipboardData) return false;
            if (root.dataset.bitOtpNocopy === 'true') return false;

            const code = Array.from(root.querySelectorAll<HTMLInputElement>('input.bit-otp-inp'))
                              .map(i => i.value)
                              .join('');
            if (!code) return false;

            e.clipboardData.setData('text/plain', code);
            e.preventDefault();

            return true;
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
