// bit version: 8.12.0-pre-04

self.assetsInclude = [];
self.assetsExclude = [/\.scp\.css$/, /weather\.json$/];
self.defaultUrl = "/";
self.prohibitedUrls = [];
self.assetsUrl = '/service-worker-assets.js';
self.externalAssets = [
    //{
    //    "hash": "sha256-lDAEEaul32OkTANWkZgjgs4sFCsMdLsR5NJxrjVcXdo=",
    //    "url": "css/app.css"
    //},
    {
        "url": "/"
    },
    {
        "url": "https://www.googletagmanager.com/gtag/js?id=G-G1ET5L69QF"
    }
];

self.caseInsensitiveUrl = true;

self.serverHandledUrls = [/\/api\//];
self.serverRenderedUrls = [/\/privacy$/];

self.noPrerenderQuery = 'no-prerender=true';

self.isPassive = true;

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
