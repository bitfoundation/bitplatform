import { describe, it, expect } from 'vitest';
import { createServiceWorkerContext, ORIGIN } from './harness.js';

const manifest = (assets, version = 'v1') => ({ version, assets });

/** A worker configured and loaded in one step, with a sane default manifest. */
function boot({ config = {}, assets = [{ url: 'index.html', hash: 'sha256-idx' }], version = 'v1', fetchHandler, configure } = {}) {
    const sw = createServiceWorkerContext({ fetchHandler });
    sw.addClient();
    sw.configure(config);
    sw.self.assetsManifest = manifest(sw.array(assets), version);
    if (configure) configure(sw);
    return sw.load();
}

/** Fire install and wait for the (possibly backgrounded) cache build to finish. */
async function install(sw) {
    const result = sw.fire('install');
    await result;
    await sw.settle();
}

describe('asset payload sent to the page', () => {
    // Internally asset.url is a RegExp matcher; a RegExp JSON-stringifies to {}, which would
    // hand every bitBswupHandler an empty object where the documented API promises a string.
    it('reports asset.url as the declared string, not a serialized RegExp', async () => {
        const sw = boot({ assets: [{ url: '_framework/blazor.boot.json', hash: 'sha256-abc' }] });
        await install(sw);

        const [progress] = sw.messagesOfType('progress');
        expect(progress.data.asset.url).toBe('_framework/blazor.boot.json');
        expect(progress.data.asset.hash).toBe('sha256-abc');
        expect(progress.data.asset.reqUrl).toBe(`${ORIGIN}/_framework/blazor.boot.json`);
    });

    it('survives a JSON round-trip with a usable url', async () => {
        const sw = boot({ assets: [{ url: 'app.css', hash: 'sha256-css' }] });
        await install(sw);

        // messagesOfType already parses what postMessage received, i.e. post-serialization.
        const [progress] = sw.messagesOfType('progress');
        expect(typeof progress.data.asset.url).toBe('string');
        expect(progress.data.asset.url).not.toEqual({});
    });

    it('never sends a null asset on the "nothing to cache" path', async () => {
        // Everything already cached => no downloads => completion must still be signalled,
        // but not as a progress tick with asset:null (handlers dereference data.asset.url).
        const sw = boot({ assets: [] });
        await install(sw);

        for (const progress of sw.messagesOfType('progress')) {
            expect(progress.data.asset).not.toBeNull();
        }
        expect(sw.messagesOfType('bypass').length).toBeGreaterThan(0);
    });
});

describe('errorTolerance', () => {
    const failing = url => (url.includes('missing') ? new Error('boom') : undefined);

    it('defaults to lax so one bad asset cannot abort the install', async () => {
        const sw = boot({
            config: { maxRetries: 0 },
            assets: [{ url: 'index.html', hash: 'h1' }, { url: 'missing.js', hash: 'h2' }],
            fetchHandler: async url => {
                if (url.includes('missing')) throw new Error('network');
                return { ok: true, status: 200, clone: () => ({}) };
            },
        });
        expect(sw.self.errorTolerance).toBe('lax');
        await expect(sw.fire('install')).resolves.not.toThrow();
    });

    it('reports a non-fatal error under lax so the UI does not show install-failed', async () => {
        const sw = boot({
            config: { maxRetries: 0 },
            assets: [{ url: 'missing.js', hash: 'h2' }],
            fetchHandler: async () => { throw new Error('network'); },
        });
        await install(sw);

        const errors = sw.messagesOfType('error');
        expect(errors.length).toBeGreaterThan(0);
        expect(errors.every(e => e.data.fatal === false)).toBe(true);
    });

    it('aborts under strict and reports a fatal install-aborted error', async () => {
        const sw = boot({
            config: { errorTolerance: 'strict', maxRetries: 0 },
            assets: [{ url: 'missing.js', hash: 'h2' }],
            fetchHandler: async () => { throw new Error('network'); },
        });
        await expect(sw.fire('install')).rejects.toThrow(/Install aborted/);

        const fatal = sw.messagesOfType('error').filter(e => e.data.fatal === true);
        expect(fatal.some(e => e.data.reason === 'install-aborted')).toBe(true);
    });

    it('falls back to lax for an unrecognized value', async () => {
        const sw = boot({ config: { errorTolerance: 'config' } }); // a value older docs mentioned
        expect(sw.self.errorTolerance).toBe('lax');
    });

    it('mode presets still select lax', async () => {
        expect(boot({ config: { mode: 'NoPrerender' } }).self.errorTolerance).toBe('lax');
        expect(boot({ config: { mode: 'FullOffline' } }).self.errorTolerance).toBe('lax');
    });
});

