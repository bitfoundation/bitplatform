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
    registration: any             // also declared (identically) in bit-bswup.sw-cleanup.ts; the ambient interfaces merge

    assetsManifest: any           // injected by service-worker-assets.js (version + asset list)
    assetsInclude: any            // extra RegExp(s) of asset URLs to precache
    assetsExclude: any            // RegExp(s) of asset URLs to skip
    externalAssets: any           // additional assets to cache; each entry is either an exact asset (string / { url }) precached on install, or a RegExp pattern ({ url: /re/ } or a bare /re/) that lazily caches server-generated URLs unknown ahead of time (e.g. resource-collection.<hash>.js)
    defaultUrl: any               // document served for navigation requests (SPA fallback)
    assetsUrl: any                // path to service-worker-assets.js (default 'service-worker-assets.js', resolved against the service-worker's own location)
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
interface Window extends BitBswupGlobals { }

// Minimal shape of the ExtendableEvent / FetchEvent surface we use, so the
// install/activate/fetch/message handlers can call waitUntil()/respondWith() and read
// request without DOM lib types. A dedicated interface rather than an augmentation of the
// global Event: this file is compiled together with bit-bswup.sw-cleanup.ts
// (tsconfig.sw.json), and augmenting Event would silently type every event over there too.
interface BswupExtendableEvent extends Event {
    waitUntil: any
    respondWith: any
    request: any
}

diagGroup('bit-bswup');

// Resolved by importScripts against the worker's own location - the directory containing the
// app's service-worker.js, which is also where Blazor publishes service-worker-assets.js - so
// the relative default works for root-mounted apps and apps mounted on a sub-path
// (https://host/myapp/) alike. Versions before v-10-6-0 defaulted to the root-absolute
// '/service-worker-assets.js', which pointed a sub-path app at the wrong path and failed its
// install with a 'manifest' error unless assetsUrl was set explicitly - even though the page
// script goes out of its way (the scope-fallback retry) to keep sub-path apps working.
const ASSETS_URL = typeof self.assetsUrl === 'string' ? self.assetsUrl : 'service-worker-assets.js';

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

// State for sendError's non-fatal console cap (see the sendError implementation below for what
// it does). Declared HERE, ahead of the manifest validation, rather than beside sendError:
// sendError is a hoisted function declaration and the validation below calls it during module
// evaluation, so `let`/`const` bindings placed next to it would still be in their temporal dead
// zone at that moment - a ReferenceError waiting on the first non-fatal error reported early.
const MAX_NONFATAL_CONSOLE_ERRORS = 10;
let nonFatalConsoleErrors = 0;

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

// Cache names are qualified with this registration's scope path so that sibling Bswup apps
// mounted under different scopes on the SAME origin get disjoint cache namespaces.
// CacheStorage is origin-global, and the pre v-10-6-0 name (`bit-bswup - <version>`) carried
// no scope: every app's deleteOldCaches() saw a sibling's bucket as "a stale version of
// mine" and deleted it, so two Bswup apps on one origin perpetually evicted each other's
// offline caches on every update. The scope path is the natural qualifier - it is exactly
// the boundary matchScopeClients() already scopes messaging and claiming to, it is stable
// across versions, and two registrations can never share one. Falls back to '/' when
// self.registration is unavailable (exotic runtimes), which reproduces a root-scoped name.
const SCOPE_PATH = (() => {
    try { return new URL(self.registration.scope).pathname; } catch { return '/'; }
})();

// Current format: `bit-bswup:<scope-path> - <version>`. The ':' can never appear in the
// legacy scope-less format (`bit-bswup - <version>`), which keeps the two distinguishable:
// legacy buckets are warm-started from and then reclaimed (see createAssetsCache /
// isOwnStaleCacheKey), never mistaken for another scope's live bucket. Both prefixes end in
// ' - ' so a scope path that extends another ('/app/' vs '/app2/') can never prefix-match
// the wrong bucket.
const LEGACY_CACHE_PREFIX = `${CACHE_NAME_PREFIX} - `;
const SCOPED_CACHE_PREFIX = `${CACHE_NAME_PREFIX}:${SCOPE_PATH} - `;
const CACHE_NAME = `${SCOPED_CACHE_PREFIX}${CACHE_VERSION}`;

// Applies one preset default: fills a setting ONLY when the app left it undefined, so an
// explicit assignment made before importScripts always wins - including falsy ones. `||=` is
// the wrong tool for this (it also overwrites legitimate falsy config: an explicit
// `caseInsensitiveUrl = false` came back true, `noPrerenderQuery = ''` came back
// 'no-prerender=true', and isPassive - previously a bare assignment - was clobbered outright).
// `??=` is close but not identical: it also overwrites an explicit `null`, and tsc downlevels
// it to the ES2019 this bundle targets anyway - the explicit undefined check is kept because
// it states the exact contract ("only fill what the app never set"), not for syntax reasons.
function presetDefault(key: string, value: any) {
    if ((self as any)[key] === undefined) (self as any)[key] = value;
}

// Named presets that expand into a coherent bundle of the individual self.* settings, so an
// app can pick a caching strategy with a single `mode` value instead of wiring each flag.
// The comment beside each case names a representative app using that strategy. Entries whose
// preset value is falsy (forcePrerender: false, noPrerenderQuery: '') are behaviorally no-ops;
// they are kept anyway so each case reads as the complete bundle it configures.
switch (self.mode) {
    // NoPrerender (e.g. adminpanel) and InitialPrerender (e.g. todo) share the same
    // service-worker preset: passive caching, no forced prerender, lax tolerance, and the
    // no-prerender query appended to the default document. They differ only in how the
    // server renders the first response, which is outside the SW's control - so they
    // intentionally fall through to one block instead of duplicating it (the previous
    // byte-identical copies were a copy-paste drift hazard).
    case 'NoPrerender':
    case 'InitialPrerender':
        presetDefault('isPassive', true);
        presetDefault('defaultUrl', '/');
        presetDefault('forcePrerender', false);
        presetDefault('errorTolerance', 'lax');
        presetDefault('caseInsensitiveUrl', true);
        presetDefault('noPrerenderQuery', 'no-prerender=true');
        break;
    case 'AlwaysPrerender': // like sales
        presetDefault('isPassive', true);
        presetDefault('defaultUrl', '/');
        presetDefault('forcePrerender', true);
        presetDefault('errorTolerance', 'lax');
        presetDefault('caseInsensitiveUrl', true);
        presetDefault('noPrerenderQuery', '');
        break;
    case 'FullOffline': // like todo-offline
        presetDefault('isPassive', false);
        presetDefault('defaultUrl', '/');
        presetDefault('forcePrerender', false);
        presetDefault('errorTolerance', 'lax');
        presetDefault('caseInsensitiveUrl', true);
        presetDefault('noPrerenderQuery', '');
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
self.addEventListener('install', (e: BswupExtendableEvent) => e.waitUntil(handleInstall(e)));
self.addEventListener('activate', (e: BswupExtendableEvent) => e.waitUntil(handleActivate(e)));
// handleFetch decides *synchronously* whether to call respondWith, so requests Bswup does not
// manage stay on the browser's own network path (see the comment on handleFetch).
self.addEventListener('fetch', handleFetch);
self.addEventListener('message', handleMessage);

async function handleInstall(e: BswupExtendableEvent) {
    diag('installing version:', VERSION);

    // Diagnostics-only storage telemetry: quota pressure is the root cause behind most
    // 'cache' / 'install-infra' failures, and seeing usage vs quota next to the failure makes
    // those reports actionable. Strictly best-effort and never awaited - the API is missing
    // in some private modes, and a telemetry call must not delay or fail an install.
    if (self.enableDiagnostics) {
        try {
            const storage = (self as any).navigator && (self as any).navigator.storage;
            if (storage && typeof storage.estimate === 'function') {
                storage.estimate().then(
                    (estimate: any) => diag('storage estimate - usage:', estimate && estimate.usage, 'quota:', estimate && estimate.quota),
                    () => { /* best effort */ });
            }
        } catch { /* best effort */ }
    }

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

    // Asset entries whose URL failed to parse during module evaluation (see
    // INVALID_ASSET_ERRORS): surfaced here so they are reported exactly once per install.
    INVALID_ASSET_ERRORS.forEach(err => sendError(err));

    if (self.errorTolerance === 'strict') {
        // Strict: any required asset that fails to fetch / store must reject the install
        // promise so the SW lifecycle treats it as a failed install. Without this, a
        // partially-populated cache becomes the new active cache on the next reload.
        try {
            await createAssetsCache();
        } catch (err) {
            // Per-asset failures and the strict abort report themselves before rejecting
            // (reason 'fetch'/'integrity'/'cache'/'install-aborted'). Anything else landing
            // here is an infrastructure failure - caches.open()/keys() rejecting under
            // storage pressure, a corrupted origin store, an exotic private mode - that
            // previously propagated with NO structured error at all: the install just failed
            // silently and, on a first install, the page sat behind the splash forever with
            // nothing telling it to boot. Report it as terminal (the page force-starts
            // Blazor on a fatal first-install error) before letting the install reject.
            reportUnreportedInstallFailure(err);
            throw err;
        }
    } else {
        // Lax: asset failures never fail the install - createAssetsCache resolves even when
        // assets were skipped (they lazy-fill in handleFetch) - but the download itself must
        // still run under the install event's lifetime (handleInstall IS the waitUntil
        // promise, so awaiting here extends it). Versions before v-10-6-0 fired the cache build
        // unawaited so the install settled immediately, which had two nasty consequences:
        //   - Nothing kept the worker alive. Browsers terminate an event-idle service worker
        //     after ~30s (fetches/timers the worker itself starts do not extend its
        //     lifetime), so any download longer than that could be killed mid-install:
        //     progress stopped, and on a first install the page - which waits for 100%
        //     before starting Blazor - froze behind the splash forever.
        //   - The worker reached 'installed' (waiting) the moment the download *started*, so
        //     the page announced updateReady - and an autoReload handler activated the new
        //     worker - while the new cache was still half-empty, deleting the previous
        //     complete cache right after the claim.
        // Awaiting fixes both: the worker stays alive for the whole download, and 'waiting'
        // again means "fully staged". Browsers cap the extension (Chromium ~5 minutes); a
        // download outlasting the cap now fails the install cleanly - retried on the next
        // load, with the page-side stall watchdog and the redundant-worker handler in
        // bit-bswup.ts still booting the app - instead of leaving a silent zombie cache.
        //
        // The catch keeps lax semantics: asset failures are already handled inside
        // createAssetsCache, so anything landing here is an infrastructure failure
        // (caches.open/keys rejecting). That used to be swallowed with a diag - no progress,
        // no bypass, no error message - which also left a first install hanging behind the
        // splash. Report it as terminal so the page can react, then resolve: with no cache
        // at all the worker still works as a network pass-through (serveAsset guards every
        // cache call), so failing the install would only take away that recovery path.
        await createAssetsCache().catch(err => {
            diag('*** lax install - createAssetsCache failed:', err);
            reportUnreportedInstallFailure(err);
        });
    }
}

// Reports an install failure that has not already been surfaced as a structured error.
// Per-asset failures and the strict abort report themselves and flag the propagated error
// with bswupReported; everything else is an infrastructure failure the page would otherwise
// never hear about - and on a first install the fatal error is what makes the page
// force-start Blazor instead of waiting behind the splash for messages that will never come.
function reportUnreportedInstallFailure(err: any) {
    if (err && err.bswupReported) return;
    sendError({
        reason: 'install-infra',
        message: 'Install failed before assets could be cached: ' + (err && err.message || String(err)),
        fatal: true,
    });
}

async function handleActivate(e: BswupExtendableEvent) {
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
        // Scope-filtered deliberately: only clients under this registration's scope can be
        // relying on our caches, so a sibling app's open tabs on the same origin must not
        // defer the pruning forever.
        const windowClients = await matchScopeClients();
        if (windowClients.length === 0) {
            diag('activate - no open window clients; pruning old caches.');
            await safeDeleteOldCaches('activate');
        } else {
            diag('activate - open window clients present; deferring cache cleanup:', windowClients.length);
        }
    } catch (err) {
        diag('*** activate - cache cleanup check failed:', err);
    }
}

