namespace BitBlazorUI {
    // What the speech scripts of this package share: which captures of the microphone are live. Dictation through the
    // speech API of the browser (AiVoiceInput) and through a recording the app transcribes (AiVoiceRecorder) are one
    // microphone, so one capture runs at a time in a page - starting one ends the other, through the end it registered -
    // and a reply read aloud (AiReadAloud) gives way the moment one starts, or the recording would carry the reading.
    export class AiSpeech {
        // The live captures, each with what ends it when another one starts.
        private static _captures = new Map<object, () => void>();

        // What wants to hear that a capture has started.
        private static _captureListeners = new Set<() => void>();

        // Whether the microphone is capturing, so nothing starts playing into it.
        public static isCapturing(): boolean {
            return AiSpeech._captures.size > 0;
        }

        // Registers a capture that has started, ending every other one first, and tells the listeners.
        public static beginCapture(capture: object, end: () => void) {
            for (const [other, endOther] of Array.from(AiSpeech._captures)) {
                if (other === capture) continue;

                AiSpeech._captures.delete(other);

                try {
                    endOther();
                } catch {
                    // a capture failing to end is no reason to keep the next one from starting
                }
            }

            AiSpeech._captures.set(capture, end);

            for (const listener of Array.from(AiSpeech._captureListeners)) {
                try {
                    listener();
                } catch {
                    // one listener failing is no reason to keep the others from hearing it
                }
            }
        }

        // Unregisters a capture that has ended, however it ended.
        public static endCapture(capture: object) {
            AiSpeech._captures.delete(capture);
        }

        public static onCaptureStart(listener: () => void) {
            AiSpeech._captureListeners.add(listener);
        }
    }
}
