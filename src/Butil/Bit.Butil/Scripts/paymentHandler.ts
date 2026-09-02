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
    // are not left with a half-applied hint.
    async function manager() {
        if (!isSupported()) return null;
        try {
            const registration: any = await navigator.serviceWorker.ready;
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

    async function enableDelegations(delegations: string[]) {
        const paymentManager = await manager();
        if (!paymentManager?.enableDelegations || !delegations?.length) return [];
        try {
            await paymentManager.enableDelegations(delegations);
            return delegations;
        } catch {
            // A name this engine does not know rejects the whole call - none of them took effect.
            return [];
        }
    }
}(BitButil));