// ============================================================================

const DEFAULT_URL = (typeof self.defaultUrl === 'string') ? self.defaultUrl : 'index.html';
// Resolved form of DEFAULT_URL, used to pair it with the asset that serves as the SPA default
// document (see uniqueAssets). Comparing resolved URLs instead of raw strings lets equivalent
// spellings match - 'index.html' vs '/index.html' on a root-mounted app - instead of silently
// losing the default document (and with it offline navigation) to a formatting difference.
const DEFAULT_REQ_URL = (() => { try { return new Request(DEFAULT_URL).url; } catch { return DEFAULT_URL; } })();
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
// Service-worker scripts must not be precached: the browser fetches them through its own
// update pipeline (updateViaCache: 'none'), never through this worker's fetch handler, so a
// cached copy is dead weight at best. All shipped variants are excluded - the minified
// bundles and the cleanup worker included - not just the exact files the old list named.
const DEFAULT_ASSETS_EXCLUDE = [
    /^_content\/Bit\.Bswup\/bit-bswup\.sw\.js$/,
    /^_content\/Bit\.Bswup\/bit-bswup\.sw\.min\.js$/,
    /^_content\/Bit\.Bswup\/bit-bswup\.sw-cleanup\.js$/,
    /^_content\/Bit\.Bswup\/bit-bswup\.sw-cleanup\.min\.js$/,
    /^service-worker\.js$/,
];

const ASSETS_INCLUDE = (self.ignoreDefaultInclude ? [] : DEFAULT_ASSETS_INCLUDE).concat(USER_ASSETS_INCLUDE);
const ASSETS_EXCLUDE = (self.ignoreDefaultExclude ? [] : DEFAULT_ASSETS_EXCLUDE).concat(USER_ASSETS_EXCLUDE);

diag('ASSETS_INCLUDE:', ASSETS_INCLUDE);
diag('ASSETS_EXCLUDE:', ASSETS_EXCLUDE);

// Invalid asset entries found while normalizing the asset list. Collected here and reported
// from handleInstall - NOT at module scope: the global scope re-evaluates on every cold start
// of the worker (browsers terminate idle workers between events), so a module-scope sendError
// would replay the same 'request' error to the page for the whole lifetime of the version,
// hours after the install, every time the worker wakes.
const INVALID_ASSET_ERRORS: any[] = [];

const ALL_ASSETS = (MANIFEST_VALID && Array.isArray(self.assetsManifest.assets) ? self.assetsManifest.assets : [])
    .filter((asset: any) => ASSETS_INCLUDE.some(pattern => pattern.test(asset.url)))
    .filter((asset: any) => !ASSETS_EXCLUDE.some(pattern => pattern.test(asset.url)))
    .concat(EXTERNAL_ASSETS);

diag('ALL_ASSETS:', ALL_ASSETS);

const UNIQUE_ASSETS = uniqueAssets(ALL_ASSETS);

diag('UNIQUE_ASSETS:', UNIQUE_ASSETS);

// Precomputed router lookups. handleFetch runs for EVERY GET on the origin, and a Blazor app
// ships hundreds of assets - scanning UNIQUE_ASSETS with a regex .test() per entry made the
// hot path O(assets) per request. Concrete assets (the overwhelming majority) resolve by
// exact origin+pathname equality - precisely the semantics their urlToRegExp matcher encodes
// (anchored origin+pathname, any query tolerated) - so they live in a Map for O(1) lookup;
// only pattern assets (RegExp externalAssets entries, usually a handful) still need a linear
// regex scan. Keys fold case per caseInsensitiveUrl, mirroring the `i` flag the regexes get.
// First occurrence wins on a key collision, preserving the old first-match-wins scan order;
// the one deliberate divergence is that a concrete asset now always beats a pattern asset
// for the same URL (exact beats wildcard), where the scan let an earlier-listed pattern win.
const CONCRETE_ASSETS = new Map<string, any>();
for (const asset of UNIQUE_ASSETS) {
    if (!asset.reqUrl) continue;
    const u = new URL(asset.reqUrl);
    const key = foldUrlKey(`${u.origin}${u.pathname}`);
    if (!CONCRETE_ASSETS.has(key)) CONCRETE_ASSETS.set(key, asset);
}
const PATTERN_ASSETS = UNIQUE_ASSETS.filter(a => !a.reqUrl);
const DEFAULT_ASSET = UNIQUE_ASSETS.find(a => a.isDefault);

// Case folding for asset-lookup keys; also what pairs defaultUrl with its asset. Hoisted
// (function declaration) because uniqueAssets() runs at module scope above.
function foldUrlKey(s: string) {
    return self.caseInsensitiveUrl ? s.toLowerCase() : s;
}

// O(1) exact origin+pathname lookup over the concrete assets. new URL() can throw on exotic
// URLs (about:, data: - the browser can dispatch fetch events for them); those are never
// assets. Used on its own for navigations (see handleFetch), which must never consult the
// pattern assets.
function findConcreteAssetForUrl(url: string) {
    try {
        const u = new URL(url);
        return CONCRETE_ASSETS.get(foldUrlKey(`${u.origin}${u.pathname}`));
    } catch {
        return undefined;
    }
}