// A mode is a bundle of *defaults*: it must only fill settings the app left unset. The old
// implementation hard-assigned isPassive and used ||= for the rest, so explicit falsy config
// (isPassive: false, caseInsensitiveUrl: false) was silently overwritten by the preset.
describe('mode presets fill only unset settings', () => {
    it('applies the preset value when the app left it unset', async () => {
        const sw = boot({ config: { mode: 'NoPrerender' } });
        await install(sw);
        // The install message reports the worker's effective isPassive.
        expect(sw.messagesOfType('install')[0].data.isPassive).toBe(true);
    });

    it('does not clobber an explicit isPassive: false (NoPrerender presets true)', async () => {
        const sw = boot({ config: { mode: 'NoPrerender', isPassive: false } });
        await install(sw);
        expect(sw.messagesOfType('install')[0].data.isPassive).toBe(false);
    });

    it('does not clobber an explicit isPassive: true (FullOffline presets false)', async () => {
        const sw = boot({ config: { mode: 'FullOffline', isPassive: true } });
        await install(sw);
        expect(sw.messagesOfType('install')[0].data.isPassive).toBe(true);
    });

    it('does not clobber an explicit caseInsensitiveUrl: false', async () => {
        const sw = boot({
            config: { mode: 'NoPrerender', caseInsensitiveUrl: false },
            configure: c => { c.self.prohibitedUrls = c.array([c.regex('/admin/')]); },
        });

        // The preset used to force caseInsensitiveUrl back to true, so /ADMIN/ was blocked
        // despite the app's explicit opt-out; the lowercase URL must still be blocked.
        const upper = await sw.fetchEvent({ url: `${ORIGIN}/ADMIN/x` });
        expect(upper.handled).toBe(false);

        const lower = await sw.fetchEvent({ url: `${ORIGIN}/admin/x` });
        expect(lower.response.status).toBe(403);
    });

    it('does not clobber an explicit noPrerenderQuery: "" (NoPrerender presets a query)', async () => {
        const sw = boot({ config: { mode: 'NoPrerender', noPrerenderQuery: '' } });
        const request = sw.fn('createNewAssetRequest')({ reqUrl: `${ORIGIN}/`, hash: 'h1', isDefault: true });
        expect(request.url).not.toContain('no-prerender');
    });
});

// The heart of the update flow: warm-start migration from the previous bucket, the stale-key
// diff, hashless refresh, and the forced default-document refresh.
describe('update migration and diff', () => {
    async function update({ oldEntries = {}, assets, config = {}, fetchHandler } = {}) {
        const sw = createServiceWorkerContext({ fetchHandler });
        sw.addClient();
        sw.configure(config);
        sw.self.assetsManifest = { version: 'v2', assets: sw.array(assets) };
        sw.load();

        const oldCache = await sw.caches.open('bit-bswup - v1');
        for (const [key, body] of Object.entries(oldEntries)) {
            await oldCache.put(key, { ok: true, status: 200, body, clone: () => ({ body }) });
        }

        await install(sw);
        return sw;
    }

    it('migrates unchanged hashed assets without re-downloading them', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/app.js.h1`]: 'v1-bytes' },
            assets: [{ url: 'app.js', hash: 'h1' }, { url: 'index.html', hash: 'idx2' }],
        });

        expect(sw.caches.snapshot()['bit-bswup - v2']).toContain(`${ORIGIN}/app.js.h1`);
        expect(sw.fetchLog.some(u => u.includes('app.js'))).toBe(false);
    });

    it('evicts the old hash and downloads the new one when an asset changes', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/app.js.h1`]: 'v1-bytes' },
            assets: [{ url: 'app.js', hash: 'h2' }, { url: 'index.html', hash: 'idx' }],
        });

        const keys = sw.caches.snapshot()['bit-bswup - v2'];
        expect(keys).toContain(`${ORIGIN}/app.js.h2`);
        expect(keys).not.toContain(`${ORIGIN}/app.js.h1`);
    });

    it('drops assets that left the manifest', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/gone.js.h1`]: 'x' },
            assets: [{ url: 'index.html', hash: 'idx' }],
        });

        expect(sw.caches.snapshot()['bit-bswup - v2']).not.toContain(`${ORIGIN}/gone.js.h1`);
    });

    it('re-downloads hashless assets on update by default', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/data.json`]: 'old' },
            assets: [{ url: 'data.json' }, { url: 'index.html', hash: 'idx' }],
        });

        expect(sw.fetchLog.some(u => u.includes('data.json'))).toBe(true);
    });

    it('keeps hashless assets when disableHashlessAssetsUpdate is set', async () => {
        const sw = await update({
            config: { disableHashlessAssetsUpdate: true },
            oldEntries: { [`${ORIGIN}/data.json`]: 'old' },
            assets: [{ url: 'data.json' }, { url: 'index.html', hash: 'idx' }],
        });

        expect(sw.fetchLog.some(u => u.includes('data.json'))).toBe(false);
        expect(sw.caches.snapshot()['bit-bswup - v2']).toContain(`${ORIGIN}/data.json`);
    });

    it('always refreshes the default document, even with an unchanged hash', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/index.html.idx`]: 'old shell' },
            assets: [{ url: 'index.html', hash: 'idx' }],
        });

        expect(sw.fetchLog.some(u => u.includes('index.html'))).toBe(true);
    });

    it('leaves the previous version bucket alone during install (deleted only after claim)', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/app.js.h1`]: 'x' },
            assets: [{ url: 'index.html', hash: 'idx' }],
        });

        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup - v1');
    });
});

