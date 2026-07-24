import { describe, it, expect } from 'vitest';
import { createPageContext, ORIGIN } from './harness.js';

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
        // Present since before v-10-5-0 - removing any of these breaks consumer switch statements.
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

    // Cache buckets are scope-qualified (bit-bswup:<scope-path> - <version>) so sibling Bswup
    // apps on one origin have independent caches; a reset of app A must not wipe app B's.
    it('spares a sibling scope bucket when the registration scope is known', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        for (const n of ['bit-bswup - v1', 'bit-bswup:/ - v2', 'bit-bswup:/other/ - v9', 'blazor-resources-/', 'my-app-offline-data']) {
            await ctx.caches.open(n);
        }
        ctx.setServiceWorker({ registration: { scope: `${ORIGIN}/`, unregister: async () => true } });
        await ctx.window.BitBswup.forceRefresh();
        // Own scoped bucket, legacy bucket and Blazor caches cleared; the sibling app's
        // scoped bucket and app-owned data survive.
        expect(Object.keys(ctx.caches.snapshot()).sort())
            .toEqual(['bit-bswup:/other/ - v9', 'my-app-offline-data']);
    });

    it('falls back to clearing every Bswup bucket when the scope is unknown', async () => {
        // The registration fake has no scope - a last-resort reset on a wedged client should
        // then prefer "reset too much" over "reset nothing".
        const ctx = page();
        ctx.load('bit-bswup.js');
        for (const n of ['bit-bswup:/ - v2', 'bit-bswup:/other/ - v9', 'my-app-offline-data']) await ctx.caches.open(n);
        ctx.setServiceWorker({ registration: { unregister: async () => true } });
        await ctx.window.BitBswup.forceRefresh();
        expect(Object.keys(ctx.caches.snapshot())).toEqual(['my-app-offline-data']);
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

    // The error payload must say where the failure landed: the built-in progress UI keys its
    // take-over decision off firstInstall (panel on a first install, stay out of the way when
    // a background update fails and the app is running fine on the previous version).
    it('marks a fatal error on a first install with firstInstall: true', async () => {
        const ctx = page();
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'manifest', message: 'bad', fatal: true } }));
        await ctx.settle();

        const [, data] = seen.find(([message]) => message === 'ERROR');
        expect(data.firstInstall).toBe(true);
    });

    it('marks a fatal error during an update with firstInstall: false', async () => {
        // An active worker at registration time is what makes this an update, not a first install.
        const registration = {
            active: { postMessage() { } },
            installing: null, waiting: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'install-aborted', message: 'bad', fatal: true } }));
        await ctx.settle();

        const [, data] = seen.find(([message]) => message === 'ERROR');
        expect(data.firstInstall).toBe(false);
    });
});

// The worker sends 'bypass' when there is nothing to download (a passive first run, or an
// update whose diff is empty). Its firstInstall routing decides whether the page runs the
// claim-and-start flow or shows the reload button - getting it wrong either reloads a fresh
// install pointlessly or leaves a passive first run waiting on a button nobody should click.
describe('bypass routing', () => {
    function seenOn(registration) {
        const ctx = page({}, registration ? { swOptions: { registration } } : {});
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        return { ctx, seen };
    }
    const activeRegistration = () => ({
        active: { postMessage() { } }, installing: null, waiting: null,
        addEventListener() { }, update: async () => { },
    });

    it('marks a bypass on an uncontrolled fresh page as a first install', async () => {
        const { ctx, seen } = seenOn(null);
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'bypass', data: {} }));
        await ctx.settle();

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(finished.firstInstall).toBe(true);
    });

    it('honors the worker-declared firstTime flag even when a worker was already active', async () => {
        // Passive first run raced against an already-resolved registration: the worker knows
        // best that its cache is empty, so data.firstTime wins over the page-side heuristic.
        const { ctx, seen } = seenOn(activeRegistration());
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'bypass', data: { firstTime: true } }));
        await ctx.settle();

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(finished.firstInstall).toBe(true);
    });

    it('marks a nothing-to-download bypass during an update as NOT a first install', async () => {
        const { ctx, seen } = seenOn(activeRegistration());
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'bypass', data: {} }));
        await ctx.settle();

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(finished.firstInstall).toBe(false);
    });
});

