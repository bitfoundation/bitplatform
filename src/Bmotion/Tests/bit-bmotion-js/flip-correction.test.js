import { describe, it, expect } from 'vitest';
import { flipChildCorrection, correctedRadius } from '../../Bit.Bmotion/wwwroot/bit-bmotion.js';

const rect = (left, top, width, height) => ({
    left, top, width, height, right: left + width, bottom: top + height, x: left, y: top,
});

describe('flipChildCorrection', () => {
    it('inverts the parent scale and origins at the parent top-left', () => {
        const parent = rect(0, 0, 200, 100);
        const child = rect(20, 10, 40, 40); // 20px right, 10px down from parent
        const c = flipChildCorrection(parent, child, 2, 0.5);

        expect(c.fromScaleX).toBe(0.5);   // 1 / 2
        expect(c.fromScaleY).toBe(2);     // 1 / 0.5
        // Origin is the parent's top-left expressed in the child's box coords → negated offset.
        expect(c.originX).toBe(-20);
        expect(c.originY).toBe(-10);
    });

    it('a child flush with the parent origin scales about its own top-left', () => {
        const parent = rect(50, 50, 300, 300);
        const child = rect(50, 50, 100, 100);
        const c = flipChildCorrection(parent, child, 3, 3);
        expect(c.originX).toBeCloseTo(0); // -0 renders identically to 0
        expect(c.originY).toBeCloseTo(0);
        expect(c.fromScaleX).toBeCloseTo(1 / 3);
        expect(c.fromScaleY).toBeCloseTo(1 / 3);
    });

    it('defaults to no translate correction when the FLIP delta is omitted', () => {
        const parent = rect(0, 0, 200, 100);
        const child = rect(20, 10, 40, 40);
        const c = flipChildCorrection(parent, child, 2, 0.5);
        expect(c.fromTranslateX).toBeCloseTo(0); // -0 renders identically to 0
        expect(c.fromTranslateY).toBeCloseTo(0);
    });

    it('cancels the parent translate as -d/s so the child does not jump', () => {
        const parent = rect(0, 0, 200, 100);
        const child = rect(20, 10, 40, 40);
        // Parent FLIP: translate(30,-12) scale(2, 0.5) about its top-left.
        const c = flipChildCorrection(parent, child, 2, 0.5, 30, -12);
        expect(c.fromTranslateX).toBe(-15); // -30 / 2
        expect(c.fromTranslateY).toBe(24);  // -(-12) / 0.5
    });

    it('clamps a zero scale so the inverse stays finite', () => {
        const parent = rect(0, 0, 200, 100);
        const child = rect(20, 10, 40, 40);
        // A 0-sized start rect yields sx = sy = 0 - must not produce Infinity/NaN.
        const c = flipChildCorrection(parent, child, 0, 0, 30, -12);
        expect(Number.isFinite(c.fromScaleX)).toBe(true);
        expect(Number.isFinite(c.fromScaleY)).toBe(true);
        expect(Number.isFinite(c.fromTranslateX)).toBe(true);
        expect(Number.isFinite(c.fromTranslateY)).toBe(true);
    });
});

describe('correctedRadius', () => {
    it('divides the radius by the scale so corners render constant', () => {
        const r = correctedRadius(12, 2, 4);
        expect(r.fromX).toBe(6);  // 12 / 2
        expect(r.fromY).toBe(3);  // 12 / 4
    });
    it('is identity at scale 1', () => {
        const r = correctedRadius(10, 1, 1);
        expect(r.fromX).toBe(10);
        expect(r.fromY).toBe(10);
    });
    it('clamps a zero scale so the radius stays finite', () => {
        const r = correctedRadius(12, 0, 0);
        expect(Number.isFinite(r.fromX)).toBe(true);
        expect(Number.isFinite(r.fromY)).toBe(true);
    });
});
