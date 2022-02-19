self.importScripts(self.assetsUrl || '/service-worker-assets.js');

const VERSION = self.assetsManifest.version;
const CACHE_NAME_PREFIX = 'bit-bswup-';
const CACHE_NAME = `${CACHE_NAME_PREFIX}${VERSION}`;

self.addEventListener('install', e => e.waitUntil(handleInstall(e)));
self.addEventListener('activate', e => e.waitUntil(handleActivate(e)));
self.addEventListener('fetch', e => e.respondWith(handleFetch(e)));
self.addEventListener('message', handleMessage);

async function handleInstall(e) {
    log(`installing version (${VERSION})...`);
    postMessage({ type: 'installing', data: { version: VERSION } });

    await createNewCache();

    log(`installed version (${VERSION})`);
    postMessage({ type: 'installed', data: { version: VERSION } });
}

async function handleActivate(e) {
    log(`activate version (${VERSION})`);

    await deleteOldCaches();

    postMessage({ type: 'activate', data: { version: VERSION } });
}

async function handleFetch(e) {
    if (e.request.method !== 'GET') return fetch(e.request);

    // For all navigation requests, try to serve index.html from cache
    // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
    const shouldServeIndexHtml = e.request.mode === 'navigate';
    const requestUrl = shouldServeIndexHtml ? (self.defaultUrl || 'index.html') : e.request.url;

    if ((self.prohibitedUrls || []).some(url => url.test(requestUrl))) {
        return new Response(new Blob(), { status: 405, "statusText": `prohibited URL: ${requestUrl}` });
    }

    const asset = self.assetsManifest.assets.find(a => shouldServeIndexHtml ? a.url === requestUrl : new URL(requestUrl).pathname.endsWith(a.url));
    const cacheUrl = asset && `${asset.url}.${asset.hash || ''}`;

    const cache = await caches.open(CACHE_NAME);
    const cachedResponse = await cache.match(cacheUrl || requestUrl);

    return cachedResponse || fetch(e.request);
}

function handleMessage(e) {
    if (e.data == 'SKIP_WAITING') {
        self.skipWaiting();
    }
}

// ============================================================================

async function createNewCache() {
    const assetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/]
        .concat(self.assetsInclude || []);
    const assetsExclude = [/^_content\/Bit.Tooling.Bswup\/bit-bswup.sw.js$/, /^service-worker\.js$/]
        .concat(self.assetsExclude || []);

    const assets = self.assetsManifest.assets
        .filter(asset => assetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !assetsExclude.some(pattern => pattern.test(asset.url)))
        .concat(self.externalAssets || []);

    let current = 0;
    const total = assets.length;

    const cacheKeys = await caches.keys();
    const oldCacheKey = cacheKeys.find(key => key.startsWith(CACHE_NAME_PREFIX));
    let oldCache;
    if (oldCacheKey) {
        oldCache = await caches.open(oldCacheKey);
    }

    const cache = await caches.open(CACHE_NAME);
    return Promise.all(assets.map(addCache));

    async function addCache(asset, index) {
        const request = new Request(asset.url, asset.hash ? { integrity: asset.hash } : {});
        const cacheUrl = `${asset.url}.${asset.hash || ''}`;
        if (oldCache) {
            const oldResponse = await oldCache.match(cacheUrl);
            if (oldResponse) {
                await cache.put(cacheUrl, oldResponse);
                current++;
                return Promise.resolve();
            }
        }
        return new Promise((resolve, reject) => {
            setTimeout(async () => {
                try {
                    const response = await fetch(request);
                    if (!response.ok) throw new TypeError('Bad response status');
                    await cache.put(cacheUrl, response);
                    const percent = (++current) / total * 100;
                    postMessage({ type: 'progress', data: { asset, percent, index: current } });
                    resolve();
                } catch (err) {
                    reject(err);
                }
            }, 1.000 * (index + 1));
        });
    }
}

async function deleteOldCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => key.startsWith(CACHE_NAME_PREFIX) && key !== CACHE_NAME).map(key => caches.delete(key));
    return Promise.all(promises);
}

function postMessage(message) {
    self.clients
        .matchAll({ includeUncontrolled: true, type: 'window', })
        .then(function (clients) {
            (clients || []).forEach(function (client) { client.postMessage(JSON.stringify(message)); });
        });
}

function log(value) {
    //console.info('BitBSWUP:sw:', value);
}