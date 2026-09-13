import { describe, it, expect } from 'vitest';
import { viewFraction } from '../../Bit.Bmotion/wwwroot/bit-bmotion.js';

// viewFraction backs the scroll-scrub fallback for BmScrollTimeline.View() on browsers without a
// native ViewTimeline. It maps a subject's journey across the scrollport onto 0→1: 0 the moment its
// leading edge appears at the far edge, 1 the moment its trailing edge leaves at the near one.
describe('viewFraction', () => {
    // A 100px-tall subject in an 800px-tall viewport (near = 0, far = 800).
    const near = 0;
    const far = 800;
    const size = 100;

    it('is 0 while the subject is still below the viewport', () => {
        expect(viewFraction(800, size, near, far)).toBe(0);
        expect(viewFraction(2000, size, near, far)).toBe(0);
    });

    it('is 1 once the subject has passed above the viewport', () => {
        expect(viewFraction(-100, size, near, far)).toBe(1);
        expect(viewFraction(-500, size, near, far)).toBe(1);
    });

    it('is 0.5 at the midpoint of the journey', () => {
        // Span is 800 + 100 = 900; halfway is start = 800 - 450 = 350.
        expect(viewFraction(350, size, near, far)).toBeCloseTo(0.5, 10);
    });

    it('increases monotonically as the subject scrolls up', () => {
        let previous = -1;
        for (let start = far; start >= -size; start -= 50) {
            const p = viewFraction(start, size, near, far);
            expect(p).toBeGreaterThanOrEqual(previous);
            previous = p;
        }
        expect(previous).toBe(1);
    });

    it('maps the full journey of a subject taller than the viewport', () => {
        // A 1600px subject in an 800px viewport still spans 0→1 rather than saturating early.
        const tall = 1600;
        expect(viewFraction(far, tall, near, far)).toBe(0);
        expect(viewFraction(-tall, tall, near, far)).toBe(1);
        expect(viewFraction((far - tall) / 2, tall, near, far)).toBeCloseTo(0.5, 10);
    });

    it('returns 0 for a degenerate span instead of dividing by zero', () => {
        expect(viewFraction(0, 0, 0, 0)).toBe(0);
        expect(Number.isNaN(viewFraction(0, 0, 0, 0))).toBe(false);
    });

    it('respects a scrollport that does not start at 0', () => {
        // A 400px-tall container occupying [200, 600) of the page.
        expect(viewFraction(600, size, 200, 600)).toBe(0);
        expect(viewFraction(100, size, 200, 600)).toBe(1);
    });
});
