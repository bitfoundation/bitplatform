import { describe, it, expect } from 'vitest';
import { waapiPlayedAt } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

describe('waapiPlayedAt', () => {
    it('reads a running animation as of now, not as of the last frame', () => {
        // Started at 1000, read at 1250: 250ms in, whatever the timeline's stale time says.
        expect(waapiPlayedAt(1250, 1000, false, 120, 1)).toBe(250);
    });

    it('scales by the playback rate', () => {
        expect(waapiPlayedAt(1250, 1000, false, null, 2)).toBe(500);
    });

    it('reads one held on its first keyframe where it is held', () => {
        // A late start (waapiStartTime) is paused at 0 until the frame that shows it: it has played nothing yet,
        // however long ago the engine built it.
        expect(waapiPlayedAt(1600, null, true, 0, 1)).toBe(0);
    });

    it('reads a paused or stopped-rate animation at its current time', () => {
        expect(waapiPlayedAt(1600, 1000, true, 320, 1)).toBe(320);
        expect(waapiPlayedAt(1600, 1000, false, 320, 0)).toBe(320);
    });

    it('reads one that has not started at its current time, or at the start', () => {
        expect(waapiPlayedAt(1600, null, false, null, 1)).toBe(0);
    });

    it('never reads before the start', () => {
        expect(waapiPlayedAt(990, 1000, false, null, 1)).toBe(0);
    });

    it('reads one the frame that first shows it will restart as not started', () => {
        // Started early in a task that then ran long: its start time says 300ms in, but nothing of it has been shown.
        expect(waapiPlayedAt(1300, 1000, false, null, 1, true)).toBe(0);
    });
});
