// In development every request goes to the network, so a change shows on the next load
self.addEventListener('fetch', () => { });

self.addEventListener('install', e => e.waitUntil(
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage('START_BLAZOR')))
));
