// stickyBarLabels: what is centered on a bar - its above/below labels and icons,
// its center icons, a section's centered content - follows the part of the bar
// that is in view, so a long bar keeps its labels on screen while it is scrolled.
// These lock where that content lands: at the bar's center while it is fully in
// view, pushed in from the edge of the bars area while it would be cut off
// there, never past the bar's own edges, and left alone when the option is off,
// for a bar too narrow to slide in, and for the start/end labels. A label wider
// than its bar holds nothing else back, the center icons slide between the
// icons inside the bar and a section's centered content steps aside for them.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');
// 7px per character (see makeSceneEngine).
const CHAR = 7;

function at(hours) {
    return START + hours * HOUR;
}

// One row, 40px per hour over two days: the bars area is x 150-1200 (the
// fixture's resource column and viewport), the timeline 1920px wide.
function makeSceneEngine(allocations, sticky = true) {
    const engine = makeBareEngine({ config: { stickyBarLabels: sticky } });
    engine._measureCtx = { font: '', measureText: (t) => ({ width: t.length * CHAR }) };
    engine.config.timeZone = 'UTC';
    engine._rebuildDateFormatters();

    engine.resources = [{ id: 'r0', name: 'Resource 0' }];
    engine._rebuildResourceStructure();

    engine.timeRange = { start: START, end: START + 48 * HOUR };
    engine.allocations = allocations.slice();
    engine._indexAllocations();

    engine._pixelsPerHour = 40;
    engine._pixelsPerMs = 40 / HOUR;
    engine._visibleWidth = 1050;
    scrollTo(engine, 0);
    // Icons are only laid out once their image has loaded; stub the cache.
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;
    return engine;
}

function scrollTo(engine, x) {
    engine.scrollX = x;
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
}

function barNode(engine, id = 'a') {
    return engine.buildScene().bars.find(b => b.id === id);
}

function label(node, text) {
    return node.labels.find(l => l.text === text);
}

// A 1h-47h bar: 1840px wide, from x 190 to 2030 unscrolled.
function longBar(extra = {}) {
    return { id: 'a', resourceId: 'r0', startTime: at(1), endTime: at(47), ...extra };
}

const TEXT = 'Long flight'; // 77px
const HALF = TEXT.length * CHAR / 2;

test('a label fully in view stays at the bar center', () => {
    // Bar 10h-20h: x 550-950, centered at 750.
    const engine = makeSceneEngine([longBar({ startTime: at(10), endTime: at(20), textAbove: TEXT })]);
    const node = barNode(engine);

    assert.equal(label(node, TEXT).x, 750);
});

