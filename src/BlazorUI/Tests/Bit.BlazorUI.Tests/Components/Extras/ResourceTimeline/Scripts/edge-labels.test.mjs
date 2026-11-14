// collapseMatchingEdgeLabels: two neighbouring bars whose facing labels match
// (the first one's end, the next one's start - the airport between two legs)
// draw that text once, and it does not stack them.
//
// The fixture measures 7px per character, so 'AAA' is 21px wide, and the label
// gap is 3px. A pair is merged while the clear space between the bars is under
// 21 + 3 + 21 + 3 + 21 = 69px, and the one label left is drawn, centered, only
// while that space holds 21 + 2 * 3 = 27px.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine, makeIndexedEngine } from './helpers/engine-fixture.mjs';

const MINUTE = 60000;
const HOUR = 60 * MINUTE;

function alloc(id, resourceId, startTime, endTime, extra = {}) {
    return { id, resourceId, startTime, endTime, ...extra };
}

// Two hour-long legs meeting at `code`, `gap` minutes apart: at one pixel per
// minute, the gap in pixels.
function legs(gap, endText = 'AAA', startText = 'AAA', extraA = {}, extraB = {}) {
    return [
        alloc('a', 'r0', 0, 60 * MINUTE, { textStart: 'XXX', textEnd: endText, ...extraA }),
        alloc('b', 'r0', (60 + gap) * MINUTE, (120 + gap) * MINUTE, { textStart: startText, textEnd: 'YYY', ...extraB })
    ];
}

function collapsing(allocations, { pxPerMinute = 1, config = {} } = {}) {
    const engine = makeIndexedEngine(allocations, {
        config: { collapseMatchingEdgeLabels: true, ...config },
        engine: { _pixelsPerMs: pxPerMinute / MINUTE }
    });
    engine.render = () => {};
    engine._syncAxisSplitterChrome = () => {};
    return engine;
}

function lanesOf(engine) {
    return engine.allocations.map(a => engine._laneInfo.get(a).lane);
}

function edges(engine) {
    return engine.allocations.map(a => {
        const s = engine._edgeLabelState(a);
        return { hideStart: s.hideStart, hideEnd: s.hideEnd, shareEnd: s.shareEnd };
    });
}

const PLAIN = { hideStart: false, hideEnd: false, shareEnd: false };

test('a gap too narrow for the word keeps the pair on one lane and draws neither copy', () => {
    const engine = collapsing(legs(8));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [
        { hideStart: false, hideEnd: true, shareEnd: false },
        { hideStart: true, hideEnd: false, shareEnd: false }
    ]);
});

test('a gap the word fits in draws it once, centered between the bars', () => {
    const engine = collapsing(legs(40));
    const [a, b] = engine.allocations;

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [
        { hideStart: false, hideEnd: false, shareEnd: true },
        { hideStart: true, hideEnd: false, shareEnd: false }
    ]);
    assert.equal(engine._edgeLabelState(a).anchorTime, (a.endTime + b.startTime) / 2);
});

test('a gap wide enough for both labels and a word between leaves them alone', () => {
    const engine = collapsing(legs(80));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('different labels still stack', () => {
    const engine = collapsing(legs(8, 'AAA', 'BBB'));

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('placeholders are not a match', () => {
    for (const placeholder of ['—', '–', '-', '…', '...']) {
        const engine = collapsing(legs(8, placeholder, ` ${placeholder} `));
        assert.deepEqual(lanesOf(engine), [0, 1], `'${placeholder}' must not collapse`);
        assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
    }
});

test('labels match trimmed and ignoring case', () => {
    const engine = collapsing(legs(8, 'aaa', ' AAA '));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.equal(edges(engine)[1].hideStart, true);
});

test('bars overlapping in time stack and keep both labels', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 60 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 50 * MINUTE, 110 * MINUTE, { textStart: 'AAA' })
    ]);

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('touching bars are a junction', () => {
    const engine = collapsing(legs(0));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine)[0], { hideStart: false, hideEnd: true, shareEnd: false });
    assert.equal(edges(engine)[1].hideStart, true);
});

test('the second leg\'s own end label is kept, even when it repeats the code', () => {
    const engine = collapsing(legs(40, 'AAA', 'AAA', {}, { textEnd: 'AAA' }));

    assert.equal(edges(engine)[1].hideEnd, false);
    assert.equal(edges(engine)[1].shareEnd, false);
});

