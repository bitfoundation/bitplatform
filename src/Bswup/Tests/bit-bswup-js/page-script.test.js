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

    it('WAITING_SKIPPED during an update reloads a controlled page exactly once', async () => {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration, controller: {} } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message('WAITING_SKIPPED');
        ctx.message('WAITING_SKIPPED'); // a second signal must not reload again
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
    });

    it('WAITING_SKIPPED never reloads an uncontrolled page - it boots it instead', async () => {
        // Reaches uncontrolled tabs too: a hard-reloaded page during an update, or an SW-free
        // page during a cleanup-worker teardown. Reloading those only discards in-page state;
        // they must simply be booted (idempotent when already running). The registration has
        // BOTH active and installing set so the load-time hard-reload force-start is skipped
        // and the boot half of this assertion is genuinely pinned on WAITING_SKIPPED.
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: fakeWorker('installing'),
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } }); // controller stays null
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(ctx.window.__blazorStarted).toBeUndefined(); // nothing booted the page yet

        ctx.message('WAITING_SKIPPED');
        await ctx.settle();

        expect(ctx.reloads.count).toBe(0);
        expect(ctx.window.__blazorStarted).toBe(1); // booted by WAITING_SKIPPED, not reloaded
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

// The first-install/update discriminator must FLIP once the first install completes and takes
// control of the page. Without the flip, a long-lived tab whose session began with the first
// install misclassified every later real update as another first install: updateReady was
// suppressed, downloadFinished said firstInstall: true (whose auto-reload posted CLAIM_CLIENTS
// at the OLD worker - wiping the freshly staged cache), and a sibling tab's update claim
// skipped the mandatory reload, running old app code against new-version caches.
describe('updates after a completed first install', () => {
    const progress100 = JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } });

    function firstInstallPage() {
        const regListeners = {};
        const registration = {
            active: null, waiting: null, installing: null,
            addEventListener: (t, fn) => { (regListeners[t] ||= []).push(fn); },
            update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        return { ctx, seen, registration, regListeners };
    }

    async function completeFirstInstall(ctx) {
        await ctx.settle();
        ctx.message('CLIENTS_CLAIMED'); // the claim that completes a first install
        await ctx.settle();
    }

    it('announces updateReady for an update staged later in the same session', async () => {
        const { ctx, seen, registration, regListeners } = firstInstallPage();
        await completeFirstInstall(ctx);

        // A real update is found and staged (e.g. via update polling).
        const staged = fakeWorker('installing');
        registration.installing = staged;
        (regListeners.updatefound || []).forEach(fn => fn({}));
        registration.installing = null;
        registration.waiting = staged;
        staged.state = 'installed';
        staged.fireStateChange();
        await ctx.settle();

        // Previously this logged "initialization finished." and told nobody anything.
        expect(seen.some(([message]) => message === 'UPDATE_READY')).toBe(true);
    });

    it('labels downloadFinished after the flip as an update, not a first install', async () => {
        const { ctx, seen } = firstInstallPage();
        await completeFirstInstall(ctx);

        ctx.message(progress100);
        await ctx.settle();

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(finished.firstInstall).toBe(false);
    });

    it('reload() after the flip drives SKIP_WAITING at the staged worker, never CLAIM_CLIENTS at the old one', async () => {
        const { ctx, seen, registration } = firstInstallPage();
        await completeFirstInstall(ctx);

        const oldActive = fakeWorker('activated');
        const staged = fakeWorker('installed');
        registration.active = oldActive;
        registration.waiting = staged;

        ctx.message(progress100);
        await ctx.settle();
        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        finished.reload();
        await ctx.settle();

        // CLAIM_CLIENTS at the old active worker would run its deleteOldCaches() and wipe the
        // staged update's freshly downloaded bucket.
        expect(oldActive.posted).toEqual([]);
        expect(staged.posted).toEqual(['SKIP_WAITING']);
    });

    it('a sibling tab accepting an update reloads this tab instead of restarting Blazor', async () => {
        const { ctx } = firstInstallPage();
        await completeFirstInstall(ctx);
        expect(ctx.window.__blazorStarted).toBe(1);

        // The sibling's worker claimed every client: this tab now runs OLD app code against
        // NEW caches and must resync via reload - the start-Blazor no-op path leaves it mixed.
        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
        expect(ctx.window.__blazorStarted).toBe(1); // no second start
    });

    it('progress before the claim still reports firstInstall: true', async () => {
        const { ctx, seen } = firstInstallPage();
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 40, index: 1, asset: { url: 'a.js' } } }));
        await ctx.settle();

        const [, data] = seen.find(([message]) => message === 'DOWNLOAD_PROGRESS');
        expect(data.firstInstall).toBe(true);
        expect(data.percent).toBe(40);
    });
});

