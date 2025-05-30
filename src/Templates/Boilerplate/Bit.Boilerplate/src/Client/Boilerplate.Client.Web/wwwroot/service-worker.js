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

    if (pageUrl === null || pageUrl === undefined) {
        return;
    }

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(clientList => {
                let targetClient = null;

                // Priority 1: Find the currently focused PWA window within our scope
                // This is the ideal scenario where the user is actively using the PWA.
                targetClient = clientList.find(client =>
                    client.focused && client.url.startsWith(self.registration.scope)
                );

                // Priority 2: If no focused client, find an existing PWA window on the target URL
                // This handles cases where the PWA is open in the background on the correct page.
                if (!targetClient) {
                    targetClient = clientList.find(client =>
                        client.url === pageUrl && client.url.startsWith(self.registration.scope)
                    );
                }

                // Priority 3: If still no specific URL match, find any PWA window within our scope
                // This covers cases where the PWA is open but on a different page.
                if (!targetClient) {
                    targetClient = clientList.find(client =>
                        client.url.startsWith(self.registration.scope) && 'focus' in client
                    );
                }

                if (targetClient && targetClient.focus) {
                    return targetClient.focus().then(focusedClient => {
                        if (focusedClient && focusedClient.postMessage) {
                            focusedClient.navigate(pageUrl);
                            focusedClient.postMessage({ key: 'PUBLISH_MESSAGE', message: 'NAVIGATE_TO', payload: pageUrl });
                        } else {
                            return clients.openWindow(pageUrl);
                        }
                    });
                } else {
                    return clients.openWindow(pageUrl);
                }
            })
            .catch(error => {
                return clients.openWindow(pageUrl);
            })
    );
});
//#endif

self.addEventListener('install', e => e.waitUntil(
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage('START_BLAZOR')))
));