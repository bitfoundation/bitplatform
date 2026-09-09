var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // How a .NET FetchRequest becomes a RequestInit. Its own module because streams.fromResponse
    // needs exactly this and nothing else of the fetch module - one mapping of the request shape,
    // shared, rather than the copy that let the shared-signal handling drift, and without the
    // response reading, the progress reporting and the upload streaming coming along with it.
    butil.fetchRequest = {
        buildInit,
        wasAborted
    };

    // The request always has its own controller (that is what abort(id) reaches). A shared signal
    // from the abortSignals registry has to be combined with it rather than replace it, so that either
    // one can abort the request.
    // Returns the signal to use and, where a listener had to be attached to the shared one, the way
    // to take it off again once the request has settled.
    function signalFor(req: any, controller: AbortController): { signal: AbortSignal; cleanup: () => void } {
        const nothingToUndo = () => { /* no listener was attached */ };

        const shared = req.signalId ? butil.abortSignals.signalOf(req.signalId) : undefined;
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
        // Headers cross as [name, value] pairs so a repeated name survives; a plain object is
        // accepted too, for a caller that hand-built the payload.
        if (Array.isArray(req.headers)) {
            for (const pair of req.headers) headers.append(pair[0], pair[1]);
        } else if (req.headers) {
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

        // Every one of these is absent-means-default: sending an explicit null would be a TypeError
        // where leaving the member off is simply the browser's own default.
        if (req.referrer !== null && req.referrer !== undefined) init.referrer = req.referrer;
        if (req.referrerPolicy) init.referrerPolicy = req.referrerPolicy;
        if (req.integrity) init.integrity = req.integrity;
        if (req.keepAlive) init.keepalive = true;
        if (req.priority) (init as any).priority = req.priority;

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
}(BitButil));