// A fake ServiceWorker whose lifecycle the test drives by hand.
function fakeWorker(state) {
    const listeners = [];
    return {
        state,
        posted: [],
        postMessage(message) { this.posted.push(message); },
        addEventListener(type, fn) { if (type === 'statechange') listeners.push(fn); },
        removeEventListener(type, fn) {
            const index = listeners.indexOf(fn);
            if (index !== -1) listeners.splice(index, 1);
        },
        // Snapshot before dispatch: whenStaged/whenActive remove themselves mid-iteration.
        fireStateChange() { listeners.slice().forEach(fn => fn({ currentTarget: this })); },
    };
}

// The 100% progress message is sent just before the SW's install promise resolves, so when a
// handler calls reload() the new worker can still be 'installing'. reload() must wait for the
// state each flow needs instead of racing the lifecycle (the old code picked between a seamless
// claim, a SKIP_WAITING reload, and a hard reload depending on which instant the message landed).
describe('reload determinism', () => {
    const progress100 = JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } });

    it('first install: waits for activation, then claims - no reload on any path', async () => {
        const installing = fakeWorker('installing');
        const registration = { active: null, waiting: null, installing, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(progress100);
        await ctx.settle();
        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        finished.reload();
        await ctx.settle();

        // Still installing: nothing posted anywhere, and crucially no hard reload (the old
        // code fell through to window.location.reload() exactly here).
        expect(installing.posted).toEqual([]);
        expect(ctx.reloads.count).toBe(0);

        // The worker activates - a first install never stays waiting.
        registration.installing = null;
        registration.active = installing;
        installing.state = 'activated';
        installing.fireStateChange();
        await ctx.settle();

        expect(installing.posted).toEqual(['CLAIM_CLIENTS']);
        expect(ctx.reloads.count).toBe(0);
    });

    it('update: waits for the staged worker instead of claiming the old active one', async () => {
        const oldActive = fakeWorker('activated');
        const installing = fakeWorker('installing');
        const registration = { active: oldActive, waiting: null, installing, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(progress100);
        await ctx.settle();
        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        finished.reload();
        await ctx.settle();

        // The old worker must never receive CLAIM_CLIENTS: its deleteOldCaches() would wipe
        // the freshly installed new cache.
        expect(oldActive.posted).toEqual([]);

        // The new worker finishes installing and is staged.
        registration.installing = null;
        registration.waiting = installing;
        installing.state = 'installed';
        installing.fireStateChange();
        await ctx.settle();

        expect(installing.posted).toEqual(['SKIP_WAITING']);
    });

    it('falls back to a plain reload when no worker is in the pipeline (retry after failure)', async () => {
        const ctx = page();
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'install-aborted', message: 'x', fatal: true } }));
        await ctx.settle();
        const [, err] = seen.find(([message]) => message === 'ERROR');
        err.reload();
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
    });

    it('does not announce updateReady while a first install is staging', async () => {
        const regListeners = {};
        const installing = fakeWorker('installing');
        const registration = {
            active: null, waiting: null, installing,
            addEventListener: (type, fn) => { (regListeners[type] ||= []).push(fn); },
            update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        (regListeners.updatefound || []).forEach(fn => fn({}));
        // The worker reaches 'installed' - transiently the waiting worker on a first install.
        registration.waiting = installing;
        installing.state = 'installed';
        installing.fireStateChange();
        await ctx.settle();

        // Announcing "an update is staged and ready" here made autoReload handlers post
        // SKIP_WAITING against a first install, needlessly reloading the page.
        expect(seen.some(([message]) => message === 'UPDATE_READY')).toBe(false);
        expect(seen.some(([message]) => message === 'STATE_CHANGED')).toBe(true);
    });

    it('still announces updateReady when a real update finishes staging', async () => {
        const regListeners = {};
        const oldActive = fakeWorker('activated');
        const installing = fakeWorker('installing');
        const registration = {
            active: oldActive, waiting: null, installing,
            addEventListener: (type, fn) => { (regListeners[type] ||= []).push(fn); },
            update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        (regListeners.updatefound || []).forEach(fn => fn({}));
        registration.waiting = installing;
        installing.state = 'installed';
        installing.fireStateChange();
        await ctx.settle();

        expect(seen.some(([message]) => message === 'UPDATE_READY')).toBe(true);
    });

    it('WAITING_SKIPPED on a first install starts Blazor in place instead of reloading', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message('WAITING_SKIPPED');
        await ctx.settle();

        expect(ctx.reloads.count).toBe(0);
        expect(ctx.window.__blazorStarted).toBe(1);
    });

    it('WAITING_SKIPPED during an update still reloads exactly once', async () => {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message('WAITING_SKIPPED');
        ctx.message('WAITING_SKIPPED'); // a second signal must not reload again
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
    });

    it('cleanup() posts CLEAN_UP to the active worker only, never the waiting one', async () => {
        // Posting CLEAN_UP at a waiting worker made its deleteOldCaches() - which spares only
        // its own NEW cache - wipe the active worker's live cache out from under this
        // still-running page: old app code fetching new-version bytes corrupts boot config /
        // DLL hashes. The worker refuses that case on its own now, but the page must not ask.
        const active = fakeWorker('activated');
        const waiting = fakeWorker('installed');
        const registration = { active, waiting, installing: null, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(progress100);
        await ctx.settle();
        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        finished.cleanup();

        expect(active.posted).toContain('CLEAN_UP');
        expect(waiting.posted).not.toContain('CLEAN_UP');
    });
});

