// Resource hierarchy, virtual-scroll mapping, option handling and selection.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine, makeIndexedEngine } from './helpers/engine-fixture.mjs';

function res(id, parentId, extra = {}) {
    return parentId == null ? { id, name: id, ...extra } : { id, name: id, parentId, ...extra };
}

// _rebuildRows reports rows to .NET; without a dotNetRef that is a no-op.
function buildHierarchy(resources) {
    const engine = makeBareEngine();
    engine.resources = resources;
    engine._rebuildResourceStructure();
    return engine;
}

test('hierarchy flattens depth-first in input order', () => {
    const engine = buildHierarchy([
        res('a'), res('a1', 'a'), res('a2', 'a'), res('a1x', 'a1'), res('b')
    ]);
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['a', 'a1', 'a1x', 'a2', 'b']);
    assert.deepEqual(engine._rows.map(r => r.depth), [0, 1, 2, 1, 0]);
    assert.deepEqual(engine._rows.map(r => r.hasChildren), [true, true, false, false, false]);
});

test('collapsed groups hide their descendants', () => {
    const engine = buildHierarchy([res('a'), res('a1', 'a'), res('a1x', 'a1'), res('b')]);
    engine._collapsed.add('a');
    engine._rebuildRows();
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['a', 'b']);
});

test('the initial collapsed flag on a resource is honoured', () => {
    const engine = buildHierarchy([res('a', null, { collapsed: true }), res('a1', 'a')]);
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['a']);
});

test('a resource pointing at a missing parent becomes a root', () => {
    const engine = buildHierarchy([res('a'), res('orphan', 'nonexistent')]);
    assert.deepEqual(engine._rows.map(r => r.resource.id), ['a', 'orphan']);
});

test('a self-parenting resource becomes a root rather than vanishing', () => {
    const engine = buildHierarchy([res('a'), res('loop', 'loop')]);
    assert.deepEqual(engine._rows.map(r => r.resource.id).sort(), ['a', 'loop']);
});

test('a parentId cycle does not lose rows or hang', () => {
    // x -> y -> x. Neither is reachable from a root, so before cycle detection
    // both disappeared from the timeline silently.
    const engine = buildHierarchy([res('a'), res('x', 'y'), res('y', 'x')]);
    const ids = engine._rows.map(r => r.resource.id).sort();
    assert.deepEqual(ids, ['a', 'x', 'y']);
});

test('a deep hierarchy flattens without exhausting the stack', () => {
    // Recursion here used to be bounded only by host data.
    const resources = [res('r0')];
    for (let i = 1; i < 50000; i++) resources.push(res('r' + i, 'r' + (i - 1)));
    const engine = buildHierarchy(resources);
    assert.equal(engine._rows.length, 50000);
    assert.equal(engine._rows[49999].depth, 49999);
});

test('row index by id stays in sync with the flattened rows', () => {
    const engine = buildHierarchy([res('a'), res('a1', 'a'), res('b')]);
    for (let i = 0; i < engine._rows.length; i++) {
        assert.equal(engine._rowIndexById.get(engine._rows[i].resource.id), i);
    }
});

test('unknown options are rejected instead of silently stored', () => {
    const engine = makeBareEngine();
    const warnings = [];
    const realWarn = console.warn;
    console.warn = (m) => warnings.push(m);
    try {
        engine._applyOptions({ barHeight: 9, bartHeight: 12, colors: { bar: '#fff', barr: '#000' } });
    } finally {
        console.warn = realWarn;
    }
    assert.equal(engine.config.barHeight, 9);
    assert.ok(!('bartHeight' in engine.config));
    assert.equal(engine.config.colors.bar, '#fff');
    assert.ok(!('barr' in engine.config.colors));
    assert.equal(warnings.length, 2);
});

test('null and undefined option values leave the default in place', () => {
    const engine = makeBareEngine();
    engine._applyOptions({ barHeight: null, barMargin: undefined });
    assert.equal(engine.config.barHeight, 4);
    assert.equal(engine.config.barMargin, 2);
});

