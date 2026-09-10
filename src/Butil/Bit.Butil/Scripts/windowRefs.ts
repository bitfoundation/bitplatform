var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Popups this app opened, keyed by the id .NET knows them by. Its own module because
    // windowMessaging needs nothing from the window module except the ability to resolve one of
    // these ids - and a lazy-loaded module file inlines its dependencies, so reaching into the
    // window module for it would put all of that in windowMessaging's download.
    const _refs: { [id: string]: any } = {};

    butil.windowRefs = {
        open,
        close,
        dispose,

        // For other modules (windowMessaging): the live window a popup id opened, or undefined once
        // it has been closed. A closed popup's ref is still an object, so the check is explicit.
        refOf(id: string) {
            const ref = _refs[id];
            return ref && !ref.closed ? ref : undefined;
        }
    };

    function close(id: string | undefined) {
        if (!id) { window.close(); return; }

        const ref = _refs[id];
        if (!ref) return;
        delete _refs[id];
        ref.close();
    }

    function open(id: string, url?: string, target?: string, windowFeatures?: string) {
        const ref = window.open(url, target, windowFeatures);
        if (!ref) return undefined;
        // Prune refs for popups the user closed manually. close(id) only runs on explicit
        // closes, so without this sweep those entries would linger in _refs until dispose().
        pruneClosedRefs();
        _refs[id] = ref;
        return id;
    }

    function pruneClosedRefs() {
        for (const key of Object.keys(_refs)) {
            if (_refs[key].closed) delete _refs[key];
        }
    }

    function dispose(ids?: string[]) {
        // _refs is shared across every Butil Window instance (i.e. across all Blazor Server
        // circuits and WASM apps in the module). Wiping it wholesale would orphan popups opened
        // by *other* live instances, silently turning their close(id) into a no-op. So we only
        // drop the ids this instance opened, which the C# side tracks and passes in here.
        if (!ids) return;
        ids.forEach(id => { delete _refs[id]; });
    }
}(BitButil));
