// Allocation indexing, stacking lanes and incremental re-indexing on edit.

import test from 'node:test';
import assert from 'node:assert/strict';

import {
    makeBareEngine, makeIndexedEngine, indexViolations, indexSnapshot,
    rng, genAllocations
} from './helpers/engine-fixture.mjs';

const RESOURCES = ['r0', 'r1', 'r2', 'r3'];

function alloc(id, resourceId, startTime, endTime, extra = {}) {
    return { id, resourceId, startTime, endTime, ...extra };
}

// Height of a row holding two undecorated bars stacked barMargin apart: the
// baseline the label clearance is measured against.
function tightRowHeight(c) {
    return 2 * c.barHeight + c.barMargin + (c.resourceHeight - c.barHeight);
}

test('index groups allocations by resource, sorted by start time', () => {
    const engine = makeIndexedEngine([
        alloc('c', 'r1', 300, 400),
        alloc('a', 'r1', 100, 200),
        alloc('b', 'r2', 150, 250)
    ]);

    assert.deepEqual(engine.allocationsByResource.get('r1').items.map(a => a.id), ['a', 'c']);
    assert.deepEqual(engine.allocationsByResource.get('r2').items.map(a => a.id), ['b']);
    assert.deepEqual(indexViolations(engine), []);
});

test('row scan bounds cover the widest effective span in that row only', () => {
    const engine = makeIndexedEngine([
        // A very long bar in r0 must not widen r1's bounds.
        alloc('long', 'r0', 0, 1000000),
        alloc('short', 'r1', 500, 600, { startBar: { duration: 50 } })
    ]);

    assert.equal(engine.allocationsByResource.get('r0').maxSpanMs, 1000000);
    assert.equal(engine.allocationsByResource.get('r1').maxSpanMs, 150);
    assert.equal(engine.allocationsByResource.get('r1').maxStartEdgeMs, 50);
    assert.equal(engine.allocationsByResource.get('r0').maxStartEdgeMs, 0);
});

test('_firstVisibleAllocationIndex never skips an allocation that is in view', () => {
    const rand = rng(7);
    const allocations = genAllocations(rand, 300, ['r0']);
    const engine = makeIndexedEngine(allocations);
    const row = engine.allocationsByResource.get('r0');

    for (let visStart = 0; visStart < 1000000; visStart += 9973) {
        const first = engine._firstVisibleAllocationIndex(row, visStart);
        // Everything before the returned index must end before the window.
        for (let i = 0; i < first; i++) {
            const a = row.items[i];
            const ee = a.endBar && a.endBar.duration > 0 ? a.endBar.duration : 0;
            assert.ok(a.endTime + ee < visStart,
                `allocation ${a.id} ends at ${a.endTime + ee} but was skipped for window ${visStart}`);
        }
    }
});

test('non-overlapping bars all sit on the row centre line', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 100, 200),   // touching, not overlapping
        alloc('c', 'r0', 300, 400)
    ]);
    for (const a of engine.allocations) {
        assert.equal(engine._stackOffset(a), 0);
    }
});

test('overlapping bars are stacked into distinct lanes around the centre', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150),
        alloc('c', 'r0', 60, 160)
    ]);
    const offsets = engine.allocations.map(a => engine._stackOffset(a));
    assert.equal(new Set(offsets).size, 3, 'three overlapping bars need three lanes');
    // Symmetric about the row centre.
    assert.ok(Math.abs(offsets.reduce((s, o) => s + o, 0)) < 1e-9);
});

