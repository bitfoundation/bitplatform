var BitButil = BitButil || {};

(function (butil: any) {
    butil.cacheStorage = {
        isSupported() { return 'caches' in window; },
        keys,
        has,
        delete: del,
        add,
        addAll,
        putBytes,
        putText,
        match,
        deleteEntry,
        entryKeys
    };

    async function keys() {
        if (!('caches' in window)) return [];
        try { return await caches.keys(); } catch { return []; }
    }

    async function has(name: string) {
        if (!('caches' in window)) return false;
        try { return await caches.has(name); } catch { return false; }
    }

    async function del(name: string) {
        if (!('caches' in window)) return false;
        try { return await caches.delete(name); } catch { return false; }
    }

    async function add(name: string, url: string) {
        if (!('caches' in window)) return;
        const cache = await caches.open(name);
        try { await cache.add(url); } catch { /* network failure / 404 */ }
    }

    async function addAll(name: string, urls: string[]) {
        if (!('caches' in window) || !urls?.length) return;
        const cache = await caches.open(name);
        try { await cache.addAll(urls); } catch { /* one or more requests failed */ }
    }

    async function putBytes(name: string, url: string, data: Uint8Array, contentType: string, status: number, statusText: string) {
        if (!('caches' in window)) return;
        const cache = await caches.open(name);
        const body = butil.utils.arrayToBuffer(data);
        const response = new Response(body, {
            status,
            statusText,
            headers: { 'Content-Type': contentType || 'application/octet-stream' }
        });
        try { await cache.put(url, response); } catch { /* quota exceeded / put failed */ }
    }

    async function putText(name: string, url: string, text: string, contentType: string, status: number, statusText: string) {
        if (!('caches' in window)) return;
        const cache = await caches.open(name);
        const response = new Response(text ?? '', {
            status,
            statusText,
            headers: { 'Content-Type': contentType || 'text/plain;charset=utf-8' }
        });
        try { await cache.put(url, response); } catch { /* quota exceeded / put failed */ }
    }

    async function match(name: string, url: string) {
        const empty = { found: false, status: 0, statusText: '', url: '', headers: {}, body: new Uint8Array() };
        if (!('caches' in window)) return empty;
        try {
            const cache = await caches.open(name);
            const response = await cache.match(url);
            if (!response) return empty;
            const buf = await response.arrayBuffer();
            const headers: any = {};
            response.headers.forEach((v, k) => { headers[k] = v; });
            return {
                found: true,
                status: response.status,
                statusText: response.statusText,
                url: response.url || url,
                headers,
                body: new Uint8Array(buf)
            };
        } catch { return empty; }
    }

    async function deleteEntry(name: string, url: string) {
        if (!('caches' in window)) return false;
        const cache = await caches.open(name);
        try { return await cache.delete(url); } catch { return false; }
    }

    async function entryKeys(name: string) {
        if (!('caches' in window)) return [];
        try {
            const cache = await caches.open(name);
            const reqs = await cache.keys();
            return reqs.map(r => r.url);
        } catch { return []; }
    }
}(BitButil));