// With no handler registered at all, nothing would ever call reload() on downloadFinished -
// the CLAIM_CLIENTS handshake never ran and a first install sat behind the splash until the
// stall watchdog fired a minute later.
describe('no progress handler registered', () => {
    const progress100 = JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } });

    it('completes a first install by driving reload() itself', async () => {
        const installing = fakeWorker('installing');
        const registration = { active: null, waiting: null, installing, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } }); // deliberately no bitBswupHandler anywhere
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(progress100);
        await ctx.settle();

        // The auto-driven reload() waits for activation like any handler-driven one.
        registration.installing = null;
        registration.active = installing;
        installing.state = 'activated';
        installing.fireStateChange();
        await ctx.settle();

        expect(installing.posted).toEqual(['CLAIM_CLIENTS']);
        expect(ctx.reloads.count).toBe(0);
    });

    it('leaves an update staged instead of force-activating it', async () => {
        const active = fakeWorker('activated');
        const staged = fakeWorker('installed');
        const registration = { active, waiting: staged, installing: null, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        ctx.message(progress100);
        await ctx.settle();

        // Auto-activating would force a reload the app never consented to; an unhandled
        // update simply waits, exactly like a handler that ignores updateReady.
        expect(staged.posted).toEqual([]);
        expect(ctx.reloads.count).toBe(0);
    });
});

// The cleanup worker's exit path: a controlled page detaches via one reload; an uncontrolled
// page is already running SW-free, and reloading it would re-register the cleanup worker and
// spin an infinite register/activate/UNREGISTER/reload loop.
describe('UNREGISTER handling', () => {
    function unregisterPage({ controlled }) {
        let unregistered = 0;
        const registration = {
            active: null, waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
            unregister: async () => { unregistered++; return true; },
        };
        const ctx = page({}, { swOptions: { registration, controller: controlled ? {} : null } });
        ctx.load('bit-bswup.js');
        return { ctx, count: () => unregistered };
    }

    it('unregisters and reloads exactly once when the page is controlled', async () => {
        const { ctx, count } = unregisterPage({ controlled: true });
        await ctx.settle();

        ctx.message('UNREGISTER');
        ctx.message('UNREGISTER'); // a duplicate signal must not double-reload
        await ctx.settle();

        expect(count()).toBe(2); // unregister is idempotent and safe to repeat
        expect(ctx.reloads.count).toBe(1); // reloadOnce coordinates with controllerchange
    });

    it('unregisters without reloading when the page is uncontrolled - and boots the app instead', async () => {
        const { ctx, count } = unregisterPage({ controlled: false });
        await ctx.settle();

        ctx.message('UNREGISTER');
        await ctx.settle();

        expect(count()).toBe(1);
        expect(ctx.reloads.count).toBe(0); // no reload => no re-register loop
        // While a cleanup worker is deployed, this teardown signal is the earliest moment an
        // uncontrolled page knows no install is coming - booting here keeps startup instant
        // for the whole cleanup-deployment window instead of waiting for the delayed nudge.
        expect(ctx.window.__blazorStarted).toBe(1);
    });
});

// The discriminator seed must be live BEFORE the registration resolves: the controllerchange
// and message listeners attach several tasks earlier, and a sibling tab accepting an update
// in that window used to be misclassified as a first-install claim on this controlled page -
// skipping the mandatory resync reload (old app code kept running against new caches).
describe('discriminator before the registration resolves', () => {
    it('a controlled page reloads on a controllerchange that lands before register() settles', async () => {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration, controller: {} } });
        ctx.load('bit-bswup.js');
        // Deliberately NO settle(): the claim arrives while register() is still pending.
        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));

        expect(ctx.reloads.count).toBe(1);
    });

    it('worker messages before the registration resolves are stamped from the controller', async () => {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration, controller: {} } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        // Again no settle(): the message races the registration promise.
        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 10, index: 1, asset: { url: 'a.js' } } }));

        const [, data] = seen.find(([message]) => message === 'DOWNLOAD_PROGRESS');
        expect(data.firstInstall).toBe(false);
    });
});

