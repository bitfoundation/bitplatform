// Bar sections: spans of a bar with their own colors, text and icon. These lock
// how a section's bounds resolve (times, percentages, the bar's own edges), that
// nothing a section draws leaves the bar, and where its text and icon land.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');

function makeSceneEngine(allocations) {
    const engine = makeBareEngine();
    // 7px per character, so a label's width is predictable.
    engine._measureCtx = { font: '', measureText: (t) => ({ width: t.length * 7 }) };
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
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    // Icons are only laid out once their image has loaded; stub the cache.
    const loaded = { complete: true, naturalWidth: 16, naturalHeight: 16 };
    engine._getImage = () => loaded;
    return engine;
}

// A 1h-6h bar: 200px wide at 40px per hour.
function sectioned(sections, extra = {}) {
    return {
        id: 'a', resourceId: 'r0',
        startTime: START + HOUR,
        endTime: START + 6 * HOUR,
        height: 20,
        sections,
        ...extra
    };
}

function at(hours) {
    return START + hours * HOUR;
}

test('a section bounded by times covers that span of the bar', () => {
    const engine = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(3), backgroundColor: '#f00' }])
    ]);
    const node = engine.buildScene().bars[0];

    assert.equal(node.sections.length, 1);
    const [s] = node.sections;
    assert.equal(s.x, engine.getTimeToX(at(2)));
    assert.equal(s.width, engine.getTimeToX(at(3)) - engine.getTimeToX(at(2)));
    assert.equal(s.y, node.y);
    assert.equal(s.height, node.height);
    assert.equal(s.color, '#f00');
    assert.equal(s.borderColor, null);
});

test('percentages measure from the bar start, and a time wins over a percentage', () => {
    const engine = makeSceneEngine([
        sectioned([
            // 20%-60% of a 5h bar starting at 1h is 2h-4h.
            { startPercent: 20, endPercent: 60, backgroundColor: '#f00' },
            { startTime: at(5), startPercent: 20, backgroundColor: '#0f0' }
        ])
    ]);
    const [byPercent, byTime] = engine.buildScene().bars[0].sections;

    assert.equal(byPercent.x, engine.getTimeToX(at(2)));
    assert.equal(byPercent.x + byPercent.width, engine.getTimeToX(at(4)));
    assert.equal(byTime.x, engine.getTimeToX(at(5)), 'the time must win for the same edge');
    assert.equal(byTime.x + byTime.width, engine.getTimeToX(at(6)),
        'a missing end must fall back to the bar end');
});

test('section bounds are clamped to the bar and an empty section is dropped', () => {
    const engine = makeSceneEngine([
        sectioned([
            { startTime: at(0), endTime: at(2), backgroundColor: '#f00' },
            { startPercent: -50, endPercent: 150, backgroundColor: '#0f0' },
            { startTime: at(7), endTime: at(8), backgroundColor: '#00f' },
            { startPercent: 60, endPercent: 40, backgroundColor: '#000' }
        ])
    ]);
    const node = engine.buildScene().bars[0];

    assert.deepEqual(node.sections.map(s => s.color), ['#f00', '#0f0'],
        'sections outside the bar or with end before start are not drawn');
    const [early, wide] = node.sections;
    assert.equal(early.x, node.x);
    assert.equal(wide.x, node.x);
    assert.equal(wide.width, node.width);
});

test('a section without colors paints nothing over the bar but still draws its text', () => {
    const engine = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(4), text: 'Taxi' }])
    ]);
    const node = engine.buildScene().bars[0];

    assert.equal(node.sections, null, 'the bar fill should show through');
    assert.equal(node.labels.length, 1);
    const [label] = node.labels;
    assert.equal(label.text, 'Taxi');
    assert.equal(label.align, 'center');
    assert.equal(label.x, (engine.getTimeToX(at(2)) + engine.getTimeToX(at(4))) / 2);
    assert.equal(label.y, node.y + node.height / 2);
    assert.equal(label.color, null, 'no foreground color means the bar-label color');
});

