self.addEventListener('notificationclick', event => {
    event.notification.close();

    event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windows =>
        windows.length > 0 ? windows[0].focus() : self.clients.openWindow(event.notification.data ?? '/')));
});
