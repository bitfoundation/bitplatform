// Scene building. The scene object, its arrays and the bar nodes inside it are
// pooled and refilled in place each frame, so these tests focus on the failure
// mode that introduces: state left over from a previous frame surfacing on a
// bar that should not have it.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');

// buildScene measures day labels through a 2D context. That is the only DOM it
// touches, so a stub with a plausible metric makes the whole path testable.
function makeSceneEngine(allocations, overrides = {}) {
    const engine = makeBareEngine(overrides);
    engine._measureCtx = { font: '', measureText: (t) => ({ width: t.length * 7 }) };
    engine.config.timeZone = 'UTC';
    engine._rebuildDateFormatters();

    engine.resources = [
        { id: 'r0', name: 'Resource 0' },
        { id: 'r1', name: 'Resource 1' }
    ];
    engine._rebuildResourceStructure();

    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine.allocations = allocations.slice().sort((a, b) => a.startTime - b.startTime);
    engine._indexAllocations();

    engine._pixelsPerHour = 40;
    engine._pixelsPerMs = 40 / HOUR;
    engine._visibleWidth = 1050;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    return engine;
}

function bar(id, resourceId, startHour, endHour, extra = {}) {
    return {
        id, resourceId,
        startTime: START + startHour * HOUR,
        endTime: START + endHour * HOUR,
        ...extra
    };
}

test('a scene carries the visible bars with resolved geometry', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3), bar('b', 'r1', 2, 4)]);
    const scene = engine.buildScene();

    assert.deepEqual(scene.bars.map(b => b.id), ['a', 'b']);
    for (const b of scene.bars) {
        assert.ok(b.width > 0, 'bars need a positive drawn width');
        assert.ok(Number.isFinite(b.x) && Number.isFinite(b.y));
    }
    assert.ok(scene.hourTicks.length > 0, 'the hour row should have ticks at this zoom');
    assert.ok(scene.days.length > 0, 'the date row should have at least one day');
});

test('the scene config is a frozen snapshot, not the live config', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)]);
    const scene = engine.buildScene();

    assert.ok(Object.isFrozen(scene.config));
    engine.config.barHeight = 99;
    assert.notEqual(scene.config.barHeight, 99,
        'a scene must not observe config changes made after it was built');
});

test('a selection outline does not persist onto a later unselected bar', () => {
    // The pooled bar node that carried the outline is reused next frame.
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)]);
    engine.selectedBars.add('a');
    const first = engine.buildScene();
    assert.ok(first.bars[0].outline, 'the selected bar should have an outline');

    engine.selectedBars.clear();
    const second = engine.buildScene();
    assert.equal(second.bars[0].outline, null,
        'the reused node must not keep the previous frame\'s outline');
    assert.equal(second.bars[0].selected, false);
});

test('a focus ring does not persist onto a later unfocused bar', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)]);
    engine._hasFocus = true;
    engine._focusAlloc = engine.allocations[0];
    assert.ok(engine.buildScene().bars[0].focusRing);

    engine._hasFocus = false;
    assert.equal(engine.buildScene().bars[0].focusRing, null);
});

test('edge bars do not persist onto a later bar without them', () => {
    const withEdges = makeSceneEngine([
        bar('a', 'r0', 1, 3, { startBar: { duration: HOUR, color: 'red' } })
    ]);
    assert.equal(withEdges.buildScene().bars[0].edges.length, 1);

    // Same engine, same pooled node, but the allocation no longer has an edge.
    withEdges.allocations[0].startBar = null;
    withEdges._indexAllocations();
    const scene = withEdges.buildScene();
    assert.equal(scene.bars[0].edges, null, 'the reused node must not keep stale edge bars');
});

test('labels do not persist onto a later bar without them', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 6, { textAbove: 'hello' })]);
    assert.deepEqual(engine.buildScene().bars[0].labels.map(l => l.text), ['hello']);

    engine.allocations[0].textAbove = null;
    const scene = engine.buildScene();
    assert.equal(scene.bars[0].labels, null, 'the reused node must not keep stale labels');
});

test('a bar with fewer labels than last frame does not accumulate them', () => {
    // The backing array is reused, so it has to be cleared rather than appended
    // to - otherwise labels pile up frame after frame.
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 6, { textAbove: 'above', textBelow: 'below' })
    ]);
    assert.equal(engine.buildScene().bars[0].labels.length, 2);
    assert.equal(engine.buildScene().bars[0].labels.length, 2, 'a repeat frame must not grow the list');

    engine.allocations[0].textBelow = null;
    assert.deepEqual(engine.buildScene().bars[0].labels.map(l => l.text), ['above']);
});

