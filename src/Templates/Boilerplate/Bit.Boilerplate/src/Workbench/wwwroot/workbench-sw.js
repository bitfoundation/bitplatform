// The service worker of reply notifications: a click focuses the tab already on the reply's page, or opens that page.
self.addEventListener('notificationclick', event => {
    event.notification.close();

    const target = new URL(event.notification.data ?? '/', self.location.origin);

    // The views inside a page are window clients too, so only tabs are matched, by the page they show.
    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windows => {
        const tab = windows.find(client => client.frameType === 'top-level' && new URL(client.url).pathname === target.pathname);

        return tab ? tab.focus() : self.clients.openWindow(target.href);
    }));
});