// Resolves a request URL to a managed asset: the concrete lookup first, then a regex scan
// over the few pattern assets.
function findAssetForUrl(url: string) {
    return findConcreteAssetForUrl(url) || PATTERN_ASSETS.find(a => a.url.test(url));
}

// A missing default asset silently disables offline navigation: every navigate request is
// answered with the SPA default document, resolved by comparing defaultUrl and each asset's
// declared url as *resolved* URLs - and when nothing matches, handleFetch just quietly passes
// navigations through to the network, so the app looks fine online and only fails in the
// field, offline. Surface the misconfiguration loudly at startup instead. The common cause is
// a Blazor Web app whose manifest has no 'index.html' entry at all (pair `self.defaultUrl =
// '/'` with `self.externalAssets = [{ url: '/' }]`). Irrelevant under forcePrerender, where
// navigations are deliberately left to the server.
if (MANIFEST_VALID && !self.forcePrerender && !DEFAULT_ASSET) {
    console.warn(`BitBswup SW: defaultUrl ('${DEFAULT_URL}') matches no asset - navigations will NOT be served from cache and the app will not work offline. Point self.defaultUrl at an entry that exists in service-worker-assets.js / externalAssets (e.g. self.defaultUrl = '/' together with self.externalAssets = [{ url: '/' }]).`);
}

diagGroupEnd();

// Runtime request router. For every GET this decides whether to serve the request from the
// Bswup cache, fall back to the SPA default document, or leave it alone. High-level flow:
//   1. Block prohibited URLs (403) and IGNORE non-GET / server-handled requests.
//   2. For navigations, serve the default document unless the URL is server-rendered or
//      forcePrerender is on (then the server owns the HTML).
//   3. Resolve the request to an asset by testing each asset's (RegExp) url against it.
//   4. Serve the asset from cache. On a cache miss the response is fetched from the network and
//      lazily written back to the cache in every mode: passive mode and pattern assets (e.g.
//      resource-collection.<hash>.js) fetch with the versioned request, and in active mode a
//      precached asset that misses fetches with the page's own request but is still cached, so
//      an asset that failed during a lax install (or was evicted) becomes available offline
//      after its first successful fetch. Hash-less assets carry no version to diff, so they are
//      re-downloaded on every update (see createAssetsCache), not on every request.
//
// THIS FUNCTION IS DELIBERATELY SYNCHRONOUS. respondWith() is a commitment: the moment it is
// called the worker owns the response and the browser will NOT fall back to its own network
// stack, so a rejected promise turns into a hard network error for that request - even for a
// request Bswup has no interest in. Previous versions called
// `e.respondWith(handleFetch(e))` unconditionally and then decided, which meant every request
// on the origin was routed through a `fetch()` inside the worker whose failure it could not
// recover from. That is the "frozen progress bar" failure: during an install the page is
// pulling Blazor's boot assets, one of those proxied fetches blips, the request hard-fails,
// Blazor never finishes starting, and the splash sits at whatever percent it had reached until
// the user refreshes.
//
// So: decide first, commit second. Every check needed to route a request is synchronous, and
// for anything we do not manage we simply return without calling respondWith - leaving the
// browser to do exactly what it would have done with no service worker installed. Only the
// managed path calls respondWith, and it is handed serveAsset(), which never rejects.
function handleFetch(e: BswupExtendableEvent) {
    const req = e.request as Request;

    if (PROHIBITED_URLS.some(pattern => pattern.test(req.url))) {
        diagFetch('+++ handleFetch ended - prohibited:', e, req);

        // 403 Forbidden: the request was understood and is being refused. Versions before
        // v-10-6-0 answered 405 Method Not Allowed, which is wrong on both counts - it says the
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
        return e.respondWith(new Response('This URL is prohibited!', {
            status: 403,
            statusText: 'Prohibited',
            headers: {
                'Content-Type': 'text/plain; charset=utf-8',
                'X-Content-Type-Options': 'nosniff',
            }
        }));
    }

    // Not ours: no respondWith, so the browser handles it exactly as it would with no service
    // worker installed. Proxying these through a fetch() in the worker only added a failure
    // mode - it could never produce a better response than the browser's own request.
    const isServerHandled = SERVER_HANDLED_URLS.some(pattern => pattern.test(req.url));
    if (req.method !== 'GET' || isServerHandled) {
        diagFetch('*** handleFetch ended - ignored - !GET or SERVER_HANDLED_URLS:', e, req);
        return;
    }

    const isServerRendered = SERVER_RENDERED_URLS.some(pattern => pattern.test(req.url));
    const shouldServeDefaultDoc = (req.mode === 'navigate') && !isServerRendered && !self.forcePrerender;

    // Only materialized when fetch diagnostics are on: this runs on EVERY request the worker
    // sees, and allocating a Date + ISO string per fetch is pure waste when nothing logs it.
    const start = self.enableFetchDiagnostics ? new Date().toISOString() : '';

    // Concrete assets resolve via the precomputed origin+pathname Map, pattern assets via
    // their RegExp (see findAssetForUrl). Navigations resolve to the SPA default document -
    // but only when the navigated URL is not itself a CONCRETE managed asset: opening
    // /manifest.json or an image directly in a tab must show that file, not the app shell
    // rendered under the wrong URL. Route URLs (/counter, /admin/...) match no asset and
    // still fall through to the default document, so deep links keep working offline.
    // Pattern assets are deliberately NOT consulted for navigations: a broad RegExp
    // externalAssets entry that happens to match a route URL would otherwise hijack that
    // route away from the shell - caching its HTML under the route URL as a bogus "pattern
    // generation" and answering it with a network error offline instead of the app shell.
    const asset = shouldServeDefaultDoc
        ? (findConcreteAssetForUrl(req.url) || DEFAULT_ASSET)
        : findAssetForUrl(req.url);

    if (!asset) {
        diagFetch('+++ handleFetch ended - ignored - asset not found:', start, req.url, e, req);
        return;
    }

    if (self.forcePrerender && asset.isDefault) {
        diagFetch('+++ handleFetch ended - ignored - forcePrerender defaultDoc:', start, asset, e, req);
        return;
    }

    // From here on the request is one Bswup manages, so it is worth taking over.
    e.respondWith(serveAsset(e, req, asset, start));
}

// Produces the response for a request Bswup manages. This must NEVER reject: it is handed
// straight to respondWith, and a rejection there is an unrecoverable network error for the
// page rather than a fallback to the network. Every await is therefore guarded, and the
// degradation order is cache -> network -> stale cache -> explicit network error.
//
// A navigation request's redirect mode is 'manual', not 'follow', so replaying a response whose
// `redirected` flag is set throws "a redirected response was used for a request whose redirect
// mode is not 'follow'" and hard-fails the navigation. That flag gets set whenever the host
// 30x-redirects the shell URL - '/' -> '/index.html' on Cloudflare Pages / Netlify / many
// reverse proxies is the classic case - so the followed response Bswup caches (or fetches) for
// the app shell carries redirected === true and every later offline navigation to a deep link
// breaks. serveAssetCore does the real work; this wrapper strips the redirect flag from the
// response before it reaches a navigation. cleanRedirect is a no-op for the overwhelmingly
// common non-redirected response, so the hot path buffers nothing.
async function serveAsset(e: any, req: Request, asset: any, start: string) {
    const response = await serveAssetCore(e, req, asset, start);
    return (req && (req as any).mode === 'navigate') ? await cleanRedirect(response) : response;
}

// Rebuilds a response so its `redirected` flag is cleared (see serveAsset). Returns the response
// untouched unless it is actually redirected - the common path pays nothing. Best-effort: on any
// read failure the original response is returned unchanged, which is no worse than the network
// error an offline navigation would otherwise get. The reconstructed response uses the followed
// target's 2xx status (never a 3xx - Response rejects a null body with a redirect status, and a
// navigation cannot consume a bare redirect anyway).
async function cleanRedirect(response: Response) {
    if (!response || !(response as any).redirected) return response;
    try {
        const body = await response.clone().blob();
        const init: any = {
            status: (response as any).status || 200,
            statusText: (response as any).statusText || '',
            headers: new Headers((response as any).headers),
        };
        diagFetch('*** cleanRedirect - rebuilding a redirected navigation response:', (response as any).url);
        return new Response(body, init);
    } catch (err) {
        diagFetch('*** cleanRedirect failed - returning the response unchanged:', err);
        return response;
    }
}

