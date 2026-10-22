namespace BitBlazorUI {
    // The browser side of BitAiVoiceInput: dictation through the speech API of the browser, with no round trip to the
    // app's own server. The audio is not kept on the device for all that: the speech service of the browser may send it to
    // its vendor to be transcribed (Chrome's does). One recognizer runs at a time - starting a second one stops the first,
    // and tells the component that started the first one that its dictation is over - and each one gets a handle of its
    // own, so a component disposing late cannot stop a recognizer another one has started since.
    export class AiVoiceInput {
        private static _recognition: any = null;
        private static _handle = 0;
        private static _nextHandle = 1;
        private static _owner: DotNetObject | null = null;
        private static _ownerId: string | null = null;

        // Whether this browser offers a speech API to transcribe with.
        public static isDictationSupported() {
            const w = window as any;

            return !!(w.SpeechRecognition || w.webkitSpeechRecognition);
        }

        // Starts dictation and returns its handle, or 0 when the browser cannot transcribe. ownerId names the component
        // asking, so the one whose dictation this start ends is told so - its recognizer's own end is ignored from here on,
        // being no longer the live one - while a component starting again over its own is not told it has stopped.
        public static startDictation(dotnetObj: DotNetObject, language: string | null, ownerId: string | null): number {
            const w = window as any;
            const Recognition = w.SpeechRecognition || w.webkitSpeechRecognition;
            if (!Recognition) return 0;

            const previous = AiVoiceInput._recognition ? AiVoiceInput._owner : null;
            const previousId = AiVoiceInput._ownerId;

            AiVoiceInput.stopDictation(0);

            if (previous && (!ownerId || previousId !== ownerId)) {
                previous.invokeMethodAsync('OnDictationEnded').catch(() => { });
            }

            const current = new Recognition();
            const handle = AiVoiceInput._nextHandle++;

            AiVoiceInput._recognition = current;
            AiVoiceInput._handle = handle;
            AiVoiceInput._owner = dotnetObj;
            AiVoiceInput._ownerId = ownerId;

            current.lang = language || navigator.language || 'en-US';
            current.continuous = true;
            current.interimResults = true;

            current.onresult = (e: any) => {
                if (AiVoiceInput._recognition !== current) return;

                let final = '';
                let interim = '';
                for (let i = e.resultIndex; i < e.results.length; i++) {
                    const result = e.results[i];
                    if (result.isFinal) {
                        final += result[0].transcript;
                    } else {
                        interim += result[0].transcript;
                    }
                }

                dotnetObj.invokeMethodAsync('OnTranscriptReceived', final, interim).catch(() => { });
            };

            current.onerror = (e: any) => {
                if (AiVoiceInput._recognition !== current) return;

                dotnetObj.invokeMethodAsync('OnDictationError', String(e.error ?? 'unknown')).catch(() => { });
            };

            // A recognizer that was stopped or replaced reports its end late; only the live one may tell .NET that the
            // dictation is over, or a quick stop and restart would show the button as off.
            current.onend = () => {
                if (AiVoiceInput._recognition !== current) return;

                AiVoiceInput.clear();
                dotnetObj.invokeMethodAsync('OnDictationEnded').catch(() => { });
            };

            try {
                current.start();
            } catch {
                AiVoiceInput.clear();
                return 0;
            }

            return handle;
        }

        // Stops the dictation; with a handle, only the recognizer that handle stands for.
        public static stopDictation(handle: number) {
            const current = AiVoiceInput._recognition;
            if (!current) return;
            if (handle && handle !== AiVoiceInput._handle) return;

            AiVoiceInput.clear();

            try {
                current.stop();
            } catch {
                // already stopped
            }
        }

        private static clear() {
            AiVoiceInput._recognition = null;
            AiVoiceInput._handle = 0;
            AiVoiceInput._owner = null;
            AiVoiceInput._ownerId = null;
        }
    }
}

// bit-lazy: teardown AiVoiceInput.stopDictation
