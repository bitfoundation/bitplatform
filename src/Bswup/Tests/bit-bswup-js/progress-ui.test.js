import { describe, it, expect } from 'vitest';
import { createPageContext, waitFor, fakeWorker } from './harness.js';

const SPLASH = { 'data-bit-bswup-config': 'true' };

/**
 * Loads bit-bswup.js (which defines the shared BswupMessage constants) and then
 * bit-bswup.progress.js, the way a host page references both.
 */
function progressPage({ elements = {}, readyState = 'complete', clampLongTimers = false } = {}) {
    const ctx = createPageContext({ elements, readyState, clampLongTimers });
    ctx.addBswupScriptTag();
    ctx.addBlazorScriptTag();
    ctx.window.Blazor = { start: async () => { } };
    ctx.setServiceWorker();
    ctx.load('bit-bswup.js');
    ctx.load('bit-bswup.progress.js');
    return ctx;
}

const fullSplash = {
    'bit-bswup': SPLASH,
    'bit-bswup-progress-bar': {},
    'bit-bswup-percent': {},
    'bit-bswup-reload': {},
    'bit-bswup-reload-status': {},
    'bit-bswup-error': {},
    'bit-bswup-error-message': {},
    'bit-bswup-error-details': {},
    'bit-bswup-error-retry': {},
};

// AutoReload defaults to true - a finished update activates itself - so the manual reload
// button only ever appears for an app that opted out, or as a stall/rejection fallback. The
// tests that exercise the button as the primary path opt out the way such an app does.
const manualReloadSplash = {
    ...fullSplash,
    'bit-bswup': { ...SPLASH, 'data-bit-bswup-auto-reload': 'false' },
};

describe('self-initialization from data-* attributes', () => {
    // The component no longer emits an inline <script> (blocked by a strict CSP, and not
    // executed at all when added by an interactive Blazor renderer).
    it('installs window.bitBswupHandler when the config element is present', () => {
        const ctx = progressPage({ elements: { 'bit-bswup': SPLASH } });
        expect(typeof ctx.window.bitBswupHandler).toBe('function');
        expect(ctx.elements['bit-bswup'].getAttribute('data-bit-bswup-initialized')).toBe('true');
    });

    it('initializes an element that appears after load (interactive render)', () => {
        const ctx = progressPage();
        expect(ctx.window.bitBswupHandler).toBeUndefined();

        const el = ctx.addElement('bit-bswup', SPLASH);
        expect(typeof ctx.window.bitBswupHandler).toBe('function');
        expect(el.getAttribute('data-bit-bswup-initialized')).toBe('true');
    });

    it('ignores markup it does not own (no config attribute)', () => {
        const ctx = progressPage({ elements: { 'bit-bswup': {} } });
        expect(ctx.window.bitBswupHandler).toBeUndefined();
    });
});

describe('MutationObserver lifetime', () => {
    // Left attached, this observes the whole documentElement subtree for the life of a Blazor
    // app, which mutates the DOM continuously.
    const observing = ctx => ctx.observers.some(o => o.observing);

    it('detaches once initialized', () => {
        const ctx = progressPage({ elements: { 'bit-bswup': SPLASH } });
        expect(observing(ctx)).toBe(false);
    });

    it('detaches after initializing a late-rendered element', () => {
        const ctx = progressPage();
        ctx.addElement('bit-bswup', SPLASH);
        expect(observing(ctx)).toBe(false);
    });

    it('detaches when the element is markup we do not own', () => {
        const ctx = progressPage();
        ctx.addElement('bit-bswup', {}); // no config attribute; autoStart will never act on it
        expect(observing(ctx)).toBe(false);
    });

    it('gives up after the timeout when the element never appears', async () => {
        // clampLongTimers lets the 60s OBSERVE_TIMEOUT fire at the harness's ~5ms clamp.
        const ctx = progressPage({ clampLongTimers: true });
        expect(observing(ctx)).toBe(true); // still waiting
        await waitFor(() => !observing(ctx), 'the observer to disconnect on timeout');
        expect(observing(ctx)).toBe(false);
    });
});

