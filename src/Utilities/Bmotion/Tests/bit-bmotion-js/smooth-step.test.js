import { describe, it, expect } from 'vitest';
import { smoothStep } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// A smoothed scroll timeline's spring: progress (0-1) following the scroll's, a frame at a time.
const recipe = { stiffness: 100, damping: 30, mass: 1 }; // motion.dev's useSpring(scrollYProgress) recipe

function run(state, target, frames, spring = recipe, dtMs = 16) {
    const values = [];
    for (let i = 0; i < frames; i++) {
        smoothStep(state, target, dtMs, spring);
        values.push(state.value);
    }
    return values;
}

describe('smoothStep', () => {
    it('glides to the target instead of jumping there', () => {
        const state = { value: 0, velocity: 0 };
        const values = run(state, 1, 3);
        expect(values[0]).toBeGreaterThan(0);
        expect(values[0]).toBeLessThan(0.1);
        expect(values[1]).toBeGreaterThan(values[0]);
        expect(values[2]).toBeGreaterThan(values[1]);
    });

    it('comes to rest exactly on the target, without overshooting it (overdamped)', () => {
        const state = { value: 0, velocity: 0 };
        let atRest = false, frames = 0, highest = 0;
        while (!atRest && frames < 600) {
            atRest = smoothStep(state, 1, 16, recipe);
            highest = Math.max(highest, state.value);
            frames++;
        }
        expect(atRest).toBe(true);
        expect(state.value).toBe(1);
        expect(state.velocity).toBe(0);
        expect(highest).toBeLessThanOrEqual(1);
        expect(frames).toBeLessThan(200); // a few seconds at most
    });

    it('a bouncy spring overshoots and settles', () => {
        const state = { value: 0, velocity: 0 };
        const values = run(state, 1, 300, { stiffness: 300, damping: 5, mass: 1 });
        expect(Math.max(...values)).toBeGreaterThan(1);
        expect(state.value).toBe(1);
    });

    it('is the same however the frames are cut', () => {
        const a = { value: 0, velocity: 0 }, b = { value: 0, velocity: 0 };
        run(a, 1, 6, recipe, 16);
        run(b, 1, 24, recipe, 4);
        expect(a.value).toBeCloseTo(b.value, 6);
    });

    it('does not fling after a long pause: one step covers 64 ms at most', () => {
        const a = { value: 0, velocity: 0 }, b = { value: 0, velocity: 0 };
        smoothStep(a, 1, 5000, recipe);
        smoothStep(b, 1, 64, recipe);
        expect(a.value).toBe(b.value);
    });

    it('is at rest straight away when it is already there', () => {
        const state = { value: 0.4, velocity: 0 };
        expect(smoothStep(state, 0.4, 16, recipe)).toBe(true);
    });
});