test('a chain of legs merges every turn and stays on one lane', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 60 * MINUTE, { textStart: 'AMS', textEnd: 'CDG' }),
        alloc('b', 'r0', 68 * MINUTE, 128 * MINUTE, { textStart: 'CDG', textEnd: 'AMS' }),
        alloc('c', 'r0', 136 * MINUTE, 196 * MINUTE, { textStart: 'AMS', textEnd: 'LHR' })
    ]);

    assert.deepEqual(lanesOf(engine), [0, 0, 0]);
    assert.deepEqual(edges(engine), [
        { hideStart: false, hideEnd: true, shareEnd: false },
        { hideStart: true, hideEnd: true, shareEnd: false },
        { hideStart: true, hideEnd: false, shareEnd: false }
    ]);
});

test('delay bars narrow the gap the word has to fit in', () => {
    // 40px between the bars, of which the delay bars take 20.
    const engine = collapsing(legs(40, 'AAA', 'AAA',
        { endBar: { duration: 10 * MINUTE } }, { startBar: { duration: 10 * MINUTE } }));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine)[0], { hideStart: false, hideEnd: true, shareEnd: false });
});

test('edge icons narrow the gap the word has to fit in', () => {
    // An end icon of 16px pushes the label 19px out: 40 - 19 = 21px left.
    const engine = collapsing(legs(40, 'AAA', 'AAA',
        { icons: [{ source: 'i.png', position: 'end', size: 16 }] }));

    assert.deepEqual(edges(engine)[0], { hideStart: false, hideEnd: true, shareEnd: false });
    assert.equal(edges(engine)[1].hideStart, true);
});

test('a first leg too narrow to carry labels merges nothing', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 20 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 28 * MINUTE, 88 * MINUTE, { textStart: 'AAA' })
    ]);

    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('a second leg too narrow to carry labels still has the first one\'s label centered', () => {
    // b draws no labels at 20px, so its start label is not counted: the pair is
    // merged under 21 + 3 + 21 = 45px, and the word fits in 40.
    const engine = collapsing([
        alloc('a', 'r0', 0, 60 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 100 * MINUTE, 120 * MINUTE, { textStart: 'AAA' })
    ]);

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [{ hideStart: false, hideEnd: false, shareEnd: true }, PLAIN]);
});

test('a pair a third bar splits onto two lanes draws both labels again', () => {
    // z overlaps a, so a takes the second lane, while b could take the first:
    // the label drawn once beside a would say nothing about b.
    for (const stackOnLabelCollision of [true, false]) {
        const engine = collapsing([
            alloc('z', 'r0', 0, 65 * MINUTE),
            alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
            alloc('b', 'r0', 78 * MINUTE, 138 * MINUTE, { textStart: 'AAA' })
        ], { config: { stackOnLabelCollision } });
        const [, a, b] = engine.allocations;
        const laneA = engine._laneInfo.get(a).lane;
        const laneB = engine._laneInfo.get(b).lane;

        assert.notEqual(laneA, laneB, `stackOnLabelCollision ${stackOnLabelCollision}`);
        assert.deepEqual(edges(engine), [PLAIN, PLAIN, PLAIN], `stackOnLabelCollision ${stackOnLabelCollision}`);
    }
});

test('a pair the lane cap hides one of draws its labels as usual', () => {
    // z and w fill both lanes when a starts, so the cap hides a behind a +N
    // marker; b, starting once they have ended, would be drawn on its own.
    const engine = collapsing([
        alloc('z', 'r0', 0, 75 * MINUTE),
        alloc('w', 'r0', 5 * MINUTE, 72 * MINUTE),
        alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 78 * MINUTE, 138 * MINUTE, { textStart: 'AAA' })
    ], { config: { maxStackLanes: 2 } });

    assert.equal(engine._laneInfo.get(engine.allocations[2]).overflow, true);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN, PLAIN, PLAIN]);
});

test('a merged pair keeps the lane the second bar would otherwise be capped out of', () => {
    // z holds the first lane throughout. With both labels drawn, b would need
    // a third lane, which the cap hides; merged, it follows a on the second.
    const engine = collapsing([
        alloc('z', 'r0', 0, 140 * MINUTE),
        alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 78 * MINUTE, 138 * MINUTE, { textStart: 'AAA' })
    ], { config: { maxStackLanes: 2 } });

    assert.deepEqual(lanesOf(engine), [0, 1, 1]);
    assert.equal(edges(engine)[2].hideStart, true);
});

test('the merge follows the zoom', () => {
    const engine = collapsing(legs(8));
    assert.equal(edges(engine)[1].hideStart, true);

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
    assert.deepEqual(lanesOf(engine), [0, 0]);

    engine._pixelsPerMs = 1 / MINUTE;
    engine._syncLanesToScale();
    assert.equal(edges(engine)[1].hideStart, true);
});

