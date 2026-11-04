// What the BitResourceTimeline port added around interaction: the disabled
// option (view-only: no press, hover or key selects or reports anything, while
// paging and zooming still work), the data-bit-esc="claim" the wrapper carries
// while an Escape would clear a selection, and keys pressed during an IME
// composition, which belong to the composition.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeIndexedEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');

function bar(id, startHour, endHour) {
    return { id, resourceId: 'r0', startTime: START + startHour * HOUR, endTime: START + endHour * HOUR };
}

// A laid-out engine with two bars on one row, whose pointer events map 1:1 onto
// the surface, and which records what it reports instead of reaching .NET.
function makeInteractiveEngine(config = {}) {
    const engine = makeIndexedEngine([bar('a', 1, 4), bar('b', 8, 10)], { config });
    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine._pixelsPerHour = 40;
    engine._pixelsPerMs = 40 / HOUR;
    engine._visibleWidth = 1050;
    engine._recomputeRowMetrics();

    engine.focused = 0;
    engine.wrapper.focus = () => { engine.focused++; };
    engine.renderer = {
        surface: {
            style: { cursor: '' },
            setPointerCapture() { },
            releasePointerCapture() { },
            getBoundingClientRect: () => ({ left: 0, top: 0 })
        }
    };
    engine._surfaceRect = { left: 0, top: 0 };
    engine.render = () => { };
    engine._relayout = () => { };
    engine._syncAxisSplitterChrome = () => { };
    engine._announceFocus = () => { };
    engine._scrollFocusIntoView = () => { };

    engine.notified = [];
    const notifySelection = engine._notifySelection;
    engine._notifySelection = function () {
        this.notified.push([...this.selectedBars]);
        notifySelection.call(this);
    };
    engine.announced = [];
    engine._announce = (m) => engine.announced.push(m);
    engine.reported = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name, ...args) => {
            engine.reported.push(name);
            return Promise.resolve(name === 'OnAllocationChanging' ? true : undefined);
        }
    };
    return engine;
}

// A wrapper element that keeps its attributes, like the real one.
function attributeWrapper() {
    const attributes = new Map();
    return {
        scrollLeft: 0,
        scrollTop: 0,
        attributes,
        writes: 0,
        focus() { },
        setAttribute(name, value) { this.writes++; attributes.set(name, String(value)); },
        removeAttribute(name) { this.writes++; attributes.delete(name); },
        getAttribute(name) { return attributes.has(name) ? attributes.get(name) : null; }
    };
}

function pointer(type, x, y, extra = {}) {
    return {
        type,
        pointerType: 'mouse',
        pointerId: 1,
        button: 0,
        clientX: x,
        clientY: y,
        ctrlKey: false,
        metaKey: false,
        shiftKey: false,
        altKey: false,
        defaultPrevented: false,
        preventDefault() { this.defaultPrevented = true; },
        ...extra
    };
}

function key(k, extra = {}) {
    return {
        key: k,
        ctrlKey: false,
        metaKey: false,
        altKey: false,
        shiftKey: false,
        defaultPrevented: false,
        preventDefault() { this.defaultPrevented = true; },
        ...extra
    };
}

// The surface point in the middle of bar `a`.
function onBarA(engine) {
    return {
        x: engine.getTimeToX(START + 2.5 * HOUR),
        y: engine.getResourceToY(0) + engine._rowHeight(0) / 2
    };
}

function click(engine, { x, y }, extra = {}) {
    const down = pointer('pointerdown', x, y, extra);
    engine.handlePointerDown(down);
    const up = pointer('pointerup', x, y, extra);
    engine.handlePointerUp(up);
    return { down, up };
}

// ---- disabled ----

test('a click selects a bar while the timeline is enabled', () => {
    const engine = makeInteractiveEngine();

    const { down } = click(engine, onBarA(engine));

    assert.deepEqual([...engine.selectedBars], ['a']);
    assert.deepEqual(engine.notified, [['a']]);
    assert.equal(engine.focused, 1, 'a press focuses the wrapper');
    assert.equal(down.defaultPrevented, true);
    assert.ok(engine.reported.includes('OnTimelineClick'));
});

