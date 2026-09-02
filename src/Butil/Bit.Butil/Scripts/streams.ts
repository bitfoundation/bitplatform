var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface ReadableEntry { stream: ReadableStream; reader?: ReadableStreamDefaultReader; }
    interface WritableEntry { stream: WritableStream; writer?: WritableStreamDefaultWriter; }

    const _readables: { [id: string]: ReadableEntry } = {};
    const _writables: { [id: string]: WritableEntry } = {};
    // transform id -> the ids its two ends are registered under, so releasing the transform
    // releases both.
    const _transforms: { [id: string]: { readableId: string; writableId: string } } = {};

    function trackReadable(id: string, stream: ReadableStream) { _readables[id] = { stream }; }
    function trackWritable(id: string, stream: WritableStream) { _writables[id] = { stream }; }

    butil.streams = {
        isSupported() { return typeof (window as any).ReadableStream === 'function'; },
        isTransformSupported() { return typeof (window as any).CompressionStream === 'function'; },

        // The one source: a fetch response body, read as it arrives instead of buffered whole.
        async fromResponse(id: string, url: string, req: any) {
            if (typeof (window as any).ReadableStream !== 'function') return null;

            const headers = new Headers();
            if (req?.headers) for (const key of Object.keys(req.headers)) headers.set(key, req.headers[key]);

            const init: RequestInit = {
                method: req?.method || 'GET',
                headers,
                credentials: req?.credentials || 'same-origin',
                mode: req?.mode || 'cors',
                cache: req?.cache || 'default',
                redirect: req?.redirect || 'follow'
            };
            if (req?.body && req.body.length > 0) init.body = butil.utils.arrayToBuffer(req.body);

            // A shared AbortSignal composes with this request the same way it does for fetch().
            const shared = req?.signalId ? butil.abortController.signalOf(req.signalId) : undefined;
            if (shared) init.signal = shared;

            let response: Response;
            try { response = await fetch(url, init); }
            catch (e: any) { return { ok: false, status: 0, error: e?.message ?? String(e) }; }

            // A 204, a HEAD, or an opaque no-cors response has no body at all - which is not an
            // error, but there is no stream to hand back either.
            if (!response.body) return { ok: false, status: response.status, error: 'The response has no body.' };

            trackReadable(id, response.body);
            const length = response.headers.get('content-length');

            return {
                ok: true,
                status: response.status,
                statusText: response.statusText,
                url: response.url,
                totalBytes: length ? Number(length) : null,
                error: null
            };
        },

        // Sink into .NET. write() returns the dispatch promise, so the stream will not pull the
        // next chunk until .NET has taken this one - which is what makes back-pressure reach across
        // the interop boundary rather than stopping at it.
        createWritable(dotNetRef: any, id: string, highWaterMark: number) {
            if (typeof (window as any).WritableStream !== 'function') return false;

            const stream = new WritableStream({
                write(chunk: any) {
                    const bytes = chunk instanceof Uint8Array ? chunk : new Uint8Array(chunk);
                    return butil.utils.dispatch(dotNetRef, 'InvokeSinkChunk', id, bytes);
                },
                close() { return butil.utils.dispatch(dotNetRef, 'InvokeSinkClose', id, null); },
                abort(reason: any) {
                    return butil.utils.dispatch(dotNetRef, 'InvokeSinkClose', id,
                        reason instanceof Error ? reason.message : String(reason ?? 'aborted'));
                }
            }, new CountQueuingStrategy({ highWaterMark: highWaterMark > 0 ? highWaterMark : 1 }));

            trackWritable(id, stream);
            return true;
        },

        createCompression(id: string, readableId: string, writableId: string, format: string, decompress: boolean) {
            const Ctor = (window as any)[decompress ? 'DecompressionStream' : 'CompressionStream'];
            if (typeof Ctor !== 'function') return false;

            let transform: TransformStream;
            try { transform = new Ctor(format); } catch { return false; } // unknown format

            trackReadable(readableId, transform.readable);
            trackWritable(writableId, transform.writable);
            _transforms[id] = { readableId, writableId };
            return true;
        },

        // Pull one chunk. The reader is acquired on first use, which is also what locks the stream -
        // tee() and pipeThrough() are unavailable from then on, by the specification's rules rather
        // than ours.
        async read(id: string) {
            const entry = _readables[id];
            if (!entry) return { done: true, data: null, error: 'unknown stream' };

            try {
                entry.reader = entry.reader ?? entry.stream.getReader();
                const { value, done } = await entry.reader.read();
                if (done) return { done: true, data: null, error: null };
                return { done: false, data: value instanceof Uint8Array ? value : new Uint8Array(value), error: null };
            } catch (e: any) {
                return { done: true, data: null, error: e?.message ?? String(e) };
            }
        },

        // Two streams from one, each getting every chunk. The original is locked afterwards and is
        // no longer readable itself - which is the point: it has been split, not copied.
        tee(id: string, firstId: string, secondId: string) {
            const entry = _readables[id];
            if (!entry || entry.reader) return false;   // already locked by a read()

            try {
                const [first, second] = entry.stream.tee();
                trackReadable(firstId, first);
                trackReadable(secondId, second);
                delete _readables[id];                  // the original is spent
                return true;
            } catch { return false; }
        },

        pipeThrough(id: string, transformId: string, resultId: string) {
            const entry = _readables[id];
            const transform = _transforms[transformId];
            if (!entry || entry.reader || !transform) return false;

            const writable = _writables[transform.writableId]?.stream;
            const readable = _readables[transform.readableId]?.stream;
            if (!writable || !readable) return false;

            try {
                const piped = entry.stream.pipeThrough({ writable, readable });
                trackReadable(resultId, piped);
                delete _readables[id];
                return true;
            } catch { return false; }
        },

        // Runs the whole pipe and settles when it is done. The promise is the completion signal:
        // everything a pipe does - back-pressure, closing the destination, propagating an error
        // from either end - happens inside it.
        async pipeTo(id: string, writableId: string, preventClose: boolean) {
            const entry = _readables[id];
            const destination = _writables[writableId];
            if (!entry || entry.reader || !destination) return 'unknown or locked stream';

            try {
                await entry.stream.pipeTo(destination.stream, { preventClose });
                delete _readables[id];
                return null;
            } catch (e: any) {
                return e?.message ?? String(e);
            }
        },

        locked(id: string) {
            const entry = _readables[id];
            if (entry) return entry.stream.locked;
            return _writables[id]?.stream.locked ?? false;
        },

        async cancel(id: string, reason: string | null) {
            const entry = _readables[id];
            if (!entry) return;
            delete _readables[id];
            try {
                // A reader has to be cancelled through itself; cancelling the stream under a live
                // reader throws for being locked.
                if (entry.reader) await entry.reader.cancel(reason ?? undefined);
                else await entry.stream.cancel(reason ?? undefined);
            } catch { /* already closed or errored */ }
        },

        async write(id: string, bytes: Uint8Array) {
            const entry = _writables[id];
            if (!entry) return false;
            try {
                entry.writer = entry.writer ?? entry.stream.getWriter();
                // Awaiting ready before writing is what makes back-pressure real for a caller that
                // writes in a loop; without it every write is accepted and queued.
                await entry.writer.ready;
                await entry.writer.write(butil.utils.arrayToBuffer(bytes));
                return true;
            } catch { return false; }
        },

        async closeWritable(id: string) {
            const entry = _writables[id];
            if (!entry) return false;
            try {
                if (entry.writer) await entry.writer.close();
                else await entry.stream.close();
                delete _writables[id];
                return true;
            } catch { return false; }
        },

        async abortWritable(id: string, reason: string | null) {
            const entry = _writables[id];
            if (!entry) return false;
            try {
                if (entry.writer) await entry.writer.abort(reason ?? undefined);
                else await entry.stream.abort(reason ?? undefined);
                delete _writables[id];
                return true;
            } catch { return false; }
        },

        // Drops registry entries without cancelling: a stream handed to a pipe is no longer this
        // registry's business, and cancelling it here would break the pipe.
        release(id: string) {
            delete _readables[id];
            delete _writables[id];
            const transform = _transforms[id];
            if (transform) {
                delete _readables[transform.readableId];
                delete _writables[transform.writableId];
                delete _transforms[id];
            }
        },

        disposeAll() {
            for (const id of Object.keys(_readables)) butil.streams.cancel(id, 'disposed');
            for (const id of Object.keys(_writables)) butil.streams.abortWritable(id, 'disposed');
            for (const id of Object.keys(_transforms)) delete _transforms[id];
        }
    };
}(BitButil));