test('a bar with fewer icons than last frame does not accumulate them', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 6, {
            icons: [
                { source: 'one.png', position: 'start' },
                { source: 'two.png', position: 'end' }
            ]
        })
    ]);
    // Icons are only laid out once their image has loaded; stub the cache.
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;

    assert.equal(engine.buildScene().bars[0].icons.length, 2);
    assert.equal(engine.buildScene().bars[0].icons.length, 2, 'a repeat frame must not grow the list');

    engine.allocations[0].icons = [{ source: 'one.png', position: 'start' }];
    assert.equal(engine.buildScene().bars[0].icons.length, 1);
});

test('center icons sit on the bar as one horizontally centered group', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 6, {
            icons: [
                { source: 'one.png', position: 'center' },
                { source: 'two.png', position: 'center' }
            ]
        })
    ]);
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;

    const node = engine.buildScene().bars[0];
    const [first, second] = node.icons;
    const gap = engine.config.barLabelGap;

    assert.equal(second.x, first.x + first.width + gap, 'the pair must sit side by side');
    const groupCenterX = (first.x + second.x + second.width) / 2;
    assert.equal(groupCenterX, node.x + node.width / 2, 'the group must be centered on the bar');

    for (const icon of node.icons) {
        assert.equal(icon.y + icon.height / 2, node.y + node.height / 2,
            'each icon must be vertically centered on the bar');
    }
});

test('inside icons hug the bar edges without displacing the labels outside it', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 6, {
            height: 24,
            textStart: 'start',
            textEnd: 'end',
            icons: [
                { source: 'one.png', position: 'start', inside: true },
                { source: 'two.png', position: 'end', inside: true }
            ]
        })
    ]);
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;

    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const [atStart, atEnd] = node.icons;

    assert.equal(atStart.x, node.x + gap, 'the start icon belongs just inside the left edge');
    assert.equal(atEnd.x + atEnd.width, node.x + node.width - gap,
        'the end icon belongs just inside the right edge');
    for (const icon of node.icons) {
        assert.equal(icon.y + icon.height / 2, node.y + node.height / 2);
    }

    // Labels are anchored to the bar's outer edges: an icon placed inside the
    // bar takes up no room out there, so they must not be pushed away.
    const byText = Object.fromEntries(node.labels.map(l => [l.text, l]));
    assert.equal(byText.start.x, node.x - gap);
    assert.equal(byText.end.x, node.x + node.width + gap);
});

test('inside icons anchored above and below hug the bar\'s top and bottom', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 6, {
            height: 24,
            icons: [
                { source: 'one.png', position: 'above', inside: true },
                { source: 'two.png', position: 'below', inside: true }
            ]
        })
    ]);
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;

    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const [atTop, atBottom] = node.icons;

    assert.equal(atTop.y, node.y + gap);
    assert.equal(atBottom.y + atBottom.height, node.y + node.height - gap);
    for (const icon of node.icons) {
        assert.equal(icon.x + icon.width / 2, node.x + node.width / 2);
    }
});

// Vertical band a bar node actually paints into: the bar itself plus every
// label and icon around it. Labels carry no height in the display list (the
// renderer measures the text), so their box is taken as the font's pixel size
// about the anchor its baseline names - a lower bound on the real line box.
function paintedBand(engine, node) {
    const lineHeight = parseFloat(engine.config.barLabelFont);
    let top = node.y;
    let bottom = node.y + node.height;
    for (const label of node.labels || []) {
        const height = label.baseline === 'middle' ? lineHeight / 2 : lineHeight;
        top = Math.min(top, label.baseline === 'top' ? label.y : label.y - height);
        bottom = Math.max(bottom, label.baseline === 'bottom' ? label.y : label.y + height);
    }
    for (const icon of node.icons || []) {
        top = Math.min(top, icon.y);
        bottom = Math.max(bottom, icon.y + icon.height);
    }
    return { top, bottom };
}

test('nothing a stacked bar paints reaches into what its neighbour paints', () => {
    const decorated = {
        textAbove: 'SRV01', textBelow: '2h', textStart: '08:30', textEnd: '10:30',
        icons: [{ source: 'i.png', position: 'center' }]
    };
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 3, decorated),
        bar('b', 'r0', 2, 4, decorated)
    ]);
    engine._getImage = () => ({ complete: true, naturalWidth: 16, naturalHeight: 16 });

    const [first, second] = engine.buildScene().bars.map(n => paintedBand(engine, n));

    assert.ok(first.bottom < second.top,
        `the upper bar paints down to ${first.bottom}, the lower one from ${second.top}`);
});

test('edge bars from one bar do not leak into the next bar in the same frame', () => {
    // Two bars share the pool; the second must start clean even though the
    // first filled its backing arrays.
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 3, { startBar: { duration: HOUR, color: 'red' }, textAbove: 'x' }),
        bar('b', 'r0', 10, 12)
    ]);
    const scene = engine.buildScene();
    const [first, second] = scene.bars;

    assert.equal(first.id, 'a');
    assert.equal(second.id, 'b');
    assert.ok(first.edges && first.edges.length === 1);
    assert.equal(second.edges, null);
    assert.equal(second.labels, null);
});

