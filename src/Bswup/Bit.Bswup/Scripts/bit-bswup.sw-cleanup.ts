self['bit-bswup.sw-cleanup version'] = '9.7.4-pre-02';

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
