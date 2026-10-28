import { describe, it, expect } from 'vitest';
import { autoScrollSpeed } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// How far a drag auto-scrolls its container in a frame, along one axis: a container from 0 to 400 px, scrolling
// within 48 px of an edge, at up to 20 px a frame.
describe('autoScrollSpeed', () => {
    it('stands still away from the edges', () => {
        expect(autoScrollSpeed(200, 0, 400)).toBe(0);
        expect(autoScrollSpeed(48, 0, 400)).toBe(0);
        expect(autoScrollSpeed(352, 0, 400)).toBe(0);
    });

    it('scrolls towards the nearer edge, faster the closer', () => {
        expect(autoScrollSpeed(376, 0, 400)).toBeCloseTo(10, 5);
        expect(autoScrollSpeed(390, 0, 400)).toBeGreaterThan(autoScrollSpeed(376, 0, 400));
        expect(autoScrollSpeed(24, 0, 400)).toBeCloseTo(-10, 5);
    });

    it('is at full speed on the edge and past it', () => {
        expect(autoScrollSpeed(400, 0, 400)).toBe(20);
        expect(autoScrollSpeed(520, 0, 400)).toBe(20);
        expect(autoScrollSpeed(-30, 0, 400)).toBe(-20);
    });

    it('keeps the edge zones to a quarter of a small container', () => {
        // 100 px tall: 25 px zones, so the middle half never scrolls.
        expect(autoScrollSpeed(30, 0, 100)).toBe(0);
        expect(autoScrollSpeed(70, 0, 100)).toBe(0);
        expect(autoScrollSpeed(90, 0, 100)).toBeCloseTo(12, 5);
    });

    it('does nothing in a container with no size', () => {
        expect(autoScrollSpeed(0, 0, 0)).toBe(0);
    });
});
