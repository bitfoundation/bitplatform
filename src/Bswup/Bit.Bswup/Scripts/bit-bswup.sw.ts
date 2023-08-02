interface Window {
    clients: any
    skipWaiting: any
    importScripts: any
    assetsManifest: any

    assetsInclude: any
    assetsExclude: any
    externalAssets: any
    defaultUrl: any
    assetsUrl: any
    prohibitedUrls: any
    caseInsensitiveUrl: any
    serverHandledUrls: any
    serverRenderedUrls: any
    noPrerenderQuery: any
    ignoreDefaultInclude: any
    ignoreDefaultExclude: any
    isPassive: any
    precachedAssetsInclude: any
    precachedAssetsExclude: any
    precachedExternalAssets: any
    ignoreDefaultPrecach: any
}

interface Event {
    waitUntil: any
    respondWith: any
}

const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : '/service-worker-assets.js';
self.importScripts(ASSETS_URL);

const VERSION = self.assetsManifest.version;
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

    await deleteOldVersionCaches();

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

    const PRE_CACHED_ASSETS_INCLUDE = (self.ignoreDefaultPrecach ? [] : DEFAULT_PRE_CACHED_ASSETS_INCLUDE).concat(USER_PRE_CACHED_ASSETS_INCLUDE);
    const PRE_CACHED_ASSETS_EXCLUDE = DEFAULT_PRE_CACHED_ASSETS_EXCLUDE.concat(USER_PRE_CACHED_ASSETS_EXCLUDE);

    PRE_CACHED_UNIQUE_ASSETS = UNIQUE_ASSETS
        .filter(asset => PRE_CACHED_ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
        .filter(asset => !PRE_CACHED_ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
        .concat(PRE_CACHED_EXTERNAL_ASSETS);
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

// ============================================================================

async function createNewCache() {
    const bitBswupCache = await caches.open(CACHE_NAME);

    //const cachedUrls = (await bitBswupCache.keys()).map(k => k?.url).filter(u => !!u);
    //const toBeRemovedUrls = cachedUrls.filter(u => !UNIQUE_ASSETS.find(a => u.endsWith(`${a.url}.${a.hash || ''}`)));
    //toBeRemovedUrls.forEach(u => bitBswupCache.delete(u));
    //if (self.isPassive) {
    //    sendMessage({ type: 'progress', data: { percent: 100 } });
    //    return PRE_CACHED_UNIQUE_ASSETS.forEach(addCache.bind(null, false));
    //}

    const keys = await bitBswupCache.keys();
    const firstTime = keys.length === 0;

    let total = 0;
    let current = 0;

    if (self.isPassive && firstTime) {
        if (PRE_CACHED_UNIQUE_ASSETS.length > 0) {
            total = PRE_CACHED_UNIQUE_ASSETS.length;
            PRE_CACHED_UNIQUE_ASSETS.forEach(addCache);
        } else {
            sendMessage({ type: 'progress', data: { percent: 100, asset: {} } });
        }
        return;
    }

    const oldUrls = [];
    const updatedAssets = [];
    for (let i = 0; i < keys.length; i++) {
        const key = keys[i];
        if (!key || !key.url) continue;

        const lastIndex = key.url.lastIndexOf('.');
        const url = lastIndex === -1 ? key.url : key.url.substring(0, lastIndex);
        const hash = lastIndex === -1 ? '' : key.url.substring(lastIndex + 1);
        oldUrls.push({ url, hash });
        const foundAsset = UNIQUE_ASSETS.find(a => url.endsWith(a.url));
        if (!foundAsset) {
            bitBswupCache.delete(key.url);
        } else if (hash && hash !== foundAsset.hash) {
            bitBswupCache.delete(key.url);
            updatedAssets.push(foundAsset);
        }
    }

    const assetsToCache = updatedAssets.concat(self.isPassive ? [] : UNIQUE_ASSETS.filter(a => !oldUrls.find(u => u.url.endsWith(a.url))));

    current = 0;
    total = assetsToCache.length;
    assetsToCache.forEach(addCache);

    async function addCache(asset) {
        const cacheUrl = `${asset.url}.${asset.hash || ''}`;
        const request = createNewAssetRequest(asset);
        try {
            const responsePromise = fetch(request);
            responsePromise.then(response => {
                if (!response.ok) {
                    return Promise.reject(response.statusText);
                }
                bitBswupCache.put(cacheUrl, response);
                const percent = (++current) / total * 100;
                sendMessage({ type: 'progress', data: { asset, percent, index: current } });
                Promise.resolve(null);
            });
            return responsePromise;
        } catch (err) {
            Promise.reject(err);
        }
    }
}

function createNewAssetRequest(asset) {
    let assetUrl;
    if (asset.url === DEFAULT_URL && self.noPrerenderQuery) {
        assetUrl = `${asset.url}?${self.noPrerenderQuery}&v=${asset.hash || self.assetsManifest.version}`;
    } else {
        assetUrl = `${asset.url}?v=${asset.hash || self.assetsManifest.version}`;
    }
    const requestInit: RequestInit = asset.hash
        ? { cache: 'no-store', integrity: asset.hash, headers: [['cache-control', 'public, max-age=3153600']] }
        : { cache: 'no-store', headers: [['cache-control', 'public, max-age=3153600']] };

    return new Request(assetUrl, requestInit);
    //return new Request(assetUrl, asset.hash ? { cache: 'no-cache', integrity: asset.hash } : { cache: 'no-cache' });
}

async function deleteOldVersionCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => key.startsWith('bit-bswup-')).map(key => caches.delete(key));
    return Promise.all(promises);
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