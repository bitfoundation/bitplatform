var BitButil = BitButil || {};

(function (butil: any) {
    const _controllers: { [id: string]: AbortController } = {};

    butil.fetch = {
        send,
        start,
        abort
    };

    function buildInit(req: any, controller: AbortController): RequestInit {
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
            redirect: req.redirect || 'follow',
            signal: controller.signal
        };
        if (req.body && req.body.length > 0) {
            init.body = butil.utils.arrayToBuffer(req.body);
        }
        return init;
    }

    function headersToObject(h: Headers) {
        const out: any = {};
        h.forEach((v, k) => { out[k] = v; });
        return out;
    }

    async function send(id: string, req: any, dotNetRef: any, withProgress: boolean): Promise<any> {
        const controller = new AbortController();
        _controllers[id] = controller;

        try {
            const resp = await fetch(req.url, buildInit(req, controller));
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
            const aborted = e?.name === 'AbortError';
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
            delete _controllers[id];
        }
    }

    function start(id: string, req: any) {
        const controller = new AbortController();
        _controllers[id] = controller;
        // Fire-and-forget: errors are silently swallowed because there's no consumer for the
        // result. Use send() when you need the response.
        fetch(req.url, buildInit(req, controller)).catch(() => { /* ignore */ }).finally(() => { delete _controllers[id]; });
    }

    function abort(id: string) {
        const c = _controllers[id];
        if (!c) return;
        delete _controllers[id];
        try { c.abort(); } catch { /* already aborted */ }
    }
}(BitButil));
