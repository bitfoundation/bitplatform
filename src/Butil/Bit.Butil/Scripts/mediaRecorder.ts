var BitButil = BitButil || {};

(function (butil: any) {
    interface Recording {
        recorder: any;
        chunks: Blob[];
        // Resolved by the recorder's own 'stop' event. stop() is fire-and-forget in the DOM API -
        // the last dataavailable arrives *after* it returns - so .NET's Stop() has to await this
        // rather than read the chunk list straight away and lose the tail of the recording.
        stopped: Promise<void>;
        resolveStopped: () => void;
        objectUrl?: string;
    }

    const _recordings: { [id: string]: Recording } = {};

    butil.mediaRecorder = {
        isSupported() { return typeof (window as any).MediaRecorder === 'function'; },
        isTypeSupported(mimeType: string) {
            const R = (window as any).MediaRecorder;
            if (typeof R?.isTypeSupported !== 'function') return false;
            try { return !!R.isTypeSupported(mimeType); } catch { return false; }
        },
        supportedTypes(candidates: string[]) {
            const R = (window as any).MediaRecorder;
            if (typeof R?.isTypeSupported !== 'function') return [];
            return (candidates ?? []).filter(t => { try { return R.isTypeSupported(t); } catch { return false; } });
        },
        start(id: string, streamId: string, options: any, timeslice: number | null, dotNetRef: any, wantsChunks: boolean) {
            const R = (window as any).MediaRecorder;
            if (typeof R !== 'function') return false;

            const stream = butil.mediaDevices.getStream(streamId);
            if (!stream) return false;

            const init: any = {};
            if (options?.mimeType) init.mimeType = options.mimeType;
            if (options?.audioBitsPerSecond) init.audioBitsPerSecond = options.audioBitsPerSecond;
            if (options?.videoBitsPerSecond) init.videoBitsPerSecond = options.videoBitsPerSecond;

            let recorder: any;
            try {
                recorder = new R(stream, init);
            } catch {
                // Unsupported mimeType, or a stream with no recordable track.
                return false;
            }

            let resolveStopped: () => void = () => { };
            const stopped = new Promise<void>(resolve => { resolveStopped = resolve; });
            const entry: Recording = { recorder, chunks: [], stopped, resolveStopped };
            _recordings[id] = entry;

            recorder.addEventListener('dataavailable', (e: any) => {
                if (!e.data || e.data.size === 0) return;
                entry.chunks.push(e.data);
                if (!wantsChunks) return;
                // Streaming callers get each slice as it lands so they can upload while recording
                // instead of holding the whole take in memory.
                e.data.arrayBuffer()
                    .then((buf: ArrayBuffer) => butil.utils.dispatch(dotNetRef, 'InvokeRecorderData', id, new Uint8Array(buf)))
                    .catch(() => { /* the blob was already neutered - nothing useful to report */ });
            });

            recorder.addEventListener('error', (e: any) => {
                butil.utils.dispatch(dotNetRef, 'InvokeRecorderError', id, e?.error?.message ?? 'MediaRecorder error');
                // An error ends the recording, and no 'stop' is guaranteed - release anyone awaiting.
                entry.resolveStopped();
            });

            recorder.addEventListener('stop', () => entry.resolveStopped());

            try {
                // A 0 timeslice is not "no timeslice": it asks for the smallest slice the runtime
                // will emit, so only pass one through when the caller actually set it.
                if (timeslice !== null && timeslice !== undefined && timeslice > 0) recorder.start(timeslice);
                else recorder.start();
            } catch {
                delete _recordings[id];
                return false;
            }

            return true;
        },
        state(id: string) { return _recordings[id]?.recorder?.state ?? 'inactive'; },
        mimeType(id: string) { return _recordings[id]?.recorder?.mimeType ?? ''; },
        pause(id: string) {
            const r = _recordings[id]?.recorder;
            if (r?.state === 'recording') { try { r.pause(); } catch { /* raced with stop */ } }
        },
        resume(id: string) {
            const r = _recordings[id]?.recorder;
            if (r?.state === 'paused') { try { r.resume(); } catch { /* raced with stop */ } }
        },
        requestData(id: string) {
            const r = _recordings[id]?.recorder;
            if (r?.state === 'recording') { try { r.requestData(); } catch { /* raced with stop */ } }
        },
        async stop(id: string, asObjectUrl: boolean) {
            const entry = _recordings[id];
            if (!entry) return null;

            const recorder = entry.recorder;
            if (recorder.state !== 'inactive') {
                try { recorder.stop(); } catch { entry.resolveStopped(); }
                // Wait for the final dataavailable + stop pair, otherwise the tail is dropped.
                await entry.stopped;
            }

            delete _recordings[id];

            const type = recorder.mimeType || entry.chunks[0]?.type || '';
            const blob = new Blob(entry.chunks, type ? { type } : undefined);
            entry.chunks.length = 0;

            if (asObjectUrl) {
                // Handing back a blob: URL keeps a large take inside the browser - a <video src>
                // can play it without the bytes ever crossing the interop boundary.
                const url = URL.createObjectURL(blob);
                return { objectUrl: url, mimeType: blob.type, size: blob.size, data: null };
            }

            const buffer = await blob.arrayBuffer();
            return { objectUrl: null, mimeType: blob.type, size: blob.size, data: new Uint8Array(buffer) };
        },
        revoke(objectUrl: string) {
            if (objectUrl) { try { URL.revokeObjectURL(objectUrl); } catch { /* already revoked */ } }
        },
        cancel(id: string) {
            const entry = _recordings[id];
            if (!entry) return;
            delete _recordings[id];
            entry.resolveStopped();
            try { if (entry.recorder.state !== 'inactive') entry.recorder.stop(); } catch { /* already stopped */ }
            entry.chunks.length = 0;
        },
        disposeAll() {
            for (const id of Object.keys(_recordings)) butil.mediaRecorder.cancel(id);
        }
    };
}(BitButil));