// Duplicate spellings of one resource ('index.html' vs '/index.html' vs an absolute URL) used
// to produce two asset entries: double downloads and ambiguous first-wins matching.
describe('asset dedupe by resolved URL', () => {
    it('collapses equivalent spellings, letting the hash-carrying manifest entry win', async () => {
        const sw = boot({
            assets: [{ url: 'index.html', hash: 'sha256-idx' }],
            configure: c => { c.self.externalAssets = c.array([{ url: '/index.html' }]); },
        });
        await install(sw);

        // One entry, keyed by the manifest entry's hash - the hashless external duplicate is gone.
        expect(sw.caches.snapshot()['bit-bswup - v1']).toEqual([`${ORIGIN}/index.html.sha256-idx`]);
    });
});

// A mass outage (network drop mid-install) must not bury the console under hundreds of
// identical per-asset errors; the page handler still receives every single one.
describe('non-fatal console flood cap', () => {
    it('caps console output but still reports every failure to the page', async () => {
        const sw = createServiceWorkerContext({ fetchHandler: () => { throw new TypeError('network down'); } });
        sw.addClient();
        const consoleErrors = [];
        sw.self.console.error = (...args) => consoleErrors.push(args);
        sw.configure({ maxRetries: 0 });
        sw.self.assetsManifest = {
            version: 'v1',
            assets: sw.array(Array.from({ length: 12 }, (_, i) => ({ url: `a${i}.js`, hash: `h${i}` }))),
        };
        sw.load();
        await install(sw);

        expect(sw.messagesOfType('error')).toHaveLength(12);
        expect(consoleErrors).toHaveLength(11); // 10 logged + 1 suppression notice
    });
});

describe('enableCacheControl and cross-origin requests', () => {
    it('keeps the cache-control header for same-origin assets', () => {
        const sw = boot({ config: { enableCacheControl: true } });
        const request = sw.fn('createNewAssetRequest')({ reqUrl: `${ORIGIN}/app.js`, hash: 'sha256-a' });
        expect(request.cache).toBe('no-store');
        expect(request.headers).toEqual([['cache-control', 'no-cache']]);
    });

    it('omits the header for cross-origin assets (it would force a CORS preflight)', () => {
        const sw = boot({ config: { enableCacheControl: true } });
        const request = sw.fn('createNewAssetRequest')({ reqUrl: 'https://cdn.example.com/lib.js', hash: 'sha256-a' });
        expect(request.cache).toBe('no-store'); // the fetch *option* survives - CORS never sees it
        expect(request.headers).toBeUndefined();
    });
});

