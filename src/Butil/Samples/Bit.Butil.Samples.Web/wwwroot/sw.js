// The standalone WebAssembly sample's service worker. It caches nothing: it exists so the
// registration-based APIs on the Workers page - navigation preload, the Clients API, Background
// Fetch and the Content Index - have something real to register against.

// Deliberately no self.skipWaiting() here. A first registration activates immediately anyway
// (nothing is controlling the page yet), while a worker installed over a running one waits - which
// is the only state ServiceWorker.SkipWaiting has anything to do in. Promoting every install would
// leave that member with no way to return anything but false.
self.addEventListener('install', () => {
    console.log('[sw] installed; waiting if another version is still in control');
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// The protocol Bit.Butil's ServiceWorker.SkipWaiting / Claim / MatchAllClients speak. Those three
// facilities live on the worker's global scope and cannot be reached from a page at all, so the
// page asks and the worker answers - here, over the MessageChannel port the page sent along. A
// worker without this handler leaves those three members timing out and reporting false.
self.addEventListener('message', event => {
    const command = event.data && event.data.__butil;
    if (!command) return;

    const reply = value => event.ports && event.ports[0] && event.ports[0].postMessage(value);

    if (command === 'skipWaiting') {
        self.skipWaiting();
        return;
    }

    // Every path below answers, the failing ones included. The page has no other way to learn that
    // the worker tried and could not: with no reply it waits out its whole timeout and then reads
    // the default, so a rejected call looks like "no clients" five seconds later rather than an
    // answer now. Promise.resolve().then wraps the call so an argument the browser rejects - an
    // unknown client type, say - is a rejection here rather than a throw out of the listener.
    const answer = (fallback, work) => event.waitUntil(
        Promise.resolve().then(work).then(reply, () => reply(fallback)));

    if (command === 'claim') {
        answer(false, () => self.clients.claim().then(() => true));
        return;
    }

    if (command === 'clients') {
        answer([], () => self.clients
            .matchAll({ includeUncontrolled: !!event.data.includeUncontrolled, type: event.data.type || 'window' })
            .then(clients => clients.map(client => ({
                id: client.id,
                url: client.url,
                type: client.type,
                frameType: client.frameType,
                focused: client.focused,
                visibilityState: client.visibilityState
            }))));
    }
});

// Background Fetch delivers its responses here rather than to the page: by the time a page could
// ask, the browser has usually released the records. A real app would store them.
self.addEventListener('backgroundfetchsuccess', event => {
    console.log('[sw] background fetch succeeded:', event.registration.id);
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