describe('download progress rendering', () => {
    it('updates the bar, percentage and aria-valuenow', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 42.4, index: 3, asset: { url: 'a.js', hash: 'h' } });

        expect(ctx.elements['bit-bswup-progress-bar'].style.width).toBe('42%');
        expect(ctx.elements['bit-bswup-progress-bar'].getAttribute('aria-valuenow')).toBe('42');
        expect(ctx.elements['bit-bswup-percent'].textContent).toBe('42%');
        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });

    it('tolerates a progress message with no asset', () => {
        const ctx = progressPage({ elements: fullSplash });
        expect(() => ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 100, index: 0, asset: null }))
            .not.toThrow();
    });

    // A non-finite percent must never reach the DOM as "NaN": aria-valuenow="NaN" is invalid ARIA
    // (assistive tech mis-announces it) and "NaN%" is visible garbage. It clamps to a valid 0-100.
    it('clamps a non-finite percent to 0 instead of emitting NaN', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: undefined, index: 1, asset: { url: 'a.js', hash: 'h' } });

        expect(ctx.elements['bit-bswup-progress-bar'].getAttribute('aria-valuenow')).toBe('0');
        expect(ctx.elements['bit-bswup-percent'].textContent).toBe('0%');
    });

    it('clamps an out-of-range percent to 100', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 250, index: 1, asset: { url: 'a.js', hash: 'h' } });

        expect(ctx.elements['bit-bswup-progress-bar'].getAttribute('aria-valuenow')).toBe('100');
    });
});

describe('error handling', () => {
    it('leaves the splash alone for a non-fatal (lax) failure', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 50, index: 1, asset: { url: 'a.js' } });
        ctx.window.bitBswupHandler('ERROR', { reason: 'fetch', message: 'optional asset 404', fatal: false });

        // The install is still running and will succeed - showing "Update failed to install"
        // here would be a lie.
        expect(ctx.elements['bit-bswup-error'].style.display).not.toBe('block');
        expect(ctx.elements['bit-bswup-percent'].style.display).not.toBe('none');
    });

    it('shows the failure panel for a fatal error', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('ERROR', { reason: 'install-aborted', message: 'install aborted', fatal: true });

        expect(ctx.elements['bit-bswup-error'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup-error-message'].textContent).toContain('install aborted');
        // A failed install must not leave a button offering to activate it.
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('none');
    });

    // A stale partial value ("47%", half a list of assets) left sitting next to the failure
    // message reads as an install still in flight. The bar is hidden through its wrapper -
    // the <div class="bit-bswup-progress"> the markup nests it in - not on the bar itself.
    it('hides the stale progress bar, percentage and asset list beside the failure', () => {
        const ctx = progressPage({ elements: { ...fullSplash, 'bit-bswup-assets': {} } });
        const barWrapper = ctx.window.document.createElement('div');
        barWrapper.append(ctx.elements['bit-bswup-progress-bar']);

        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 47, index: 1, asset: { url: 'a.js', hash: 'h' } });
        ctx.window.bitBswupHandler('ERROR', { reason: 'fetch', message: 'download failed', fatal: true });

        expect(barWrapper.style.display).toBe('none');
        expect(ctx.elements['bit-bswup-percent'].style.display).toBe('none');
        expect(ctx.elements['bit-bswup-assets'].style.display).toBe('none');
    });

    it('clears the download splash instead of hijacking a running app when an update fails', () => {
        const ctx = progressPage({ elements: fullSplash });
        // An update download was underway - the splash is visible mid-progress.
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 47, index: 9, asset: { url: 'a.js' } });
        expect(ctx.elements['bit-bswup'].style.display).toBe('block');

        ctx.window.bitBswupHandler('ERROR', { reason: 'install-aborted', message: 'update failed', fatal: true, firstInstall: false });

        // The app keeps running on the previous version: no failure panel, the splash is not
        // left frozen at 47%, and no button invites the user to activate the failed update.
        expect(ctx.elements['bit-bswup-error'].style.display).not.toBe('block');
        expect(ctx.elements['bit-bswup'].style.display).toBe('none');
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('none');
    });

    it('still shows the failure panel when a first install fails (firstInstall: true)', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('ERROR', { reason: 'install-aborted', message: 'first install failed', fatal: true, firstInstall: true });
        expect(ctx.elements['bit-bswup-error'].style.display).toBe('block');
    });

    it('hides Retry for deterministic failures that a reload cannot fix', () => {
        for (const reason of ['manifest', 'integrity', 'install-incomplete', 'install-infra']) {
            const ctx = progressPage({ elements: fullSplash });
            ctx.window.bitBswupHandler('ERROR', { reason, message: 'not retriable', fatal: true });
            expect(ctx.elements['bit-bswup-error-retry'].style.display).toBe('none');
        }
    });

    it('offers Retry for a transient failure', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('ERROR', { reason: 'fetch', message: 'network blip', fatal: true });
        expect(ctx.elements['bit-bswup-error-retry'].style.display).toBe('inline-block');
    });
});

