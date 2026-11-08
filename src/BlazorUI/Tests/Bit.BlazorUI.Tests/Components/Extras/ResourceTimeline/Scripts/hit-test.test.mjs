// Hit-testing: click tolerance, resize vs move, locked bars, lane Y.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeIndexedEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');

function makeHitEngine(allocations, overrides = {}) {
    const engine = makeIndexedEngine(allocations, overrides);
    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine._pixelsPerHour = 40;
    engine._pixelsPerMs = 40 / HOUR;
    engine._visibleWidth = 1050;
    engine.scrollX = 0;
    engine.scrollY = 0;
    engine._recomputeRowMetrics();
    return engine;
}

function bar(id, startHour, endHour, extra = {}) {
    return {
        id, resourceId: 'r0',
        startTime: START + startHour * HOUR,
        endTime: START + endHour * HOUR,
        ...extra
    };
}

test('_barAt hits the stacked upper lane and misses the lower one', () => {
    const engine = makeHitEngine([
        bar('low', 1, 4),
        bar('high', 1.5, 3.5)
    ]);
    const low = engine.allocations.find(a => a.id === 'low');
    const high = engine.allocations.find(a => a.id === 'high');
    const rowCenter = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const h = engine.config.barHeight;
    const x = engine.getTimeToX((low.startTime + high.endTime) / 2);

    const upperY = rowCenter + engine._stackOffset(high);
    const lowerY = rowCenter + engine._stackOffset(low);
    assert.notEqual(upperY, lowerY);

    const hitUpper = engine._barAt(x, upperY);
    const hitLower = engine._barAt(x, lowerY);
    assert.equal(hitUpper && hitUpper.alloc.id, 'high');
    assert.equal(hitLower && hitLower.alloc.id, 'low');
});

test('_editZone is resize at the ends and move in the middle', () => {
    const engine = makeHitEngine([bar('a', 1, 4)], { config: { editResizeHandlePx: 6 } });
    const alloc = engine.allocations[0];
    const startX = engine.getTimeToX(alloc.startTime);
    const endX = engine.getTimeToX(alloc.endTime);
    const mid = (startX + endX) / 2;

    assert.equal(engine._editZone(alloc, startX), 'resize-start');
    assert.equal(engine._editZone(alloc, endX), 'resize-end');
    assert.equal(engine._editZone(alloc, mid), 'move');
});

test('_editZone is null for a locked bar', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    const alloc = engine.allocations[0];
    alloc.locked = true;
    const mid = engine.getTimeToX((alloc.startTime + alloc.endTime) / 2);
    assert.equal(engine._editZone(alloc, mid), null);
});

test('_barAt takes a click within the tolerance around both ends of the longest bar', () => {
    for (const tolerance of [3, 8]) {
        // The only bar is the longest of its row, so a lookup that went back by
        // its span alone would start past it for a click just after its end.
        const engine = makeHitEngine([bar('a', 1, 4)], { config: { hitTolerance: tolerance } });
        const alloc = engine.allocations[0];
        const startX = engine.getTimeToX(alloc.startTime);
        const endX = engine.getTimeToX(alloc.endTime);
        const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
        const idAt = (x) => { const hit = engine._barAt(x, y); return hit && hit.alloc.id; };

        assert.equal(idAt(endX + tolerance - 1), 'a', `just after the end, tolerance ${tolerance}`);
        assert.equal(idAt(endX + tolerance + 1), null, `past the tolerance after the end, tolerance ${tolerance}`);
        assert.equal(idAt(startX - tolerance + 1), 'a', `just before the start, tolerance ${tolerance}`);
        assert.equal(idAt(startX - tolerance - 1), null, `past the tolerance before the start, tolerance ${tolerance}`);
    }
});

test('_barAt takes a click on the minimum width of a bar shorter than it', () => {
    // 36 seconds is under a pixel at 40px/hour: the bar is drawn (and hit) 6px wide.
    const engine = makeHitEngine([bar('a', 1, 1.01)], { config: { minBarWidth: 6, hitTolerance: 3 } });
    const startX = engine.getTimeToX(engine.allocations[0].startTime);
    const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const idAt = (x) => { const hit = engine._barAt(x, y); return hit && hit.alloc.id; };

    assert.equal(idAt(startX + 5), 'a', 'on the minimum width');
    assert.equal(idAt(startX + 8), 'a', 'within the tolerance past the minimum width');
    assert.equal(idAt(startX + 10), null, 'past both');
});

test('_barAt ignores overflow bars', () => {
    const bars = [];
    for (let i = 0; i < 5; i++) bars.push(bar('a' + i, 1, 3));
    const engine = makeHitEngine(bars, { config: { maxStackLanes: 2 } });
    const overflow = engine.allocations.filter(a => engine._isOverflow(a));
    assert.ok(overflow.length > 0);
    const x = engine.getTimeToX(START + 2 * HOUR);
    const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const hit = engine._barAt(x, y);
    assert.ok(hit);
    assert.equal(engine._isOverflow(hit.alloc), false);
});

test('_hitArea classifies content, axes and the corner', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    const ax = engine.config.resourceAxisWidth;
    const ay = engine.config.timeAxisHeight;
    assert.equal(engine._hitArea(ax - 1, ay - 1), 'corner');
    assert.equal(engine._hitArea(ax + 10, ay - 1), 'timeAxis');
    assert.equal(engine._hitArea(ax - 1, ay + 10), 'resourceAxis');
    assert.equal(engine._hitArea(ax + 10, ay + 10), 'content');
});

