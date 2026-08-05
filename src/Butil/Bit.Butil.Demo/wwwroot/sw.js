// Minimal service worker used by the ServiceWorker / Push / BackgroundSync demo pages.
// It intentionally does no caching: it only exists so registration-based APIs have
// something real to register and inspect.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('sync', event => {
    console.log('[sw] background sync event:', event.tag);
});

self.addEventListener('push', event => {
    const text = event.data ? event.data.text() : '(no payload)';
    console.log('[sw] push event:', text);
    event.waitUntil(self.registration.showNotification('Bit.Butil push demo', { body: text }));
});
