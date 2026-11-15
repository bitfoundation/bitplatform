// Group separators (Options.GroupSeparators): where they go in the resource
// hierarchy, the room they take between the rows, what a point over one hits,
// the strips the scene paints them with, and the gap the rows reported to the
// ResourceTemplate overlay leave for them.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');

function res(id, parentId, extra = {}) {
    return parentId == null ? { id, name: id, ...extra } : { id, name: id, parentId, ...extra };
}

// Two regions of sites and machines, two leaves between them, a collapsed
// region and a last leaf. The visible rows, by index:
//    0 A (d0, group)    1 D1 (d1, group)   2 S1 (d2)   3 S2 (d2)
//    4 D2 (d1, group)   5 S3 (d2)          6 B (d0, group)          7 S4 (d1)
//    8 L1 (d0)          9 L2 (d0)          10 C (d0, collapsed)     11 L3 (d0)
const HIERARCHY = [
    res('A'), res('D1', 'A'), res('S1', 'D1'), res('S2', 'D1'), res('D2', 'A'), res('S3', 'D2'),
    res('B'), res('S4', 'B'),
    res('L1'), res('L2'),
    res('C', null, { collapsed: true }), res('S5', 'C'),
    res('L3')
];

function makeEngine(groupSeparators, extraConfig = {}) {
    const engine = makeBareEngine({ config: { groupSeparators, timeZone: 'UTC', ...extraConfig } });
    engine._rebuildDateFormatters();
    engine.resources = HIERARCHY.map(r => ({ ...r }));
    engine._rebuildResourceStructure();
    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine._pixelsPerHour = 40;
    engine._pixelsPerMs = 40 / HOUR;
    // Tall enough for every row and separator, so the scene culls none of them.
    engine._viewportH = 1200;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    return engine;
}

function separatedRows(engine) {
    return engine._rowGaps.map((gap, i) => (gap > 0 ? i : -1)).filter(i => i >= 0);
}

test('the rows are the hierarchy the separator rules are checked against', () => {
    const engine = makeEngine(null);
    assert.deepEqual(engine._rows.map(r => r.resource.id),
        ['A', 'D1', 'S1', 'S2', 'D2', 'S3', 'B', 'S4', 'L1', 'L2', 'C', 'L3']);
});

test('a separator goes where a top-level group begins or ends', () => {
    const engine = makeEngine({ height: 2 });
    // B after A's subtree, L1 after B's, C (a group) after the leaf L2, and L3
    // after the collapsed C. Not between the two leaves L1 and L2, not between a
    // group's row and its first child, and not inside A (D2 is at depth 1).
    assert.deepEqual(separatedRows(engine), [6, 8, 10, 11]);
});

test('maxDepth separates the groups of deeper levels too', () => {
    const engine = makeEngine({ height: 2, maxDepth: 1 });
    // D2 now as well: it follows D1's subtree. Still nothing under a parent row.
    assert.deepEqual(separatedRows(engine), [4, 6, 8, 10, 11]);
    // Every group is above depth 2 here, so a deeper limit adds nothing.
    engine.config.groupSeparators = { height: 2, maxDepth: 2 };
    engine._recomputeRowMetrics();
    assert.deepEqual(separatedRows(engine), [4, 6, 8, 10, 11]);
});

test('collapsing and expanding a group moves the separators with the rows', () => {
    const engine = makeEngine({ height: 2 });
    engine._collapsed.add('A');
    engine._rebuildRows();
    // A, B, S4, L1, L2, C, L3: B now follows the collapsed group A itself.
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['A', 'B', 'S4', 'L1', 'L2', 'C', 'L3']);
    assert.deepEqual(separatedRows(engine), [1, 3, 5, 6]);

    engine._collapsed.delete('C');
    engine._rebuildRows();
    // C expanded: L3 follows C's child S5, which is deeper than it.
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['A', 'B', 'S4', 'L1', 'L2', 'C', 'S5', 'L3']);
    assert.deepEqual(separatedRows(engine), [1, 3, 5, 7]);
});

test('a separator takes the room of its height and both borders between the rows', () => {
    const engine = makeEngine({ height: 4, borderWidth: 1 });
    const h = engine.config.resourceHeight;
    const gap = 4 + 2 * 1;
    assert.equal(engine._rowGap(6), gap);
    // Every row keeps its own height; the ones below a separator move down by it.
    assert.equal(engine._rowHeight(6), h);
    assert.equal(engine._rowContentTop(5), 5 * h);
    assert.equal(engine._rowContentTop(6), 6 * h + gap);
    assert.equal(engine._rowContentTop(8), 8 * h + 2 * gap);
    assert.equal(engine._totalRowsHeight(), 12 * h + 4 * gap);
});

