var BitButil = BitButil || {};

(function (butil: any) {
    // Each acquired lock parks on a Promise that resolves only when dotnet calls release(token).
    // The map of pending releases lives here so multiple locks per tab don't trip each other.
    const _pending: { [token: string]: () => void } = {};

    butil.webLocks = {
        isSupported() { return !!(window.navigator as any).locks; },
        acquire,
        release,
        query
    };

    async function acquire(name: string, mode: 'exclusive' | 'shared', ifAvailable: boolean, steal: boolean, token: string) {
        const lockManager = (window.navigator as any).locks;
        if (!lockManager) return false;

        // Bridge the callback model into a deferred. We resolve `acquired` once we're inside the
        // callback and resolve the *holder* promise when dotnet releases.
        let acquiredResolve: (v: boolean) => void;
        const acquiredPromise = new Promise<boolean>(r => { acquiredResolve = r; });

        const holder = new Promise<void>(resolve => {
            _pending[token] = resolve;
        });

        const options: any = { mode };
        if (ifAvailable) options.ifAvailable = true;
        if (steal) options.steal = true;

        // Fire the request without awaiting the outer call; the JS callback receiving null
        // means the lock wasn't acquired (only happens with ifAvailable: true).
        lockManager.request(name, options, (lock: any) => {
            if (!lock) {
                acquiredResolve(false);
                delete _pending[token];
                return undefined;
            }
            acquiredResolve(true);
            return holder;
        }).catch(() => {
            acquiredResolve(false);
            delete _pending[token];
        });

        return acquiredPromise;
    }

    function release(token: string) {
        const r = _pending[token];
        if (!r) return;
        delete _pending[token];
        r();
    }

    async function query() {
        const lockManager = (window.navigator as any).locks;
        if (!lockManager?.query) return { held: [], pending: [] };
        const snap = await lockManager.query();
        const map = (arr: any[]) => (arr || []).map(l => ({ name: l.name, mode: l.mode, clientId: l.clientId }));
        return { held: map(snap.held), pending: map(snap.pending) };
    }
}(BitButil));
