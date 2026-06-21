var BitButil = BitButil || {};

(function (butil: any) {
    let _sentinel: any = null;
    const _persistent: { [token: string]: { sentinel: any, listener: () => void } } = {};

    butil.wakeLock = {
        isSupported() { return !!(window.navigator as any).wakeLock; },
        async request() {
            const lock = (window.navigator as any).wakeLock;
            if (!lock) return false;
            try {
                _sentinel = await lock.request('screen');
                _sentinel.addEventListener?.('release', () => { _sentinel = null; });
                return true;
            } catch {
                _sentinel = null;
                return false;
            }
        },
        async release() {
            if (!_sentinel) return;
            try { await _sentinel.release(); } catch { /* already released */ }
            _sentinel = null;
        },
        async persist(token: string) {
            const lockApi = (window.navigator as any).wakeLock;
            if (!lockApi) return;

            const acquire = async () => {
                if (document.visibilityState !== 'visible') return;
                const entry = _persistent[token];
                // The sentinel may already be active and unreleased, in which case re-requesting
                // would create a second one we can't track - bail out.
                if (entry?.sentinel && !entry.sentinel.released) return;
                try {
                    const sentinel = await lockApi.request('screen');
                    if (_persistent[token]) _persistent[token].sentinel = sentinel;
                } catch { /* permission denied or page not visible */ }
            };

            const listener = () => { acquire(); };
            _persistent[token] = { sentinel: null, listener };
            document.addEventListener('visibilitychange', listener);
            await acquire();
        },
        async unpersist(token: string) {
            const entry = _persistent[token];
            if (!entry) return;
            delete _persistent[token];
            document.removeEventListener('visibilitychange', entry.listener);
            if (entry.sentinel && !entry.sentinel.released) {
                try { await entry.sentinel.release(); } catch { /* already released */ }
            }
        }
    };
}(BitButil));
