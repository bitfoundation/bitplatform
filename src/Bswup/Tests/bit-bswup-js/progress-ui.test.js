import { describe, it, expect } from 'vitest';
import { createPageContext } from './harness.js';

const SPLASH = { 'data-bit-bswup-config': 'true' };

/**
 * Loads bit-bswup.js (which defines the shared BswupMessage constants) and then
 * bit-bswup.progress.js, the way a host page references both.
 */
function progressPage({ elements = {}, readyState = 'complete' } = {}) {
    const ctx = createPageContext({ elements, readyState });
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
    'bit-bswup-error': {},
    'bit-bswup-error-message': {},
    'bit-bswup-error-details': {},
    'bit-bswup-error-retry': {},
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
        const ctx = progressPage();
        expect(observing(ctx)).toBe(true); // still waiting
        await new Promise(r => setTimeout(r, 20)); // harness clamps timers to ~5ms
        expect(observing(ctx)).toBe(false);
    });
});

describe('download progress rendering', () => {
    it('updates the bar, percentage and aria-valuenow', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('DOWNLOAD_PROGRESS', { percent: 42.4, index: 3, asset: { url: 'a.js', hash: 'h' } });

        expect(ctx.elements['bit-bswup-progress-bar'].style.display).not.toBe('none');
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

    it('hides Retry for deterministic failures that a reload cannot fix', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('ERROR', { reason: 'manifest', message: 'malformed', fatal: true });
        expect(ctx.elements['bit-bswup-error-retry'].style.display).toBe('none');
    });

    it('offers Retry for a transient failure', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.bitBswupHandler('ERROR', { reason: 'fetch', message: 'network blip', fatal: true });
        expect(ctx.elements['bit-bswup-error-retry'].style.display).toBe('inline-block');
    });
});

describe('update ready', () => {
    it('wires the reload button when autoReload is off', () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.BitBswupProgress.config({ autoReload: false });

        let reloaded = 0;
        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => { reloaded++; return Promise.resolve(); } });

        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('inline');
        ctx.elements['bit-bswup-reload'].onclick();
        expect(reloaded).toBe(1);
    });

    it('falls back to a manual button if an auto-reload rejects', async () => {
        const ctx = progressPage({ elements: fullSplash });
        ctx.window.BitBswupProgress.config({ autoReload: true });

        ctx.window.bitBswupHandler('UPDATE_READY', { reload: () => Promise.reject(new Error('nope')) });
        await ctx.settle();
        expect(ctx.elements['bit-bswup-reload'].style.display).toBe('inline');
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
});
