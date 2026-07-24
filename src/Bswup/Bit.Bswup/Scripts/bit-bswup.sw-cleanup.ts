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
// from offline support, or recovering clients stuck on a broken worker/cache). On activation
// it wipes every Bswup/Blazor cache this app owns, unregisters its own registration, and
// signals in-scope tabs to detach - leaving the app running purely from the network with no
// SW. Tabs the takeover controlled reload once (their controllerchange); everything after
// that stays quiet even while the app HTML keeps re-registering this script.

// On install, only skipWaiting() so this cleanup worker activates without waiting for existing
// clients to close. All teardown work (cache purge + client notification) is deferred to the
// 'activate' handler below, once this worker is actually in control.
self.addEventListener('install', (e: any) => e.waitUntil(self.skipWaiting()));

// Run teardown only once this worker has *activated* - not during install. skipWaiting-driven
// activation is also what hands this fetch-less worker control of every tab the previous
// (real) worker controlled: per the lifecycle, the new active worker takes over controlled
// clients immediately, firing their 'controllerchange' (which is the page's reload-and-detach
// signal). clients.claim() is deliberately NOT called: claiming would additionally attach
// UNCONTROLLED pages - pages that are already running SW-free, including the very tabs that
// just reloaded to detach - re-entangling them with a worker whose whole purpose is to
// disappear, and turning every future page load into another claim/reload cycle.
self.addEventListener('activate', (e: any) => e.waitUntil(teardownClients()));

// Activate-time teardown: purge the caches this library (and Blazor) created, unregister this
// registration (the client-independent step - see below), then message each in-scope window
// client to detach itself. The delayed 'WAITING_SKIPPED' nudge is a fallback reload signal
// for clients that don't act on 'UNREGISTER' fast enough, so no tab is left running against
// the now-deleted caches. Every step is individually best-effort: this worker is deployed
// into broken-storage / wedged-client situations, and any one step failing must not stop the
// rest. Await the whole chain so the activate event (which waits on this via waitUntil)
// doesn't resolve before the teardown signalling has actually been dispatched.
async function teardownClients() {
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

    // Unregister HERE, in the worker, not only via the clients. The 'UNREGISTER' message
    // below is inherently racy: tabs the takeover just switched are already reloading on
    // their 'controllerchange' when it is posted (a navigating client silently drops
    // messages), and tabs that load later never hear it at all - teardown runs once, at
    // activate. Relying on clients alone left the registration alive indefinitely. Calling
    // registration.unregister() from the activate handler is the canonical self-destroying
    // service-worker pattern and needs no client cooperation: the registration is marked for
    // removal now and fully clears once the last client detaches. Pages that re-register
    // later (the app HTML still ships the Bswup script) just repeat this cycle silently -
    // register, activate, purge (no-op), unregister - without ever being claimed or reloaded.
    try {
        await self.registration.unregister();
    } catch (err) {
        console.warn('BitBswup SW cleanup: self-unregister failed (continuing with client notification):', err);
    }

    // Only target window clients that belong to this registration's scope. matchAll with
    // includeUncontrolled returns every same-origin client (including those under other
    // scopes / mounted sub-apps and non-window clients like workers); broadcasting
    // 'UNREGISTER' to all of them would tell unrelated apps to tear themselves down. Filter
    // to in-scope window clients so the reloadSignals loop only reloads this registration.
    const scope = self.registration && self.registration.scope;
    // Same best-effort rule as every other step: a matchAll rejection must not abort the
    // teardown (the purge and unregister above already ran; only the notification is lost).
    let allClients: any = [];
    try {
        allClients = await self.clients.matchAll({ includeUncontrolled: true });
    } catch (err) {
        console.warn('BitBswup SW cleanup: client enumeration failed (teardown already completed):', err);
    }
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
