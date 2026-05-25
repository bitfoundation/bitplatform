self.addEventListener('install', e => {
    console.log('sw install');
    // Tell the browser to keep this worker alive until install work is done.
    // Calling skipWaiting inside waitUntil makes the new worker activate as
    // soon as install completes, without waiting for existing clients to close.
    e.waitUntil(self.skipWaiting());
});