// A page that loads while an update is already mid-install: updatefound fired before this
// page existed, so the load-time path must watch the in-flight worker itself - previously
// nothing did, and updateReady/stateChanged were silently lost in that tab.
describe('an install already in flight at page load', () => {
    it('announces updateReady when the in-flight update finishes staging', async () => {
        const staged = fakeWorker('installing');
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: staged,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        registration.installing = null;
        registration.waiting = staged;
        staged.state = 'installed';
        staged.fireStateChange();
        await ctx.settle();

        expect(seen.some(([message]) => message === 'UPDATE_READY')).toBe(true);
        expect(seen.some(([message]) => message === 'STATE_CHANGED')).toBe(true);
    });

    it('rescues a first install whose in-flight worker dies before any updatefound', async () => {
        const installing = fakeWorker('installing');
        const registration = {
            active: null, waiting: null, installing,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        ctx.load('bit-bswup.js');
        await ctx.settle();

        registration.installing = null;
        installing.state = 'redundant';
        installing.fireStateChange();
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
    });
});

describe('updateReady for an update already staged at page load', () => {
    it('is announced with a working reload callback', async () => {
        const staged = fakeWorker('installed');
        const registration = {
            active: fakeWorker('activated'), waiting: staged, installing: null,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        await ctx.settle();

        const found = seen.find(([message]) => message === 'UPDATE_READY');
        expect(found).toBeTruthy();

        found[1].reload();
        await ctx.settle();
        expect(staged.posted).toEqual(['SKIP_WAITING']);
    });
});

// BLAZOR_STARTED is the sole trigger of the worker's post-start cache top-up - in passive
// mode that top-up IS the offline story, so the reply must go back on every path.
describe('CLIENTS_CLAIMED handshake', () => {
    it('replies BLAZOR_STARTED to the claiming worker once Blazor has started', async () => {
        const ctx = page();
        ctx.load('bit-bswup.js');
        await ctx.settle();

        const sourcePosted = [];
        ctx.message('CLIENTS_CLAIMED', { postMessage: m => sourcePosted.push(m) });
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
        expect(sourcePosted).toEqual(['BLAZOR_STARTED']);
    });

    it('still replies BLAZOR_STARTED when Blazor.start() rejects', async () => {
        const ctx = page();
        ctx.window.Blazor = { start: async () => { throw new Error('boot failed'); } };
        ctx.load('bit-bswup.js');
        await ctx.settle();

        const sourcePosted = [];
        ctx.message('CLIENTS_CLAIMED', { postMessage: m => sourcePosted.push(m) });
        await ctx.settle();

        // The worker's top-up must proceed either way, and the first-install reload()
        // promise must not hang the splash.
        expect(sourcePosted).toEqual(['BLAZOR_STARTED']);
    });
});

describe('checkForUpdate outcomes', () => {
    function checkPage(updateImpl) {
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: updateImpl,
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        return { ctx, seen, registration };
    }

    const letTimersFire = () => new Promise(r => setTimeout(r, 20));

    it('emits updateNotFound when the check finds nothing new', async () => {
        const { ctx, seen } = checkPage(async () => { });
        await ctx.settle();

        await ctx.window.BitBswup.checkForUpdate();
        await letTimersFire();
        await ctx.settle();

        expect(seen.some(([message]) => message === 'UPDATE_NOT_FOUND')).toBe(true);
    });

    it('does not emit updateNotFound when a new worker is already staging', async () => {
        const { ctx, seen, registration } = checkPage(async () => {
            registration.installing = fakeWorker('installing');
        });
        await ctx.settle();

        await ctx.window.BitBswup.checkForUpdate();
        await letTimersFire();
        await ctx.settle();

        expect(seen.some(([message]) => message === 'UPDATE_NOT_FOUND')).toBe(false);
    });

    it('emits the non-blocking updateCheckFailed - not the install-path error - on a failed check', async () => {
        const { ctx, seen } = checkPage(async () => { throw new Error('offline'); });
        await ctx.settle();

        await ctx.window.BitBswup.checkForUpdate();
        await ctx.settle();

        // The install-path 'error' would make the built-in UI hide a healthy running app.
        expect(seen.some(([message]) => message === 'UPDATE_CHECK_FAILED')).toBe(true);
        expect(seen.some(([message]) => message === 'ERROR')).toBe(false);
    });
});

describe('update polling', () => {
    function pollingPage(attrs) {
        let updates = 0;
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener() { }, update: async () => { updates++; },
        };
        const ctx = page({ scriptAttributes: attrs }, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        return { ctx, seen, count: () => updates };
    }

    it('registers a timer from updateInterval (seconds) and checks on each tick', async () => {
        const { ctx, count } = pollingPage({ updateInterval: '300' });
        await ctx.settle();

        expect(ctx.intervals).toHaveLength(1);
        expect(ctx.intervals[0].ms).toBe(300 * 1000);

        ctx.tickIntervals();
        await ctx.settle();
        expect(count()).toBe(1);
    });

    it('skips ticks while the tab is hidden', async () => {
        const { ctx, count } = pollingPage({ updateInterval: '300' });
        await ctx.settle();

        ctx.window.document.visibilityState = 'hidden';
        ctx.tickIntervals();
        await ctx.settle();
        expect(count()).toBe(0);
    });

    it('checks when the tab becomes visible with updateOnVisibility', async () => {
        const { ctx, count } = pollingPage({ updateOnVisibility: 'true' });
        await ctx.settle();

        ctx.fire('visibilitychange');
        await ctx.settle();
        expect(count()).toBe(1);
    });

    it('cleanup() stops the poll timer and the visibility listener', async () => {
        const { ctx, seen, count } = pollingPage({ updateInterval: '300', updateOnVisibility: 'true' });
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } }));
        await ctx.settle();
        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        finished.cleanup();

        ctx.tickIntervals();
        ctx.fire('visibilitychange');
        await ctx.settle();

        expect(count()).toBe(0); // previously the timer polled for the life of the page
    });
});

