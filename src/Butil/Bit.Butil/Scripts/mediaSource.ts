var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface SourceEntry {
        ms: any;
        element: any;
        objectUrl: string | null;
        buffers: { [bufferId: string]: BufferEntry };
        listeners: { [listenerId: string]: (e: any) => void };
    }

    interface BufferEntry {
        sb: any;
        // Every append and every remove goes through this promise chain: a SourceBuffer throws
        // InvalidStateError when it is asked to do anything while `updating` is true, and .NET can
        // easily have two appends in flight (one per fetch that just completed).
        queue: Promise<any>;
    }

    const _sources: { [id: string]: SourceEntry } = {};

    butil.mediaSource = {
        isSupported() { return 'MediaSource' in window || 'ManagedMediaSource' in window; },
        isManagedSupported() { return 'ManagedMediaSource' in window; },
        isTypeSupported(mimeType: string) {
            const ctor: any = (window as any).MediaSource || (window as any).ManagedMediaSource;
            try { return !!ctor?.isTypeSupported(mimeType); } catch { return false; }
        },
        supportedTypes(candidates: string[]) {
            return (candidates ?? []).filter(t => butil.mediaSource.isTypeSupported(t));
        },
        open,
        addSourceBuffer,
        removeSourceBuffer,
        append,
        remove,
        abort,
        changeType,
        setMode,
        setTimestampOffset,
        setAppendWindow,
        buffered,
        isUpdating,
        readyState,
        duration,
        setDuration,
        endOfStream,
        setLiveSeekableRange,
        clearLiveSeekableRange,
        subscribe,
        unsubscribe,
        close,
        disposeAll
    };

    // Resolves once the element has actually adopted the MediaSource ('sourceopen'), because every
    // other call - addSourceBuffer above all - is an InvalidStateError until then. Handing .NET a
    // handle only at that point removes a whole category of ordering bugs from the caller's side.
    function open(id: string, element: any, managed: boolean) {
        const Managed: any = (window as any).ManagedMediaSource;
        const Plain: any = (window as any).MediaSource;
        const Ctor = (managed && Managed) ? Managed : (Plain || Managed);
        if (!Ctor || !element) return Promise.resolve(false);

        close(id);

        const ms = new Ctor();
        const entry: SourceEntry = { ms, element, objectUrl: null, buffers: {}, listeners: {} };
        _sources[id] = entry;

        return new Promise<boolean>(resolve => {
            let settled = false;
            const done = (ok: boolean) => { if (!settled) { settled = true; resolve(ok); } };

            ms.addEventListener('sourceopen', () => done(true), { once: true });

            try {
                // A ManagedMediaSource must be attached through srcObject - that attachment is what
                // lets the engine tell the page when to stop buffering. A classic MediaSource only
                // understands an object URL.
                if (managed && Managed && ms instanceof Managed && 'srcObject' in element) {
                    element.disableRemotePlayback = true;
                    element.srcObject = ms;
                } else {
                    entry.objectUrl = URL.createObjectURL(ms);
                    element.src = entry.objectUrl;
                }
            } catch {
                done(false);
                return;
            }

            // An element that never loads (preload="none" and no play, or a detached element) never
            // fires sourceopen, and a caller left awaiting forever is worse than one told it failed.
            setTimeout(() => {
                if (settled) return;
                close(id);
                done(false);
            }, 10000);
        });
    }

    function addSourceBuffer(id: string, bufferId: string, mimeType: string) {
        const entry = _sources[id];
        if (!entry || entry.ms.readyState !== 'open') return false;
        try {
            const sb = entry.ms.addSourceBuffer(mimeType);
            entry.buffers[bufferId] = { sb, queue: Promise.resolve() };
            return true;
        } catch {
            // Unsupported type, too many buffers, or the source closed between the check and here.
            return false;
        }
    }

    function removeSourceBuffer(id: string, bufferId: string) {
        const entry = _sources[id];
        const buffer = entry?.buffers[bufferId];
        if (!entry || !buffer) return;
        delete entry.buffers[bufferId];
        try { entry.ms.removeSourceBuffer(buffer.sb); } catch { /* source already closed */ }
    }

    function append(id: string, bufferId: string, data: Uint8Array) {
        return enqueue(id, bufferId, buffer => new Promise<string>(resolve => {
            const sb = buffer.sb;
            let failed = false;
            const finish = (status: string) => {
                sb.removeEventListener('updateend', onEnd);
                sb.removeEventListener('error', onError);
                sb.removeEventListener('abort', onAbort);
                resolve(status);
            };
            // 'error' fires before 'updateend', so this flag is what distinguishes a rejected segment
            // from an accepted one - a SourceBuffer exposes no error property to read afterwards.
            const onError = () => { failed = true; };
            const onAbort = () => finish('aborted');
            const onEnd = () => finish(failed ? 'failed' : 'success');
            sb.addEventListener('updateend', onEnd);
            sb.addEventListener('error', onError);
            sb.addEventListener('abort', onAbort);

            try {
                sb.appendBuffer(butil.utils.arrayToBuffer(data));
            } catch (e: any) {
                // QuotaExceededError is the one a player has to handle rather than log: the buffer is
                // full, and the fix is to remove already-played ranges and append the segment again.
                finish(e?.name === 'QuotaExceededError' ? 'quota-exceeded'
                    : e?.name === 'InvalidStateError' ? 'closed'
                        : 'failed');
            }
        }), 'closed');
    }

    function remove(id: string, bufferId: string, start: number, end: number) {
        return enqueue(id, bufferId, buffer => new Promise<string>(resolve => {
            const sb = buffer.sb;
            const finish = (status: string) => {
                sb.removeEventListener('updateend', onEnd);
                resolve(status);
            };
            const onEnd = () => finish('success');
            sb.addEventListener('updateend', onEnd);
            try { sb.remove(start, end); } catch { finish('failed'); }
        }), 'closed');
    }

    // Chains work onto the buffer's queue so only one operation is ever outstanding, and so a
    // rejected step cannot poison the chain for the ones queued behind it.
    function enqueue(id: string, bufferId: string, work: (buffer: BufferEntry) => Promise<string>, missing: string) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return Promise.resolve(missing);

        const run = buffer.queue.then(() => work(buffer), () => work(buffer));
        buffer.queue = run.catch(() => undefined);
        return run;
    }

    function abort(id: string, bufferId: string) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return;
        try { buffer.sb.abort(); } catch { /* not updating, or the source is no longer open */ }
    }

    function changeType(id: string, bufferId: string, mimeType: string) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer?.sb.changeType) return false;
        try { buffer.sb.changeType(mimeType); return true; } catch { return false; }
    }

    function setMode(id: string, bufferId: string, mode: string) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return false;
        try { buffer.sb.mode = mode; return true; } catch { return false; }
    }

    function setTimestampOffset(id: string, bufferId: string, offset: number) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return false;
        try { buffer.sb.timestampOffset = offset; return true; } catch { return false; }
    }

    function setAppendWindow(id: string, bufferId: string, start: number, end: number) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return false;
        try {
            // Order matters: a start that is not below the current end is rejected, so the window is
            // widened at the end first and only then moved at the start.
            buffer.sb.appendWindowEnd = end;
            buffer.sb.appendWindowStart = start;
            return true;
        } catch {
            return false;
        }
    }

    function buffered(id: string, bufferId: string) {
        const buffer = _sources[id]?.buffers[bufferId];
        if (!buffer) return [];
        return toRanges(buffer.sb.buffered);
    }

    function isUpdating(id: string, bufferId: string) {
        return !!_sources[id]?.buffers[bufferId]?.sb.updating;
    }

    function readyState(id: string) {
        return _sources[id]?.ms.readyState ?? 'closed';
    }

    function duration(id: string) {
        const value = _sources[id]?.ms.duration;
        // A MediaSource with no duration set yet reports NaN, which does not survive JSON.
        return (typeof value === 'number' && isFinite(value)) ? value : null;
    }

    function setDuration(id: string, value: number) {
        const entry = _sources[id];
        if (!entry) return false;
        try { entry.ms.duration = value; return true; } catch { return false; }
    }

    function endOfStream(id: string, error: string | null) {
        const entry = _sources[id];
        if (!entry || entry.ms.readyState !== 'open') return false;
        try {
            if (error) entry.ms.endOfStream(error); else entry.ms.endOfStream();
            return true;
        } catch {
            // A buffer is still updating, or the stream already ended.
            return false;
        }
    }

    function setLiveSeekableRange(id: string, start: number, end: number) {
        const entry = _sources[id];
        if (!entry?.ms.setLiveSeekableRange) return false;
        try { entry.ms.setLiveSeekableRange(start, end); return true; } catch { return false; }
    }

    function clearLiveSeekableRange(id: string) {
        const entry = _sources[id];
        if (!entry?.ms.clearLiveSeekableRange) return false;
        try { entry.ms.clearLiveSeekableRange(); return true; } catch { return false; }
    }

    function subscribe(id: string, dotNetRef: any, listenerId: string, method: string) {
        const entry = _sources[id];
        if (!entry) return;
        const handler = () => butil.utils.dispatch(dotNetRef, method, listenerId, entry.ms.readyState);
        entry.listeners[listenerId] = handler;
        entry.ms.addEventListener('sourceopen', handler);
        entry.ms.addEventListener('sourceended', handler);
        entry.ms.addEventListener('sourceclose', handler);
    }

    function unsubscribe(id: string, listenerId: string) {
        const entry = _sources[id];
        const handler = entry?.listeners[listenerId];
        if (!entry || !handler) return;
        delete entry.listeners[listenerId];
        entry.ms.removeEventListener('sourceopen', handler);
        entry.ms.removeEventListener('sourceended', handler);
        entry.ms.removeEventListener('sourceclose', handler);
    }

    function close(id: string) {
        const entry = _sources[id];
        if (!entry) return;
        delete _sources[id];

        for (const listenerId of Object.keys(entry.listeners)) {
            const handler = entry.listeners[listenerId];
            entry.ms.removeEventListener('sourceopen', handler);
            entry.ms.removeEventListener('sourceended', handler);
            entry.ms.removeEventListener('sourceclose', handler);
        }

        try { if (entry.ms.readyState === 'open') entry.ms.endOfStream(); } catch { /* still updating */ }

        // The object URL keeps the MediaSource alive for as long as it exists, so revoking it is
        // what actually lets the buffered media be freed.
        if (entry.objectUrl) {
            try { URL.revokeObjectURL(entry.objectUrl); } catch { /* already revoked */ }
        }
        try {
            if (entry.element?.srcObject === entry.ms) entry.element.srcObject = null;
            else if (entry.objectUrl && entry.element?.src === entry.objectUrl) {
                entry.element.removeAttribute('src');
                entry.element.load?.();
            }
        } catch { /* the element is already gone */ }
    }

    function disposeAll() {
        for (const id of Object.keys(_sources)) close(id);
    }

    function toRanges(ranges: any) {
        const result: { start: number, end: number }[] = [];
        if (!ranges) return result;
        for (let i = 0; i < ranges.length; i++) {
            result.push({ start: ranges.start(i), end: ranges.end(i) });
        }
        return result;
    }
}(BitButil));