test('a separator with nothing to draw, or a negative depth, takes no room', () => {
    for (const spec of [null, {}, { height: 0, borderWidth: 0 }, { background: '#f00' }, { height: 3, maxDepth: -1 }, { height: -4 }]) {
        const engine = makeEngine(spec);
        assert.deepEqual(separatedRows(engine), [], JSON.stringify(spec));
        assert.equal(engine._totalRowsHeight(), 12 * engine.config.resourceHeight, JSON.stringify(spec));
    }
    // Borders alone are a separator.
    assert.deepEqual(separatedRows(makeEngine({ borderWidth: 1 })), [6, 8, 10, 11]);
});

test('a point over a separator hits no row, while its slot is the row below it', () => {
    const engine = makeEngine({ height: 10 });
    const c = engine.config;
    const gapTop = engine._rowContentTop(5) + engine._rowHeight(5);
    for (const inGap of [gapTop, gapTop + 5, gapTop + 9.5]) {
        assert.equal(engine._rowIndexAtContentY(inGap), 6, `slot at ${inGap - gapTop}`);
        assert.equal(engine._rowAtContentY(inGap), -1);
        assert.equal(engine.getYToResource(c.timeAxisHeight + inGap), -1);
        assert.equal(engine._rowAtY(c.timeAxisHeight + inGap), -1);
    }
    // Either side of it, the rows themselves.
    assert.equal(engine._rowIndexAtContentY(gapTop - 1), 5);
    assert.equal(engine.getYToResource(c.timeAxisHeight + gapTop - 1), 5);
    assert.equal(engine.getYToResource(c.timeAxisHeight + gapTop + 10), 6);
});

test('a drop over a separator goes to the nearer of the rows it separates', () => {
    const engine = makeEngine({ height: 20 });
    const gapTop = engine._rowContentTop(5) + engine._rowHeight(5);
    assert.equal(engine._nearestRowAtContentY(gapTop - 1), 5);
    assert.equal(engine._nearestRowAtContentY(gapTop + 1), 5);
    assert.equal(engine._nearestRowAtContentY(gapTop + 9.5), 5);
    assert.equal(engine._nearestRowAtContentY(gapTop + 10), 6);
    assert.equal(engine._nearestRowAtContentY(gapTop + 19), 6);
    assert.equal(engine._nearestRowAtContentY(gapTop + 20), 6);
    // Out of range stays out of range, and the first row has no separator above it.
    assert.equal(engine._nearestRowAtContentY(-1), -1);
    assert.equal(engine._nearestRowAtContentY(0), 0);
    assert.equal(engine._nearestRowAtContentY(engine._totalRowsHeight()), -1);
});

test('the first keyboard focus skips a row scrolled out of view above a separator', () => {
    const engine = makeEngine({ height: 10 });
    engine.scrollY = engine._rowContentTop(5) + engine._rowHeight(5) + 3;
    assert.equal(engine._firstVisibleResourceIndex(), 6);
});

test('a scroll anchor inside a separator keeps its place when the rows above it change height', () => {
    const engine = makeEngine({ height: 10 });
    const c = engine.config;
    // Short enough to scroll, so _setScrollY does not clamp the view to the top.
    engine._viewportH = c.timeAxisHeight + 50;
    const gapTop = engine._rowContentTop(5) + engine._rowHeight(5);
    engine.scrollY = gapTop + 3;
    const anchor = engine._captureRowAnchor();
    assert.equal(anchor.rowId, 'B');
    assert.equal(anchor.rowOffset, -7);

    // Every row above grows: the view stays 3 pixels into the same separator.
    c.resourceHeight += 8;
    engine._recomputeRowMetrics();
    engine._restoreRowAnchor(anchor);
    assert.equal(engine.scrollY, engine._rowContentTop(5) + engine._rowHeight(5) + 3);
});

test('the scene paints a separator as its band and its borders, in place of the grid line', () => {
    const engine = makeEngine({ height: 4, borderWidth: 1, background: '#eeeeee', borderColor: '#333333' });
    const scene = engine.buildScene();
    const strips = scene.groupSeparators;
    assert.equal(strips.length, 4 * 3, 'a band and two borders for each of the four separators');

    const y = engine.getResourceToY(6) - 6;
    const at6 = strips.filter(s => s.y >= y && s.y < y + 6).sort((a, b) => a.y - b.y);
    assert.deepEqual(at6.map(s => [s.y - y, s.height, s.color, s.dash, s.axis]), [
        [0, 1, '#333333', 0, true],
        [1, 4, '#eeeeee', 0, true],
        [5, 1, '#333333', 0, true]
    ]);

    // The grid line along the top of a separated row is left out, the others stay.
    const lineRows = scene.gridH.map(line => line.rowIndex);
    for (const i of [6, 8, 10, 11]) assert.ok(!lineRows.includes(i), `no grid line at row ${i}`);
    for (const i of [1, 5, 7, 9, 12]) assert.ok(lineRows.includes(i), `a grid line at row ${i}`);
});

