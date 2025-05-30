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
                    type: 'window'
                })
                .then((clientList) => {
                    for (const client of clientList) {
                        if (!client.focus || !client.navigate) continue;
                        client.navigate(pageUrl);
                        return client.focus();
                    }
                    return clients.openWindow(pageUrl);
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