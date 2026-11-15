// collapseMatchingEdgeLabels: two neighbouring bars whose facing labels match
// (the first one's end, the next one's start - the airport between two legs)
// draw that text once, and it does not stack them.
//
// The fixture measures 7px per character, so 'AAA' is 21px wide, and the label
// gap is 3px. A pair is merged while the clear space between the bars holds the
// word with a gap either side, 21 + 2 * 3 = 27px, and is still under
// 21 + 3 + 21 + 3 + 21 = 69px; the one label left is drawn centered in it.

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
        return { hideStart: s.hideStart, shareEnd: s.shareEnd };
    });
}

const PLAIN = { hideStart: false, shareEnd: false };
const SHARED = { hideStart: false, shareEnd: true };
const HIDDEN = { hideStart: true, shareEnd: false };

test('a gap the word fits in draws it once, centered between the bars', () => {
    const engine = collapsing(legs(40));
    const [a, b] = engine.allocations;

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);
    assert.equal(engine._edgeLabelState(a).anchorTime, (a.endTime + b.startTime) / 2);
});

test('a gap too narrow for the word merges nothing, and the pair stacks as it would without the option', () => {
    const engine = collapsing(legs(8));

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('touching bars draw both labels, stacked, rather than neither', () => {
    const engine = collapsing(legs(0));

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('a gap wide enough for both labels and a word between leaves them alone', () => {
    const engine = collapsing(legs(80));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('different labels still stack', () => {
    const engine = collapsing(legs(40, 'AAA', 'BBB'));

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('placeholders are not a match', () => {
    const placeholders = ['—', '–', '-', '--', '—-', '…', '...', '?', '???', '*', 'N/A', 'n/a', 'NA', 'TBD', 'tba', 'TBC', 'Unknown', 'none', 'NULL'];
    for (const placeholder of placeholders) {
        // A gap a real word of the same length would be merged in.
        const width = 7 * placeholder.length;
        const engine = collapsing(legs(2 * width + 6, placeholder, ` ${placeholder} `));
        assert.deepEqual(edges(engine), [PLAIN, PLAIN], `'${placeholder}' must not collapse`);
    }

    // The same gap does merge a code of the same length.
    const engine = collapsing(legs(2 * 21 + 6, 'CDG', 'CDG'));
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);
});

test('labels match trimmed and ignoring case', () => {
    const engine = collapsing(legs(40, 'aaa', ' AAA '));

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);
});

test('bars overlapping in time stack and keep both labels', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 60 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 50 * MINUTE, 110 * MINUTE, { textStart: 'AAA' })
    ]);

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('the second leg\'s own end label is kept, even when it repeats the code', () => {
    const engine = collapsing(legs(40, 'AAA', 'AAA', {}, { textEnd: 'AAA' }));

    assert.equal(edges(engine)[1].shareEnd, false);
});

test('a chain of legs merges every turn and stays on one lane', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 60 * MINUTE, { textStart: 'AMS', textEnd: 'CDG' }),
        alloc('b', 'r0', 100 * MINUTE, 160 * MINUTE, { textStart: 'CDG', textEnd: 'AMS' }),
        alloc('c', 'r0', 200 * MINUTE, 260 * MINUTE, { textStart: 'AMS', textEnd: 'LHR' })
    ]);

    assert.deepEqual(lanesOf(engine), [0, 0, 0]);
    assert.deepEqual(edges(engine), [SHARED, { hideStart: true, shareEnd: true }, HIDDEN]);
});

test('delay bars narrow the gap the word has to fit in', () => {
    // 40px between the bars, of which the delay bars take 20: 20px is too few.
    const narrow = collapsing(legs(40, 'AAA', 'AAA',
        { endBar: { duration: 10 * MINUTE } }, { startBar: { duration: 10 * MINUTE } }));
    assert.deepEqual(edges(narrow), [PLAIN, PLAIN]);

    // 60px less 20 leaves 40, centered between the delay bars.
    const wide = collapsing(legs(60, 'AAA', 'AAA',
        { endBar: { duration: 10 * MINUTE } }, { startBar: { duration: 10 * MINUTE } }));
    assert.deepEqual(edges(wide), [SHARED, HIDDEN]);
    assert.deepEqual(lanesOf(wide), [0, 0]);
    assert.equal(wide._edgeLabelState(wide.allocations[0]).anchorTime, 90 * MINUTE);
});

