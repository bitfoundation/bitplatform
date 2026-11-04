// Scene contract: bar ids, non-overlap within a row, working-time bands,
// surface stripes, Hour12 labels. Renderer paint needs a canvas; this locks
// the display list.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z'); // Monday

function makeSceneEngine(allocations, overrides = {}) {
    const engine = makeBareEngine(overrides);
    engine._measureCtx = { font: '', measureText: (t) => ({ width: t.length * 7 }) };
    engine.config.timeZone = engine.config.timeZone || 'UTC';
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

function rectsOverlap(a, b) {
    return a.x < b.x + b.width && a.x + a.width > b.x
        && a.y < b.y + b.height && a.y + a.height > b.y;
}

test('buildScene bar ids match the fixture and do not overlap in a row', () => {
    const engine = makeSceneEngine([
        bar('a', 'r0', 1, 3),
        bar('b', 'r0', 1.5, 4),
        bar('c', 'r1', 2, 5)
    ]);
    const scene = engine.buildScene();
    assert.deepEqual(scene.bars.map(b => b.id).sort(), ['a', 'b', 'c']);

    const row0 = scene.bars.filter(b => b.id === 'a' || b.id === 'b');
    assert.equal(row0.length, 2);
    assert.equal(rectsOverlap(row0[0], row0[1]), false,
        'stacked bars in one row must not share the same rectangle');
});

test('Hour12 tick labels are 12-hour', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 1, 3)], {
        config: { hour12: true, locale: 'en-US', timeZone: 'UTC' }
    });
    engine._rebuildDateFormatters();
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    const scene = engine.buildScene();
    const labeled = scene.hourTicks.filter(t => t.label);
    assert.ok(labeled.length > 0);
    assert.ok(labeled.some(t => /AM|PM/i.test(t.label)));
});

test('UTC Sat–Sun span produces non-working weekend rects', () => {
    // 2026-05-09 is Saturday.
    const sat = Date.parse('2026-05-09T00:00:00Z');
    const engine = makeSceneEngine([bar('a', 'r0', 0, 1)], {
        config: { timeZone: 'UTC', nonWorkingDays: [0, 6] }
    });
    engine.timeRange = { start: sat, end: sat + 48 * HOUR };
    engine._rebuildDateFormatters();
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    const scene = engine.buildScene();
    assert.ok(scene.nonWorking.length >= 1, 'weekend columns should be shaded');
});

test('09:00–17:00 working hours produce two off-hour bands per day', () => {
    const engine = makeSceneEngine([bar('a', 'r0', 0, 1)], {
        config: {
            timeZone: 'UTC',
            workingHoursStart: 9 * 60,
            workingHoursEnd: 17 * 60
        }
    });
    const scene = engine.buildScene();
    assert.ok(scene.nonWorking.length >= 2, 'morning and evening bands');
});

// 2026-05-04 is a Monday. Column colors are a function of that calendar, so the
// expected color is computed here rather than by replaying the stripe walker.
function dayOrdinal(year, month, day) {
    return Math.floor(Date.UTC(year, month - 1, day) / 86400000);
}

function surfaceEngine(surface, { pph = 4, hours = 24 * 14, firstDayOfWeek = null } = {}) {
    const engine = makeSceneEngine([bar('a', 'r0', 0, 1)], {
        config: { dateRowHeight: 22, firstDayOfWeek, surface }
    });
    engine._pixelsPerHour = pph;
    engine._pixelsPerMs = pph / HOUR;
    engine._visibleWidth = 1050;
    engine._viewportW = 1200;
    engine._viewportH = 600;
    engine.scrollX = 0;
    engine.timeRange = { start: START, end: START + hours * HOUR };
    engine.visibleTimeRange = engine.calculateVisibleTimeRange();
    return engine;
}

test('alternating day colors follow the calendar and stay full height', () => {
    const colors = ['#eef2f6', '#ffffff'];
    const scene = surfaceEngine({ columns: { colors } }).buildScene();
    const stripes = scene.surface;

    assert.ok(stripes.length >= 11 && stripes.length <= 14, 'about a dozen visible days');
    const ordinal = dayOrdinal(2026, 5, 4);
    assert.equal(stripes[0].color, colors[((ordinal % 2) + 2) % 2]);
    assert.equal(stripes[1].color, colors[((ordinal + 1) % 2 + 2) % 2]);
    assert.notEqual(stripes[0].color, stripes[1].color);
    assert.equal(stripes[0].x, 150);
    assert.equal(stripes[0].width, 24 * 4);
    assert.equal(stripes[0].y, 60);
    assert.equal(stripes[0].height, 540);
    assert.equal(scene.surfaceTimeAxis.length, 0, 'axis tint is opt-in');
});

test('a column offset swaps which day gets the first color', () => {
    const colors = ['#aaa', '#bbb'];
    const plain = surfaceEngine({ columns: { colors } }).buildScene();
    const shifted = surfaceEngine({ columns: { colors, offset: 1 } }).buildScene();
    assert.equal(shifted.surface[0].color, plain.surface[1].color);
    assert.notEqual(shifted.surface[0].color, plain.surface[0].color);
});

