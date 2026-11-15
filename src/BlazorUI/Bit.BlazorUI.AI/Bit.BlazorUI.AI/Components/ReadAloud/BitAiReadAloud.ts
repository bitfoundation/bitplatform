namespace BitBlazorUI {
    // The browser side of BitAiReadAloud: plays the speech the app synthesized for a reply, and stops it. One reading
    // plays at a time in a page - starting one stops the other, and tells the BitAiReadAloud that started it - and each
    // gets a handle of its own, so a late stop cannot silence a reading started since. Nothing plays while the microphone
    // captures (AiSpeech), and a capture starting silences what is playing: the recording would otherwise carry it.
    export class AiReadAloud {
        private static _audio: HTMLAudioElement | null = null;
        private static _handle = 0;
        private static _nextHandle = 1;
        private static _owner: DotNetObject | null = null;
        private static _objectUrl: string | null = null;
        private static _listening = false;
        private static _watching = false;

        // Primes the player inside every click of an element marked data-bit-ai-read-aloud (the Read aloud button of a
        // reply), in the browser and so inside the gesture itself: the press reaches .NET, which calls prime, only a round
        // trip later on Blazor Server, by when the browser no longer counts it as the user's.
        public static watch() {
            if (AiReadAloud._watching) return;

            AiReadAloud._watching = true;
            document.addEventListener('click', e => {
                const target = e.target as Element | null;
                if (target?.closest?.('[data-bit-ai-read-aloud]')) AiReadAloud.prime();
            }, true);
        }

        // Wakes the audio element up inside the press that asked for a reading. A browser plays sound only once the user
        // has interacted with the page, and mobile Safari only on an element first played inside a gesture - which the
        // speech, synthesized on a round trip after the press, no longer is. Played here once, the element plays the
        // reading when it arrives, and every reading after it ("keep reading") without a press of its own.
        public static prime() {
            const audio = AiReadAloud.element();
            if (AiReadAloud._handle || !audio.paused) return;

            try {
                audio.muted = true;
                audio.src = AiReadAloud.silence();
                const played = audio.play();
                played?.then(() => {
                    if (!AiReadAloud._handle) audio.pause();
                    audio.muted = false;
                }, () => {
                    audio.muted = false;
                });
            } catch {
                audio.muted = false;
            }
        }

        // Plays the audio at the address, or the bytes streamed from .NET, and resolves to the handle of the reading - or
        // to 0 when nothing plays (the microphone is capturing, or a newer reading or a stop came first) and -1 when the
        // browser refused to play it.
        public static async play(dotnetObj: DotNetObject, url: string | null, content: any, contentType: string | null, rate: number): Promise<number> {
            AiReadAloud.listen();

            if (AiSpeech.isCapturing()) return 0;

            AiReadAloud.stopCurrent(true);

            const handle = AiReadAloud._nextHandle++;
            AiReadAloud._handle = handle;
            AiReadAloud._owner = dotnetObj;

            let source = url;
            if (content) {
                let buffer: ArrayBuffer;
                try {
                    buffer = await content.arrayBuffer();
                } catch {
                    if (AiReadAloud._handle === handle) AiReadAloud.clear();
                    return -1;
                }

                // Overtaken by a newer reading, a stop or the microphone while the bytes arrived.
                if (AiReadAloud._handle !== handle || AiSpeech.isCapturing()) {
                    if (AiReadAloud._handle === handle) AiReadAloud.clear();
                    return 0;
                }

                AiReadAloud._objectUrl = URL.createObjectURL(new Blob([buffer], { type: contentType || 'audio/mpeg' }));
                source = AiReadAloud._objectUrl;
            }

            if (!source) {
                AiReadAloud.clear();
                return -1;
            }

            const audio = AiReadAloud.element();
            audio.muted = false;
            audio.src = source;
            audio.defaultPlaybackRate = rate > 0 ? rate : 1;
            audio.playbackRate = audio.defaultPlaybackRate;

            audio.onended = () => AiReadAloud.ended(handle);
            audio.onerror = () => AiReadAloud.ended(handle);

            try {
                await audio.play();
            } catch {
                // A stop, a newer reading or the microphone pausing it rejects play() too, and is no failure.
                if (AiReadAloud._handle !== handle) return 0;

                AiReadAloud.clear();
                return -1;
            }

            // Stopped while play() was settling.
            if (AiReadAloud._handle !== handle) return 0;

            return handle;
        }

        // Stops the reading; with a handle, only the one that handle stands for.
        public static stop(handle: number) {
            if (!AiReadAloud._handle) return;
            if (handle && handle !== AiReadAloud._handle) return;

            AiReadAloud.clear();
        }

        // Stops whatever plays, telling the BitAiReadAloud that started it that its reading is over.
        private static stopCurrent(notify: boolean) {
            const handle = AiReadAloud._handle;
            const owner = AiReadAloud._owner;
            if (!handle) return;

            AiReadAloud.clear();

            if (notify && owner) {
                owner.invokeMethodAsync('OnPlaybackEnded', handle).catch(() => { });
            }
        }

        private static ended(handle: number) {
            if (AiReadAloud._handle !== handle) return;

            AiReadAloud.stopCurrent(true);
        }

        // Silences a reading the moment the microphone opens, rather than when a reading next starts: the first capture
        // of the page may come long after this script loaded, and a reading may be mid-sentence when it does.
        private static listen() {
            if (AiReadAloud._listening) return;

            AiReadAloud._listening = true;
            AiSpeech.onCaptureStart(() => AiReadAloud.stopCurrent(true));
        }

        private static clear() {
            const audio = AiReadAloud._audio;
            if (audio) {
                audio.onended = null;
                audio.onerror = null;
                try {
                    audio.pause();
                } catch {
                    // already stopped
                }
                audio.removeAttribute('src');
                try {
                    audio.load();
                } catch {
                    // nothing to unload
                }
            }

            // The object url pins the audio in memory until it is revoked.
            if (AiReadAloud._objectUrl) {
                URL.revokeObjectURL(AiReadAloud._objectUrl);
                AiReadAloud._objectUrl = null;
            }

            AiReadAloud._handle = 0;
            AiReadAloud._owner = null;
        }

        private static element(): HTMLAudioElement {
            return AiReadAloud._audio ??= new Audio();
        }

        // A WAV of a few milliseconds of silence, which is all prime needs to play.
        private static silence(): string {
            const samples = 64;
            const view = new DataView(new ArrayBuffer(44 + samples * 2));
            const text = (offset: number, value: string) => {
                for (let i = 0; i < value.length; i++) view.setUint8(offset + i, value.charCodeAt(i));
            };

            text(0, 'RIFF');
            view.setUint32(4, 36 + samples * 2, true);
            text(8, 'WAVE');
            text(12, 'fmt ');
            view.setUint32(16, 16, true);
            view.setUint16(20, 1, true); // PCM
            view.setUint16(22, 1, true); // mono
            view.setUint32(24, 8000, true);
            view.setUint32(28, 16000, true);
            view.setUint16(32, 2, true);
            view.setUint16(34, 16, true);
            text(36, 'data');
            view.setUint32(40, samples * 2, true);

            let binary = '';
            const bytes = new Uint8Array(view.buffer);
            for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);

            return 'data:audio/wav;base64,' + btoa(binary);
        }
    }
}

// bit-lazy: teardown AiReadAloud.stop
