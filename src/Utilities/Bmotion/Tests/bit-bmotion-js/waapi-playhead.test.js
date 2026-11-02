import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { playWaapiAnimation, cancelWaapiAnimation, waapiPlayhead } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// A Web Animation reduced to what the bridge's start bookkeeping touches.
class FakeAnimation {
    constructor() {
        this.playState = 'running';   // a pending play
        this.pending = true;
        this._start = null;
        this.currentTime = 0;
        this.playbackRate = 1;
        this.effect = null;
        this.finished = new Promise((_, reject) => { this._reject = reject; });
    }
    get startTime() { return this._start; }
    set startTime(t) { this._start = t; this.pending = false; this.playState = 'running'; }
    pause() { this.playState = 'paused'; this._start = null; this.currentTime = 0; }
    play() { this.playState = 'running'; this.pending = true; }
    cancel() { this.playState = 'idle'; this._start = null; this.currentTime = null; this._reject(new Error('cancelled')); }
    commitStyles() { }
}

let now, frame, frames;

beforeEach(() => {
    frames = [];
    vi.spyOn(performance, 'now').mockImplementation(() => now);
    globalThis.document = {
        timeline: { get currentTime() { return frame; } },
        getElementById: () => ({ style: {}, animate: () => new FakeAnimation() }),
    };
    globalThis.requestAnimationFrame = cb => frames.push(cb);
});

afterEach(() => {
    vi.restoreAllMocks();
    delete globalThis.document;
    delete globalThis.requestAnimationFrame;
});

describe('waapiPlayhead', () => {
    it('reads a running animation as of now, final once its frame has passed', () => {
        frame = 1000; now = 1005;
        playWaapiAnimation('run', '1', [], { duration: 1000 });
        now = 1020;
        expect(waapiPlayhead('run', '1')).toEqual([15, 0]);   // the task that started it may yet run long

        frame = 1016; now = 1100;
        expect(waapiPlayhead('run', '1')).toEqual([95, 1]);
    });

    it('answers for a cancelled animation with where it was cancelled, not with the start', () => {
        // A drag's start cancels the element's animations, then asks the engine where the element is - which asks
        // here, before the cancellations are off the books.
        frame = 5000; now = 5010;
        playWaapiAnimation('drag', '1', [], { duration: 1000 });
        frame = 5290; now = 5300;

        cancelWaapiAnimation('drag', '1', true);
        now = 5310;

        expect(waapiPlayhead('drag', '1')).toEqual([290, 1]);
    });

    it('reads an early start of a task that turns long as not started, until the frame that shows it', () => {
        frame = 7000; now = 7005;
        playWaapiAnimation('early', '1', [], { duration: 1000 });   // started at once
        now = 7080;
        playWaapiAnimation('late', '1', [], { duration: 1000 });    // the task has run long: held

        // The frame that shows them restarts the early one: nothing of it has been seen.
        expect(waapiPlayhead('early', '1')).toEqual([0, 0]);
        expect(waapiPlayhead('late', '1')).toEqual([0, 0]);

        frame = 7100; now = 7100;
        frames.shift()();
        now = 7150;

        expect(waapiPlayhead('early', '1')).toEqual([50, 1]);
        expect(waapiPlayhead('late', '1')).toEqual([0, 0]);   // played, pending until the browser starts it
    });

    it('answers null for an animation it is not playing', () => {
        expect(waapiPlayhead('nowhere', '1')).toBeNull();
    });
});
