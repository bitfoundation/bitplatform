self['bit-bswup.sw-cleanup version'] = '10.0.0-pre-07';

self.addEventListener('install', e => e.waitUntil(removeBswup()));

async function removeBswup() {
    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    self.skipWaiting().then(() => self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => {
            client.postMessage('UNREGISTER');
            setTimeout(() => client.postMessage('WAITING_SKIPPED'), 1000);
        })));
}
