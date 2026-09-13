// Guards the fetch path against the "frozen progress bar" failure.
//
// respondWith() is a commitment: once called, the worker owns the response and the browser will
// not fall back to its own network stack, so a rejected promise becomes a hard network error for
// the page. Previous versions called respondWith unconditionally and only then decided what to
// do, routing every request on the origin through a fetch() inside the worker. A single blip on
// a proxied request that Bswup did not even care about could fail a Blazor boot asset, leaving
// Blazor half-started and the splash stuck at whatever percent it had reached until a refresh.
//
// Two invariants keep that from happening, and both are tested here:
//   1. Requests Bswup does not manage never reach respondWith at all.
//   2. The managed path never rejects - it degrades cache -> network -> stale cache -> error.

import { describe, it, expect } from 'vitest';
import { createServiceWorkerContext, ORIGIN, FakeBlob, FakeResponse, decodeBody } from './harness.js';

function boot({ config = {}, assets = [{ url: 'app.js', hash: 'h1' }], fetchHandler, cacheStorageError, configure } = {}) {
    const sw = createServiceWorkerContext({ fetchHandler, cacheStorageError });
    sw.addClient();
    sw.configure(config);
    sw.self.assetsManifest = { version: 'v1', assets: sw.array(assets) };
    if (configure) configure(sw);
    return sw.load();
}

const NETWORK_DOWN = async () => { throw new TypeError('Failed to fetch'); };

// The asset cache bucket every test here boots into: `bit-bswup:<scope-path> - <version>` with
// the harness's root scope and the 'v1' manifest version set in boot(). Named once so a change
// to the cache-key format is a one-line update rather than a sweep through the file.
const ASSET_CACHE = 'bit-bswup:/ - v1';
const openAssetCache = sw => sw.caches.open(ASSET_CACHE);

