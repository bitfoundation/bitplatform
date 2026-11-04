// Hover tooltip. Runs against a minimal DOM stub rather than a real browser -
// enough to assert the show-delay, subject switching and edge flipping, which
// is where the behaviour actually lives. The engine's side of it (what a hover
// resolves to, and the text it asks for) needs no DOM at all.

import test from 'node:test';
import assert from 'node:assert/strict';

import { Tooltip, makeZonedEngine } from './helpers/engine-fixture.mjs';

// The window the fixture loaded the modules against, put back after each stub.
const fixtureWindow = globalThis.window;

// Minimal document/window stub. The tooltip only creates one div, appends it to
// body, writes styles and measures itself.
function installDom({ width = 1000, height = 800, boxW = 100, boxH = 40 } = {}) {
    const body = { children: [] };
    const made = [];

    globalThis.document = {
        createElement() {
            const el = {
                style: {},
                textContent: '',
                remove() {
                    const i = body.children.indexOf(el);
                    if (i >= 0) body.children.splice(i, 1);
                },
                getBoundingClientRect: () => ({ width: boxW, height: boxH })
            };
            made.push(el);
            return el;
        },
        body: {
            appendChild(el) { body.children.push(el); }
        }
    };
    globalThis.window = { innerWidth: width, innerHeight: height };

    return { body, made };
}

function uninstallDom() {
    delete globalThis.document;
    globalThis.window = fixtureWindow;
}

const STYLE = { font: '11px sans-serif', background: '#212529', color: '#fff' };

// The tooltip schedules through setTimeout; give the timer a chance to run.
const tick = (ms) => new Promise(r => setTimeout(r, ms));

test('nothing is added to the document until a tooltip is actually shown', () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(10, 10);
        assert.equal(body.children.length, 0, 'a timeline that is never hovered adds no element');
    } finally {
        uninstallDom();
    }
});

