(self as any)['bit-bswup.sw version'] = '10.5.0';

// This file (and bit-bswup.sw-cleanup.ts) is a classic service-worker script loaded via
// importScripts - not an ES module - and is compiled against the "WebWorker" lib (see
// tsconfig.sw.json), which properly types caches, fetch, Request/Response, the Cache API,
// importScripts, location, etc. The lib types the bare `self` as WorkerGlobalScope, so we
// augment that interface with the two service-worker globals this code uses (clients,
// skipWaiting) plus the configuration knobs an app assigns on `self` before importing this
// script. We avoid re-declaring `self` as ServiceWorkerGlobalScope because that conflicts
// with the lib's own declaration in a non-module script. These declarations are ambient, so
// bit-bswup.sw-cleanup.ts in the same compilation sees them too.
interface BitBswupGlobals {
    clients: any
    skipWaiting: any
    
    assetsManifest: any           // injected by service-worker-assets.js (version + asset list)
    assetsInclude: any            // extra RegExp(s) of asset URLs to precache
    assetsExclude: any            // RegExp(s) of asset URLs to skip
    externalAssets: any           // additional assets to cache; each entry is either an exact asset (string / { url }) precached on install, or a RegExp pattern ({ url: /re/ } or a bare /re/) that lazily caches server-generated URLs unknown ahead of time (e.g. resource-collection.<hash>.js)
    defaultUrl: any               // document served for navigation requests (SPA fallback)
    assetsUrl: any                // path to service-worker-assets.js (default '/service-worker-assets.js')
    prohibitedUrls: any           // RegExp(s) that must always be answered with 403
    caseInsensitiveUrl: any       // match asset URLs case-insensitively
    serverHandledUrls: any        // RegExp(s) bypassed straight to the network (server owns them)
    serverRenderedUrls: any       // RegExp(s) of navigations that must NOT get the SPA fallback
    noPrerenderQuery: any         // query appended to defaultUrl to request the non-prerendered doc
    ignoreDefaultInclude: any     // drop the built-in DEFAULT_ASSETS_INCLUDE list
    ignoreDefaultExclude: any     // drop the built-in DEFAULT_ASSETS_EXCLUDE list
    isPassive: any                // passive: don't precache on install, fill cache lazily on fetch
    enableIntegrityCheck: any     // attach SRI `integrity` to asset requests from the manifest hash
    errorTolerance: any           // 'strict' (fail install on any error) | 'lax' (best-effort)
    maxRetries: any               // extra download attempts after the first on transient failure
    retryDelay: any               // base backoff (ms) between retries
    enableDiagnostics: any        // verbose console grouping/logging of install/activate
    enableFetchDiagnostics: any   // verbose console logging on every fetch (noisy)
    disableHashlessAssetsUpdate: any // don't re-download cached assets that have no hash
    forcePrerender: any           // always hit the network for the default doc (server prerender)
    enableCacheControl: any       // add no-store/no-cache to asset requests (bypass HTTP cache)
    cacheVersion: any             // override the version used in the cache bucket name
    mode: any                     // preset bundle of the above (see the switch below)
}

// `self` resolves to different types depending on which lib is in effect. The real build
// (tsconfig.sw.json) uses the WebWorker lib, where `self` is WorkerGlobalScope & typeof
// globalThis. The editor, however, type-checks this file in a default project that loads
// the DOM lib - tsserver doesn't auto-discover the non-standard tsconfig.sw.json - so there
// `self` is Window & typeof globalThis. Extending both interfaces from one shared list keeps
// every `self.*` access typed in both contexts without duplicating the declarations. In each
// compilation the interface for the *other* lib (WorkerGlobalScope under DOM, Window under
// WebWorker) is simply a harmless, unused standalone declaration.
interface WorkerGlobalScope extends BitBswupGlobals { }
interface Window extends BitBswupGlobals {
    // importScripts: any
 }

// Minimal shape of the ExtendableEvent / FetchEvent surface we use. Declared locally so the
// install/activate/fetch handlers can call waitUntil()/respondWith() without DOM lib types.
interface Event {
    waitUntil: any
    respondWith: any
}

diagGroup('bit-bswup');

const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : '/service-worker-assets.js';

diag('ASSETS_URL:', ASSETS_URL);

// importScripts can throw (404, network error, or a syntax error in the generated assets
// file). Fail soft: swallow the error here so self.assetsManifest stays undefined and the
// validateAssetsManifest check below reports a structured 'manifest' error to the page,
// instead of the whole service-worker script aborting with an unhandled exception before
// any error event can reach the client.
try {
    self.importScripts(ASSETS_URL);
} catch (err) {
    diag('*** importScripts failed:', ASSETS_URL, err);
}

const MANIFEST_ERRORS = validateAssetsManifest(self.assetsManifest);
// When the manifest is missing/malformed the service worker must not proceed to enumerate
// or cache assets - doing so would dereference self.assetsManifest.assets and crash, or
// promote a broken cache to the active one. We report the failure (so the page UI can react)
// and keep MANIFEST_VALID around to gate the rest of startup. We can't `return` at module
// scope, so the install handler and asset enumeration below guard on this flag instead.
const MANIFEST_VALID = MANIFEST_ERRORS.length === 0;
if (!MANIFEST_VALID) {
    diag('*** assetsManifest validation failed:', MANIFEST_ERRORS);
    sendError({
        reason: 'manifest',
        message: 'service-worker-assets.js is missing or malformed: ' + MANIFEST_ERRORS.join('; '),
        url: ASSETS_URL,
        fatal: true, // handleInstall aborts below regardless of errorTolerance
    });

    // Normalize to a benign, fully-formed shape so the rest of this module - which reads
    // self.assetsManifest.assets / .version in several places (some unconditionally, e.g.
    // createNewAssetRequest) - can finish evaluating without throwing. An exception at
    // module-evaluation time would tear down the worker before any install/error handling
    // could run, so the page would never receive the 'manifest' error reported above. With a
    // safe shape the script keeps reporting errors predictably; MANIFEST_VALID still gates
    // caching so we never promote this empty manifest over a previously good cache.
    self.assetsManifest = normalizeAssetsManifest(self.assetsManifest);
}

const VERSION = (self.assetsManifest && typeof self.assetsManifest.version === 'string') ? self.assetsManifest.version : '0.0.0-invalid-manifest';
const CACHE_NAME_PREFIX = 'bit-bswup';

// Cache identity normally tracks Blazor's manifest version (assetsManifest.version), a
// hash over the published assets. cacheVersion lets an app override the value used in the
// cache name: pin a stable string across noisy dev rebuilds (so perturbed asset hashes
// don't needlessly evict the whole cache), or bump it to force a refresh when a meaningful
// change lives outside Blazor's asset manifest. Only the cache *bucket name* is affected;
// the per-asset `?v=` cache-buster and SRI hashes still derive from VERSION, so integrity
// is unchanged. Falls back to the manifest version when unset or not a non-empty string.
const CACHE_VERSION = (typeof self.cacheVersion === 'string' && self.cacheVersion) || VERSION;
const CACHE_NAME = `${CACHE_NAME_PREFIX} - ${CACHE_VERSION}`;

