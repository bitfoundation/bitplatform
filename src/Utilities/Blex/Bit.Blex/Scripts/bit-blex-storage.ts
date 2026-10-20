// Thin bridge over the browser's Web Storage areas used by Blex persistence.
// `area` is "local" or "session"; failures (private mode, quota) degrade to no-ops.

type BlexStorageArea = 'local' | 'session';

interface DotNetObjectReference {
    invokeMethodAsync(methodName: string, ...args: unknown[]): Promise<unknown>;
}

function store(area: BlexStorageArea): Storage {
    return area === 'session' ? window.sessionStorage : window.localStorage;
}

export function get(area: BlexStorageArea, key: string): string | null {
    try {
        return store(area).getItem(key);
    } catch {
        return null;
    }
}

export function set(area: BlexStorageArea, key: string, value: string): void {
    // Deliberately not swallowed. A failed write (quota exhausted, private mode, storage disabled)
    // means the state the user thinks is saved is not saved; reporting it through Blex's OnError
    // lets the app say so, where a console warning nobody reads means silent data loss.
    store(area).setItem(key, value);
}

export function remove(area: BlexStorageArea, key: string): void {
    try {
        store(area).removeItem(key);
    } catch {
        // ignore
    }
}

// Cross-tab sync. The browser's `storage` event fires only in the *other* tabs sharing the origin,
// never in the one that wrote the value, so this cannot echo our own writes back at us.
// `e.key` is null when the area was cleared wholesale; that is forwarded as-is.
//
// ES modules are cached per URL, so every importer shares this module instance. Watchers are keyed
// by handle for the same reason connections are in bit-blex-devtools.ts: with a single slot, a second
// storage instance (a second Blazor Server circuit, or a scope re-created by a render-mode switch)
// silently detached the first one's listener, and that tab stopped receiving cross-tab changes with
// no error anywhere.
const watchers = new Map<number, (e: StorageEvent) => void>();
let nextWatchHandle = 1;

export function watch(area: BlexStorageArea, handler: DotNetObjectReference): number {
    // sessionStorage is per-tab, so a storage event can never concern it; watching it would only
    // deliver another tab's localStorage writes under the wrong area.
    if (area !== 'local') {
        return 0;
    }

    const listener = (e: StorageEvent) => {
        if (e.storageArea && e.storageArea !== window.localStorage) {
            return;
        }

        // Fire and forget: an exception on the .NET side must not break the page's event loop.
        handler.invokeMethodAsync('OnExternalChangeAsync', e.key).catch(() => { });
    };

    window.addEventListener('storage', listener);

    const handle = nextWatchHandle++;
    watchers.set(handle, listener);
    return handle;
}

export function unwatch(handle: number): void {
    const listener = watchers.get(handle);
    if (!listener) {
        return;
    }

    watchers.delete(handle);
    window.removeEventListener('storage', listener);
}