test('rows grow so stacked overlapping bars stay inside with consistent padding', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150),
        alloc('c', 'r0', 60, 160),
        // A sibling resource with no overlaps stays at the minimum height.
        alloc('d', 'r1', 0, 100)
    ]);
    const c = engine.config;
    const stackH = 3 * c.barHeight + 2 * c.barMargin;
    const padTotal = c.resourceHeight - c.barHeight;
    const expectedTall = stackH + padTotal;

    assert.equal(engine._rowHeight(0), expectedTall);
    assert.equal(engine._rowHeight(1), c.resourceHeight);

    // Outer lane centres must stay within the row band (same padding as a
    // single default bar would have in a minimum-height row).
    const half = expectedTall / 2;
    const pad = padTotal / 2;
    for (const a of engine.allocations.filter(x => x.resourceId === 'r0')) {
        const barH = c.barHeight;
        const top = engine._stackOffset(a) - barH / 2;
        const bottom = engine._stackOffset(a) + barH / 2;
        assert.ok(top >= -half + pad - 1e-9, 'stack must not breach top padding');
        assert.ok(bottom <= half - pad + 1e-9, 'stack must not breach bottom padding');
    }
});

test('visible-row culling uses cumulative heights, not index * resourceHeight', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150),
        alloc('c', 'r0', 60, 160),
        alloc('d', 'r1', 0, 100),
        alloc('e', 'r2', 0, 100)
    ]);
    // Viewport shows only the top of the first (tall) row.
    engine._viewportH = engine.config.timeAxisHeight + 10;
    engine.scrollY = 0;
    const { start, end } = engine._visibleRowWindow(0);
    assert.equal(start, 0);
    assert.ok(end >= 1 && end <= 2, 'only the first tall row (plus pad) should be in view');

    // Scroll past the tall row into r1.
    engine.scrollY = engine._rowHeight(0);
    const mid = engine._visibleRowWindow(0);
    assert.equal(mid.start, 1);
    assert.ok(mid.end >= 2);
});

test('a fully overlapping row assigns one lane per bar', () => {
    // The degenerate case the lane-end lower bound short-circuits.
    const allocations = [];
    for (let i = 0; i < 200; i++) allocations.push(alloc('a' + i, 'r0', i, 100000));
    const engine = makeIndexedEngine(allocations);
    const lanes = new Set(engine.allocations.map(a => engine._laneInfo.get(a).lane));
    assert.equal(lanes.size, 200);
});

test('stacking offsets follow barHeight/barMargin changes', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150)
    ]);
    const before = engine.allocations.map(a => engine._stackOffset(a));

    engine.config.barMargin = 20;
    engine._barLayoutGen++;
    const after = engine.allocations.map(a => engine._stackOffset(a));

    assert.notDeepEqual(before, after, 'a larger margin must spread the lanes apart');
    assert.ok(Math.abs(after[0] - after[1]) > Math.abs(before[0] - before[1]));
});

test('stacked bars leave room between them for the labels they carry', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100, { textAbove: 'A', textBelow: '1h' }),
        alloc('b', 'r0', 50, 150, { textAbove: 'B', textBelow: '1h' })
    ]);
    const c = engine.config;
    // Lower bound on what one label needs beside a bar: its gap plus the font's
    // pixel size. The real line box is a little taller, so asserting against
    // this checks that the text fits rather than restating the engine's maths.
    const labelRoom = c.barLabelGap + parseFloat(c.barLabelFont);
    const [first, second] = engine.allocations.map(a => engine._stackOffset(a));
    const between = Math.abs(second - first) - c.barHeight;

    // The upper bar's textBelow and the lower bar's textAbove both live in the
    // space between the two bars, so neither is drawn over a bar.
    assert.ok(between >= 2 * labelRoom + c.barMargin,
        `two stacked labels need ${2 * labelRoom + c.barMargin}px between the bars, got ${between}`);

    // The row grew by exactly that extra room: the outermost bars still sit on
    // the padding a single default bar has in a minimum-height row, which is
    // where their own outward-facing labels are drawn.
    const half = engine._rowHeight(0) / 2;
    const pad = (c.resourceHeight - c.barHeight) / 2;
    assert.equal(Math.min(first, second) - c.barHeight / 2, -half + pad);
    assert.equal(Math.max(first, second) + c.barHeight / 2, half - pad);
});

