import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { setInert } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// The attributes of one element, which is all setInert touches.
function fakeElement(attributes = []) {
    const attrs = new Set(attributes);
    return {
        hasAttribute: name => attrs.has(name),
        setAttribute: name => attrs.add(name),
        removeAttribute: name => attrs.delete(name),
        attrs,
    };
}

// An exiting element is made inert, and a cancelled exit makes it interactive again - without ever taking away an
// inert the app set on the element itself.
describe('setInert', () => {
    let elements;
    let previousDocument;

    beforeEach(() => {
        elements = {};
        previousDocument = globalThis.document;
        globalThis.document = { getElementById: id => elements[id] ?? null };
    });

    afterEach(() => {
        globalThis.document = previousDocument;
    });

    it('makes an exiting element inert, and interactive again when its exit is cancelled', () => {
        const el = elements.box = fakeElement();
        setInert('box', true);
        expect(el.hasAttribute('inert')).toBe(true);
        setInert('box', false);
        expect(el.hasAttribute('inert')).toBe(false);
        expect(el.attrs.size).toBe(0);
    });

    it('leaves an inert the app set alone', () => {
        const el = elements.box = fakeElement(['inert']);
        setInert('box', true);
        setInert('box', false);
        expect(el.hasAttribute('inert')).toBe(true);
    });

    it('ignores an element that is gone', () => {
        expect(() => setInert('missing', true)).not.toThrow();
    });
});
