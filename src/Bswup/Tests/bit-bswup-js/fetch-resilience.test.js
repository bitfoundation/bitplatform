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