// matchAll({ includeUncontrolled: true }) returns every same-origin window client - including
// pages of unrelated apps mounted under other scopes on the same host. Broadcasting to those
// would make a sibling Bswup app react to this app's lifecycle messages.
describe('broadcast scope', () => {
    it('sends messages only to clients under this registration scope', async () => {
        const sw = createServiceWorkerContext();
        sw.self.registration = { scope: `${ORIGIN}/app/` };
        const inScope = [];
        const outOfScope = [];
        sw.clients.push({ type: 'window', url: `${ORIGIN}/app/page`, postMessage: m => inScope.push(m) });
        sw.clients.push({ type: 'window', url: `${ORIGIN}/other/page`, postMessage: m => outOfScope.push(m) });
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([]) };
        sw.load();

        sw.fn('sendMessage')({ type: 'activate', data: { version: 'v1' } });
        await sw.settle();

        expect(inScope.length).toBeGreaterThan(0);
        expect(outOfScope).toHaveLength(0);
    });

    it('falls back to broadcasting to all clients when no scope is available', async () => {
        // The harness defines no self.registration, mirroring an exotic runtime without one.
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([]) };
        sw.load();

        sw.fn('sendMessage')({ type: 'activate', data: { version: 'v1' } });
        await sw.settle();

        expect(sw.messagesOfType('activate')).toHaveLength(1);
    });
});

// A defaultUrl that matches no asset silently disables offline navigation - navigations just
// pass through to the network, so the app looks fine online and only fails offline.
describe('defaultUrl misconfiguration warning', () => {
    function warnsFrom({ config = {}, assets }) {
        const sw = createServiceWorkerContext();
        const warns = [];
        sw.self.console.warn = (...args) => warns.push(args.join(' '));
        sw.configure(config);
        sw.self.assetsManifest = { version: 'v1', assets: sw.array(assets) };
        sw.load();
        return warns.filter(w => w.includes('defaultUrl'));
    }

    it('warns at startup when no asset matches', () => {
        expect(warnsFrom({ assets: [{ url: 'app.js', hash: 'h1' }] })).toHaveLength(1);
    });

    it('stays silent when the default document resolves', () => {
        // boot()'s default DEFAULT_URL is 'index.html'; a matching manifest entry exists here.
        expect(warnsFrom({ assets: [{ url: 'index.html', hash: 'h1' }] })).toHaveLength(0);
    });

    it('stays silent when an externalAssets entry provides the default document', () => {
        expect(warnsFrom({
            config: { defaultUrl: '/', externalAssets: [{ url: '/' }] },
            assets: [{ url: 'app.js', hash: 'h1' }],
        })).toHaveLength(0);
    });

    it('stays silent under forcePrerender (the server owns navigations)', () => {
        expect(warnsFrom({ config: { forcePrerender: true }, assets: [{ url: 'app.js', hash: 'h1' }] })).toHaveLength(0);
    });
});

// The assets file must load for sub-path apps (https://host/myapp/) too: importScripts resolves
// a relative URL against the worker's own location, while a root-absolute default pointed those
// apps at the wrong path and failed the install with a 'manifest' error.
describe('assets file location', () => {
    function importsSeen(configure) {
        const sw = createServiceWorkerContext();
        const imports = [];
        sw.self.importScripts = url => {
            imports.push(url);
            sw.self.assetsManifest = { version: 'v1', assets: sw.array([]) };
        };
        if (configure) configure(sw);
        sw.load();
        return imports;
    }

    it('defaults to a relative path, resolved against the worker location', () => {
        expect(importsSeen()).toEqual(['service-worker-assets.js']);
    });

    it('honors an explicit assetsUrl', () => {
        expect(importsSeen(sw => { sw.self.assetsUrl = '/custom/assets.js'; })).toEqual(['/custom/assets.js']);
    });
});