describe('options object configuration', () => {
    it('merges a partial window.bitBswup over the defaults without clobbering them', async () => {
        const ctx = createPageContext();
        ctx.addBswupScriptTag(); // no attributes - everything comes from the object + defaults
        ctx.addBlazorScriptTag();
        ctx.window.Blazor = { start: async () => { } };
        ctx.setServiceWorker({});
        ctx.window.bitBswup = { log: 'error' }; // partial: must not blank scope/handlerName/sw
        ctx.load('bit-bswup.js');
        await ctx.settle();

        // The default scope survived the merge (a partial object used to blank it) ...
        expect(ctx.registrations).toEqual(['/']);

        // ... and so did the default handler name: a late-registered bitBswupHandler still
        // receives messages.
        const seen = [];
        ctx.window.bitBswupHandler = (message) => seen.push(message);
        ctx.message(JSON.stringify({ type: 'activate', data: { version: 'v2' } }));
        expect(seen).toContain('ACTIVATE');
    });
});

// A secondary tab opened during a first install receives the worker's broadcasts before its
// own register() promise can resolve (register jobs for one scope serialize behind the
// running install job). downloadFinished used to dispatch with reload === undefined there:
// the built-in splash teardown threw and the splash froze at 100% over the booted app.
describe('messages arriving before the registration resolves', () => {
    it('carry a working reload that completes once the claim has booted the app', async () => {
        const registration = { active: null, waiting: null, installing: null, addEventListener() { }, update: async () => { } };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        // Deliberately NO settle: the 100% broadcast lands while register() is still pending.
        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } }));

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(typeof finished.reload).toBe('function');
        expect(typeof finished.cleanup).toBe('function');

        const reloadPromise = finished.reload();
        // The initiating tab's claim reaches this tab as a controllerchange and boots it;
        // in a real browser the claim also sets navigator.serviceWorker.controller.
        ctx.window.navigator.serviceWorker.controller = {};
        (ctx.swListeners.controllerchange || []).forEach(fn => fn({}));
        await ctx.settle();

        let resolved = false;
        reloadPromise.then(() => { resolved = true; });
        await ctx.settle();

        expect(ctx.window.__blazorStarted).toBe(1);
        expect(resolved).toBe(true);          // the splash teardown can run
        expect(ctx.reloads.count).toBe(0);    // and no pointless reload happened
    });
});