describe('requests Bswup does not manage', () => {
    it('ignores non-GET requests instead of proxying them', async () => {
        const sw = boot();
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/api/orders`, method: 'POST' });
        expect(handled).toBe(false);
    });

    it('ignores server-handled URLs', async () => {
        const sw = boot({ configure: c => { c.self.serverHandledUrls = c.array([c.regex('/api/')]); } });
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/api/orders` });
        expect(handled).toBe(false);
    });

    it('ignores URLs that match no asset', async () => {
        const sw = boot();
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/something/unknown.txt` });
        expect(handled).toBe(false);
    });

    it('ignores the default document under forcePrerender', async () => {
        const sw = boot({ config: { forcePrerender: true, defaultUrl: 'index.html' }, assets: [{ url: 'index.html' }] });
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/index.html`, mode: 'navigate' });
        expect(handled).toBe(false);
    });

    // The whole point: if the worker never responds, its own network stack cannot be the thing
    // that breaks the request - the browser just does what it always would.
    it('stays out of the way even when every fetch in the worker would fail', async () => {
        const sw = boot({ fetchHandler: NETWORK_DOWN });
        for (const url of [`${ORIGIN}/unknown.txt`, `${ORIGIN}/api/x`]) {
            const { handled } = await sw.fetchEvent({ url });
            expect(handled).toBe(false);
        }
    });

    it('still blocks prohibited URLs (those it must answer)', async () => {
        const sw = boot({ configure: c => { c.self.prohibitedUrls = c.array([c.regex('/admin/')]); } });
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/admin/x` });
        expect(handled).toBe(true);
        expect(response.status).toBe(403);
    });
});

describe('the managed path never rejects', () => {
    const managed = { url: 'app.js', hash: 'h1' };

    it('serves a stale cached copy when the network is down', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js`, { ok: true, status: 200, body: 'stale', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('stale');
    });

    // Hashed assets are keyed `reqUrl.<hash>`, so a raw-URL lookup can never hit them: when
    // the current-hash entry is missing and the network is down, a previous version's copy of
    // the same URL is the only thing left that can keep the app alive.
    it('serves a previous-hash copy when the network is down', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [{ url: 'app.js', hash: 'sha256-new' }], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js.sha256-old`, { ok: true, status: 200, body: 'previous-version', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('previous-version');
    });

    it('never serves a sibling file (app.js.map / app.js.br) as the stale fallback', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [{ url: 'app.js', hash: 'sha256-new' }], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js.map`, { ok: true, status: 200, body: 'a source map', clone: () => ({}) });
        await cache.put(`${ORIGIN}/app.js.br`, { ok: true, status: 200, body: 'brotli bytes', clone: () => ({}) });
        await cache.put(`${ORIGIN}/app.js.map.sha256-x`, { ok: true, status: 200, body: 'hashed map', clone: () => ({}) });

        // A hard network error beats silently serving the wrong file's bytes.
        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(response.type).toBe('error');
    });

    it('returns a network-error response rather than rejecting when nothing is cached', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN });

        // The assertion that matters: this resolves. A rejection here is what hard-fails the
        // request in a real browser.
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.type).toBe('error');
    });

    it('survives CacheStorage being unavailable', async () => {
        const sw = boot({
            config: { isPassive: true },
            assets: [managed],
            cacheStorageError: new Error('QuotaExceededError'),
        });
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.status).toBe(200); // fell through to the network, no rejection
    });

    it('falls back to the plain request when the versioned one fails', async () => {
        // The `?v=` buster (and no-store headers under enableCacheControl) can be rejected by a
        // fussy proxy while the page's own plain request still succeeds.
        const seen = [];
        const sw = boot({
            config: { isPassive: true },
            assets: [managed],
            fetchHandler: async url => {
                seen.push(url);
                if (url.includes('?v=')) throw new TypeError('proxy rejected the query');
                return { ok: true, status: 200, body: 'plain', clone: () => ({ body: 'plain' }) };
            },
        });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(seen.some(u => u.includes('?v='))).toBe(true);
        expect(response.body).toBe('plain');
    });

    // Retrying without integrity would serve exactly the unverified bytes SRI exists to reject.
    it('does NOT retry without integrity when the integrity-checked request fails', async () => {
        const attempts = [];
        const sw = boot({
            config: { isPassive: true, enableIntegrityCheck: true },
            assets: [{ url: 'app.js', hash: 'sha256-abc' }],
            fetchHandler: async url => { attempts.push(url); throw new TypeError('integrity mismatch'); },
        });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.type).toBe('error');   // failed, as it must
        expect(attempts.length).toBe(1);       // and did not silently downgrade
    });

    it('serves from cache without touching the network', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'cached', clone: () => ({}) });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(response.body).toBe('cached');
        expect(sw.fetchLog.length).toBe(0);
    });

    it('falls back to cache in active mode when the network is down', async () => {
        const sw = boot({ config: { isPassive: false }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js`, { ok: true, status: 200, body: 'stale', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('stale');
    });

    // Routing switched from an O(assets) regex scan to a Map keyed by origin+pathname; the
    // regex semantics that matter (any query tolerated, caseInsensitiveUrl folding) must hold.
    it('matches an asset URL regardless of its query string (Map lookup is query-tolerant)', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'cached', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js?asp-append-version=abc123` });
        expect(handled).toBe(true);
        expect(response.body).toBe('cached');
    });

    it('matches case-insensitively when caseInsensitiveUrl is set', async () => {
        const sw = boot({ config: { isPassive: true, caseInsensitiveUrl: true }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'cached', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/APP.JS` });
        expect(handled).toBe(true);
        expect(response.body).toBe('cached');
    });
});

// Media elements fetch audio/video with Range headers. Answering a ranged request with a
// cached full 200 breaks playback (Safari refuses it outright), and caching a server's 206
// under the asset's key would pin a fragment into the cache as if it were the whole file.
describe('range requests', () => {
    const managed = { url: 'clip.mp4', hash: 'h1' };
    const includeMp4 = c => { c.self.assetsInclude = c.array([c.regex('\\.mp4$')]); };
    const TEXT = '0123456789';
    // A REAL FakeResponse, not a hand-rolled object: its one-shot body and clone-after-use
    // throw are exactly what protects the clone-before-read discipline in applyRangeHeader -
    // a fixture with a free clone() would keep these tests green with the clone deleted.
    const fullBody = () => new FakeResponse(TEXT, { status: 200, headers: { 'content-type': 'video/mp4' } });
    const decode = decodeBody;

    async function bootCached() {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN, configure: includeMp4 });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/clip.mp4.h1`, fullBody());
        return sw;
    }

    it('serves a real 206 slice from the cached full body', async () => {
        const sw = await bootCached();
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=2-5' } });
        expect(handled).toBe(true);
        expect(response.status).toBe(206);
        expect(decode(response)).toBe('2345');
        expect(response.headers.get('content-range')).toBe('bytes 2-5/10');
        expect(response.headers.get('content-length')).toBe('4');
    });

    it('supports open-ended and suffix ranges', async () => {
        const sw = await bootCached();

        const openEnded = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=4-' } });
        expect(openEnded.response.status).toBe(206);
        expect(decode(openEnded.response)).toBe('456789');

        const suffix = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=-3' } });
        expect(suffix.response.status).toBe(206);
        expect(decode(suffix.response)).toBe('789');
        expect(suffix.response.headers.get('content-range')).toBe('bytes 7-9/10');
    });

    it('falls back to the full response for multi-range, foreign-unit or unsatisfiable specs', async () => {
        const sw = await bootCached();
        for (const range of ['bytes=0-2,5-7', 'bytes=99-', 'lines=1-2', 'bytes=-0']) {
            const { response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range } });
            expect(response.status).toBe(200); // unchanged full body - the pre-slicing behavior
            expect(response.body).toBe(TEXT);
        }
    });

    it('never caches a 206 partial response under the asset key', async () => {
        // Active-mode miss with a ranged request: the page's own request goes out and the
        // server honors the Range with a 206. response.ok is TRUE for 206, so without an
        // explicit guard the fragment would be cached and served later as the whole file.
        const sw = boot({
            config: { isPassive: false },
            assets: [managed],
            fetchHandler: async () => ({ ok: true, status: 206, body: '23', clone: () => ({ status: 206, body: '23' }) }),
            configure: includeMp4,
        });

        // The Cache API itself also rejects 206 puts, so an empty cache alone cannot prove
        // the worker's own guard exists - assert the put was never even attempted.
        const cache = await openAssetCache(sw);
        let putAttempts = 0;
        const originalPut = cache.put.bind(cache);
        cache.put = async (...args) => { putAttempts++; return originalPut(...args); };

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=2-3' } });
        expect(response.status).toBe(206); // the server's partial passes through untouched
        expect(putAttempts).toBe(0);
        expect(sw.caches.snapshot()[ASSET_CACHE] || []).toHaveLength(0);
    });

    it('sends a ranged passive-mode fetch with the page\'s own request so the server can answer 206', async () => {
        const seen = [];
        const sw = boot({
            config: { isPassive: true },
            assets: [managed],
            fetchHandler: async url => { seen.push(url); return { ok: true, status: 206, body: '23', clone: () => ({}) }; },
            configure: includeMp4,
        });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=2-3' } });
        expect(response.status).toBe(206);
        // The versioned request would have dropped the Range header (full download per range
        // request) - the plain request keeps it, so no `?v=` may appear.
        expect(seen).toHaveLength(1);
        expect(seen[0].includes('?v=')).toBe(false);
    });

    // A cached 200 that still declares Content-Encoding cannot be byte-sliced: blob() yields the
    // DECODED body, so a 206 carrying the encoded header would make the client inflate raw slice
    // bytes (corrupt). Fall back to the full response rather than emit a mislabeled 206.
    it('falls back to the full response when the cached body declares a content encoding', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN, configure: includeMp4 });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/clip.mp4.h1`, new FakeResponse(TEXT, { status: 200, headers: new Headers({ 'content-encoding': 'br', 'content-type': 'video/mp4' }) }));

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=2-5' } });
        expect(response.status).toBe(200); // not sliced
        expect(decode(response)).toBe(TEXT);
    });

    // 'identity' is the no-op encoding and must still slice normally.
    it('still slices when the content encoding is identity', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [managed], fetchHandler: NETWORK_DOWN, configure: includeMp4 });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/clip.mp4.h1`, new FakeResponse(TEXT, { status: 200, headers: new Headers({ 'content-encoding': 'identity' }) }));

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/clip.mp4`, headers: { range: 'bytes=2-5' } });
        expect(response.status).toBe(206);
        expect(decode(response)).toBe('2345');
    });
});

// The active-mode heal path caches whatever it fetches under the asset key. With SRI enabled it
// must fetch the VERIFIED versioned request (integrity attached), not the page's plain request -
// otherwise a lazily-healed asset (missed a lax precache, or evicted) could cache tampered bytes
// and serve them offline as trusted. Every other download path already attaches integrity.
describe('active-mode lazy heal respects integrity', () => {
    const asset = { url: 'app.js', hash: 'sha256-abc' };

    it('fetches the verified versioned request, not the plain page request', async () => {
        const seen = [];
        const sw = boot({
            config: { isPassive: false, enableIntegrityCheck: true },
            assets: [asset],
            fetchHandler: async (url, req) => { seen.push({ url, integrity: req && req.integrity }); return new FakeResponse('verified', { status: 200 }); },
        });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(decodeBody(response)).toBe('verified');
        // The versioned request carries the ?v= buster and the integrity attribute; the plain
        // page request carries neither. ?v= present proves the heal did not downgrade SRI.
        expect(seen).toHaveLength(1);
        expect(seen[0].url.includes('?v=')).toBe(true);
        expect(seen[0].integrity).toBe('sha256-abc');
    });

    it('does not cache tampered bytes when the verified fetch fails SRI', async () => {
        const sw = boot({
            config: { isPassive: false, enableIntegrityCheck: true },
            assets: [asset],
            fetchHandler: async () => { throw new TypeError('integrity mismatch'); },
        });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(response.type).toBe('error'); // failed, not served
        expect(sw.caches.snapshot()[ASSET_CACHE] || []).toHaveLength(0);
    });

    // A ranged heal keeps the page's own request: a 206 can't be SRI-verified and Safari needs
    // the Range to reach the server, so the versioned/integrity request must NOT be substituted.
    it('keeps the plain request for a ranged heal even with integrity enabled', async () => {
        const seen = [];
        const sw = boot({
            config: { isPassive: false, enableIntegrityCheck: true },
            assets: [asset],
            fetchHandler: async url => { seen.push(url); return new FakeResponse('part', { status: 206 }); },
        });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js`, headers: { range: 'bytes=0-1' } });
        expect(response.status).toBe(206);
        expect(seen).toHaveLength(1);
        expect(seen[0].includes('?v=')).toBe(false); // plain request, Range preserved
    });
});