test('a label whose bar center is scrolled past the left edge is pinned at that edge', () => {
    const engine = makeSceneEngine([longBar({ textAbove: TEXT, textBelow: TEXT + '!' })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    // Bar center would be at x 110, under the resource column. Both labels share
    // one vertical line, set by the wider of them.
    const widest = (TEXT.length + 1) * CHAR / 2;
    for (const l of node.labels) {
        assert.equal(l.align, 'center');
        assert.equal(l.x, 150 + gap + widest);
    }
});

test('the option off keeps the label at the bar center, off screen', () => {
    const engine = makeSceneEngine([longBar({ textAbove: TEXT })], false);
    scrollTo(engine, 1000);
    const node = barNode(engine);

    assert.equal(label(node, TEXT).x, 110);
});

test('a label whose bar center is past the right edge is pinned at that edge', () => {
    // Bar 20h-47h: x 950-2030 unscrolled, centered at 1490.
    const engine = makeSceneEngine([longBar({ startTime: at(20), textBelow: TEXT })]);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    assert.equal(label(node, TEXT).x, 1200 - gap - HALF);
});

test('a pinned label leaves the view with the edge of its bar', () => {
    const engine = makeSceneEngine([longBar({ textAbove: TEXT })]);
    // The bar ends at x 180: less room in view than the label takes.
    scrollTo(engine, 1850);
    const node = barNode(engine);

    assert.equal(label(node, TEXT).x + HALF, node.x + node.width, 'the label must end at the bar end');
});

test('a label wider than its bar stays centered', () => {
    // A 1h bar (40px) under a 77px label, its center under the resource column.
    const engine = makeSceneEngine([longBar({ startTime: at(0), endTime: at(1), textAbove: TEXT })]);
    scrollTo(engine, 20);
    const node = barNode(engine);

    assert.equal(label(node, TEXT).x, 150);
});

test('above and below icons move with the labels, center icons on a line of their own', () => {
    const engine = makeSceneEngine([longBar({
        textAbove: TEXT,
        icons: [
            { source: 'above.png', position: 'above' },
            { source: 'below.png', position: 'below' },
            { source: 'center.png', position: 'center' },
            { source: 'inside.png', position: 'above', inside: true },
            { source: 'start.png', position: 'start' }
        ]
    })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;
    const x = label(node, TEXT).x;
    const icons = Object.fromEntries(node.icons.map(i => [i.source, i]));

    for (const source of ['above.png', 'below.png', 'inside.png']) {
        assert.equal(icons[source].x + icons[source].width / 2, x, `${source} must share the label's line`);
    }
    // The center icon is pinned by its own width, not the label's.
    assert.equal(icons['center.png'].x, 150 + gap);
    // The start icon hugs the bar's start edge, scrolled out of view.
    assert.ok(icons['start.png'].x + icons['start.png'].width < node.x);
});

test('a label wider than its bar does not hold the others back', () => {
    // Bar 10h-15h: x 550-750 unscrolled (200px), at x 50-250 scrolled, its
    // center (150) at the edge of the bars area.
    const wide = 'X'.repeat(40); // 280px
    const engine = makeSceneEngine([longBar({ startTime: at(10), endTime: at(15), textAbove: 'A1', textBelow: wide })]);
    scrollTo(engine, 500);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    assert.equal(label(node, 'A1').x, 150 + gap + 'A1'.length * CHAR / 2);
    assert.equal(label(node, wide).x, 150);
});

test('the center icons stay clear of the icons inside the bar\'s end', () => {
    const engine = makeSceneEngine([longBar({
        icons: [
            { source: 'center.png', position: 'center' },
            { source: 'end.png', position: 'end', inside: true }
        ]
    })]);
    // The bar ends at x 180: its end icon covers x 161-177.
    scrollTo(engine, 1850);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;
    const icons = Object.fromEntries(node.icons.map(i => [i.source, i]));

    assert.equal(icons['end.png'].x, 161);
    assert.equal(icons['center.png'].x + icons['center.png'].width, icons['end.png'].x - gap);
});

test('a section\'s centered text steps aside for the bar\'s center icons', () => {
    const engine = makeSceneEngine([longBar({
        height: 20,
        icons: [{ source: 'center.png', position: 'center' }],
        sections: [{ text: 'Hold' }]
    })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;
    const icon = node.icons.find(i => i.source === 'center.png');
    const text = label(node, 'Hold');

    // Both would be pinned at the left edge; the text moves to the nearer side.
    assert.equal(icon.x, 150 + gap);
    assert.equal(text.x - 'Hold'.length * CHAR / 2, icon.x + icon.width + gap);
});

test('a section\'s centered text with no room beside the center icons is left out', () => {
    // Bar 10h-11h: x 550-590, room for the 16px icon but not the text beside it.
    const engine = makeSceneEngine([longBar({
        startTime: at(10), endTime: at(11), height: 20,
        icons: [{ source: 'center.png', position: 'center' }],
        sections: [{ text: 'Go' }]
    })]);
    const node = barNode(engine);

    assert.equal(node.icons.length, 1);
    assert.equal(node.labels, null);

    // The option off draws both at the center, as it always has.
    engine.config.stickyBarLabels = false;
    assert.equal(label(barNode(engine), 'Go').x, 570);
});

test('a label wider than the bars area is centered in it', () => {
    const wide = 'X'.repeat(42); // 294px
    const engine = makeSceneEngine([longBar({ textAbove: wide })]);
    // A 250px bars area: x 150-400.
    engine._viewportW = 400;
    engine._visibleWidth = 250;
    scrollTo(engine, 1000);
    const node = barNode(engine);

    assert.equal(label(node, wide).x, 275);
});

test('the start and end labels stay beside the bar', () => {
    const engine = makeSceneEngine([longBar({ textStart: 'AMS', textEnd: 'JFK' })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    assert.equal(label(node, 'AMS').x, node.x - gap);
    assert.equal(label(node, 'JFK').x, engine.getTimeToX(at(47)) + gap);
});

test('a section\'s centered text follows the section\'s visible part', () => {
    const engine = makeSceneEngine([longBar({
        height: 20,
        sections: [{ text: 'Hold' }, { startTime: at(1), endTime: at(2), text: 'Go', textPosition: 'start' }]
    })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    assert.equal(label(node, 'Hold').x, 150 + gap + 'Hold'.length * CHAR / 2);
    // Text at a section's edge keeps hugging it.
    assert.equal(label(node, 'Go').x, engine.getTimeToX(at(1)) + gap);
});

test('a section\'s centered icon and text stay one group when pinned', () => {
    const engine = makeSceneEngine([longBar({
        height: 20,
        sections: [{ icon: 'a.png', iconPosition: 'center', text: 'Hold' }]
    })]);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;
    const [icon] = node.icons;
    const text = label(node, 'Hold');

    assert.equal(icon.x, 150 + gap);
    assert.equal(text.align, 'left');
    assert.equal(text.x, icon.x + icon.width + gap);
});

test('scrolling back puts the label at the bar center again', () => {
    const engine = makeSceneEngine([longBar({ startTime: at(10), endTime: at(20), textAbove: TEXT })]);
    // Bar at x -100-300: its center, x 100, is under the resource column.
    scrollTo(engine, 650);
    assert.equal(label(barNode(engine), TEXT).x, 150 + engine.config.barLabelGap + HALF);
    scrollTo(engine, 0);
    assert.equal(label(barNode(engine), TEXT).x, 750);
});
