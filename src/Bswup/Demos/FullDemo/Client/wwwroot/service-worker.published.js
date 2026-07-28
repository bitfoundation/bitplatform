// bit version: 10.5.0

self.assetsInclude = [];
// The client's scoped-css bundle is in this app's asset manifest but is never served: in a
// Blazor Web App the host project merges the client's scoped styles into its own
// <HostAssembly>.styles.css. Precaching it would fail with a 404, so it's excluded here and
// the two files the page actually loads are precached through externalAssets instead.
self.assetsExclude = [/^Bit\.Bswup\.FullDemo\.Client\.styles\.css$/, /weather\.json$/];
self.defaultUrl = "/";
self.prohibitedUrls = [];
// self.assetsUrl is deliberately NOT set: since v-10-5-0 it defaults to a relative
// 'service-worker-assets.js', resolved next to this service-worker file - which keeps
// sub-path-mounted apps working. Set it explicitly only when the file lives elsewhere.
self.externalAssets = [
    //{
    //    "hash": "sha256-lDAEEaul32OkTANWkZgjgs4sFCsMdLsR5NJxrjVcXdo=",
    //    "url": "css/app.css"
    //},
    {
        "url": "/"
    },
    // The host's scoped-css bundle (linked from App.razor) and the client bundle it @imports.
    // Both are served by the host project, so they're absent from the client asset manifest.
    {
        "url": "Bit.Bswup.FullDemo.Server.styles.css"
    },
    {
        "url": "Bit.Bswup.FullDemo.Client.bundle.scp.css"
    },
    // A Blazor Web App boots through blazor.web.js, not the blazor.webassembly.js that the
    // client's asset manifest lists - it belongs to the host project, so the manifest never
    // sees it and the app cannot start offline without this entry.
    {
        "url": "_framework/blazor.web.js"
    },
    // blazor.web.js then loads the WASM resource list from a fingerprinted
    // resource-collection.<hash>.js, named by the host at build time. The concrete name is
    // unknown here and changes every build, so it is matched with a RegExp and cached lazily
    // on first fetch (see the externalAssets notes in the Bswup README).
    {
        "url": /\/_framework\/resource-collection\.[^\/]*\.js$/
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

//// Diagnostics knobs (see the Bswup README for details):
//self.enableDiagnostics = true;            // log install/activate/cache decisions to the console
//self.enableFetchDiagnostics = true;       // additionally log every intercepted fetch (very verbose)

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
