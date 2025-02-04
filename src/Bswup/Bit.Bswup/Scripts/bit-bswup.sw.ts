self['bit-bswup.sw version'] = '9.4.0-pre-03';

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
    disablePassiveFirstBoot: any
    enableIntegrityCheck: any
    errorTolerance: any
    enableDiagnostics: any
    enableFetchDiagnostics: any
    disableHashlessAssetsUpdate: any
}

interface Event {
    waitUntil: any
    respondWith: any
}

diagGroup('bit-bswup');

const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : '/service-worker-assets.js';

diag('ASSETS_URL:', ASSETS_URL);

self.importScripts(ASSETS_URL);

const VERSION = self.assetsManifest.version;
const CACHE_NAME_PREFIX = 'bit-bswup';
const CACHE_NAME = `${CACHE_NAME_PREFIX} - ${VERSION}`;

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

    //await deleteOldCaches();

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

const DEFAULT_ASSETS_INCLUDE = [/\.dll$/, /\.wasm/, /\.pdb/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.woff2$/, /\.ttf$/, /\.webp$/];
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

diagGroupEnd();

async function handleFetch(e) {
    const req = e.request as Request;

    if (req.method !== 'GET' || SERVER_HANDLED_URLS.some(pattern => pattern.test(req.url))) {
        diagFetch('*** handleFetch ended - skipped:', e, req);
        return fetch(req);
    }

    const isServerRendered = SERVER_RENDERED_URLS.some(pattern => pattern.test(req.url))
    const shouldServeIndexHtml = (req.mode === 'navigate' && !isServerRendered);
    const requestUrl = shouldServeIndexHtml ? DEFAULT_URL : req.url;

    const start = new Date().toISOString();

    if (PROHIBITED_URLS.some(pattern => pattern.test(requestUrl))) {
        diagFetch('+++ handleFetch ended - prohibited:', start, requestUrl, e, req);

        return new Response(new Blob(), { status: 405, "statusText": `prohibited URL: ${requestUrl}` });
    }

    const caseMethod = self.caseInsensitiveUrl ? 'toLowerCase' : 'toString';

    // the assets url are only the pathname part of the actual request url!
    // since only the index url is simple and other urls have extra thins in them(like 'https://...`)
    let asset = UNIQUE_ASSETS.find(a => a[shouldServeIndexHtml ? 'url' : 'reqUrl'][caseMethod]() === requestUrl[caseMethod]());

    if (!asset) { // for assets that has asp-append-version or similar type of url versioning
        try {
            const url = new URL(requestUrl);
            const reqUrl = `${url.origin}${url.pathname}`;
            asset = UNIQUE_ASSETS.find(a => a.reqUrl[caseMethod]() === reqUrl[caseMethod]());
        } catch { }
    }

    if (!asset?.url) {
        diagFetch('+++ handleFetch ended - asset not found:', start, asset, requestUrl, e, req);

        return fetch(req);
    }

    const cacheUrl = createCacheUrl(asset);

    const bitBswupCache = await caches.open(CACHE_NAME);
    const cachedResponse = await bitBswupCache.match(cacheUrl || requestUrl);

    if (cachedResponse || !self.isPassive) {
        diagFetch('+++ handleFetch ended - ', cachedResponse ? '' : 'NOT', 'using cache.', start, asset);

        return cachedResponse || fetch(req);
    }

    const request = createNewAssetRequest(asset);
    const response = await fetch(request);
    bitBswupCache.put(cacheUrl, response.clone());

    diagFetch('+++ handleFetch ended - passive saving asset:', start, asset, e, req);

    return response;
}

function handleMessage(e) {
    diag('handleMessage:', e);

    if (e.data === 'SKIP_WAITING') {
        deleteOldCaches(); // remove the old caches when the new sw skips waiting
        return self.skipWaiting().then(() => sendMessage('WAITING_SKIPPED'));
    }

    if (e.data === 'CLAIM_CLIENTS') {
        deleteOldCaches(); // remove the old caches when the new sw claims all clients
        return self.clients.claim().then(() => e.source.postMessage('CLIENTS_CLAIMED'));
    }

    if (e.data === 'BLAZOR_STARTED') {
        createAssetsCache(true);
    }
}