test('the scene config snapshot is frozen and reused until the config changes', () => {
    const engine = makeBareEngine();
    const first = engine._configSnapshot();
    assert.ok(Object.isFrozen(first));
    assert.ok(Object.isFrozen(first.colors ?? Object.freeze({})));
    assert.equal(engine._configSnapshot(), first, 'unchanged config must reuse the snapshot');

    engine._applyOptions({ barHeight: 7 });
    const second = engine._configSnapshot();
    assert.notEqual(second, first, 'a config change must produce a new snapshot');
    assert.equal(second.barHeight, 7);
    assert.equal(first.barHeight, 4, 'the old snapshot must not observe later changes');
});

test('time/x conversions round-trip through the current scale and scroll', () => {
    const engine = makeBareEngine();
    engine.timeRange = { start: 1000000, end: 1000000 + 24 * 3600000 };
    engine._pixelsPerMs = 50 / 3600000;
    engine.scrollX = 1234;

    for (const time of [1000000, 1000000 + 5 * 3600000, 1000000 + 23 * 3600000]) {
        const x = engine.getTimeToX(time);
        assert.ok(Math.abs(engine.getXToTime(x) - time) < 1e-6, 'x -> time must invert time -> x');
    }
});

test('resource row hit-testing maps y back to the row index', () => {
    const engine = makeBareEngine();
    engine._rows = [1, 2, 3].map(i => ({ resource: { id: 'r' + i }, depth: 0, hasChildren: false }));
    engine._recomputeRowMetrics();
    engine.scrollY = 0;

    // Row i occupies [timeAxisHeight + i*h, +h) when every row is the minimum height.
    const h = engine.config.resourceHeight;
    const axis = engine.config.timeAxisHeight;
    assert.equal(engine.getYToResource(axis + 0.5 * h), 0);
    assert.equal(engine.getYToResource(axis + 1.5 * h), 1);
    assert.equal(engine.getYToResource(axis + 2.5 * h), 2);
    assert.equal(engine.getYToResource(axis + 3.5 * h), -1, 'past the last row');
    assert.equal(engine.getYToResource(axis - 1), -1, 'above the content area');
});

test('variable-height rows keep hit-testing aligned with cumulative tops', () => {
    const engine = makeBareEngine();
    engine._rows = [0, 1, 2].map(i => ({ resource: { id: 'r' + i }, depth: 0, hasChildren: false }));
    // Simulate a tall middle row (as deep stacks produce).
    engine._rowHeights = [40, 80, 40];
    engine._rowTops = [0, 40, 120, 160];
    engine.scrollY = 0;
    const axis = engine.config.timeAxisHeight;

    assert.equal(engine.getYToResource(axis + 20), 0);
    assert.equal(engine.getYToResource(axis + 40), 1);
    assert.equal(engine.getYToResource(axis + 100), 1);
    assert.equal(engine.getYToResource(axis + 120), 2);
    assert.equal(engine.getResourceToY(1), axis + 40);
    assert.equal(engine._totalRowsHeight(), 160);
});

test('selection holds ids only and survives a data reload', () => {
    const engine = makeIndexedEngine([
        { id: 'a', resourceId: 'r0', startTime: 0, endTime: 100 }
    ]);
    engine.selectedBars.add('a');

    // Same allocation re-delivered as a fresh object, as a windowed refetch does.
    engine.allocations = [{ id: 'a', resourceId: 'r0', startTime: 0, endTime: 100 }];
    engine._indexAllocations();

    assert.ok(engine.selectedBars.has('a'), 'selection is by id, so it survives new instances');
    assert.deepEqual(engine.getSelectedBarIds(), ['a']);
});

test('selection order is preserved', () => {
    const engine = makeBareEngine();
    for (const id of ['c', 'a', 'b']) engine.selectedBars.add(id);
    assert.deepEqual(engine.getSelectedBarIds(), ['c', 'a', 'b']);
});

