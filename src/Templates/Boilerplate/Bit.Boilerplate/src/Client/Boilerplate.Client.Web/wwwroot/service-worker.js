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

self.addEventListener("notificationclick", (event) => {
    console.log("On notification click: ", event.notification.tag);
    event.notification.close();

    // This looks to see if the current is already open and
    // focuses if it is
    event.waitUntil(
        clients
            .matchAll({
                type: "window",
            })
            .then((clientList) => {
                for (const client of clientList) {
                    if (client.url === "/about" && "focus" in client) return client.focus();
                }
                if (clients.openWindow) return clients.openWindow("/about");
            }),
    );
});

//#endif

self.addEventListener('install', e => e.waitUntil(
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage('START_BLAZOR')))
));