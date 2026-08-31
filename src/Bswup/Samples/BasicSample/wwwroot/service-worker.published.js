// bit version: 10.6.0-pre-04

self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.caseInsensitiveUrl = true;

self.externalAssets = [
   {
       "url": "not-found/script.file.js"
   }
];

self.errorTolerance = 'lax';

//// Resiliency knobs (see the Bswup README for details):
//self.errorTolerance = 'strict';           // abort the install if any asset fails ('lax' = best-effort lazy-fill, the default)
//self.maxRetries = 2;                      // extra download attempts on transient failures (408/429/5xx, dropped connections)
//self.retryDelay = 300;                    // base backoff in ms between those retries (exponential, with jitter)
//self.enableIntegrityCheck = true;         // attach SRI hashes so tampered assets are rejected (requires byte-identical serving)
//self.cacheVersion = '2026.07.24-abc1234'; // pin/bump the cache bucket independently of the asset manifest

self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js');

//// Caution! Be sure you understand the caveats before publishing an application with
//// offline support. See https://aka.ms/blazor-offline-considerations

//self.importScripts('./service-worker-assets.js');
//self.addEventListener('install', event => event.waitUntil(onInstall(event)));
//self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
//self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

//const cacheNamePrefix = 'offline-cache-';
//const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
//const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
//const offlineAssetsExclude = [ /^service-worker\.js$/ ];

//async function onInstall(event) {
//    console.info('Service worker: Install');

//    // Fetch and cache all matching items from the assets manifest
//    const assetsRequests = self.assetsManifest.assets
//        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
//        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
//        .map(asset => new Request(asset.url, { integrity: asset.hash }));
//    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
//}

//async function onActivate(event) {
//    console.info('Service worker: Activate');

//    // Delete unused caches
//    const cacheKeys = await caches.keys();
//    await Promise.all(cacheKeys
//        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
//        .map(key => caches.delete(key)));
//}

//async function onFetch(event) {
//    let cachedResponse = null;
//    if (event.request.method === 'GET') {
//        // For all navigation requests, try to serve index.html from cache
//        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
//        const shouldServeIndexHtml = event.request.mode === 'navigate';

//        const request = shouldServeIndexHtml ? 'index.html' : event.request;
//        const cache = await caches.open(cacheName);
//        cachedResponse = await cache.match(request);
//    }

//    return cachedResponse || fetch(event.request);
//}
