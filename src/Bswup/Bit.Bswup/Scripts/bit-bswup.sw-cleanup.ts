(self as any)['bit-bswup.sw-cleanup version'] = '10.5.0-pre-01';

// Self-destructing "uninstall" service worker. Deploy this in place of the real
// bit-bswup.sw.js when an app needs to fully back out of Bswup (e.g. switching a site away
// from offline support, or recovering clients stuck on a broken worker/cache). On install
// it wipes every Bswup/Blazor cache, immediately takes over all clients, and tells each one
// to unregister and reload - leaving the app running purely from the network with no SW.
self.addEventListener('install', e => e.waitUntil(removeBswup()));

// Purges the caches this library (and Blazor) created, then activates immediately and
// signals every open client to tear down. Runs once, at install time.
async function removeBswup() {
    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    // skipWaiting() so this cleanup worker activates without waiting for existing clients to
    // close, then message every client (controlled or not) to unregister itself. The delayed
    // 'WAITING_SKIPPED' nudge is a fallback reload signal for clients that don't act on
    // 'UNREGISTER' fast enough, so no tab is left running against the now-deleted caches.
    self.skipWaiting().then(() => self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => {
            client.postMessage('UNREGISTER');
            setTimeout(() => client.postMessage('WAITING_SKIPPED'), 1000);
        })));
}
