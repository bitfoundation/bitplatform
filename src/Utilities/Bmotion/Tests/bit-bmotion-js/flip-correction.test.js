import { describe, it, expect } from 'vitest';
import { flipChildFrames, flipRadiusFrames, flipCorrectionOffsets } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

const rect = (left, top, width, height) => ({
    left, top, width, height, right: left + width, bottom: top + height, x: left, y: top,
});

// The numbers in a "translate(tx,ty) scaleX(kx) scaleY(ky)" keyframe.
const parse = frame => {
    const [tx, ty, kx, ky] = frame.transform.match(/-?[\d.e+-]+(?=px|\))/g).map(Number);
    return { tx, ty, kx, ky };
};

// What a keyframe list shows at progress q: linear between the two keyframes around it, as WAAPI does.
const at = (frames, q, read) => {
    const i = Math.max(0, frames.findIndex(f => f.offset >= q) - 1);
    const a = frames[i], b = frames[Math.min(i + 1, frames.length - 1)];
    const f = b.offset === a.offset ? 0 : (q - a.offset) / (b.offset - a.offset);
    const va = read(a), vb = read(b);
    return Object.fromEntries(Object.keys(va).map(k => [k, va[k] + (vb[k] - va[k]) * f]));
};

describe('flipChildFrames', () => {
    it('starts at the inverse of the parent transform and ends at identity', () => {
        const c = flipChildFrames(rect(0, 0, 200, 100), rect(20, 10, 40, 40), 2, 0.5, 30, -12);
        const first = parse(c.frames[0]), last = parse(c.frames[c.frames.length - 1]);

        expect(c.frames[0].offset).toBe(0);
        expect(first.kx).toBe(0.5);   // 1 / 2
        expect(first.ky).toBe(2);     // 1 / 0.5
        expect(first.tx).toBe(-15);   // -30 / 2
        expect(first.ty).toBe(24);    // -(-12) / 0.5
        expect(c.frames[c.frames.length - 1].offset).toBe(1);
        expect(last.kx).toBe(1);
        expect(last.ky).toBe(1);
        expect(last.tx).toBeCloseTo(0); // -0 renders identically to 0
        expect(last.ty).toBeCloseTo(0);
    });

    it('origins at the parent anchor, in the child box coordinates', () => {
        const parent = rect(0, 0, 200, 100);
        const child = rect(20, 10, 40, 40); // 20px right, 10px down from the parent
        const topLeft = flipChildFrames(parent, child, 2, 0.5, 0, 0);
        expect(topLeft.originX).toBe(-20);
        expect(topLeft.originY).toBe(-10);

        const centre = flipChildFrames(parent, child, 2, 0.5, 0, 0, 0.5, 0.5);
        expect(centre.originX).toBe(80);  // the parent's centre (100, 50) seen from the child (20, 10)
        expect(centre.originY).toBe(40);
    });

    it('stays the exact inverse in between, not just at the ends', () => {
        // A 2.5x grow: two keyframes (1/s -> 1, linear) would leave the child 22% too wide halfway.
        const sx = 0.4, sy = 0.4, dx = 60, dy = -30;
        const c = flipChildFrames(rect(0, 0, 200, 100), rect(20, 10, 40, 40), sx, sy, dx, dy);
        for (let q = 0; q <= 1; q += 0.01) {
            const scale = sx + (1 - sx) * q;
            const shown = at(c.frames, q, parse);
            expect(Math.abs(shown.kx * scale - 1)).toBeLessThan(0.01);
            // The child's translate, carried by the parent's scale, cancels the parent's translate.
            expect(Math.abs(shown.tx * scale + dx * (1 - q))).toBeLessThan(0.5);
        }
    });

    it('clamps a zero scale so the keyframes stay finite', () => {
        const c = flipChildFrames(rect(0, 0, 200, 100), rect(20, 10, 40, 40), 0, 0, 30, -12);
        for (const frame of c.frames)
            expect(Object.values(parse(frame)).every(Number.isFinite)).toBe(true);
    });
});

describe('flipRadiusFrames', () => {
    const radii = frame => {
        const [x, y] = frame.borderRadius.match(/[\d.]+(?=px)/g).map(Number);
        return { x, y };
    };

    it('divides the radius by the scale so corners render constant', () => {
        const frames = flipRadiusFrames(12, 2, 4);
        expect(radii(frames[0])).toEqual({ x: 6, y: 3 });   // 12 / 2, 12 / 4
        expect(radii(frames[frames.length - 1])).toEqual({ x: 12, y: 12 });
    });

    it('renders constant all the way through', () => {
        const frames = flipRadiusFrames(10, 0.5, 0.5);
        for (let q = 0; q <= 1; q += 0.01)
            expect(Math.abs(at(frames, q, radii).x * (0.5 + 0.5 * q) - 10)).toBeLessThan(0.1);
    });

    it('clamps a zero scale so the radius stays finite', () => {
        for (const frame of flipRadiusFrames(12, 0, 0))
            expect(Object.values(radii(frame)).every(Number.isFinite)).toBe(true);
    });
});

describe('flipCorrectionOffsets', () => {
    it('runs from 0 to 1 in order, without duplicates', () => {
        const offsets = flipCorrectionOffsets(0.3, 2);
        expect(offsets[0]).toBe(0);
        expect(offsets[offsets.length - 1]).toBe(1);
        for (let i = 1; i < offsets.length; i++) expect(offsets[i]).toBeGreaterThan(offsets[i - 1]);
    });
});
