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
        dispose
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

    function showTracked(id: string, title: string, options: NotificationOptions | undefined,
        clickMethod: string, showMethod: string, closeMethod: string, errorMethod: string) {
        normalize(options);
        try {
            const n = new Notification(title, options);
            _tracked[id] = n;
            n.onclick = () => DotNet.invokeMethodAsync('Bit.Butil', clickMethod, id);
            n.onshow = () => DotNet.invokeMethodAsync('Bit.Butil', showMethod, id);
            n.onclose = () => DotNet.invokeMethodAsync('Bit.Butil', closeMethod, id);
            n.onerror = () => DotNet.invokeMethodAsync('Bit.Butil', errorMethod, id);
        } catch {
            // Service-worker fallback can't be tracked the same way (the toast is owned by the SW)
            // — fire show + error so callers can detect graceful degradation.
            navigator.serviceWorker?.getRegistration().then(reg => {
                reg?.showNotification(title, options);
                DotNet.invokeMethodAsync('Bit.Butil', showMethod, id);
            }).catch(() => DotNet.invokeMethodAsync('Bit.Butil', errorMethod, id));
        }
    }

    function close(id: string) {
        const n = _tracked[id];
        if (!n) return;
        try { n.close(); } catch { /* already closed */ }
    }

    function dispose(id: string) {
        const n = _tracked[id];
        if (!n) return;
        delete _tracked[id];
        n.onclick = null; n.onshow = null; n.onclose = null; n.onerror = null;
        try { n.close(); } catch { /* already closed */ }
    }

}(BitButil));