test('section text takes its foreground color and its edge position', () => {
    const engine = makeSceneEngine([
        sectioned([
            { startTime: at(1), endTime: at(3), text: 'A', textPosition: 'start', foregroundColor: '#fff' },
            { startTime: at(3), endTime: at(6), text: 'B', textPosition: 'end' }
        ])
    ]);
    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const byText = Object.fromEntries(node.labels.map(l => [l.text, l]));

    assert.equal(byText.A.color, '#fff');
    assert.equal(byText.A.align, 'left');
    assert.equal(byText.A.x, engine.getTimeToX(at(1)) + gap);
    assert.equal(byText.B.align, 'right');
    assert.equal(byText.B.x, engine.getTimeToX(at(6)) - gap);
});

test('section text that does not fit the section or the bar height is dropped', () => {
    // A 40px section cannot hold 10 characters at 7px each.
    const narrow = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(3), text: 'Turnaround' }])
    ]);
    assert.equal(narrow.buildScene().bars[0].labels, null);

    // The 11px label font does not fit on a 4px bar.
    const thin = makeSceneEngine([
        sectioned([{ text: 'Hi' }], { height: 4 })
    ]);
    assert.equal(thin.buildScene().bars[0].labels, null);
});

test('an icon sharing an edge with the text sits nearer that edge', () => {
    const engine = makeSceneEngine([
        sectioned([
            { startTime: at(1), endTime: at(3), icon: 'a.png', iconPosition: 'start', text: 'A', textPosition: 'start' },
            { startTime: at(3), endTime: at(6), icon: 'b.png', iconPosition: 'end', text: 'B', textPosition: 'end' }
        ])
    ]);
    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const icons = Object.fromEntries(node.icons.map(i => [i.source, i]));
    const labels = Object.fromEntries(node.labels.map(l => [l.text, l]));

    assert.equal(icons['a.png'].x, engine.getTimeToX(at(1)) + gap);
    assert.equal(labels.A.x, icons['a.png'].x + icons['a.png'].width + gap);
    assert.equal(icons['b.png'].x + icons['b.png'].width, engine.getTimeToX(at(6)) - gap);
    assert.equal(labels.B.x, icons['b.png'].x - gap);
    for (const icon of node.icons) {
        assert.equal(icon.y + icon.height / 2, node.y + node.height / 2);
    }
});

test('a centered icon and text form one group centered on the section', () => {
    const engine = makeSceneEngine([
        sectioned([{ icon: 'a.png', iconPosition: 'center', text: 'Hold' }])
    ]);
    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const [icon] = node.icons;
    const [label] = node.labels;

    assert.equal(label.align, 'left');
    assert.equal(label.x, icon.x + icon.width + gap);
    const groupRight = label.x + 'Hold'.length * 7;
    assert.equal((icon.x + groupRight) / 2, node.x + node.width / 2);
});

test('centered text with room beside an edge icon is moved clear of it, not dropped', () => {
    // A 104px section: 3 + 16 (icon) + 3 + 77 (text) + 3 = 102px are needed, but
    // the text centered on the section would cover the icon.
    const engine = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(4.6), icon: 'a.png', text: 'Hello world' }])
    ]);
    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const [icon] = node.icons;
    const [label] = node.labels;
    const width = 'Hello world'.length * 7;

    assert.equal(icon.x, engine.getTimeToX(at(2)) + gap);
    assert.equal(label.align, 'center');
    assert.equal(label.x - width / 2, icon.x + icon.width + gap, 'the text must clear the icon');
    assert.ok(label.x + width / 2 <= engine.getTimeToX(at(4.6)) - gap, 'and stay inside the section');

    // Wider, the text is centered on the section again.
    const wide = makeSceneEngine([
        sectioned([{ startTime: at(1), endTime: at(6), icon: 'a.png', text: 'Hello world' }])
    ]);
    const wideNode = wide.buildScene().bars[0];
    assert.equal(wideNode.labels[0].x, wideNode.x + wideNode.width / 2);
});

