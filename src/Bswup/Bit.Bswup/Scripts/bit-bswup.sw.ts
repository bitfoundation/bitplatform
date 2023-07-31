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
    noPrerenderQuery: any
    ignoreDefaultInclude: any
    ignoreDefaultExclude: any
    isPassive: any
    precachedAssetsInclude: any
    precachedAssetsExclude: any
    precachedExternalAssets: any
}

interface Event {
    waitUntil: any
    respondWith: any
}

const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : '/service-worker-assets.js';
self.importScripts(ASSETS_URL);

const VERSION = self.assetsManifest.version;
const CACHE_NAME_PREFIX = 'bit-bswup-';
//const CACHE_NAME = `${CACHE_NAME_PREFIX}${VERSION}`;
const CACHE_NAME = 'bit-bswup';

self.addEventListener('install', e => e.waitUntil(handleInstall(e)));
self.addEventListener('activate', e => e.waitUntil(handleActivate(e)));
self.addEventListener('fetch', e => e.respondWith(handleFetch(e)));
self.addEventListener('message', handleMessage);

async function handleInstall(e) {
    log(`installing version (${VERSION})...`);

    sendMessage({ type: 'install', data: { version: VERSION, isPassive: self.isPassive } });

    createNewCache();
}

async function handleActivate(e) {
    log(`activate version (${VERSION})`);

    sendMessage({ type: 'activate', data: { version: VERSION, isPassive: self.isPassive } });
}

// ============================================================================

const DEFAULT_URL = (typeof self.defaultUrl === 'string') ? self.defaultUrl : 'index.html';
const PROHIBITED_URLS = prepareRegExpArray(self.prohibitedUrls);
const SERVER_HANDLED_URLS = prepareRegExpArray(self.serverHandledUrls);
const SERVER_RENDERED_URLS = prepareRegExpArray(self.serverRenderedUrls);

// ==================== ASSETS ====================

const USER_ASSETS_INCLUDE = prepareRegExpArray(self.assetsInclude);
const USER_ASSETS_EXCLUDE = prepareRegExpArray(self.assetsExclude);
const EXTERNAL_ASSETS = prepareExternalAssetsArray(self.externalAssets);

const DEFAULT_ASSETS_INCLUDE = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/];
const DEFAULT_ASSETS_EXCLUDE = [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^service-worker\.js$/];

const ASSETS_INCLUDE = (self.ignoreDefaultInclude ? [] : DEFAULT_ASSETS_INCLUDE).concat(USER_ASSETS_INCLUDE);
const ASSETS_EXCLUDE = (self.ignoreDefaultExclude ? [] : DEFAULT_ASSETS_EXCLUDE).concat(USER_ASSETS_EXCLUDE);