test('edge icons narrow the gap the word has to fit in', () => {
    // An end icon of 16px pushes the label 19px out: 40 - 19 = 21px is too few.
    const icon = { icons: [{ source: 'i.png', position: 'end', size: 16 }] };
    assert.deepEqual(edges(collapsing(legs(40, 'AAA', 'AAA', icon))), [PLAIN, PLAIN]);

    // 60 - 19 = 41px, centered between the icon and the next bar.
    const wide = collapsing(legs(60, 'AAA', 'AAA', icon));
    assert.deepEqual(edges(wide), [SHARED, HIDDEN]);
    assert.equal(wide._edgeLabelState(wide.allocations[0]).anchorTime, (79 + 120) / 2 * MINUTE);
});

test('a first leg too narrow to carry labels merges nothing', () => {
    const engine = collapsing([
        alloc('a', 'r0', 0, 20 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 60 * MINUTE, 120 * MINUTE, { textStart: 'AAA' })
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
    assert.deepEqual(edges(engine), [SHARED, PLAIN]);
});

test('a pair a third bar overlaps stays on one lane', () => {
    // z overlaps a, so a takes the second lane; b, free to take the first,
    // follows a instead, so the label drawn once beside a is on b's line too.
    for (const stackOnLabelCollision of [true, false]) {
        const engine = collapsing([
            alloc('z', 'r0', 0, 65 * MINUTE),
            alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
            alloc('b', 'r0', 110 * MINUTE, 170 * MINUTE, { textStart: 'AAA' })
        ], { config: { stackOnLabelCollision } });
        const [, a, b] = engine.allocations;

        assert.deepEqual(lanesOf(engine), [0, 1, 1], `stackOnLabelCollision ${stackOnLabelCollision}`);
        assert.equal(engine._laneInfo.get(a).cluster, engine._laneInfo.get(b).cluster);
        assert.deepEqual(edges(engine), [PLAIN, SHARED, HIDDEN], `stackOnLabelCollision ${stackOnLabelCollision}`);
    }
});

test('a pair the lane cap hides one of draws its labels as usual', () => {
    // z and w fill both lanes when a starts, so the cap hides a behind a +N
    // marker; b, starting once they have ended, is drawn with its own label.
    const engine = collapsing([
        alloc('z', 'r0', 0, 75 * MINUTE),
        alloc('w', 'r0', 5 * MINUTE, 72 * MINUTE),
        alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 110 * MINUTE, 170 * MINUTE, { textStart: 'AAA' })
    ], { config: { maxStackLanes: 2 } });

    assert.equal(engine._laneInfo.get(engine.allocations[2]).overflow, true);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN, PLAIN, PLAIN]);
    assert.notEqual(engine._laneInfo.get(engine.allocations[3]).overflow, true);
});

test('a merged pair keeps the lane the second bar would otherwise be capped out of', () => {
    // z holds the first lane throughout. With both labels drawn, b would need
    // a third lane, which the cap hides; merged, it follows a on the second.
    const engine = collapsing([
        alloc('z', 'r0', 0, 200 * MINUTE),
        alloc('a', 'r0', 10 * MINUTE, 70 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 110 * MINUTE, 170 * MINUTE, { textStart: 'AAA' })
    ], { config: { maxStackLanes: 2 } });

    assert.deepEqual(lanesOf(engine), [0, 1, 1]);
    assert.deepEqual(edges(engine)[2], HIDDEN);
});

test('the +N marker of the first leg\'s cluster sits past the second leg, not on the shared label', () => {
    // The cap hides w in y's cluster, which a joins. b, merged with a, joins it
    // too, so the cluster - and its marker - ends past b rather than in the gap
    // the shared label is drawn in.
    const engine = collapsing([
        alloc('y', 'r0', 0, 50 * MINUTE),
        alloc('w', 'r0', 5 * MINUTE, 20 * MINUTE),
        alloc('a', 'r0', 50 * MINUTE, 110 * MINUTE, { textEnd: 'AAA' }),
        alloc('b', 'r0', 150 * MINUTE, 210 * MINUTE, { textStart: 'AAA' })
    ], { config: { maxStackLanes: 1 } });
    const [, w, a, b] = engine.allocations;
    const cluster = engine._laneInfo.get(a).cluster;

    assert.equal(engine._laneInfo.get(w).overflow, true);
    assert.deepEqual(edges(engine)[3], HIDDEN);
    assert.equal(engine._laneInfo.get(b).cluster, cluster);
    assert.ok(cluster.trailEnd >= b.endTime);
});

test('the merge follows the zoom', () => {
    const engine = collapsing(legs(40));
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
    assert.deepEqual(lanesOf(engine), [0, 0]);

    engine._pixelsPerMs = 1 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);
});

