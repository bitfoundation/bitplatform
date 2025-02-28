self['bit-bp.sw-cleanup version'] = '9.5.1';

self.addEventListener('install', e => e.waitUntil(handleInstall(e)));

async function handleInstall(e) {
    await removeBswup();
}

async function removeBswup() {
    const cacheKeys = await caches.keys();
    const cachePromises = cacheKeys.filter(key => key.startsWith('bit-bswup') || key.startsWith('blazor-resources')).map(key => caches.delete(key));
    await Promise.all(cachePromises);

    self.skipWaiting().then(() => self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => {
            client.postMessage('UNREGISTER');
            client.postMessage('WAITING_SKIPPED');
        })));
}