test('a disabled timeline ignores a press, a click and a context menu', () => {
    const engine = makeInteractiveEngine({ disabled: true });
    const at = onBarA(engine);

    const { down } = click(engine, at);

    assert.equal(engine.selectedBars.size, 0, 'a click must not select');
    assert.deepEqual(engine.notified, []);
    assert.ok(!engine.drag && !engine.edit, 'no gesture starts');
    assert.equal(engine.focused, 0, 'the press is left to the browser');
    assert.equal(down.defaultPrevented, false, 'nothing is prevented either');

    const menu = pointer('contextmenu', at.x, at.y);
    engine.handleContextMenu(menu);
    assert.equal(menu.defaultPrevented, false, 'the browser keeps its own menu');
    assert.deepEqual(engine.reported, [], 'nothing is reported to .NET');
});

test('a disabled timeline neither edits nor marquees', () => {
    const engine = makeInteractiveEngine({ disabled: true, editable: true });
    const at = onBarA(engine);
    const before = engine.allocations.map(a => [a.startTime, a.endTime]);

    engine.handlePointerDown(pointer('pointerdown', at.x, at.y));
    engine.handlePointerMove(pointer('pointermove', at.x + 200, at.y));
    engine.handlePointerUp(pointer('pointerup', at.x + 200, at.y));

    assert.deepEqual(engine.allocations.map(a => [a.startTime, a.endTime]), before);
    assert.equal(engine.selectedBars.size, 0);
    assert.deepEqual(engine.reported, []);
});

test('a disabled timeline ignores keyboard selection but still pages', () => {
    const engine = makeInteractiveEngine({ disabled: true });
    engine._focusAlloc = engine.allocations[0];
    engine._focusResource = 0;
    let moves = 0;
    engine._moveFocusHorizontal = () => { moves++; };
    const pages = [];
    engine._pageScroll = (dir) => pages.push(dir);

    engine.handleKeyDown(key('Enter'));
    engine.handleKeyDown(key(' '));
    assert.equal(engine.selectedBars.size, 0, 'Enter and Space must not select the focused bar');

    engine.handleKeyDown(key('ArrowRight'));
    assert.equal(moves, 0, 'the arrow keys do not move the bar focus');

    engine.handleKeyDown(key('PageDown'));
    engine.handleKeyDown(key('PageUp'));
    assert.deepEqual(pages, [1, -1], 'paging still works');
    assert.deepEqual(engine.notified, []);
});

test('a disabled timeline leaves a selection alone on Escape', () => {
    const engine = makeInteractiveEngine();
    engine.selectBars(['a'], false);
    engine.setOptions({ disabled: true });

    const escape = key('Escape');
    engine.handleKeyDown(escape);

    assert.deepEqual([...engine.selectedBars], ['a']);
    assert.equal(escape.defaultPrevented, false);
    assert.deepEqual(engine.announced, []);
});

test('switching disabled on cancels a marquee in progress', () => {
    const engine = makeInteractiveEngine();
    engine.selectedBars.add('a');
    // A live marquee has already widened the selection; nothing has reported it.
    engine.drag = { pointerId: 1, baseSelection: new Set(['a']), moved: true };
    engine.selectedBars.add('b');

    engine.setOptions({ disabled: true });

    assert.equal(engine.drag, null);
    assert.deepEqual([...engine.selectedBars], ['a'], 'the selection goes back to where the marquee started');
});

// ---- the Escape claim ----

test('the wrapper claims Escape exactly while there is a selection to clear', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;

    assert.equal(wrapper.getAttribute('data-bit-esc'), null, 'no selection, no claim');

    engine.selectBars(['a'], false);
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');
    assert.equal(engine._escClaimed, true);

    const writes = wrapper.writes;
    engine.selectBars(['b'], false);
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');
    assert.equal(wrapper.writes, writes, 'a selection that stays non-empty leaves the attribute alone');

    const escape = key('Escape');
    engine.handleKeyDown(escape);
    assert.equal(escape.defaultPrevented, true);
    assert.equal(engine.selectedBars.size, 0);
    assert.deepEqual(engine.announced, ['Selection cleared']);
    assert.equal(wrapper.getAttribute('data-bit-esc'), null, 'nothing left for an Escape to do');
    assert.equal(engine._escClaimed, false);

    // An Escape with nothing selected is not the timeline's: it is not consumed.
    const idle = key('Escape');
    engine.handleKeyDown(idle);
    assert.equal(idle.defaultPrevented, false);
});

