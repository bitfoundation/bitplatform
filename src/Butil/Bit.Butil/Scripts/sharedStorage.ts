var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    function storage() { return (window as any).sharedStorage; }

    butil.sharedStorage = {
        isSupported() { return !!storage(); },
        async set(key: string, value: string, ignoreIfPresent: boolean) {
            const store = storage();
            if (!store?.set) return false;
            try {
                await store.set(key, value, { ignoreIfPresent });
                return true;
            } catch {
                // Not a secure context, blocked by permissions policy, or over the key/value limits.
                return false;
            }
        },
        async append(key: string, value: string) {
            const store = storage();
            if (!store?.append) return false;
            try {
                await store.append(key, value);
                return true;
            } catch {
                return false;
            }
        },
        async delete(key: string) {
            const store = storage();
            if (!store?.delete) return false;
            try {
                await store.delete(key);
                return true;
            } catch {
                return false;
            }
        },
        async clear() {
            const store = storage();
            if (!store?.clear) return false;
            try {
                await store.clear();
                return true;
            } catch {
                return false;
            }
        },
        async addModule(url: string) {
            const worklet = storage()?.worklet;
            if (!worklet?.addModule) return false;
            try {
                // A worklet may only be added once per page, and it is the only code that can ever
                // read what was written - the page itself is deliberately write-only.
                await worklet.addModule(url);
                return true;
            } catch {
                return false;
            }
        },
        async run(operation: string, data: any, keepAlive: boolean) {
            const store = storage();
            if (!store?.run) return false;
            try {
                await store.run(operation, { data, keepAlive });
                return true;
            } catch {
                // No worklet registered under that name, or the module was never added.
                return false;
            }
        },
        async selectURL(operation: string, urls: string[], data: any, resolveToConfig: boolean) {
            const store = storage();
            if (!store?.selectURL) return false;
            try {
                // The worklet picks one of the urls by index; the page never learns which, which is
                // the entire privacy model. The result is only usable inside a fenced frame.
                const result = await store.selectURL(
                    operation,
                    (urls ?? []).map(url => ({ url })),
                    { data, resolveToConfig });
                return !!result;
            } catch {
                return false;
            }
        }
    };
}(BitButil));