test('repeat day colors keep a weekday, continuous day colors do not', () => {
    const colors = ['#aaa', '#bbb'];
    // May 4 and May 11 are both Mondays. A 2-color continuous cycle flips,
    // because seven days is odd; repeat restarts each week so Monday stays put.
    const repeated = surfaceEngine({
        columns: { unit: 'day', align: 'repeat', colors }
    }, { firstDayOfWeek: 1 }).buildScene().surface;
    const continuous = surfaceEngine({
        columns: { unit: 'day', align: 'continuous', colors }
    }).buildScene().surface;

    assert.equal(repeated[0].color, repeated[7].color);
    assert.notEqual(continuous[0].color, continuous[7].color);
});

test('week stripes are one color per week', () => {
    const scene = surfaceEngine({
        columns: { unit: 'week', colors: ['#aaa', '#bbb'] }
    }, { firstDayOfWeek: 1 }).buildScene();
    const stripes = scene.surface;
    assert.ok(stripes.length >= 2);
    assert.equal(stripes[0].width, 7 * 24 * 4);
    assert.notEqual(stripes[0].color, stripes[1].color);
});

test('repeat hour span of 8 paints three shifts', () => {
    const colors = ['#111', '#222', '#333'];
    const stripes = surfaceEngine({
        columns: { unit: 'hour', align: 'repeat', span: 8, colors }
    }, { pph: 10 }).buildScene().surface;

    assert.equal(stripes[0].width, 80);
    assert.equal(stripes[1].width, 80);
    assert.deepEqual(stripes.slice(0, 3).map(s => s.color), colors);
});

test('hour stripes thinner than a pixel are not drawn', () => {
    const scene = surfaceEngine({
        columns: { unit: 'hour', colors: ['#111', '#222'] }
    }, { pph: 0.5 }).buildScene();
    assert.equal(scene.surface.length, 0);
});

test('row stripes alternate and a resource background wins', () => {
    const engine = surfaceEngine({
        rows: { colors: ['#111', '#222'] }
    });
    engine.resources[0].background = '#abc';
    const stripes = engine.buildScene().surface;

    assert.equal(stripes.length, 2);
    assert.equal(stripes[0].color, '#abc');
    assert.equal(stripes[1].color, '#222');
    assert.equal(stripes[0].height, 40);
    assert.equal(stripes[0].width, 1050);
});

test('resourceColors fills a row that has no background', () => {
    const scene = surfaceEngine({
        resourceColors: { r1: '#0f0' },
        rows: { colors: ['#111', '#222'] }
    }).buildScene();
    assert.equal(scene.surface[0].color, '#111');
    assert.equal(scene.surface[1].color, '#0f0');
});

test('row span groups consecutive rows onto one color', () => {
    const engine = surfaceEngine({
        rows: { colors: ['#111', '#222'], span: 2 }
    });
    engine.resources.push({ id: 'r2', name: 'R2' }, { id: 'r3', name: 'R3' });
    engine._rebuildResourceStructure();
    const colors = engine.buildScene().surface.map(s => s.color);
    assert.deepEqual(colors, ['#111', '#111', '#222', '#222']);
});

test('columns paint over rows unless rows are on top', () => {
    const over = surfaceEngine({
        columns: { colors: ['#aaa', '#bbb'] },
        rows: { colors: ['#111', '#222'] }
    }).buildScene().surface;
    assert.ok(over.at(-1).height > 400, 'the last pass is the full-height columns');
    assert.ok(over.some(r => r.height === 40));

    const under = surfaceEngine({
        columns: { colors: ['#aaa', '#bbb'] },
        rows: { colors: ['#111', '#222'] },
        combine: 'rowsontop'
    }).buildScene().surface;
    assert.equal(under.at(-1).height, 40);
    assert.ok(under.some(r => r.height > 400));
});

test('checker colors a cell from the day index and the row index', () => {
    const colors = ['#aaa', '#bbb'];
    const cells = surfaceEngine({
        columns: { colors },
        combine: 'checker'
    }, { pph: 20 }).buildScene().surface;

    const column = cells.filter(c => c.x === cells[0].x);
    assert.equal(column.length, 2);
    assert.notEqual(column[0].color, column[1].color);
    assert.equal(column[0].height, 40);
    assert.equal(column[0].color, cells[0].color);
    // The date-row tint is the column color, which matches row 0.
    assert.ok(cells.every(c => c.height === 40));
});

test('a resource background stays a solid row inside a checker', () => {
    const engine = surfaceEngine({
        columns: { colors: ['#aaa', '#bbb'] },
        combine: 'checker'
    }, { pph: 20 });
    engine.resources[0].background = '#abc';
    const cells = engine.buildScene().surface;
    const solid = cells.find(c => c.color === '#abc');
    assert.ok(solid);
    assert.equal(solid.width, 1050);
    assert.ok(cells.some(c => c.y > solid.y && c.width < 600));
});

