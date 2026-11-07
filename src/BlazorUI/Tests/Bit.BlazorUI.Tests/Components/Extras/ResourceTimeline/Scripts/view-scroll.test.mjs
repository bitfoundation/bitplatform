// Where the viewport lands across data loads: the first-load centering on
// "now" (autoScrollToNow), keeping the view across a reload
// (preserveScrollOnReload), the "now" indicator's refresh timer, and the
// day/week steps panByDays makes (24-hour or panToDayStart midnight snaps).

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine, ZonedTime } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;

// Applies a horizontal scale (pixels per hour) and the derived scroll extent,
// as _relayout does once the wrapper has a size.
function applyScale(engine, pixelsPerHour) {
    const c = engine.config;
    engine._pixelsPerHour = pixelsPerHour;
    engine._pixelsPerMs = pixelsPerHour / HOUR;
    engine._visibleWidth = engine._viewportW - c.resourceAxisWidth;
    const virtualWidth =
        c.resourceAxisWidth + (engine.timeRange.end - engine.timeRange.start) * engine._pixelsPerMs;
    engine._virtualScrollMaxX = Math.max(0, virtualWidth - engine._viewportW);
}

// A laid-out engine over a three-day range centered on the current time, at
// 60px/hour, with enough minimum-height rows to overflow the fixture's
// viewport (so the vertical position is not simply clamped to zero).
function makeLaidOutEngine(overrides = {}) {
    const engine = makeBareEngine(overrides);
    const now = Date.now();
    engine.timeRange = { start: now - 36 * HOUR, end: now + 36 * HOUR };
    setRows(engine, rowIds(20));
    applyScale(engine, 60);
    return engine;
}

function rowIds(count, prefix = 'r') {
    return Array.from({ length: count }, (_, i) => prefix + i);
}

function setRows(engine, ids) {
    engine._rows = ids.map(id => ({ resource: { id, name: id }, depth: 0, hasChildren: false }));
    engine._rowIndexById = new Map(ids.map((id, i) => [id, i]));
    engine._recomputeRowMetrics();
}

// Screen x of the horizontal center of the content area (where "now" belongs
// after an auto-scroll), and the screen x the "now" line actually sits at.
function contentCenterX(engine) {
    return engine.config.resourceAxisWidth + engine._visibleWidth / 2;
}

// Just enough of a KeyboardEvent for handleKeyDown.
function keyEvent(key, { ctrl = false } = {}) {
    return { key, ctrlKey: ctrl, metaKey: false, altKey: false, shiftKey: false, preventDefault() { } };
}

// A pannable engine: the range is wide enough that a week-long step still has
// somewhere to go, and painting is stubbed out (there is no renderer here).
function makePannableEngine(days, config = {}) {
    const engine = makeLaidOutEngine({ config });
    const now = Date.now();
    engine.timeRange = { start: now, end: now + days * 24 * HOUR };
    applyScale(engine, 60);
    engine.render = () => { };
    return engine;
}

// The time at the left edge of the content area - what a pan moves.
function leadTime(engine) {
    return engine.getXToTime(engine.config.resourceAxisWidth);
}

// Visible duration of the content area, in days: the quantity zoomToDays sets.
function visibleDays(engine) {
    return engine._visibleWidth / (engine._pixelsPerHour * 24);
}

// A zoomable engine: _applyZoom's relayout path is stubbed to the scale +
// scroll-extent math without a renderer or spacer element.
function makeZoomableEngine(days) {
    const engine = makePannableEngine(days);
    engine._relayout = function () {
        this._updateScale();
        const virtualWidth =
            this.config.resourceAxisWidth
            + (this.timeRange.end - this.timeRange.start) * this._pixelsPerMs;
        this._virtualScrollMaxX = Math.max(0, virtualWidth - this._viewportW);
    };
    engine._scheduleWindowCheck = () => { };
    return engine;
}

test('an arrow key steps the view exactly one day via panByDays', () => {
    const engine = makePannableEngine(30);
    const before = leadTime(engine);

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - (before + 24 * HOUR)) < 1);
});

test('a week step is seven day steps', () => {
    const engine = makePannableEngine(30);
    const before = leadTime(engine);

    engine.panByDays(7);

    assert.ok(Math.abs(leadTime(engine) - (before + 7 * 24 * HOUR)) < 1);
});

