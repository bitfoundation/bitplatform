import { describe, it, expect } from 'vitest';
import { nativeSpaceActivation } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// A tappable element suppresses Space's page scroll - but only where Space would scroll. On a native control it
// is the control's own activation (a button clicks on keyup) or typing, and must be left alone.
describe('nativeSpaceActivation', () => {
    it('leaves Space to native controls', () => {
        for (const tag of ['button', 'BUTTON', 'input', 'select', 'textarea', 'summary'])
            expect(nativeSpaceActivation(tag, false)).toBe(true);
    });

    it('leaves Space to editable content', () => {
        expect(nativeSpaceActivation('div', true)).toBe(true);
    });

    it('suppresses the page scroll on elements without an activation of their own', () => {
        for (const tag of ['div', 'li', 'a', 'span', 'section'])
            expect(nativeSpaceActivation(tag, false)).toBe(false);
    });
});
