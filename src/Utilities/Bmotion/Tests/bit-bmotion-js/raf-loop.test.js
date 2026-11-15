import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { startRafLoop, stopRafLoop, watchReducedMotion, unwatchReducedMotion, _requestBeforeFrame } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// Blazor revives a brand-new DotNetObject wrapper every time it marshals a DotNetObjectReference, so each call
// below gets a fresh wrapper for the same engine - the way the real interop hands them over (a fake that passes
// the same object back would hide the bug where stopping by identity removed nothing). onFrame runs inside the
// engine's ComputeFrame, the way .NET calls back into the module synchronously on WebAssembly.
function engine(onFrame) {
    const engine = { calls: 0, timestamps: [] };
    engine.wrapper = () => ({
        invokeMethod(name, timestamp) {
            if (name !== 'ComputeFrame') return null;
            engine.calls++;
            engine.timestamps.push(timestamp);
            onFrame?.();
            return null;
        },
    });
    return engine;
}

// A hand-driven requestAnimationFrame: frame() runs every callback asked for before it started, as one browser frame
// does (with one timestamp), and leaves the ones they ask for to the next.
let pending = new Map();
let nextId = 0;
let now = 0;
function frame() {
    const due = [...pending.values()];
    pending.clear();
    now += 16;
    for (const callback of due) callback(now);
}

beforeEach(() => {
    pending = new Map();
    globalThis.requestAnimationFrame = cb => { pending.set(++nextId, cb); return nextId; };
    globalThis.cancelAnimationFrame = id => { pending.delete(id); };
});

afterEach(() => {
    stopRafLoop(null);
    frame(); // run off anything left queued, so it can't leak into the next test
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
        expect(pending.size).toBe(0);
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
        expect(pending.size).toBe(1);
    });

    it('stops every engine without a key', () => {
        const a = engine();
        const b = engine();
        startRafLoop('1', a.wrapper());
        startRafLoop('2', b.wrapper());
        stopRafLoop(null);
        expect(pending.size).toBe(0);
    });

    it('schedules no further frame for an engine that stops itself inside its tick', () => {
        const a = engine(() => stopRafLoop('1'));
        startRafLoop('1', a.wrapper());
        frame();
        expect(a.calls).toBe(1);
        expect(pending.size).toBe(0);
    });

    it('runs a single chain when the loop stops and restarts inside one tick', () => {
        // The last engine stops itself, and the same tick starts another (a later engine of another root, a
        // re-render): the frame the restart scheduled is the loop's only one.
        const b = engine();
        const a = engine(() => { stopRafLoop('1'); startRafLoop('2', b.wrapper()); });
        startRafLoop('1', a.wrapper());
        frame();
        expect(pending.size).toBe(1);
        const ticked = b.calls; // the tick that started it may already have reached it

        frame();
        frame();
        expect(b.calls - ticked).toBe(2); // once a frame, not once per chain
        expect(new Set(b.timestamps).size).toBe(b.timestamps.length);

        stopRafLoop('2');
        expect(pending.size).toBe(0);
    });
});

describe('the work before a frame', () => {
    it('still runs work queued while the loop ran, when the loop stops before its next tick', () => {
        let runs = 0;
        const a = engine();
        startRafLoop('1', a.wrapper());
        _requestBeforeFrame(() => runs++); // the loop's next tick would run it, so it gets no frame of its own
        stopRafLoop('1');
        frame();
        expect(runs).toBe(1);
    });

    it('still runs work queued inside a tick that stops the loop', () => {
        let runs = 0;
        const a = engine(() => { _requestBeforeFrame(() => runs++); stopRafLoop('1'); });
        startRafLoop('1', a.wrapper());
        frame();
        frame();
        expect(runs).toBe(1);
        expect(a.calls).toBe(1);
    });

    it('keeps re-queued work going after the loop stops', () => {
        // A drag's auto-scroll asks again each frame for as long as it scrolls.
        let runs = 0;
        const step = () => { if (++runs < 3) _requestBeforeFrame(step); };
        const a = engine();
        startRafLoop('1', a.wrapper());
        _requestBeforeFrame(step);
        frame();
        stopRafLoop('1');
        frame();
        frame();
        expect(runs).toBe(3);
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
