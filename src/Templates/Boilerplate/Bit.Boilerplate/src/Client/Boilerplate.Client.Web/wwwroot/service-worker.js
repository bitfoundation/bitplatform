// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

self.addEventListener('push', function (event) {

    const data = event.data.json();

    self.registration.showNotification(data.notification.title, {

        body: data.notification.body,

        icon: '/images/icons/bit-icon-512.png'

    });

});