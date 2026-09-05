var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _controllers: { [id: string]: AbortController } = {};

    // Aborts that arrived before the request they name existed here. .NET registers the cancellation
    // callback before it posts the call, so a token that fires in between reaches abort() while
    // _controllers still has nothing under that id - and without this the abort would be dropped and
    // the request would run, and upload its whole body, uncancelled.
    const _pendingAborts: { [id: string]: boolean } = {};

    // How long a pending abort is kept. An abort naming an id that never starts - the token firing
    // just after the request completed and deleted its controller - would otherwise sit here for the
    // life of the document; the id is single-use, so forgetting it after a while loses nothing.
    const PENDING_ABORT_TTL = 30000;

    // The controller for one request, already aborted if the abort beat it here.
    function openController(id: string) {
        const controller = new AbortController();
        _controllers[id] = controller;
        if (_pendingAborts[id]) {
            delete _pendingAborts[id];
            try { controller.abort(); } catch { /* nothing to abort yet */ }
        }
        return controller;
    }

    butil.fetch = {
        send,
        sendStream,
        start,
        abort,
        // Streaming an upload needs a whole-stream request body, which only lands on HTTP/2 or
        // HTTP/3 over a secure context, and only in engines that accept the duplex option. The
        // feature test is the request constructor itself: engines without it throw here.
        supportsStreamingUpload
    };

    function buildInit(req: any, controller: AbortController): RequestInit {
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
            redirect: req.redirect || 'follow',
            signal: controller.signal
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
        return init;
    }

    function headersToArray(h: Headers) {
        const out: [string, string][] = [];
        // getSetCookie() is the only way to see repeated Set-Cookie headers; forEach folds them
        // into one comma-joined value, which is invalid for cookies.
        const setCookies = typeof (h as any).getSetCookie === 'function' ? (h as any).getSetCookie() : null;
        h.forEach((v, k) => {
            if (setCookies && k.toLowerCase() === 'set-cookie') return;
            out.push([k, v]);
        });
        if (setCookies) {
            for (const cookie of setCookies) out.push(['set-cookie', cookie]);
        }
        return out;
    }

    function toResponse(resp: Response, bytes: Uint8Array) {
        return {
            ok: resp.ok,
            status: resp.status,
            statusText: resp.statusText,
            url: resp.url,
            headers: headersToArray(resp.headers),
            body: bytes,
            redirected: resp.redirected,
            type: resp.type,
            aborted: false,
            error: null
        };
    }

    function toErrorResponse(url: string, e: any) {
        const aborted = e?.name === 'AbortError';
        return {
            ok: false,
            status: 0,
            statusText: '',
            url,
            headers: [],
            body: new Uint8Array(),
            redirected: false,
            type: 'error',
            aborted,
            error: aborted ? null : (e?.message ?? String(e))
        };
    }

    function contentLengthOf(resp: Response) {
        const cl = resp.headers.get('content-length');
        return cl ? Number(cl) : null;
    }

    async function readWithProgress(resp: Response, dotNetRef: any, id: string) {
        const total = contentLengthOf(resp);
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
        const bytes = new Uint8Array(loaded);
        let offset = 0;
        for (const c of chunks) { bytes.set(c, offset); offset += c.byteLength; }
        return bytes;
    }

    async function send(id: string, req: any, dotNetRef: any, withProgress: boolean): Promise<any> {
        const controller = openController(id);

        try {
            const resp = await fetch(req.url, buildInit(req, controller));

            let bytes: Uint8Array;
            if (withProgress && resp.body && typeof (resp.body as any).getReader === 'function') {
                bytes = await readWithProgress(resp, dotNetRef, id);
            } else {
                const buf = await resp.arrayBuffer();
                bytes = new Uint8Array(buf);
                if (withProgress) {
                    butil.utils.dispatch(dotNetRef, 'InvokeFetchProgress', id, { loaded: bytes.byteLength, total: contentLengthOf(resp) });
                }
            }

            return toResponse(resp, bytes);
        } catch (e: any) {
            return toErrorResponse(req.url, e);
        } finally {
            delete _controllers[id];
        }
    }

    async function sendStream(id: string, req: any, streamRef: any, dotNetRef: any, withProgress: boolean, total: number | null): Promise<any> {
        // Consumes an abort that arrived before this call did, so the fetch below starts already
        // aborted rather than pulling the whole body from .NET first.
        const controller = openController(id);

        try {
            // Aborted before it began: answer without ever asking .NET for the stream, so nothing of
            // the body is pulled and the caller's stream is untouched.
            if (controller.signal.aborted) return toErrorResponse(req.url, { name: 'AbortError' });

            // The .NET stream arrives as a reference; stream() turns it into a ReadableStream that
            // pulls from .NET on demand, so the body is never held in memory whole on either side.
            const source: ReadableStream = await streamRef.stream();
            const init: any = buildInit(req, controller);
            init.body = withProgress ? countingStream(source, dotNetRef, id, total) : source;
            // Required by the spec for a streamed request body, and the member whose absence is how
            // an engine without streaming upload rejects the call.
            init.duplex = 'half';

            const resp = await fetch(req.url, init);
            const buf = await resp.arrayBuffer();
            return toResponse(resp, new Uint8Array(buf));
        } catch (e: any) {
            return toErrorResponse(req.url, e);
        } finally {
            delete _controllers[id];
        }
    }

    // Wraps the upload so each chunk is counted on its way past. Progress here is what .NET has
    // handed the browser, which is ahead of what the server has acknowledged.
    function countingStream(source: ReadableStream, dotNetRef: any, id: string, total: number | null) {
        const reader = source.getReader();
        let loaded = 0;

        return new ReadableStream({
            async pull(controller) {
                const { value, done } = await reader.read();
                if (done) {
                    controller.close();
                    return;
                }
                loaded += value.byteLength;
                butil.utils.dispatch(dotNetRef, 'InvokeFetchProgress', id, { loaded, total });
                controller.enqueue(value);
            },
            cancel(reason) { return reader.cancel(reason); }
        });
    }

    function supportsStreamingUpload() {
        if (typeof ReadableStream !== 'function' || typeof Request !== 'function') return false;

        let used = false;
        try {
            // Constructing a Request with a stream body only reads the body member on an engine
            // that supports it - which is exactly what this getter detects.
            new Request('https://example.com', {
                method: 'POST',
                body: new ReadableStream(),
                get duplex() { used = true; return 'half'; }
            } as any);
        } catch {
            return false;
        }
        return used;
    }

    function start(id: string, req: any) {
        const controller = openController(id);
        // Fire-and-forget: errors are silently swallowed because there's no consumer for the
        // result. Use send() when you need the response.
        fetch(req.url, buildInit(req, controller)).catch(() => { /* ignore */ }).finally(() => { delete _controllers[id]; });
    }

    function abort(id: string) {
        const c = _controllers[id];
        if (!c) {
            // Either the request has not reached this module yet - the race openController() closes -
            // or it has already finished, in which case the note expires unread.
            _pendingAborts[id] = true;
            setTimeout(() => { delete _pendingAborts[id]; }, PENDING_ABORT_TTL);
            return;
        }
        delete _controllers[id];
        try { c.abort(); } catch { /* already aborted */ }
    }
}(BitButil));
