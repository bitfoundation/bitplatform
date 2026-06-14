(self as any)['bit-bswup.sw-cleanup version'] = '10.4.5';

self.addEventListener('install', (e: ExtendableEvent) => e.waitUntil(removeBswup()));

async function removeBswup() {
    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    // Keep the whole teardown inside the install waitUntil lifetime: skip waiting, then
    // notify every client to unregister + reload. The previous version posted
    // 'WAITING_SKIPPED' from a detached setTimeout(1000) that ran outside waitUntil - the
    // worker could be terminated before it fired, so the message was unreliable. 'UNREGISTER'
    // already triggers a reload on the page (see bit-bswup.ts > handleMessage), so the delayed
    // 'WAITING_SKIPPED' was redundant; we drop it and post a single deterministic message.
    await self.skipWaiting();
    const clients = await self.clients.matchAll({ includeUncontrolled: true });
    (clients || []).forEach(client => client.postMessage('UNREGISTER'));
}
