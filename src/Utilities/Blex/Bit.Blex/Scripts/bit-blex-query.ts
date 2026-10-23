// Tells the query cache when the app is back: the page became visible again, or the network returned.
// Stale queries an observer watches are then refetched (BlexQueryClient.NotifyFocused/NotifyReconnected).

interface DotNetObjectReference {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

// Keyed by handle, like the storage and DevTools bridges: every importer shares this module instance, and a
// single slot would let a second client (another Blazor Server circuit) detach the first one's listeners.
const watchers = new Map<number, () => void>();
let nextHandle = 1;

export function watch(handler: DotNetObjectReference): number {
    const onVisibility = () => {
        if (document.visibilityState === 'visible') {
            // Fire and forget: an exception on the .NET side must not break the page's event loop.
            handler.invokeMethodAsync('OnFocused').catch(() => { });
        }
    };

    const onOnline = () => {
        handler.invokeMethodAsync('OnReconnected').catch(() => { });
    };

    document.addEventListener('visibilitychange', onVisibility);
    window.addEventListener('online', onOnline);

    const handle = nextHandle++;
    watchers.set(handle, () => {
        document.removeEventListener('visibilitychange', onVisibility);
        window.removeEventListener('online', onOnline);
    });
    return handle;
}

export function unwatch(handle: number): void {
    const stop = watchers.get(handle);
    if (!stop) {
        return;
    }

    watchers.delete(handle);
    stop();
}