test('a day step back returns to where a step forward started', () => {
    const engine = makePannableEngine(30);
    engine._setVirtualScrollX(5000);
    const before = engine.scrollX;

    engine.panByDays(1);
    engine.panByDays(-1);

    assert.equal(engine.scrollX, before);
});

test('the arrow keys move the bar focus and leave the time axis alone', () => {
    const engine = makePannableEngine(30);
    let moves = 0;
    engine._moveFocusHorizontal = () => { moves++; };

    engine.handleKeyDown(keyEvent('ArrowRight'));

    assert.equal(moves, 1);
    assert.equal(engine.scrollX, 0, 'arrow keys must leave the time axis alone');
});

test('a pan is clamped to the range and reports that it went nowhere', () => {
    const engine = makePannableEngine(30);
    engine._setVirtualScrollX(engine._virtualScrollMaxX);

    assert.equal(engine.panByDays(1), false);
    assert.equal(engine.scrollX, engine._virtualScrollMaxX);
});

test('a pan does not change the horizontal scale', () => {
    const engine = makePannableEngine(30);
    const scale = engine._pixelsPerHour;

    engine.panByDays(7);

    assert.equal(engine._pixelsPerHour, scale);
});

// A laid-out engine whose pans snap the leading edge to local midnight.
function makeDayStartEngine({ timeZone = 'UTC', rangeStart, days = 40, panToDayStart = true } = {}) {
    const engine = makeLaidOutEngine({ config: { timeZone, panToDayStart } });
    engine._rebuildDateFormatters();
    engine.timeRange = { start: rangeStart, end: rangeStart + days * 24 * HOUR };
    applyScale(engine, 60);
    engine.render = () => { };
    return engine;
}

function setLead(engine, time) {
    engine._setVirtualScrollX((time - engine.timeRange.start) * engine._pixelsPerMs);
}

test('panToDayStart steps from mid-day to the next local midnight', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T15:30:00Z'));

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('panToDayStart from a midnight is the next calendar day, not 24 hours later', () => {
    // Sunday 2026-03-08 00:00 America/New_York to Monday 00:00 is 23 hours:
    // clocks jump 02:00 -> 03:00 that morning.
    const zone = new ZonedTime('America/New_York');
    const sunday = zone.wallClockToTs(2026, 3, 8, 0, 0, 0);
    const monday = zone.addDays(sunday, 1);
    const engine = makeDayStartEngine({
        timeZone: 'America/New_York',
        rangeStart: zone.addDays(sunday, -5)
    });
    setLead(engine, sunday);

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - monday) < 1);
    assert.equal(monday - sunday, 23 * HOUR, 'oracle: the stepped day is 23 hours long');
});

test('panToDayStart treats a fall-back day as one calendar step', () => {
    // Sunday 2026-11-01 00:00 America/New_York to Monday 00:00 is 25 hours:
    // clocks repeat 01:00 that morning.
    const zone = new ZonedTime('America/New_York');
    const sunday = zone.wallClockToTs(2026, 11, 1, 0, 0, 0);
    const monday = zone.addDays(sunday, 1);
    const engine = makeDayStartEngine({
        timeZone: 'America/New_York',
        rangeStart: zone.addDays(sunday, -5)
    });
    setLead(engine, sunday);

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - monday) < 1);
    assert.equal(monday - sunday, 25 * HOUR);
});

test('panToDayStart a week lands on the midnight seven calendar days ahead', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T15:30:00Z'));

    engine.panByDays(7);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-17T00:00:00Z')) < 1);
});

test('panToDayStart still steps when the lead is a fraction of a pixel before midnight', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    // Native scrollLeft is an integer CSS pixel; after a midnight landing the
    // read-back virtual offset can sit just before that instant.
    engine.scrollX -= 0.6;

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('panToDayStart back still steps when the lead is a fraction of a pixel before midnight', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.scrollX -= 0.6;

    engine.panByDays(-1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-09T00:00:00Z')) < 1);
});

test('panToDayStart a week still steps when the lead is a fraction of a pixel before midnight', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.scrollX -= 0.6;

    engine.panByDays(7);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-17T00:00:00Z')) < 1);
});

