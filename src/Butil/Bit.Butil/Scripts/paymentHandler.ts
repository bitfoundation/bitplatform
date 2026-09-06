var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.paymentHandler = {
        isSupported,
        getUserHint,
        setUserHint,
        enableDelegations
    };

    function isSupported() {
        return !!navigator.serviceWorker
            && typeof (window as any).ServiceWorkerRegistration === 'function'
            && 'paymentManager' in (window as any).ServiceWorkerRegistration.prototype;
    }

    // How long an installing worker is given to activate. `ready` never rejects and never resolves
    // without an active registration, so a worker that fails to activate - its install handler threw,
    // its script 404s - would otherwise leave every member of this module pending for the life of the
    // page. Losing the race answers "nothing to configure here", which is what a page with no usable
    // handler has anyway.
    const READY_TIMEOUT = 10000;

    // Every member goes through the active registration: paymentManager hangs off it, and a page
    // whose worker has not activated yet has nothing to configure. `ready` is the wait for that,
    // so callers are not left with a half-applied hint. Which is also why getRegistration() comes
    // first: with no worker registered at all `ready` never settles, and every call would hang
    // instead of answering at once. Once the registration has an active worker, `ready` would
    // resolve to that same registration, so it is only awaited while there is none yet.
    async function manager() {
        if (!isSupported()) return null;
        try {
            const registered: any = await navigator.serviceWorker.getRegistration();
            if (!registered) return null;

            const registration: any = registered.active ? registered : await ready();
            return registration?.paymentManager ?? null;
        } catch {
            return null;
        }
    }

    function ready() {
        let timer: any = null;
        const timeout = new Promise<null>(resolve => { timer = setTimeout(() => resolve(null), READY_TIMEOUT); });
        return Promise.race([navigator.serviceWorker.ready, timeout])
            .finally(() => { if (timer !== null) clearTimeout(timer); });
    }

    async function getUserHint() {
        const paymentManager = await manager();
        return paymentManager?.userHint ?? '';
    }

    async function setUserHint(userHint: string) {
        const paymentManager = await manager();
        if (!paymentManager) return;
        try { paymentManager.userHint = userHint ?? ''; }
        catch { /* read-only in this engine */ }
    }

    // All or nothing: a name this engine does not know rejects the whole call, so there is no
    // "partly accepted" to report - true means every delegation took effect.
    async function enableDelegations(delegations: string[]) {
        const paymentManager = await manager();
        if (!paymentManager?.enableDelegations || !delegations?.length) return false;
        try {
            await paymentManager.enableDelegations(delegations);
            return true;
        } catch {
            return false;
        }
    }
}(BitButil));
