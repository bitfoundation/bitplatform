namespace BitBlazorUI {
    // The recording half of BitAiVoiceInput: the microphone recorded with MediaRecorder, and the take handed to .NET as a
    // stream once it ends, for the app's own transcriber - which is what makes dictation work where the browser has no
    // speech API (Firefox, Android WebView). One recording runs at a time in a page, and it shares the microphone with the
    // speech API's dictation through AiSpeech: starting either ends the other.
    //
    // A take ends when its component stops it, when it reaches its maximum length, when the user has been quiet for the
    // silence timeout (if one is set) after saying something, or when another capture starts. Every end but a discard
    // hands the take over (OnRecordingCompleted), so nothing said is lost to a timeout; a take discarded - its component
    // went away - is not sent anywhere.
    export class AiVoiceRecorder {
        private static _current: AiVoiceRecording | null = null;
        private static _nextHandle = 1;

        // The handle of a start still waiting on the microphone (the permission prompt of the browser), which a stop of
        // that handle cancels before anything is recorded.
        private static _pending = 0;

        // The containers asked for, best first: what each engine records differs (Chromium and Firefox webm, Safari mp4),
        // and a transcriber reads the format off the bytes, so the first one the browser supports wins.
        private static readonly TYPES = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4', 'audio/ogg;codecs=opus', 'audio/ogg'];

        // How loud a frame has to be (its root mean square, 0 to 1) to count as speech for the silence timeout.
        private static readonly SPEECH_LEVEL = 0.02;

        // Whether this browser can record the microphone.
        public static isSupported(): boolean {
            const devices = navigator.mediaDevices;

            return !!devices && typeof devices.getUserMedia === 'function' && typeof (window as any).MediaRecorder === 'function';
        }

        // Starts recording and resolves to its handle - or to 0 when the start was cancelled while the browser asked for
        // the microphone, -1 when the microphone was refused, -2 when there is none to open and -3 when the browser cannot
        // record it. The handle stands for this take only, so a component disposing late cannot stop the take another one
        // has started since.
        public static async start(dotnetObj: DotNetObject, maxDuration: number, silenceTimeout: number): Promise<number> {
            if (!AiVoiceRecorder.isSupported()) return -3;

            const handle = AiVoiceRecorder._nextHandle++;
            AiVoiceRecorder._pending = handle;

            let stream: MediaStream;
            try {
                stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            } catch (e: any) {
                if (AiVoiceRecorder._pending === handle) AiVoiceRecorder._pending = 0;

                const name = e?.name;
                return name === 'NotAllowedError' || name === 'SecurityError' ? -1 : -2;
            }

            // Stopped, or overtaken by another start, while the browser asked.
            if (AiVoiceRecorder._pending !== handle) {
                AiVoiceRecorder.release(stream);
                return 0;
            }

            AiVoiceRecorder._pending = 0;

            const type = AiVoiceRecorder.pickType();
            let recorder: MediaRecorder;
            try {
                recorder = type ? new MediaRecorder(stream, { mimeType: type }) : new MediaRecorder(stream);
            } catch {
                AiVoiceRecorder.release(stream);
                return -3;
            }

            const recording: AiVoiceRecording = {
                handle,
                dotnetObj,
                stream,
                recorder,
                type,
                chunks: [],
                keep: true,
                ended: false,
                timer: 0,
                silenceTimer: 0,
                audioContext: null,
            };

            recorder.ondataavailable = (e: BlobEvent) => {
                if (e.data && e.data.size > 0) recording.chunks.push(e.data);
            };
            recorder.onstop = () => AiVoiceRecorder.finish(recording);
            recorder.onerror = () => {
                if (recording.ended) return;

                AiVoiceRecorder.end(recording, false);
                dotnetObj.invokeMethodAsync('OnDictationError', 'recording-failed').catch(() => { });
            };

            // A microphone unplugged, or taken by the system, ends the take with what it has.
            stream.getAudioTracks().forEach(t => t.addEventListener('ended', () => AiVoiceRecorder.end(recording, true)));

            try {
                recorder.start();
            } catch {
                AiVoiceRecorder.release(stream);
                return -3;
            }

            AiVoiceRecorder._current = recording;

            // The microphone is the page's one: a dictation through the speech API stops as this starts, and a reply read
            // aloud falls silent. Another capture starting ends this take, which is still handed over.
            AiSpeech.beginCapture(recording, () => AiVoiceRecorder.end(recording, true));

            if (maxDuration > 0) {
                recording.timer = window.setTimeout(() => AiVoiceRecorder.end(recording, true), maxDuration);
            }

            if (silenceTimeout > 0) {
                AiVoiceRecorder.watchSilence(recording, silenceTimeout);
            }

            return handle;
        }

        // Ends the take the handle stands for: kept, it is handed to its component; otherwise it is thrown away. A start
        // of that handle still waiting on the microphone is cancelled.
        public static stop(handle: number, keep: boolean) {
            if (!handle) return;

            if (AiVoiceRecorder._pending === handle) {
                AiVoiceRecorder._pending = 0;
                return;
            }

            const recording = AiVoiceRecorder._current;
            if (!recording || recording.handle !== handle) return;

            AiVoiceRecorder.end(recording, keep);
        }

        private static end(recording: AiVoiceRecording, keep: boolean) {
            if (recording.ended) return;

            recording.ended = true;
            recording.keep = keep;

            clearTimeout(recording.timer);
            clearInterval(recording.silenceTimer);
            recording.audioContext?.close().catch(() => { });
            recording.audioContext = null;

            AiSpeech.endCapture(recording);

            if (AiVoiceRecorder._current === recording) {
                AiVoiceRecorder._current = null;
            }

            // Stopping is what flushes the last of the audio; finish runs on the recorder's stop event once it has.
            try {
                if (recording.recorder.state !== 'inactive') {
                    recording.recorder.stop();
                    return;
                }
            } catch {
                // already stopped, so no stop event is coming
            }

            AiVoiceRecorder.finish(recording);
        }

        private static finish(recording: AiVoiceRecording) {
            // The microphone is a lifetime of its own: a stream left open keeps the recording indicator of the browser lit
            // over a button that stopped listening.
            AiVoiceRecorder.release(recording.stream);

            // A recorder that stopped by itself (the browser's limit, a failure) ends the take like any other end.
            if (!recording.ended) {
                recording.ended = true;
                AiSpeech.endCapture(recording);
                if (AiVoiceRecorder._current === recording) AiVoiceRecorder._current = null;
                clearTimeout(recording.timer);
                clearInterval(recording.silenceTimer);
            }

            if (!recording.keep) return;
            recording.keep = false; // handed over once, whatever raises finish again

            // The type the recorder actually used, with its parameters (codecs) left for .NET to drop.
            const type = recording.recorder.mimeType || recording.type || 'audio/webm';
            const blob = new Blob(recording.chunks, { type });
            recording.chunks = [];

            // The take goes as a stream rather than inline: the bytes in the interop message itself would exceed the
            // SignalR limit of Blazor Server (32 KB by default), while a stream reference is read in chunks on Server and
            // WebAssembly alike.
            const content = blob.size > 0 ? (window as any).DotNet.createJSStreamReference(blob) : null;

            recording.dotnetObj.invokeMethodAsync('OnRecordingCompleted', content, type, blob.size).catch(() => { });
        }

        // Ends the take once the user has said something and then been quiet for the timeout: a frame of the microphone is
        // read a few times a second, and nothing is stopped before the first one loud enough to be speech.
        private static watchSilence(recording: AiVoiceRecording, timeout: number) {
            const w = window as any;
            const Context = w.AudioContext || w.webkitAudioContext;
            if (!Context) return;

            try {
                const context: AudioContext = new Context();
                const analyser = context.createAnalyser();
                analyser.fftSize = 2048;
                context.createMediaStreamSource(recording.stream).connect(analyser);

                // A context created outside a gesture of the user - after the await on the microphone, or a round trip
                // after the press on Blazor Server - starts suspended, and a suspended one reads nothing but silence. The
                // capture of the microphone is what lets the browser run it (Safari lifts its autoplay rule for a page
                // capturing), so it is resumed now, and again on a tick that finds it suspended still.
                const resume = () => {
                    if (context.state === 'suspended') context.resume().catch(() => { });
                };
                resume();

                const frame = new Uint8Array(analyser.fftSize);
                let heard = false;
                let quietSince = performance.now();

                recording.audioContext = context;
                recording.silenceTimer = window.setInterval(() => {
                    // Nothing is heard while it is not running, so the quiet is not counted either.
                    if (context.state !== 'running') {
                        resume();
                        quietSince = performance.now();
                        return;
                    }

                    analyser.getByteTimeDomainData(frame);

                    let sum = 0;
                    for (let i = 0; i < frame.length; i++) {
                        const v = (frame[i] - 128) / 128;
                        sum += v * v;
                    }

                    const now = performance.now();
                    if (Math.sqrt(sum / frame.length) > AiVoiceRecorder.SPEECH_LEVEL) {
                        heard = true;
                        quietSince = now;
                    } else if (heard && now - quietSince >= timeout) {
                        AiVoiceRecorder.end(recording, true);
                    }
                }, 100);
            } catch {
                // no silence timeout where the audio cannot be analysed; the maximum length still ends the take
            }
        }

        private static pickType(): string | null {
            const Recorder = (window as any).MediaRecorder;
            if (typeof Recorder?.isTypeSupported !== 'function') return null;

            return AiVoiceRecorder.TYPES.find(t => {
                try {
                    return Recorder.isTypeSupported(t);
                } catch {
                    return false;
                }
            }) ?? null;
        }

        private static release(stream: MediaStream) {
            try {
                stream.getTracks().forEach(t => t.stop());
            } catch {
                // already released
            }
        }
    }

    interface AiVoiceRecording {
        handle: number;
        dotnetObj: DotNetObject;
        stream: MediaStream;
        recorder: MediaRecorder;
        type: string | null;
        chunks: Blob[];
        keep: boolean;
        ended: boolean;
        timer: number;
        silenceTimer: number;
        audioContext: AudioContext | null;
    }
}

// bit-lazy: teardown AiVoiceRecorder.stop
