var BitButil = BitButil || {};

(function (butil: any) {
    function toItem(c: any) {
        return {
            name: c.name,
            value: c.value,
            domain: c.domain ?? null,
            path: c.path ?? null,
            // CookieStore exposes Unix epoch milliseconds; map to ISO 8601 for dotnet.
            expires: typeof c.expires === 'number' ? new Date(c.expires).toISOString() : null,
            secure: !!c.secure,
            sameSite: c.sameSite ?? null,
            partitioned: typeof c.partitioned === 'boolean' ? c.partitioned : null
        };
    }

    function toInit(c: any) {
        const init: any = { name: c.name, value: c.value };
        if (c.domain) init.domain = c.domain;
        if (c.path) init.path = c.path;
        if (c.expires) init.expires = Date.parse(c.expires);
        if (typeof c.secure === 'boolean') init.secure = c.secure;
        if (c.sameSite) init.sameSite = c.sameSite;
        if (typeof c.partitioned === 'boolean') init.partitioned = c.partitioned;
        return init;
    }

    butil.cookieStore = {
        isSupported() { return 'cookieStore' in window; },
        async getAll() {
            const cs: any = (window as any).cookieStore;
            if (!cs) return [];
            const list = await cs.getAll();
            return list.map(toItem);
        },
        async get(name: string) {
            const cs: any = (window as any).cookieStore;
            if (!cs) return null;
            const c = await cs.get(name);
            return c ? toItem(c) : null;
        },
        async set(cookie: any) {
            const cs: any = (window as any).cookieStore;
            if (!cs) return;
            await cs.set(toInit(cookie));
        },
        async delete(name: string) {
            const cs: any = (window as any).cookieStore;
            if (!cs) return;
            await cs.delete(name);
        }
    };
}(BitButil));
