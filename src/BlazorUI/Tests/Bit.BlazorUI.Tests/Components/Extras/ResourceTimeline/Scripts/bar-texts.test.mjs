// The texts of a bar (alloc.texts): one per position - the top and bottom ones
// at the bar's start edge, its center and its end edge, and the start and end
// ones beside it - each in its own color and font and moved by its own offset.
// These lock where each position anchors its label, what a text's style carries
// into the scene, how stickyBarLabels holds the edge-aligned ones in view, the
// room the measurement keeps for them, and which text names the bar.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine } from './helpers/engine-fixture.mjs';

const HOUR = 3600000;
const START = Date.parse('2026-05-04T00:00:00Z');
// 7px per character, 14px in a font of 20px or more (see makeSceneEngine).
const CHAR = 7;

function at(hours) {
    return START + hours * HOUR;
}

// One row, 40px per hour over two days: the bars area is x 150-1200 (the
// fixture's resource column and viewport), the timeline 1920px wide.
function makeSceneEngine(allocations, sticky = false) {
    const engine = makeBareEngine({ config: { stickyBarLabels: sticky } });
    const ctx = {
        font: '',
        measureText: (t) => ({ width: t.length * (/\b(2\d|[3-9]\d)px/.test(ctx.font) ? 2 * CHAR : CHAR) })
    };
    engine._measureCtx = ctx;
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
    return (node.labels || []).find(l => l.text === text);
}

// Bar 10h-20h: x 550-950, centered at 750.
function bar(texts, extra = {}) {
    return { id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(20), texts, ...extra };
}

// A 1h-47h bar: 1840px wide, from x 190 to 2030 unscrolled.
function longBar(texts) {
    return { id: 'a', resourceId: 'r0', startTime: at(1), endTime: at(47), texts };
}

const ALL = {
    topStart: { text: 'TS' },
    topCenter: { text: 'TC' },
    topEnd: { text: 'TE' },
    start: { text: 'S' },
    end: { text: 'E' },
    bottomStart: { text: 'BS' },
    bottomCenter: { text: 'BC' },
    bottomEnd: { text: 'BE' }
};

test('the top and bottom texts anchor at the start edge, the center and the end edge of the bar', () => {
    const engine = makeSceneEngine([bar(ALL)]);
    const node = barNode(engine);

    const ts = label(node, 'TS'), tc = label(node, 'TC'), te = label(node, 'TE');
    const bs = label(node, 'BS'), bc = label(node, 'BC'), be = label(node, 'BE');

    assert.deepEqual([ts.x, ts.align], [550, 'left']);
    assert.deepEqual([tc.x, tc.align], [750, 'center']);
    assert.deepEqual([te.x, te.align], [950, 'right']);
    assert.deepEqual([bs.x, bs.align], [550, 'left']);
    assert.deepEqual([bc.x, bc.align], [750, 'center']);
    assert.deepEqual([be.x, be.align], [950, 'right']);

    // The top ones share one line just above the bar, the bottom ones one just below it.
    const gap = engine.config.barLabelGap;
    for (const l of [ts, tc, te]) {
        assert.equal(l.baseline, 'bottom');
        assert.equal(l.y, node.y - gap);
    }
    for (const l of [bs, bc, be]) {
        assert.equal(l.baseline, 'top');
        assert.equal(l.y, node.y + node.height + gap);
    }
});

test('the start and end texts sit beside the bar on its center line', () => {
    const engine = makeSceneEngine([bar(ALL)]);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;
    const centerY = node.y + node.height / 2;

    const s = label(node, 'S'), e = label(node, 'E');
    assert.deepEqual([s.x, s.y, s.align, s.baseline], [550 - gap, centerY, 'right', 'middle']);
    assert.deepEqual([e.x, e.y, e.align, e.baseline], [950 + gap, centerY, 'left', 'middle']);
});

test('a text carries its color and font into the scene and is moved by its offsets', () => {
    const engine = makeSceneEngine([bar({
        topStart: { text: 'TS', color: 'tomato', font: 'italic 600 12px serif', offsetX: 5, offsetY: -3 },
        bottomEnd: { text: 'BE' }
    })]);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    const ts = label(node, 'TS');
    assert.equal(ts.x, 555);
    assert.equal(ts.y, node.y - gap - 3);
    assert.equal(ts.color, 'tomato');
    assert.equal(ts.font, 'italic 600 12px serif');

    // A text with no style of its own leaves the renderer's defaults in force.
    const be = label(node, 'BE');
    assert.equal(be.x, 950);
    assert.equal(be.color, null);
    assert.equal(be.font, null);
});