// ============================================================================

async function createAssetsCache(ignoreProgressReport = false) {
    diagGroup('bit-bswup:createAssetsCache:' + ignoreProgressReport);

    const newCache = await caches.open(CACHE_NAME);
    const cacheKeys = await caches.keys();

    if (!ignoreProgressReport) {
        const oldCacheKey = cacheKeys.find(key => key.startsWith(CACHE_NAME_PREFIX));
        if (oldCacheKey) {
            diag('copying old cache:', oldCacheKey);
            const oldCache = await caches.open(oldCacheKey);
            const oldKeys = await oldCache.keys();
            for (var i = 0; i < oldKeys.length; i++) {
                const oldKey = oldKeys[i];
                if (!oldKey || !oldKey.url) continue;

                const oldRes = await oldCache.match(oldKey.url);
                await newCache.put(oldKey.url, oldRes);
            }
        }
    }

    let newCacheKeys = await newCache.keys();
    const firstTime = newCacheKeys.length === 0;
    const passiveFirstTime = self.isPassive && firstTime
    if (passiveFirstTime && self.disablePassiveFirstBoot) {
        if (!ignoreProgressReport) {
            sendMessage({ type: 'bypass', data: { firstTime: true } });
        }
        return;
    }

    diag('passiveFirstTime:', passiveFirstTime);

    let current = 0;
    let total = UNIQUE_ASSETS.length;

    if (passiveFirstTime) {
        const blazorBootAsset = UNIQUE_ASSETS.find(a => a.url.includes('blazor.boot.json'));
        const blazorBootJson = await (await addCache(false, blazorBootAsset)).json();
        const blazorResources = Object.keys(blazorBootJson.resources.assembly)
            .concat(Object.keys(blazorBootJson.resources.runtime || {})) // before .NET 8
            .concat(Object.keys(blazorBootJson.resources.jsModuleNative || {})) // after .NET 8
            .concat(Object.keys(blazorBootJson.resources.jsModuleRuntime || {}))
            .concat(Object.keys(blazorBootJson.resources.wasmNative || {}))
            .concat(Object.keys(blazorBootJson.resources.coreAssembly || {})) // after .NET 9
            .concat(Object.keys(blazorBootJson.resources.icu || {}))
            .concat(Object.keys(blazorBootJson.resources.jsModuleGlobalization || {}));
        const blazorAssets = blazorResources.map(r => UNIQUE_ASSETS.find(a => a.url.endsWith(`/${r}`))).filter(a => !!a);

        diag('blazorBootAsset:', blazorBootAsset);
        diag('blazorBootJson:', blazorBootJson);
        diag('blazorResources:', blazorResources);
        diag('blazorAssets:', blazorAssets);

        total = blazorAssets.length;
        const promises = blazorAssets.map(addCache.bind(null, true));

        diag('createAssetsCache ended - passive firstTime');
        diagGroupEnd();

        return;
    }

    const oldUrls = [];
    const updatedAssets = [];
    for (let i = 0; i < newCacheKeys.length; i++) {
        const key = newCacheKeys[i];
        if (!key || !key.url) continue;

        const lastIndex = key.url.lastIndexOf('.');
        let url = lastIndex === -1 ? key.url : key.url.substring(0, lastIndex);
        let hash = lastIndex === -1 ? '' : key.url.substring(lastIndex + 1);
        oldUrls.push({ url, hash });

        const foundAsset = UNIQUE_ASSETS.find(a => url.endsWith(a.url));
        if (!foundAsset) {
            diag('*** removed oldUrl:', key.url);
            newCache.delete(key.url);
        } else if ((hash && hash !== foundAsset.hash) || (!hash && !self.disableHashlessAssetsUpdate)) {
            diag('*** updated oldUrl:', key.url);
            newCache.delete(key.url);
            updatedAssets.push(foundAsset);
        }
    }

    const defaultAsset = UNIQUE_ASSETS.find(a => a.url === DEFAULT_URL);
    if (!updatedAssets.includes(defaultAsset)) {
        updatedAssets.push(defaultAsset); // get the latest version of the default doc in each update!
    }

    diag('oldUrls:', oldUrls);
    diag('updatedAssets:', updatedAssets);

    const assetsToCache = updatedAssets.concat(UNIQUE_ASSETS.filter(a => !oldUrls.find(u => u.url.endsWith(a.url) || a.url.endsWith(u.url))));

    diag('assetsToCache:', assetsToCache);

    total = assetsToCache.length;
    const promises = assetsToCache.map(addCache.bind(null, !ignoreProgressReport));

    diag('createAssetsCache ended.');
    diagGroupEnd();

    async function addCache(report, asset) {
        const request = createNewAssetRequest(asset);

        try {
            const responsePromise = fetch(request);
            return responsePromise.then(async response => {
                try {
                    if (!response.ok) {
                        diag('*** addCache - !response.ok:', request);
                        doReport(true);
                        return Promise.reject(response);
                    }

                    const cacheUrl = createCacheUrl(asset);
                    await newCache.put(cacheUrl, response.clone());

                    doReport();

                    return response;

                } catch (err) {
                    diag('*** addCache - put cache err:', err);
                    doReport(true);
                    return Promise.reject(err);
                }
            });
        } catch (err) {
            diag('*** addCache - catch err:', err);
            doReport(true);
            return Promise.reject(err);
        }

        function doReport(rejected = false) {
            if (!report) return;
            if (rejected && self.errorTolerance !== 'lax') return;

            const percent = (++current) / total * 100;
            sendMessage({ type: 'progress', data: { asset, percent, index: current } });
        }
    }
}