test('a click on empty content drops the claim with the selection', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;

    click(engine, onBarA(engine));
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');

    click(engine, { x: engine.getTimeToX(START + 20 * HOUR), y: onBarA(engine).y });
    assert.equal(engine.selectedBars.size, 0);
    assert.equal(wrapper.getAttribute('data-bit-esc'), null);
});

test('disabling the timeline withdraws the claim and enabling it restores it', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;
    engine.selectBars(['a'], false);

    engine.setOptions({ disabled: true });
    assert.equal(wrapper.getAttribute('data-bit-esc'), null, 'a disabled timeline does not act on Escape');
    assert.deepEqual([...engine.selectedBars], ['a'], 'the selection itself is kept');

    engine.setOptions({ disabled: false });
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');
});

test('a live marquee claims Escape as soon as it selects something', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;
    const a = engine.allocations.find(x => x.id === 'a');
    const band = engine._barContentBand(a, 0);

    engine._applyMarqueeSelection({
        startX: engine._timeToContentX(a.startTime) + 1,
        startY: band.top + 0.1,
        currentX: engine._timeToContentX(a.endTime) - 1,
        currentY: band.bottom - 0.1,
        additive: false,
        baseSelection: new Set()
    });

    assert.deepEqual([...engine.selectedBars], ['a']);
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');
});

test('there is no claim without a time range', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;
    engine.timeRange = { start: null, end: null };

    engine.selectedBars.add('a');
    engine._syncEscapeClaim();

    assert.equal(wrapper.getAttribute('data-bit-esc'), null);
    assert.equal(engine._escClaimed, false);
});

test('a wrapper without attributes (a bare engine) is never marked', () => {
    const engine = makeInteractiveEngine();
    assert.equal(typeof engine.wrapper.setAttribute, 'undefined', 'fixture: the bare wrapper has no DOM API');

    engine.selectBars(['a'], false);

    assert.deepEqual([...engine.selectedBars], ['a']);
    assert.equal(engine._escClaimed, false, 'the claim is only recorded once it is on an element');
});

// ---- IME composition ----

test('an Escape pressed during an IME composition leaves the selection alone', () => {
    const engine = makeInteractiveEngine();
    const wrapper = attributeWrapper();
    engine.wrapper = wrapper;
    engine.selectBars(['a'], false);

    for (const composing of [key('Escape', { isComposing: true }), key('Escape', { keyCode: 229 })]) {
        engine.handleKeyDown(composing);
        assert.deepEqual([...engine.selectedBars], ['a']);
        assert.equal(composing.defaultPrevented, false, 'the key is the composition\'s');
    }
    assert.deepEqual(engine.announced, []);
    assert.equal(wrapper.getAttribute('data-bit-esc'), 'claim');

    // Once the composition is over the same key clears the selection.
    engine.handleKeyDown(key('Escape'));
    assert.equal(engine.selectedBars.size, 0);
});

test('no key acts during an IME composition, and the core library decides what one is', () => {
    const engine = makeInteractiveEngine();
    engine._focusAlloc = engine.allocations[0];
    engine._focusResource = 0;
    let moves = 0;
    engine._moveFocusHorizontal = () => { moves++; };
    const utils = globalThis.BitBlazorUI.Utils;
    const isComposingKey = utils.isComposingKey;
    const asked = [];
    utils.isComposingKey = (e) => { asked.push(e.key); return e.key !== 'ArrowLeft'; };
    try {
        engine.handleKeyDown(key('ArrowRight'));
        engine.handleKeyDown(key('Enter'));
        engine.handleKeyDown(key('=', { ctrlKey: true }));
        assert.equal(moves, 0);
        assert.equal(engine.selectedBars.size, 0);

        engine.handleKeyDown(key('ArrowLeft'));
        assert.equal(moves, 1, 'a key that is not composing still acts');
    } finally {
        utils.isComposingKey = isComposingKey;
    }
    assert.deepEqual(asked, ['ArrowRight', 'Enter', '=', 'ArrowLeft']);
});