describe('update ready', () => {
    // The attribute-less fallback in autoStart() must track BswupProgress.razor's AutoReload
    // default, or hand-written config markup silently behaves differently from the component.
    it('defaults to an automatic reload', () => {
        const ctx = progressPage({ elements: fullSplash }); // no auto-reload attribute anywhere

        let reloaded = 0;
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => { reloaded++; return Promise.resolve(); } });

        expect(reloaded).toBe(1);
    });

    it('honors an explicit data-bit-bswup-auto-reload="false"', () => {
        const ctx = progressPage({ elements: manualReloadSplash });

        let reloaded = 0;
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => { reloaded++; return Promise.resolve(); } });

        expect(reloaded).toBe(0);
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
    });

    it('wires the reload button when autoReload is off', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.BitBswupProgress.config({ autoReload: false });

        let reloaded = 0;
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => { reloaded++; return Promise.resolve(); } });

        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
        ctx.elements['bit-bswup-reload'].onclick();
        expect(reloaded).toBe(1);
    });

    it('falls back to a manual button if an auto-reload rejects', async () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.BitBswupProgress.config({ autoReload: true });

        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => Promise.reject(new Error('nope')) });
        await ctx.settle();
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
    });

    // The update path's reload() NEVER settles (the page is about to navigate), so a silently
    // stalled skipWaiting - WAITING_SKIPPED / controllerchange never arriving - cannot surface
    // through reload() rejecting. A fallback timer must reveal the manual button anyway, so an
    // autoReload user is never left stuck one version behind with no prompt.
    it('surfaces the manual button if an auto-reload stalls without navigating', async () => {
        const ctx = progressPage({ elements: fullSplash, clampLongTimers: true });
        ctx.window.BitBswupProgress.config({ autoReload: true });

        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => new Promise(() => { }) }); // never settles

        // Not shown synchronously - the fallback timer is still pending.
        expect(ctx.elements['bit-bswup-reload'].style.display).not.toBe('block');
        // clampLongTimers fires the 10s AUTO_RELOAD_FALLBACK_MS at the harness's ~5ms clamp.
        await waitFor(() => ctx.elements['bit-bswup-reload'].style.display === 'block',
            'the fallback timer to reveal the reload button');
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
    });
});

// The first-install reload() promise resolves only once Blazor.start() fully settles, and in
// passive mode that start downloads the whole runtime from the network - easily outlasting
// the 10s fallback grace period on a slow connection. firstInstallClaimed is the signal that
// the CLAIM_CLIENTS handshake finished and the pending reload() now tracks a boot in flight:
// it must stand the fallback down (no spurious "update ready" button or screen-reader
// announcement mid-boot), while a handshake that truly stalls - which never raises the
// message - must still surface the button, whose re-invocation of reload() is a genuine retry.
describe('first-install claim cancels the stalled-reload fallback', () => {
    it('does not surface the reload button when the claim lands and boot is merely slow', async () => {
        const ctx = progressPage({ elements: fullSplash, clampLongTimers: true });

        let finishBoot;
        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: true, reload: () => new Promise(r => { finishBoot = r; }) });
        ctx.window.bitBswupHandler('FIRST_INSTALL_CLAIMED');

        // Give the (clamped) fallback timer ample time to fire were it still armed.
        await new Promise(r => setTimeout(r, 30));
        expect(ctx.elements['bit-bswup-reload'].style.display).not.toBe('block');
        expect(ctx.elements['bit-bswup-reload-status'].textContent).toBe('');

        // The splash still tears down when the boot completes.
        finishBoot();
        await ctx.settle();
        expect(ctx.elements['bit-bswup'].style.display).toBe('none');
    });

    it('still surfaces the reload button when the claim never lands (stalled handshake)', async () => {
        const ctx = progressPage({ elements: fullSplash, clampLongTimers: true });

        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: true, reload: () => new Promise(() => { }) });

        await waitFor(() => ctx.elements['bit-bswup-reload'].style.display === 'block',
            'the fallback timer to reveal the reload button');
        expect(ctx.elements['bit-bswup-reload-status'].textContent).not.toBe('');
    });

    it('still surfaces the reload button when reload() rejects after the claim', async () => {
        const ctx = progressPage({ elements: fullSplash, clampLongTimers: true });

        let failBoot;
        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: true, reload: () => new Promise((_, reject) => { failBoot = reject; }) });
        ctx.window.bitBswupHandler('FIRST_INSTALL_CLAIMED');

        // A rejection is a reported failure, not a slow boot - the cancelled timer must not
        // swallow the recovery path.
        failBoot(new Error('boot failed'));
        await ctx.settle();
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
    });
});