test('resource-axis width clamp honours min, max and the viewport floor', () => {
    const engine = makeBareEngine();
    engine._viewportW = 1000;
    engine.config.resourceAxisMinWidth = 80;
    engine.config.resourceAxisMaxWidth = 0;

    assert.equal(engine._clampResourceAxisWidth(40), 80, 'below min');
    assert.equal(engine._clampResourceAxisWidth(200), 200, 'inside range');
    // Viewport keeps 100px of content: 1000 - 100 = 900.
    assert.equal(engine._clampResourceAxisWidth(2000), 900, 'viewport cap when no host max');

    engine.config.resourceAxisMaxWidth = 240;
    assert.equal(engine._clampResourceAxisWidth(400), 240, 'host max tighter than viewport');
    engine.config.resourceAxisMaxWidth = 5000;
    assert.equal(engine._clampResourceAxisWidth(4000), 900, 'viewport still wins over a huge host max');
});

test('resource-axis width bounds never invert when the viewport is tiny', () => {
    const engine = makeBareEngine();
    engine._viewportW = 50;
    engine.config.resourceAxisMinWidth = 80;
    const { min, max } = engine._resourceAxisWidthBounds();
    assert.equal(min, 80);
    assert.ok(max >= min);
});

test('resource-axis resize options are accepted by name', () => {
    const engine = makeBareEngine();
    const warnings = [];
    const realWarn = console.warn;
    console.warn = (m) => warnings.push(m);
    try {
        engine._applyOptions({
            resourceAxisResizable: false,
            resourceAxisMinWidth: 64,
            resourceAxisMaxWidth: 320
        });
    } finally {
        console.warn = realWarn;
    }
    assert.equal(warnings.length, 0);
    assert.equal(engine.config.resourceAxisResizable, false);
    assert.equal(engine.config.resourceAxisMinWidth, 64);
    assert.equal(engine.config.resourceAxisMaxWidth, 320);
});

test('panToDayStart is accepted by name', () => {
    const engine = makeBareEngine();
    const warnings = [];
    const realWarn = console.warn;
    console.warn = (m) => warnings.push(m);
    try {
        engine._applyOptions({ panToDayStart: true });
    } finally {
        console.warn = realWarn;
    }
    assert.equal(warnings.length, 0);
    assert.equal(engine.config.panToDayStart, true);
});

test('setting the resource-axis width updates config and notifies only when asked', () => {
    const engine = makeBareEngine();
    engine._viewportW = 1000;
    engine.config.resourceAxisWidth = 150;
    let relayouts = 0;
    const notified = [];
    engine._hasTimeRange = () => true;
    engine._relayout = () => { relayouts += 1; };
    engine._syncAxisOverlays = () => {};
    engine._syncAxisSplitterChrome = () => {};
    engine.render = () => {};
    engine._notifyResourceAxisWidth = (w) => notified.push(w);

    assert.equal(engine._setResourceAxisWidth(220, false), 220);
    assert.equal(engine.config.resourceAxisWidth, 220);
    assert.equal(relayouts, 1);
    assert.deepEqual(notified, []);

    assert.equal(engine._setResourceAxisWidth(260, true), 260);
    assert.deepEqual(notified, [260]);

    assert.equal(engine._setResourceAxisWidth(10, true), 80, 'clamped to min before notify');
    assert.equal(engine.config.resourceAxisWidth, 80);
    assert.equal(notified.at(-1), 80);
});

