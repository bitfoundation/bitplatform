import { describe, it, expect } from 'vitest';
import { createServiceWorkerContext, FakeResponse, ORIGIN } from './harness.js';

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
        // The per-asset error dispatch is not awaited by the install promise (sendError goes
        // through clients.matchAll()), so drain it rather than leave it floating past the test.
        await sw.settle();
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

// The lax install used to fire the cache build unawaited: the install settled while the
// download was still running, so the browser could terminate the event-idle worker mid-
// download (freezing a first install behind the splash), and 'waiting' - and with it
// updateReady - was announced while the new cache was still half-empty. The build now runs
// under the install event's waitUntil in both tolerance modes, and infrastructure failures
// (CacheStorage itself unusable) surface as a terminal error instead of dying silently.
describe('install lifetime and infrastructure failures', () => {
    function bootWithBrokenCaches({ config = {} } = {}) {
        const sw = createServiceWorkerContext({ cacheStorageError: new Error('quota exceeded') });
        sw.addClient();
        sw.configure(config);
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        return sw.load();
    }

    it('lax holds the install event open until the cache build has settled', async () => {
        const sw = boot({ assets: [{ url: 'index.html', hash: 'idx' }, { url: 'app.js', hash: 'h1' }] });
        await sw.fire('install');
        // Deliberately no settle(): resolution of the install promise itself must imply a
        // populated cache - that is what keeps the worker alive for the whole download and
        // what makes the later 'waiting' state mean "fully staged".
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/app.js.h1`);
    });

    it('lax resolves the install but reports a fatal install-infra error when CacheStorage is unusable', async () => {
        const sw = bootWithBrokenCaches();

        await expect(sw.fire('install')).resolves.not.toThrow();
        await sw.settle();

        // Previously this was swallowed with a diagnostic log: no progress, no bypass, no
        // error - a first install hung behind the splash with nothing telling the page to boot.
        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.reason === 'install-infra' && e.data.fatal === true)).toBe(true);
    });

    it('strict reports the same fatal install-infra error before rejecting', async () => {
        const sw = bootWithBrokenCaches({ config: { errorTolerance: 'strict' } });

        await expect(sw.fire('install')).rejects.toThrow(/quota/);
        await sw.settle();

        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.reason === 'install-infra' && e.data.fatal === true)).toBe(true);
    });

    it('a strict abort is not double-reported as install-infra', async () => {
        const sw = boot({
            config: { errorTolerance: 'strict', maxRetries: 0 },
            assets: [{ url: 'missing.js', hash: 'h2' }],
            fetchHandler: async () => { throw new Error('network'); },
        });
        await expect(sw.fire('install')).rejects.toThrow(/Install aborted/);
        await sw.settle();

        expect(sw.messagesOfType('error').some(e => e.data.reason === 'install-infra')).toBe(false);
    });

    it('a BLAZOR_STARTED top-up infrastructure failure never rejects the message event', async () => {
        const sw = bootWithBrokenCaches();

        let waited;
        sw.handlers.message({ data: 'BLAZOR_STARTED', waitUntil: p => { waited = p; } });
        expect(waited).toBeTruthy();
        await expect(waited).resolves.not.toThrow();
    });

    // Regression for the premature-updateReady bug: under lax the worker used to reach
    // 'installed' (waiting) the moment the download *started*, so the page announced "update
    // ready" - and an autoReload handler activated a half-empty cache, deleting the previous
    // complete one right after the claim - while the download was still running. The install
    // promise is what the browser's state machine waits on before 'waiting' can exist, so it
    // must stay pending until every download has settled.
    it('lax keeps the install pending while downloads are still in flight', async () => {
        let releaseFetch;
        const gate = new Promise(r => { releaseFetch = r; });
        const sw = boot({
            assets: [{ url: 'index.html', hash: 'idx' }, { url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => { await gate; return { ok: true, status: 200, clone: () => ({}) }; },
        });

        let settled = false;
        const installPromise = sw.fire('install');
        installPromise.then(() => { settled = true; });

        await sw.settle();
        expect(settled).toBe(false); // downloads in flight => the worker cannot reach 'waiting' yet

        releaseFetch();
        await installPromise;
        await sw.settle();

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/app.js.h1`);
        const percents = sw.messagesOfType('progress').map(m => m.data.percent);
        expect(Math.max(...percents)).toBe(100);
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
        for (const [key, entry] of Object.entries(oldEntries)) {
            // A plain string is only the body; an object also carries the headers the entry was
            // cached with - what a header-only refresh has to replace.
            const { body, headers } = (entry && typeof entry === 'object') ? entry : { body: entry };
            await oldCache.put(key, new FakeResponse(body, { status: 200, headers }));
        }

        await install(sw);
        return sw;
    }

    it('migrates unchanged hashed assets without re-downloading them', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/app.js.h1`]: 'v1-bytes' },
            assets: [{ url: 'app.js', hash: 'h1' }, { url: 'index.html', hash: 'idx2' }],
        });

        expect(sw.caches.snapshot()['bit-bswup:/ - v2']).toContain(`${ORIGIN}/app.js.h1`);
        expect(sw.fetchLog.some(u => u.includes('app.js'))).toBe(false);
    });

    it('evicts the old hash and downloads the new one when an asset changes', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/app.js.h1`]: 'v1-bytes' },
            assets: [{ url: 'app.js', hash: 'h2' }, { url: 'index.html', hash: 'idx' }],
        });

        const keys = sw.caches.snapshot()['bit-bswup:/ - v2'];
        expect(keys).toContain(`${ORIGIN}/app.js.h2`);
        expect(keys).not.toContain(`${ORIGIN}/app.js.h1`);
    });

    it('drops assets that left the manifest', async () => {
        const sw = await update({
            oldEntries: { [`${ORIGIN}/gone.js.h1`]: 'x' },
            assets: [{ url: 'index.html', hash: 'idx' }],
        });

        expect(sw.caches.snapshot()['bit-bswup:/ - v2']).not.toContain(`${ORIGIN}/gone.js.h1`);
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
        expect(sw.caches.snapshot()['bit-bswup:/ - v2']).toContain(`${ORIGIN}/data.json`);
    });

    // Why hashless matters even for unchanging bytes: the demo re-lists the PDF.js worker as a
    // hashless externalAsset because its CONTENT never changes - only its
    // Cross-Origin-Embedder-Policy did (credentialless -> require-corp). Migrating the cached
    // response would keep the pre-switch header and leave the dedicated worker failing its
    // embedder-policy check, so the refreshed response - headers included - has to land in the
    // new bucket.
    it('refreshes the cached headers of a hashless asset when only its headers changed', async () => {
        const workerUrl = '_content/Bit.BlazorUI.Legacy/pdf.js/pdfjs-4.7.76-worker.js';
        const worker = `${ORIGIN}/${workerUrl}`;
        const sw = await update({
            config: { externalAssets: [{ url: workerUrl }] },
            oldEntries: { [worker]: { body: 'worker-bytes', headers: { 'cross-origin-embedder-policy': 'credentialless' } } },
            assets: [{ url: 'index.html', hash: 'idx' }],
            // Same bytes, new header - exactly what the COEP switch produced on the server.
            fetchHandler: async url => url.includes('pdfjs-4.7.76-worker.js')
                ? new FakeResponse('worker-bytes', { status: 200, headers: { 'cross-origin-embedder-policy': 'require-corp' } })
                : new FakeResponse('ok', { status: 200 }),
        });

        const cached = await (await sw.caches.open('bit-bswup:/ - v2')).match(worker);
        expect(await cached.text()).toBe('worker-bytes');
        expect(cached.headers['cross-origin-embedder-policy']).toBe('require-corp');
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
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toEqual([`${ORIGIN}/index.html.sha256-idx`]);
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

// Multi-tab coverage: several tabs of the same app share one worker. The progress stream and
// the update-reload signal must reach every tab (uncontrolled ones included - nothing is
// controlled during a first install), while the first-install CLAIM_CLIENTS handshake must
// reply to each requesting tab individually (each tab starts its own Blazor instance).
describe('multi-tab flows', () => {
    function bootWithTabs() {
        const sw = createServiceWorkerContext();
        const tabA = [];
        const tabB = [];
        sw.clients.push({ type: 'window', url: `${ORIGIN}/`, postMessage: m => tabA.push(m) });
        sw.clients.push({ type: 'window', url: `${ORIGIN}/counter`, postMessage: m => tabB.push(m) });
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        sw.load();
        return { sw, tabA, tabB };
    }
    const parse = tab => tab.map(m => { try { return JSON.parse(m); } catch { return m; } });

    it('broadcasts install and progress to every open tab, not just the initiator', async () => {
        const { sw, tabA, tabB } = bootWithTabs();
        await install(sw);

        for (const tab of [tabA, tabB]) {
            const messages = parse(tab);
            expect(messages.some(m => m && m.type === 'install')).toBe(true);
            expect(messages.some(m => m && m.type === 'progress' && m.data.percent === 100)).toBe(true);
        }
    });

    it('replies CLIENTS_CLAIMED to each requesting tab individually', async () => {
        // Every tab that reaches 100% posts its own CLAIM_CLIENTS; the reply must go to that
        // tab's own source (broadcasting it would be fine; dropping one would strand a tab).
        const { sw } = bootWithTabs();
        const sourceA = [];
        const sourceB = [];
        let waitedA, waitedB;
        sw.handlers.message({ data: 'CLAIM_CLIENTS', source: { postMessage: m => sourceA.push(m) }, waitUntil: p => { waitedA = p; } });
        sw.handlers.message({ data: 'CLAIM_CLIENTS', source: { postMessage: m => sourceB.push(m) }, waitUntil: p => { waitedB = p; } });
        await waitedA;
        await waitedB;
        await sw.settle();

        expect(sourceA).toEqual(['CLIENTS_CLAIMED']);
        expect(sourceB).toEqual(['CLIENTS_CLAIMED']);
    });

    it('broadcasts WAITING_SKIPPED to every tab when an update is accepted in one of them', async () => {
        const { sw, tabA, tabB } = bootWithTabs();
        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(tabA).toContain('WAITING_SKIPPED');
        expect(tabB).toContain('WAITING_SKIPPED');
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

    // Asset routing folds case under caseInsensitiveUrl; the default-document pairing used to
    // be the one comparison that didn't, silently losing offline navigation to a case typo.
    it('pairs a differently-cased defaultUrl with its asset under caseInsensitiveUrl', () => {
        expect(warnsFrom({
            config: { caseInsensitiveUrl: true, defaultUrl: '/Index.html' },
            assets: [{ url: 'index.html', hash: 'h1' }],
        })).toHaveLength(0);
    });

    it('still treats case as significant when caseInsensitiveUrl is off', () => {
        expect(warnsFrom({
            config: { defaultUrl: '/Index.html' },
            assets: [{ url: 'index.html', hash: 'h1' }],
        })).toHaveLength(1);
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
        const cache = await sw.caches.open('bit-bswup:/ - v1');
        for (const h of hashes) await cache.put(rcKey(h), { ok: true, status: 200, body: h, clone: () => ({}) });
        return sw;
    }

    it('evicts the oldest generations beyond the cap (insertion order = age)', async () => {
        const sw = await bootWithCachedGenerations(['g1', 'g2', 'g3', 'g4', 'g5']);
        await install(sw);

        const keys = sw.caches.snapshot()['bit-bswup:/ - v1'];
        expect(keys).not.toContain(rcKey('g1'));
        expect(keys).not.toContain(rcKey('g2'));
        expect(keys).toContain(rcKey('g3'));
        expect(keys).toContain(rcKey('g4'));
        expect(keys).toContain(rcKey('g5'));
    });

    it('keeps every generation while under the cap', async () => {
        const sw = await bootWithCachedGenerations(['g1', 'g2']);
        await install(sw);

        const keys = sw.caches.snapshot()['bit-bswup:/ - v1'];
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
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(CDN);
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

    // deleteOldCaches() sits in the middle of both command chains and can reject under the
    // same storage pressure the install path is hardened against. It is best-effort tidiness;
    // a rejection must never swallow the signal that follows it - CLIENTS_CLAIMED is the ONLY
    // trigger that starts Blazor on a first install.
    it('CLAIM_CLIENTS still replies CLIENTS_CLAIMED when cache cleanup fails', async () => {
        const sw = boot();
        sw.caches.keys = async () => { throw new Error('storage pressure'); };

        const sourcePosted = [];
        let waited;
        sw.handlers.message({
            data: 'CLAIM_CLIENTS',
            source: { postMessage: m => sourcePosted.push(m) },
            waitUntil: p => { waited = p; },
        });

        await expect(waited).resolves.not.toThrow();
        await sw.settle();

        expect(sourcePosted).toEqual(['CLIENTS_CLAIMED']);
    });

    it('SKIP_WAITING still broadcasts WAITING_SKIPPED when cache cleanup fails', async () => {
        const sw = boot();
        sw.caches.keys = async () => { throw new Error('storage pressure'); };

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });

        await expect(waited).resolves.not.toThrow();
        await sw.settle();

        expect(sw.posted).toContain('WAITING_SKIPPED');
    });

    it('CLEAN_UP prunes this app\'s stale buckets when no update is staged or staging', async () => {
        const sw = boot();
        await sw.caches.open('bit-bswup:/ - v0'); // own previous version
        await sw.caches.open('bit-bswup - v0');   // legacy scope-less bucket
        await install(sw);                        // populates the current 'bit-bswup:/ - v1'

        let waited;
        sw.handlers.message({ data: 'CLEAN_UP', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        const keys = Object.keys(sw.caches.snapshot());
        expect(keys).toContain('bit-bswup:/ - v1');
        expect(keys).not.toContain('bit-bswup:/ - v0');
        expect(keys).not.toContain('bit-bswup - v0');
    });

    // "Stale" is relative to the NEWEST worker, not the receiver: deleteOldCaches() spares
    // only the receiver's own CACHE_NAME, so with an update staged it would delete a LIVE
    // cache - the update's freshly downloaded bucket (received by the old active worker), or
    // the active worker's bucket out from under every running page (received by a waiting
    // worker, which always sees itself in registration.waiting - one guard covers both).
    it('CLEAN_UP is refused while an update is staged', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.registration = { scope: `${ORIGIN}/`, waiting: {} };
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        sw.load();
        await sw.caches.open('bit-bswup:/ - v2'); // the staged update's live cache
        await sw.caches.open('bit-bswup:/ - v0'); // genuinely stale - deferred to activation

        let waited;
        sw.handlers.message({ data: 'CLEAN_UP', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        const keys = Object.keys(sw.caches.snapshot());
        expect(keys).toContain('bit-bswup:/ - v2');
        expect(keys).toContain('bit-bswup:/ - v0');
    });
});

// The self-destructing recovery worker (bit-bswup.sw-cleanup.js) is deployed in place of the
// real worker to back an app out of Bswup - often precisely because origin storage is broken.
describe('cleanup worker', () => {
    function bootCleanup({ registration } = {}) {
        const sw = createServiceWorkerContext();
        sw.addClient();
        if (registration) sw.self.registration = registration;
        return sw.load('bit-bswup.sw-cleanup.js');
    }

    it('purges the Bswup/Blazor caches, then tells clients to unregister and nudges a reload', async () => {
        const sw = bootCleanup();
        await sw.caches.open('bit-bswup - v1');
        await sw.caches.open('blazor-resources-/');
        await sw.caches.open('my-app-data');

        await sw.fire('activate');

        expect(Object.keys(sw.caches.snapshot())).toEqual(['my-app-data']);
        expect(sw.posted).toContain('UNREGISTER');
        expect(sw.posted).toContain('WAITING_SKIPPED'); // the delayed fallback reload signal
    });

    it('still tells clients to unregister when the cache purge fails', async () => {
        const sw = bootCleanup();
        sw.caches.keys = async () => { throw new Error('storage broken'); };

        // A purge rejection used to abort teardownClients() before any client was messaged,
        // stranding every tab on the broken worker forever - the opposite of this worker's job.
        await expect(sw.fire('activate')).resolves.not.toThrow();

        expect(sw.posted).toContain('UNREGISTER');
    });

    // The UNREGISTER message is inherently racy (tabs the takeover switched are already
    // reloading when it is posted, and later visitors never hear it at all), so the worker
    // must remove its own registration - the canonical self-destroying pattern. Relying on
    // clients alone left the registration alive indefinitely on the common path.
    it('unregisters its own registration during teardown', async () => {
        let unregistered = 0;
        const sw = bootCleanup({ registration: { scope: `${ORIGIN}/`, unregister: async () => { unregistered++; return true; } } });

        await sw.fire('activate');

        expect(unregistered).toBe(1);
    });

    it('still messages clients when self-unregister fails', async () => {
        const sw = bootCleanup({ registration: { scope: `${ORIGIN}/`, unregister: async () => { throw new Error('nope'); } } });

        await expect(sw.fire('activate')).resolves.not.toThrow();

        expect(sw.posted).toContain('UNREGISTER');
    });

    // claim() would attach UNCONTROLLED pages - including the very tabs that just reloaded to
    // detach, and every future visitor - re-entangling them with a worker whose whole purpose
    // is to disappear, and turning each later page load into another claim/reload cycle.
    // skipWaiting-driven activation already hands over the previously controlled tabs.
    it('never claims clients', async () => {
        let claimed = 0;
        const sw = bootCleanup({ registration: { scope: `${ORIGIN}/`, unregister: async () => true } });
        sw.self.clients.claim = async () => { claimed++; };

        await sw.fire('activate');

        expect(claimed).toBe(0);
    });
});

// The activate-time prune is the "waiting worker activated naturally after every tab closed"
// path. Getting its conditions wrong deletes a live cache under open tabs - the classic
// SRI/boot-hash corruption - so each branch is pinned here.
describe('activate-time cache pruning', () => {
    async function bootInstalled({ withClient = false, registration } = {}) {
        const sw = createServiceWorkerContext();
        if (withClient) sw.addClient();
        if (registration) sw.self.registration = registration;
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        sw.load();
        await install(sw);
        await sw.caches.open(registration ? `bit-bswup:${new URL(registration.scope).pathname} - v0` : 'bit-bswup:/ - v0');
        return sw;
    }

    it('prunes own stale buckets when no window clients are open', async () => {
        const sw = await bootInstalled();

        await sw.fire('activate');
        await sw.settle();

        const keys = Object.keys(sw.caches.snapshot());
        expect(keys).toContain('bit-bswup:/ - v1');
        expect(keys).not.toContain('bit-bswup:/ - v0');
    });

    it('defers pruning while window clients are open (they may still ride the old cache)', async () => {
        const sw = await bootInstalled({ withClient: true });

        await sw.fire('activate');
        await sw.settle();

        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - v0');
    });

    it('skips pruning while a newer version is already staging', async () => {
        const sw = await bootInstalled({ registration: { scope: `${ORIGIN}/`, installing: {} } });

        await sw.fire('activate');
        await sw.settle();

        // The "stale" bucket could just as well be the newer install's freshly migrated one -
        // deferral is the only safe answer while an install is in flight.
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - v0');
    });
});

// deleteOldCaches() spares only the receiver's own CACHE_NAME, so every prune site - not just
// CLEAN_UP - must refuse to run while another version is mid-install.
describe('pruning never races a newer install', () => {
    function bootWithRegistration(registration) {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.registration = registration;
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        return sw.load();
    }

    it('SKIP_WAITING skips pruning while a newer version is installing, but still signals', async () => {
        // active is set: this models an UPDATE being accepted while yet another version
        // installs; without it the receiver would count as a first-install activation and
        // reply CLIENTS_CLAIMED to its source instead of broadcasting.
        const sw = bootWithRegistration({ scope: `${ORIGIN}/`, active: {}, installing: {} });
        await sw.caches.open('bit-bswup:/ - v0'); // could be the newer install's bucket

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - v0');
        expect(sw.posted).toContain('WAITING_SKIPPED'); // deferring must never cost the signal
    });

    it('CLAIM_CLIENTS skips pruning while an update is staged, but still replies', async () => {
        const sw = bootWithRegistration({ scope: `${ORIGIN}/`, waiting: {} });
        await sw.caches.open('bit-bswup:/ - v2'); // the staged update's live bucket

        const sourcePosted = [];
        let waited;
        sw.handlers.message({ data: 'CLAIM_CLIENTS', source: { postMessage: m => sourcePosted.push(m) }, waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - v2');
        expect(sourcePosted).toEqual(['CLIENTS_CLAIMED']);
    });
});

// A transient blip (CDN hiccup, 5xx, dropped connection) must not fail a strict install or
// silently drop an asset under lax; deterministic failures must not burn time retrying.
describe('download retry with backoff', () => {
    it('retries a transient rejection and succeeds without reporting an error', async () => {
        let calls = 0;
        const sw = boot({
            config: { maxRetries: 2, retryDelay: 1 },
            assets: [{ url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => {
                if (++calls === 1) throw new TypeError('connection reset');
                return { ok: true, status: 200, clone: () => ({}) };
            },
        });
        await install(sw);

        expect(calls).toBe(2);
        expect(sw.messagesOfType('error')).toHaveLength(0);
        expect(Math.max(...sw.messagesOfType('progress').map(m => m.data.percent))).toBe(100);
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/app.js.h1`);
    });

    it('retries transient HTTP statuses (500) until they succeed', async () => {
        let calls = 0;
        const sw = boot({
            config: { maxRetries: 2, retryDelay: 1 },
            assets: [{ url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => (++calls < 3
                ? { ok: false, status: 500, statusText: 'oops', clone: () => ({}) }
                : { ok: true, status: 200, clone: () => ({}) }),
        });
        await install(sw);

        expect(calls).toBe(3);
        expect(sw.messagesOfType('error')).toHaveLength(0);
    });

    it('does not retry permanent HTTP statuses (404)', async () => {
        let calls = 0;
        const sw = boot({
            config: { maxRetries: 2, retryDelay: 1 },
            assets: [{ url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => { calls++; return { ok: false, status: 404, statusText: 'Not Found', clone: () => ({}) }; },
        });
        await install(sw);

        expect(calls).toBe(1); // re-fetching a 404 cannot change the answer
        const errors = sw.messagesOfType('error');
        expect(errors).toHaveLength(1);
        expect(errors[0].data.status).toBe(404);
    });

    it('does not retry an SRI/integrity failure (identical bytes fail identically)', async () => {
        let calls = 0;
        const sw = boot({
            config: { maxRetries: 2, retryDelay: 1, enableIntegrityCheck: true },
            assets: [{ url: 'app.js', hash: 'sha256-abc' }],
            fetchHandler: async () => { calls++; throw new TypeError('Failed to find a valid digest in the integrity attribute'); },
        });
        await install(sw);

        expect(calls).toBe(1);
        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.reason === 'integrity')).toBe(true);
    });

    it('falls back to the default retry policy when the config is not a sane number', async () => {
        let calls = 0;
        const sw = boot({
            config: { maxRetries: 'plenty', retryDelay: -100 },
            assets: [{ url: 'app.js', hash: 'h1' }],
            fetchHandler: async () => { calls++; throw new TypeError('down'); },
        });
        await install(sw);

        expect(calls).toBe(3); // default MAX_RETRIES = 2 extra attempts after the first
    });
});

describe('strict abort hygiene', () => {
    it('discards the partially populated bucket so a later install cannot warm-start from it', async () => {
        const sw = boot({
            config: { errorTolerance: 'strict', maxRetries: 0 },
            assets: [{ url: 'good.js', hash: 'g1' }, { url: 'missing.js', hash: 'm1' }],
            fetchHandler: async url => {
                if (url.includes('missing')) throw new Error('network');
                return { ok: true, status: 200, clone: () => ({}) };
            },
        });
        await expect(sw.fire('install')).rejects.toThrow(/Install aborted/);
        await sw.settle();

        expect(Object.keys(sw.caches.snapshot())).not.toContain('bit-bswup:/ - v1');
    });
});

describe('integrity failure aggregation', () => {
    it('reports one advisory install-incomplete with the failure count under lax', async () => {
        const sw = boot({
            config: { maxRetries: 0, enableIntegrityCheck: true },
            assets: [{ url: 'a.js', hash: 'sha256-a' }, { url: 'b.js', hash: 'sha256-b' }, { url: 'index.html', hash: 'idx' }],
            fetchHandler: async url => {
                if (url.includes('index')) return { ok: true, status: 200, clone: () => ({}) };
                throw new TypeError('Failed to find a valid digest in the integrity attribute');
            },
        });
        await expect(sw.fire('install')).resolves.not.toThrow();
        await sw.settle();

        const incomplete = sw.messagesOfType('error').filter(e => e.data.reason === 'install-incomplete');
        expect(incomplete).toHaveLength(1);
        expect(incomplete[0].data.count).toBe(2);
        expect(incomplete[0].data.fatal).toBe(false); // advisory under lax - the app still runs
    });
});

// Deleting an entry before its replacement has downloaded turns a failed refresh into "gone":
// the asset (or the app shell itself) that WAS available offline no longer is. cache.put
// overwrites in place, so the old copy must survive until the new bytes actually land.
describe('failed refreshes keep the last good copy', () => {
    async function updateWithFailingRefresh({ oldEntries, assets, failFor }) {
        const sw = createServiceWorkerContext({
            fetchHandler: async url => {
                if (url.includes(failFor)) throw new TypeError('network down mid-update');
                return { ok: true, status: 200, clone: () => ({}) };
            },
        });
        sw.addClient();
        sw.self.assetsManifest = { version: 'v2', assets: sw.array(assets) };
        sw.load();
        const oldCache = await sw.caches.open('bit-bswup - v1');
        for (const [key, body] of Object.entries(oldEntries)) {
            await oldCache.put(key, { ok: true, status: 200, body, clone: () => ({ body }) });
        }
        await install(sw);
        return sw;
    }

    it('keeps the cached default document when its forced refresh fails', async () => {
        const sw = await updateWithFailingRefresh({
            oldEntries: { [`${ORIGIN}/index.html.idx`]: 'old shell' },
            assets: [{ url: 'index.html', hash: 'idx' }],
            failFor: 'index.html',
        });

        // Offline navigation survives on the previous shell instead of dying entirely.
        const cache = await sw.caches.open('bit-bswup:/ - v2');
        const kept = await cache.match(`${ORIGIN}/index.html.idx`);
        expect(kept && kept.body).toBe('old shell');
    });

    it('keeps a hashless asset when its refresh fails', async () => {
        const sw = await updateWithFailingRefresh({
            oldEntries: { [`${ORIGIN}/data.json`]: 'old data' },
            assets: [{ url: 'data.json' }, { url: 'index.html', hash: 'idx' }],
            failFor: 'data.json',
        });

        const cache = await sw.caches.open('bit-bswup:/ - v2');
        const kept = await cache.match(`${ORIGIN}/data.json`);
        expect(kept && kept.body).toBe('old data');
    });

    it('still replaces the copy once the refresh succeeds', async () => {
        // The keep-until-replaced behavior must not decay into never-refreshing.
        const sw = createServiceWorkerContext({
            fetchHandler: async () => ({ ok: true, status: 200, body: 'new data', clone: () => ({ ok: true, status: 200, body: 'new data' }) }),
        });
        sw.addClient();
        sw.self.assetsManifest = { version: 'v2', assets: sw.array([{ url: 'data.json' }, { url: 'index.html', hash: 'idx' }]) };
        sw.load();
        const oldCache = await sw.caches.open('bit-bswup - v1');
        await oldCache.put(`${ORIGIN}/data.json`, { ok: true, status: 200, body: 'old data', clone: () => ({ body: 'old data' }) });
        await install(sw);

        const cache = await sw.caches.open('bit-bswup:/ - v2');
        const refreshed = await cache.match(`${ORIGIN}/data.json`);
        expect(refreshed && refreshed.body).toBe('new data');
    });
});

// One bad entry (an externalAssets typo, a corrupt manifest line) must cost that one asset -
// not tear down the whole worker at module-evaluation time with no structured error.
describe('invalid asset URLs', () => {
    it('skips an unparseable externalAssets url and still installs the rest', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        sw.self.externalAssets = sw.array(['https://']); // new Request('https://') throws

        expect(() => sw.load()).not.toThrow();

        // Reported at INSTALL time, not module-evaluation time: the global scope re-runs on
        // every cold start of the worker, and a module-scope report would replay the same
        // error to the page for the whole lifetime of the version.
        await sw.settle();
        expect(sw.messagesOfType('error')).toHaveLength(0);

        await install(sw);

        const requestErrors = sw.messagesOfType('error').filter(e => e.data.reason === 'request');
        expect(requestErrors).toHaveLength(1);
        expect(requestErrors[0].data.fatal).toBe(false);
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/index.html.idx`);
    });
});

describe('service worker scripts are never precached', () => {
    const swScripts = [
        '_content/Bit.Bswup/bit-bswup.sw.js',
        '_content/Bit.Bswup/bit-bswup.sw.min.js',
        '_content/Bit.Bswup/bit-bswup.sw-cleanup.js',
        '_content/Bit.Bswup/bit-bswup.sw-cleanup.min.js',
        'service-worker.js',
    ];

    it('excludes every shipped worker-script variant by default', async () => {
        const sw = boot({ assets: [{ url: 'index.html', hash: 'idx' }, ...swScripts.map(url => ({ url, hash: 'h' }))] });
        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toEqual([`${ORIGIN}/index.html.idx`]);
    });

    it('ignoreDefaultExclude opts back into caching them', async () => {
        const sw = boot({
            config: { ignoreDefaultExclude: true },
            assets: [{ url: 'service-worker.js', hash: 'h' }, { url: 'index.html', hash: 'idx' }],
        });
        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/service-worker.js.h`);
    });

    // The hardcoded list only covers the default name, but the page script lets an app
    // register any filename via its `sw` attribute - the exclusion must track the RUNNING
    // worker's own URL (self.location), in every manifest spelling that resolves to it.
    it('excludes a custom-named worker script, derived from self.location', async () => {
        const sw = boot({
            assets: [
                { url: 'index.html', hash: 'idx' },
                { url: 'my-worker.js', hash: 'h1' },
                { url: '/my-worker.js', hash: 'h2' },
                { url: `${ORIGIN}/my-worker.js`, hash: 'h3' },
            ],
            configure: c => { c.self.location = { origin: ORIGIN, href: `${ORIGIN}/my-worker.js` }; },
        });
        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toEqual([`${ORIGIN}/index.html.idx`]);
    });

    // A same-named file in a subfolder is a DIFFERENT file (manifest entries resolve against
    // the worker's own directory) and must keep being precached.
    it('does not exclude a same-named script in a subfolder', async () => {
        const sw = boot({
            assets: [{ url: 'index.html', hash: 'idx' }, { url: 'js/my-worker.js', hash: 'h1' }],
            configure: c => { c.self.location = { origin: ORIGIN, href: `${ORIGIN}/my-worker.js` }; },
        });
        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toContain(`${ORIGIN}/js/my-worker.js.h1`);
    });
});

// /\.wasm/, /\.pdb/ and /\.html/ were historically unanchored, so any URL merely CONTAINING
// the extension was swept into the precache. The anchored forms must keep the one legitimate
// loose match: the .br/.gz compressed variants Blazor publishes, which the
// decompress-in-browser loadBootResource pattern fetches directly and therefore needs offline.
describe('default include anchoring', () => {
    it('still precaches compressed variants of wasm/pdb/html assets', async () => {
        const sw = boot({
            assets: [
                { url: 'index.html', hash: 'idx' },
                { url: '_framework/dotnet.native.wasm.br', hash: 'h1' },
                { url: '_framework/app.pdb.gz', hash: 'h2' },
                { url: 'offline.html.br', hash: 'h3' },
            ],
        });
        await install(sw);

        const cached = sw.caches.snapshot()['bit-bswup:/ - v1'];
        expect(cached).toContain(`${ORIGIN}/_framework/dotnet.native.wasm.br.h1`);
        expect(cached).toContain(`${ORIGIN}/_framework/app.pdb.gz.h2`);
        expect(cached).toContain(`${ORIGIN}/offline.html.br.h3`);
    });

    it('no longer sweeps in URLs that merely contain the extension', async () => {
        const sw = boot({
            assets: [
                { url: 'index.html', hash: 'idx' },
                { url: 'x.htmlsomething', hash: 'h1' },
                { url: 'data.wasmfile', hash: 'h2' },
                { url: 'notes.pdbx', hash: 'h3' },
            ],
        });
        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).toEqual([`${ORIGIN}/index.html.idx`]);
    });
});

describe('passive mode', () => {
    const assets = [{ url: 'index.html', hash: 'idx' }, { url: 'app.js', hash: 'h1' }];

    it('first install downloads nothing and reports bypass { firstTime: true }', async () => {
        const sw = boot({ config: { isPassive: true }, assets });
        await install(sw);

        expect(sw.fetchLog).toHaveLength(0);
        const bypass = sw.messagesOfType('bypass');
        expect(bypass).toHaveLength(1);
        expect(bypass[0].data.firstTime).toBe(true);
    });

    // The offline story must not depend on whether a lazy-fill put happened to land before
    // BLAZOR_STARTED arrived: the top-up deterministically fills whatever is missing.
    it('the BLAZOR_STARTED top-up fills the cache even when nothing was lazily cached yet', async () => {
        const sw = boot({ config: { isPassive: true }, assets });
        await install(sw);
        expect(sw.fetchLog).toHaveLength(0);

        let waited;
        sw.handlers.message({ data: 'BLAZOR_STARTED', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        const keys = sw.caches.snapshot()['bit-bswup:/ - v1'];
        expect(keys).toContain(`${ORIGIN}/index.html.idx`);
        expect(keys).toContain(`${ORIGIN}/app.js.h1`);
        // And silently: the page UI is long gone - no progress stream, no second bypass.
        expect(sw.messagesOfType('progress')).toHaveLength(0);
        expect(sw.messagesOfType('bypass')).toHaveLength(1);
    });
});

// A first install can be activated through the skipWaiting path too (BitBswup.skipWaiting()
// catching the transient waiting state). The initiating page must then get the first-install
// completion signal - CLIENTS_CLAIMED, which starts Blazor in place and triggers the
// post-start top-up - not the update reload signal, which would kill the boot that the
// claim's controllerchange just started.
describe('SKIP_WAITING distinguishes first installs from updates', () => {
    function bootWith(registration) {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.registration = registration;
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        return sw.load();
    }

    it('replies CLIENTS_CLAIMED to the source on a first-install activation', async () => {
        const sw = bootWith({ scope: `${ORIGIN}/` }); // no active worker => first install

        const sourcePosted = [];
        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', source: { postMessage: m => sourcePosted.push(m) }, waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(sourcePosted).toEqual(['CLIENTS_CLAIMED']);
        expect(sw.posted).not.toContain('WAITING_SKIPPED');
    });

    it('broadcasts WAITING_SKIPPED on a real update activation', async () => {
        const sw = bootWith({ scope: `${ORIGIN}/`, active: {} }); // previous version active => update

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(sw.posted).toContain('WAITING_SKIPPED');
    });

    it('still prunes when only the receiver itself lingers in the waiting slot mid-transition', async () => {
        // Some engines briefly leave the activating worker visible in registration.waiting;
        // that must not veto its own pruning - only a DIFFERENT worker there may.
        const sw = bootWith({ scope: `${ORIGIN}/`, active: {} });
        const selfWorker = {};
        sw.self.serviceWorker = selfWorker;
        sw.self.registration.waiting = selfWorker;
        await sw.caches.open('bit-bswup:/ - v0');

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        expect(Object.keys(sw.caches.snapshot())).not.toContain('bit-bswup:/ - v0');
    });
});

describe('precache rejects partial responses', () => {
    it('treats a 206 answer to a full precache request as a failure and caches nothing', async () => {
        // A resuming middlebox answering the versioned request with a fragment: response.ok
        // is TRUE for 206, and a fragment stored as the asset's full body would be served
        // broken until the next hash change (applyRangeHeader refuses non-200 responses).
        const sw = boot({
            config: { maxRetries: 0 },
            assets: [{ url: 'clip.mp4', hash: 'h1' }, { url: 'index.html', hash: 'idx' }],
            fetchHandler: async url => (url.includes('clip.mp4')
                ? { ok: true, status: 206, statusText: 'Partial Content', clone: () => ({}) }
                : { ok: true, status: 200, clone: () => ({}) }),
            configure: c => { c.self.assetsInclude = c.array([c.regex('\\.mp4$')]); },
        });
        await install(sw);

        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.status === 206)).toBe(true);
        expect(sw.caches.snapshot()['bit-bswup:/ - v1']).not.toContain(`${ORIGIN}/clip.mp4.h1`);
    });
});

describe('pattern retention never adopts stale hashed keys', () => {
    it('deletes a hashed concrete key even when an unanchored pattern matches it', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 'v1', assets: sw.array([{ url: 'index.html', hash: 'idx' }]) };
        sw.self.externalAssets = sw.array([sw.regex('resource-collection')]); // no anchors
        sw.load();
        const cache = await sw.caches.open('bit-bswup:/ - v1');
        // A genuine lazily-cached generation, and a stale hashed key of a re-hashed concrete
        // asset that the unanchored pattern also matches.
        await cache.put(`${ORIGIN}/_framework/resource-collection.g1.js`, { ok: true, status: 200, clone: () => ({}) });
        await cache.put(`${ORIGIN}/_framework/resource-collection.g0.js.sha256-dead`, { ok: true, status: 200, clone: () => ({}) });

        await install(sw);

        const keys = sw.caches.snapshot()['bit-bswup:/ - v1'];
        expect(keys).toContain(`${ORIGIN}/_framework/resource-collection.g1.js`);
        expect(keys).not.toContain(`${ORIGIN}/_framework/resource-collection.g0.js.sha256-dead`);
    });
});

describe('the top-up never re-downloads what the install just fetched', () => {
    it('skips the hashless/default-document refresh on the BLAZOR_STARTED pass', async () => {
        const sw = boot({ assets: [{ url: 'index.html', hash: 'idx' }, { url: 'data.json' }] });
        await install(sw);
        const installFetches = sw.fetchLog.length; // shell + hashless data.json

        let waited;
        sw.handlers.message({ data: 'BLAZOR_STARTED', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        // The refresh semantics belong to real updates; seconds after the install they only
        // doubled every hashless (and shell) download on every single first install.
        expect(sw.fetchLog.length).toBe(installFetches);
    });
});

describe('noPrerenderQuery', () => {
    it('appends the preset no-prerender query to the default document request', () => {
        const sw = boot({ config: { mode: 'NoPrerender' } });
        const request = sw.fn('createNewAssetRequest')({ reqUrl: `${ORIGIN}/`, hash: 'h1', isDefault: true });
        expect(request.url).toContain('no-prerender=true');
    });
});

describe('sendMessage resilience', () => {
    it('survives a clients.matchAll rejection without an unhandled rejection', async () => {
        const sw = boot();
        sw.self.clients.matchAll = async () => { throw new Error('dying worker'); };

        expect(() => sw.fn('sendMessage')({ type: 'activate', data: {} })).not.toThrow();
        await sw.settle(); // an unhandled rejection here would fail the test run
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

    // A present-but-malformed manifest must take the same path as a missing one: a bad shape is
    // caught by validateAssetsManifest (here both the non-string version and the non-array
    // assets), never by a crash mid-evaluation that would leave the page with no error at all.
    it('does not throw at module-evaluation time on a malformed manifest', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.assetsManifest = { version: 5, assets: 'not-an-array' };
        expect(() => sw.load()).not.toThrow();
        await sw.settle();

        const errors = sw.messagesOfType('error');
        expect(errors.some(e => e.data.reason === 'manifest' && e.data.fatal === true)).toBe(true);
        await expect(sw.fire('install')).rejects.toThrow();
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
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - abc123');
    });

    it('honors an explicit cacheVersion override', async () => {
        const sw = boot({ version: 'abc123', config: { cacheVersion: '2026.05.31' } });
        await install(sw);
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/ - 2026.05.31');
    });
});

// CacheStorage is origin-global while service worker registrations are per-scope. The
// pre-scoping name (`bit-bswup - <version>`) made every app's deleteOldCaches() treat a
// sibling app's bucket as a stale version of its own: two Bswup apps on one origin
// perpetually evicted each other's offline caches on every update.
describe('cache identity is scoped per registration', () => {
    function bootScoped({ scope = `${ORIGIN}/app/`, version = 'v2', assets } = {}) {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.registration = { scope };
        sw.self.assetsManifest = {
            version,
            assets: sw.array(assets || [{ url: 'index.html', hash: 'idx' }, { url: 'app.js', hash: 'h1' }]),
        };
        return sw.load();
    }

    it('includes the registration scope path in the cache name', async () => {
        const sw = bootScoped({ scope: `${ORIGIN}/app/`, version: 'v1' });
        await install(sw);
        expect(Object.keys(sw.caches.snapshot())).toContain('bit-bswup:/app/ - v1');
    });

    it('cleanup reclaims own previous and legacy buckets but never a sibling scope', async () => {
        const sw = bootScoped();
        await sw.caches.open('bit-bswup:/app/ - v1');   // this app's previous version
        await sw.caches.open('bit-bswup - v1');          // legacy scope-less (pre v-10-6-0)
        await sw.caches.open('bit-bswup:/other/ - v1');  // a sibling app's live bucket

        let waited;
        sw.handlers.message({ data: 'SKIP_WAITING', waitUntil: p => { waited = p; } });
        await waited;
        await sw.settle();

        const keys = Object.keys(sw.caches.snapshot());
        expect(keys).toContain('bit-bswup:/other/ - v1'); // the sibling survives
        expect(keys).not.toContain('bit-bswup:/app/ - v1');
        expect(keys).not.toContain('bit-bswup - v1');
    });

    it('migrates from its own previous bucket without re-downloading', async () => {
        const sw = bootScoped();
        const own = await sw.caches.open('bit-bswup:/app/ - v1');
        await own.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'bytes', clone: () => ({}) });

        await install(sw);

        expect(sw.caches.snapshot()['bit-bswup:/app/ - v2']).toContain(`${ORIGIN}/app.js.h1`);
        expect(sw.fetchLog.some(u => u.includes('app.js'))).toBe(false);
    });

    it('never uses a sibling scope bucket as a migration source', async () => {
        const sw = bootScoped();
        // The sibling happens to hold a byte-identical entry; warm-starting from it would
        // "work" here, but it belongs to a different app - it must not be treated as ours.
        const sibling = await sw.caches.open('bit-bswup:/other/ - v9');
        await sibling.put(`${ORIGIN}/app.js.h1`, { ok: true, status: 200, body: 'bytes', clone: () => ({}) });

        await install(sw);

        expect(sw.fetchLog.some(u => u.includes('app.js'))).toBe(true); // downloaded, not copied
    });

    it('the cleanup worker purges own + legacy + blazor buckets but leaves sibling scopes', async () => {
        const sw = createServiceWorkerContext();
        sw.addClient();
        sw.self.registration = { scope: `${ORIGIN}/app/` };
        sw.load('bit-bswup.sw-cleanup.js');
        await sw.caches.open('bit-bswup:/app/ - v1');
        await sw.caches.open('bit-bswup - v0');
        await sw.caches.open('bit-bswup:/other/ - v1');
        await sw.caches.open('blazor-resources-/app/');
        await sw.caches.open('my-app-data');

        await sw.fire('activate');

        expect(Object.keys(sw.caches.snapshot()).sort()).toEqual(['bit-bswup:/other/ - v1', 'my-app-data']);
    });
});