test('panToDayStart still steps after native scrollLeft quantization', () => {
    const start = Date.parse('2026-06-01T00:07:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    applyScale(engine, 47);
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.wrapper.scrollLeft = Math.floor(engine.scrollX / engine._scrollScaleX);
    engine.scrollX = engine.wrapper.scrollLeft * engine._scrollScaleX;

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('panToDayStart still steps across a scaled scrollbar tick', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    engine._scrollScaleX = 4;
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.scrollX -= 2;

    engine.panByDays(1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('panToDayStart still steps when the lead is a fraction of a pixel after midnight', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.scrollX += 0.6;

    engine.panByDays(1);
    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);

    setLead(engine, Date.parse('2026-06-10T00:00:00Z'));
    engine.scrollX += 0.6;
    engine.panByDays(-1);
    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-09T00:00:00Z')) < 1);
});

test('panToDayStart from a midnight round-trips', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    const midnight = Date.parse('2026-06-10T00:00:00Z');
    setLead(engine, midnight);
    const before = engine.scrollX;

    engine.panByDays(1);
    engine.panByDays(-1);

    assert.equal(engine.scrollX, before);
});

test('panToDayStart back from mid-day lands on that day\'s start, not the original time', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T15:30:00Z'));

    engine.panByDays(-1);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-09T00:00:00Z')) < 1);
});

test('panToDayStart is still clamped to the range', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start, days: 10 });
    engine._setVirtualScrollX(engine._virtualScrollMaxX);

    assert.equal(engine.panByDays(1), false);
    assert.equal(engine.scrollX, engine._virtualScrollMaxX);
});

test('a panByDays(true) argument snaps to midnight even when the option is off', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start, panToDayStart: false });
    setLead(engine, Date.parse('2026-06-10T15:30:00Z'));

    engine.panByDays(1, true);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('a panByDays(false) argument keeps a 24-hour step even when the option is on', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    const afternoon = Date.parse('2026-06-10T15:30:00Z');
    setLead(engine, afternoon);

    engine.panByDays(1, false);

    assert.ok(Math.abs(leadTime(engine) - (afternoon + 24 * HOUR)) < 1);
});

test('a null panByDays override defers to the option', () => {
    const start = Date.parse('2026-06-01T00:00:00Z');
    const engine = makeDayStartEngine({ rangeStart: start });
    setLead(engine, Date.parse('2026-06-10T15:30:00Z'));

    engine.panByDays(1, null);

    assert.ok(Math.abs(leadTime(engine) - Date.parse('2026-06-11T00:00:00Z')) < 1);
});

test('zoomToDays fits exactly that many days into the content area', () => {
    const engine = makeZoomableEngine(30);
    engine._setVirtualScrollX(2000);
    const center = engine.getXToTime(contentCenterX(engine));

    engine.zoomToDays(3);

    assert.ok(Math.abs(visibleDays(engine) - 3) < 1e-9);
    assert.ok(
        Math.abs(engine.getXToTime(contentCenterX(engine)) - center) < 1,
        'the time under the viewport center must stay put');
});

test('zoomToDays(1) is one day in the viewport', () => {
    const engine = makeZoomableEngine(30);

    engine.zoomToDays(1);

    assert.ok(Math.abs(visibleDays(engine) - 1) < 1e-9);
});

test('zoomToDays(7) is a week in the viewport', () => {
    const engine = makeZoomableEngine(30);

    engine.zoomToDays(7);

    assert.ok(Math.abs(visibleDays(engine) - 7) < 1e-9);
});

test('zoomToDays ignores a non-positive span and leaves the scale alone', () => {
    const engine = makeZoomableEngine(30);
    const scale = engine._pixelsPerHour;

    assert.equal(engine.zoomToDays(0), scale);
    assert.equal(engine.zoomToDays(-3), scale);
    assert.equal(engine.zoomToDays(Number.NaN), scale);
    assert.equal(engine._pixelsPerHour, scale);
});

test('zoomToDays clamps to the configured min and max scale', () => {
    const engine = makeZoomableEngine(30);
    engine.config.minPixelsPerHour = 10;
    engine.config.maxPixelsPerHour = 20;

    engine.zoomToDays(1);
    assert.equal(engine._pixelsPerHour, 20, 'a one-day fit is tighter than the max');

    engine.zoomToDays(100);
    assert.equal(engine._pixelsPerHour, 10, 'a hundred-day fit is looser than the min');
});