test('clearance is reserved only on the side carrying the decoration', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150, { textBelow: 'below' })
    ]);
    const c = engine.config;
    const [first, second] = engine.allocations.map(a => engine._stackOffset(a));

    // The label hangs below the lower lane, into the padding a single bar's
    // label would use, so the lanes themselves need not move apart.
    assert.equal(Math.abs(second - first), c.barHeight + c.barMargin);
    assert.equal(engine._rowHeight(0), tightRowHeight(c));
});

test('an icon above a stacked bar reserves its box, loaded or not', () => {
    // Icons are measured by their box rather than their aspect-fitted natural
    // size, so a stack's geometry does not shift as images arrive.
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 50, 150, { icons: [{ source: 'i.png', position: 'above', size: 20 }] })
    ]);
    const c = engine.config;
    const [first, second] = engine.allocations.map(a => engine._stackOffset(a));

    assert.equal(Math.abs(second - first) - c.barHeight, c.barMargin + c.barLabelGap + 20);
});

test('turning stackLabelClearance off collapses the stack again', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 100, { textAbove: 'A' }),
        alloc('b', 'r0', 50, 150, { textAbove: 'B' })
    ]);
    engine.render = () => {};
    engine._syncAxisSplitterChrome = () => {};
    const c = engine.config;
    assert.ok(engine._rowHeight(0) > tightRowHeight(c), 'clearance is on by default');

    engine.setOptions({ stackLabelClearance: false });

    const [first, second] = engine.allocations.map(a => engine._stackOffset(a));
    assert.equal(Math.abs(second - first), c.barHeight + c.barMargin);
    assert.equal(engine._rowHeight(0), tightRowHeight(c));
});

// The arrangement from the bug report: two bars a few minutes apart, each wide
// enough to carry labels, with their start/end times drawn into the gap between
// them. At one pixel per minute the gap is 8px and each label is 35px wide.
const MINUTE = 60000;
const NEIGHBOURS = [
    alloc('a', 'r0', 0, 60 * MINUTE, { textStart: '13:03', textEnd: '14:00' }),
    alloc('b', 'r0', 68 * MINUTE, 128 * MINUTE, { textStart: '15:12', textEnd: '16:12' })
];

function lanesOf(engine) {
    return engine.allocations.map(a => engine._laneInfo.get(a).lane);
}