test('a shrinking frame does not leave stale bars or rows behind', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 3), bar('b', 'r0', 5, 7), bar('c', 'r1', 2, 4)
    ]);
    assert.equal(engine.buildScene().bars.length, 3);

    engine.allocations = [engine.allocations[0]];
    engine._indexAllocations();
    const scene = engine.buildScene();
    assert.equal(scene.bars.length, 1);
    assert.deepEqual(scene.bars.map(b => b.id), ['a']);
});

test('resource rows shrink when a group collapses', () => {
    const engine = makeSceneEngine([]);
    engine.resources = [
        { id: 'g', name: 'Group' },
        { id: 'c1', name: 'Child 1', parentId: 'g' },
        { id: 'c2', name: 'Child 2', parentId: 'g' }
    ];
    engine._rebuildResourceStructure();
    assert.equal(engine.buildScene().resourceRows.length, 3);

    engine._collapsed.add('g');
    engine._rebuildRows();
    const scene = engine.buildScene();
    assert.equal(scene.resourceRows.length, 1);
    assert.equal(scene.resourceRows[0].name, 'Group');
    assert.equal(scene.resourceRows[0].collapsed, true);
});

test('resource rows are omitted entirely when an HTML template renders them', () => {
    const engine = makeSceneEngine([]);
    engine.config.resourceTemplate = true;
    assert.equal(engine.buildScene().resourceRows, null);
});

test('the marquee and ghost are absent unless an interaction is in progress', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)]);
    const scene = engine.buildScene();
    assert.equal(scene.marquee, null);
    assert.equal(scene.ghost, null);
});

test('a marquee is cleared from the scene once the drag ends', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)]);
    engine.drag = { startX: 10, startY: 10, currentX: 200, currentY: 120, moved: true };
    const during = engine.buildScene();
    assert.ok(during.marquee);
    assert.ok(during.marquee.width > 0 && during.marquee.height > 0);

    engine.drag = null;
    assert.equal(engine.buildScene().marquee, null);
});

test('bars outside the visible time range are culled', () => {
    const engine = makeSceneEngine([bar('near', 'r0', 1, 2), bar('far', 'r0', 40, 42)]);
    const ids = engine.buildScene().bars.map(b => b.id);
    assert.ok(ids.includes('near'));
    assert.ok(!ids.includes('far'), 'a bar well past the viewport should be culled');
});

test('grid lines line up with the hour ticks', () => {
    const engine = makeSceneEngine([]);
    const scene = engine.buildScene();
    assert.deepEqual(scene.gridV.map(line => line.x), scene.hourTicks.map(t => t.x));
    assert.ok(scene.gridV.every(line => line.color == null));
});

test('the hour row fills the band under the date row when the UTC row is off', () => {
    const engine = makeSceneEngine([]);
    const scene = engine.buildScene();
    const v = scene.viewport;

    assert.equal(v.utcRowY, null);
    assert.deepEqual(scene.utcTicks, [], 'no UTC row means no UTC ticks');
    for (const tick of scene.hourTicks) {
        assert.equal(tick.labelY, (v.dateRowHeight + v.axisHeight) / 2);
    }
});

test('showUtcTime adds a second hour row above the zone row', () => {
    // The fixture's window is in May, when Berlin is on CEST (UTC+2). A
    // whole-hour offset puts both rows' ticks on the same instants, so they
    // line up horizontally and differ only in the numbers they show.
    const engine = makeSceneEngine([]);
    engine.config.timeZone = 'Europe/Berlin';
    engine.config.showUtcTime = true;
    engine._rebuildDateFormatters();

    const scene = engine.buildScene();
    const v = scene.viewport;
    assert.equal(v.utcRowY, (v.dateRowHeight + v.axisHeight) / 2);
    assert.ok(scene.hourTicks.length > 0);
    assert.equal(scene.utcTicks.length, scene.hourTicks.length);

    for (let i = 0; i < scene.utcTicks.length; i++) {
        const utc = scene.utcTicks[i];
        const zone = scene.hourTicks[i];
        // UTC above the divider, the zone row below it, neither overlapping
        // the day labels nor spilling out of the axis.
        assert.ok(utc.labelY > v.dateRowHeight && utc.labelY < v.utcRowY);
        assert.ok(zone.labelY > v.utcRowY && zone.labelY < v.axisHeight);
        assert.equal(utc.x, zone.x, '+02:00 keeps the rows aligned');
        assert.equal(Number(utc.label), (Number(zone.label) + 22) % 24,
            'the UTC row must read two hours behind the Berlin row');
    }
});

