import { describe, it, expect } from 'vitest';
import {
    applyElastic,
    resolveConstraintBounds,
    clampToConstraints,
    scrollFraction,
} from '../../Bit.Bmotion/wwwroot/bit-bmotion.js';

const rect = (left, top, width, height) => ({
    left, top, width, height, right: left + width, bottom: top + height,
    x: left, y: top,
});

describe('applyElastic', () => {
    it('scales overflow by the edge factor', () => {
        expect(applyElastic(100, 0.5)).toBe(50);
        expect(applyElastic(100, 1)).toBe(100);
    });
    it('is rigid (0) for a non-positive edge', () => {
        expect(applyElastic(100, 0)).toBe(0);
        expect(applyElastic(100, -1)).toBe(0);
    });
});

describe('resolveConstraintBounds', () => {
    it('computes bounds from container/element rects (no active transform)', () => {
        // 300x200 container, 50x50 element at its top-left, no transform applied.
        const c = rect(0, 0, 300, 200);
        const e = rect(0, 0, 50, 50);
        const b = resolveConstraintBounds(c, e, 0, 0);
        expect(b.left).toBe(0);
        expect(b.top).toBe(0);
        expect(b.right).toBe(250);   // 300 - 50
        expect(b.bottom).toBe(150);  // 200 - 50
    });

    it('backs the current transform out of the element rect', () => {
        // The element already shows a +40/+30 transform, so its untransformed origin is (10,20).
        const c = rect(0, 0, 300, 200);
        const e = rect(50, 50, 50, 50); // measured rect includes the +40/+30 offset
        const b = resolveConstraintBounds(c, e, 40, 30);
        // base = (50-40, 50-30) = (10, 20)
        expect(b.left).toBe(-10);
        expect(b.top).toBe(-20);
        expect(b.right).toBe(240);   // 300 - (10 + 50)
        expect(b.bottom).toBe(130);  // 200 - (20 + 50)
    });

    it('collapses to a centered offset when the element is larger than the container', () => {
        const c = rect(0, 0, 100, 100);
        const e = rect(0, 0, 160, 160); // bigger than container
        const b = resolveConstraintBounds(c, e, 0, 0);
        // right (100-160=-60) < left (0) → both become the midpoint -30
        expect(b.left).toBe(-30);
        expect(b.right).toBe(-30);
        expect(b.top).toBe(-30);
        expect(b.bottom).toBe(-30);
    });
});

describe('clampToConstraints', () => {
    const elastic = { left: 0, right: 0, top: 0, bottom: 0 };
    const c = { left: -100, right: 100, top: -50, bottom: 50 };

    it('passes through values within bounds', () => {
        expect(clampToConstraints(20, 10, c, elastic)).toEqual({ x: 20, y: 10 });
    });

    it('rigidly clamps beyond bounds when elastic is 0', () => {
        expect(clampToConstraints(200, 999, c, elastic)).toEqual({ x: 100, y: 50 });
        expect(clampToConstraints(-200, -999, c, elastic)).toEqual({ x: -100, y: -50 });
    });

    it('applies elastic give past the edge', () => {
        // 50px past the right edge with 0.5 elastic → 100 + 25 = 125
        const soft = { left: 0.5, right: 0.5, top: 0.5, bottom: 0.5 };
        expect(clampToConstraints(150, 0, c, soft).x).toBe(125);
        // 50px past the left edge → -100 - 25 = -125
        expect(clampToConstraints(-150, 0, c, soft).x).toBe(-125);
    });

    it('returns the input unchanged when there are no constraints', () => {
        expect(clampToConstraints(9999, -9999, null, elastic)).toEqual({ x: 9999, y: -9999 });
    });

    it('ignores an unconstrained (null) edge', () => {
        const partial = { left: null, right: 100, top: null, bottom: null };
        // left is null → large negative x passes through; right still clamps.
        expect(clampToConstraints(-5000, 0, partial, elastic).x).toBe(-5000);
        expect(clampToConstraints(5000, 0, partial, elastic).x).toBe(100);
    });
});

describe('scrollFraction', () => {
    it('is 0 when the content does not overflow', () => {
        expect(scrollFraction(0, 500, 500)).toBe(0);
        expect(scrollFraction(0, 400, 500)).toBe(0);
    });
    it('is the normalised progress across the scrollable range', () => {
        expect(scrollFraction(0, 1000, 500)).toBe(0);
        expect(scrollFraction(250, 1000, 500)).toBe(0.5);
        expect(scrollFraction(500, 1000, 500)).toBe(1);
    });
});