test('bars whose labels collide are stacked though their times do not overlap', () => {
    const engine = makeIndexedEngine(NEIGHBOURS, { engine: { _pixelsPerMs: 1 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 1], 'the labels do not fit beside each other');
    const [first, second] = engine.allocations.map(a => engine._stackOffset(a));
    assert.ok(first < second, 'the second bar belongs below the first');
    assert.ok(engine._rowHeight(0) > engine.config.resourceHeight, 'the row grows for the stack');
});

test('zooming in until the labels fit puts the bars back on one lane', () => {
    const engine = makeIndexedEngine(NEIGHBOURS, { engine: { _pixelsPerMs: 10 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 0]);
    for (const a of engine.allocations) assert.equal(engine._stackOffset(a), 0);
    assert.equal(engine._rowHeight(0), engine.config.resourceHeight);
});

test('bars too narrow to carry labels claim no room when zoomed out', () => {
    // Below minBarWidthForLabels nothing is drawn around a bar, so a zoomed-out
    // row collapses back to one lane instead of stacking every bar in it.
    const engine = makeIndexedEngine(NEIGHBOURS, { engine: { _pixelsPerMs: 0.1 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.equal(engine._rowHeight(0), engine.config.resourceHeight);
});

test('delay bars count towards a collision even with no labels', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 0, 60 * MINUTE, { endBar: { duration: 30 * MINUTE } }),
        alloc('b', 'r0', 70 * MINUTE, 130 * MINUTE, { startBar: { duration: 30 * MINUTE } })
    ], { engine: { _pixelsPerMs: 1 / MINUTE } });

    assert.deepEqual(lanesOf(engine), [0, 1]);
});

test('a scale change reassigns the lanes, an unchanged one does not', () => {
    const engine = makeIndexedEngine(NEIGHBOURS, { engine: { _pixelsPerMs: 10 / MINUTE } });
    assert.deepEqual(lanesOf(engine), [0, 0]);

    const before = engine._laneInfo.get(engine.allocations[0]).cluster;
    engine._syncLanesToScale();
    assert.equal(engine._laneInfo.get(engine.allocations[0]).cluster, before,
        'the same scale must not rebuild the lane records');

    engine._pixelsPerMs = 1 / MINUTE;
    engine._syncLanesToScale();
    assert.deepEqual(lanesOf(engine), [0, 1]);
    assert.ok(engine._rowHeight(0) > engine.config.resourceHeight);
});

test('stackOnLabelCollision off stacks on a time overlap alone', () => {
    const engine = makeIndexedEngine(NEIGHBOURS, {
        config: { stackOnLabelCollision: false },
        engine: { _pixelsPerMs: 1 / MINUTE }
    });

    assert.deepEqual(lanesOf(engine), [0, 0]);
    assert.equal(engine._rowHeight(0), engine.config.resourceHeight);
});

test('lane state is not written onto the caller\'s allocation objects', () => {
    const a = alloc('a', 'r0', 0, 100);
    const b = alloc('b', 'r0', 50, 150);
    makeIndexedEngine([a, b]);
    for (const obj of [a, b]) {
        assert.deepEqual(
            Object.keys(obj).sort(),
            ['endTime', 'id', 'resourceId', 'startTime'],
            'the engine must not decorate host-owned allocation objects'
        );
    }
});

test('incremental re-index matches a full rebuild across random edits', () => {
    for (let seed = 1; seed <= 60; seed++) {
        const rand = rng(seed);
        const engine = makeIndexedEngine(genAllocations(rand, 40, RESOURCES));

        for (let step = 0; step < 20; step++) {
            const target = engine.allocations[Math.floor(rand() * engine.allocations.length)];
            const prevResourceId = target.resourceId;
            const prevStartTime = target.startTime;

            const kind = rand();
            if (kind < 0.4) {
                const delta = Math.floor((rand() - 0.5) * 200000);
                target.startTime += delta;
                target.endTime += delta;
            } else if (kind < 0.7) {
                target.endTime = target.startTime + 1 + Math.floor(rand() * 80000);
            } else if (kind < 0.85) {
                target.startTime = target.endTime - 1 - Math.floor(rand() * 80000);
            } else {
                target.resourceId = RESOURCES[Math.floor(rand() * RESOURCES.length)];
            }

            engine._reindexAllocation(target, prevResourceId, prevStartTime);

            assert.deepEqual(indexViolations(engine), [],
                `seed ${seed} step ${step}: index invariants violated`);

            // Same allocations, rebuilt from scratch, must produce the same
            // order, lanes and offsets.
            const rebuilt = makeIndexedEngine(engine.allocations.map(a => ({ ...a })));
            assert.deepEqual(indexSnapshot(engine), indexSnapshot(rebuilt),
                `seed ${seed} step ${step}: incremental index diverged from a full rebuild`);
        }
    }
});

test('editing an allocation onto a resource with no allocations yet works', () => {
    const engine = makeIndexedEngine([alloc('a', 'r0', 100, 200)]);
    const target = engine.allocations[0];
    target.resourceId = 'brand-new';
    engine._reindexAllocation(target, 'r0', 100);

    assert.deepEqual(engine.allocationsByResource.get('brand-new').items.map(a => a.id), ['a']);
    assert.deepEqual(engine.allocationsByResource.get('r0').items, []);
    assert.deepEqual(indexViolations(engine), []);
});

test('allocations with identical start times are re-indexed individually', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 100, 300),
        alloc('c', 'r0', 100, 400)
    ]);
    const target = engine.allocations.find(a => a.id === 'b');
    target.startTime = 500;
    target.endTime = 600;
    engine._reindexAllocation(target, 'r0', 100);

    assert.deepEqual(engine.allocationsByResource.get('r0').items.map(a => a.id), ['a', 'c', 'b']);
    assert.deepEqual(indexViolations(engine), []);
});

