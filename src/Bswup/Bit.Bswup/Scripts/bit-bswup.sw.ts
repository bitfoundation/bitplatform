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
    enableIntegrityCheck: any
    enableDiagnostics: any
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
    diag('installing version:', VERSION);

    sendMessage({ type: 'install', data: { version: VERSION, isPassive: self.isPassive } });

    createAssetsCache();
}

async function handleActivate(e) {
    diag('activate version:', VERSION);

    await deleteOldCaches();

    sendMessage({ type: 'activate', data: { version: VERSION, isPassive: self.isPassive } });
}

// ============================================================================

const DEFAULT_URL = (typeof self.defaultUrl === 'string') ? self.defaultUrl : 'index.html';
const PROHIBITED_URLS = prepareRegExpArray(self.prohibitedUrls);
const SERVER_HANDLED_URLS = prepareRegExpArray(self.serverHandledUrls);
const SERVER_RENDERED_URLS = prepareRegExpArray(self.serverRenderedUrls);

diag('DEFAULT_URL:', DEFAULT_URL);
diag('PROHIBITED_URLS:', PROHIBITED_URLS);
diag('SERVER_HANDLED_URLS:', SERVER_HANDLED_URLS);
diag('SERVER_RENDERED_URLS:', SERVER_RENDERED_URLS);

// ==================== ASSETS ====================

const USER_ASSETS_INCLUDE = prepareRegExpArray(self.assetsInclude);
const USER_ASSETS_EXCLUDE = prepareRegExpArray(self.assetsExclude);
const EXTERNAL_ASSETS = prepareExternalAssetsArray(self.externalAssets);

diag('USER_ASSETS_INCLUDE:', USER_ASSETS_INCLUDE);
diag('USER_ASSETS_EXCLUDE:', USER_ASSETS_EXCLUDE);
diag('EXTERNAL_ASSETS:', EXTERNAL_ASSETS);

const DEFAULT_ASSETS_INCLUDE = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/];
const DEFAULT_ASSETS_EXCLUDE = [/^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/, /^service-worker\.js$/];

const ASSETS_INCLUDE = (self.ignoreDefaultInclude ? [] : DEFAULT_ASSETS_INCLUDE).concat(USER_ASSETS_INCLUDE);
const ASSETS_EXCLUDE = (self.ignoreDefaultExclude ? [] : DEFAULT_ASSETS_EXCLUDE).concat(USER_ASSETS_EXCLUDE);

diag('ASSETS_INCLUDE:', ASSETS_INCLUDE);
diag('ASSETS_EXCLUDE:', ASSETS_EXCLUDE);

