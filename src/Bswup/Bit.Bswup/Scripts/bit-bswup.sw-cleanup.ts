(self as any)['bit-bswup.sw-cleanup version'] = '10.5.0';

interface BitBswupGlobals {
    clients: any
    skipWaiting: any
    registration: any
}

interface WorkerGlobalScope extends BitBswupGlobals { }
interface Window extends BitBswupGlobals { }

// Self-destructing "uninstall" service worker. Deploy this in place of the real
// bit-bswup.sw.js when an app needs to fully back out of Bswup (e.g. switching a site away
// from offline support, or recovering clients stuck on a broken worker/cache). It takes over
// all clients, wipes every Bswup/Blazor cache once in control, and tells each one to
// unregister and reload - leaving the app running purely from the network with no SW.

// On install, only skipWaiting() so this cleanup worker activates without waiting for existing
// clients to close. All teardown work (cache purge + client notification) is deferred to the
// 'activate' handler below, once this worker is actually in control.
self.addEventListener('install', (e: any) => e.waitUntil(self.skipWaiting()));

// Take over all clients and run teardown only once this worker has *activated* - not during
// install. Doing the cache purge at activate time (after clients.claim()) guarantees the
// cleanup worker is fully in control before its caches disappear, so no controlled tab is left
// using a worker whose caches were already purged.
self.addEventListener('activate', (e: any) => e.waitUntil(teardownClients()));

// Activate-time teardown: claim every client, purge the caches this library (and Blazor)
// created, then message each (controlled or not) to unregister itself. The delayed
// 'WAITING_SKIPPED' nudge is a fallback reload signal for clients that don't act on
// 'UNREGISTER' fast enough, so no tab is left running against the now-deleted caches. Await the
// whole chain so the activate event (which waits on this via waitUntil) doesn't resolve before
// the teardown signalling has actually been dispatched.
async function teardownClients() {
    // Claim first so controlled tabs are served by this fetch-less worker (straight from the
    // network) before their caches vanish, then purge the Bswup/Blazor caches.
    await self.clients.claim();

    // Best-effort: CacheStorage can reject under storage pressure / broken origin storage -
    // the very situations this recovery worker is deployed into. The purge must never abort
    // the teardown, or no client would ever be told to UNREGISTER and every tab would stay
    // stranded on this worker forever; an unpurged cache is reclaimed by the browser once
    // the registration is gone.
    try {
        // Scope-aware purge, mirroring the main worker's cache identity (bit-bswup.sw.ts):
        // this app's own scoped buckets (`bit-bswup:<scope-path> - <version>`), legacy
        // scope-less buckets from pre-scoping releases (`bit-bswup - <version>`), and Blazor's
        // resource caches. A sibling Bswup app's scoped buckets are deliberately left alone -
        // this worker backs ONE app out of Bswup, the same boundary the client filter below
        // draws. blazor-resources stays prefix-wide: its suffix is the document base path,
        // which need not equal the SW scope, and in a recovery scenario missing a possibly
        // corrupt cache is worse than making a sibling re-fill one.
        const scopePath = (() => {
            try { return new URL(self.registration.scope).pathname; } catch { return '/'; }
        })();
        const cacheKeys = await caches.keys();
        const cachePromises = cacheKeys
            .filter(key => key.startsWith(`bit-bswup:${scopePath} - `) || key.startsWith('bit-bswup - ') || key.startsWith('blazor-resources'))
            .map(key => caches.delete(key));
        await Promise.all(cachePromises);
    } catch (err) {
        console.warn('BitBswup SW cleanup: cache purge failed (continuing with unregister):', err);
    }
    // Only target window clients that belong to this registration's scope. matchAll with
    // includeUncontrolled returns every same-origin client (including those under other
    // scopes / mounted sub-apps and non-window clients like workers); broadcasting
    // 'UNREGISTER' to all of them would tell unrelated apps to tear themselves down. Filter
    // to in-scope window clients so the reloadSignals loop only reloads this registration.
    const scope = self.registration && self.registration.scope;
    const allClients = await self.clients.matchAll({ includeUncontrolled: true });
    const clients = (allClients || []).filter((client: any) =>
        client.type === 'window' && (!scope || (typeof client.url === 'string' && client.url.indexOf(scope) === 0)));
    const reloadSignals: Promise<void>[] = [];
    (clients || []).forEach((client: any) => {
        client.postMessage('UNREGISTER');
        // Keep the activate event (waitUntil -> teardownClients) alive until the delayed
        // 'WAITING_SKIPPED' nudge has actually fired; otherwise the browser may terminate
        // this short-lived cleanup worker before the 1s timer runs and the fallback reload
        // signal would never be dispatched.
        reloadSignals.push(new Promise<void>(resolve => setTimeout(() => {
            client.postMessage('WAITING_SKIPPED');
            resolve();
        }, 1000)));
    });
    await Promise.all(reloadSignals);
}