test('empty resources return a usable empty row index', () => {
    const engine = makeBareEngine();
    const row = engine._rowIndexFor('nobody');
    assert.deepEqual(row.items, []);
    assert.equal(row.maxSpanMs, 0);
    assert.equal(engine._firstVisibleAllocationIndex(row, 12345), 0);
});

test('upsert one bar keeps an untouched id in the selection', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 300, 400)
    ]);
    engine._relayout = () => {};
    engine.selectedBars.add('a');
    engine.selectedBars.add('b');

    engine.upsertAllocations([alloc('b', 'r0', 300, 500)]);

    assert.ok(engine.selectedBars.has('a'));
    assert.ok(engine.selectedBars.has('b'));
    assert.equal(engine.allocations.find(x => x.id === 'b').endTime, 500);
    assert.deepEqual(indexViolations(engine), []);
});

test('remove of the focused id clears focus only', () => {
    const engine = makeIndexedEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 300, 400)
    ]);
    engine._relayout = () => {};
    engine.selectedBars.add('a');
    engine.selectedBars.add('b');
    engine._focusAlloc = engine.allocations.find(x => x.id === 'a');

    engine.removeAllocations(['a']);

    assert.equal(engine._focusAlloc, null);
    assert.ok(engine.selectedBars.has('b'));
    assert.equal(engine.selectedBars.has('a'), false);
    assert.equal(engine.allocations.some(x => x.id === 'a'), false);
    assert.deepEqual(indexViolations(engine), []);
});

test('sanitize drops inverted ranges and empty ids', () => {
    const engine = makeIndexedEngine([alloc('ok', 'r0', 0, 50)]);
    const warnings = [];
    const realWarn = console.warn;
    console.warn = (m) => warnings.push(m);
    try {
        const cleaned = engine._sanitizeAllocations([
            alloc('ok', 'r0', 0, 50),
            alloc('bad', 'r0', 200, 100),
            { id: '', resourceId: 'r0', startTime: 0, endTime: 10 },
            alloc('ok', 'r0', 10, 80)
        ]);
        assert.deepEqual(cleaned.map(a => a.id), ['ok']);
        assert.equal(cleaned[0].startTime, 10);
        engine.allocations = cleaned;
        engine._indexAllocations();
        assert.deepEqual(indexViolations(engine), []);
    } finally {
        console.warn = realWarn;
    }
});

test('MaxStackLanes 3 on 10 overlapping bars yields 3 lanes and overflow 7', () => {
    const bars = [];
    for (let i = 0; i < 10; i++) bars.push(alloc('a' + i, 'r0', 0, 100));
    const engine = makeIndexedEngine(bars, { config: { maxStackLanes: 3 } });
    const lanes = new Set();
    let overflow = 0;
    for (const a of engine.allocations) {
        const info = engine._laneInfo.get(a);
        if (info && info.overflow) overflow++;
        else lanes.add(info.lane);
    }
    assert.equal(lanes.size, 3);
    assert.equal(overflow, 7);
});

// An engine in the windowed mode of session 7, as beginWindowed leaves it.
function makeWindowedEngine(allocations) {
    const engine = makeIndexedEngine(allocations);
    engine._relayouts = 0;
    engine._relayout = () => { engine._relayouts++; };
    engine.render = () => {};
    engine._hideTooltip = () => {};
    engine._windowed = true;
    engine._windowSession = 7;
    engine._windowAppliedId = -1;
    engine._windowRequestId = 1;
    return engine;
}

