//+:cnd:noEmit
// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

//#if (notification == true)

self.addEventListener('push', function (event) {

    const eventData = event.data.json();

    self.registration.showNotification(eventData.title, {

        data: eventData.data,
        body: eventData.message,
        icon: '/images/icons/bit-icon-512.png'

    });

});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const pageUrl = event.notification.data.pageUrl;
    if (pageUrl != null) {
        event.waitUntil(
            clients
                .matchAll({
                    type: 'window',
                    includeUncontrolled: true,
                })
                .then((clientList) => {
                    return new Promise((resolve) => {
                        setTimeout(() => {
                            for (const client of clientList) {
                                if (!client.focus || !client.postMessage) continue;
                                client.postMessage({ key: 'PUBLISH_MESSAGE', message: 'NAVIGATE_TO', payload: pageUrl });
                                resolve(client.focus());
                                return;
                            }
                            resolve(clients.openWindow(pageUrl));
                        }, 3000 /* https://bugs.webkit.org/show_bug.cgi?id=268797#c20 */);
                    });
                })
        );
    }
});

//#endif

self.addEventListener('install', e => e.waitUntil(
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage('START_BLAZOR')))
));