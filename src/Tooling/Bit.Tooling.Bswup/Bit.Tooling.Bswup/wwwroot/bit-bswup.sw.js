const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : '/service-worker-assets.js';
self.importScripts(ASSETS_URL);

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

const DEFAULT_URL = (typeof self.defaultUrl === 'string') ? self.defaultUrl : 'index.html';
const PROHIBITED_URLS = self.prohibitedUrls
    ? (self.prohibitedUrls instanceof Array
        ? self.prohibitedUrls
        : [self.prohibitedUrls]).filter(pattern => pattern instanceof RegExp)
    : [];

const SERVER_HANDLED_URLS = self.serverHandledUrls
    ? (self.serverHandledUrls instanceof Array
        ? self.serverHandledUrls
        : [self.serverHandledUrls]).filter(pattern => pattern instanceof RegExp)
    : [];
async function handleFetch(e) {
    if (e.request.method !== 'GET' || SERVER_HANDLED_URLS.some(pattern => pattern.test(e.request.url))) {
        return fetch(e.request);
    }

    // For all navigation requests, try to serve index.html from cache
    // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
    const shouldServeIndexHtml = e.request.mode === 'navigate';
    const requestUrl = shouldServeIndexHtml ? DEFAULT_URL : e.request.url;

    if (PROHIBITED_URLS.some(pattern => pattern.test(requestUrl))) {
        return new Response(new Blob(), { status: 405, "statusText": `prohibited URL: ${requestUrl}` });
    }

    const caseMethod = self.caseInsensitiveUrl ? 'toLowerCase' : 'toString';
    const asset = self.assetsManifest.assets.find(a => shouldServeIndexHtml
        ? a.url[caseMethod]() === requestUrl[caseMethod]()
        : new URL(requestUrl).pathname.endsWith(a.url));
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
    const userAssetsInclude = self.assetsInclude
        ? (self.assetsInclude instanceof Array
            ? self.assetsInclude
            : [self.assetsInclude])
        : [];

    const userAssetsExclude = self.assetsExclude
        ? (self.assetsExclude instanceof Array
            ? self.assetsExclude
            : [self.assetsExclude])
        : [];

    const externalAssets = (
        self.externalAssets
            ? (self.externalAssets instanceof Array
                ? self.externalAssets
                : [self.externalAssets])
            : [])
        .map(asset => {
            if (asset && asset.url) {
                return asset;
            }
            if (typeof asset === 'string') {
                return ({ url: asset });
            }
            return null;
        })
        .filter(asset => asset !== null);

    // ================================================

    const assetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/]
        .concat(userAssetsInclude).filter(pattern => pattern instanceof RegExp);
    const assetsExclude = [/^_content\/Bit.Tooling.Bswup\/bit-bswup.sw.js$/, /^service-worker\.js$/]
        .concat(userAssetsExclude).filter(pattern => pattern instanceof RegExp);

    const assets = self.assetsManifest.assets
        .filter(asset => assetsInclude.some(pattern => {
            try {
                return pattern.test(asset.url);
            } catch (e) {
                return false;
            }
        }))
        .filter(asset => !assetsExclude.some(pattern => {
            try {
                return pattern.test(asset.url);
            } catch (e) {
                return false;
            }
        }))
        .concat(externalAssets);
    const uniqueAssets = distinct(assets);

    let current = 0;
    const total = uniqueAssets.length;

    const cacheKeys = await caches.keys();
    const oldCacheKey = cacheKeys.find(key => key.startsWith(CACHE_NAME_PREFIX));
    let oldCache;
    if (oldCacheKey) {
        oldCache = await caches.open(oldCacheKey);
    }

    const cache = await caches.open(CACHE_NAME);
    return Promise.all(uniqueAssets.map(addCache));

    async function addCache(asset, index) {
        const request = new Request(asset.url, asset.hash ? { integrity: asset.hash } : {});
        const cacheUrl = `${asset.url}.${asset.hash || ''}`;

        if (oldCache && asset.hash) {
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

    function distinct(assets) {
        const unique = {};
        const distinct = [];
        for (let i = 0; i < assets.length; i++) {
            if (unique[assets[i].url]) continue;
            distinct.push(assets[i]);
            unique[assets[i].url] = 1;
        }
        return distinct;
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