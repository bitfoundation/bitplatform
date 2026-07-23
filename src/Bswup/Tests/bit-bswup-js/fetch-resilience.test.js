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
import { createServiceWorkerContext, ORIGIN } from './harness.js';

function boot({ config = {}, assets = [{ url: 'app.js', hash: 'h1' }], fetchHandler, cacheStorageError, configure } = {}) {
    const sw = createServiceWorkerContext({ fetchHandler, cacheStorageError });
    sw.addClient();
    sw.configure(config);
    sw.self.assetsManifest = { version: 'v1', assets: sw.array(assets) };
    if (configure) configure(sw);
    return sw.load();
}

const NETWORK_DOWN = async () => { throw new TypeError('Failed to fetch'); };

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
        const cache = await sw.caches.open('bit-bswup - v1');
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
        const cache = await sw.caches.open('bit-bswup - v1');
        await cache.put(`${ORIGIN}/app.js.sha256-old`, { ok: true, status: 200, body: 'previous-version', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('previous-version');
    });

    it('never serves a sibling file (app.js.map / app.js.br) as the stale fallback', async () => {
        const sw = boot({ config: { isPassive: true }, assets: [{ url: 'app.js', hash: 'sha256-new' }], fetchHandler: NETWORK_DOWN });
        const cache = await sw.caches.open('bit-bswup - v1');
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
        const cache = await sw.caches.open('bit-bswup - v1');
        await cache.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'cached', clone: () => ({}) });

        const { response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(response.body).toBe('cached');
        expect(sw.fetchLog.length).toBe(0);
    });

    it('falls back to cache in active mode when the network is down', async () => {
        const sw = boot({ config: { isPassive: false }, assets: [managed], fetchHandler: NETWORK_DOWN });
        const cache = await sw.caches.open('bit-bswup - v1');
        await cache.put(`${ORIGIN}/app.js`, { ok: true, status: 200, body: 'stale', clone: () => ({}) });

        const { handled, response } = await sw.fetchEvent({ url: `${ORIGIN}/app.js` });
        expect(handled).toBe(true);
        expect(response.body).toBe('stale');
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
        expect(sw.caches.snapshot()['bit-bswup - v1']).toContain(`${ORIGIN}/app.js.h1`);

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
        expect(sw.caches.snapshot()['bit-bswup - v1'] || []).toHaveLength(0);
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
        expect(sw.caches.snapshot()['bit-bswup - v1']).toContain(CDN);
    });
});
