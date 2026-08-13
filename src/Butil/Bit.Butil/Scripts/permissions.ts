var BitButil = BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: { status: any, handler: () => void } } = {};

    butil.permissions = {
        isSupported() { return !!(window.navigator as any).permissions?.query; },
        async query(name: string) {
            const perms: any = (window.navigator as any).permissions;
            if (!perms || typeof perms.query !== 'function') return 'unknown';
            try {
                const status = await perms.query({ name });
                return status?.state ?? 'unknown';
            } catch {
                // The browser doesn't recognize this descriptor name.
                return 'unknown';
            }
        },
        // The PermissionStatus object is the live one - it keeps updating itself, so holding onto
        // it and listening for 'change' is the only way to learn that the user revoked a grant
        // from browser UI rather than from the page.
        async subscribe(dotNetRef: any, listenerId: string, name: string) {
            const perms: any = (window.navigator as any).permissions;
            if (!perms || typeof perms.query !== 'function') return 'unknown';
            let status: any;
            try {
                status = await perms.query({ name });
            } catch {
                return 'unknown';   // unrecognized descriptor
            }

            const handler = () => butil.utils.dispatch(dotNetRef, 'InvokePermissionChange', listenerId, status.state ?? 'unknown');
            _listeners[listenerId] = { status, handler };
            status.addEventListener('change', handler);
            return status.state ?? 'unknown';
        },
        unsubscribe(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            entry.status.removeEventListener('change', entry.handler);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.permissions.unsubscribe(id);
        }
    };
}(BitButil));