// Named presets that expand into a coherent bundle of the individual self.* settings, so an
// app can pick a caching strategy with a single `mode` value instead of wiring each flag.
// The comment beside each case names a representative app using that strategy. Every preset
// uses ||= so any value the app set explicitly still wins over the preset default.
switch (self.mode) {
    // NoPrerender (e.g. adminpanel) and InitialPrerender (e.g. todo) share the same
    // service-worker preset: passive caching, no forced prerender, lax tolerance, and the
    // no-prerender query appended to the default document. They differ only in how the
    // server renders the first response, which is outside the SW's control - so they
    // intentionally fall through to one block instead of duplicating it (the previous
    // byte-identical copies were a copy-paste drift hazard).
    case 'NoPrerender':
    case 'InitialPrerender':
        self.isPassive = true;
        self.defaultUrl ||= "/";
        self.forcePrerender ||= false;
        self.errorTolerance ||= 'lax';
        self.caseInsensitiveUrl ||= true;
        self.noPrerenderQuery ||= 'no-prerender=true';
        break;
    case 'AlwaysPrerender': // like sales
        self.isPassive = true;
        self.defaultUrl ||= "/";
        self.forcePrerender ||= true;
        self.errorTolerance ||= 'lax';
        self.caseInsensitiveUrl ||= true;
        self.noPrerenderQuery ||= '';
        break;
    case 'FullOffline': // like todo-offline
        self.isPassive = false;
        self.defaultUrl ||= "/";
        self.forcePrerender ||= false;
        self.errorTolerance ||= 'lax';
        self.caseInsensitiveUrl ||= true;
        self.noPrerenderQuery ||= '';
        break;
}

// Default error tolerance when no mode preset applies. 'lax' is the default because it
// preserves the behavior every existing app was already getting: previous versions never
// awaited (and never rejected) the install, so a failed asset was always best-effort. It is
// also the safe default - a single optional externalAssets entry that 404s must not be able
// to abort a *first* install, which would leave the app with no active worker and no way to
// reach the start-Blazor handshake.
// Set 'strict' explicitly to opt into the standard Microsoft template / Workbox semantics:
// any precache failure aborts the install and the previous SW (if any) keeps serving.
self.errorTolerance ||= 'lax';
if (self.errorTolerance !== 'strict' && self.errorTolerance !== 'lax') {
    diag('*** unknown errorTolerance, falling back to lax:', self.errorTolerance);
    self.errorTolerance = 'lax';
}

// Transient-failure retry policy for asset downloads. A single flaky request (CDN blip,
// dropped connection, 5xx/429/408) shouldn't fail the whole strict install or silently
// drop an asset under lax. We retry such failures with exponential backoff before giving
// up. Deterministic failures (SRI/integrity mismatch, 404/403 and other permanent 4xx) are
// NOT retried because re-fetching identical bytes would just fail again.
// MAX_RETRIES is the number of *additional* attempts after the first try (default 2 => up
// to 3 total attempts). RETRY_DELAY is the base backoff in ms; attempt n waits
// RETRY_DELAY * 2^(n-1) (e.g. 300ms, 600ms) plus jitter.
const MAX_RETRIES = normalizeNonNegativeInt(self.maxRetries, 2);
const RETRY_DELAY = normalizeNonNegativeInt(self.retryDelay, 300);

diag('MAX_RETRIES:', MAX_RETRIES, 'RETRY_DELAY:', RETRY_DELAY);

// Wire up the four service-worker lifecycle/runtime events. install/activate extend the
// event with waitUntil() so the browser keeps the worker alive until our async work
// settles; fetch uses respondWith() to take over the response; message handles the
// page<->worker commands (SKIP_WAITING, CLAIM_CLIENTS, BLAZOR_STARTED, CLEAN_UP).
self.addEventListener('install', (e) => e.waitUntil(handleInstall(e)));
self.addEventListener('activate', (e) => e.waitUntil(handleActivate(e)));
self.addEventListener('fetch', (e) => e.respondWith(handleFetch(e)));
self.addEventListener('message', handleMessage);

async function handleInstall(e: any) {
    diag('installing version:', VERSION);

    if (!MANIFEST_VALID) {
        // The manifest is missing/malformed - sendError already notified the page. Reject the
        // install so the SW lifecycle aborts: a worker that never built a valid cache must not
        // reach the waiting/active state, otherwise a later SKIP_WAITING could activate it and
        // run deleteOldCaches(), discarding the last-known-good cache and promoting a broken
        // update. Throwing keeps the previous service worker in control until the manifest is
        // fixed.
        diag('*** aborting install - invalid assetsManifest.');
        throw new Error('Install aborted: service-worker-assets.js is missing or malformed.');
    }

    sendMessage({ type: 'install', data: { version: VERSION, isPassive: self.isPassive } });

    if (self.errorTolerance === 'strict') {
        // Strict: any required asset that fails to fetch / store must reject the install
        // promise so the SW lifecycle treats it as a failed install. Without this, a
        // partially-populated cache becomes the new active cache on the next reload.
        await createAssetsCache();
    } else {
        // Lax: lifecycle proceeds immediately; missing assets are filled lazily by
        // handleFetch. This preserves best-effort behavior for callers that explicitly
        // opt in via errorTolerance: 'lax'. We intentionally don't await the cache build so
        // install completes right away, but we still attach a catch: createAssetsCache won't
        // reject for asset failures under lax, yet infrastructure calls (caches.open, etc.)
        // can still throw, and an unhandled rejection here would surface as a console error
        // and a (cosmetic) failed-install signal. Swallow it - lazy-fill will recover.
        createAssetsCache().catch(err => {
            diag('*** lax install - background createAssetsCache failed (assets will lazy-fill):', err);
        });
    }
}