async function serveAssetCore(e: any, req: Request, asset: any, start: string) {
    // Concrete assets are keyed/fetched via their reqUrl (+ hash / ?v= cache-buster). A pattern
    // asset (a RegExp externalAssets entry, e.g. resource-collection.<hash>.js) has no precomputed
    // URL, so it is keyed and fetched by the actual request.
    const cacheUrl = asset.reqUrl ? createCacheUrl(asset) : req.url;

    // Whether the page's own request actually targets this asset's URL. False exactly when the
    // asset was chosen as the NAVIGATION fallback: req.url is a route (/counter) and the asset
    // is the SPA default document. In that case the page's request must NEVER be used to fetch
    // bytes that get written under the asset's cache key - the server's answer for /counter is
    // route-specific (often prerendered) HTML, and caching it as the app shell would poison
    // every future navigation with it. Those paths fetch the asset's own versioned URL instead.
    const reqMatchesAsset = asset.reqUrl
        ? !!(asset.url && typeof asset.url.test === 'function' && asset.url.test(req.url))
        : true; // pattern assets are keyed by the live request URL - the request IS the asset

    // CacheStorage is not guaranteed available: it throws under storage pressure, in some
    // private-browsing modes, and when the origin's quota is exhausted. Losing the cache is
    // survivable (we fall through to the network); letting it reject is not.
    const bitBswupCache = await tryCacheOpen();
    const cachedResponse = bitBswupCache ? await tryCacheMatch(bitBswupCache, cacheUrl) : undefined;

    if (cachedResponse) {
        diagFetch('+++ serveAsset ended - using cache.', start, asset);
        // Ranged requests get a real 206 sliced from the cached full body (see
        // applyRangeHeader) - Safari in particular refuses to play cached media served as a
        // 200 in response to a Range request.
        return await applyRangeHeader(req, cachedResponse);
    }

    // In active (non-passive) mode a precached asset that missed the cache goes to the network
    // with the page's own request - but the response is still written back to the cache. A miss
    // here means the asset failed to precache during a lax install or its entry was evicted;
    // without the write-back it would stay uncached forever (install never re-runs until the
    // next update), every request would hit the network, and the asset would never become
    // available offline - contradicting the documented lax semantics, which promise lazy fill
    // on first fetch in BOTH modes. Pattern assets don't take this path (no reqUrl); they fall
    // through to the versioned-request lazy path below - as does a navigation-fallback request
    // (see reqMatchesAsset), whose route URL must not supply the bytes cached under the
    // default document's key.
    if (!self.isPassive && asset.reqUrl && reqMatchesAsset) {
        // The write-back below caches whatever comes back under the asset's key, so when SRI is
        // enabled for this asset the bytes must be verified first. Fetching the page's own plain
        // request (which carries no `integrity`) would let a lazily-healed asset - one that
        // missed a lax precache or was evicted - accept tampered/mismatched bytes and cache them
        // as trusted, served offline forever. Every other download path attaches integrity
        // (addCache and the versioned lazy path below both go through createNewAssetRequest); this
        // branch used `req` directly, silently downgrading SRI. Fetch the verified versioned
        // request instead when integrity applies. Skipped for a ranged request: a 206 cannot be
        // SRI-verified anyway, and the page's own Range must reach the server (Safari media), so
        // that case keeps using `req`. createNewAssetRequest can throw on a pathological composed
        // URL, so guard it and fall back to the plain request (serveAsset must never reject).
        let healRequest: Request = req;
        if (asset.hash?.startsWith('sha') && self.enableIntegrityCheck && !getRangeHeader(req)) {
            try { healRequest = createNewAssetRequest(asset); } catch (err) {
                diagFetch('*** serveAsset - createNewAssetRequest threw on the heal path; using the plain request:', err);
                healRequest = req;
            }
        }
        const response = await tryFetch(healRequest);
        if (!response) {
            return await lastResort(bitBswupCache, req, asset);
        }

        lazyFill(e, bitBswupCache, cacheUrl, response, asset);

        diagFetch('+++ serveAsset ended - active-mode cache miss, lazily (re)caching asset:', start, asset);
        return response;
    }

    // The versioned request drops the page's own headers - including Range. Answering a
    // ranged request with a full 200 body breaks media playback (Safari), and slicing a
    // *network* response here would mean buffering the whole stream before the first byte
    // reaches the page. So a ranged request goes out as the page sent it: the server answers
    // 206 itself, and lazyFill refuses partial responses so the cache only ever holds full
    // bodies (filled by whatever non-ranged request comes first). Except when the request
    // does not target the asset's own URL (a ranged navigation fallback, a degenerate case):
    // there the versioned request is the only one that fetches the right bytes.
    // serveAsset must never reject (it is handed straight to respondWith, where a rejection is a
    // hard network error, not a fallback). createNewAssetRequest can throw on a pathological
    // composed URL - new URL()/new Request() - exactly as addCache guards against at install
    // time. Build it defensively and fall back to the page's own request on failure.
    let request: Request = req;
    if (asset.reqUrl && (!getRangeHeader(req) || !reqMatchesAsset)) {
        try { request = createNewAssetRequest(asset); } catch (err) {
            diagFetch('*** serveAsset - createNewAssetRequest threw; using the plain request:', err);
            request = req;
        }
    }
    let response = await tryFetch(request);

    // The versioned request carries a `?v=` buster and, with enableCacheControl, no-store
    // headers - either of which a fussy proxy or an offline-ish network can reject while the
    // page's own plain request still succeeds. Retry with it before giving up. (When the
    // ranged path above already used the plain request, there is nothing different to retry.)
    //
    // Not when the request carried an integrity hash, though: SRI failures surface as a
    // rejected fetch, and retrying without integrity would serve exactly the unverified bytes
    // the check exists to reject. A tampered asset must fail, not silently downgrade.
    // And not when the page's request doesn't target the asset's URL (navigation fallback):
    // fetching the route URL would cache route-specific HTML under the shell's key.
    if (!response && asset.reqUrl && request !== req && reqMatchesAsset && !(request as any).integrity) {
        diagFetch('*** serveAsset - versioned request failed, retrying with the plain request:', start, asset);
        response = await tryFetch(req);
    }

    if (!response) {
        return await lastResort(bitBswupCache, req, asset);
    }

    lazyFill(e, bitBswupCache, cacheUrl, response, asset);

    diagFetch('+++ serveAsset ended - lazily caching asset:', start, asset, e, req);

    return response;
}

// Writes a fetched response into the cache in the background while the page consumes it.
// response.clone() tees the stream so the page and the cache write consume bytes as they
// arrive; e.waitUntil keeps the worker alive until the background write completes. Cached:
// ok (2xx) responses, and opaque responses (cross-origin no-cors - status reads 0 and ok is
// false BY DESIGN, exactly how a script/img tag consumes a cross-origin externalAssets entry;
// refusing them would keep such assets out of the cache forever and break their offline
// story). Error statuses are never cached - they must not shadow the network. A failed put
// (quota, storage pressure) is logged and swallowed: losing the write-back is survivable,
// breaking the in-flight response is not. Note that browsers pad opaque entries heavily in
// quota accounting (Chromium reserves megabytes per entry) - the price of caching responses
// whose real size is deliberately hidden.
function lazyFill(e: any, cache: any, cacheUrl: string, response: Response, asset: any) {
    if (!cache) return;
    // A 206 Partial Content body (the server honoring a Range header the page sent) must
    // never be written under the asset's cache key: response.ok is true for 206, so without
    // this check one ranged media request could pin a partial body into the cache and every
    // later request - including full ones - would be served that fragment as if it were the
    // whole file, forever. Full bodies land in the cache via non-ranged requests instead.
    if ((response as any).status === 206) return;
    if (!response.ok && (response as any).type !== 'opaque') return;

    const cachePut = cache.put(cacheUrl, response.clone()).catch((err: any) => {
        diagFetch('+++ serveAsset - lazy-fill put failed:', err, asset);
    });
    e.waitUntil(cachePut);
}

// Matches the `<hash>` tail of a `reqUrl.<hash>` cache key: an SRI-style digest (sha256-...,
// sha384-..., sha512-...) whose base64 alphabet can never contain a '.'. This is what keeps a
// sibling file whose URL merely extends the asset's (app.js -> app.js.map, app.js.br, or a
// hashed app.js.map.<hash>) from ever being mistaken for a stale copy of the asset itself.
const STALE_HASH_KEY_SUFFIX = /^sha\d+-[A-Za-z0-9+\/=]+$/;

