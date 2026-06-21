var BitButil = BitButil || {};

(function (butil: any) {
    const _tracked: { [id: string]: Notification } = {};

    butil.notification = {
        isSupported,
        getPermission,
        requestPermission,
        show,
        showTracked,
        close,
        dispose,
        disposeAll
    };

    function isSupported() {
        return ('Notification' in window);
    }

    function getPermission() {
        return Notification.permission;
    }

    async function requestPermission() {
        return await Notification.requestPermission();
    }

    function normalize(options?: NotificationOptions) {
        if (!options) return options;
        for (const key in options) {
            if (Object.prototype.hasOwnProperty.call(options, key) && (options as any)[key] === null) {
                (options as any)[key] = undefined;
            }
        }
        return options;
    }

    function show(title: string, options?: NotificationOptions) {
        normalize(options);
        try {
            // tslint:disable-next-line:no-unused-expression
            new Notification(title, options);
        } catch (e) {
            navigator.serviceWorker?.getRegistration().then(reg => {
                reg?.showNotification(title, options);
            });
        }
    }

    function showTracked(id: string, title: string, options: NotificationOptions | undefined, dotNetRef: any) {
        normalize(options);
        try {
            const n = new Notification(title, options);
            _tracked[id] = n;
            n.onclick = () => butil.utils.dispatch(dotNetRef, 'InvokeNotificationClick', id);
            n.onshow = () => butil.utils.dispatch(dotNetRef, 'InvokeNotificationShow', id);
            n.onclose = () => {
                // `close` fires for both natural dismiss and programmatic close(id), so this is the
                // single cleanup point: notify .NET (which also drops the C# listener) then purge the
                // JS-side entry so neither side accumulates references for the service lifetime.
                butil.utils.dispatch(dotNetRef, 'InvokeNotificationClose', id);
                untrack(id);
            };
            n.onerror = () => butil.utils.dispatch(dotNetRef, 'InvokeNotificationError', id);
        } catch {
            // Service-worker fallback can't be tracked the same way (the toast is owned by the SW)
            // - fire show + error so callers can detect graceful degradation.
            navigator.serviceWorker?.getRegistration().then(reg => {
                reg?.showNotification(title, options);
                butil.utils.dispatch(dotNetRef, 'InvokeNotificationShow', id);
            }).catch(() => butil.utils.dispatch(dotNetRef, 'InvokeNotificationError', id));
        }
    }

    function close(id: string) {
        const n = _tracked[id];
        if (!n) return;
        // Triggers the `close` event, whose handler removes the entry from _tracked and drops the
        // C# listener - keeping Close() and natural dismiss on the same cleanup path.
        try { n.close(); } catch { /* already closed */ }
    }

    function untrack(id: string) {
        const n = _tracked[id];
        if (!n) return;
        delete _tracked[id];
        n.onclick = null; n.onshow = null; n.onclose = null; n.onerror = null;
    }

    function dispose(id: string) {
        const n = _tracked[id];
        if (!n) return;
        try { n.close(); } catch { /* already closed */ }
        untrack(id);
    }

    function disposeAll(ids: string[]) {
        (ids || []).forEach(dispose);
    }

}(BitButil));