// A pattern asset (RegExp externalAssets entry) matches every fingerprint it ever cached, so
// without a bound the cache grows by one dead resource-collection.<hash>.js per update, forever.
describe('pattern-asset generation cap', () => {
    const rcKey = h => `${ORIGIN}/_framework/resource-collection.${h}.js`;

    async function bootWithCachedGenerations(hashes) {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'sha256-idx' }]) };
        sw.self.externalAssets = sw.array([sw.regex('resource-collection\\..*\\.js')]);
        sw.load();
        const cache = await sw.caches.open('bit-bswup - v1');
        for (const h of hashes) await cache.put(rcKey(h), { ok: true, status: 200, body: h, clone: () => ({}) });
        return sw;
    }

    it('evicts the oldest generations beyond the cap (insertion order = age)', async () => {
        const sw = await bootWithCachedGenerations(['g1', 'g2', 'g3', 'g4', 'g5']);
        await install(sw);

        const keys = sw.caches.snapshot()['bit-bswup - v1'];
        expect(keys).not.toContain(rcKey('g1'));
        expect(keys).not.toContain(rcKey('g2'));
        expect(keys).toContain(rcKey('g3'));
        expect(keys).toContain(rcKey('g4'));
        expect(keys).toContain(rcKey('g5'));
    });

    it('keeps every generation while under the cap', async () => {
        const sw = await bootWithCachedGenerations(['g1', 'g2']);
        await install(sw);

        const keys = sw.caches.snapshot()['bit-bswup - v1'];
        expect(keys).toContain(rcKey('g1'));
        expect(keys).toContain(rcKey('g2'));
    });
});

// A cross-origin host that sends no CORS headers rejects a cors-mode fetch with the same
// TypeError as a network failure. Without the no-cors fallback such an externalAssets entry
// (the README's Google Tag Manager example) could never enter the cache and silently had no
// offline story at all.
describe('cross-origin externalAssets without CORS headers', () => {
    const CDN = 'https://cdn.example.com/lib.js';
    const opaque = () => ({ ok: false, status: 0, type: 'opaque', body: 'opaque-bytes', clone: () => ({ type: 'opaque', body: 'opaque-bytes' }) });

    it('precaches an opaque copy when the cors request is refused', async () => {
        const sw = boot({
            fetchHandler: (url, req) => {
                if (!url.startsWith('https://cdn.example.com/')) return { ok: true, status: 200, body: 'ok', clone: () => ({}) };
                if (req && req.mode === 'no-cors') return opaque();
                throw new TypeError('Failed to fetch');
            },
            configure: c => { c.self.externalAssets = c.array([{ url: CDN }]); },
        });
        await install(sw);

        expect(sw.messagesOfType('error')).toHaveLength(0);
        expect(sw.caches.snapshot()['bit-bswup - v1']).toContain(CDN);
    });

    it('never downgrades to no-cors when integrity was requested for the asset', async () => {
        const modes = [];
        const sw = boot({
            config: { enableIntegrityCheck: true },
            fetchHandler: (url, req) => {
                if (!url.startsWith('https://cdn.example.com/')) return { ok: true, status: 200, body: 'ok', clone: () => ({}) };
                modes.push(req && req.mode);
                throw new TypeError('Failed to fetch');
            },
            configure: c => { c.self.externalAssets = c.array([{ url: CDN, hash: 'sha256-abc' }]); },
        });
        await install(sw);

        // An opaque body cannot be integrity-verified: the asset must fail, not silently
        // downgrade to unverified bytes.
        expect(modes.length).toBeGreaterThan(0);
        expect(modes).not.toContain('no-cors');
        expect(sw.messagesOfType('error').length).toBeGreaterThan(0);
    });
});

// The browser may terminate an idle worker between any two async steps once the synchronous
// message handler returns - only a promise passed to e.waitUntil keeps it alive. CLIENTS_CLAIMED
// in particular is the only trigger that starts Blazor on a first install.
describe('page->worker commands extend the event lifetime', () => {
    it('SKIP_WAITING runs skipWaiting -> claim -> cache cleanup -> broadcast under waitUntil', async () => {
        const sw = boot();
        await sw.caches.open('bit-bswup - v0'); // stale bucket from a previous version

        const order = [];
        sw.self.skipWaiting = async () => { order.push('skipWaiting'); };
        sw.self.clients.claim = async () => { order.push('claim'); };

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        expect(waited).toBeTruthy(); // the chain must run under waitUntil, not as a bare return

        await waited;
        await sw.settle();

        expect(order).toEqual(['skipWaiting', 'claim']);
        expect(Object.keys(sw.caches.snapshot())).not.toContain('bit-bswup - v0');
        expect(sw.posted).toContain('WAITING_SKIPPED');
    });

    it('CLAIM_CLIENTS replies CLIENTS_CLAIMED to the requesting client under waitUntil', async () => {
        const sw = boot();
        await sw.caches.open('bit-bswup - v0');

        let claimed = 0;
        sw.self.clients.claim = async () => { claimed++; };

        const sourcePosted = [];
        let waited;
        sw.handlers.message({
            data: 'CLAIM_CLIENTS',
            source: { postMessage: m => sourcePosted.push(m) },
            waitUntil: p => { waited = p; },
        });
        expect(waited).toBeTruthy();

        await waited;
        await sw.settle();

        expect(claimed).toBe(1);
        expect(Object.keys(sw.caches.snapshot())).not.toContain('bit-bswup - v0');
        expect(sourcePosted).toEqual(['CLIENTS_CLAIMED']);
    });
});