// The network is gone and we already committed to responding. Any previously cached copy of
// this asset beats a hard failure - it is how an offline reload still boots - and when there
// is nothing at all, Response.error() produces the same network error the page would have
// seen without a service worker (rather than an unhandled rejection).
async function lastResort(cache: any, req: Request, asset: any) {
    if (cache) {
        // Exact raw-URL entries: pattern assets are keyed by the live request URL, and
        // hashless assets by reqUrl alone.
        const stale = await tryCacheMatch(cache, req.url);
        if (stale) {
            diagFetch('*** serveAsset - network failed, serving a stale cached copy:', req.url, asset);
            return await applyRangeHeader(req, stale);
        }

        // Hashed assets are keyed `reqUrl.<hash>`, so the raw-URL lookup above can never hit
        // them: when the current-hash key missed (the reason serveAsset ended up here), a copy
        // from a previous version may still sit under the same URL with an older hash - e.g. a
        // lax background install whose worker was terminated after migrating the old cache but
        // before the diff/downloads completed. Scan for any such key. Serving bytes from a
        // different version is a calculated risk taken knowingly: this path only runs when the
        // network is unreachable AND the current version is not cached, where the alternative
        // is a guaranteed hard failure. Non-SRI custom hashes are deliberately not matched
        // (see STALE_HASH_KEY_SUFFIX) - a wrong-file false positive is worse than no fallback.
        if (asset && asset.reqUrl) {
            const prefix = `${asset.reqUrl}.`;
            const keys = await tryCacheKeys(cache);
            const staleKey = keys.find((key: any) => key && key.url
                && key.url.startsWith(prefix)
                && STALE_HASH_KEY_SUFFIX.test(key.url.slice(prefix.length)));
            const staleByHash = staleKey ? await tryCacheMatch(cache, staleKey.url) : undefined;
            if (staleByHash) {
                diagFetch('*** serveAsset - network failed, serving a previous-hash cached copy:', staleKey.url, asset);
                return await applyRangeHeader(req, staleByHash);
            }
        }
    }

    diagFetch('*** serveAsset - network failed and nothing cached:', req.url, asset);
    return Response.error();
}

// cache.keys() that reports failure as an empty list instead of a rejection - CacheStorage
// enumeration can throw under the same storage pressure that makes match()/open() fail, and
// the stale-copy scan above is strictly best-effort.
async function tryCacheKeys(cache: any) {
    try {
        return await cache.keys();
    } catch (err) {
        diagFetch('*** tryCacheKeys - enumeration failed:', err);
        return [];
    }
}

// The request's Range header value, or '' when absent/unreadable. Guarded because exotic
// Request-alikes (and old test doubles) may lack a Headers object entirely.
function getRangeHeader(req: Request) {
    try {
        return (req.headers && typeof (req.headers as any).get === 'function' && req.headers.get('range')) || '';
    } catch {
        return '';
    }
}

// Serves the requested byte range out of a fully cached 200 response as a real 206 Partial
// Content. Media elements fetch audio/video with Range headers, and answering those with a
// cached 200 full body breaks playback - Safari outright refuses to play it. Only cached
// responses are sliced (their body is already materialized locally); network responses are
// never sliced here because that would buffer the entire stream before the page saw a byte -
// ranged network fetches instead go out with the page's own request so the server answers
// 206 itself (see serveAsset). Anything that can't be sliced safely - no Range header, a
// non-200 status (opaque bodies are unreadable, 206 is already partial), a malformed or
// multi-range spec, an unsatisfiable range, a read failure - falls back to returning the
// response unchanged, which is exactly the pre-10.5.0 behavior.
async function applyRangeHeader(req: Request, response: Response) {
    try {
        const rangeHeader = getRangeHeader(req);
        if (!rangeHeader) return response;
        if (response.status !== 200) return response;

        // A stored 200 that still declares a transfer Content-Encoding (gzip/br/...) cannot be
        // byte-sliced: blob() yields the DECODED body, so a range computed over decoded bytes
        // paired with the original encoded Content-Encoding header would tell the client to
        // inflate raw slice bytes - a corrupt result, with Content-Range/Content-Length in the
        // wrong units. Browsers normally strip Content-Encoding from a decoded Response so this
        // rarely fires; when it does, fall back to the full response rather than emit a
        // mislabeled 206.
        const headerBag: any = (response as any).headers;
        const contentEncoding = (headerBag && typeof headerBag.get === 'function' && headerBag.get('content-encoding') || '').trim().toLowerCase();
        if (contentEncoding && contentEncoding !== 'identity') return response;

        // Clone before reading: on any fallback path the original response must still carry
        // an unconsumed body for the page. Read as a Blob, not an ArrayBuffer: browsers keep
        // cached-response blobs disk-backed, and blob.slice() is a lazy view - so serving a
        // range never materializes the whole file in memory. Media elements issue MANY small
        // Range requests while seeking; buffering a full-length video per request was pure
        // memory waste for bytes the page never asked for.
        const blob = await response.clone().blob();
        const size = blob.size;
        const range = parseRangeHeader(rangeHeader, size);
        if (!range) return response;

        const headers = new Headers((response as any).headers);
        headers.set('content-range', `bytes ${range.start}-${range.end}/${size}`);
        headers.set('content-length', String(range.end - range.start + 1));
        return new Response(blob.slice(range.start, range.end + 1), { status: 206, statusText: 'Partial Content', headers });
    } catch (err) {
        diagFetch('*** applyRangeHeader failed - returning the full response:', err);
        return response;
    }
}

// Parses a single-range `bytes=` header against a known body size. Returns null for anything
// this code does not handle - multi-range specs, other units, malformed or unsatisfiable
// ranges - signalling the caller to fall back to the full response rather than guess.
function parseRangeHeader(value: string, size: number): { start: number, end: number } | null {
    const match = /^bytes=(\d*)-(\d*)$/.exec(String(value).trim());
    if (!match || (!match[1] && !match[2])) return null;

    let start: number;
    let end: number;
    if (!match[1]) {
        // Suffix form (`bytes=-500`): the final N bytes.
        const suffix = Number(match[2]);
        if (!suffix) return null; // 'bytes=-0' is unsatisfiable per RFC 9110
        start = Math.max(0, size - suffix);
        end = size - 1;
    } else {
        start = Number(match[1]);
        end = match[2] ? Math.min(Number(match[2]), size - 1) : size - 1;
    }

    if (size === 0 || start >= size || start > end) return null;
    return { start, end };
}

// fetch() that reports failure as `undefined` instead of a rejection, so callers can fall back.
async function tryFetch(request: Request) {
    try {
        return await fetch(request);
    } catch (err) {
        diagFetch('*** tryFetch - request failed:', (request && request.url) || request, err);
        return undefined;
    }
}

async function tryCacheOpen() {
    try {
        return await caches.open(CACHE_NAME);
    } catch (err) {
        diagFetch('*** tryCacheOpen - cache unavailable:', err);
        return undefined;
    }
}

async function tryCacheMatch(cache: any, url: string) {
    try {
        return await cache.match(url);
    } catch (err) {
        diagFetch('*** tryCacheMatch - lookup failed:', url, err);
        return undefined;
    }
}