describe('custom handler', () => {
    it('runs after the built-in one, and its errors cannot break the splash', () => {
        const ctx = progressPage({ elements: fullSplash });
        const seen = [];
        ctx.window.myHandler = (message) => { seen.push(message); throw new Error('handler blew up'); };
        // Re-init against a handler name, as the component's data-bit-bswup-handler does.
        ctx.window.BitBswupProgress.start(true, false, false, '#app', false, false, 'myHandler');

        expect(() => ctx.window.bitBswupHandler('ACTIVATE', { version: 'v2' })).not.toThrow();
        expect(seen).toEqual(['ACTIVATE']);
    });

    // Handler="bitBswupHandler" points the custom-handler hook at the very global this script
    // registers: the handler would invoke itself until the stack blew, with handleInternal
    // prepending duplicate asset rows at every depth on the way down.
    it('refuses a Handler that resolves to bitBswupHandler itself', () => {
        const ctx = progressPage({
            elements: {
                ...fullSplash,
                'bit-bswup-assets': {},
                'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-assets': 'true', 'data-bit-bswup-handler': 'bitBswupHandler' },
            },
        });

        expect(() => ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 10, index: 1, asset: { url: 'a.js', hash: 'h' } }))
            .not.toThrow();
        // Exactly one row: the recursion would have prepended one per stack frame.
        expect(ctx.elements['bit-bswup-assets'].children).toHaveLength(1);
    });
});

