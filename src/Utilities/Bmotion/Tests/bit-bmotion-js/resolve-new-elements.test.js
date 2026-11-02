import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { resolveBySelector, resolveByRef } from '../../Bit.Bmotion/Scripts/bit-bmotion.ts';

// An element with the id and attributes a page gave it, which is all the resolve functions touch.
function fakeElement(id = '') {
    const attrs = new Map();
    return {
        id,
        getAttribute: name => attrs.get(name) ?? null,
        setAttribute: (name, value) => attrs.set(name, value),
    };
}

// The engine keeps what a programmatic animation left on an element under its id. An element the engine has never
// addressed - one rendered again in place of an element with the same id - shows none of it, so the resolve functions
// name those elements, and the engine drops what it kept for their ids.
describe('resolveBySelector / resolveByRef', () => {
    let matches;
    let previousDocument;

    beforeEach(() => {
        matches = [];
        previousDocument = globalThis.document;
        globalThis.document = {
            querySelectorAll: () => matches,
            getElementById: id => matches.find(el => el.id === id) ?? null,
        };
    });

    afterEach(() => {
        globalThis.document = previousDocument;
    });

    it('names an element the first time it is resolved, and not again', () => {
        matches = [fakeElement('ball')];

        expect(resolveBySelector('#ball')).toEqual([['ball'], ['ball']]);
        expect(resolveBySelector('#ball')).toEqual([['ball'], []]);
    });

    it('names a new element that took the id of one resolved before', () => {
        matches = [fakeElement('ball')];
        resolveBySelector('#ball');

        // The page was left and opened again: the same id, another element.
        matches = [fakeElement('ball')];

        expect(resolveBySelector('#ball')).toEqual([['ball'], ['ball']]);
    });

    it('names only the new ones among several', () => {
        const known = fakeElement('a');
        matches = [known];
        resolveBySelector('.item');
        matches = [known, fakeElement('b')];

        expect(resolveBySelector('.item')).toEqual([['a', 'b'], ['b']]);
    });

    it('does the same for an element handed over by reference', () => {
        const ball = fakeElement('ball');

        expect(resolveByRef(ball)).toEqual([['ball'], ['ball']]);
        expect(resolveByRef(ball)).toEqual([['ball'], []]);
        expect(resolveByRef(fakeElement('ball'))).toEqual([['ball'], ['ball']]);
    });
});