const ALL_ASSETS = self.assetsManifest.assets
    .filter(asset => ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
    .filter(asset => !ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
    .concat(EXTERNAL_ASSETS);

const UNIQUE_ASSETS = uniqueAssets(ALL_ASSETS);

// ==================== PRE CACHED ====================

let PRE_CACHED_UNIQUE_ASSETS = [];

if (self.isPassive) {
    const USER_PRE_CACHED_ASSETS_INCLUDE = prepareRegExpArray(self.precachedAssetsInclude);
    const USER_PRE_CACHED_ASSETS_EXCLUDE = prepareRegExpArray(self.precachedAssetsExclude);
    const PRE_CACHED_EXTERNAL_ASSETS = prepareExternalAssetsArray(self.precachedExternalAssets);

    const DEFAULT_PRE_CACHED_ASSETS_INCLUDE = [new RegExp(`${DEFAULT_URL}$`), /manifest\.json$/, /blazor\.webassembly\.js$/, /bit-bswup\.js$/];
    const DEFAULT_PRE_CACHED_ASSETS_EXCLUDE = [];

    const PRE_CACHED_ASSETS_INCLUDE = DEFAULT_PRE_CACHED_ASSETS_INCLUDE.concat(USER_PRE_CACHED_ASSETS_INCLUDE);
    const PRE_CACHED_ASSETS_EXCLUDE = DEFAULT_PRE_CACHED_ASSETS_EXCLUDE.concat(USER_PRE_CACHED_ASSETS_EXCLUDE);

    const ALL_PRE_CACHED_ASSETS = self.assetsManifest.assets
        .filter(asset => PRE_CACHED_ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
        .filter(asset => !PRE_CACHED_ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
        .concat(PRE_CACHED_EXTERNAL_ASSETS);

    PRE_CACHED_UNIQUE_ASSETS = uniqueAssets(ALL_PRE_CACHED_ASSETS);
}

async function handleFetch(e) {
    if (e.request.method !== 'GET' || SERVER_HANDLED_URLS.some(pattern => pattern.test(e.request.url))) {
        return fetch(e.request);
    }

    const isServerRendered = SERVER_RENDERED_URLS.some(pattern => pattern.test(e.request.url))
    const shouldServeIndexHtml = (e.request.mode === 'navigate' && !isServerRendered);
    const requestUrl = shouldServeIndexHtml ? DEFAULT_URL : e.request.url;

    if (PROHIBITED_URLS.some(pattern => pattern.test(requestUrl))) {
        return new Response(new Blob(), { status: 405, "statusText": `prohibited URL: ${requestUrl}` });
    }

    const caseMethod = self.caseInsensitiveUrl ? 'toLowerCase' : 'toString';

    let asset = UNIQUE_ASSETS.find(a => shouldServeIndexHtml
        ? a.url[caseMethod]() === requestUrl[caseMethod]()
        : new URL(requestUrl).pathname.endsWith(a.url)
    );

    if (!asset) {
        asset = EXTERNAL_ASSETS.find(a => a.url[caseMethod]() === requestUrl[caseMethod]());
    }

    const cacheUrl = asset && `${asset.url}.${asset.hash || ''}`;

    const bitBswupCache = await caches.open(CACHE_NAME);
    const cachedResponse = await bitBswupCache.match(cacheUrl || requestUrl);

    if (!self.isPassive) return cachedResponse || fetch(e.request);

    if (cachedResponse) return cachedResponse;

    if (!asset?.url) return fetch(e.request);

    const request = createNewAssetRequest(asset);
    const response = await fetch(request);
    bitBswupCache.put(cacheUrl, response.clone());

    return response;
}

function handleMessage(e) {
    if (e.data === 'SKIP_WAITING') {
        self.skipWaiting().then(() => sendMessage('WAITING_SKIPPED'));
    }

    if (e.data === 'CLAIM_CLIENTS') {
        self.clients.claim().then(() => e.source.postMessage('CLIENTS_CLAIMED'));
    }
}

// ====================================================================================

async function createNewCache() {
    const bitBswupCache = await caches.open(CACHE_NAME);

    const cachedUrls = (await bitBswupCache.keys()).map(k => k?.url).filter(u => !!u);
    const toBeRemovedUrls = cachedUrls.filter(u => !UNIQUE_ASSETS.find(a => u.endsWith(`${a.url}.${a.hash || ''}`)));
    toBeRemovedUrls.forEach(u => bitBswupCache.delete(u));

    if (self.isPassive) {
        sendMessage('PASSIVE_READY');
        return PRE_CACHED_UNIQUE_ASSETS.forEach(addCache.bind(null, false));
    }

    let current = 0;
    const total = UNIQUE_ASSETS.length;
    UNIQUE_ASSETS.forEach(addCache.bind(null, true));

    async function addCache(report, asset) {
        const cacheUrl = `${asset.url}.${asset.hash || ''}`;

        const oldResponse = await bitBswupCache.match(cacheUrl);
        if (oldResponse) {
            current++;
            return Promise.resolve();
        }

        const request = createNewAssetRequest(asset);
        try {
            const responsePromise = fetch(request);
            responsePromise.then(response => {
                if (!response.ok) {
                    return Promise.reject(response.statusText);
                }
                bitBswupCache.put(cacheUrl, response);
                if (report) {
                    const percent = (++current) / total * 100;
                    sendMessage({ type: 'progress', data: { asset, percent, index: current } });
                }
                Promise.resolve(null);
            });
            return responsePromise;
        } catch (err) {
            Promise.reject(err);
        }
    }
}

function createNewAssetRequest(asset) {
    let assetUrl = asset.url;
    if (asset.url === DEFAULT_URL && self.noPrerenderQuery) {
        assetUrl = asset.url + '?' + self.noPrerenderQuery;
    }
    return new Request(asset.url, asset.hash ? { cache: 'no-cache', integrity: asset.hash } : { cache: 'no-cache' });
}

function uniqueAssets(assets) {
    const unique = {};
    const distinct = [];
    for (let i = 0; i < assets.length; i++) {
        if (unique[assets[i].url]) continue;
        distinct.push(assets[i]);
        unique[assets[i].url] = 1;
    }
    return distinct;
}

function unique(array) {
    const uniques = {};
    const distinct = [];
    for (let i = 0; i < array.length; i++) {
        if (uniques[array[i]]) continue;
        distinct.push(array[i]);
        uniques[array[i]] = 1;
    }
    return distinct;
}

async function deleteOldCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => key.startsWith(CACHE_NAME_PREFIX) && key !== CACHE_NAME).map(key => caches.delete(key));
    return Promise.all(promises);
}

function sendMessage(message) {
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then(clients => (clients || []).forEach(client => client.postMessage(typeof message === 'string' ? message : JSON.stringify(message))));
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