test('an empty or missing text draws nothing at its position', () => {
    const engine = makeSceneEngine([
        bar({ topStart: { text: '' }, topEnd: null, bottomCenter: { color: 'red' } }),
        { id: 'b', resourceId: 'r0', startTime: at(22), endTime: at(24), texts: null }
    ]);

    assert.equal(barNode(engine, 'a').labels, null);
    assert.equal(barNode(engine, 'b').labels, null);
});

test('a shared end text keeps its style and offset', () => {
    const engine = makeSceneEngine([
        { id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(11), texts: { end: { text: 'AMS', color: 'navy', font: '10px serif', offsetY: 2 } } },
        { id: 'b', resourceId: 'r0', startTime: at(11.75), endTime: at(13), texts: { start: { text: 'AMS' } } }
    ]);
    engine.config.collapseMatchingEdgeLabels = true;
    engine._indexAllocations();
    const node = barNode(engine, 'a');
    const shared = label(node, 'AMS');

    assert.equal(shared.align, 'center', 'the pair draws the text once, in the gap');
    assert.equal(shared.color, 'navy');
    assert.equal(shared.font, '10px serif');
    assert.equal(shared.y, node.y + node.height / 2 + 2);
    assert.equal(label(barNode(engine, 'b'), 'AMS'), undefined);
});

test('stickyBarLabels holds the start-aligned texts in from the start edge of the bars area', () => {
    const engine = makeSceneEngine([longBar({ topStart: { text: 'TS' }, bottomStart: { text: 'BS' }, topEnd: { text: 'TE' } })], true);
    scrollTo(engine, 1000);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    // The bar now runs from x -810 to 1030.
    assert.equal(label(node, 'TS').x, 150 + gap);
    assert.equal(label(node, 'BS').x, 150 + gap);
    assert.equal(label(node, 'TE').x, 1030, 'an end edge in view stays where it is');
});

test('stickyBarLabels holds the end-aligned texts in from the end edge of the bars area', () => {
    const engine = makeSceneEngine([longBar({ topEnd: { text: 'TE' }, bottomEnd: { text: 'BE' }, topStart: { text: 'TS' } })], true);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    // The bar runs from x 190 to 2030, past the 1200 the bars area ends at.
    assert.equal(label(node, 'TE').x, 1200 - gap);
    assert.equal(label(node, 'BE').x, 1200 - gap);
    assert.equal(label(node, 'TS').x, 190, 'a start edge in view stays where it is');
});

test('a held edge-aligned text leaves the view with its bar', () => {
    const text = 'Long flight'; // 77px
    const engine = makeSceneEngine([longBar({ topStart: { text } })], true);
    // The bar ends at x 180: less room in view than the text takes.
    scrollTo(engine, 1850);

    assert.equal(label(barNode(engine), text).x, 180 - text.length * CHAR);
});

test('without stickyBarLabels an edge-aligned text stays at its edge, off screen', () => {
    const engine = makeSceneEngine([longBar({ topStart: { text: 'TS' } })]);
    scrollTo(engine, 1000);

    assert.equal(label(barNode(engine), 'TS').x, -810);
});

test('a text wider than its bar stays at its edge', () => {
    const engine = makeSceneEngine([{
        id: 'a', resourceId: 'r0', startTime: at(0), endTime: at(1),
        texts: { topStart: { text: 'Long flight' }, bottomEnd: { text: 'Long flight!' } }
    }], true);
    scrollTo(engine, 20);
    const node = barNode(engine);

    assert.equal(label(node, 'Long flight').x, 130);
    assert.equal(label(node, 'Long flight!').x, 170);
});