test('the merge follows the zoom with stackOnLabelCollision off', () => {
    const engine = collapsing(legs(8), { config: { stackOnLabelCollision: false } });
    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.equal(edges(engine)[1].hideStart, true, 'the duplicate goes though lanes ignore labels');

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('turning the option off stacks the pair again, and on merges it again', () => {
    const engine = collapsing(legs(8));
    assert.deepEqual(lanesOf(engine), [0, 0]);

    engine.setOptions({ collapseMatchingEdgeLabels: false });
    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);

    engine.setOptions({ collapseMatchingEdgeLabels: true });
    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.equal(edges(engine)[1].hideStart, true);
});

test('a wider label threshold undoes a merge when the first leg stops carrying labels', () => {
    const engine = collapsing(legs(8));
    assert.equal(edges(engine)[1].hideStart, true);

    engine.setOptions({ minBarWidthForLabels: 100 });
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('the option is off by default', () => {
    const engine = makeIndexedEngine(legs(8), { engine: { _pixelsPerMs: 1 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('the merge is not written onto the caller\'s allocation objects', () => {
    const [a, b] = legs(8);
    const keys = [Object.keys(a).sort(), Object.keys(b).sort()];
    collapsing([a, b]);

    assert.deepEqual([Object.keys(a).sort(), Object.keys(b).sort()], keys);
    assert.equal(a.textEnd, 'AAA');
    assert.equal(b.textStart, 'AAA');
});

// ---- scene ----

const START = Date.parse('2026-05-04T00:00:00Z');

function makeSceneEngine(allocations, pixelsPerHour = 40) {
    const engine = makeBareEngine({ config: { collapseMatchingEdgeLabels: true } });
    engine.config.timeZone = 'UTC';
    engine._rebuildDateFormatters();
    engine.resources = [{ id: 'r0', name: 'Resource 0' }];
    engine._rebuildResourceStructure();
    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine.allocations = allocations.slice().sort((a, b) => a.startTime - b.startTime);
    engine._indexAllocations();
    engine._pixelsPerHour = pixelsPerHour;
    engine._pixelsPerMs = pixelsPerHour / HOUR;
    engine._visibleWidth = 1050;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    // Lanes, and with them the merge, are rebuilt for the scale the layout uses.
    engine._syncLanesToScale();
    return engine;
}

function bar(id, startHour, endHour, extra = {}) {
    return { id, resourceId: 'r0', startTime: START + startHour * HOUR, endTime: START + endHour * HOUR, ...extra };
}

function texts(node) {
    return (node.labels || []).map(l => l.text);
}

test('a shared label is drawn once, centered on the gap, and the next bar does not repeat it', () => {
    // 40px/h: the hour between the bars is 40px, which holds the 21px word.
    const engine = makeSceneEngine([
        bar('a', 1, 3, { textStart: 'XXX', textEnd: 'AAA' }),
        bar('b', 4, 6, { textStart: 'AAA', textEnd: 'YYY' })
    ]);
    const [a, b] = engine.buildScene().bars;

    const shared = a.labels.find(l => l.text === 'AAA');
    assert.equal(shared.align, 'center');
    assert.equal(shared.x, engine.getTimeToX(START + 3.5 * HOUR));
    assert.deepEqual(texts(a), ['XXX', 'AAA']);
    assert.deepEqual(texts(b), ['YYY'], 'the next bar does not draw the code again');
    assert.equal(a.y, b.y, 'one lane');
});

test('a gap of a few pixels draws neither copy and stays on one lane', () => {
    const engine = makeSceneEngine([
        bar('a', 1, 3, { textStart: 'XXX', textEnd: 'AAA' }),
        bar('b', 3.1, 5, { textStart: 'AAA', textEnd: 'YYY' })
    ]);
    const [a, b] = engine.buildScene().bars;

    assert.deepEqual(texts(a), ['XXX']);
    assert.deepEqual(texts(b), ['YYY']);
    assert.equal(a.y, b.y);
});

test('different codes are both drawn beside their bars', () => {
    const engine = makeSceneEngine([
        bar('a', 1, 3, { textEnd: 'AAA' }),
        bar('b', 4, 6, { textStart: 'BBB' })
    ]);
    const [a, b] = engine.buildScene().bars;

    assert.deepEqual(a.labels.map(l => [l.text, l.align]), [['AAA', 'left']]);
    assert.deepEqual(b.labels.map(l => [l.text, l.align]), [['BBB', 'right']]);
});
