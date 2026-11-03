self.addEventListener('fetch', () => { });

self.addEventListener('install', e => e.waitUntil(
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage('START_BLAZOR')))
));