test('the separator colors default to the axis border color', () => {
    const engine = makeEngine({ height: 2, borderWidth: 1 }, { colors: { axisBorder: '#abcdef' } });
    const strips = engine.buildScene().groupSeparators;
    assert.ok(strips.length > 0);
    assert.ok(strips.every(s => s.color === '#abcdef'));
});

test('dashed, dotted and double borders', () => {
    const borders = (spec) => {
        const engine = makeEngine({ borderWidth: 3, ...spec });
        const strips = engine.buildScene().groupSeparators;
        const y = engine.getResourceToY(6) - engine._rowGap(6);
        return strips.filter(s => s.y >= y && s.y < y + engine._rowGap(6))
            .sort((a, b) => a.y - b.y)
            .map(s => [s.y - y, s.height, s.dash, s.dashGap]);
    };
    assert.deepEqual(borders({ borderStyle: 'Dashed' }), [[0, 3, 9, 6], [3, 3, 9, 6]]);
    assert.deepEqual(borders({ borderStyle: 'dotted' }), [[0, 3, 3, 3], [3, 3, 3, 3]]);
    // Two lines of a third of the width, the gap between them left unpainted.
    assert.deepEqual(borders({ borderStyle: 'Double' }), [[0, 1, 0, 0], [2, 1, 0, 0], [3, 1, 0, 0], [5, 1, 0, 0]]);
    // Too thin to hold two lines and a gap: solid.
    assert.deepEqual(borders({ borderStyle: 'Double', borderWidth: 2 }), [[0, 2, 0, 0], [2, 2, 0, 0]]);
    // A style it does not know is solid too.
    assert.deepEqual(borders({ borderStyle: 'Groove' }), [[0, 3, 0, 0], [3, 3, 0, 0]]);
});

test('showInResourceAxis false keeps the separators to the content pane, not their room', () => {
    const engine = makeEngine({ height: 3, showInResourceAxis: false });
    const strips = engine.buildScene().groupSeparators;
    assert.ok(strips.length > 0);
    assert.ok(strips.every(s => s.axis === false));
    assert.equal(engine._rowGap(6), 3);
});

test('a separator scrolled under the time axis is clipped to the content area', () => {
    const engine = makeEngine({ height: 10 });
    const c = engine.config;
    // Scroll so the time axis covers the top half of the separator above row 6.
    engine.scrollY = engine._rowContentTop(6) - 5;
    const strips = engine.buildScene().groupSeparators;
    const clipped = strips.find(s => s.y === c.timeAxisHeight);
    assert.ok(clipped, 'the separator is still painted below the axis');
    assert.equal(clipped.height, 5);
    assert.ok(strips.every(s => s.y >= c.timeAxisHeight && s.y + s.height <= engine._viewportH));
});

test('setOptions lays the rows out again for new separators, and an empty one clears them', () => {
    const engine = makeEngine(null);
    // The relayout that follows sizes the spacer and paints: no DOM here.
    engine._relayout = () => {};
    assert.deepEqual(separatedRows(engine), []);

    engine.setOptions({ groupSeparators: { height: 3, borderWidth: 1 } });
    assert.deepEqual(separatedRows(engine), [6, 8, 10, 11]);
    assert.equal(engine._rowGap(6), 5);

    // Options without separators leave them as they are.
    engine.setOptions({ barMargin: 2 });
    assert.equal(engine._rowGap(6), 5);

    engine.setOptions({ groupSeparators: {} });
    assert.deepEqual(separatedRows(engine), []);
    assert.equal(engine.buildScene().groupSeparators.length, 0);
});

test('the rows reported to the ResourceTemplate overlay carry the separator above them', () => {
    const engine = makeEngine({ height: 4 });
    engine.config.resourceTemplate = true;
    const sent = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name, rows) => {
            if (name === 'OnResourceRowsChanged') sent.push(rows);
            return Promise.resolve();
        }
    };
    engine._reportResourceRows();
    assert.equal(sent.length, 1);
    assert.deepEqual(sent[0].map(r => r.gap), [0, 0, 0, 0, 0, 0, 4, 0, 4, 0, 4, 4]);
});
