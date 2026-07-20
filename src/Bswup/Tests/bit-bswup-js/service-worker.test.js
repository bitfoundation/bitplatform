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
