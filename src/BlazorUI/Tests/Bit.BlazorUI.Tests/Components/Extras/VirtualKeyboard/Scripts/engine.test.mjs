import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Engine } from './helpers/engine-fixture.mjs';

const back = (text) => Engine.deleteBackward(text, text.length, text.length).text;

test('Backspace deletes one code point after a combining mark, a harakat and a ZWNJ', () => {
    assert.equal(back('e\u{301}'), 'e');
    assert.equal(back('\u{628}\u{64E}'), '\u{628}');
    assert.equal(back('\u{645}\u{6CC}\u{200C}'), '\u{645}\u{6CC}');
    assert.equal(back('ab'), 'a');
});

test('Backspace deletes a whole emoji sequence and never half a surrogate pair', () => {
    assert.equal(back('a\u{1F44D}\u{1F3FD}'), 'a');
    assert.equal(back('a\u{1F468}\u{200D}\u{1F469}\u{200D}\u{1F467}'), 'a');
    assert.equal(back('a\u{1F1EE}\u{1F1F7}'), 'a');
    assert.equal(back('a1\u{FE0F}\u{20E3}'), 'a');
    assert.equal(back('a\u{1D400}'), 'a');
});

test('Backspace deletes the selection, and nothing at the start', () => {
    assert.deepEqual(Engine.deleteBackward('abcd', 1, 3), { text: 'ad', start: 1, end: 1, inserted: '' });
    assert.equal(Engine.deleteBackward('abc', 0, 0), null);
});

test('Delete mirrors Backspace forward', () => {
    assert.equal(Engine.deleteForward('e\u{301}x', 0, 0).text, '\u{301}x');
    assert.equal(Engine.deleteForward('\u{1F44D}\u{1F3FD}x', 0, 0).text, 'x');
    assert.equal(Engine.deleteForward('ab', 2, 2), null);
});

test('insert replaces the selection and keeps to maxlength in code units', () => {
    assert.deepEqual(Engine.insert('abcdef', 2, 4, 'XY', null), { text: 'abXYef', start: 4, end: 4, inserted: 'XY' });
    assert.equal(Engine.insert('ab', 2, 2, '\u{1F600}', 3), null, 'an emoji is two code units and does not fit');
    assert.equal(Engine.insert('ab', 2, 2, 'cde', 3).text, 'abc');
    assert.equal(Engine.insert('abc', 3, 3, 'd', 3), null);
    assert.equal(Engine.insert('abc', 1, 2, 'X', 3).text, 'aXc', 'a replaced selection makes room');
});

test('the allowed characters are a character class every grapheme must match', () => {
    assert.equal(Engine.isAllowed('12', '0-9'), true);
    assert.equal(Engine.isAllowed('1a', '0-9'), false);
    assert.equal(Engine.isAllowed('anything', null), true);
    assert.equal(Engine.isAllowed('x', 'z-a'), true, 'an invalid body allows all');
});

test('a dead key composes with a character, else types itself before it', () => {
    const acute = { e: '\u{E9}', ' ': '\u{B4}' };
    assert.equal(Engine.resolveDeadKey(acute, 'e'), '\u{E9}');
    assert.equal(Engine.resolveDeadKey(acute, 'x'), '\u{B4}x');
    assert.equal(Engine.deadKeyAlone(acute), '\u{B4}');
    assert.equal(Engine.resolveDeadKey(null, 'x'), 'x');
});

test('multitap cycles through its items', () => {
    assert.equal(Engine.nextMultiTap(3, 0), 1);
    assert.equal(Engine.nextMultiTap(3, 2), 0);
});

test('Shift latches, locks on a double tap and lets go after a character', () => {
    let state = { mode: 'off', lastTap: 0 };
    state = Engine.tapShift(state, 1000);
    assert.equal(state.mode, 'latched');
    assert.equal(Engine.afterTyping(state).mode, 'off');

    state = Engine.tapShift(state, 1200);
    assert.equal(state.mode, 'locked');
    assert.equal(Engine.afterTyping(state).mode, 'locked');

    state = Engine.tapShift(state, 3000);
    assert.equal(state.mode, 'off');

    state = Engine.tapShift(Engine.tapShift({ mode: 'off', lastTap: 0 }, 1000), 2000);
    assert.equal(state.mode, 'off', 'two slow taps turn it on and off');
});

test('a type=number input takes only whole numbers', () => {
    for (const valid of ['1', '-1', '1.5', '.5', '1e3']) assert.equal(Engine.isNumber(valid), true, valid);
    for (const invalid of ['-', '1e', '1,5', '', 'e', '1.', '-1.']) assert.equal(Engine.isNumber(invalid), false, invalid);
    assert.equal(Engine.isNumberCharacter('5'), true);
    assert.equal(Engine.isNumberCharacter('\u{6F5}'), false);
});

test('grapheme boundaries move the caret by whole clusters', () => {
    const text = 'a\u{1F44D}\u{1F3FD}e\u{301}';
    assert.equal(Engine.nextGraphemeEnd(text, 1), 5);
    assert.equal(Engine.previousGraphemeStart(text, 7), 5);
    assert.deepEqual(Engine.graphemes(text), ['a', '\u{1F44D}\u{1F3FD}', 'e\u{301}']);
});