test('section text stays drawn at every zoom once its section has room for it', () => {
    // The section is a fixed share of the bar, so it widens with the zoom: once
    // the icon and the text fit, no wider zoom may drop the text again.
    let shown = false;
    for (let pph = 10; pph <= 80; pph += 0.5) {
        const engine = makeSceneEngine([
            sectioned([{ startPercent: 20, endPercent: 60, icon: 'a.png', text: 'Hello world' }])
        ]);
        engine._pixelsPerHour = pph;
        engine._pixelsPerMs = pph / HOUR;
        engine.visibleTimeRange = engine.calculateVisibleTimeRange();
        const node = engine.buildScene().bars[0];
        // 20%-60% of the 1h-6h bar is 2h-4h.
        const room = engine.getTimeToX(at(4)) - engine.getTimeToX(at(2));
        const drawn = !!(node.labels && node.labels.length);
        if (shown) assert.ok(drawn, `the text must not hide again at ${pph}px per hour`);
        assert.equal(drawn, room >= 3 + 16 + 3 + 77 + 3, `at ${pph}px per hour (${room}px section)`);
        shown = drawn;
    }
    assert.ok(shown);
});

test('a centered icon stays clear of text at the section\'s edge', () => {
    // A 120px section: the start text covers 3-80px of it, so the icon centered
    // on the section (52-68px) would sit on the text.
    const engine = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(5), icon: 'a.png', iconPosition: 'center', text: 'Hello world', textPosition: 'start' }])
    ]);
    const node = engine.buildScene().bars[0];
    const gap = engine.config.barLabelGap;
    const [icon] = node.icons;
    const [label] = node.labels;

    assert.equal(label.align, 'left');
    assert.equal(label.x, engine.getTimeToX(at(2)) + gap);
    assert.equal(icon.x, label.x + 'Hello world'.length * 7 + gap);

    // With no room left beside the text, the icon is dropped rather than drawn over it.
    const narrow = makeSceneEngine([
        sectioned([{ startTime: at(2), endTime: at(4.5), icon: 'a.png', iconPosition: 'center', text: 'Hello world', textPosition: 'start' }])
    ]);
    const narrowNode = narrow.buildScene().bars[0];
    assert.equal(narrowNode.icons, null);
    assert.equal(narrowNode.labels.length, 1);
});

test('an icon shrinks to the bar height and is dropped when the section is too narrow', () => {
    const engine = makeSceneEngine([
        sectioned([
            { startTime: at(1), endTime: at(3), icon: 'big.png', iconSize: 32 },
            // 4px at 40px per hour: no room for a 16px icon.
            { startTime: at(4), endTime: at(4.1), icon: 'small.png' }
        ], { height: 12 })
    ]);
    const node = engine.buildScene().bars[0];

    assert.equal(node.icons.length, 1);
    assert.equal(node.icons[0].source, 'big.png');
    assert.equal(node.icons[0].height, 12);
    assert.ok(node.icons[0].y >= node.y && node.icons[0].y + node.icons[0].height <= node.y + node.height);
});

test('sections do not persist onto a later bar without them', () => {
    const engine = makeSceneEngine([
        sectioned([{ startPercent: 0, endPercent: 50, backgroundColor: '#f00', text: 'Hi' }])
    ]);
    assert.equal(engine.buildScene().bars[0].sections.length, 1);
    assert.equal(engine.buildScene().bars[0].sections.length, 1, 'a repeat frame must not grow the list');

    engine.allocations[0].sections = null;
    const node = engine.buildScene().bars[0];
    assert.equal(node.sections, null, 'the reused node must not keep stale sections');
    assert.equal(node.labels, null, 'nor their text');
});
