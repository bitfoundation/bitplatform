var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _watchers: { [id: string]: any } = {};

    butil.closeWatcher = {
        isSupported() { return typeof (window as any).CloseWatcher === 'function'; },
        create(dotNetRef: any, id: string, closeMethod: string, cancelMethod: string, cancellable: boolean) {
            const CW = (window as any).CloseWatcher;
            if (typeof CW !== 'function') return false;

            let watcher: any;
            try {
                watcher = new CW();
            } catch {
                // More than one watcher without a user activation between them: the browser groups
                // them so a single Escape closes them all, and rejects the extra construction.
                return false;
            }

            watcher.addEventListener('close', () => {
                delete _watchers[id];
                butil.utils.dispatch(dotNetRef, closeMethod, id);
            });

            if (cancellable) {
                // 'cancel' is the chance to keep the thing open ("you have unsaved changes"). The
                // browser only fires it while there is user activation to spend, so a close can
                // still arrive without one.
                watcher.addEventListener('cancel', (e: any) => {
                    e.preventDefault();
                    butil.utils.dispatch(dotNetRef, cancelMethod, id);
                });
            }

            _watchers[id] = watcher;
            return true;
        },
        requestClose(id: string) {
            // Goes through 'cancel' first, exactly as an Escape press would.
            try { _watchers[id]?.requestClose(); } catch { /* already closed */ }
        },
        close(id: string) {
            // Skips 'cancel' and closes.
            try { _watchers[id]?.close(); } catch { /* already closed */ }
        },
        destroy(id: string) {
            const watcher = _watchers[id];
            if (!watcher) return;
            delete _watchers[id];
            // Deactivates without firing 'close' - the right call when the dialog was dismissed by
            // some other route and the watcher would otherwise stay in the browser's close stack.
            try { watcher.destroy(); } catch { /* already destroyed */ }
        },
        disposeAll() {
            for (const id of Object.keys(_watchers)) butil.closeWatcher.destroy(id);
        }
    };
}(BitButil));