test('windowed apply keeps overlapping ids and object identity', () => {
    const a1 = alloc('1', 'r0', 0, 50);
    const a2 = alloc('2', 'r0', 80, 150);
    const engine = makeWindowedEngine([a1, a2]);
    engine.selectedBars.add('2');
    engine._focusAlloc = a2;

    // The window re-delivers 2 as a fresh object with new times, as a refetch after an edit on the server does.
    const a3 = alloc('3', 'r0', 140, 200);
    engine.applyAllocationWindow(1, [{ ...a2, startTime: 90, endTime: 170 }, a3], 80, 200, 7);

    assert.equal(engine.allocations.some(a => a.id === '1'), false);
    assert.equal(engine.allocations.find(a => a.id === '2'), a2);
    assert.equal(a2.startTime, 90, 'the kept object takes the new start');
    assert.equal(a2.endTime, 170, 'the kept object takes the new end');
    assert.ok(engine.allocations.some(a => a.id === '3'));
    assert.ok(engine.selectedBars.has('2'));
    assert.equal(engine._focusAlloc, a2);
    assert.deepEqual(indexViolations(engine), []);
});

test('windowed apply drops a bar the payload no longer holds, even where it overlaps the window', () => {
    // The app returns every allocation overlapping the window, so one missing from it was deleted or moved away.
    const kept = alloc('kept', 'r0', 100, 150);
    const gone = alloc('gone', 'r0', 120, 180);
    const engine = makeWindowedEngine([kept, gone]);
    engine.selectedBars.add('gone');
    let notified = 0;
    engine._notifySelection = () => { notified++; };

    engine.applyAllocationWindow(1, [{ ...kept }], 0, 1000, 7);

    assert.deepEqual(engine.allocations.map(a => a.id), ['kept']);
    assert.equal(engine.allocations[0], kept);
    assert.equal(engine.selectedBars.has('gone'), false);
    assert.equal(notified, 1);
    assert.deepEqual(indexViolations(engine), []);
});

test('windowed apply ignores a window of another session, whatever its id', () => {
    const engine = makeWindowedEngine([alloc('current', 'r0', 0, 50)]);

    // A fetch of the previous Config, still running when this session began: its id outranks the new session's.
    engine.applyAllocationWindow(5, [alloc('stale', 'r0', 0, 50)], 0, 1000, 6);
    assert.deepEqual(engine.allocations.map(a => a.id), ['current']);
    assert.equal(engine._windowAppliedId, -1, 'the stale window does not outrank the new session\'s ones');

    engine.applyAllocationWindow(0, [alloc('fresh', 'r0', 0, 50)], 0, 1000, 7);
    assert.deepEqual(engine.allocations.map(a => a.id), ['fresh']);

    // Nor is a window applied once the engine left the windowed mode.
    engine._windowed = false;
    engine.applyAllocationWindow(2, [alloc('late', 'r0', 0, 50)], 0, 1000, 7);
    assert.deepEqual(engine.allocations.map(a => a.id), ['fresh']);
});

test('windowed apply lays the rows out again, since stacking the bars changes their heights', () => {
    const engine = makeWindowedEngine([]);
    engine._rows = [{ resource: { id: 'r0', name: 'r0' }, depth: 0, hasChildren: false }];
    engine._rowIndexById = new Map([['r0', 0]]);

    engine.applyAllocationWindow(0, [alloc('a', 'r0', 0, 100), alloc('b', 'r0', 0, 100)], 0, 1000, 7);

    assert.equal(engine._relayouts, 1);
    assert.ok(engine._rowHeight(0) > engine.config.resourceHeight, 'the two overlapping bars stack');
});

test('the window a fetch is needed for never reaches past what one loads', () => {
    const engine = makeWindowedEngine([]);
    engine.timeRange = { start: 0, end: 100 * 3600000 };
    engine._pixelsPerMs = 50 / 3600000;
    engine.scrollX = 2000;
    engine.config.windowBufferFactor = 0.5;
    engine.config.windowRefetchThreshold = 2;

    // A threshold above the buffer would leave every fetched window short of the range it was needed for, and ask for
    // the next one at once.
    const [ns, ne] = engine._windowNeededRange();
    const [fs, fe] = engine._windowFetchRange();
    assert.ok(fs <= ns && fe >= ne);
});