test('a half-hour zone offsets the UTC row horizontally', () => {
    // Kolkata is +05:30, so the UTC row's whole hours fall between the zone
    // row's - the horizontal shift is the point of drawing it as its own row.
    const engine = makeSceneEngine([]);
    engine.config.timeZone = 'Asia/Kolkata';
    engine.config.showUtcTime = true;
    engine._rebuildDateFormatters();

    const scene = engine.buildScene();
    assert.ok(scene.utcTicks.length > 0);
    const zoneX = scene.hourTicks.map(t => t.x);

    for (const tick of scene.utcTicks) {
        assert.ok(!zoneX.includes(tick.x), 'the rows must not share tick positions here');
        // Both rows still label whole hours - no minutes anywhere.
        assert.match(tick.label, /^\d\d$/);
    }
    // Half an hour at the fixture's 40px/hour scale is 20px of shift.
    const shift = Math.min(...scene.utcTicks.map(u => Math.min(...zoneX.map(x => Math.abs(u.x - x)))));
    assert.ok(Math.abs(shift - 20) < 0.001, `expected a 20px shift, got ${shift}`);
});

// Date-row labels: a 10-character stub so widths are an oracle (7px/char in
// the fixture) rather than depending on the viewer's locale.
const DAY_LABEL = 'Mon, Aug 9';
const DAY_LABEL_W = DAY_LABEL.length * 7;
const DAY_LABEL_PAD = 6;

function sceneAtScroll(scrollX) {
    const engine = makeSceneEngine([]);
    engine._time.formatDate = () => DAY_LABEL;
    engine.scrollX = scrollX;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    return { engine, scene: engine.buildScene() };
}

test('a day that started off-screen pins its title to the left of the content area', () => {
    const startX = 150;
    const { scene } = sceneAtScroll(200);
    const labeled = scene.days.filter(d => d.label != null);
    assert.ok(labeled.length >= 1);
    assert.equal(labeled[0].labelX, startX + DAY_LABEL_PAD);
});

test('the incoming day title pushes the previous title left instead of stacking on it', () => {
    // Day 2's midnight is 40px into the content area: closer than the previous
    // label's width, so the sticky title must slide left rather than sit at
    // the viewport edge on top of "Mon, Aug 10".
    const startX = 150;
    const { engine, scene } = sceneAtScroll(920);
    const labeled = scene.days.filter(d => d.label != null);
    assert.ok(labeled.length >= 2, 'both the outgoing and incoming days must still be labeled');

    const outgoing = labeled[0];
    const incoming = labeled[1];
    assert.ok(outgoing.labelX < startX + DAY_LABEL_PAD, 'the outgoing title must have been pushed');
    assert.ok(
        outgoing.labelX + DAY_LABEL_W <= incoming.labelX,
        'the two titles must not overlap');

    const day2StartX = engine.getTimeToX(START + 24 * HOUR);
    assert.ok(Math.abs(incoming.labelX - (day2StartX + DAY_LABEL_PAD)) < 1e-6,
        'the incoming title sits at its own midnight, not at the viewport edge');
});

test('a title fully pushed past the left edge is omitted', () => {
    // Midnight is only a few pixels into the content area - not enough room
    // for the outgoing label after the incoming one takes its padding.
    const startX = 150;
    const { engine, scene } = sceneAtScroll(957);
    const day2StartX = engine.getTimeToX(START + 24 * HOUR);
    const wouldBeX = day2StartX - DAY_LABEL_PAD - DAY_LABEL_W;
    assert.ok(wouldBeX + DAY_LABEL_W <= startX,
        'fixture: the outgoing title would sit entirely left of the content area');

    const labeled = scene.days.filter(d => d.label != null);
    assert.ok(
        !labeled.some(d => Math.abs(d.labelX - wouldBeX) < 1e-6),
        'a title that has been fully pushed out must not stay in the scene');
    assert.ok(
        labeled.some(d => Math.abs(d.labelX - (day2StartX + DAY_LABEL_PAD)) < 1e-6),
        'the incoming day must still be labeled');
});

test('adjacent day titles never overlap', () => {
    const { scene } = sceneAtScroll(400);
    const labeled = scene.days.filter(d => d.label != null);
    for (let i = 1; i < labeled.length; i++) {
        assert.ok(
            labeled[i - 1].labelX + DAY_LABEL_W <= labeled[i].labelX,
            `day ${i - 1} overlaps day ${i}`);
    }
});

test('UTC ticks do not linger in the scene once the row is turned off', () => {
    const engine = makeSceneEngine([]);
    engine.config.showUtcTime = true;
    assert.ok(engine.buildScene().utcTicks.length > 0);

    engine.config.showUtcTime = false;
    const scene = engine.buildScene();
    assert.ok(scene.hourTicks.length > 0);
    assert.deepEqual(scene.utcTicks, [],
        'the pooled scene must not keep the previous frame\'s UTC row');
});
