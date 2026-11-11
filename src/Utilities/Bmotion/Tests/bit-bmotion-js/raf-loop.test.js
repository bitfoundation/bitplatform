import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { startRafLoop, stopRafLoop, watchReducedMotion, unwatchReducedMotion } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// Blazor revives a brand-new DotNetObject wrapper every time it marshals a DotNetObjectReference, so each call
// below gets a fresh wrapper for the same engine - the way the real interop hands them over (a fake that passes
// the same object back would hide the bug where stopping by identity removed nothing).
function engine() {
    const engine = { calls: 0 };
    engine.wrapper = () => ({
        invokeMethod(name) { if (name === 'ComputeFrame') engine.calls++; return null; },
    });
    return engine;
}

// A hand-driven requestAnimationFrame: frame() runs the callback the loop last asked for.
let pending = null;
let nextId = 0;
function frame() {
    const callback = pending;
    pending = null;
    callback?.(performance.now());
}

beforeEach(() => {
    pending = null;
    globalThis.requestAnimationFrame = cb => { pending = cb; return ++nextId; };
    globalThis.cancelAnimationFrame = () => { pending = null; };
});

afterEach(() => {
    stopRafLoop(null);
    delete globalThis.requestAnimationFrame;
    delete globalThis.cancelAnimationFrame;
});

describe('the rAF loop', () => {
    it('stops ticking an engine that stopped, though it arrives as a new wrapper', () => {
        const a = engine();
        startRafLoop('1', a.wrapper());
        frame();
        expect(a.calls).toBe(1);

        stopRafLoop('1');
        expect(pending).toBeNull();
        frame();
        expect(a.calls).toBe(1);
    });

    it('ticks a restarted engine once a frame, however often it restarts', () => {
        const a = engine();
        for (let visit = 0; visit < 4; visit++) {
            startRafLoop('1', a.wrapper());
            frame();
            stopRafLoop('1');
        }
        expect(a.calls).toBe(4);

        startRafLoop('1', a.wrapper());
        startRafLoop('1', a.wrapper());
        a.calls = 0;
        frame();
        frame();
        expect(a.calls).toBe(2);
    });

    it('keeps ticking the other engines when one stops', () => {
        const a = engine();
        const b = engine();
        startRafLoop('1', a.wrapper());
        startRafLoop('2', b.wrapper());
        stopRafLoop('1');
        frame();
        expect(a.calls).toBe(0);
        expect(b.calls).toBe(1);
        expect(pending).not.toBeNull();
    });

    it('stops every engine without a key', () => {
        const a = engine();
        const b = engine();
        startRafLoop('1', a.wrapper());
        startRafLoop('2', b.wrapper());
        stopRafLoop(null);
        expect(pending).toBeNull();
    });
});

describe('the reduced-motion watchers', () => {
    let listeners;
    beforeEach(() => {
        listeners = new Set();
        globalThis.matchMedia = () => ({
            matches: false,
            addEventListener: (_, l) => listeners.add(l),
            removeEventListener: (_, l) => listeners.delete(l),
        });
    });
    afterEach(() => { delete globalThis.matchMedia; });

    it('drops a watcher that unwatches, though it arrives as a new wrapper', () => {
        const a = engine();
        watchReducedMotion('1', a.wrapper());
        expect(listeners.size).toBe(1);

        unwatchReducedMotion('1');
        expect(listeners.size).toBe(0);
    });

    it('notifies each engine once, however often it watched', () => {
        let notified = 0;
        const wrapper = () => ({ invokeMethodAsync() { notified++; return Promise.resolve(); } });
        watchReducedMotion('1', wrapper());
        watchReducedMotion('1', wrapper());
        for (const l of listeners) l({ matches: true });
        expect(notified).toBe(1);
        unwatchReducedMotion('1');
        expect(listeners.size).toBe(0);
    });
});