test('a band with a resource id paints only that row', () => {
    const scene = surfaceEngine({
        bands: [
            { start: START, end: START + 2 * HOUR, color: '#f00' },
            { start: START, end: START + 2 * HOUR, color: '#0f0', resourceId: 'r1' },
            { start: START, end: START + HOUR, color: '#00f', resourceId: 'missing' }
        ]
    }, { pph: 40 }).buildScene();

    assert.equal(scene.surface.length, 2);
    assert.equal(scene.surface[0].color, '#f00');
    assert.equal(scene.surface[0].height, 540);
    assert.equal(scene.surface[0].width, 80);
    assert.equal(scene.surface[1].color, '#0f0');
    assert.equal(scene.surface[1].height, 40);
    assert.equal(scene.surface[1].y, 100);
});

test('shade flags tint the date row and the resource column', () => {
    const scene = surfaceEngine({
        columns: { colors: ['#aaa', '#bbb'] },
        rows: { colors: ['#111', '#222'] },
        shadeTimeAxis: true,
        shadeResourceAxis: true
    }).buildScene();

    assert.equal(scene.surfaceTimeAxis.length, scene.surface.filter(r => r.height > 400).length);
    assert.equal(scene.surfaceTimeAxis[0].y, 0);
    assert.equal(scene.surfaceTimeAxis[0].height, 22);
    assert.equal(scene.surfaceTimeAxis[0].color, scene.surface.find(r => r.height > 400).color);
    assert.equal(scene.surfaceResourceAxis.length, 2);
    assert.deepEqual(scene.surfaceResourceAxis.map(r => r.color), ['#111', '#222']);
    assert.equal(scene.surfaceResourceAxis[0].width, 150);
});

test('clearing the surface drops the previous frame\'s rects', () => {
    const engine = surfaceEngine({
        columns: { colors: ['#111', '#222'] },
        shadeTimeAxis: true
    });
    assert.ok(engine.buildScene().surface.length > 0);
    engine.config.surface = null;
    const scene = engine.buildScene();
    assert.equal(scene.surface.length, 0);
    assert.equal(scene.surfaceTimeAxis.length, 0);
    assert.equal(scene.surfaceResourceAxis.length, 0);
});

test('day boundary lines are colored and the other hour lines are not', () => {
    const colors = ['#15a', '#888'];
    const engine = surfaceEngine(null, { pph: 40, hours: 48 });
    engine.config.axisLines = {
        vertical: { unit: 'day', colors },
        boundariesOnly: true
    };
    const scene = engine.buildScene();

    const midnight = scene.gridV.filter(line => line.color);
    const plain = scene.gridV.filter(line => line.color == null);
    assert.ok(midnight.length >= 1);
    assert.ok(plain.length >= 1, 'hour lines between midnights keep the grid color');
    assert.notEqual(midnight[0].color, plain[0] && plain[0].color);
    const ordinal = dayOrdinal(2026, 5, 4);
    assert.equal(midnight[0].color, colors[((ordinal % 2) + 2) % 2]);
    const seps = scene.days.filter(day => day.sepX != null);
    assert.ok(seps.every(day => day.sepColor));
    assert.equal(seps[0].sepColor, midnight[0].color);
});

test('every vertical line takes the color of the day it sits in', () => {
    const engine = surfaceEngine(null, { pph: 40, hours: 48 });
    engine.config.axisLines = { vertical: { unit: 'day', colors: ['#15a', '#888'] } };
    const scene = engine.buildScene();
    assert.ok(scene.gridV.length > 2);
    assert.ok(scene.gridV.every(line => line.color));
    assert.notEqual(scene.gridV[0].color, scene.gridV[scene.gridV.length - 1].color);
});

test('row lines alternate and a resource line color wins', () => {
    const engine = surfaceEngine(null);
    engine.config.axisLines = { horizontal: { colors: ['#111', '#222'] } };
    engine.resources[0].lineColor = '#abc';
    const scene = engine.buildScene();
    const tops = scene.gridH.filter(line => line.rowIndex === 0 || line.rowIndex === 1);
    assert.equal(tops.find(line => line.rowIndex === 0).color, '#abc');
    assert.equal(tops.find(line => line.rowIndex === 1).color, '#222');
    assert.equal(scene.resourceAxisLines.length, scene.gridH.filter(line => line.color).length);
    assert.equal(scene.resourceAxisLines[0].color, '#abc');
});

test('weekend wash still builds when a surface is set', () => {
    const engine = surfaceEngine({ columns: { colors: ['#aaa', '#bbb'] } });
    engine.config.nonWorkingDays = [0, 6];
    const scene = engine.buildScene();
    assert.ok(scene.surface.length > 0);
    assert.ok(scene.nonWorking.length >= 1);
});
