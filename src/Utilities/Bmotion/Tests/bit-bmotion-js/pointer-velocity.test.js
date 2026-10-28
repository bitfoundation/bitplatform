import { describe, it, expect } from 'vitest';
import { pointerVelocity } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// What a drag throws with at release, and what a pan reports per move: the pointer's average velocity over the last
// 100 ms, in px per the unit asked for.
describe('pointerVelocity', () => {
    it('is the average speed over the window', () => {
        const v = pointerVelocity();
        v.reset(0, 0, 0);
        for (let t = 16; t <= 96; t += 16) v.add(t, t / 2, t); // 1 px/ms right, 0.5 px/ms down
        const at = v.read(96, 1000);
        expect(at.x).toBeCloseTo(1000, 0);
        expect(at.y).toBeCloseTo(500, 0);
    });

    it('does not spike when moves arrive back to back', () => {
        // Three moves 33 px apart, half a millisecond each: no faster on screen than within one frame.
        const v = pointerVelocity();
        v.reset(0, 0, 1000);
        v.add(33, 0, 1000.5);
        v.add(66, 0, 1001);
        v.add(100, 0, 1001.5);
        expect(v.read(1002, 16).x).toBeCloseTo(100, 5); // 100 px per 16 ms frame, not 1056
    });

    it('has none once the pointer stood still for the whole window', () => {
        const v = pointerVelocity();
        v.reset(0, 0, 0);
        v.add(50, 0, 16);
        v.add(100, 0, 32);
        expect(v.read(40, 16).x).toBeGreaterThan(30);
        expect(v.read(250, 16)).toEqual({ x: 0, y: 0 });
    });

    it('slows as the pointer slows before it lets go', () => {
        // Moving fast, then held still 60 ms: the window counts the stillness too.
        const v = pointerVelocity();
        v.reset(0, 0, 0);
        v.add(40, 0, 20);
        const moving = v.read(20, 1000).x;
        const held = v.read(80, 1000).x;
        expect(moving).toBeCloseTo(2000, 0);
        expect(held).toBeCloseTo(500, 0);
    });

    it('forgets the last gesture when a new one starts', () => {
        const v = pointerVelocity();
        v.reset(0, 0, 0);
        v.add(100, 0, 10);
        v.reset(100, 0, 20);
        expect(v.read(25, 1000)).toEqual({ x: 0, y: 0 });
    });
});
