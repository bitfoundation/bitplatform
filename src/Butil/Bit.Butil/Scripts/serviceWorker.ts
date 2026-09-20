var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _msgListeners: { [id: string]: (e: MessageEvent) => void } = {};
    const _ccListeners: { [id: string]: () => void } = {};

    butil.serviceWorker = {
        isSupported() { return 'serviceWorker' in window.navigator; },
        register,
        getRegistration,
        getRegistrations,
        ready,
        update,
        unregister,
        postMessage,
        subscribeMessage,
        unsubscribeMessage,
        subscribeControllerChange,
        unsubscribeControllerChange,
        enableNavigationPreload,
        disableNavigationPreload,
        setNavigationPreloadHeader,
        navigationPreloadState,
        skipWaiting,
        claim,
        matchAllClients
    };

    // The three service-worker facilities a page cannot reach on its own - the Clients API,
    // clients.claim() and skipWaiting() - all live on the worker's global scope, so the page's only
    // way in is to ask the worker to do it. These post a `{ __butil: '<command>' }` message and read
    // the answer back off a MessageChannel port, which is the protocol the worker has to implement
    // (documented on the C# members, and implemented by the demo site's /sw.js). A worker that does
    // not answer times out rather than hanging the caller.
    const BUTIL_MESSAGE = '__butil';

    function ask(worker: ServiceWorker | null | undefined, command: string, message: any, timeoutMs: number) {
        if (!worker) return Promise.resolve(null);

        return new Promise<any>(resolve => {
            let settled = false;
            const channel = new MessageChannel();
            const finish = (value: any) => {
                if (settled) return;
                settled = true;
                try { channel.port1.close(); } catch { /* already closed */ }
                resolve(value);
            };

            channel.port1.onmessage = (e: MessageEvent) => finish(e.data ?? null);
            window.setTimeout(() => finish(null), timeoutMs);

            try { worker.postMessage({ [BUTIL_MESSAGE]: command, ...message }, [channel.port2]); }
            catch { finish(null); }
        });
    }

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

    async function getRegistrations() {
        if (!('serviceWorker' in window.navigator)) return [];
        const regs = await window.navigator.serviceWorker.getRegistrations();
        return (regs || []).map(info);
    }

    // navigator.serviceWorker.ready resolves only once a registration is ACTIVE, so it is the
    // reliable point to start postMessage-ing; a freshly registered worker is still 'installing'.
    // It never rejects and never resolves without a worker, hence the timeout.
    async function ready(timeoutMs: number) {
        if (!('serviceWorker' in window.navigator)) return info(null);
        const timeout = new Promise<null>(resolve => window.setTimeout(() => resolve(null), timeoutMs));
        const reg = await Promise.race([window.navigator.serviceWorker.ready, timeout]);
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

    async function preload(scope: string | null) {
        const reg: any = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        return reg?.navigationPreload ?? null;
    }

    async function enableNavigationPreload(scope: string | null) {
        const navigationPreload = await preload(scope);
        if (!navigationPreload?.enable) return false;
        // Rejects while the registration has no active worker: preload is a property of the active
        // worker rather than of the registration.
        try { await navigationPreload.enable(); return true; } catch { return false; }
    }

    async function disableNavigationPreload(scope: string | null) {
        const navigationPreload = await preload(scope);
        if (!navigationPreload?.disable) return false;
        try { await navigationPreload.disable(); return true; } catch { return false; }
    }

    async function setNavigationPreloadHeader(scope: string | null, value: string) {
        const navigationPreload = await preload(scope);
        if (!navigationPreload?.setHeaderValue) return false;
        try { await navigationPreload.setHeaderValue(value); return true; } catch { return false; }
    }

    async function navigationPreloadState(scope: string | null) {
        const navigationPreload = await preload(scope);
        if (!navigationPreload?.getState) return { isSupported: false, enabled: false, headerValue: '' };
        try {
            const state = await navigationPreload.getState();
            return { isSupported: true, enabled: !!state.enabled, headerValue: state.headerValue ?? '' };
        } catch {
            return { isSupported: true, enabled: false, headerValue: '' };
        }
    }

    async function skipWaiting(scope: string | null) {
        const reg = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        // The waiting worker is the one to tell: a worker can only skip its own waiting phase, and
        // the active one is not waiting for anything.
        if (!reg?.waiting) return false;
        try { reg.waiting.postMessage({ [BUTIL_MESSAGE]: 'skipWaiting' }); return true; }
        catch { return false; }
    }

    // The worker a scoped question goes to. The controller is the answer only to the unscoped
    // question: it is whichever worker controls this page, which is not necessarily the one
    // registered under the scope the caller named.
    function workerFor(reg: ServiceWorkerRegistration | null | undefined, scope: string | null) {
        return reg?.active ?? (scope === null ? window.navigator.serviceWorker?.controller : null);
    }

    async function claim(scope: string | null, timeoutMs: number) {
        const reg = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        const worker = workerFor(reg, scope);
        const answer = await ask(worker, 'claim', {}, timeoutMs);
        return answer === true || answer?.claimed === true;
    }

    async function matchAllClients(scope: string | null, includeUncontrolled: boolean, type: string, timeoutMs: number) {
        const reg = await window.navigator.serviceWorker?.getRegistration(scope ?? undefined);
        const worker = workerFor(reg, scope);
        const answer = await ask(worker, 'clients', { includeUncontrolled, type }, timeoutMs);
        const clients = Array.isArray(answer) ? answer : answer?.clients;
        if (!Array.isArray(clients)) return [];

        return clients.map((client: any) => ({
            id: client.id ?? '',
            url: client.url ?? '',
            type: client.type ?? '',
            frameType: client.frameType ?? '',
            focused: !!client.focused,
            visibilityState: client.visibilityState ?? ''
        }));
    }

    function unsubscribeControllerChange(listenerId: string) {
        const handler = _ccListeners[listenerId];
        if (!handler) return;
        delete _ccListeners[listenerId];
        try { window.navigator.serviceWorker?.removeEventListener('controllerchange', handler); } catch { /* ignore */ }
    }
}(BitButil));