test('a text overlapping one drawn before it on its line is left out', () => {
    // Bar 10h-12h: x 550-630, centered at 590.
    const engine = makeSceneEngine([{
        id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(12),
        texts: {
            topCenter: { text: 'Heavy' },     // 572.5-607.5
            topStart: { text: 'D-ABYT' },     // 550-592: over the center one
            topEnd: { text: 'Ck' },           // 616-630: clear of it
            bottomStart: { text: 'D-ABYT' },  // 550-592
            bottomEnd: { text: 'C-check' }    // 581-630: over the start one
        }
    }]);
    const node = barNode(engine);

    assert.ok(label(node, 'Heavy'), 'the center text is drawn first');
    assert.equal(label(node, 'Ck').x, 630);
    assert.equal(node.labels.filter(l => l.text === 'D-ABYT').length, 1, 'only the bottom start text is clear');
    assert.equal(label(node, 'D-ABYT').baseline, 'top');
    assert.equal(label(node, 'C-check'), undefined, 'the end text gives way to the start one');
});

test('a text moved off its line by its offset is not in the way of the others', () => {
    const engine = makeSceneEngine([{
        id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(12),
        texts: { topCenter: { text: 'Heavy' }, topStart: { text: 'D-ABYT', offsetY: -30 } }
    }]);
    const node = barNode(engine);

    assert.ok(label(node, 'Heavy'));
    assert.ok(label(node, 'D-ABYT'));
});

test('stickyBarLabels keeps the centered text clear of an end-aligned one held at the end of the bars area', () => {
    const center = 'Heavy maintenance'; // 119px
    const engine = makeSceneEngine([longBar({ topStart: { text: 'TS' }, topCenter: { text: center }, topEnd: { text: 'C-check' } })], true);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    // The bar runs from x 190 to 2030: the end text is held at 1200 - gap, 49px wide.
    const te = label(node, 'C-check');
    const tc = label(node, center);
    assert.equal(te.x, 1200 - gap);
    assert.ok(tc, 'the centered text is still drawn');
    assert.equal(tc.x + 119 / 2, te.x - 49 - gap, 'it ends the label gap before the end text');
});

test('stickyBarLabels keeps the centered text clear of a start-aligned one held at the start of the bars area', () => {
    const center = 'Heavy maintenance'; // 119px
    const engine = makeSceneEngine([longBar({ topStart: { text: 'TS' }, topCenter: { text: center }, topEnd: { text: 'C-check' } })], true);
    // The bar now runs from x -710 to 1130, centered at 210.
    scrollTo(engine, 900);
    const node = barNode(engine);
    const gap = engine.config.barLabelGap;

    const ts = label(node, 'TS');
    const tc = label(node, center);
    assert.equal(ts.x, 150 + gap);
    assert.ok(tc);
    assert.equal(tc.x - 119 / 2, ts.x + 2 * CHAR + gap, 'it starts the label gap after the start text');
});

test('a font the canvas cannot parse falls back to the bar-label font', () => {
    const engine = makeSceneEngine([{
        id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(20),
        texts: { topStart: { text: 'abcd', font: 'bold 24px serif' }, topEnd: { text: 'abcd', font: '12 px Arial' } }
    }]);
    // A canvas ignores the assignment of a font it cannot parse, keeping the last one.
    let current = '11px sans-serif';
    engine._measureCtx = {
        get font() { return current; },
        set font(v) { if (/\dpx/.test(v)) current = v; },
        measureText: (t) => ({ width: t.length * (/\b(2\d|[3-9]\d)px/.test(current) ? 2 * CHAR : CHAR) })
    };
    // Drop what indexing measured with the fixture's context, which takes any font.
    engine._decorGen++;
    const node = barNode(engine);

    const te = node.labels.find(l => l.text === 'abcd' && l.align === 'right');
    assert.equal(te.font, null, 'the scene draws it in the bar-label font');
    assert.equal(engine._textWidth(engine.allocations[0].texts.topEnd), 4 * CHAR, 'and it is measured in it, not in the last font set');
    assert.equal(engine._textLineHeight(engine.allocations[0].texts.topEnd, engine._labelMetrics()), engine._labelMetrics().lineHeight);
});

test('the line height of a text is read off its font size, not off a line height in px', () => {
    const engine = makeSceneEngine([]);
    const metrics = engine._labelMetrics();
    const height = (font) => engine._textLineHeight({ text: 'x', font }, metrics);

    assert.equal(height('600 0.75rem/24px sans-serif'), metrics.lineHeight, 'a size the canvas does not resolve keeps the bar-label line');
    assert.equal(height('bold 20px/40px serif'), 24);
    assert.equal(height('italic 10px serif'), 12);
});