describe('invalid manifest', () => {
    it('reports a fatal manifest error and refuses to install', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = undefined; // importScripts failed / file missing
        sw.load();
        // The manifest error is broadcast during module evaluation, but delivery goes through
        // clients.matchAll(), so it lands a microtask later.
        await sw.settle();

        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.reason === 'manifest' && e.data.fatal === true)).toBe(true);
        await expect(sw.fire('install')).rejects.toThrow();
    });

    it('does not throw at module-evaluation time on a malformed manifest', () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 5, assets: 'not-an-array' };
        expect(() => sw.load()).not.toThrow();
    });
});

describe('prepareRegExpArray', () => {
    const hits = (sw, patterns, url) => {
        const prepare = sw.fn('prepareRegExpArray');
        return prepare(sw.array(patterns)).some(p => p.test(url));
    };

    it('matches string entries literally, not as regex source', () => {
        const sw = boot();
        // '.' must not behave as a wildcard...
        expect(hits(sw, ['/admin/v1.0/'], `${ORIGIN}/admin/v1X0/`)).toBe(false);
        expect(hits(sw, ['/admin/v1.0/'], `${ORIGIN}/admin/v1.0/`)).toBe(true);
        // ...and quantifiers must not apply.
        expect(hits(sw, ['a+b'], `${ORIGIN}/aaab`)).toBe(false);
        expect(hits(sw, ['a+b'], `${ORIGIN}/a+b`)).toBe(true);
    });

    it('does not throw on a string that is not valid regex source', () => {
        const sw = boot();
        expect(() => hits(sw, ['('], `${ORIGIN}/(`)).not.toThrow();
        expect(hits(sw, ['('], `${ORIGIN}/(`)).toBe(true);
    });

    it('keeps full pattern semantics for RegExp entries', () => {
        const sw = boot();
        expect(hits(sw, [sw.regex('\\.css$')], `${ORIGIN}/a.css`)).toBe(true);
        expect(hits(sw, [sw.regex('\\.css$')], `${ORIGIN}/a.css?v=1`)).toBe(false);
    });

    it('strips g/y flags, which would make .test() stateful across requests', () => {
        const sw = boot();
        const prepared = sw.fn('prepareRegExpArray')(sw.array([sw.regex('admin', 'g')]));
        const url = `${ORIGIN}/admin/`;
        // With a surviving /g flag the second call would return false (lastIndex advanced).
        expect(prepared[0].test(url)).toBe(true);
        expect(prepared[0].test(url)).toBe(true);
    });

    it('applies caseInsensitiveUrl to the compiled patterns', () => {
        const sw = boot({ config: { caseInsensitiveUrl: true } });
        expect(hits(sw, ['/admin/'], `${ORIGIN}/ADMIN/`)).toBe(true);
    });
});

describe('prohibited urls', () => {
    it('answers 403 with a fixed body for any method', async () => {
        const sw = boot({ configure: c => { c.self.prohibitedUrls = c.array([c.regex('/admin/')]); } });

        const res = await sw.fire('fetch', { request: { url: `${ORIGIN}/admin/secrets`, method: 'GET', mode: 'cors' } });
        expect(res.status).toBe(403);
        expect(res.headers['X-Content-Type-Options']).toBe('nosniff');
        // The offending URL must not be reflected back (statusText is a reason-phrase).
        expect(res.statusText).toBe('Prohibited');
        expect(String(res.body)).not.toContain('secrets');
    });
});

describe('cache naming', () => {
    it('tracks the manifest version by default', async () => {
        const sw = boot({ version: 'abc123' });
        await install(sw);
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup - abc123');
    });

    it('honors an explicit cacheVersion override', async () => {
        const sw = boot({ version: 'abc123', config: { cacheVersion: '2026.05.31' } });
        await install(sw);
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup - 2026.05.31');
    });
});
