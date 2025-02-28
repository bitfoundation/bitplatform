﻿self.addEventListener('install', e => e.waitUntil(handleInstall(e)));

async function handleInstall(e) {
    await resetBswup();
}

async function resetBswup() {
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