test('the merge follows the zoom with stackOnLabelCollision off', () => {
    const engine = collapsing(legs(40), { config: { stackOnLabelCollision: false } });
    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [SHARED, HIDDEN], 'the duplicate goes though lanes ignore labels');

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('with stackOnLabelCollision off, a zoom redoes only the rows with a pair to merge', () => {
    const engine = collapsing([
        ...legs(40),
        alloc('c', 'r1', 0, 60 * MINUTE, { textEnd: 'AAA' }),
        alloc('d', 'r1', 100 * MINUTE, 160 * MINUTE, { textStart: 'BBB' })
    ], { config: { stackOnLabelCollision: false } });
    const restacked = [];
    const assign = engine._assignStackLanes;
    engine._assignStackLanes = function (list, row) {
        restacked.push(list[0].resourceId);
        return assign.call(this, list, row);
    };

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(restacked, ['r0']);
    assert.equal(engine._laneScale, engine._pixelsPerMs);

    // Nothing at all to redo once no row has a pair.
    engine.allocationsByResource.get('r0').edgePairs = false;
    restacked.length = 0;
    let reported = 0;
    engine._reportResourceRows = () => { reported++; };
    engine._pixelsPerMs = 2 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(restacked, []);
    assert.equal(reported, 0);
});

test('turning the option off stacks the pair again, and on merges it again', () => {
    const engine = collapsing(legs(40));
    assert.deepEqual(lanesOf(engine), [0, 0]);

    engine.setOptions({ collapseMatchingEdgeLabels: false });
    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);

    engine.setOptions({ collapseMatchingEdgeLabels: true });
    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);
});

test('a wider label threshold undoes a merge when the first leg stops carrying labels', () => {
    const engine = collapsing(legs(40));
    assert.deepEqual(edges(engine), [SHARED, HIDDEN]);

    engine.setOptions({ minBarWidthForLabels: 100 });
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('the option is off by default', () => {
    const engine = makeIndexedEngine(legs(40), { engine: { _pixelsPerMs: 1 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.deepEqual(edges(engine), [PLAIN, PLAIN]);
});

test('the merge is not written onto the caller\'s allocation objects', () => {
    const [a, b] = legs(40);
    const keys = [Object.keys(a).sort(), Object.keys(b).sort()];
    collapsing([a, b]);

    assert.deepEqual([Object.keys(a).sort(), Object.keys(b).sort()], keys);
    assert.equal(a.textEnd, 'AAA');
    assert.equal(b.textStart, 'AAA');
});

test('the decoration box does not change with the merge', () => {
    const engine = collapsing(legs(40));
    const [a, b] = engine.allocations;
    const boxA = engine._decorationBox(a);
    const boxB = engine._decorationBox(b);

    engine._pixelsPerMs = 10 / MINUTE;
    engine._syncLanesToScale();
    assert.equal(engine._decorationBox(a), boxA);
    assert.equal(engine._decorationBox(b), boxB);
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

test('the shared label stays when the first bar is scrolled out of view', () => {
    const engine = makeSceneEngine([
        bar('a', 1, 3, { textStart: 'XXX', textEnd: 'AAA' }),
        bar('b', 4, 6, { textStart: 'AAA', textEnd: 'YYY' })
    ]);
    // a ends 10px left of the content area; the gap's middle is 10px into it.
    engine.scrollX = 130;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    const bars = engine.buildScene().bars;

    assert.deepEqual(bars.map(n => n.id), ['b']);
    const shared = bars[0].labels.find(l => l.text === 'AAA');
    assert.ok(shared, 'the code is still drawn');
    assert.equal(shared.align, 'center');
    assert.equal(shared.x, engine.getTimeToX(START + 3.5 * HOUR));
    assert.equal(bars[0].labels.filter(l => l.text === 'AAA').length, 1);
});

test('a gap of a few pixels draws both labels, stacked', () => {
    const engine = makeSceneEngine([
        bar('a', 1, 3, { textStart: 'XXX', textEnd: 'AAA' }),
        bar('b', 3.1, 5, { textStart: 'AAA', textEnd: 'YYY' })
    ]);
    const [a, b] = engine.buildScene().bars;

    assert.deepEqual(texts(a), ['XXX', 'AAA']);
    assert.deepEqual(texts(b), ['AAA', 'YYY']);
    assert.notEqual(a.y, b.y);
});

test('a first bar right at the label threshold draws the label it shares', () => {
    // 24px wide at 40px/h is 36 minutes: exactly minBarWidthForLabels.
    const engine = makeSceneEngine([
        bar('a', 1, 1.6, { textEnd: 'AAA' }),
        bar('b', 2.6, 4, { textStart: 'AAA' })
    ]);
    const [a, b] = engine.buildScene().bars;

    assert.deepEqual(texts(a), ['AAA']);
    assert.deepEqual(texts(b), []);
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