test('the tooltip appears only after the configured delay', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        tip.show({ id: 'a' }, 'Bar A', 40);

        assert.equal(body.children.length, 0, 'must not appear before the delay elapses');
        await tick(80);
        assert.equal(body.children.length, 1);
        assert.equal(body.children[0].textContent, 'Bar A');
        assert.equal(body.children[0].style.display, 'block');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('hiding before the delay elapses cancels the pending show', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        tip.show({ id: 'a' }, 'Bar A', 40);
        tip.hide();

        await tick(80);
        assert.equal(body.children.length, 0, 'a cancelled tooltip must never appear');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('the same subject describing itself differently refreshes the visible text', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        const bar = { id: 'a' };
        tip.show(bar, 'Bar A 08:00', 10);
        await tick(40);

        tip.show(bar, 'Bar A 09:00', 10000);
        assert.equal(body.children[0].textContent, 'Bar A 09:00');
        assert.equal(body.children[0].style.display, 'block');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('the same subject describing itself differently before it shows appears with the new text', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        const bar = { id: 'a' };
        tip.show(bar, 'Bar A 08:00', 30);
        tip.show(bar, 'Bar A 09:00', 30);

        await tick(80);
        assert.equal(body.children[0].textContent, 'Bar A 09:00');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('moving to another bar while visible swaps content immediately', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        tip.show({ id: 'a' }, 'Bar A', 10);
        await tick(40);
        assert.equal(body.children[0].textContent, 'Bar A');

        tip.show({ id: 'b' }, 'Bar B', 10000);
        assert.equal(body.children[0].textContent, 'Bar B',
            'already-visible tooltips should not re-serve the delay');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('empty content takes a visible tooltip down, for the same subject and for another', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        const bar = { id: 'a' };
        tip.show(bar, 'Bar A', 10);
        await tick(40);
        assert.equal(body.children[0].style.display, 'block');

        tip.show(bar, '', 10);
        assert.equal(body.children[0].style.display, 'none', 'the same subject with nothing to say');

        tip.show(bar, 'Bar A again', 10);
        assert.equal(body.children[0].style.display, 'none', 'content coming back waits out the delay');
        await tick(40);
        assert.equal(body.children[0].style.display, 'block');
        assert.equal(body.children[0].textContent, 'Bar A again');

        tip.show({ id: 'b' }, '', 10);
        assert.equal(body.children[0].style.display, 'none', 'another subject with nothing to say');
        await tick(40);
        assert.equal(body.children[0].style.display, 'none', 'nothing shows for it later either');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('content arriving for a hidden subject that had none shows after the delay', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        const bar = { id: 'a' };
        tip.show(bar, '', 10);
        await tick(40);
        assert.equal(body.children.length, 0, 'nothing to show yet');

        tip.show(bar, 'Bar A', 10);
        await tick(40);
        assert.equal(body.children.length, 1);
        assert.equal(body.children[0].textContent, 'Bar A');
        assert.equal(body.children[0].style.display, 'block');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('re-showing the same subject does not restart the delay', async () => {
    const { body, made } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        const subject = { id: 'a' };
        tip.trackPointer(100, 100);
        tip.show(subject, 'Bar A', 30);
        await tick(10);
        tip.show(subject, 'Bar A', 30);   // same subject, part-way through
        await tick(40);
        assert.equal(body.children.length, 1, 'the original timer should still have fired');
        assert.equal(made.length, 1, 'only one element is ever created');
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('the tooltip flips to the other side near the viewport edges', async () => {
    const { body } = installDom({ width: 500, height: 400, boxW: 120, boxH: 50 });
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(40, 40);
        tip.show({ id: 'a' }, 'x', 0);
        await tick(20);
        // Room to the bottom-right: offset from the pointer.
        assert.equal(body.children[0].style.left, '52px');
        assert.equal(body.children[0].style.top, '52px');

        // Near the bottom-right corner it must flip rather than overflow.
        tip.trackPointer(480, 380);
        tip.show({ id: 'b' }, 'y', 0);
        const left = parseFloat(body.children[0].style.left);
        const top = parseFloat(body.children[0].style.top);
        assert.ok(left + 120 <= 500, `tooltip overflows the right edge (left ${left})`);
        assert.ok(top + 50 <= 400, `tooltip overflows the bottom edge (top ${top})`);
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('the tooltip never positions itself off the top-left edge', async () => {
    const { body } = installDom({ width: 200, height: 150, boxW: 190, boxH: 140 });
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(5, 5);
        tip.show({ id: 'a' }, 'big', 0);
        await tick(20);
        assert.ok(parseFloat(body.children[0].style.left) >= 4);
        assert.ok(parseFloat(body.children[0].style.top) >= 4);
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('dispose removes the element and cancels any pending show', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(10, 10);
        tip.show({ id: 'a' }, 'Bar A', 0);
        await tick(20);
        assert.equal(body.children.length, 1);

        tip.dispose();
        assert.equal(body.children.length, 0);

        // A show queued right before disposal must not resurrect an element.
        tip.show({ id: 'b' }, 'Bar B', 5);
        tip.dispose();
        await tick(30);
        assert.equal(body.children.length, 0);
    } finally {
        uninstallDom();
    }
});

test('the element is styled by its bit-rtm-ttp class, and inline only where it sits and what the config sets', async () => {
    const { body } = installDom();
    try {
        const tip = new Tooltip(STYLE);
        tip.trackPointer(100, 100);
        tip.show({ id: 'a' }, 'Bar A', 0);
        await tick(20);
        const el = body.children[0];

        assert.equal(el.className, 'bit-rtm-ttp');
        assert.equal(el.style.position, 'fixed');
        assert.equal(el.style.pointerEvents, 'none');
        assert.equal(el.style.font, STYLE.font);
        assert.equal(el.style.background, STYLE.background);
        assert.equal(el.style.color, STYLE.color);
        // Its size cap, padding, corners, line height, wrapping and shadow come
        // from the stylesheet rule rather than from inline styles.
        for (const prop of ['maxWidth', 'padding', 'borderRadius', 'lineHeight', 'whiteSpace', 'boxShadow']) {
            assert.equal(el.style[prop], undefined, `${prop} must not be set inline`);
        }
        tip.dispose();
    } finally {
        uninstallDom();
    }
});

test('setStyle restyles the element in place, and one not created yet starts with the new style', async () => {
    const { body } = installDom();
    try {
        const later = new Tooltip(STYLE);
        const next = { font: '13px Segoe UI', background: '#000', color: '#eee' };
        later.setStyle(next);
        assert.equal(body.children.length, 0, 'restyling creates no element');
        later.trackPointer(10, 10);
        later.show({ id: 'a' }, 'A', 0);
        await tick(20);
        assert.equal(body.children[0].style.font, next.font);
        later.dispose();

        const live = new Tooltip(STYLE);
        live.trackPointer(10, 10);
        live.show({ id: 'b' }, 'B', 0);
        await tick(20);
        body.children[0].style.left = 'stale';
        live.setStyle(next);
        const el = body.children[0];
        assert.equal(el.style.left, '22px', 'a visible tooltip is placed again for its new size');
        assert.equal(el.style.font, next.font);
        assert.equal(el.style.background, next.background);
        assert.equal(el.style.color, next.color);
        assert.equal(el.textContent, 'B', 'the content is left as it was');
        live.dispose();
    } finally {
        uninstallDom();
    }
});

// ---- What the engine puts in a tooltip, and when ----

function hidden(id, startHour, endHour, extra = {}) {
    return {
        id,
        resourceId: 'r0',
        startTime: Date.UTC(2026, 4, 4, startHour),
        endTime: Date.UTC(2026, 4, 4, endHour),
        ...extra
    };
}

// An engine that reports what it was asked to show instead of building a real
// tooltip, with one +N marker registered at a known spot in the content area.
function makeHoverEngine(bars, overrides = {}) {
    const engine = makeZonedEngine('UTC');
    Object.assign(engine.config, overrides);
    engine._overflowHits = [{
        x: 400, y: 200, width: 22, height: 14,
        ids: bars.map(b => b.id),
        bars
    }];
    engine.shown = [];
    engine.hovered = [];
    engine._tooltip = {
        trackPointer() {},
        show: (subject, content) => engine.shown.push({ subject, content }),
        hide: () => engine.shown.push(null)
    };
    engine._notifyHover = (id) => engine.hovered.push(id);
    return engine;
}

test('a +N tooltip counts the hidden bars and lists them', () => {
    const engine = makeZonedEngine('UTC');
    const bars = [
        hidden('h1', 9, 10, { textAbove: 'SRV01' }),
        hidden('h2', 11, 12, { tooltip: 'Batch job\nsecond line' }),
        hidden('h3', 13, 14)
    ];

    const lines = engine._buildOverflowTooltip(bars).split('\n');

    assert.equal(lines.length, 4, 'a count and one line per hidden bar');
    assert.equal(lines[0], '3 hidden allocations');
    assert.ok(lines[1].startsWith('SRV01 · '), lines[1]);
    assert.ok(lines[2].startsWith('Batch job · '), 'a host tooltip stands in for a missing label');
    assert.ok(!lines[2].includes('second line'), 'only its first line, to keep the list scannable');
    for (const line of lines.slice(1)) {
        assert.ok(line.includes('–'), `every entry needs its time range: ${line}`);
    }
});

test('a single hidden bar is described in the singular', () => {
    const engine = makeZonedEngine('UTC');
    const text = engine._buildOverflowTooltip([hidden('h1', 9, 10)]);
    assert.equal(text.split('\n')[0], '1 hidden allocation');
});

test('a long +N list is cut off with a count of the rest', () => {
    const engine = makeZonedEngine('UTC');
    const bars = [];
    for (let i = 0; i < 30; i++) bars.push(hidden('h' + i, 1, 2, { textAbove: 'B' + i }));

    const lines = engine._buildOverflowTooltip(bars).split('\n');

    assert.equal(lines[0], '30 hidden allocations');
    assert.ok(lines.length < 12, `a tooltip must not grow with the cluster (${lines.length} lines)`);
    assert.equal(lines[lines.length - 1], `… and ${30 - (lines.length - 2)} more`);
});

test('hovering a +N marker describes it instead of the bars behind it', () => {
    const bars = [hidden('h1', 9, 10, { textAbove: 'SRV01' }), hidden('h2', 11, 12)];
    const engine = makeHoverEngine(bars);
    let barAtCalls = 0;
    engine._barAt = () => { barAtCalls += 1; return null; };

    engine._hoverAt(405, 205, 500, 300);

    assert.equal(barAtCalls, 0, 'the marker wins the hit-test, as it does for a click');
    assert.equal(engine.shown.length, 1);
    assert.equal(engine.shown[0].subject, bars, 'the cluster is the tooltip subject');
    assert.ok(engine.shown[0].content.startsWith('2 hidden allocations'));
    assert.deepEqual(engine.hovered, [null], 'no bar is hovered on a marker');
});

test('a +N marker is described even when a TooltipTemplate is set', () => {
    // There is no allocation to hand the template, so the built-in text is all
    // that can describe the marker.
    const engine = makeHoverEngine([hidden('h1', 9, 10)], { tooltipTemplate: true });
    engine._hoverAt(405, 205, 500, 300);
    assert.equal(engine.shown.length, 1);
    assert.ok(engine.shown[0].content.startsWith('1 hidden allocation'));
});

test('showTooltips off leaves the +N marker silent', () => {
    const engine = makeHoverEngine([hidden('h1', 9, 10)], { showTooltips: false });
    engine._hoverAt(405, 205, 500, 300);
    assert.deepEqual(engine.shown, []);
});