async function handleActivate(e: any) {
    diag('activate version:', VERSION);

    sendMessage({ type: 'activate', data: { version: VERSION, isPassive: self.isPassive } });

    // Prune stale caches, but only when it is safe to do so. A previous version's cache may
    // still be serving tabs that the old worker controls (a client keeps its controller until
    // it reloads), and deleting it out from under them would make their old app code fetch
    // new-version bytes -> SRI/boot-hash mismatch. So we clean up here only when there are no
    // open window clients at all - the case when a waiting worker activates naturally after
    // every tab has been closed, where nothing can be relying on an old cache. When tabs are
    // open and the user accepts an update, the SKIP_WAITING flow deletes old caches *after*
    // claiming clients (so they reload onto the new version first) instead.
    try {
        const windowClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        if (windowClients.length === 0) {
            diag('activate - no open window clients; pruning old caches.');
            await deleteOldCaches();
        } else {
            diag('activate - open window clients present; deferring cache cleanup:', windowClients.length);
        }
    } catch (err) {
        diag('*** activate - cache cleanup check failed:', err);
    }
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
// EXTERNAL_ASSETS holds every externalAssets entry, including RegExp patterns. Exact entries are
// precached on install; RegExp entries (e.g. Blazor Web's fingerprinted
// _framework/resource-collection.<hash>.js) can't be precached because their concrete URL isn't
// known ahead of time, so they're matched and cached lazily in handleFetch and kept across
// updates so the app still boots offline.
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

const ALL_ASSETS = (MANIFEST_VALID && Array.isArray(self.assetsManifest.assets) ? self.assetsManifest.assets : [])
    .filter((asset: any) => ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
    .filter((asset: any) => !ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
    .concat(EXTERNAL_ASSETS);

diag('ALL_ASSETS:', ALL_ASSETS);

const UNIQUE_ASSETS = uniqueAssets(ALL_ASSETS);

diag('UNIQUE_ASSETS:', UNIQUE_ASSETS);

diagGroupEnd();

// Runtime request router. For every GET this decides whether to serve the request from the
// Bswup cache, fall back to the SPA default document, or pass the request straight to the
// network. High-level flow:
//   1. Block prohibited URLs (403) and pass through non-GET / server-handled requests.
//   2. For navigations, serve the default document unless the URL is server-rendered or
//      forcePrerender is on (then the server owns the HTML).
//   3. Resolve the request to an asset by testing each asset's (RegExp) url against it.
//   4. Serve the asset from cache. On a cache miss the response is fetched from the network and
//      lazily written to the cache (in passive mode, and always for non-precacheable RegExp
//      pattern assets like resource-collection.<hash>.js); in active mode a precached asset that
//      misses simply goes to the network. Hash-less assets carry no version to diff, so they are
//      re-downloaded on every update (see createAssetsCache), not on every request.
async function handleFetch(e: any) {
    const req = e.request as Request;

    if (PROHIBITED_URLS.some(pattern => pattern.test(req.url))) {
        diagFetch('+++ handleFetch ended - prohibited:', e, req);

        // 403 Forbidden: the request was understood and is being refused. Versions before
        // 10.5.0 answered 405 Method Not Allowed, which is wrong on both counts - it says the
        // *method* is unsupported for this resource (the URL is blocked here regardless of
        // method) and RFC 9110 requires a 405 to carry an Allow header listing the permitted
        // methods, which there is no meaningful value for. Code that detects a Bswup-blocked
        // URL by checking for status 405 needs to look for 403 instead.
        //
        // The offending URL is deliberately not echoed back. It used to be interpolated into
        // statusText, which is an HTTP reason-phrase: it cannot carry arbitrary text, and the
        // Response constructor throws on an invalid one - turning a blocked request into an
        // exception inside the fetch handler. Keeping the body a fixed string means nothing
        // attacker-influenced is ever reflected; nosniff stops the text/plain body from being
        // re-interpreted as HTML by content sniffing. The URL is still available via
        // enableFetchDiagnostics (logged above).
        return new Response('This URL is prohibited!', {
            status: 403,
            statusText: 'Prohibited',
            headers: {
                'Content-Type': 'text/plain; charset=utf-8',
                'X-Content-Type-Options': 'nosniff',
            }
        });
    }

    const isServerHandled = SERVER_HANDLED_URLS.some(pattern => pattern.test(req.url));
    if (req.method !== 'GET' || isServerHandled) {
        diagFetch('*** handleFetch ended - skipped - !GET or SERVER_HANDLED_URLS:', e, req);
        return fetch(req);
    }



    const isServerRendered = SERVER_RENDERED_URLS.some(pattern => pattern.test(req.url));
    const shouldServeDefaultDoc = (req.mode === 'navigate') && !isServerRendered && !self.forcePrerender;

    const start = new Date().toISOString();

    // Every asset's `url` is a RegExp (uniqueAssets converts concrete string URLs to anchored,
    // query-tolerant patterns and keeps externalAssets RegExp entries as-is), so matching is a
    // single uniform test against the actual request URL. Navigations instead resolve to the SPA
    // default document.
    const asset = shouldServeDefaultDoc
        ? UNIQUE_ASSETS.find(a => a.isDefault)
        : UNIQUE_ASSETS.find(a => a.url.test(req.url));

    if (!asset) {
        diagFetch('+++ handleFetch ended - asset not found:', start, req.url, e, req);

        return fetch(req);
    }

    if (self.forcePrerender && asset.isDefault) {
        diagFetch('+++ handleFetch ended - skipped - forcePrerender defaultDoc:', start, asset, e, req);

        return fetch(req);
    }

    // Concrete assets are keyed/fetched via their reqUrl (+ hash / ?v= cache-buster). A pattern
    // asset (a RegExp externalAssets entry, e.g. resource-collection.<hash>.js) has no precomputed
    // URL, so it is keyed and fetched by the actual request.
    const cacheUrl = asset.reqUrl ? createCacheUrl(asset) : req.url;

    const bitBswupCache = await caches.open(CACHE_NAME);
    const cachedResponse = await bitBswupCache.match(cacheUrl);

    // Serve from cache when present. In active (non-passive) mode a precached asset that missed
    // the cache goes straight to the network; pattern assets are never precached (their concrete
    // URL isn't known ahead of time), so they must lazily fill the cache even in active mode.
    if (cachedResponse || (!self.isPassive && asset.reqUrl)) {
        diagFetch('+++ handleFetch ended - ', cachedResponse ? '' : 'NOT', 'using cache.', start, asset);

        return cachedResponse || fetch(req);
    }

    const request = asset.reqUrl ? createNewAssetRequest(asset) : req;
    const response = await fetch(request);

    if (response.ok) {
        // Stream the response to the page immediately and write to the cache in the background.
        // response.clone() tees the stream so the page and the cache write consume bytes as they
        // arrive; e.waitUntil keeps the worker alive until the background write completes.
        const cachePut = bitBswupCache.put(cacheUrl, response.clone()).catch(err => {
            diagFetch('+++ handleFetch - lazy-fill put failed:', err, asset);
        });
        e.waitUntil(cachePut);
    }

    diagFetch('+++ handleFetch ended - lazily caching asset:', start, asset, e, req);

    return response;
}

// Handles commands posted from the page (bit-bswup.ts). Each branch corresponds to a string
// command in the page<->worker protocol; non-matching JSON messages are ignored.
function handleMessage(e: MessageEvent<string>) {
    diag('handleMessage:', e);

    // Trust model: a service worker can only receive postMessage from clients on its own
    // origin (the browser enforces this), so every sender here is same-origin app code -
    // no cross-origin command injection is possible. We therefore don't filter on
    // e.origin / e.source. The commands below (SKIP_WAITING, CLAIM_CLIENTS, CLEAN_UP,
    // BLAZOR_STARTED) only drive this app's own SW lifecycle and caches.

    if (e.data === 'SKIP_WAITING') {
        // Activate the waiting worker, then take control of every open client so each tab
        // receives a 'controllerchange' and reloads onto the new version (handled in
        // bit-bswup.ts > handleControllerChange). Claiming is what makes multi-tab updates
        // consistent: without it, sibling tabs keep running the old app code while their
        // asset requests are served from the new worker - or from a cache we just deleted -
        // which corrupts boot config / DLL hashes. Old caches are removed only *after* the
        // claim so no controlled client is left pointing at a cache that no longer exists.
        return self.skipWaiting()
            .then(() => self.clients.claim())
            .then(() => deleteOldCaches())
            .then(() => sendMessage('WAITING_SKIPPED'));
    }

    if (e.data === 'CLAIM_CLIENTS') {
        // First-install claim. Take control so this page can start Blazor; sibling tabs
        // that observe the resulting 'controllerchange' will NOT reload because there was
        // no previously-active worker (see hadActiveWorkerAtStartup in bit-bswup.ts).
        return self.clients.claim()
            .then(() => deleteOldCaches())
            .then(() => e.source?.postMessage('CLIENTS_CLAIMED'));
    }

    if (e.data === 'BLAZOR_STARTED') {
        // Keep the worker alive until the post-start top-up pass settles; createAssetsCache(true)
        // is designed to never reject, so no extra error handling is needed here.
        e.waitUntil(createAssetsCache(true));
    }

    if (e.data === 'CLEAN_UP') {
        e.waitUntil(deleteOldCaches()); // remove the old caches
    }
}

// ============================================================================

// Builds (or updates) the version-suffixed cache for the current VERSION. This is the heart
// of the install/update flow:
//   - Warm-starts from the previous cache by copying over still-valid entries so an update
//     only re-downloads what actually changed (skipped when ignoreProgressReport is set).
//   - Diffs the existing cache against UNIQUE_ASSETS: removes assets no longer in the
//     manifest, re-downloads ones whose hash changed (and always refreshes the default doc).
//   - Downloads the remaining assets with retry/backoff, reporting progress to the page and
//     surfacing integrity/network failures via sendError.
// `ignoreProgressReport` is true for the post-BLAZOR_STARTED top-up pass: that run must not
// report progress to the UI and must never reject (the install has already activated).
async function createAssetsCache(ignoreProgressReport = false) {
    diagGroup('bit-bswup:createAssetsCache:' + ignoreProgressReport);

    const newCache = await caches.open(CACHE_NAME);
    const cacheKeys = await caches.keys();

    if (!ignoreProgressReport) {
        // Migrate previously-cached assets into the new cache so unchanged files (matched by
        // their hash-suffixed key) survive an update without being re-downloaded; the diff
        // loop below then prunes anything stale. Copy from every *other* bit-bswup bucket -
        // not just the first one found - and explicitly exclude CACHE_NAME so we never copy
        // the new cache onto itself (cacheKeys already contains it, since we opened it above).
        // Skip any key already present so an entry migrated from one bucket isn't overwritten
        // by a copy in another.
        const oldCacheKeys = cacheKeys.filter(key => key.startsWith(CACHE_NAME_PREFIX) && key !== CACHE_NAME);
        for (const oldCacheKey of oldCacheKeys) {
            diag('copying old cache:', oldCacheKey);
            const oldCache = await caches.open(oldCacheKey);
            const oldKeys = await oldCache.keys();
            for (let i = 0; i < oldKeys.length; i++) {
                const oldKey = oldKeys[i];
                if (!oldKey || !oldKey.url) continue;

                if (await newCache.match(oldKey.url)) continue;

                const oldRes = await oldCache.match(oldKey.url);
                if (!oldRes) continue;
                await newCache.put(oldKey.url, oldRes);
            }
        }
    }

    let newCacheKeys = await newCache.keys();
    const firstTime = newCacheKeys.length === 0;
    const passiveFirstTime = self.isPassive && firstTime
    if (passiveFirstTime) {
        if (!ignoreProgressReport) {
            sendMessage({ type: 'bypass', data: { firstTime: true } });
        }
        return;
    }

    diag('passiveFirstTime:', passiveFirstTime);

    let current = 0;
    let total = UNIQUE_ASSETS.length;

    // Resolve each manifest asset to the exact absolute cache key the Cache API stores for it
    // (createCacheUrl + the same URL resolution cache.put performs). Diffing the existing
    // cache against these precomputed keys is exact and unambiguous.
    //
    // The previous approach recovered the asset URL and hash by splitting each cached key on
    // its last '.', which mis-parsed hashless keys: 'index.html' became url 'index' + hash
    // 'html', so every hashless asset looked "removed" and was re-downloaded on each update
    // even when disableHashlessAssetsUpdate was set. Its endsWith fallback could also conflate
    // distinct assets that merely share a URL suffix ('app.css' vs 'myapp.css'). Exact-key
    // matching avoids both.
    const fold = (s: string) => self.caseInsensitiveUrl ? s.toLowerCase() : s;
    const assetByCacheKey = new Map<string, any>();
    for (const asset of UNIQUE_ASSETS) {
        // Pattern assets (no concrete reqUrl) can't be precached or diffed; they are matched and
        // cached lazily by handleFetch, so skip them here.
        if (!asset.reqUrl) continue;
        assetByCacheKey.set(fold(new Request(createCacheUrl(asset)).url), asset);
    }

    // Assets confirmed present at their current cache key - these are not re-downloaded.
    const cachedAssets = new Set<any>();

    // Collect stale entries to delete and await them as a batch below, rather than firing
    // newCache.delete(...) unawaited. The unawaited form let deletes race the subsequent
    // addCache puts (and the concurrent post-BLAZOR_STARTED top-up run), so a freshly
    // written asset could be removed by a still-in-flight delete. Gathering keys first and
    // awaiting them keeps the cache state deterministic before we repopulate.
    const keysToDelete = [] as string[];
    for (let i = 0; i < newCacheKeys.length; i++) {
        const key = newCacheKeys[i];
        if (!key || !key.url) continue;

        const matched = assetByCacheKey.get(fold(key.url));
        if (!matched) {
            // A key lazily cached for a pattern asset (no concrete reqUrl, e.g.
            // resource-collection.<hash>.js) has no entry in assetByCacheKey; keep it so it stays
            // available offline instead of being pruned as stale.
            if (UNIQUE_ASSETS.some(a => !a.reqUrl && a.url.test(key.url))) {
                diag('*** keeping lazily-cached pattern asset key:', key.url);
                continue;
            }

            // No current asset maps to this key: the asset was removed from the manifest, or
            // its hash changed (a changed hash yields a different key, so the old hashed key
            // no longer matches). Either way it's stale - drop it.
            diag('*** removed/stale cache key:', key.url);
            keysToDelete.push(key.url);
            continue;
        }

        // Exact key match: the asset is cached at its current version. Hashed keys are
        // content-addressed, so an unchanged hash means the bytes are current - keep them.
        // Hashless keys carry no version, so re-download them each update unless the app
        // opts out via disableHashlessAssetsUpdate.
        if (!matched.hash && !self.disableHashlessAssetsUpdate) {
            diag('*** refreshing hashless cache key:', key.url);
            keysToDelete.push(key.url);
        } else {
            cachedAssets.add(matched);
        }
    }

    // Always refresh the default document on each update so navigations pick up the latest
    // app shell even when its hash is unchanged. If it was kept above, drop it from the kept
    // set and delete its current entry so it is re-fetched below.
    const defaultAsset = UNIQUE_ASSETS.find(a => a.isDefault);
    if (defaultAsset && cachedAssets.has(defaultAsset)) {
        cachedAssets.delete(defaultAsset);
        keysToDelete.push(new Request(createCacheUrl(defaultAsset)).url); // get the latest version of the default doc in each update if exists!!
    }

    await Promise.all(keysToDelete.map(url => newCache.delete(url)));

    // Pattern assets are excluded: they can't be precached (no concrete URL) and are filled
    // lazily by handleFetch instead.
    const assetsToCache = UNIQUE_ASSETS.filter(a => !cachedAssets.has(a) && a.reqUrl);

    diag('cachedAssets:', cachedAssets.size, 'assetsToCache:', assetsToCache);

    total = assetsToCache.length;

    // Nothing to download: every asset is already cached and up to date (e.g. an update
    // whose only change lives outside the asset set, or a misconfigured defaultUrl that
    // matches no manifest asset). Without an asset to drive a 'progress' message to 100%,
    // the page would never receive downloadFinished and the splash would hang, so we still
    // have to signal completion. 'bypass' is the right signal rather than a synthetic
    // 'progress': it already means "nothing to download, we're done" (it is what the passive
    // first-run path sends) and bit-bswup.ts maps it straight to downloadFinished. A fake
    // progress tick would instead have to carry an asset, and every handler in the wild -
    // including the built-in ShowAssets list and the example in the README - dereferences
    // data.asset.url, so a null there would throw in app code. The post-BLAZOR_STARTED top-up
    // (ignoreProgressReport) stays silent because the page UI is already gone by then.
    if (total === 0) {
        diag('createAssetsCache - nothing to cache; reporting completion.');
        if (!ignoreProgressReport) {
            // No firstTime flag: whether this counts as a first install is decided on the page
            // from whether a worker was already active at startup.
            sendMessage({ type: 'bypass', data: {} });
        }
        diagGroupEnd();
        return;
    }

    // Local to this invocation: createAssetsCache can run concurrently (the install run and
    // the post-BLAZOR_STARTED top-up via createAssetsCache(true)). A module-level counter
    // would let those runs clobber each other's tally and misreport integrity failures, so
    // each run gets its own counter, closed over by the nested addCache below.
    let integrityFailureCount = 0;
    const promises = assetsToCache.map(addCache.bind(null, !ignoreProgressReport));

    // Await install batch so SRI/network failures surface as install rejections instead of
    // unhandled promise rejections. A bare Promise.all would settle on the first rejection and
    // leave the siblings' rejections unhandled; we want every asset attempted and reported even
    // when the install will ultimately fail. Mapping each promise to a never-rejecting boolean
    // first gives exactly that, and unlike Promise.allSettled (ES2020) it runs on the ES2019
    // baseline this bundle targets - a service worker is the last place to depend on a recent
    // runtime, since a browser too old to parse it fails the whole offline story silently.
    const outcomes = await Promise.all(promises.map((p: Promise<any>) => p.then(() => true, () => false)));
    const rejectedCount = outcomes.reduce((n, succeeded) => n + (succeeded ? 0 : 1), 0);

    if (integrityFailureCount > 0 && !ignoreProgressReport) {
        sendError({
            reason: 'install-incomplete',
            message: `Install completed with ${integrityFailureCount} integrity failure(s). The service worker will not activate cleanly; check that service-worker-assets.js, blazor.boot.json, and the framework files are served byte-identical (no on-the-fly gzip/minify by a CDN or proxy).`,
            count: integrityFailureCount,
            // Under 'strict' the abort below tears the install down; under 'lax' the install
            // still succeeds and the affected assets lazy-fill, so this is advisory only.
            fatal: self.errorTolerance === 'strict',
        });
    }

    diag('createAssetsCache ended.');
    diagGroupEnd();

    // Strict tolerance: if any required asset failed to fetch / store, reject so the SW
    // lifecycle aborts the install and the previous SW (if any) keeps serving. The cache
    // we partially populated is discarded explicitly here; the next install will recreate
    // it under the version-suffixed CACHE_NAME.
    // ignoreProgressReport === true means this run is the post-BLAZOR_STARTED top-up; that
    // path must never reject because install has already activated.
    if (!ignoreProgressReport && self.errorTolerance === 'strict' && rejectedCount > 0) {
        try { await caches.delete(CACHE_NAME); } catch { /* best effort */ }

        const abortMessage =
            `Install aborted under errorTolerance 'strict': ${rejectedCount} of ${total} asset(s) failed. ` +
            `Switch to errorTolerance 'lax' (the default) to allow a partial cache plus runtime fallback.`;

        // Announce the abort as its own terminal error before throwing. The per-asset errors
        // above each describe one failure; this is the one message that says the *install* is
        // over. The page needs it to force-start Blazor on a first install - without an active
        // worker there is no CLAIM_CLIENTS handshake, so nothing else would ever start the app.
        sendError({ reason: 'install-aborted', message: abortMessage, count: rejectedCount, total, fatal: true });

        throw new Error(abortMessage);
    }

    async function addCache(report: boolean, asset: any) {
        let request: Request;
        try {
            request = createNewAssetRequest(asset);
        } catch (err) {
            diag('*** addCache - catch err:', err);
            sendError({
                reason: 'request',
                message: 'Failed to build asset request: ' + (err && (err as any).message || String(err)),
                url: asset && asset.reqUrl,
                hash: asset && asset.hash,
                fatal: isFatalAssetFailure(),
            });
            doReport(true);
            return Promise.reject(err);
        }

        const hasIntegrity = !!(request as any).integrity;
        let lastError: any;

        // Attempt the download up to MAX_RETRIES additional times after the first try.
        // Only transient failures fall through to the next iteration; deterministic ones
        // (integrity mismatch, permanent HTTP statuses) reject immediately.
        for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
            if (attempt > 0) {
                // Exponential backoff with jitter: attempt 1 waits ~RETRY_DELAY, attempt 2
                // ~2*RETRY_DELAY, etc. Jitter spreads the retry storm when many of the 200+
                // assets fail at once (e.g. a brief CDN outage) so they don't all re-hit the
                // origin on the same tick.
                const backoff = RETRY_DELAY * Math.pow(2, attempt - 1);
                const wait = backoff + Math.floor(Math.random() * RETRY_DELAY);
                diag(`*** addCache - retrying (${attempt}/${MAX_RETRIES}) in ${wait}ms:`, asset.reqUrl);
                await delay(wait);
            }

            let response: Response;
            try {
                response = await fetch(request);
            } catch (fetchErr) {
                // Browsers reject fetch() with a TypeError when SRI validation fails. The
                // browser also logs "Failed to find a valid digest in the 'integrity' attribute"
                // to the console, but the SW would otherwise silently swallow this. Surface it.
                // SRI and transient network failures both reject as TypeError; only treat as
                // integrity when the message signals a digest/SRI problem, not on TypeError alone.
                const isIntegrity =
                    hasIntegrity &&
                    /integrity|digest|EPRPROTO/i.test(String(fetchErr && (fetchErr as any).message || fetchErr));

                // Integrity failures are deterministic: re-fetching identical bytes fails the
                // same way, so never retry them. Genuine network errors are transient and
                // worth another attempt while retries remain.
                if (!isIntegrity && attempt < MAX_RETRIES) {
                    lastError = fetchErr;
                    diag('*** addCache - fetch rejected (will retry):', fetchErr, asset.reqUrl);
                    continue;
                }

                if (isIntegrity) integrityFailureCount++;
                diag('*** addCache - fetch rejected:', fetchErr, 'integrity?', isIntegrity);
                sendError({
                    reason: isIntegrity ? 'integrity' : 'fetch',
                    message: isIntegrity
                        ? `Subresource Integrity check failed for ${asset.reqUrl}. The bytes served do not match the SHA hash recorded in service-worker-assets.js / blazor.boot.json. This is the classic Blazor "Failed to find a valid digest" failure and usually means a CDN, reverse proxy, or compression layer is rewriting the response after publish.`
                        : 'Asset fetch rejected' + (attempt > 0 ? ` after ${attempt + 1} attempts` : '') + ': ' + (fetchErr && (fetchErr as any).message || String(fetchErr)),
                    url: asset.reqUrl,
                    hash: asset.hash,
                    integrity: hasIntegrity,
                    fatal: isFatalAssetFailure(),
                });
                doReport(true);
                return Promise.reject(fetchErr);
            }

            if (!response.ok) {
                // Retry only transient HTTP statuses (request timeout, rate limit, 5xx).
                // Permanent ones (404, 403, ...) will not change on retry.
                if (isRetryableStatus(response.status) && attempt < MAX_RETRIES) {
                    lastError = response;
                    diag('*** addCache - !response.ok (will retry):', response.status, asset.reqUrl);
                    continue;
                }

                diag('*** addCache - !response.ok:', request);
                sendError({
                    reason: 'fetch',
                    message: `Asset fetch failed with HTTP ${response.status} ${response.statusText || ''}`.trim() + (attempt > 0 ? ` after ${attempt + 1} attempts` : ''),
                    url: asset.reqUrl,
                    hash: asset.hash,
                    status: response.status,
                    integrity: hasIntegrity,
                    fatal: isFatalAssetFailure(),
                });
                doReport(true);
                return Promise.reject(response);
            }

            try {
                const cacheUrl = createCacheUrl(asset);
                await newCache.put(cacheUrl, response.clone());

                doReport();

                return response;
            } catch (err) {
                diag('*** addCache - put cache err:', err);
                sendError({
                    reason: 'cache',
                    message: 'Failed to store asset in cache: ' + (err && (err as any).message || String(err)),
                    url: asset.reqUrl,
                    hash: asset.hash,
                    fatal: isFatalAssetFailure(),
                });
                doReport(true);
                return Promise.reject(err);
            }
        }

        // Unreachable in practice (the loop returns on success or rejects on the final
        // attempt), but keep a defensive fallback so the promise always settles.
        return Promise.reject(lastError);

        // Whether a single asset failure will actually bring the install down. Under 'strict'
        // any rejection aborts the install (see the throw above), so the page must treat it as
        // terminal. Under 'lax' the install still completes and the asset lazily fills on first
        // fetch, so it is reported for visibility only. The post-BLAZOR_STARTED top-up run
        // (report === false) can never abort anything - the install already activated.
        function isFatalAssetFailure() {
            return report && self.errorTolerance === 'strict';
        }

        function doReport(rejected = false) {
            if (!report) return;
            if (rejected && self.errorTolerance !== 'lax') return;

            const percent = (++current) / total * 100;
            sendMessage({ type: 'progress', data: { asset: toMessageAsset(asset), percent, index: current } });
        }
    }
}

// Cache key for a concrete asset: its reqUrl suffixed with `.<hash>` when a hash exists, so a
// changed hash produces a distinct cache entry (the old one is detected and evicted during the
// update diff in createAssetsCache). Hashless assets are keyed by reqUrl alone. Only called for
// concrete assets (asset.reqUrl set); pattern assets are keyed by the live request URL instead.
function createCacheUrl(asset: any) {
    return asset.hash ? `${asset.reqUrl}.${asset.hash}` : asset.reqUrl;
}

// Resolves after `ms` milliseconds. Used to space out asset-download retries.
function delay(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Whether an HTTP status code represents a transient failure worth retrying. 408 (Request
// Timeout) and 429 (Too Many Requests) are explicitly transient; any 5xx is treated as a
// server-side hiccup. Everything else (notably 404/403 and other 4xx) is permanent and
// must not be retried.
function isRetryableStatus(status: number) {
    return status === 408 || status === 429 || (status >= 500 && status <= 599);
}

// Coerces a self.* config value into a non-negative integer, falling back to `fallback`
// when the value is missing or not a sane number. Keeps MAX_RETRIES / RETRY_DELAY robust
// against bad app configuration.
function normalizeNonNegativeInt(value: any, fallback: number) {
    const n = Number(value);
    if (!Number.isFinite(n) || n < 0) return fallback;
    return Math.floor(n);
}

// Builds the network Request used to download an asset. The asset version (its own hash, or
// the manifest version as a fallback) is base64url-normalized and appended as a `?v=` cache
// buster so each published version is fetched distinctly. For the default document the
// optional noPrerenderQuery params are added to request the non-prerendered variant. When
// the hash is an SRI digest (sha*) and integrity checks are enabled, the request carries the
// `integrity` attribute so the browser rejects tampered/mismatched bytes; enableCacheControl
// adds no-store/no-cache headers to bypass the HTTP cache and force a fresh fetch.
function createNewAssetRequest(asset: any) {
    // Global .replace() rather than .replaceAll(): identical result for these single-character
    // literals, but replaceAll is ES2021 and this bundle targets ES2019. (This call predates
    // the ES2019 target and silently raised the real runtime floor to Chrome 85 / Safari 13.4
    // even while tsconfig claimed ES2019 - `target` downlevels syntax, never library methods.)
    const version = ((asset.hash || self.assetsManifest.version) as string).replace(/\+/g, '-').replace(/\//g, '_');
    const trimmedVersion = encodeURIComponent(trimEnd(version, '='));

    const url = new URL(asset.reqUrl, self.location.origin);
    url.searchParams.set('v', trimmedVersion);
    if (asset.isDefault && self.noPrerenderQuery) {
        new URLSearchParams(String(self.noPrerenderQuery)).forEach((value, key) => url.searchParams.set(key, value));
    }

    const assetUrl = url.toString();

    const requestInit: RequestInit = {};
    // SECURITY NOTE: Subresource Integrity is OPT-IN. Even though Blazor ships a SHA hash
    // for every asset, we only attach `integrity` (so the browser rejects tampered/mismatched
    // bytes) when the app sets self.enableIntegrityCheck. It defaults to off because an
    // intermediary that rewrites bytes after publish - CDN gzip/brotli, HTML/JS minifying
    // proxy, etc. - would otherwise make every asset fail SRI and brick the install. Apps that
    // serve assets byte-identical (the recommended setup) should enable it for tamper
    // protection; see the 'integrity' error path that reports the classic Blazor
    // "Failed to find a valid digest" failure.
    if (asset.hash?.startsWith('sha') && self.enableIntegrityCheck) {
        requestInit.integrity = asset.hash;
    }
    if (self.enableCacheControl) {
        requestInit.cache = 'no-store';
        requestInit.headers = [['cache-control', 'no-cache']];
    }

    return new Request(assetUrl, requestInit);
}

// Removes every Bswup cache except the current CACHE_NAME. Called after a new worker claims
// clients (SKIP_WAITING / CLAIM_CLIENTS) and on the CLEAN_UP command, so stale
// version-suffixed caches from previous installs are reclaimed once they're no longer needed.
async function deleteOldCaches() {
    const cacheKeys = await caches.keys();
    const promises = cacheKeys.filter(key => (key.startsWith(CACHE_NAME_PREFIX) && key !== CACHE_NAME)).map(key => caches.delete(key));
    return Promise.all(promises);
}

// De-duplicates the asset list by URL (the first occurrence wins) and normalizes each entry so
// the rest of the worker can treat every asset uniformly:
//   - `url`   becomes a RegExp matcher. A concrete string URL is converted to an anchored,
//             query-tolerant pattern for its resolved origin+pathname; an externalAssets RegExp
//             entry (a server-generated file whose concrete name isn't known ahead of time, e.g.
//             resource-collection.<hash>.js) is kept as the matcher it already is.
//   - `reqUrl` holds the concrete absolute URL used to build cache keys and download requests. It
//             is undefined for pattern assets, whose concrete URL is only known when a matching
//             request arrives (they are cached lazily by that request URL instead).
//   - `isDefault` flags the SPA default document, since `url` is no longer comparable by string.
//   - `srcUrl` preserves the original `url` exactly as the app/manifest declared it (the relative
//             manifest path, e.g. '_framework/blazor.boot.json', or a pattern entry's source
//             text). `url` is overwritten with the matcher, and a RegExp is not JSON
//             serializable - it stringifies to `{}` - so this is what gets reported to the page
//             in progress messages. See toMessageAsset.
// The manifest entries are shallow-copied rather than mutated in place, since self.assetsManifest
// / externalAssets are caller-owned and read elsewhere.
function uniqueAssets(assets: any) {
    const unique = {} as any;
    const distinct = [];
    for (let i = 0; i < assets.length; i++) {
        const a = assets[i];
        const isPattern = a.url instanceof RegExp;
        const dedupeKey = isPattern ? a.url.toString() : a.url;
        if (unique[dedupeKey]) continue;
        unique[dedupeKey] = 1;

        const reqUrl = isPattern ? undefined : new Request(a.url).url;
        distinct.push({
            ...a,
            url: isPattern ? applyUrlCaseSensitivity(a.url) : urlToRegExp(reqUrl as string),
            srcUrl: isPattern ? String(a.url) : a.url,
            reqUrl,
            isDefault: !isPattern && a.url === DEFAULT_URL,
        });
    }
    return distinct;
}

// Converts an internal asset into a shape that survives JSON.stringify on its way to the page.
// Internally `asset.url` is a RegExp matcher, and JSON.stringify turns a RegExp into `{}` - so
// posting the raw asset would hand every `bitBswupHandler` (and the built-in ShowAssets list)
// an empty object where the documented API promises a URL string. Restore `url` to the string
// the app declared, keeping the rest of the entry (hash, reqUrl, isDefault, plus any custom
// fields an app attached to its externalAssets) intact.
function toMessageAsset(asset: any) {
    if (!asset) return asset;

    return { ...asset, url: asset.srcUrl ?? asset.reqUrl ?? String(asset.url) };
}

// Escapes RegExp metacharacters so a literal string can be embedded in a pattern.
function escapeRegExp(str: string) {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Converts a concrete asset URL into a RegExp that matches requests for it. Anchored to the
// resolved origin+pathname and tolerant of any query string, so cache-busting variants
// (?asp-append-version, ?v=, ...) still match. Honors caseInsensitiveUrl via the `i` flag.
function urlToRegExp(url: string) {
    const u = new URL(url, self.location.origin);
    return new RegExp('^' + escapeRegExp(`${u.origin}${u.pathname}`) + '(\\?.*)?$', self.caseInsensitiveUrl ? 'i' : '');
}

// Broadcasts a message to every client (controlled or not), so all open tabs - not just the
// one that triggered the work - receive install/progress/activate/error updates. Objects are
// JSON-stringified; plain string commands (e.g. 'WAITING_SKIPPED') are sent as-is.
function sendMessage(message: any) {
    self.clients
        .matchAll({ includeUncontrolled: true })
        .then((clients: any) => (clients || []).forEach((client: any) => client.postMessage(typeof message === 'string' ? message : JSON.stringify(message))));
}

// Reports a structured install/runtime failure: logs it for diagnostics, also writes to the
// console as a best-effort signal in case no client is connected yet, then forwards it to the
// page as an 'error' message so the progress UI can show it (see bit-bswup.progress.ts).
//
// `fatal` tells the page whether this failure actually stops the install:
//   - true  => the install is aborting (invalid manifest, or a strict-mode abort). The page
//              must surface it and, on a first install, force-start Blazor so the app still
//              boots from the network instead of hanging behind the splash.
//   - false => best-effort failure under 'lax'; the install continues and the asset will be
//              lazily filled on first fetch. The page logs it but must NOT replace the
//              progress UI with a failure panel for what is a recoverable, expected case
//              (e.g. an optional externalAssets entry that 404s).
// Callers always pass it explicitly; an omitted value is treated as fatal by the page.
function sendError(data: { reason: string; message: string; fatal?: boolean;[key: string]: any }) {
    diag('*** error:', data);
    try {
        // Best-effort console output so the failure is visible even before any client connects.
        console.error('BitBswup SW:', data.message, data);
    } catch { /* ignore */ }
    sendMessage({ type: 'error', data });
}

// Coerces an invalid/missing assetsManifest into a benign, fully-formed object so the rest
// of the module can read .version / .assets without throwing. Preserves any salvageable
// fields from a partially-valid manifest (e.g. a present version but missing assets array).
// This never makes an invalid manifest "valid" - MANIFEST_VALID still gates caching - it
// only guarantees a safe shape so the worker can keep reporting errors instead of crashing.
function normalizeAssetsManifest(manifest: any) {
    const safe = (manifest && typeof manifest === 'object') ? manifest : {};
    if (typeof safe.version !== 'string' || !safe.version) {
        safe.version = '0.0.0-invalid-manifest';
    }
    if (!Array.isArray(safe.assets)) {
        safe.assets = [];
    }
    return safe;
}

// Validates the manifest injected by service-worker-assets.js and returns a list of human
// readable problems (empty array == valid). Checks it's an object, has a non-empty version
// string, has an assets array, and that every asset entry carries a url. The result gates
// MANIFEST_VALID, which in turn decides whether the worker is allowed to cache/activate.
function validateAssetsManifest(manifest: any): string[] {
    const errors: string[] = [];
    if (!manifest || typeof manifest !== 'object') {
        errors.push('assetsManifest is not defined');
        return errors;
    }
    if (typeof manifest.version !== 'string' || !manifest.version) {
        errors.push('assetsManifest.version is missing');
    }
    if (!Array.isArray(manifest.assets)) {
        errors.push('assetsManifest.assets is not an array');
        return errors;
    }
    let badEntries = 0;
    for (let i = 0; i < manifest.assets.length; i++) {
        const a = manifest.assets[i];
        if (!a || typeof a.url !== 'string' || !a.url) badEntries++;
    }
    if (badEntries > 0) {
        errors.push(`${badEntries} asset entr${badEntries === 1 ? 'y has' : 'ies have'} no url`);
    }
    return errors;
}

// Normalizes self.externalAssets into a consistent array of `{ url, ... }` objects. Accepts a
// single value or an array, passes through entries that already have a url (a concrete string or
// a RegExp pattern), wraps bare strings and bare RegExps into `{ url }`, and drops anything else
// (null/invalid). RegExp `url` entries flow through the same list as exact assets; they simply
// aren't precached (their concrete URL is unknown) and are matched/cached lazily in handleFetch.
function prepareExternalAssetsArray(value: any) {
    const array = value ? (value instanceof Array ? value : [value]) : [];

    return array.map(asset => {
        if (asset && asset.url) {
            return asset;
        }

        if (typeof asset === 'string' || asset instanceof RegExp) {
            return ({ url: asset });
        }

        return null;
    }).filter(asset => asset !== null);
}

function prepareRegExpArray(value: any) {
    // Threat model: the patterns here come from developer-configured sources
    // (self.prohibitedUrls, self.serverHandledUrls, etc.), not end-user input.
    // They are compiled into RegExp objects and run against URLs on every request,
    // so a pathological pattern can cause catastrophic backtracking (ReDoS) and
    // stall the service worker. When authoring patterns:
    //   - avoid nested/overlapping quantifiers such as (a+)+, (a*)*, (.*)*
    //   - prefer anchored, specific patterns over broad .* wildcards
    //   - keep pattern length bounded; very long patterns are a smell
    // Invalid patterns are caught below and skipped rather than throwing. String entries are
    // escaped to literals (see below), so they can never introduce backtracking.
    const array = value ? (value instanceof Array ? value : [value]) : [];

    return array.map(p => {
        if (p instanceof RegExp) {
            return applyUrlCaseSensitivity(p);
        }

        // String entries are matched LITERALLY: the text is regex-escaped, so it matches
        // itself as a substring of the URL and nothing else. Pass a real RegExp when you want
        // pattern semantics - that is what every example in the README does.
        //
        // The tempting alternative, compiling the string as regular-expression *source*, is a
        // trap. '/admin/v1.0/' would silently become an unanchored pattern where '.' matches
        // any character, so it also matches '/admin/v1X0/'; and 'app.css' in assetsExclude
        // would match 'myapp1css'. Worse, previous releases dropped string entries entirely,
        // so any string sitting in one of these lists today has never actually been applied -
        // upgrading would suddenly activate it with over-matching semantics its author never
        // reviewed. That is a bad surprise for assetsExclude (silently stops caching) and a
        // genuinely dangerous one for prohibitedUrls (a broader block, or a substring match
        // that lets an unexpected URL through a serverHandledUrls bypass). Literal matching
        // makes the string do exactly, and only, what it looks like it does.
        if (typeof p === 'string') {
            try {
                return applyUrlCaseSensitivity(new RegExp(escapeRegExp(p)));
            } catch (err) {
                console.warn('BitBswup SW: ignoring invalid pattern:', p, err);
                return null;
            }
        }

        console.warn('BitBswup SW: ignoring non-RegExp entry (expected RegExp or string):', p);
        return null;
    }).filter((p): p is RegExp => p !== null);
}

// When caseInsensitiveUrl is enabled, every URL-matching pattern (prohibited / server-handled
// / server-rendered URLs and the user include/exclude lists) should fold case too, so routing
// and asset matching behave consistently with the explicit toLowerCase comparisons in
// handleFetch. Without this a pattern like /admin/ would not match /ADMIN/ even with
// caseInsensitiveUrl set - a surprising gap for the security-relevant prohibitedUrls list.
// Patterns that already carry the `i` flag (including RegExp instances the app built with it)
// keep it. The global (g) and sticky (y) flags are always stripped: both make RegExp.test()
// stateful by advancing lastIndex between calls, so a pattern reused across requests (the
// PROHIBITED_URLS / SERVER_*_URLS lists are compiled once and tested on every fetch) returns
// alternating true/false results - an intermittent security bypass. Removing them is a
// security-critical normalization that runs regardless of caseInsensitiveUrl; the `i` flag is
// then added only when case-insensitive matching is requested.
function applyUrlCaseSensitivity(re: RegExp): RegExp {
    const safeFlags = re.flags.replace(/[gy]/g, '');
    const needsI = self.caseInsensitiveUrl && safeFlags.indexOf('i') === -1;
    const flags = needsI ? safeFlags + 'i' : safeFlags;
    if (flags === re.flags) return re;
    return new RegExp(re.source, flags);
}

// Strips trailing occurrences of `char` from the end of `str`. Used to drop base64 `=`
// padding from version/hash values before they go into the `?v=` query. `char` is regex
// escaped first so callers can pass literal characters safely.
function trimEnd(str: string, char: string) {
    const escaped = char.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // escape regex special chars
    return str.replace(new RegExp(`${escaped}+$`), "");
}

// Diagnostics helpers - all no-ops unless the matching flag is enabled, so verbose logging
// can be turned on per app without code changes. diag*/diagGroup* gate on enableDiagnostics;
// diagFetch gates on the separate (noisier) enableFetchDiagnostics. diag/diagFetch append an
// ISO timestamp to every line to make install/fetch timing legible in the console.
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