// Offline navigation is the core feature: every route the user deep-links to must come back
// as the cached app shell, while URLs the server owns (and URLs that ARE assets) must not.
describe('offline navigation (SPA default document)', () => {
    const assets = [
        { url: 'index.html', hash: 'idx' },
        { url: 'manifest.json', hash: 'mf' },
    ];

    async function bootOffline(configure) {
        const sw = boot({ config: { isPassive: true }, assets, fetchHandler: NETWORK_DOWN, configure });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/index.html.idx`, { ok: true, status: 200, body: 'the app shell', clone: () => ({}) });
        await cache.put(`${ORIGIN}/manifest.json.mf`, { ok: true, status: 200, body: '{"name":"app"}', clone: () => ({}) });
        return sw;
    }

    it('serves the cached app shell for a deep-link navigation while offline', async () => {
        const sw = await bootOffline();
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/counter/42`, mode: 'navigate' });
        expect(handled).toBe(true);
        expect(response.body).toBe('the app shell');
    });

    // Opening /manifest.json (or an image) directly in a tab must show that file - serving
    // the SPA shell under an asset's URL renders markup where the browser expected data.
    it('serves the asset itself when the navigation targets a managed asset URL', async () => {
        const sw = await bootOffline();
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/manifest.json`, mode: 'navigate' });
        expect(handled).toBe(true);
        expect(response.body).toBe('{"name":"app"}');
    });

    it('leaves serverRenderedUrls navigations to the network', async () => {
        const sw = await bootOffline(c => { c.self.serverRenderedUrls = c.array([c.regex('/account/')]); });
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/account/login`, mode: 'navigate' });
        expect(handled).toBe(false);
    });

    it('leaves navigations alone when no asset matches the default document', async () => {
        const sw = boot({ config: { isPassive: true, defaultUrl: 'missing.html' }, assets, fetchHandler: NETWORK_DOWN });
        const { handled } = await sw.fetchEvent({ url: `${ORIGIN}/counter`, mode: 'navigate' });
        expect(handled).toBe(false);
    });

    // A RegExp externalAssets entry that happens to match a route URL must not hijack the
    // navigation away from the app shell: pattern assets are keyed by the live request URL,
    // so the route would be cached as a bogus "pattern generation" and answered with a
    // network error offline instead of the shell.
    it('never lets a pattern asset hijack a navigation', async () => {
        const sw = await bootOffline(c => { c.self.externalAssets = c.array([c.regex('/counter')]); });
        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/counter/42`, mode: 'navigate' });
        expect(handled).toBe(true);
        expect(response.body).toBe('the app shell');
    });

    // Cache-poisoning regression: an active-mode navigation whose shell entry is missing must
    // fetch the SHELL's own URL - never the route URL - because whatever comes back is written
    // under the shell's cache key. A server answering /counter with route-specific HTML would
    // otherwise become the app shell for every future navigation.
    it('a shell cache miss on a navigation fetches the shell URL, not the route URL', async () => {
        const seen = [];
        const sw = boot({
            config: { isPassive: false },
            assets,
            fetchHandler: async url => {
                seen.push(url);
                if (url.includes('index.html')) return { ok: true, status: 200, body: 'the real shell', clone: () => ({ ok: true, status: 200, body: 'the real shell' }) };
                return { ok: true, status: 200, body: 'route-specific html', clone: () => ({ ok: true, status: 200, body: 'route-specific html' }) };
            },
        });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/counter/42`, mode: 'navigate' });
        expect(handled).toBe(true);
        expect(response.body).toBe('the real shell');
        expect(seen.some(u => u.includes('/counter'))).toBe(false); // the route URL is never fetched

        // And what landed under the shell's key is the shell, not the route's HTML.
        const cache = await openAssetCache(sw);
        const cached = await cache.match(`${ORIGIN}/index.html.idx`);
        expect(cached && cached.body).toBe('the real shell');
    });
});