// A background update downloads behind an app that is already running, and by default its
// progress is shown exactly like a first install's - the splash and its percentage are the
// only feedback most apps give that an update is being fetched. ShowOnUpdate="false" is the
// opt-out for an app whose splash is a take-over it does not want painted over a live UI.
describe('background-update progress', () => {
    it('paints the splash for background-update progress by default (firstInstall: false)', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' }, firstInstall: false });

        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup-progress-bar'].style.width).toBe('30%');
        expect(ctx.elements['bit-bswup-percent'].textContent).toBe('30%');
    });

    // The custom-splash contract: a ChildContent indicator is driven purely through the CSS
    // variables on #bit-bswup, so an update that skipped them would leave a visible-but-frozen
    // indicator rather than nothing at all.
    it('drives the percent CSS variables for a background update too', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' }, firstInstall: false });

        expect(ctx.elements['bit-bswup'].style.getPropertyValue('--bit-bswup-percent')).toBe('30%');
        expect(ctx.elements['bit-bswup'].style.getPropertyValue('--bit-bswup-percent-text')).toBe('"30%"');
    });

    it('paints the splash for first-install progress', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' }, firstInstall: true });

        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });

    it('suppresses the background-update splash under ShowOnUpdate="false"', () => {
        const ctx = progressPage({
            elements: { ...fullSplash, 'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-on-update': 'false' } },
        });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' }, firstInstall: false });

        expect(ctx.elements['bit-bswup'].style.display).not.toBe('block');
    });

    // ShowOnUpdate is about the OVERLAY, not about the app: a first install has no running app
    // to protect, so it is painted whatever the setting says.
    it('still paints a first install under ShowOnUpdate="false"', () => {
        const ctx = progressPage({
            elements: { ...fullSplash, 'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-on-update': 'false' } },
        });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' }, firstInstall: true });

        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });

    // firstInstall rides on the message from bit-bswup.js; an older copy of that bundle still
    // cached alongside this script omits it. Absent must mean "paint", never "suppress".
    it('paints when the flag is absent, even under ShowOnUpdate="false"', () => {
        const ctx = progressPage({
            elements: { ...fullSplash, 'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-on-update': 'false' } },
        });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 30, index: 1, asset: { url: 'a.js' } });

        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });

    it('activates a finished background update automatically', () => {
        const ctx = progressPage({ elements: fullSplash });
        let reloaded = 0;
        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: false, reload: () => { reloaded++; return Promise.resolve(); } });

        expect(reloaded).toBe(1);
    });

    // The button lives OUTSIDE #bit-bswup so it can appear without revealing the splash - which
    // is what a background update needs under AutoReload="false", and what an update already
    // staged at page load needs regardless (it never produced a progress event to reveal it).
    it('surfaces a finished background update through the button under AutoReload="false"', () => {
        const ctx = progressPage({ elements: manualReloadSplash });
        let reloaded = 0;
        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: false, reload: () => { reloaded++; return Promise.resolve(); } });

        expect(reloaded).toBe(0);
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup'].style.display).not.toBe('block');
    });

    // The stalled-auto-reload recovery reveals the splash so the manual retry is not offered on
    // a blank page - but under ShowOnUpdate="false" the whole point is that the overlay never
    // paints over a running app, and a stall is no reason to break that. The button alone is
    // enough there: it lives outside the overlay.
    it('recovers a stalled background auto-reload without revealing the splash under ShowOnUpdate="false"', async () => {
        const ctx = progressPage({
            elements: { ...fullSplash, 'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-on-update': 'false' } },
            clampLongTimers: true,
        });

        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: false, reload: () => new Promise(() => { }) }); // never settles

        await waitFor(() => ctx.elements['bit-bswup-reload'].style.display === 'block',
            'the fallback timer to reveal the reload button');
        expect(ctx.elements['bit-bswup'].style.display).not.toBe('block');
    });

    // The same recovery on a FIRST install still reveals the splash: there is no running app to
    // protect, and the retry button would otherwise sit alone on an empty page.
    it('still reveals the splash when a stalled first install recovers under ShowOnUpdate="false"', async () => {
        const ctx = progressPage({
            elements: { ...fullSplash, 'bit-bswup': { ...SPLASH, 'data-bit-bswup-show-on-update': 'false' } },
            clampLongTimers: true,
        });

        ctx.window.bitBswupHandler('DOWNLOAD_FINISHED', { firstInstall: true, reload: () => new Promise(() => { }) });

        await waitFor(() => ctx.elements['bit-bswup-reload'].style.display === 'block',
            'the fallback timer to reveal the reload button');
        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });
});

// The reload button is display:none, which removes it from the accessibility tree entirely -
// its appearance alone is never announced. The always-present role="status" region carries
// the announcement.
describe('update-ready announcement', () => {
    it('announces through the status live region when the button appears', () => {
        const ctx = progressPage({ elements: manualReloadSplash });
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => Promise.resolve() });

        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup-reload-status'].textContent).not.toBe('');
    });

    it('clears the announcement when a failed install hides the button', () => {
        const ctx = progressPage({ elements: manualReloadSplash });
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => Promise.resolve() });
        ctx.window.bitBswupHandler('ERROR', { reason: 'install-aborted', message: 'failed', fatal: true });

        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('none');
        expect(ctx.elements['bit-bswup-reload-status'].textContent).toBe('');
    });
});

