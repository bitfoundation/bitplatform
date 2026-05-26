self.addEventListener('install', e => {
    console.log('sw install');
    // Tell the browser to keep this worker alive until install work is done.
    // Calling skipWaiting inside waitUntil makes the new worker activate as
    // soon as install completes, without waiting for existing clients to close.
    e.waitUntil(self.skipWaiting());
});

self.addEventListener('activate', e => {
    console.log('sw activate');
    // Take control of any already-open clients (tabs) so they start using this
    // new worker immediately, without requiring a reload.
    e.waitUntil(self.clients.claim());
});