test('a bound that is not positive keeps its default, and inverted bounds are swapped', () => {
    const engine = makeZoomableEngine(30);

    engine.config.minPixelsPerHour = 0;
    engine.config.maxPixelsPerHour = -5;
    engine.setPixelsPerHour(0.01);
    assert.equal(engine._pixelsPerHour, 0.25, 'the default min stands in for a zero one');
    engine.setPixelsPerHour(5000);
    assert.equal(engine._pixelsPerHour, 1200, 'the default max stands in for a negative one');

    engine.config.minPixelsPerHour = 20;
    engine.config.maxPixelsPerHour = 10;
    engine.setPixelsPerHour(1);
    assert.equal(engine._pixelsPerHour, 10);
    engine.setPixelsPerHour(100);
    assert.equal(engine._pixelsPerHour, 20);
});

test('the first load centers "now" in the content area when autoScrollToNow is set', () => {
    const engine = makeLaidOutEngine({ config: { autoScrollToNow: true } });

    engine._prepareLoadScroll();
    engine._applyPendingScroll();

    assert.ok(
        Math.abs(engine.getTimeToX(Date.now()) - contentCenterX(engine)) < 1,
        'the now line must end up at the center of the content area');
});

test('the first load leaves the view alone without autoScrollToNow', () => {
    const engine = makeLaidOutEngine();

    engine._prepareLoadScroll();
    engine._applyPendingScroll();

    assert.equal(engine.scrollX, 0);
});

test('only the first load centers on "now"', () => {
    const engine = makeLaidOutEngine({ config: { autoScrollToNow: true } });
    engine._prepareLoadScroll();
    engine._applyPendingScroll();

    // The user scrolls elsewhere, then the host reloads its data.
    engine._setVirtualScrollX(0);
    engine._prepareLoadScroll();
    engine._applyPendingScroll();

    assert.equal(engine.scrollX, 0, 'a later load must not pull the view back to now');
});

test('a reload keeps the leading time in view despite a changed range and scale', () => {
    const engine = makeLaidOutEngine({ config: { preserveScrollOnReload: true } });
    engine._firstLoadDone = true;
    engine._setVirtualScrollX(1000);
    const leadTime = engine.getXToTime(engine.config.resourceAxisWidth);

    engine._prepareLoadScroll();
    // The reload widens the range in both directions and zooms out.
    engine.timeRange = {
        start: engine.timeRange.start - 24 * HOUR,
        end: engine.timeRange.end + 24 * HOUR
    };
    applyScale(engine, 30);
    engine._applyPendingScroll();

    assert.ok(
        Math.abs(engine.getXToTime(engine.config.resourceAxisWidth) - leadTime) < 1000,
        'the same time must still sit at the left edge of the content area');
});

test('a reload keeps the top row in view when the rows above it change', () => {
    const engine = makeLaidOutEngine({ config: { preserveScrollOnReload: true } });
    engine._firstLoadDone = true;
    engine._setScrollY(engine._rowContentTop(4));
    assert.ok(engine.scrollY > 0, 'the fixture must actually be scrolled vertically');

    engine._prepareLoadScroll();
    // A row is inserted ahead of the anchored one, moving it further down.
    setRows(engine, ['new', ...rowIds(20)]);
    engine._applyPendingScroll();

    assert.equal(engine._rowIndexAtContentY(engine.scrollY), 5, 'r4 must still be the top row');
});

test('a reload that drops the anchored row keeps the vertical offset', () => {
    const engine = makeLaidOutEngine({ config: { preserveScrollOnReload: true } });
    engine._firstLoadDone = true;
    engine._setScrollY(engine._rowContentTop(4));
    const scrollY = engine.scrollY;

    engine._prepareLoadScroll();
    setRows(engine, rowIds(20, 'x'));
    engine._applyPendingScroll();

    assert.equal(engine.scrollY, scrollY);
});

test('a reload without the flag does not queue anything', () => {
    const engine = makeLaidOutEngine();
    engine._firstLoadDone = true;
    engine._setVirtualScrollX(1000);

    engine._prepareLoadScroll();

    assert.equal(engine._pendingScroll, null);
});