// A bad AppContainer selector makes document.querySelector throw; that used to happen BEFORE
// window.bitBswupHandler was assigned, so no handler ever registered, the downloadFinished ->
// reload() handshake never ran, and a first install hung until the stall watchdog.
// An interactive Blazor render can replace the whole splash subtree AFTER initialization
// (hydration mismatch, layout re-render). The handler used to keep driving the detached
// nodes - bar frozen at its last value, button toggling on an orphan - with the observer
// already disconnected and no recovery path.
describe('splash subtree replaced after initialization', () => {
    it('keeps driving the UI through the replacement elements', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 20, index: 1, asset: { url: 'a.js', hash: 'h' } });
        expect(ctx.elements['bit-bswup-progress-bar'].style.width).toBe('20%');

        // The interactive render swaps the subtree: same ids, new nodes.
        ctx.addElement('bit-bswup', SPLASH);
        ctx.addElement('bit-bswup-progress-bar', {});
        ctx.addElement('bit-bswup-percent', {});

        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 80, index: 2, asset: { url: 'b.js', hash: 'h' } });

        expect(ctx.elements['bit-bswup-progress-bar'].style.width).toBe('80%');
        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
    });

    it('shows the reload button on a replacement node too', () => {
        const ctx = progressPage({ elements: manualReloadSplash });
        ctx.addElement('bit-bswup-reload', {});
        ctx.addElement('bit-bswup-reload-status', {});

        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => Promise.resolve() });

        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup-reload-status'].textContent).not.toBe('');
    });
});

// The one test that exercises the SHIPPED wiring as a single chain: bit-bswup.js resolving
// the progress script's bitBswupHandler, real worker messages driving the splash, the
// built-in downloadFinished branch calling the REAL reload(), that reload waiting for
// activation and posting CLAIM_CLIENTS, and the CLIENTS_CLAIMED reply booting Blazor and
// hiding the splash. Every piece is tested elsewhere in isolation; a regression in the seam
// between the two bundles would pass all of those and still ship a broken first install.
describe('end-to-end first install through the built-in handler', () => {
    it('progress -> reload() -> CLAIM_CLIENTS -> CLIENTS_CLAIMED -> Blazor started, splash hidden', async () => {
        const installing = fakeWorker('installing');
        const registration = { active: null, waiting: null, installing, addEventListener() { }, update: async () => { } };
        const ctx = createPageContext({ elements: fullSplash });
        ctx.addBswupScriptTag();
        ctx.addBlazorScriptTag();
        let started = 0;
        ctx.window.Blazor = { start: async () => { started++; } };
        ctx.setServiceWorker({ registration });
        ctx.load('bit-bswup.js');
        ctx.load('bit-bswup.progress.js');
        await ctx.settle();

        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 50, index: 1, asset: { url: 'a.js', hash: 'h' } } }));
        expect(ctx.elements['bit-bswup'].style.display).toBe('block');
        expect(ctx.elements['bit-bswup-progress-bar'].style.width).toBe('50%');

        ctx.message(JSON.stringify({ type: 'progress', data: { percent: 100, index: 2, asset: { url: 'b.js', hash: 'h' } } }));
        await ctx.settle();

        // The built-in handler called the REAL reload(), which waits for activation - nothing
        // posted yet, and crucially no hard reload.
        expect(installing.posted).toEqual([]);
        expect(ctx.reloads.count).toBe(0);

        registration.installing = null;
        registration.active = installing;
        installing.state = 'activated';
        installing.fireStateChange();
        await ctx.settle();
        expect(installing.posted).toEqual(['CLAIM_CLIENTS']);

        const sourcePosted = [];
        ctx.message('CLIENTS_CLAIMED', { postMessage: m => sourcePosted.push(m) });
        await ctx.settle();

        expect(started).toBe(1);
        expect(sourcePosted).toEqual(['BLAZOR_STARTED']);
        expect(ctx.reloads.count).toBe(0);
        // The resolved reload() promise is what hides the splash - the end of the chain.
        expect(ctx.elements['bit-bswup'].style.display).toBe('none');
    });
});

describe('invalid app container selector', () => {
    it('still registers the handler and initializes', () => {
        const ctx = progressPage({
            elements: {
                ...fullSplash,
                'bit-bswup': { ...SPLASH, 'data-bit-bswup-app-container': '#' }, // '#' throws in querySelector
            },
        });

        expect(typeof ctx.window.bitBswupHandler).toBe('function');
        expect(ctx.elements['bit-bswup'].getAttribute('data-bit-bswup-initialized')).toBe('true');
        // And the splash still functions without the app-hiding nicety.
        expect(() => ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 10, index: 1, asset: { url: 'a.js' } }))
            .not.toThrow();
    });
});
