// Minimal service worker used by the ServiceWorker / Push / BackgroundSync / BackgroundFetch /
// ContentIndex demo pages. It intentionally does no caching of its own: it only exists so
// registration-based APIs have something real to register and inspect.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// The protocol Bit.Butil's ServiceWorker.SkipWaiting / Claim / MatchAllClients speak. Those three
// facilities live on the worker's global scope and cannot be reached from a page at all, so the
// page asks and the worker answers - here, over the MessageChannel port the page sent along.
self.addEventListener('message', event => {
    const command = event.data && event.data.__butil;
    if (!command) return;

    const reply = value => event.ports && event.ports[0] && event.ports[0].postMessage(value);

    if (command === 'skipWaiting') {
        self.skipWaiting();
        return;
    }

    if (command === 'claim') {
        event.waitUntil(self.clients.claim().then(() => reply(true)));
        return;
    }

    if (command === 'clients') {
        event.waitUntil(self.clients
            .matchAll({ includeUncontrolled: !!event.data.includeUncontrolled, type: event.data.type || 'window' })
            .then(clients => reply(clients.map(client => ({
                id: client.id,
                url: client.url,
                type: client.type,
                frameType: client.frameType,
                focused: client.focused,
                visibilityState: client.visibilityState
            })))));
    }
});

self.addEventListener('sync', event => {
    console.log('[sw] background sync event:', event.tag);
});

// Background Fetch delivers its responses here rather than to the page: by the time a page could
// ask, the browser has usually released the records. A real app would store them.
self.addEventListener('backgroundfetchsuccess', event => {
    console.log('[sw] background fetch succeeded:', event.registration.id);
    event.waitUntil(event.registration.matchAll()
        .then(records => Promise.all(records.map(record => record.responseReady)))
        .then(responses => console.log('[sw] background fetch responses:', responses.length)));
});

self.addEventListener('backgroundfetchfail', event => {
    console.log('[sw] background fetch failed:', event.registration.id, event.registration.failureReason);
});

self.addEventListener('backgroundfetchabort', event => {
    console.log('[sw] background fetch aborted:', event.registration.id);
});

// Fired when the user removes a Content Index entry from the browser's own UI, which is the browser
// telling the app that its offline content is no longer being advertised.
self.addEventListener('contentdelete', event => {
    console.log('[sw] content index entry deleted:', event.id);
});

self.addEventListener('push', event => {
    const text = event.data ? event.data.text() : '(no payload)';
    console.log('[sw] push event:', text);
    event.waitUntil(self.registration.showNotification('Bit.Butil push demo', { body: text }));
});