test('centering is skipped when "now" falls outside the loaded range', () => {
    const engine = makeLaidOutEngine({ config: { autoScrollToNow: true } });
    engine.timeRange = { start: Date.now() - 72 * HOUR, end: Date.now() - 48 * HOUR };
    applyScale(engine, 60);

    engine._prepareLoadScroll();
    engine._applyPendingScroll();

    assert.equal(engine.scrollX, 0);
});

test('a streamed load restores the view once the batches have landed', () => {
    const engine = makeLaidOutEngine({ config: { preserveScrollOnReload: true } });
    engine._firstLoadDone = true;
    engine._setVirtualScrollX(1000);
    const leadTime = engine.getXToTime(engine.config.resourceAxisWidth);
    // The layout applies the queued view, as the real one does once it has a size.
    engine._relayout = function () { this._applyPendingScroll(); };
    engine._hideTooltip = () => {};
    const { start, end } = engine.timeRange;
    const resources = engine._rows.map(r => r.resource);

    // beginData queues the view and hands it to endData, because the rows only
    // reach their final heights once the batches have been indexed.
    engine.beginData(resources, start, end, 0);
    engine._setVirtualScrollX(0);

    engine.endData();

    assert.ok(Math.abs(engine.getXToTime(engine.config.resourceAxisWidth) - leadTime) < 1);
});

test('the "now" indicator timer is off when its interval is zero', () => {
    const engine = makeLaidOutEngine({ config: { nowLineRefreshMs: 0 } });
    engine._nowTimer = null;

    engine._startNowTimer();

    assert.equal(engine._nowTimer, null);
});

test('the "now" indicator repaints on its interval, but only when it would move', (t) => {
    t.mock.timers.enable({ apis: ['setInterval'] });
    const engine = makeLaidOutEngine({ config: { nowLineRefreshMs: 1000 } });
    // The wall clock the timer reads, held still until the test moves it.
    let clock = Date.now();
    t.mock.method(Date, 'now', () => clock);
    let renders = 0;
    engine.render = () => { renders++; };
    engine._lastNowX = NaN;
    engine._nowTimer = null;

    engine._startNowTimer();
    try {
        t.mock.timers.tick(1000);
        assert.equal(renders, 1, 'the first tick moves the line off its unknown position');

        // The clock has not advanced, so the line would land on the same pixel
        // and the frame would be identical.
        t.mock.timers.tick(1000);
        assert.equal(renders, 1, 'a tick that would not move the line must not repaint');

        // Two minutes at 60px/hour move the line two pixels.
        clock += 2 * 60000;
        t.mock.timers.tick(1000);
        assert.equal(renders, 2, 'a tick that moves the line must repaint');
    } finally {
        engine._stopNowTimer();
    }
});

test('setPixelsPerHour notifies OnViewChanged once when the scale changes', () => {
    const engine = makeZoomableEngine(30);
    const calls = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name, ...args) => { calls.push({ name, args }); return Promise.resolve(); }
    };

    engine.setPixelsPerHour(40);

    const views = calls.filter(c => c.name === 'OnViewChanged');
    assert.equal(views.length, 1);
    assert.equal(views[0].args.length, 3);
    assert.equal(views[0].args[2], engine._pixelsPerHour);
});

test('scrollToAllocation puts the bar start in the viewport', () => {
    const engine = makeLaidOutEngine();
    const start = engine.timeRange.start + 48 * HOUR;
    const alloc = {
        id: 'far',
        resourceId: engine._rows[0].resource.id,
        startTime: start,
        endTime: start + HOUR
    };
    engine.allocations = [alloc];
    engine._rowIndexById.set(alloc.resourceId, 0);
    engine.render = () => {};

    assert.equal(engine.scrollToAllocation('far'), true);
    const startC = engine._timeToContentX(start);
    assert.ok(startC >= engine.scrollX - 1, 'start is not left of the view');
    assert.ok(startC <= engine.scrollX + engine._visibleWidth + 1, 'start is not right of the view');
});

test('scrollToAllocation returns false for an unknown id', () => {
    const engine = makeLaidOutEngine();
    engine.render = () => {};
    assert.equal(engine.scrollToAllocation('nope'), false);
});
