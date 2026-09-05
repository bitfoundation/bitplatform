var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _controllers: { [id: string]: AbortController } = {};

    butil.fetch = {
        send,
        start,
        abort,
        // Exported so streams.fromResponse builds its request through the same mapping instead of
        // keeping a second copy of it - the copy is what let the shared-signal handling drift.
        requestInit: buildInit
    };

    // The request always has its own controller (that is what abort(id) reaches). A shared signal
    // from butil.abortController has to be combined with it rather than replace it, so that either
    // one can abort the request.
    // Returns the signal to use and, where a listener had to be attached to the shared one, the way
    // to take it off again once the request has settled.
    function signalFor(req: any, controller: AbortController): { signal: AbortSignal; cleanup: () => void } {
        const nothingToUndo = () => { /* no listener was attached */ };

        const shared = req.signalId ? butil.abortController.signalOf(req.signalId) : undefined;
        if (!shared) return { signal: controller.signal, cleanup: nothingToUndo };

        const AS: any = (window as any).AbortSignal;
        if (typeof AS?.any === 'function') return { signal: AS.any([controller.signal, shared]), cleanup: nothingToUndo };

        // Pre-Safari-17.4: forward the shared signal into this request's own controller.
        if (shared.aborted) {
            controller.abort((shared as any).reason);
            return { signal: controller.signal, cleanup: nothingToUndo };
        }

        // A shared signal is meant to outlive the requests it guards, and until this listener comes
        // off again it holds this request's controller with it - one leak per request, for the life
        // of the signal.
        const forward = () => controller.abort((shared as any).reason);
        shared.addEventListener('abort', forward, { once: true });
        return { signal: controller.signal, cleanup: () => shared.removeEventListener('abort', forward) };
    }

    // An abort is not always an AbortError: a signal aborted with a reason rejects fetch with that
    // reason, and AbortSignal.timeout rejects with a TimeoutError. The signal is the only reliable
    // witness, so ask it rather than the exception it produced.
    function wasAborted(signal: AbortSignal | undefined, e: any): boolean {
        return signal?.aborted === true || e?.name === 'AbortError';
    }

    function buildInit(req: any, controller: AbortController): { init: RequestInit; cleanup: () => void } {
        const headers = new Headers();
        if (req.headers) {
            for (const k of Object.keys(req.headers)) headers.set(k, req.headers[k]);
        }
        const init: RequestInit = {
            method: req.method || 'GET',
            headers,
            credentials: req.credentials || 'same-origin',
            mode: req.mode || 'cors',
            cache: req.cache || 'default',
            redirect: req.redirect || 'follow'
        };
        if (req.body && req.body.length > 0) {
            init.body = butil.utils.arrayToBuffer(req.body);
        }
        // Last, because signalFor can register a listener on a shared signal and only the cleanup it
        // returns takes it off again: anything above throwing before that cleanup reaches the caller
        // would leak the listener, and this request's controller with it.
        const { signal, cleanup } = signalFor(req, controller);
        init.signal = signal;
        return { init, cleanup };
    }

    function headersToObject(h: Headers) {
        const out: any = {};
        h.forEach((v, k) => { out[k] = v; });
        return out;
    }

    async function send(id: string, req: any, dotNetRef: any, withProgress: boolean): Promise<any> {
        const controller = new AbortController();
        _controllers[id] = controller;

        // buildInit throws on its own (an invalid header name, most likely). It stays inside the
        // try so that failure comes back as the documented error result rather than as a
        // JSException, and so the finally still releases the controller.
        let cleanup = () => { /* nothing was built yet */ };
        let signal: AbortSignal | undefined;

        try {
            const built = buildInit(req, controller);
            cleanup = built.cleanup;
            signal = built.init.signal as AbortSignal;

            const resp = await fetch(req.url, built.init);
            const total = (() => {
                const cl = resp.headers.get('content-length');
                return cl ? Number(cl) : null;
            })();

            let bytes: Uint8Array;
            if (withProgress && resp.body && typeof (resp.body as any).getReader === 'function') {
                const reader = (resp.body as any).getReader();
                const chunks: Uint8Array[] = [];
                let loaded = 0;
                while (true) {
                    const { value, done } = await reader.read();
                    if (done) break;
                    chunks.push(value);
                    loaded += value.byteLength;
                    butil.utils.dispatch(dotNetRef, 'InvokeFetchProgress', id, { loaded, total });
                }
                bytes = new Uint8Array(loaded);
                let offset = 0;
                for (const c of chunks) { bytes.set(c, offset); offset += c.byteLength; }
            } else {
                const buf = await resp.arrayBuffer();
                bytes = new Uint8Array(buf);
                if (withProgress) {
                    butil.utils.dispatch(dotNetRef, 'InvokeFetchProgress', id, { loaded: bytes.byteLength, total });
                }
            }

            return {
                ok: resp.ok,
                status: resp.status,
                statusText: resp.statusText,
                url: resp.url,
                headers: headersToObject(resp.headers),
                body: bytes,
                aborted: false,
                error: null
            };
        } catch (e: any) {
            const aborted = wasAborted(signal, e);
            return {
                ok: false,
                status: 0,
                statusText: '',
                url: req.url,
                headers: {},
                body: new Uint8Array(),
                aborted,
                error: aborted ? null : (e?.message ?? String(e))
            };
        } finally {
            cleanup();
            delete _controllers[id];
        }
    }

    function start(id: string, req: any) {
        const controller = new AbortController();
        _controllers[id] = controller;

        // Fire-and-forget: errors are silently swallowed because there's no consumer for the
        // result - including the ones buildInit raises. Use send() when you need the response.
        try {
            const { init, cleanup } = buildInit(req, controller);
            fetch(req.url, init).catch(() => { /* ignore */ }).finally(() => { cleanup(); delete _controllers[id]; });
        } catch {
            delete _controllers[id];
        }
    }

    function abort(id: string) {
        const c = _controllers[id];
        if (!c) return;
        delete _controllers[id];
        try { c.abort(); } catch { /* already aborted */ }
    }
}(BitButil));
