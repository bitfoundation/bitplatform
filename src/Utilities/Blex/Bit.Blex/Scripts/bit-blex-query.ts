// Tells the query cache when the app is back: the page became visible again, or the network returned.
// Stale queries an observer watches are then refetched (BlexQueryClient.NotifyFocused/NotifyReconnected).
// Where requests leave from this device (WebAssembly, Hybrid), it also reports the network going away, so
// requests wait for it instead of failing (BlexQueryClient.NotifyOffline). Like TanStack Query v5, it starts
// online and follows the events: navigator.onLine is unreliable in some browsers.
// With SyncAcrossTabs, it also carries key invalidations between the app's tabs over a BroadcastChannel.

interface DotNetObjectReference {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

interface Watcher {
    stop: () => void;
    channel: BroadcastChannel | null;
}

// Keyed by handle, like the storage and DevTools bridges: every importer shares this module instance, and a
// single slot would let a second client (another Blazor Server circuit) detach the first one's listeners.
const watchers = new Map<number, Watcher>();
let nextHandle = 1;

export function watch(handler: DotNetObjectReference, reportOffline: boolean, channelName: string | null): number {
    const onVisibility = () => {
        if (document.visibilityState === 'visible') {
            // Fire and forget: an exception on the .NET side must not break the page's event loop.
            handler.invokeMethodAsync('OnFocused').catch(() => { });
        }
    };

    const onOnline = () => {
        handler.invokeMethodAsync('OnReconnected').catch(() => { });
    };

    const onOffline = () => {
        handler.invokeMethodAsync('OnOffline').catch(() => { });
    };

    // Back from the back/forward cache: an "online" event that came while the page was frozen is not replayed.
    // Only a positive reading is trusted -- navigator.onLine can say false while online.
    const onPageShow = (event: PageTransitionEvent) => {
        if (event.persisted && navigator.onLine) {
            onOnline();
        }
    };

    document.addEventListener('visibilitychange', onVisibility);
    window.addEventListener('online', onOnline);
    if (reportOffline) {
        window.addEventListener('offline', onOffline);
        window.addEventListener('pageshow', onPageShow);
    }

    // Another tab's channel object of the same name receives what this one posts; this one does not.
    let channel: BroadcastChannel | null = null;
    if (channelName && typeof BroadcastChannel !== 'undefined') {
        channel = new BroadcastChannel(channelName);
        channel.onmessage = (event: MessageEvent) => {
            const message = event.data;
            if (message && message.kind === 'invalidate' && typeof message.key === 'string') {
                handler.invokeMethodAsync('OnInvalidatedElsewhere', message.key, message.exact === true).catch(() => { });
            }
        };
    }

    const handle = nextHandle++;
    watchers.set(handle, {
        channel,
        stop: () => {
            document.removeEventListener('visibilitychange', onVisibility);
            window.removeEventListener('online', onOnline);
            window.removeEventListener('offline', onOffline);
            window.removeEventListener('pageshow', onPageShow);
            channel?.close();
        },
    });
    return handle;
}

export function broadcastInvalidation(handle: number, key: string, exact: boolean): void {
    watchers.get(handle)?.channel?.postMessage({ kind: 'invalidate', key, exact });
}

export function unwatch(handle: number): void {
    const watcher = watchers.get(handle);
    if (!watcher) {
        return;
    }

    watchers.delete(handle);
    watcher.stop();
}
