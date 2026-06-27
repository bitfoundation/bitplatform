var BitButil = BitButil || {};

(function (butil: any) {
    const _msgListeners: { [id: string]: (e: MessageEvent) => void } = {};
    const _ccListeners: { [id: string]: () => void } = {};

    butil.serviceWorker = {
        isSupported() { return 'serviceWorker' in window.navigator; },
        register,
        getRegistration,
        update,
        unregister,
        postMessage,
        subscribeMessage,
        unsubscribeMessage,
        subscribeControllerChange,
        unsubscribeControllerChange
    };

    function info(reg: ServiceWorkerRegistration | null | undefined) {
        if (!reg) return { isRegistered: false, scope: '', activeState: null, installingState: null, waitingState: null, updateViaCache: null };
        return {
            isRegistered: true,
            scope: reg.scope ?? '',
            activeState: reg.active?.state ?? null,
            installingState: reg.installing?.state ?? null,
            waitingState: reg.waiting?.state ?? null,
            updateViaCache: (reg as any).updateViaCache ?? null
        };
    }

    async function register(scriptUrl: string, scope: string | null, updateViaCache: string | null, moduleType: boolean) {
        if (!('serviceWorker' in window.navigator)) return info(null);
        try {
            const opts: any = {};
            if (scope) opts.scope = scope;
            if (updateViaCache) opts.updateViaCache = updateViaCache;
            if (moduleType) opts.type = 'module';
            const reg = await window.navigator.serviceWorker.register(scriptUrl, opts);
            return info(reg);
        } catch {
            return info(null);
        }
    }

    async function getRegistration(scope: string | null) {
        if (!('serviceWorker' in window.navigator)) return info(null);
        const reg = await window.navigator.serviceWorker.getRegistration(scope ?? undefined);
        return info(reg);
    }

    async function update(scope: string | null) {
        const reg = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        if (!reg) return;
        try { await reg.update(); } catch { /* network failure / 404 - surface via subsequent getRegistration */ }
    }

    async function unregister(scope: string | null) {
        const reg = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        if (!reg) return false;
        try { return await reg.unregister(); } catch { return false; }
    }

    function postMessage(message: any) {
        const ctrl = window.navigator.serviceWorker?.controller;
        if (!ctrl) return false;
        try { ctrl.postMessage(message); return true; } catch { return false; }
    }

    function subscribeMessage(dotNetRef: any, listenerId: string) {
        const sw = window.navigator.serviceWorker;
        if (!sw) return;
        const handler = (e: MessageEvent) => {
            butil.utils.dispatch(dotNetRef, 'InvokeServiceWorkerMessage', listenerId, e.data ?? null);
        };
        _msgListeners[listenerId] = handler;
        sw.addEventListener('message', handler);
    }

    function unsubscribeMessage(listenerId: string) {
        const handler = _msgListeners[listenerId];
        if (!handler) return;
        delete _msgListeners[listenerId];
        try { window.navigator.serviceWorker?.removeEventListener('message', handler); } catch { /* ignore */ }
    }

    function subscribeControllerChange(dotNetRef: any, listenerId: string) {
        const sw = window.navigator.serviceWorker;
        if (!sw) return;
        const handler = () => { butil.utils.dispatch(dotNetRef, 'InvokeServiceWorkerControllerChange', listenerId); };
        _ccListeners[listenerId] = handler;
        sw.addEventListener('controllerchange', handler);
    }

    function unsubscribeControllerChange(listenerId: string) {
        const handler = _ccListeners[listenerId];
        if (!handler) return;
        delete _ccListeners[listenerId];
        try { window.navigator.serviceWorker?.removeEventListener('controllerchange', handler); } catch { /* ignore */ }
    }
}(BitButil));