test('marquee covering only the upper lane selects one stacked id', () => {
    const engine = makeIndexedEngine([
        { id: 'low', resourceId: 'r0', startTime: 0, endTime: 400 },
        { id: 'high', resourceId: 'r0', startTime: 50, endTime: 350 }
    ]);
    engine.timeRange = { start: 0, end: 1000 };
    engine._pixelsPerMs = 1;
    engine._recomputeRowMetrics();
    const high = engine.allocations.find(a => a.id === 'high');
    const low = engine.allocations.find(a => a.id === 'low');
    const band = engine._barContentBand(high, 0);
    const lowBand = engine._barContentBand(low, 0);
    assert.ok(band.bottom <= lowBand.top || lowBand.bottom <= band.top
        || band.top !== lowBand.top);

    engine.drag = {
        startX: engine._timeToContentX(high.startTime) + 1,
        startY: band.top + 0.1,
        currentX: engine._timeToContentX(high.endTime) - 1,
        currentY: band.bottom - 0.1,
        additive: false,
        baseSelection: new Set()
    };
    engine._applyMarqueeSelection();
    assert.deepEqual([...engine.selectedBars], ['high']);
});

test('Shift-range select on a 3-bar row selects the middle span', () => {
    const engine = makeIndexedEngine([
        { id: 'a', resourceId: 'r0', startTime: 0, endTime: 50 },
        { id: 'b', resourceId: 'r0', startTime: 100, endTime: 150 },
        { id: 'c', resourceId: 'r0', startTime: 200, endTime: 250 }
    ]);
    engine._selectRange('a', 'c');
    assert.deepEqual([...engine.selectedBars], ['a', 'b', 'c']);
    engine._selectRange('b', 'c');
    assert.deepEqual([...engine.selectedBars], ['b', 'c']);
});

test('selectBars notifies only when the selected ids change', () => {
    const engine = makeIndexedEngine([
        { id: 'a', resourceId: 'r0', startTime: 0, endTime: 100 },
        { id: 'b', resourceId: 'r0', startTime: 200, endTime: 300 }
    ]);
    const notified = [];
    engine.render = () => {};
    engine._notifySelection = () => { notified.push([...engine.selectedBars]); };

    engine.selectBars(['a'], false);
    assert.deepEqual(notified, [['a']]);

    engine.selectBars(['a'], false);
    assert.deepEqual(notified, [['a']], 'same replacement is a no-op');

    engine.selectBars(['a'], true);
    assert.deepEqual(notified, [['a']], 'additive add of an already-selected id is a no-op');

    engine.selectBars(['b'], false);
    assert.deepEqual(notified, [['a'], ['b']]);
});

test('Enter on an already-sole-selected focused bar does not notify', () => {
    const engine = makeIndexedEngine([
        { id: 'a', resourceId: 'r0', startTime: 0, endTime: 100 }
    ]);
    engine._focusAlloc = engine.allocations[0];
    engine._focusResource = 0;
    engine.selectedBars.add('a');
    const notified = [];
    engine.render = () => {};
    engine._notifySelection = () => { notified.push([...engine.selectedBars]); };
    engine._announceFocus = () => {};

    engine._toggleSelectFocused(false);
    assert.deepEqual(notified, []);
    assert.deepEqual([...engine.selectedBars], ['a']);

    engine._toggleSelectFocused(true);
    assert.deepEqual(notified, [[]]);
});

test('a marquee that matches the existing selection does not notify', () => {
    const engine = makeIndexedEngine([
        { id: 'high', resourceId: 'r0', startTime: 0, endTime: 400 }
    ]);
    engine.timeRange = { start: 0, end: 1000 };
    engine._pixelsPerMs = 1;
    engine._recomputeRowMetrics();
    const high = engine.allocations[0];
    const band = engine._barContentBand(high, 0);
    engine.selectedBars.add('high');

    const drag = {
        startX: engine._timeToContentX(high.startTime) + 1,
        startY: band.top + 0.1,
        currentX: engine._timeToContentX(high.endTime) - 1,
        currentY: band.bottom - 0.1,
        additive: false,
        baseSelection: new Set(engine.selectedBars)
    };
    engine._applyMarqueeSelection(drag);
    assert.deepEqual([...engine.selectedBars], ['high']);
    assert.equal(engine._selectionEquals(drag.baseSelection, engine.selectedBars), true);
});
