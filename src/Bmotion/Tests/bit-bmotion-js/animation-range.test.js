import { describe, it, expect } from 'vitest';
import { splitAnimationRange } from '../../Bit.Bmotion/wwwroot/bit-bmotion.js';

// BmScrollTimeline.Range carries a CSS `animation-range` shorthand, but the Web Animations API
// takes the two ends as separate rangeStart / rangeEnd values - so the bridge has to split it.
describe('splitAnimationRange', () => {
    it('splits two keyword-and-offset parts', () => {
        expect(splitAnimationRange('entry 0% cover 50%')).toEqual(['entry 0%', 'cover 50%']);
        expect(splitAnimationRange('cover 25% cover 75%')).toEqual(['cover 25%', 'cover 75%']);
    });

    it('splits bare keywords', () => {
        expect(splitAnimationRange('entry exit')).toEqual(['entry', 'exit']);
        expect(splitAnimationRange('cover')).toEqual(['cover', undefined]);
    });

    it('splits bare percentages, which have no keyword to attach to', () => {
        expect(splitAnimationRange('0% 100%')).toEqual(['0%', '100%']);
        expect(splitAnimationRange('40%')).toEqual(['40%', undefined]);
    });

    it('handles a keyword start with a bare end and vice versa', () => {
        expect(splitAnimationRange('entry 20% 80%')).toEqual(['entry 20%', '80%']);
        expect(splitAnimationRange('20% exit 80%')).toEqual(['20%', 'exit 80%']);
    });

    it('accepts lengths as well as percentages', () => {
        expect(splitAnimationRange('entry 100px exit 200px')).toEqual(['entry 100px', 'exit 200px']);
    });

    it('tolerates irregular whitespace', () => {
        expect(splitAnimationRange('  entry   0%   cover  50% ')).toEqual(['entry 0%', 'cover 50%']);
    });

    it('returns nothing for an empty or missing range', () => {
        expect(splitAnimationRange('')).toEqual([undefined, undefined]);
        expect(splitAnimationRange('   ')).toEqual([undefined, undefined]);
        expect(splitAnimationRange(null)).toEqual([undefined, undefined]);
        expect(splitAnimationRange(undefined)).toEqual([undefined, undefined]);
    });

    it('is case-insensitive about the keywords', () => {
        expect(splitAnimationRange('ENTRY 0% Cover 50%')).toEqual(['ENTRY 0%', 'Cover 50%']);
    });
});