test('the line height of a text is read off its effective size in any unit', () => {
    const engine = makeSceneEngine([]);
    // A canvas serializes the font it is handed with its size computed to px and
    // no line height: 1rem is 16px here, 1pt 4/3px.
    let current = '11px sans-serif';
    engine._measureCtx = {
        get font() { return current; },
        set font(v) {
            current = v.replace(/(\d*\.?\d+)(rem|pt)(\/\S+)?/, (_, n, unit) => `${unit === 'rem' ? n * 16 : n * 4 / 3}px`);
        },
        measureText: (t) => ({ width: t.length * CHAR })
    };
    engine._decorGen++;
    const metrics = engine._labelMetrics();
    const height = (font) => engine._textLineHeight({ text: 'x', font }, metrics);

    assert.equal(height('600 1.25rem/40px sans-serif'), 24, '1.25rem is 20px');
    assert.equal(height('15pt serif'), 24, '15pt is 20px');
    assert.equal(height('bold 20px/40px serif'), 24, 'a px size is as it was');
    assert.equal(height('italic 10px serif'), 12);
});

test('the measurement keeps room past the other edge for an edge-aligned text wider than its bar', () => {
    const engine = makeSceneEngine([
        { id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(11), texts: { topStart: { text: 'Twelve chars' } } },
        { id: 'b', resourceId: 'r0', startTime: at(20), endTime: at(21), texts: { bottomEnd: { text: 'Eight ch' } } }
    ]);
    const scale = engine._pixelsPerMs;
    const a = engine.allocations[0];
    const b = engine.allocations[1];

    const boxA = engine._decorationBox(a);
    const boxB = engine._decorationBox(b);
    assert.equal(boxA.startAligned, 12 * CHAR);
    assert.equal(boxA.endAligned, 0);
    assert.equal(boxB.endAligned, 8 * CHAR);

    // The 40px bar: the 84px start-aligned text reaches 44px past its end, the
    // 56px end-aligned one 16px before its start.
    const spanA = engine._paintedSpan(a, scale, { lead: 0, trail: 0 }, engine._edgeLabelState(a));
    const spanB = engine._paintedSpan(b, scale, { lead: 0, trail: 0 }, engine._edgeLabelState(b));
    assert.equal(Math.round(spanA.trail * scale), 44);
    assert.equal(spanA.lead, 0);
    assert.equal(Math.round(spanB.lead * scale), 16);
    assert.equal(spanB.trail, 0);
});

test('a text is measured in its own font, for its width and for the room above or below the bar', () => {
    const engine = makeSceneEngine([
        { id: 'a', resourceId: 'r0', startTime: at(10), endTime: at(11), texts: { topCenter: { text: 'abcd' } } },
        { id: 'b', resourceId: 'r0', startTime: at(20), endTime: at(21), texts: { topCenter: { text: 'abcd', font: 'bold 24px serif' } } }
    ]);
    const plain = engine._decorationBox(engine.allocations[0]);
    const large = engine._decorationBox(engine.allocations[1]);

    assert.equal(plain.width, 4 * CHAR);
    assert.equal(large.width, 8 * CHAR, 'the width cache keeps the two fonts apart');
    assert.ok(large.above > plain.above, 'a larger font takes a taller line above the bar');
    assert.equal(large.below, plain.below);
});

test('the first text in label order names the bar', () => {
    const engine = makeSceneEngine([]);
    const named = (texts) => engine._barLabel({ id: 'x', resourceId: 'r0', startTime: 0, endTime: 1, texts });

    assert.equal(named(ALL), 'TC');
    assert.equal(named({ bottomEnd: { text: 'BE' }, topStart: { text: 'TS' } }), 'TS');
    assert.equal(named({ end: { text: 'E' }, start: { text: 'S' }, bottomCenter: { text: 'BC' } }), 'S');
    assert.equal(named({ bottomEnd: { text: 'BE' }, bottomStart: { text: 'BS' } }), 'BS');
    assert.equal(named({ topCenter: { text: '' }, bottomEnd: { text: 'BE' } }), 'BE');
    assert.equal(named(null), '');
});
