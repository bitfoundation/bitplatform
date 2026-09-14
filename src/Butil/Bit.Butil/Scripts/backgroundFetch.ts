var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: { registration: any; handler: (e: any) => void } } = {};

    async function manager() {
        const reg: any = await window.navigator.serviceWorker?.getRegistration();
        return reg?.backgroundFetch ?? null;
    }

    function info(registration: any) {
        if (!registration) return null;
        return {
            id: registration.id ?? '',
            uploadTotal: Number(registration.uploadTotal ?? 0),
            uploaded: Number(registration.uploaded ?? 0),
            downloadTotal: Number(registration.downloadTotal ?? 0),
            downloaded: Number(registration.downloaded ?? 0),
            // '' while the fetch is still running; 'success' or 'failure' once it is over.
            result: registration.result ?? '',
            failureReason: registration.failureReason ?? '',
            recordsAvailable: !!registration.recordsAvailable
        };
    }

    butil.backgroundFetch = {
        async isSupported() {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            return !!(reg && reg.backgroundFetch);
        },
        async fetch(id: string, urls: string[], title: string, downloadTotal: number, icons: any[]) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.fetch) return null;

            const options: any = {};
            if (title) options.title = title;
            if (downloadTotal > 0) options.downloadTotal = downloadTotal;
            if (icons?.length) {
                options.icons = icons.map(icon => ({
                    src: icon.src,
                    sizes: icon.sizes || undefined,
                    type: icon.type || undefined,
                    label: icon.label || undefined
                }));
            }

            try {
                // One url is passed as a single request rather than a one-element array so the
                // browser's own UI names the file instead of calling it a batch of 1.
                const requests = urls?.length === 1 ? urls[0] : urls;
                return info(await backgroundFetch.fetch(id, requests, options));
            } catch {
                // A duplicate id, an empty request list, a cross-origin no-cors request, or the
                // quota being exceeded - all TypeErrors with nothing worth relaying.
                return null;
            }
        },
        async get(id: string) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.get) return null;
            try { return info(await backgroundFetch.get(id)); } catch { return null; }
        },
        async getIds() {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.getIds) return [];
            try { return await backgroundFetch.getIds(); } catch { return []; }
        },
        async abort(id: string) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.get) return false;
            try {
                const registration = await backgroundFetch.get(id);
                if (!registration?.abort) return false;
                return await registration.abort();
            } catch {
                return false;
            }
        },
        async getRecordUrls(id: string) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.get) return [];
            try {
                const registration = await backgroundFetch.get(id);
                if (!registration?.matchAll) return [];
                const records = await registration.matchAll();
                return records.map((record: any) => record.request?.url ?? '');
            } catch {
                // recordsAvailable goes false once the fetch is over and the records are released.
                return [];
            }
        },
        async readRecordText(id: string, url: string, timeoutMs: number) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.get) return null;
            try {
                const registration = await backgroundFetch.get(id);
                if (!registration?.match) return null;
                const record = await registration.match(url);
                if (!record?.responseReady) return null;

                // responseReady stays pending for the whole download, which for the API's own
                // use case is minutes - the timeout is what keeps a .NET caller from awaiting it.
                const timeout = new Promise<null>(resolve => window.setTimeout(() => resolve(null), timeoutMs));
                const response = await Promise.race([record.responseReady, timeout]);
                return response ? await (response as Response).text() : null;
            } catch {
                return null;
            }
        },
        async subscribeProgress(dotNetRef: any, listenerId: string, id: string) {
            const backgroundFetch = await manager();
            if (!backgroundFetch?.get) return false;
            try {
                const registration = await backgroundFetch.get(id);
                if (!registration?.addEventListener) return false;

                const handler = () => butil.utils.dispatch(dotNetRef, 'InvokeBackgroundFetchProgress', listenerId, info(registration));
                _listeners[listenerId] = { registration, handler };
                registration.addEventListener('progress', handler);
                return true;
            } catch {
                return false;
            }
        },
        unsubscribeProgress(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            try { entry.registration.removeEventListener('progress', entry.handler); } catch { /* registration is gone */ }
        }
    };
}(BitButil));