// Handles commands posted from the page (bit-bswup.ts). Each branch corresponds to a string
// command in the page<->worker protocol; non-matching JSON messages are ignored.
function handleMessage(e: MessageEvent<string> & BswupExtendableEvent) {
    diag('handleMessage:', e);

    // Trust model: a service worker can only receive postMessage from clients on its own
    // origin (the browser enforces this), so every sender here is same-origin app code -
    // no cross-origin command injection is possible. We therefore don't filter on
    // e.origin / e.source. The commands below (SKIP_WAITING, CLAIM_CLIENTS, CLEAN_UP,
    // BLAZOR_STARTED) only drive this app's own SW lifecycle and caches.

    // Both command chains below run under e.waitUntil. A message handler's return value means
    // nothing to the browser - previous versions returned these promise chains bare, leaving
    // the worker eligible for termination between any two steps once the synchronous handler
    // finished. For SKIP_WAITING the activate event's own waitUntil usually kept the worker
    // alive incidentally; CLAIM_CLIENTS had no such cover, and its final CLIENTS_CLAIMED reply
    // is the ONLY trigger that starts Blazor on a first install - dropping it would leave the
    // app hanging behind the splash with no backup signal.

    // In both command chains below, deleteOldCaches() is best-effort tidiness and must never
    // block the signal that follows it. It enumerates and deletes via CacheStorage, which can
    // reject under the same storage pressure serveAsset/handleInstall are hardened against -
    // and an uncaught rejection would end the .then chain right before its most important
    // step: CLIENTS_CLAIMED is the ONLY trigger that starts Blazor on a first install (a
    // dropped one used to hang the app behind the splash until the page's stall watchdog
    // fired), and WAITING_SKIPPED is the initiating tab's reload signal (recoverable via
    // 'controllerchange', but a dropped message is still a dropped message). Stale caches
    // that survive here are reclaimed by the next activate/CLEAN_UP pass.

    if (e.data === 'SKIP_WAITING') {
        // Activate the waiting worker, then take control of every open client so each tab
        // receives a 'controllerchange' and reloads onto the new version (handled in
        // bit-bswup.ts > handleControllerChange). Claiming is what makes multi-tab updates
        // consistent: without it, sibling tabs keep running the old app code while their
        // asset requests are served from the new worker - or from a cache we just deleted -
        // which corrupts boot config / DLL hashes. Old caches are removed only *after* the
        // claim so no controlled client is left pointing at a cache that no longer exists.
        //
        // Captured BEFORE skipWaiting flips the lifecycle slots: no active worker right now
        // means the receiver is a FIRST install being activated through the skipWaiting path
        // (e.g. BitBswup.skipWaiting() catching the transient waiting state). The initiating
        // page must then receive the first-install completion signal - CLIENTS_CLAIMED, which
        // starts Blazor in place and triggers the post-start cache top-up - instead of the
        // update reload signal: by the time a WAITING_SKIPPED landed, the claim's
        // controllerchange had already marked the page as controlled-by-an-active-worker, so
        // the reload it triggers would kill the very boot that controllerchange just started.
        // Sibling tabs need nothing extra - their controllerchange handles the first-install
        // start. Defaults to update semantics when the registration is unavailable.
        const isFirstInstallActivation = !!(self.registration && !self.registration.active);
        return e.waitUntil(
            self.skipWaiting()
                .then(() => self.clients.claim())
                .then(() => safeDeleteOldCaches('SKIP_WAITING'))
                .then(() => isFirstInstallActivation
                    ? e.source?.postMessage('CLIENTS_CLAIMED')
                    : sendMessage('WAITING_SKIPPED')));
    }

    if (e.data === 'CLAIM_CLIENTS') {
        // First-install claim. Take control so this page can start Blazor; sibling tabs
        // that observe the resulting 'controllerchange' will NOT reload because there was
        // no previously-active worker (see hasActiveWorker in bit-bswup.ts).
        return e.waitUntil(
            self.clients.claim()
                .then(() => safeDeleteOldCaches('CLAIM_CLIENTS'))
                .then(() => e.source?.postMessage('CLIENTS_CLAIMED')));
    }

    if (e.data === 'BLAZOR_STARTED') {
        // Keep the worker alive until the post-start top-up pass settles. Asset failures are
        // handled inside createAssetsCache, but infrastructure failures (caches.open/keys
        // rejecting) can still reject it; the app is already running at this point, so log
        // and move on rather than raising a terminal error for a background top-up - and
        // rather than letting the rejection surface as a failed extended event.
        e.waitUntil(createAssetsCache(true).catch(err => {
            diag('*** BLAZOR_STARTED - background top-up failed:', err);
        }));
    }

    if (e.data === 'CLEAN_UP') {
        // App-triggered pruning of this app's stale cache buckets (the page exposes it as the
        // `cleanup` callback on downloadFinished). Guarded, because "stale" is relative to the
        // NEWEST worker, not to the receiver: deleteOldCaches() spares only the receiver's own
        // CACHE_NAME, so with an update staged or staging it would destroy a LIVE cache.
        // Received by the old active worker, it would wipe the update's freshly downloaded
        // cache and waste the entire download; received by a waiting worker (pre-10.5.0 pages
        // posted it there too), it would delete the ACTIVE worker's cache out from under every
        // running page, making old app code fetch new-version bytes - the classic SRI /
        // boot-hash corruption. A waiting worker always sees itself in registration.waiting,
        // so this one check covers both hazards. When skipped, nothing is lost: the
        // SKIP_WAITING / activate flows prune at the moment it is actually safe. The catch
        // matches the other command chains - pruning is best-effort tidiness.
        if (self.registration && (self.registration.waiting || self.registration.installing)) {
            diag('CLEAN_UP skipped - an update is staged or staging; pruning now would delete a live cache.');
            return;
        }
        // safeDeleteOldCaches re-checks the staged/staging condition at prune time; the early
        // return above just keeps the historical "skipped" diagnostic explicit for CLEAN_UP.
        e.waitUntil(safeDeleteOldCaches('CLEAN_UP'));
    }
}

// ============================================================================

