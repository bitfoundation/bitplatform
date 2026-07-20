import { describe, it, expect } from 'vitest';
import { createPageContext } from './harness.js';

/** A page with the bswup + blazor script tags in place, before bit-bswup.js is loaded. */
function page(options = {}, { swOptions, blazor = true } = {}) {
    const ctx = createPageContext(options);
    ctx.addBswupScriptTag(options.scriptAttributes || {});
    if (blazor) ctx.addBlazorScriptTag();
    ctx.window.Blazor = { start: async () => { ctx.window.__blazorStarted = (ctx.window.__blazorStarted || 0) + 1; } };
    ctx.setServiceWorker(swOptions || {});
    return ctx;
}

describe('service worker registration scope', () => {
    it('registers once with the configured scope when the browser accepts it', async () => {
        const ctx = page({ scriptAttributes: { scope: '/' } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(ctx.registrations).toEqual(['/']);
    });

    // A worker may only control URLs under its own folder, so the default '/' is rejected for an
    // app mounted at /myapp/. Losing the service worker entirely there is worse than narrowing.
    it('retries with the default scope when the browser rejects the configured one', async () => {
        const securityError = Object.assign(new Error('scope not allowed'), { name: 'SecurityError' });
        const ctx = page({ scriptAttributes: { scope: '/' } }, { swOptions: { registerError: securityError } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(ctx.registrations).toEqual(['/', '<omitted>']);
    });

    // Chrome's script-fetch failures also contain the word "scope", so a message probe would
    // wrongly retry here - and a narrower scope succeeding silently would be a nasty bug.
    it('does NOT retry on a non-scope failure, even if the message mentions "scope"', async () => {
        const notFound = Object.assign(
            new Error("Failed to register a ServiceWorker for scope ('/'): bad HTTP response code (404)"),
            { name: 'TypeError' });
        const ctx = page({ scriptAttributes: { scope: '/' } }, { swOptions: { registerError: notFound } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(ctx.registrations).toEqual(['/']);
    });
});

describe('BswupMessage', () => {
    it('exposes every constant previous versions shipped', () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        const m = ctx.window.BswupMessage;
        // Present since before 10.5.0 - removing any of these breaks consumer switch statements.
        expect(m.downloadStarted).toBe('DOWNLOAD_STARTED');
        expect(m.downloadProgress).toBe('DOWNLOAD_PROGRESS');
        expect(m.downloadFinished).toBe('DOWNLOAD_FINISHED');
        expect(m.activate).toBe('ACTIVATE');
        expect(m.updateReady).toBe('UPDATE_READY');
        expect(m.updateFound).toBe('UPDATE_FOUND');
        expect(m.stateChanged).toBe('STATE_CHANGED');
        expect(m.updateInstalled).toBe('UPDATE_INSTALLED'); // deprecated, never raised, kept for compat
    });

    it('adds the newer constants', () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        const m = ctx.window.BswupMessage;
        expect(m.updateNotFound).toBe('UPDATE_NOT_FOUND');
        expect(m.updateCheckFailed).toBe('UPDATE_CHECK_FAILED');
        expect(m.error).toBe('ERROR');
    });
});

describe('forceRefresh', () => {
    async function withCaches(names, arg, hasArg) {
        const ctx = page();
        ctx.load('bit-bswup.js');
        for (const n of names) await ctx.caches.open(n);
        ctx.setServiceWorker({ registration: { unregister: async () => true } });
        await (hasArg ? ctx.window.BitBswup.forceRefresh(arg) : ctx.window.BitBswup.forceRefresh());
        return Object.keys(ctx.caches.snapshot()).sort();
    }

    const ALL = ['bit-bswup - v1', 'blazor-resources-/', 'workbox-precache', 'my-app-offline-data'];

    // App-owned caches can hold data with no other copy; widening the default on upgrade would
    // destroy it unrecoverably.
    it('clears only the Bswup and Blazor caches by default', async () => {
        expect(await withCaches(ALL)).toEqual(['my-app-offline-data', 'workbox-precache']);
    });

    it('clears everything when asked explicitly', async () => {
        expect(await withCaches(ALL, () => true, true)).toEqual([]);
    });

    it('accepts a string prefix', async () => {
        expect(await withCaches(ALL, 'bit-bswup', true))
            .toEqual(['blazor-resources-/', 'my-app-offline-data', 'workbox-precache']);
    });

    it('accepts a RegExp', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        for (const n of ALL) await ctx.caches.open(n);
        ctx.setServiceWorker({ registration: { unregister: async () => true } });
        await ctx.window.BitBswup.forceRefresh(ctx.regex('^my-app'));
        expect(Object.keys(ctx.caches.snapshot()).sort())
            .toEqual(['bit-bswup - v1', 'blazor-resources-/', 'workbox-precache']);
    });
});

describe('idempotency', () => {
    it('a duplicate <script> inclusion does not re-run setup', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();
        const afterFirst = ctx.registrations.length;

        ctx.load('bit-bswup.js'); // stray second tag
        await ctx.settle();
        expect(ctx.registrations.length).toBe(afterFirst);
    });
});

describe('fatal install failure', () => {
    // Install aborted => no active worker => no CLIENTS_CLAIMED handshake => Blazor would never
    // start and the app would hang behind the splash forever.
    it('starts Blazor anyway on a first install so the app still boots', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'manifest', message: 'bad', fatal: true } }));
        await ctx.settle();
        expect(ctx.window.__blazorStarted).toBe(1);
    });

    it('does not start Blazor for a non-fatal (lax) asset failure', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'fetch', message: 'nope', fatal: false } }));
        await ctx.settle();
        expect(ctx.window.__blazorStarted).toBeUndefined();
    });
});

describe('malformed worker messages', () => {
    it('ignores non-JSON payloads instead of throwing', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(() => ctx.message('not json at all')).not.toThrow();
        expect(() => ctx.message(JSON.stringify('a bare string'))).not.toThrow();
    });
});