test('_pointerHit reports the bar, resource and time in the content area', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    const alloc = engine.allocations[0];
    const x = engine.getTimeToX((alloc.startTime + alloc.endTime) / 2);
    const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const hit = engine._pointerHit(x, y);
    assert.equal(hit.area, 'content');
    assert.equal(hit.allocId, 'a');
    assert.equal(hit.resourceId, 'r0');
    assert.equal(hit.overflowIds, null);
    assert.equal(hit.x, x);
    assert.equal(hit.y, y);
    assert.ok(Math.abs(hit.time - (alloc.startTime + alloc.endTime) / 2) < 60_000);
});

test('_pointerHit on empty content still has resource and time', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    const x = engine.getTimeToX(START + 20 * HOUR);
    const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const hit = engine._pointerHit(x, y);
    assert.equal(hit.area, 'content');
    assert.equal(hit.allocId, null);
    assert.equal(hit.resourceId, 'r0');
    assert.ok(hit.time != null);
});

test('_pointerHit on the resource axis has a resource but no time', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    const y = engine.getResourceToY(0) + engine._rowHeight(0) / 2;
    const hit = engine._pointerHit(10, y);
    assert.equal(hit.area, 'resourceAxis');
    assert.equal(hit.resourceId, 'r0');
    assert.equal(hit.time, null);
    assert.equal(hit.allocId, null);
});

test('_pointerHit on the time axis has a time but no resource', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    const x = engine.getTimeToX(START + 3 * HOUR);
    const hit = engine._pointerHit(x, 10);
    assert.equal(hit.area, 'timeAxis');
    assert.equal(hit.resourceId, null);
    assert.ok(hit.time != null);
    assert.equal(hit.allocId, null);
});

test('_pointerHit on an overflow label reports the hidden ids, not a bar', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    engine._overflowHits = [{ x: 200, y: 80, width: 22, height: 14, ids: ['h1', 'h2'] }];
    const hit = engine._pointerHit(210, 85);
    assert.equal(hit.allocId, null);
    assert.deepEqual(hit.overflowIds, ['h1', 'h2']);
    assert.equal(hit.area, 'content');
});

test('_recordClick treats a second still press within the window as a double-click', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    assert.equal(engine._recordClick(100, 80, 0), false);
    assert.equal(engine._recordClick(101, 81, 400), true);
    assert.equal(engine._recordClick(102, 80, 800), false);
    assert.equal(engine._recordClick(102, 80, 900), true);
});

test('_recordClick ignores a second press that moved or waited too long', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    engine._recordClick(100, 80, 0);
    assert.equal(engine._recordClick(100, 80, 501), false);
    // The late press starts a pair of its own: the next still press within the window completes it.
    assert.equal(engine._recordClick(100, 80, 600), true);
    engine._recordClick(100, 80, 0);
    assert.equal(engine._recordClick(100 + engine.config.dragThreshold + 1, 80, 100), false);
});

function trackSelectionNotify(engine) {
    const notified = [];
    engine.render = () => {};
    engine._notifySelection = () => { notified.push([...engine.selectedBars]); };
    return notified;
}

function contentPoint(engine, startHour) {
    return {
        x: engine.getTimeToX(START + startHour * HOUR),
        y: engine.getResourceToY(0) + engine._rowHeight(0) / 2
    };
}

test('clicking an unselected bar notifies with that bar', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 2.5);
    engine._handleClickSelect(x, y, false, false);
    assert.deepEqual(notified, [['a']]);
    assert.deepEqual([...engine.selectedBars], ['a']);
});

test('clicking empty content does not notify when nothing is selected', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 20);
    engine._handleClickSelect(x, y, false, false);
    assert.deepEqual(notified, []);
    assert.equal(engine.selectedBars.size, 0);
});

test('clicking empty content clears a selection and notifies once', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    engine.selectedBars.add('a');
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 20);
    engine._handleClickSelect(x, y, false, false);
    assert.deepEqual(notified, [[]]);
    assert.equal(engine.selectedBars.size, 0);
});

test('re-clicking the sole selected bar does not notify', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    engine.selectedBars.add('a');
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 2.5);
    engine._handleClickSelect(x, y, false, false);
    assert.deepEqual(notified, []);
    assert.deepEqual([...engine.selectedBars], ['a']);
});

test('clicking a different bar replaces the selection and notifies', () => {
    const engine = makeHitEngine([bar('a', 1, 3), bar('b', 8, 10)]);
    engine.selectedBars.add('a');
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 9);
    engine._handleClickSelect(x, y, false, false);
    assert.deepEqual(notified, [['b']]);
    assert.deepEqual([...engine.selectedBars], ['b']);
});

test('Ctrl-click on empty content leaves the selection and does not notify', () => {
    const engine = makeHitEngine([bar('a', 1, 4)]);
    engine.selectedBars.add('a');
    const notified = trackSelectionNotify(engine);
    const { x, y } = contentPoint(engine, 20);
    engine._handleClickSelect(x, y, true, false);
    assert.deepEqual(notified, []);
    assert.deepEqual([...engine.selectedBars], ['a']);
});

test('re-clicking an overflow cluster that is already selected does not notify', () => {
    const engine = makeHitEngine([bar('a', 1, 2)]);
    engine._overflowHits = [{ x: 200, y: 80, width: 22, height: 14, ids: ['h1', 'h2'] }];
    engine.selectedBars.add('h1');
    engine.selectedBars.add('h2');
    const notified = trackSelectionNotify(engine);
    engine._handleClickSelect(210, 85, false, false);
    assert.deepEqual(notified, []);
    assert.deepEqual([...engine.selectedBars], ['h1', 'h2']);
});
