// Minimal service worker used by the ServiceWorker / Push / BackgroundSync / BackgroundFetch /
// ContentIndex demo pages. It exists so registration-based APIs have something real to register and
// inspect. The one cache it touches - butil-content-index - is filled by the ContentIndex page;
// the worker only refreshes and prunes what is already in there.

// Deliberately no self.skipWaiting() here. A first registration activates immediately anyway
// (nothing is controlling the page yet), while a worker installed over a running one waits - which
// is the only state ServiceWorker.SkipWaiting has anything to do in. Promoting every install would
// leave that member with no way to return anything but false on this site.
self.addEventListener('install', () => {
    console.log('[sw] installed; waiting if another version is still in control');
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// The Content Index demo's other half. Registering an entry is a claim that the URL is available
// offline, and nothing but a fetch handler can make that true: the browser navigating to an indexed
// entry with no network reaches this, and only this. Only the butil-content-index cache is
// consulted, and only for what the ContentIndex page put there - everything else goes to the
// network exactly as it would with no worker at all.
self.addEventListener('fetch', event => {
    // Navigations only. That is the request an indexed entry produces, and leaving every other
    // request unhandled keeps the rest of the site on the network path it has without a worker.
    if (event.request.mode !== 'navigate' || event.request.method !== 'GET') return;
    event.respondWith(
        caches.open('butil-content-index')
            // ignoreSearch so a query string the browser appends on its way to an indexed page
            // does not turn a hit into a miss.
            .then(cache => cache.match(event.request, { ignoreSearch: true }))
            // A cache that cannot be opened or read must not take the navigation down with it.
            // Without this, one rejected caches.open() breaks every navigation on the site - and
            // since this worker never skips waiting on its own, a fixed sw.js could not take over
            // to undo that while a tab stayed open. Any failure here falls back to the network
            // path the page would have had with no worker at all.
            .catch(() => undefined)
            .then(cached => {
                if (!cached) return fetch(event.request);

                // Cache-first, then refresh in the background: an entry that stays indexed would
                // otherwise be served as it was the day it was added forever, since nothing else
                // ever writes to this cache again. The navigation answers from the cache now; the
                // network copy replaces it for next time.
                // A redirected response is refused when a service worker hands it to a navigation,
                // so one is left out of the cache rather than stored to break the next visit.
                event.waitUntil(fetch(event.request)
                    .then(response => response.ok && !response.redirected
                        ? caches.open('butil-content-index').then(cache => cache.put(event.request, response))
                        : undefined)
                    .catch(() => undefined));

                return cached;
            }));
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
    // An entry leaving the index has to take its cached response with it, or the fetch handler
    // above goes on serving a page nothing advertises any more. The event carries only the id, so
    // the cache is reconciled against what is still indexed rather than against the URL that just
    // went away. ContentIndexPage does the same from the page side for its own delete.
    event.waitUntil(reconcileContentIndexCache());
});

// Drops every cached response whose URL is no longer in the content index. Compared without the
// query string, matching the ignoreSearch the fetch handler looks entries up with.
async function reconcileContentIndexCache() {
    if (!self.registration.index) return;

    const [cache, entries] = await Promise.all([
        caches.open('butil-content-index'),
        self.registration.index.getAll()
    ]);

    const withoutSearch = url => {
        const parsed = new URL(url, self.location.href);
        parsed.search = '';
        return parsed.href;
    };

    const indexed = new Set(entries.map(entry => withoutSearch(entry.url)));
    const requests = await cache.keys();

    await Promise.all(requests
        .filter(request => !indexed.has(withoutSearch(request.url)))
        .map(request => cache.delete(request)));
}

self.addEventListener('push', event => {
    const text = event.data ? event.data.text() : '(no payload)';
    console.log('[sw] push event:', text);
    event.waitUntil(self.registration.showNotification('Bit.Butil push demo', { body: text }));
});
