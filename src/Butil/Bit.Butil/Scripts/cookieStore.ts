var BitButil = (window as any).BitButil = (window as any).BitButil || {};

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

    const _listeners: { [id: string]: (e: any) => void } = {};

    butil.cookieStore = {
        isSupported() { return 'cookieStore' in window; },
        subscribe(dotNetRef: any, listenerId: string) {
            const cs: any = (window as any).cookieStore;
            if (!cs) return false;
            // A single change event carries both halves: cookies written and cookies removed.
            // An overwrite shows up as a delete followed by a set, so both lists can be non-empty.
            const handler = (e: any) => butil.utils.dispatch(dotNetRef, 'InvokeCookieStoreChange', listenerId, {
                changed: (e.changed || []).map(toItem),
                deleted: (e.deleted || []).map(toItem)
            });
            _listeners[listenerId] = handler;
            cs.addEventListener('change', handler);
            return true;
        },
        unsubscribe(listenerId: string) {
            const handler = _listeners[listenerId];
            if (!handler) return;
            delete _listeners[listenerId];
            (window as any).cookieStore?.removeEventListener('change', handler);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.cookieStore.unsubscribe(id);
        },
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