// How many generations of a lazily-cached pattern asset survive an update, per pattern. A
// pattern asset (a RegExp externalAssets entry, e.g. resource-collection.<hash>.js) matches
// every fingerprint it ever cached, so the update diff cannot distinguish the current entry
// from dead ones - without a bound the cache would grow by one dead entry per update, forever.
// Three covers the entry the running version uses, the previous generation (a stale offline
// shell served by lastResort can still reference it and boot), and one of slack for an
// in-flight update.
const MAX_PATTERN_ASSET_GENERATIONS = 3;

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
        // loop below then prunes anything stale. The sources are exactly the buckets this app
        // owns (isOwnStaleCacheKey): this scope's previous versions plus legacy scope-less
        // buckets - never a sibling scope's bucket, whose entries belong to a different app.
        // (A legacy bucket on a multi-app origin may still hold a sibling's entries; that is
        // harmless - keys are full URLs, so foreign entries simply fail the diff below and
        // are pruned.) CACHE_NAME itself is excluded so we never copy the new cache onto
        // itself (cacheKeys already contains it, since we opened it above). Skip any key
        // already present so an entry migrated from one bucket isn't overwritten by a copy
        // in another.
        const oldCacheKeys = cacheKeys.filter(isOwnStaleCacheKey);
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
    // Passive first install: skip the download and let the page boot immediately (assets
    // lazy-fill as the app fetches them) - but only for the INSTALL run. The
    // post-BLAZOR_STARTED top-up (ignoreProgressReport) must proceed regardless: it is what
    // completes the offline cache in the background once the app is running. Gating it on
    // the cache being non-empty made that completion a race against the first lazy-fill
    // put - almost always won, but "the offline story depends on a put landing first" is
    // not a semantic; now the top-up deterministically fills whatever is still missing.
    if (passiveFirstTime && !ignoreProgressReport) {
        sendMessage({ type: 'bypass', data: { firstTime: true } });
        diagGroupEnd();
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
    const assetByCacheKey = new Map<string, any>();
    for (const asset of UNIQUE_ASSETS) {
        // Pattern assets (no concrete reqUrl) can't be precached or diffed; they are matched and
        // cached lazily by handleFetch, so skip them here.
        if (!asset.reqUrl) continue;
        // A pathological custom hash can make new Request() throw on the composed cache URL;
        // fall back to the raw string key (self-consistent for the diff) rather than letting
        // one bad entry reject the whole createAssetsCache pass as an install-infra failure.
        let diffKey: string;
        try {
            diffKey = new Request(createCacheUrl(asset)).url;
        } catch {
            diffKey = createCacheUrl(asset);
        }
        assetByCacheKey.set(foldUrlKey(diffKey), asset);
    }

    // Assets confirmed present at their current cache key - these are not re-downloaded.
    const cachedAssets = new Set<any>();

    // Lazily-cached keys per pattern asset, in enumeration (= insertion = age) order, so the
    // oldest generations beyond MAX_PATTERN_ASSET_GENERATIONS can be evicted after the loop.
    const patternKeys = new Map<any, string[]>();

    // Collect stale entries to delete and await them as a batch below, rather than firing
    // newCache.delete(...) unawaited. The unawaited form let deletes race the subsequent
    // addCache puts (and the concurrent post-BLAZOR_STARTED top-up run), so a freshly
    // written asset could be removed by a still-in-flight delete. Gathering keys first and
    // awaiting them keeps the cache state deterministic before we repopulate.
    const keysToDelete = [] as string[];
    for (let i = 0; i < newCacheKeys.length; i++) {
        const key = newCacheKeys[i];
        if (!key || !key.url) continue;

        const matched = assetByCacheKey.get(foldUrlKey(key.url));
        if (!matched) {
            // A key lazily cached for a pattern asset (no concrete reqUrl, e.g.
            // resource-collection.<hash>.js) has no entry in assetByCacheKey; it must survive
            // the diff so the app still boots offline - but not unboundedly. The pattern
            // matches every fingerprint it ever cached, so the diff cannot tell the current
            // entry from dead ones; collect matches per pattern here and evict the oldest
            // beyond MAX_PATTERN_ASSET_GENERATIONS after the loop.
            //
            // A hashed CONCRETE key (`...url.<sha...>`) can never be a lazily-cached pattern
            // entry - pattern assets are keyed by their live request URL, which carries no
            // hash suffix. Without this guard an unanchored pattern (e.g.
            // /resource-collection/) also matched the stale hashed keys of removed/re-hashed
            // concrete assets, retaining them as bogus "generations" that leak storage and
            // evict genuine generations from the cap slots.
            const lastDot = key.url.lastIndexOf('.');
            const isHashedConcreteKey = lastDot !== -1 && STALE_HASH_KEY_SUFFIX.test(key.url.slice(lastDot + 1));
            const pattern = isHashedConcreteKey ? undefined : PATTERN_ASSETS.find(a => a.url.test(key.url));
            if (pattern) {
                diag('*** keeping lazily-cached pattern asset key (subject to the generation cap):', key.url);
                const kept = patternKeys.get(pattern) || [];
                kept.push(key.url);
                patternKeys.set(pattern, kept);
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
        // opts out via disableHashlessAssetsUpdate. The existing entry is deliberately NOT
        // deleted here: cache.put() overwrites the same key atomically on success, while
        // deleting first meant a failed re-download under lax left NO cached copy at all -
        // an asset (or the app shell) that had been available offline went dark until some
        // later fetch happened to succeed. Keeping the old entry until the new bytes land
        // degrades to "one version stale" instead of "gone".
        // The refresh is skipped for the post-BLAZOR_STARTED top-up (ignoreProgressReport):
        // that run happens seconds after the install already fetched these exact bytes, and
        // re-refusing to mark them cached made every install download all hashless assets
        // (and the default document, below) twice.
        if (!matched.hash && !self.disableHashlessAssetsUpdate && !ignoreProgressReport) {
            diag('*** refreshing hashless cache key (old copy kept until the new download lands):', key.url);
        } else {
            cachedAssets.add(matched);
        }
    }

    // Evict the oldest pattern-asset generations beyond the cap. Cache.keys() enumerates in
    // insertion order and the migration above copies old-bucket entries in that same order
    // before anything new is written, so the front of each per-pattern list is the oldest.
    patternKeys.forEach((keptKeys) => {
        for (let i = 0; i < keptKeys.length - MAX_PATTERN_ASSET_GENERATIONS; i++) {
            diag('*** evicting old pattern asset generation:', keptKeys[i]);
            keysToDelete.push(keptKeys[i]);
        }
    });

    // Always refresh the default document on each update so navigations pick up the latest
    // app shell even when its hash is unchanged. Dropping it from the kept set is enough to
    // re-fetch it below; the current entry is NOT deleted first (cache.put overwrites in
    // place), so a failed refresh under lax keeps serving the previous shell offline instead
    // of losing offline navigation entirely. Skipped for the top-up run for the same reason
    // as the hashless refresh above - the install fetched the shell seconds ago.
    if (!ignoreProgressReport && DEFAULT_ASSET && cachedAssets.has(DEFAULT_ASSET)) {
        cachedAssets.delete(DEFAULT_ASSET);
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

        const abortError = new Error(abortMessage) as any;
        // Consumed by handleInstall: this failure is already reported (the 'install-aborted'
        // sendError above), so it must not be double-reported as 'install-infra' on the way out.
        abortError.bswupReported = true;
        throw abortError;
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
        // Whether this asset lives on another origin - only those can hit the "host sends no
        // CORS headers" failure that the no-cors fallback below exists for.
        const isCrossOrigin = new URL(asset.reqUrl, self.location.origin).origin !== self.location.origin;
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

            let response: Response | undefined;
            try {
                response = await fetch(request);
            } catch (fetchErr) {
                // Browsers reject fetch() with a TypeError when SRI validation fails. The
                // browser also logs "Failed to find a valid digest in the 'integrity' attribute"
                // to the console, but the SW would otherwise silently swallow this. Surface it.
                // SRI and transient network failures both reject as TypeError; only treat as
                // integrity when the message signals a digest/SRI problem, not on TypeError alone.
                // ('integrity' covers Chrome/Firefox wording, 'digest' Safari's; a previous
                // revision also matched the string 'EPRPROTO' - a typo matching nothing any
                // browser emits - which has been dropped.)
                const isIntegrity =
                    hasIntegrity &&
                    /integrity|digest/i.test(String(fetchErr && (fetchErr as any).message || fetchErr));

                // A cross-origin host that sends no CORS headers rejects the cors-mode request
                // with the same TypeError as a genuine network failure. Before classifying,
                // retry the URL once as no-cors: if the host is actually reachable this yields
                // an opaque response, which the Cache API stores and no-cors consumers (script
                // and img tags - exactly how a page consumes cross-origin externalAssets) use
                // normally. Never attempted when integrity was requested for this asset: an
                // opaque body cannot be verified (the Request constructor rejects integrity +
                // no-cors), and silently dropping the check would defeat its purpose. Gated on
                // hasIntegrity - not isIntegrity - so a CORS-shaped TypeError can never sneak
                // an integrity-checked asset into the unverified path.
                if (!hasIntegrity && isCrossOrigin) {
                    // createNewAssetRequest can throw on a pathological composed URL (as the
                    // guarded call sites elsewhere assume). Letting it escape from inside this
                    // catch block would reject addCache without ever running sendError/doReport,
                    // so the page would see neither an error nor the progress tick. Swallow it
                    // and leave `response` undefined: the block below then classifies and
                    // reports the ORIGINAL fetch failure, which is the meaningful one.
                    try {
                        response = await tryFetch(createNewAssetRequest(asset, true));
                    } catch (noCorsErr) {
                        diag('*** addCache - no-cors fallback request build failed:', noCorsErr, asset.reqUrl);
                    }
                }

                if (!response) {
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
            }

            // An opaque response (type 'opaque', from the no-cors fallback above) reads status
            // 0 / ok false BY DESIGN - its real status and bytes are hidden - so the HTTP
            // status checks below don't apply to it; it goes straight to the cache write.
            const isOpaque = (response as any).type === 'opaque';
            // 206 Partial Content reads response.ok === true, but a partial body must never be
            // stored as the asset's full bytes (a resuming middlebox answering the versioned
            // request with a fragment would pin that fragment under the asset's key until the
            // next hash change - and applyRangeHeader refuses to slice non-200 responses, so
            // even ranged requests would serve it broken). Treat it like a failed status; the
            // Cache API itself rejects 206 puts, so this also fails cleanly instead of loudly.
            if (!isOpaque && (!response.ok || response.status === 206)) {
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
function createNewAssetRequest(asset: any, noCors = false) {
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

    if (noCors) {
        // Fallback shape for a cross-origin host that sends no CORS headers (see addCache).
        // Deliberately minimal: integrity is invalid on a no-cors request (the Request
        // constructor rejects the combination), and the no-cors header guard silently drops
        // non-safelisted headers like cache-control - only the `cache` *option* survives.
        requestInit.mode = 'no-cors';
        if (self.enableCacheControl) {
            requestInit.cache = 'no-store';
        }
        return new Request(assetUrl, requestInit);
    }

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
        // The cache-control request header is NOT a CORS-safelisted header, so attaching it to
        // a cross-origin request upgrades the fetch to a preflighted one - an OPTIONS round
        // trip most third-party asset hosts reject - which turned enableCacheControl into a
        // switch that broke every cross-origin externalAssets entry. Same-origin requests keep
        // the header (a revalidation hint for proxies on the way to the origin); cross-origin
        // ones rely on cache: 'no-store' alone, which is a fetch *option* the CORS protocol
        // never sees.
        if (url.origin === self.location.origin) {
            requestInit.headers = [['cache-control', 'no-cache']];
        }
    }

    return new Request(assetUrl, requestInit);
}

// The one gate every cache-pruning call site goes through. deleteOldCaches() spares only the
// receiver's own CACHE_NAME, so running it while ANOTHER version is mid-install would delete
// the bucket that install just created/migrated - wasting its download at best, activating it
// over an empty cache at worst. That hazard is not unique to CLEAN_UP: the SKIP_WAITING /
// CLAIM_CLIENTS chains and the activate-time prune can all race a concurrent update check
// that started staging a newer version. Skipping is always the safe direction - a deferred
// prune just leaves stale buckets until the next safe point (activation with no clients,
// CLEAN_UP, or the next update's migration reclaiming them), whereas a wrong delete destroys
// a live cache. Failures are logged and swallowed for the same reason as before: pruning is
// best-effort tidiness and must never break the signal that follows it in a command chain.
// Whether another (newer) version of this app's worker is currently staged or staging.
function updateStagedOrStaging() {
    try {
        const reg = self.registration;
        // (self as any).serviceWorker is this worker's own ServiceWorker object (newer
        // engines). It excludes the receiver itself when an engine briefly leaves it in the
        // waiting slot mid skip-waiting transition; where unsupported, a lingering self in
        // `waiting` merely defers the prune - deferring is safe, deleting wrongly is not.
        const selfWorker = (self as any).serviceWorker;
        return !!(reg && (reg.installing || (reg.waiting && reg.waiting !== selfWorker)));
    } catch {
        // Reading registration state must never break the caller's chain.
        return false;
    }
}

function safeDeleteOldCaches(label: string) {
    if (updateStagedOrStaging()) {
        diag(`${label} - a newer install is staged or staging; skipping old-cache pruning (it runs at the next safe point).`);
        return Promise.resolve();
    }
    // Removes every Bswup cache THIS APP owns except the current CACHE_NAME, so stale
    // version-suffixed caches from previous installs are reclaimed once they're no longer
    // needed. "Owns" is defined by isOwnStaleCacheKey: this scope's previous versions plus
    // legacy scope-less buckets; another scope's buckets are never touched.
    return (async () => {
        const cacheKeys = await caches.keys();
        // Re-checked after the async enumeration: an update poll can start staging a newer
        // version in that window, and its freshly created bucket would look "stale" to
        // isOwnStaleCacheKey. The remaining race (staging begins mid-delete) cannot be closed
        // from here, but this narrows it from "any time during enumeration" to microseconds -
        // and a lost bucket only costs a re-download, never correctness (the install's own
        // puts land in a resurrected empty bucket and the diff re-fetches what is missing).
        if (updateStagedOrStaging()) {
            diag(`${label} - a newer install started staging mid-prune; skipping.`);
            return;
        }
        await Promise.all(cacheKeys.filter(isOwnStaleCacheKey).map(key => caches.delete(key)));
    })().catch((err: any) => diag(`*** ${label} - old-cache pruning failed (continuing):`, err));
}

// Whether a cache bucket is a stale one this registration owns - the shared definition for
// both cleanup (deleteOldCaches) and update migration (createAssetsCache warm-start):
//   - buckets under SCOPED_CACHE_PREFIX are this scope's previous versions;
//   - legacy scope-less buckets (`bit-bswup - <version>`, pre-scoping releases) cannot be
//     attributed to a scope, so they are claimed for migration and then reclaimed by whichever
//     upgraded app touches them first. On a multi-app origin an un-upgraded sibling can lose
//     its legacy bucket once during the transition - the pre-scoping behavior did exactly that
//     on EVERY update, and the sibling self-heals by refilling; keeping legacy buckets forever
//     would instead leak them for every single-app origin, the overwhelmingly common case.
// A sibling scope's `bit-bswup:<other>/ - <version>` buckets match neither prefix - which is
// the point of scope-qualified names.
function isOwnStaleCacheKey(key: string) {
    if (key === CACHE_NAME) return false;
    return key.startsWith(SCOPED_CACHE_PREFIX) || key.startsWith(LEGACY_CACHE_PREFIX);
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

        // new Request() rejects URLs it cannot represent ('https://', credentials in the URL,
        // bad percent-encoding, ...). uniqueAssets runs at module-evaluation time, so an
        // unguarded throw here killed the whole worker before any install/error handling could
        // run - no structured error, nothing controlling the page, only the page's stall
        // watchdog to rescue a first install a minute later. One bad entry (a typo in
        // externalAssets, a corrupt manifest line) must cost that one asset, not the app.
        let reqUrl: string | undefined;
        if (!isPattern) {
            try {
                reqUrl = new Request(a.url).url;
            } catch (err) {
                // Deferred to handleInstall via INVALID_ASSET_ERRORS (see its declaration):
                // reported once per install instead of on every worker cold start.
                diag('*** uniqueAssets - skipping asset with an invalid url:', a.url, err);
                INVALID_ASSET_ERRORS.push({
                    reason: 'request',
                    message: 'Skipping asset with an invalid url: ' + String(a.url) + ' (' + (err && (err as any).message || String(err)) + ')',
                    url: String(a.url),
                    fatal: false, // the asset is dropped; the install itself continues
                });
                continue;
            }
        }

        // Dedupe on the *resolved* URL rather than the declared string, so 'index.html',
        // '/index.html' and 'https://host/index.html' - three spellings of one resource -
        // collapse into a single entry instead of producing duplicate downloads and
        // ambiguous first-wins matching. The first occurrence wins, and manifest entries
        // precede externalAssets in ALL_ASSETS, so a hash-carrying manifest entry always
        // beats a hashless external duplicate. Pattern entries dedupe on their source text.
        const dedupeKey = isPattern ? a.url.toString() : (reqUrl as string);
        if (unique[dedupeKey]) continue;
        unique[dedupeKey] = 1;

        distinct.push({
            ...a,
            url: isPattern ? applyUrlCaseSensitivity(a.url) : urlToRegExp(reqUrl as string),
            srcUrl: isPattern ? String(a.url) : a.url,
            reqUrl,
            // Paired by resolved URL too (see DEFAULT_REQ_URL) so an equivalent spelling of
            // defaultUrl still finds its asset - and folded per caseInsensitiveUrl, so
            // defaultUrl = '/Index.html' still pairs with a manifest 'index.html' when the
            // app opted into case-insensitive matching (asset routing already folds; the
            // default-document pairing used to be the one comparison that didn't).
            isDefault: !isPattern && foldUrlKey(reqUrl as string) === foldUrlKey(DEFAULT_REQ_URL),
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

// Window clients that belong to this registration's scope, controlled or not. Uncontrolled
// clients matter: on a first install nothing is controlled yet, and the page still needs the
// progress stream. The scope filter matters just as much: matchAll({ includeUncontrolled:
// true }) returns every same-origin window client, including pages of unrelated apps mounted
// under other scopes on the same host - and only clients under this scope can be controlled
// by this worker or depend on its caches. When the scope is unavailable (self.registration
// missing in an exotic runtime) fall back to no filtering, which is the pre v-10-6-0 behavior.
// The cleanup worker (bit-bswup.sw-cleanup.ts) applies the same filter for the same reason.
async function matchScopeClients() {
    const scope = self.registration && self.registration.scope;
    const clients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    // Raw string prefix matching is deliberate: it is exactly the Service Worker spec's scope
    // match algorithm (longest prefix over the serialized URLs, with no path-segment
    // awareness), so this filter selects precisely the clients this registration can control.
    // "Fixing" it to require a path-boundary would make the filter NARROWER than the
    // worker's real authority - e.g. a scope registered as '/app' (no trailing slash) does
    // control '/application' in every browser, so those clients must receive our messages.
    return (clients || []).filter((client: any) => !scope || (typeof client.url === 'string' && client.url.indexOf(scope) === 0));
}

// Broadcasts a message to every client under this registration's scope (controlled or not),
// so all open tabs of THIS app - not just the one that triggered the work - receive
// install/progress/activate/error updates. Broadcasting beyond the scope would make a sibling
// Bswup app on the same origin react to this app's lifecycle: its handler would run
// downloadFinished/error logic (splash, reload prompts) against a registration that never
// staged anything for it. Objects are JSON-stringified; plain string commands (e.g.
// 'WAITING_SKIPPED') are sent as-is.
function sendMessage(message: any) {
    matchScopeClients()
        .then((clients: any) => (clients || []).forEach((client: any) => client.postMessage(typeof message === 'string' ? message : JSON.stringify(message))))
        // clients.matchAll can reject in a dying worker / broken runtime; messaging is
        // best-effort by design, so log instead of surfacing an unhandled rejection.
        .catch((err: any) => diag('*** sendMessage - client enumeration failed:', err));
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
// Direct console output of non-fatal (lax) failures is capped: a mass outage - the network
// dropping while 200 assets are still downloading - otherwise floods the console with hundreds
// of identical errors that bury the one line explaining what happened. Every failure is still
// reported to the page (handlers may count or display them) and to diag; fatal errors are
// always logged. (MAX_NONFATAL_CONSOLE_ERRORS / nonFatalConsoleErrors are declared near the
// top of this file, not here - see the note beside them.)

function sendError(data: { reason: string; message: string; fatal?: boolean;[key: string]: any }) {
    diag('*** error:', data);
    try {
        // Best-effort console output so the failure is visible even before any client connects.
        if (data.fatal !== false) {
            console.error('BitBswup SW:', data.message, data);
        } else if (nonFatalConsoleErrors < MAX_NONFATAL_CONSOLE_ERRORS) {
            nonFatalConsoleErrors++;
            console.error('BitBswup SW:', data.message, data);
            if (nonFatalConsoleErrors === MAX_NONFATAL_CONSOLE_ERRORS) {
                console.error('BitBswup SW: further non-fatal asset failures will not be logged to the console; they are still reported to the page handler (enable enableDiagnostics for the full stream).');
            }
        }
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
