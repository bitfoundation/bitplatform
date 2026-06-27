var BitButil = BitButil || {};

(function (butil: any) {
    const _observers: { [id: string]: any } = {};

    butil.reporting = {
        isSupported() { return 'ReportingObserver' in window; },
        observe(dotNetRef: any, listenerId: string, types: string[] | null, buffered: boolean) {
            const W = window as any;
            if (typeof W.ReportingObserver !== 'function') return;
            const options: any = { buffered };
            if (types?.length) options.types = types;
            const observer = new W.ReportingObserver((reports: any[]) => {
                const payload = reports.map(r => ({
                    type: r.type,
                    url: r.url,
                    body: r.body ?? null
                }));
                butil.utils.dispatch(dotNetRef, 'InvokeBrowserReport', listenerId, payload);
            }, options);
            try { observer.observe(); _observers[listenerId] = observer; }
            catch { /* invalid options - silently ignore */ }
        },
        disconnect(listenerId: string) {
            const o = _observers[listenerId];
            if (!o) return;
            delete _observers[listenerId];
            try { o.disconnect(); } catch { /* already disconnected */ }
        }
    };
}(BitButil));
