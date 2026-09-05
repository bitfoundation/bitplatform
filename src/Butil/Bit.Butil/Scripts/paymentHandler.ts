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

    // Every member goes through the active registration: paymentManager hangs off it, and a page
    // whose worker has not activated yet has nothing to configure. `ready` is the wait for that,
    // and it never rejects - it simply does not settle until there is a registration, so callers
    // are not left with a half-applied hint. Which is also why getRegistration() comes first: with
    // no worker registered at all `ready` never settles, and every call would hang forever instead
    // of answering "nothing to configure here". Once the registration has an active worker, `ready`
    // would resolve to that same registration, so it is only awaited while there is none yet.
    async function manager() {
        if (!isSupported()) return null;
        try {
            const registered: any = await navigator.serviceWorker.getRegistration();
            if (!registered) return null;

            const registration: any = registered.active ? registered : await navigator.serviceWorker.ready;
            return registration?.paymentManager ?? null;
        } catch {
            return null;
        }
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