const ALL_ASSETS = self.assetsManifest.assets
    .filter(asset => ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
    .filter(asset => !ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
    .concat(EXTERNAL_ASSETS);

diag('ALL_ASSETS:', ALL_ASSETS);

const UNIQUE_ASSETS = uniqueAssets(ALL_ASSETS);

diag('UNIQUE_ASSETS:', UNIQUE_ASSETS);

async function handleFetch(e) {
    diag('-------------------- handleFetch:', e);

    if (e.request.method !== 'GET' || SERVER_HANDLED_URLS.some(pattern => pattern.test(e.request.url))) {
        diag('-------------------- handleFetch ended - bypassed:', e);

        return fetch(e.request);
    }

    const isServerRendered = SERVER_RENDERED_URLS.some(pattern => pattern.test(e.request.url))
    const shouldServeIndexHtml = (e.request.mode === 'navigate' && !isServerRendered);
    const requestUrl = shouldServeIndexHtml ? DEFAULT_URL : e.request.url;

    diag('isServerRendered:', isServerRendered);
    diag('shouldServeIndexHtml:', shouldServeIndexHtml);
    diag('requestUrl:', requestUrl);


    if (PROHIBITED_URLS.some(pattern => pattern.test(requestUrl))) {
        diag('-------------------- handleFetch ended - prohibited:', requestUrl);

        return new Response(new Blob(), { status: 405, "statusText": `prohibited URL: ${requestUrl}` });
    }

    const caseMethod = self.caseInsensitiveUrl ? 'toLowerCase' : 'toString';

    diag('caseMethod:', caseMethod);

    let asset = UNIQUE_ASSETS.find(a => shouldServeIndexHtml
        ? a.url[caseMethod]() === requestUrl[caseMethod]()
        : new URL(requestUrl).pathname.endsWith(a.url)
    );

    diag('asset:', asset);

    if (!asset?.url) return fetch(e.request);

    const cacheUrl = `${asset.url}.${asset.hash || ''}`;

    diag('cacheUrl:', cacheUrl);

    const bitBswupCache = await caches.open(CACHE_NAME);
    const cachedResponse = await bitBswupCache.match(cacheUrl || requestUrl);

    if (cachedResponse || !self.isPassive) {
        diag('-------------------- handleFetch ended - ', cachedResponse ? '' : 'NOT', 'using cache.');
        return cachedResponse || fetch(e.request);
    }

    diag('passive: handling not cached asset...');

    const request = createNewAssetRequest(asset);
    const response = await fetch(request);
    bitBswupCache.put(cacheUrl, response.clone());

    diag('-------------------- handleFetch ended - passive');

    return response;
}

function handleMessage(e) {
    diag('handleMessage:', e);

    if (e.data === 'SKIP_WAITING') {
        return self.skipWaiting().then(() => sendMessage('WAITING_SKIPPED'));
    }

    if (e.data === 'CLAIM_CLIENTS') {
        return self.clients.claim().then(() => e.source.postMessage('CLIENTS_CLAIMED'));
    }

    if (e.data === 'BLAZOR_STARTED') {
        setTimeout(() => createAssetsCache(true), 1984);
    }
}

// ============================================================================

async function createAssetsCache(ignoreProgressReport = false) {
    diag('-------------------- createAssetsCache:', ignoreProgressReport);

    const bitBswupCache = await caches.open(CACHE_NAME);
    let keys = await bitBswupCache.keys();
    const firstTime = keys.length === 0;
    const passiveFirstTime = self.isPassive && firstTime

    diag('passiveFirstTime:', passiveFirstTime);

    let current = 0;
    let total = UNIQUE_ASSETS.length;

    if (passiveFirstTime) {
        const blazorBootAsset = UNIQUE_ASSETS.find(a => a.url.includes('blazor.boot.json'));
        const blazorBootJson = await (await addCache(false, blazorBootAsset)).json();
        const blazorResources = Object.keys(blazorBootJson.resources.assembly).concat(Object.keys(blazorBootJson.resources.runtime));
        const blazorAssets = blazorResources.map(r => UNIQUE_ASSETS.find(a => a.url.endsWith(`/${r}`))).filter(a => !!a);

        diag('blazorBootAsset:', blazorBootAsset);
        diag('blazorBootJson:', blazorBootJson);
        diag('blazorResources:', blazorResources);
        diag('blazorAssets:', blazorAssets);

        total = blazorAssets.length;
        const promises = blazorAssets.map(addCache.bind(null, true));

        diag('await Promise.all(promises)');

        await Promise.all(promises);

        diag('-------------------- createAssetsCache ended - passive firstTime');

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
            diag('>>>>>>>>>> removed oldUrl:', key.url);
            bitBswupCache.delete(key.url);
        } else if (hash && hash !== foundAsset.hash) {
            diag('>>>>>>>>>> updated oldUrl:', key.url);
            bitBswupCache.delete(key.url);
            updatedAssets.push(foundAsset);
        }
    }

    diag('oldUrls:', oldUrls);
    diag('updatedAssets:', updatedAssets);

    const assetsToCache = updatedAssets.concat(UNIQUE_ASSETS.filter(a => !oldUrls.find(u => u.url.endsWith(a.url))));

    diag('assetsToCache:', assetsToCache);

    total = assetsToCache.length;
    const promises = assetsToCache.map(addCache.bind(null, ignoreProgressReport ? false : true));

    diag('-------------------- createAssetsCache ended.');

    async function addCache(report, asset) {
        const request = createNewAssetRequest(asset);
        
        try {
            const responsePromise = fetch(request);
            return responsePromise.then(response => {
                if (!response.ok) {
                    diag('>>>>>>>>>> addCache - !response.ok:', request);
                    return Promise.reject(response.statusText);
                }

                const cacheUrl = `${asset.url}.${asset.hash || ''}`;
                const cachePromise = bitBswupCache.put(cacheUrl, response.clone());

                if (report) {
                    const percent = (++current) / total * 100;
                    sendMessage({ type: 'progress', data: { asset, percent, index: current } });
                }

                return cachePromise.then(() => response);
            });
        } catch (err) {
            diag('>>>>>>>>>> addCache - catch err:', err);
            return Promise.reject(err);
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
    const requestInit: RequestInit = asset.hash && self.enableIntegrityCheck
        ? { cache: 'no-store', integrity: asset.hash, headers: [['cache-control', 'public, max-age=3153600']] }
        : { cache: 'no-store', headers: [['cache-control', 'public, max-age=3153600']] };

    return new Request(assetUrl, requestInit);
    //return new Request(assetUrl, asset.hash ? { cache: 'no-cache', integrity: asset.hash } : { cache: 'no-cache' });
}

async function deleteOldCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => key.startsWith('bit-bswup-') || key.startsWith('blazor-resources-')).map(key => caches.delete(key));
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

function diag(...args: any[]) {
    if (!self.enableDiagnostics) return;

    console.info(...['bit bswup:', ...args, new Date().toISOString()]);
}