// updateReady must fire exactly once per staged worker: statechange listeners outlive the
// waiting slot, so a superseded staged worker's 'redundant' event fires while a NEWER,
// already-announced worker occupies the slot.
describe('updateReady announcement dedupe', () => {
    function updatePage() {
        const regListeners = {};
        const registration = {
            active: fakeWorker('activated'), waiting: null, installing: null,
            addEventListener: (t, fn) => { (regListeners[t] ||= []).push(fn); },
            update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message) => seen.push(message);
        ctx.load('bit-bswup.js');
        return { ctx, seen, registration, regListeners };
    }

    it('a superseded staged worker going redundant does not re-announce the newer one', async () => {
        const { ctx, seen, registration, regListeners } = updatePage();
        await ctx.settle();

        // Update B stages and is announced.
        const b = fakeWorker('installing');
        registration.installing = b;
        (regListeners.updatefound || []).forEach(fn => fn({}));
        registration.installing = null;
        registration.waiting = b;
        b.state = 'installed';
        b.fireStateChange();
        await ctx.settle();

        // Update C stages, replacing B; then B's redundant statechange fires late.
        const c = fakeWorker('installing');
        registration.installing = c;
        (regListeners.updatefound || []).forEach(fn => fn({}));
        registration.installing = null;
        registration.waiting = c;
        c.state = 'installed';
        c.fireStateChange();
        b.state = 'redundant';
        b.fireStateChange();
        await ctx.settle();

        expect(seen.filter(message => message === 'UPDATE_READY')).toHaveLength(2); // once for B, once for C
    });

    it('still announces an un-announced staged update when a newer install dies', async () => {
        // Staged-at-load B (deferred while C installs) must be announced when C goes
        // redundant - the one 'redundant' path that legitimately emits updateReady.
        const b = fakeWorker('installed');
        const c = fakeWorker('installing');
        const registration = {
            active: fakeWorker('activated'), waiting: b, installing: c,
            addEventListener() { }, update: async () => { },
        };
        const ctx = page({}, { swOptions: { registration } });
        const seen = [];
        ctx.window.bitBswupHandler = (message) => seen.push(message);
        ctx.load('bit-bswup.js');
        await ctx.settle();
        expect(seen.filter(message => message === 'UPDATE_READY')).toHaveLength(0); // deferred

        registration.installing = null;
        c.state = 'redundant';
        c.fireStateChange();
        await ctx.settle();

        expect(seen.filter(message => message === 'UPDATE_READY')).toHaveLength(1); // B, first time
    });
});

// When register() fails for good, the interim reload/cleanup must terminate rather than wait
// forever on a registration that will never come.
describe('interim reload after a failed registration', () => {
    it('falls back to a full reload (the only remaining retry) instead of hanging', async () => {
        const notFound = Object.assign(new Error('bad HTTP response code (404)'), { name: 'TypeError' });
        const ctx = page({}, { swOptions: { registerError: notFound } });
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        // A sibling tab's install broadcasts reach this tab despite its failed register().
        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } }));
        await ctx.settle();

        const [, finished] = seen.find(([message]) => message === 'DOWNLOAD_FINISHED');
        expect(typeof finished.reload).toBe('function');
        finished.reload();
        expect(() => finished.cleanup()).not.toThrow();
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
    });
});

// A Retry button wired to the interim reload must stay functional after a fatal
// first-install failure force-started the app: "running with nothing staged" alone is not
// proof of a completed claim (the controller check is what separates the two states).
describe('interim reload after a fatal first-install failure', () => {
    it('hard-reloads to retry the install instead of resolving into a dead button', async () => {
        const ctx = page(); // fresh first install, uncontrolled
        const seen = [];
        ctx.window.bitBswupHandler = (message, data) => seen.push([message, data]);
        ctx.load('bit-bswup.js');
        // The fatal error lands before this tab's registration resolves.
        ctx.message(JSON.stringify({ type: 'error', data: { reason: 'install-aborted', message: 'x', fatal: true } }));
        await ctx.settle();
        expect(ctx.window.__blazorStarted).toBe(1); // force-started, uncontrolled

        const [, err] = seen.find(([message]) => message === 'ERROR');
        err.reload(); // the Retry button
        await ctx.settle();

        expect(ctx.reloads.count).toBe(1);
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
