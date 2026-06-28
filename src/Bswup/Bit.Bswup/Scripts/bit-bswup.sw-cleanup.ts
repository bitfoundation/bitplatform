(self as any)['bit-bswup.sw-cleanup version'] = '10.5.0-pre-04';

interface BitBswupGlobals {
    clients: any
    skipWaiting: any
    registration: any
}

interface WorkerGlobalScope extends BitBswupGlobals { }
interface Window extends BitBswupGlobals { }

// Self-destructing "uninstall" service worker. Deploy this in place of the real
// bit-bswup.sw.js when an app needs to fully back out of Bswup (e.g. switching a site away
// from offline support, or recovering clients stuck on a broken worker/cache). On install
// it wipes every Bswup/Blazor cache, immediately takes over all clients, and tells each one
// to unregister and reload - leaving the app running purely from the network with no SW.
self.addEventListener('install', (e: any) => e.waitUntil(removeBswup()));

// Take over all clients and signal teardown only once this worker has *activated* - not
// during install. Sending the reload signal at activate time guarantees the cleanup worker
// is fully in control before any tab is told to unregister/reload, so no client reloads
// against a half-installed worker.
self.addEventListener('activate', (e: any) => e.waitUntil(teardownClients()));

// Purges the caches this library (and Blazor) created, then activates immediately. Runs once,
// at install time. Client teardown signalling happens later, in the activate handler.
async function removeBswup() {
    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    // skipWaiting() so this cleanup worker activates without waiting for existing clients to
    // close. The actual client notification is deferred to the 'activate' event below.
    await self.skipWaiting();
}

// Activate-time teardown: claim every client, then message each (controlled or not) to
// unregister itself. The delayed 'WAITING_SKIPPED' nudge is a fallback reload signal for
// clients that don't act on 'UNREGISTER' fast enough, so no tab is left running against the
// now-deleted caches. Await the whole chain so the activate event (which waits on this via
// waitUntil) doesn't resolve before the teardown signalling has actually been dispatched.
async function teardownClients() {
    await self.clients.claim();
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