// An install can die without reporting anything: the browser terminates a worker whose install
// outlives the waitUntil cap, register() can hang, a message channel can drop. On a first
// install the page waits behind the splash for the install to finish before starting Blazor,
// so a silent death would leave the app unbootable forever. The stall watchdog (timer) and the
// redundant-worker handler (statechange) are the last-resort boot guarantees.
describe('first-install stall watchdog', () => {
    // Real watchdog delays are >= 1s and deliberately never fire inside a test (the harness
    // unrefs them); sub-second values take the clamped fast path so a test can see it fire.
    const FIRING = '0.001';

    function installingRegistration(overrides = {}) {
        return {
            active: null, waiting: null, installing: fakeWorker('installing'),
            addEventListener() { }, update: async () => { },
            ...overrides,
        };
    }

    const letTimersFire = () => new Promise(r => setTimeout(r, 30));

    it('starts Blazor after total service-worker silence on a first install', async () => {
        const ctx = page({ scriptAttributes: { stallTimeout: FIRING } },
            { swOptions: { registration: installingRegistration() } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        await letTimersFire();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
        expect(ctx.reloads.count).toBe(0);
    });

    it('is disabled with stallTimeout="0"', async () => {
        const ctx = page({ scriptAttributes: { stallTimeout: '0' } },
            { swOptions: { registration: installingRegistration() } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        await letTimersFire();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBeUndefined();
    });

    it('disarms when the registration reveals a previously-active worker (an update)', async () => {
        // active + installing: an uncontrolled load while an update is staging - the one
        // uncontrolled shape that does not take the hard-reload force-start path, so only the
        // watchdog could start Blazor here, and it must not (the update flow owns this page).
        const ctx = page({ scriptAttributes: { stallTimeout: FIRING } },
            { swOptions: { registration: installingRegistration({ active: fakeWorker('activated') }) } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        await letTimersFire();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBeUndefined();
    });

    it('starts Blazor when the installing worker goes redundant on a first install', async () => {
        const regListeners = {};
        const installing = fakeWorker('installing');
        const registration = installingRegistration({
            installing,
            addEventListener: (type, fn) => { (regListeners[type] ||= []).push(fn); },
        });
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        (regListeners.updatefound || []).forEach(fn => fn({}));
        // The worker dies - e.g. terminated mid-install - with 'redundant' as the only signal.
        registration.installing = null;
        installing.state = 'redundant';
        installing.fireStateChange();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
        expect(ctx.reloads.count).toBe(0);
    });

    it('a claimed-but-never-replied tab starts Blazor on first-install controllerchange', async () => {
        // CLIENTS_CLAIMED goes only to the ONE tab that posted CLAIM_CLIENTS. A sibling tab
        // opened between the 100% broadcast and the claim sees nothing but this
        // controllerchange - previously it returned without starting anything and the tab sat
        // behind the splash until the stall watchdog fired.
        const ctx = page(); // default registration: no active worker => first install
        ctx.load('bit-bswup.js');
        await ctx.settle();

        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
        expect(ctx.reloads.count).toBe(0); // and crucially still no reload on a first install
    });

    it('controllerchange during an update still reloads exactly once and starts nothing', async () => {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();
        // The hard-reload force-start path (uncontrolled + active + not installing) already
        // booted Blazor once here; capture the count before the controller changes.
        const startedBefore = ctx.window.__blazorStarted || 0;

        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));
        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1); // reloadOnce guards the double signal
        expect(ctx.window.__blazorStarted || 0).toBe(startedBefore);
    });

    it('a worker going redundant during an update starts nothing (the app is already running)', async () => {
        const regListeners = {};
        const installing = fakeWorker('installing');
        const registration = installingRegistration({
            active: fakeWorker('activated'),
            installing,
            addEventListener: (type, fn) => { (regListeners[type] ||= []).push(fn); },
        });
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        (regListeners.updatefound || []).forEach(fn => fn({}));
        registration.installing = null;
        installing.state = 'redundant';
        installing.fireStateChange();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBeUndefined();
        expect(ctx.reloads.count).toBe(0);
    });
});

// .NET 9+ templates reference the entry script through @Assets[...] / the ImportMap, which
// emits a fingerprinted name (_framework/blazor.web.<fingerprint>.js). With autostart="false"
// a missed match means Blazor is never started - a dead app behind the splash.
describe('Blazor entry-script detection', () => {
    function pageWithBlazorSrc(src, extra = {}) {
        const ctx = page({}, { blazor: false });
        ctx.addBlazorScriptTag({ src, ...extra });
        return ctx;
    }

    async function started(ctx) {
        ctx.load('bit-bswup.js');
        await ctx.settle();
        ctx.message('START_BLAZOR'); // drives startBlazor(true) directly
        await ctx.settle();
        return ctx.window.__blazorStarted === 1;
    }

    it('detects the fingerprinted Blazor Web App script', async () => {
        expect(await started(pageWithBlazorSrc('_framework/blazor.web.bl98zn3vgs.js'))).toBe(true);
    });

    it('detects the fingerprinted standalone WebAssembly script', async () => {
        expect(await started(pageWithBlazorSrc('_framework/blazor.webassembly.k3j2h1x9.js'))).toBe(true);
    });

    it('still detects the plain script with a query string', async () => {
        expect(await started(pageWithBlazorSrc('_framework/blazor.web.js?v=10.0.0'))).toBe(true);
    });

    it('does not match lookalike names', async () => {
        expect(await started(pageWithBlazorSrc('_framework/blazor.website.js'))).toBe(false);
        expect(await started(pageWithBlazorSrc('_framework/blazor.web.jsx'))).toBe(false);
    });

    it('still requires autostart="false" on a fingerprinted tag', async () => {
        expect(await started(pageWithBlazorSrc('_framework/blazor.web.bl98zn3vgs.js', { autostart: 'true' }))).toBe(false);
    });
});

describe('Blazor.start() rejection', () => {
    it('releases the single-start latch so a later trigger can retry', async () => {
        const ctx = page();
        let attempts = 0;
        ctx.window.Blazor = {
            start: async () => {
                attempts++;
                if (attempts === 1) throw new Error('boot resources failed');
                ctx.window.__blazorStarted = (ctx.window.__blazorStarted || 0) + 1;
            },
        };
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message('START_BLAZOR'); // first attempt rejects
        await ctx.settle();
        expect(attempts).toBe(1);
        expect(ctx.window.__blazorStarted).toBeUndefined();

        // Previously blazorStarted stayed stuck at true here, so this returned the same dead
        // promise and Blazor.start was never reached again.
        ctx.message('START_BLAZOR');
        await ctx.settle();
        expect(attempts).toBe(2);
        expect(ctx.window.__blazorStarted).toBe(1);
    });

    it('still never double-starts after a successful start', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message('START_BLAZOR');
        ctx.message('START_BLAZOR');
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
    });
});

// Best-effort storage can be evicted (disk pressure, Safari's 7-day rule), wiping the whole
// offline story. persist() is a Window-only API, so the page script owns the request.
describe('persistent storage', () => {
    function withStorage(ctx, { persisted = false, grant = true } = {}) {
        const calls = { persisted: 0, persist: 0 };
        ctx.window.navigator.storage = {
            persisted: async () => { calls.persisted++; return persisted; },
            persist: async () => { calls.persist++; return grant; },
        };
        return calls;
    }

    it('requests persistence at startup when persistStorage="true"', async () => {
        const ctx = page({ scriptAttributes: { persistStorage: 'true' } });
        const calls = withStorage(ctx);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        expect(calls.persist).toBe(1);
    });

    it('does not request persistence by default', async () => {
        const ctx = page();
        const calls = withStorage(ctx);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        expect(calls.persist).toBe(0);
    });

    it('BitBswup.persistStorage() skips the request when already persistent', async () => {
        const ctx = page();
        const calls = withStorage(ctx, { persisted: true });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        expect(await ctx.window.BitBswup.persistStorage()).toBe(true);
        expect(calls.persist).toBe(0);
    });

    it('BitBswup.persistStorage() resolves false gracefully when unsupported', async () => {
        const ctx = page(); // harness navigator has no storage object at all
        ctx.load('bit-bswup.js');
        await ctx.settle();

        expect(await ctx.window.BitBswup.persistStorage()).toBe(false);
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
