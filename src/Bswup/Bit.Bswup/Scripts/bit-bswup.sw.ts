interface Window {
    clients: any
    skipWaiting: any
    importScripts: any

    assetsUrl: any
    assetsManifest: any
    defaultUrl: any
    prohibitedUrls: any
    serverHandledUrls: any
    serverRenderedUrls: any
    caseInsensitiveUrl: any
    assetsInclude: any
    assetsExclude: any
    externalAssets: any
}

interface Event {
    waitUntil: any
    respondWith: any
}

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
    sendMessage({ type: 'installing', data: { version: VERSION } });

    createNewCache();

    log(`installed version (${VERSION})`);
    sendMessage({ type: 'installed', data: { version: VERSION } });
}

async function handleActivate(e) {
    log(`activate version (${VERSION})`);

    await deleteOldCaches();

    sendMessage({ type: 'activate', data: { version: VERSION } });
}

const DEFAULT_URL = (typeof self.defaultUrl === 'string') ? self.defaultUrl : 'index.html';
const PROHIBITED_URLS = prepareRegExpArray(self.prohibitedUrls);
const SERVER_HANDLED_URLS = prepareRegExpArray(self.serverHandledUrls);
const SERVER_RENDERED_URLS = prepareRegExpArray(self.serverRenderedUrls);

async function handleFetch(e) {
    if (e.request.method !== 'GET' || SERVER_HANDLED_URLS.some(pattern => pattern.test(e.request.url))) {
        return fetch(e.request);
    }

    const shouldServeIndexHtml = (e.request.mode === 'navigate' && !SERVER_RENDERED_URLS.some(pattern => pattern.test(e.request.url)));
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

const DEFAULT_ASSETS_INCLUDE = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/];
const DEFAULT_ASSETS_EXCLUDE = [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^_content\/Bit\.Bswup\/bit-bswup\.sw\.min\.js$/, /^service-worker\.js$/, /^service-worker\.min\.js$/];

async function createNewCache() {
    const userAssetsInclude = prepareRegExpArray(self.assetsInclude);
    const userAssetsExclude = prepareRegExpArray(self.assetsExclude);
    const externalAssets = prepareExternalAssetsArray(self.externalAssets);

    const assetsInclude = DEFAULT_ASSETS_INCLUDE.concat(userAssetsInclude);
    const assetsExclude = DEFAULT_ASSETS_EXCLUDE.concat(userAssetsExclude);

    const assets = self.assetsManifest.assets
        .filter(asset => assetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !assetsExclude.some(pattern => pattern.test(asset.url)))
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
    uniqueAssets.map(addCache);

    async function addCache(asset, index) {
        const request = new Request(asset.url, asset.hash ? { cache: 'no-cache', integrity: asset.hash } : { cache: 'no-cache' });
        const cacheUrl = `${asset.url}.${asset.hash || ''}`;

        if (oldCache && asset.hash) {
            const oldResponse = await oldCache.match(cacheUrl);
            if (oldResponse) {
                cache.put(cacheUrl, oldResponse);
                current++;
                return Promise.resolve();
            }
        }

        try {
            const responsePromise = fetch(request);
            responsePromise.then(response => {
                if (!response.ok) {
                    return Promise.reject(response.statusText);
                }
                cache.put(cacheUrl, response);
                const percent = (++current) / total * 100;
                sendMessage({ type: 'progress', data: { asset, percent, index: current } });
                Promise.resolve(null);
            });
            return responsePromise;
        } catch (err) {
            Promise.reject(err);
        }
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

function sendMessage(message) {
    self.clients
        .matchAll({ includeUncontrolled: true, type: 'window', })
        .then(function (clients) {
            (clients || []).forEach(function (client) { client.postMessage(JSON.stringify(message)); });
        });
}

function log(value) {
    //console.info('BitBSWUP:sw:', value);
}

function prepareExternalAssetsArray(value) {
    const array = value ? (value instanceof Array ? value : [value]) : [];

    return array.map(asset => {
        if (asset && asset.url) {
            return asset;
        }

        if (typeof asset === 'string') {
            return ({ url: asset });
        }

        return null;
    }).filter(asset => asset !== null);
}

function prepareRegExpArray(value) {
    return value ? (value instanceof Array ? value : [value]).filter(p => p instanceof RegExp) : [];
}