function createCacheUrl(asset: any) {
    return asset.hash ? `${asset.url}.${asset.hash}` : asset.url;
}

function createNewAssetRequest(asset) {
    let assetUrl;
    if (asset.url === DEFAULT_URL && self.noPrerenderQuery) {
        assetUrl = `${asset.url}?v=${asset.hash || self.assetsManifest.version}&${self.noPrerenderQuery}`;
    } else {
        assetUrl = `${asset.url}?v=${asset.hash || self.assetsManifest.version}`;
    }
    const requestInit: RequestInit = asset.hash && asset.hash.startsWith('sha') && self.enableIntegrityCheck
        ? { cache: 'no-store', integrity: asset.hash, headers: [['cache-control', 'public, max-age=3153600']] }
        : { cache: 'no-store', headers: [['cache-control', 'public, max-age=3153600']] };

    return new Request(assetUrl, requestInit);
}

async function deleteOldCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => key.startsWith('blazor-resources') || (key.startsWith(CACHE_NAME_PREFIX) && key !== CACHE_NAME)).map(key => caches.delete(key));
    return Promise.all(promises);
}

function uniqueAssets(assets) {
    const unique = {};
    const distinct = [];
    for (let i = 0; i < assets.length; i++) {
        const a = assets[i];
        if (unique[a.url]) continue;

        a.reqUrl = new Request(a.url).url;
        distinct.push(a);
        unique[a.url] = 1;
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

function diagGroup(label: string) {
    if (!self.enableDiagnostics) return;

    console.group(label);
}

function diagGroupEnd() {
    if (!self.enableDiagnostics) return;

    console.groupEnd();
}

function diag(...args: any[]) {
    if (!self.enableDiagnostics) return;

    console.info(...[...args, `(${new Date().toISOString()})`]);
}

function diagFetch(...args: any[]) {
    if (!self.enableFetchDiagnostics) return;

    console.info(...[...args, `(${new Date().toISOString()})`]);
}