// A navigation request's redirect mode is 'manual', so a response with response.redirected ===
// true cannot be replayed to it - the browser throws "a redirected response was used for a
// request whose redirect mode is not 'follow'" and the navigation hard-fails. Hosts that
// 30x-redirect the shell URL ('/' -> '/index.html' on Cloudflare Pages / Netlify / many reverse
// proxies) make the followed shell response carry that flag, so offline deep-link navigations
// break. serveAsset must strip the flag by rebuilding the response before it reaches a navigation.
describe('redirected shell responses are cleaned for navigations', () => {
    const assets = [{ url: 'index.html', hash: 'idx' }];

    it('rebuilds a cached redirected shell served to a deep-link navigation', async () => {
        const sw = boot({ config: { isPassive: true }, assets, fetchHandler: NETWORK_DOWN });
        const cache = await openAssetCache(sw);
        await cache.put(`${ORIGIN}/index.html.idx`, new FakeResponse('the app shell', { status: 200, redirected: true }));

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/counter/42`, mode: 'navigate' });
        expect(handled).toBe(true);
        // Body is preserved, and the redirected flag the browser rejects is gone.
        expect(decodeBody(response)).toBe('the app shell');
        expect(response.redirected).toBeFalsy();
    });

    it('rebuilds a network-fetched redirected shell on a cache miss', async () => {
        const sw = boot({
            config: { isPassive: false },
            assets,
            fetchHandler: async url =>
                url.includes('index.html')
                    ? new FakeResponse('the real shell', { status: 200, redirected: true })
                    : new FakeResponse('route html', { status: 200 }),
        });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/counter/42`, mode: 'navigate' });
        expect(handled).toBe(true);
        expect(decodeBody(response)).toBe('the real shell');
        expect(response.redirected).toBeFalsy();
    });

    // A non-navigation subresource request (script/img) has redirect mode 'follow', so a
    // redirected response is legitimate there - the worker must not needlessly rebuild it.
    it('leaves a redirected non-navigation response untouched', async () => {
        const sw = boot({
            config: { isPassive: false },
            assets: [{ url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => new FakeResponse('from-network', { status: 200, redirected: true }),
        });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js`, mode: 'cors' });
        expect(handled).toBe(true);
        expect(decodeBody(response)).toBe('from-network');
        expect(response.redirected).toBe(true); // not rebuilt
    });
});

// A miss in active mode means the asset failed to precache during a lax install or its cache
// entry was evicted. Install does not re-run until the next update, so the fetch path is the
// only chance to get the asset back into the cache - the documented lax semantics ("missing
// assets are filled in lazily on the first fetch" in both modes) depend on this write-back.
describe('active mode lazily heals the cache', () => {
    const managed = { url: 'app.js', hash: 'h1' };
    const networkResponse = () =>
        ({ ok: true, status: 200, body: 'from-network', clone: () => ({ ok: true, status: 200, body: 'from-network' }) });

    it('writes a cache-miss response back under the hash-suffixed key', async () => {
        const sw = boot({ config: { isPassive: false }, assets: [managed], fetchHandler: async () => networkResponse() });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('from-network');
        expect(sw.caches.snapshot()[ASSET_CACHE]).toContain(`${ORIGIN}/app.js.h1`);

        // The healed entry now serves offline: the second request never touches the network.
        const again = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(again.response.body).toBe('from-network');
        expect(sw.fetchLog.length).toBe(1);
    });

    it('does not cache non-ok responses', async () => {
        const sw = boot({
            config: { isPassive: false },
            assets: [managed],
            fetchHandler: async () => ({ ok: false, status: 404, body: 'nope', clone: () => ({}) }),
        });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(response.status).toBe(404); // passed through to the page untouched
        expect(sw.caches.snapshot()[ASSET_CACHE] || []).toHaveLength(0);
    });

    // A cross-origin externalAssets host without CORS headers rejects the worker's cors-mode
    // request; the page's own no-cors request (a script/img tag) succeeds with an opaque
    // response. That opaque response must be cached - it is the asset's only offline story.
    it('lazily caches the opaque response of a cross-origin asset', async () => {
        const CDN = 'https://cdn.example.com/lib.js';
        const sw = boot({
            config: { isPassive: true },
            assets: [],
            fetchHandler: (url, req) => {
                if (req && req.mode === 'no-cors') return { ok: false, status: 0, type: 'opaque', body: 'opaque-bytes', clone: () => ({ type: 'opaque', body: 'opaque-bytes' }) };
                throw new TypeError('Failed to fetch');
            },
            configure: c => { c.self.externalAssets = c.array([{ url: CDN }]); },
        });

        const { handled, response } = await sw.fetchEvent({ url: CDN, mode: 'no-cors' });
        expect(handled).toBe(true);
        expect(response.body).toBe('opaque-bytes');
        expect(sw.caches.snapshot()[ASSET_CACHE]).toContain(CDN);
    });
});
