import { test } from 'node:test';
import assert from 'node:assert/strict';
import { History, state } from './helpers/history-fixture.mjs';

// Types `text` one character at a time from `start`, the way the editor reports it: before each input, then the state
// after it, then after it again. Returns the time after the last character.
function type(history, start, text, now = 1000, gap = 50) {
    let html = start;
    for (const ch of text) {
        if (history.beforeInput('insert', now)) history.commit();
        html += ch;
        history.capture(state(html));
        if (history.afterInput('insert', ch)) history.commit();
        now += gap;
    }
    return now;
}

test('a new history has nothing to undo or redo', () => {
    const history = new History(state('<p>a</p>'));
    assert.equal(history.canUndo, false);
    assert.equal(history.canRedo, false);
    assert.equal(history.undo(), null);
    assert.equal(history.redo(), null);
    assert.equal(history.touched, false);
});

test('a command is one step, and undo and redo walk it', () => {
    const history = new History(state('<p>a</p>'));
    assert.equal(history.record(state('<p><b>a</b></p>')), true);
    assert.equal(history.canUndo, true);

    assert.equal(history.undo().html, '<p>a</p>');
    assert.equal(history.canUndo, false);
    assert.equal(history.canRedo, true);

    assert.equal(history.redo().html, '<p><b>a</b></p>');
    assert.equal(history.canRedo, false);
});

test('recording what is already there makes no step', () => {
    const history = new History(state('<p>a</p>'));
    assert.equal(history.record(state('<p>a</p>', 'caret moved')), false);
    assert.equal(history.canUndo, false);
});

test('a word typed is one step, closed by the space after it', () => {
    const history = new History(state(''));
    type(history, '', 'hello ');
    assert.equal(history.undoDepth, 1);
    assert.equal(history.hasPending, false);
    assert.equal(history.undo().html, '');
});

test('text typed but not yet closed by a boundary can be undone, and goes first', () => {
    const history = new History(state(''));
    type(history, '', 'one tw');
    assert.equal(history.canUndo, true, 'the pending "tw" counts');
    assert.equal(history.undo().html, 'one ');
    assert.equal(history.undo().html, '');
});

test('a pause longer than the typing pause starts a new step mid-word', () => {
    const history = new History(state(''));
    const after = type(history, '', 'ab');
    type(history, 'ab', 'cd', after + History.TYPING_PAUSE_MS + 1);
    assert.equal(history.undo().html, 'ab');
    assert.equal(history.undo().html, '');
});

test('deleting after typing is a step of its own', () => {
    const history = new History(state(''));
    let now = type(history, '', 'abc');
    if (history.beforeInput('delete', now)) history.commit();
    history.capture(state('ab'));
    history.afterInput('delete', null);
    assert.equal(history.undo().html, 'abc');
    assert.equal(history.undo().html, '');
});

test('an input that does not group (a paste, Enter) closes the step before and after it', () => {
    const history = new History(state(''));
    let now = type(history, '', 'ab');
    assert.equal(history.beforeInput('other', now), true);
    history.commit();
    history.capture(state('ab<br>'));
    assert.equal(history.afterInput('other', null), true);
    history.commit();
    assert.equal(history.undo().html, 'ab');
    assert.equal(history.undo().html, '');
});

test('a composition is one run however long it takes', () => {
    const history = new History(state(''));
    assert.equal(history.beforeInput('compose', 0), false);
    history.capture(state('k'));
    assert.equal(history.afterInput('compose', 'k'), false);
    assert.equal(history.beforeInput('compose', 10_000), false, 'a slow composition is not split by the pause');
    history.capture(state('か'));
    history.afterInput('compose', 'か');
    history.commit();
    assert.equal(history.undo().html, '');
});

test('a new edit after an undo clears the redo steps', () => {
    const history = new History(state('a'));
    history.record(state('ab'));
    history.undo();
    assert.equal(history.canRedo, true);
    history.record(state('ax'));
    assert.equal(history.canRedo, false);
});

test('typing after an undo clears the redo steps at the first character, before the word is committed', () => {
    const history = new History(state('a'));
    history.record(state('ab'));
    history.undo();
    assert.equal(history.canRedo, true);
    type(history, 'a', 'x');
    assert.equal(history.hasPending, true);
    assert.equal(history.canRedo, false);
});

test('undo brings back the selection of the state it returns to', () => {
    const history = new History(state('a', 'start'));
    history.record(state('ab', 'after b'));
    history.record(state('abc', 'after c'));
    assert.equal(history.undo().selection, 'after b');
});

test('the history keeps at most maxSteps steps, dropping the oldest', () => {
    const history = new History(state('0'), 3);
    for (let i = 1; i <= 5; i++) history.record(state(String(i)));
    assert.equal(history.undoDepth, 3);
    assert.equal(history.undo().html, '4');
    assert.equal(history.undo().html, '3');
    assert.equal(history.undo().html, '2');
    assert.equal(history.undo(), null);
});

test('the history keeps under maxChars, dropping the oldest steps but never the current state', () => {
    const history = new History(state('x'.repeat(10)), 200, 25);
    history.record(state('y'.repeat(10)));
    assert.equal(history.undoDepth, 1);
    history.record(state('z'.repeat(10)));
    assert.equal(history.undoDepth, 1, 'the oldest step went to stay under 25 characters');
    assert.ok(history.size <= 25);
    history.record(state('w'.repeat(40)));
    assert.equal(history.undoDepth, 0, 'a state over the limit on its own is kept, with nothing behind it');
    assert.equal(history.size, 40);
});

test('the size follows the states as they move between the stacks', () => {
    const history = new History(state('aa'));
    history.record(state('bbbb'));
    history.record(state('cccccc'));
    assert.equal(history.size, 12);
    history.undo();
    history.undo();
    assert.equal(history.size, 12);
    history.record(state('d'));
    assert.equal(history.size, 3, 'the redo steps are gone');
});

test('replaceCurrent moves the current state without a step and keeps the redo steps', () => {
    const history = new History(state('a'));
    history.record(state('ab'));
    history.undo();
    history.replaceCurrent(state('a '));
    assert.equal(history.canRedo, true);
    assert.equal(history.canUndo, false);
    assert.equal(history.redo().html, 'ab');
    assert.equal(history.undo().html, 'a ');
});

test('reset starts over and forgets that anything was typed', () => {
    const history = new History(state(''));
    type(history, '', 'ab ');
    assert.equal(history.touched, true);
    history.reset(state('<p>value</p>'));
    assert.equal(history.touched, false);
    assert.equal(history.canUndo, false);
});

test('the input types fall into the kinds that group', () => {
    assert.equal(History.kindOf('insertText'), 'insert');
    assert.equal(History.kindOf('insertReplacementText'), 'insert');
    assert.equal(History.kindOf('insertCompositionText'), 'compose');
    assert.equal(History.kindOf('deleteContentBackward'), 'delete');
    assert.equal(History.kindOf('deleteWordForward'), 'delete');
    assert.equal(History.kindOf('deleteByCut'), 'other');
    assert.equal(History.kindOf('insertParagraph'), 'other');
    assert.equal(History.kindOf('insertFromPaste'), 'other');
    assert.equal(History.kindOf('formatBold'), 'other');
    assert.equal(History.kindOf(undefined), 'other');
});
