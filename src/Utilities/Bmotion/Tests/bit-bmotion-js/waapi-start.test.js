import { describe, it, expect } from 'vitest';
import { waapiStartTime } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

describe('waapiStartTime', () => {
    it('starts a new animation at once, in an ordinary task', () => {
        // A click a few milliseconds after the last frame: no frame of standing still on the first keyframe.
        expect(waapiStartTime(1008, 1000, false)).toBe(1008);
    });

    it('starts a new animation on its first frame once its task has run long', () => {
        // Building a batch for 600ms: backdated, a 300ms animation would be over before anything showed it.
        expect(waapiStartTime(1600, 1000, false)).toBeNull();
    });

    it('starts at once when there is no timeline time to compare with', () => {
        expect(waapiStartTime(1600, null, false)).toBe(1600);
    });

    it('carries a continuation on from the instant it was interrupted, however long the task', () => {
        expect(waapiStartTime(1600, 1000, true, 1010)).toBe(1010);
        expect(waapiStartTime(1008, 1000, true, 1002)).toBe(1002);
    });

    it('starts a continuation now when the interruption is unknown or in the future', () => {
        expect(waapiStartTime(1600, 1000, true)).toBe(1600);
        expect(waapiStartTime(1600, 1000, true, 1700)).toBe(1600);
    });
});
