// What the engine says (the texts option) and how it looks (setTheme), both of
// which the BitResourceTimeline port added: every user-facing string comes from
// config.texts with the English built-ins as its defaults, and a theme forms a
// layer under the colors and fonts the options set.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine, makeZonedEngine, makeIndexedEngine } from './helpers/engine-fixture.mjs';

function captureWarnings(fn) {
    const warnings = [];
    const realWarn = console.warn;
    console.warn = (m) => warnings.push(m);
    try {
        fn();
    } finally {
        console.warn = realWarn;
    }
    return warnings;
}

function hidden(id, startHour, endHour, extra = {}) {
    return {
        id,
        resourceId: 'r0',
        startTime: Date.UTC(2026, 4, 4, startHour),
        endTime: Date.UTC(2026, 4, 4, endHour),
        ...extra
    };
}

// ---- texts ----

test('a config without texts falls back to the built-in English ones', () => {
    const engine = makeZonedEngine('UTC');
    assert.equal(engine.config.texts, undefined, 'fixture: the config carries no texts');

    assert.equal(engine._text('selectionCleared'), 'Selection cleared');
    assert.equal(engine._text('resizeResourceColumn'), 'Resize resource column');
    assert.equal(engine._text('timeRangeFormat', 'a', 'b'), 'a – b');
    assert.equal(engine._buildOverflowTooltip([hidden('h1', 9, 10)]).split('\n')[0], '1 hidden allocation');
});

test('a texts option replaces the built-in texts key by key', () => {
    const engine = makeZonedEngine('UTC');
    const warnings = captureWarnings(() => engine._applyOptions({
        texts: {
            hiddenAllocationsFormat: '{0} verborgene Zuordnungen',
            moreHiddenAllocationsFormat: '… und {0} weitere'
        }
    }));
    assert.deepEqual(warnings, []);

    const bars = [];
    for (let i = 0; i < 30; i++) bars.push(hidden('h' + i, 1, 2, { textAbove: 'B' + i }));
    const lines = engine._buildOverflowTooltip(bars).split('\n');
    assert.equal(lines[0], '30 verborgene Zuordnungen');
    assert.equal(lines[lines.length - 1], `… und ${30 - (lines.length - 2)} weitere`);

    // The keys the options did not name keep their defaults.
    assert.equal(engine.config.texts.hiddenAllocationFormat, '{0} hidden allocation');
    assert.equal(engine._buildOverflowTooltip([hidden('h1', 9, 10)]).split('\n')[0], '1 hidden allocation');
    assert.ok(lines[1].startsWith('B0 · '), 'an item keeps the built-in item format');
});

test('an unknown text key is warned about and not stored; a null text keeps the default', () => {
    const engine = makeBareEngine();
    const warnings = captureWarnings(() => engine._applyOptions({
        texts: { selectionCleard: 'typo', selectionCleared: 'Auswahl aufgehoben', editRefused: null }
    }));

    assert.equal(warnings.length, 1);
    assert.match(warnings[0], /^BitResourceTimeline: /, 'the console prefix names the bit component');
    assert.match(warnings[0], /'selectionCleard'/);
    assert.ok(!('selectionCleard' in engine.config.texts));
    assert.equal(engine._text('selectionCleared'), 'Auswahl aufgehoben');
    assert.equal(engine._text('editRefused'), 'Edit refused');
});

test('an unknown option or color is warned about under the BitResourceTimeline prefix', () => {
    const engine = makeBareEngine();
    const warnings = captureWarnings(() => engine._applyOptions({ bartHeight: 1, colors: { barr: '#000' } }));
    assert.equal(warnings.length, 2);
    for (const warning of warnings) {
        assert.match(warning, /^BitResourceTimeline: ignoring unknown (option|color) '/);
    }
});

test('a text is a .NET-style composite format', () => {
    const engine = makeBareEngine();
    engine._applyOptions({
        texts: {
            copiedFormat: '{{{0}}} copied',
            timeRangeFormat: '{1} back to {0}'
        }
    });

    assert.equal(engine._text('copiedFormat', 3), '{3} copied', 'doubled braces are literal ones');
    assert.equal(engine._text('timeRangeFormat', 'a', 'b'), 'b back to a', 'placeholders may be reordered');
    assert.equal(engine._text('timeRangeFormat', null, 'b'), 'b back to ', 'a null argument is empty');
    assert.equal(engine._text('emptyRowFormat'), '{0}, no allocations', 'a missing argument leaves its placeholder');
    assert.equal(engine._text('noSuchText'), 'noSuchText', 'a key with no text at all reads as itself');
});

test('announcements take their wording from the texts', async () => {
    const engine = makeIndexedEngine([{ id: 'a', resourceId: 'r0', startTime: 100, endTime: 200 }]);
    engine.timeRange = { start: 0, end: 1_000_000 };
    engine._pixelsPerMs = 1;
    engine._relayout = () => { };
    engine.render = () => { };
    engine._scrollFocusIntoView = () => { };
    const announced = [];
    engine._announce = (m) => announced.push(m);
    engine._applyOptions({ texts: { editRefused: 'Abgelehnt' } });
    engine.dotNetRef = { invokeMethodAsync: async (name) => name !== 'OnAllocationChanging' };

    const alloc = engine.allocations[0];
    await engine._commitEdit({
        alloc,
        mode: 'move',
        origStart: 100,
        origEnd: 200,
        origResourceId: 'r0',
        origResourceIndex: 0,
        previewStart: 150,
        previewEnd: 250,
        previewResourceIndex: 0
    });

    assert.deepEqual(announced, ['Abgelehnt']);
    assert.equal(alloc.startTime, 100, 'the refused edit is rolled back');
});

// ---- setTheme ----

// A bare engine whose setTheme/setOptions refresh has nothing to paint: there is
// no renderer and no splitter element here.
function makeThemedEngine() {
    const engine = makeBareEngine();
    engine.render = () => { };
    engine._syncAxisSplitterChrome = () => { };
    return engine;
}

const THEME = {
    colors: {
        bar: '#222222',
        grid: '#333333',
        tooltipBg: '#101010',
        // Not a color the timeline has, and one the theme could not resolve.
        notATimelineColor: '#444444',
        axisBg: ''
    },
    fontFamily: 'Segoe UI'
};

test('a theme colors what the options leave alone, and an option color always wins', () => {
    const engine = makeThemedEngine();
    engine.setOptions({ colors: { bar: '#111111' } });

    engine.setTheme(THEME);

    const colors = engine.config.colors;
    assert.equal(colors.bar, '#111111', 'a color an option set is not the theme\'s to change');
    assert.equal(colors.grid, '#333333');
    assert.equal(colors.axisBg, '#f8f9fa', 'an unresolved theme color keeps the default');
    assert.equal(colors.contentBg, '#ffffff', 'a color the theme leaves out keeps the default');
    assert.ok(!('notATimelineColor' in colors), 'a theme does not add colors the timeline lacks');

    // An option set after the theme is just as explicit, through a later theme too.
    engine.setOptions({ colors: { grid: '#555555' } });
    assert.equal(colors.grid, '#555555');
    engine.setTheme({ ...THEME, colors: { ...THEME.colors, grid: '#666666' } });
    assert.equal(engine.config.colors.grid, '#555555');
});

test('dropping the theme puts the built-in colors back under the option colors', () => {
    const engine = makeThemedEngine();
    engine.setOptions({ colors: { bar: '#111111' } });
    engine.setTheme(THEME);

    engine.setTheme(null);

    assert.equal(engine.config.colors.grid, '#e9ecef');
    assert.equal(engine.config.colors.tooltipBg, '#212529');
    assert.equal(engine.config.colors.bar, '#111111');
});

test('a theme font family stands in only for the fonts no option has set', () => {
    const engine = makeThemedEngine();
    engine.setOptions({ barLabelFont: 'bold 14px Georgia' });

    engine.setTheme(THEME);

    const c = engine.config;
    assert.equal(c.barLabelFont, 'bold 14px Georgia', 'an explicit font is kept whole');
    assert.equal(c.dateLabelFont, '12px Segoe UI', 'the size of a default font stays');
    assert.equal(c.hourLabelFont, '12px Segoe UI');
    assert.equal(c.resourceLabelFont, '13px Segoe UI');
    assert.equal(c.resourceGroupFont, 'bold 13px Segoe UI', 'so does its weight');
    assert.equal(c.resourceChevronFont, '10px Segoe UI');

    // A font set while the theme is in force is explicit from then on.
    engine.setOptions({ dateLabelFont: '15px Courier' });
    engine.setTheme({ colors: {}, fontFamily: 'Roboto' });
    assert.equal(c.dateLabelFont, '15px Courier');
    assert.equal(c.hourLabelFont, '12px Roboto');

    // No family: the built-in fonts again, the explicit ones still untouched.
    engine.setTheme({ colors: {}, fontFamily: null });
    assert.equal(c.hourLabelFont, '12px sans-serif');
    assert.equal(c.resourceGroupFont, 'bold 13px sans-serif');
    assert.equal(c.barLabelFont, 'bold 14px Georgia');
    assert.equal(c.dateLabelFont, '15px Courier');
});

test('a theme invalidates the scene config snapshot and restyles a live tooltip', () => {
    const engine = makeThemedEngine();
    const styles = [];
    engine._tooltip = { setStyle: (s) => styles.push(s), hide() { } };
    const before = engine._configSnapshot();

    engine.setTheme(THEME);

    const after = engine._configSnapshot();
    assert.notEqual(after, before, 'the theme changed the config, so the snapshot must be rebuilt');
    assert.equal(after.colors.grid, '#333333');
    assert.deepEqual(styles.at(-1), { font: '11px Segoe UI', background: '#101010', color: '#ffffff' });
});

test('a theme reflows the stacked lanes when its font changes the label metrics', () => {
    const engine = makeIndexedEngine([
        { id: 'a', resourceId: 'r0', startTime: 0, endTime: 100, textAbove: 'A' },
        { id: 'b', resourceId: 'r0', startTime: 50, endTime: 150, textAbove: 'B' }
    ]);
    engine.render = () => { };
    engine._syncAxisSplitterChrome = () => { };
    const decorGen = engine._decorGen;
    const layoutGen = engine._barLayoutGen;

    engine.setTheme({ colors: {}, fontFamily: 'Segoe UI' });

    assert.equal(engine.config.barLabelFont, '11px Segoe UI');
    assert.ok(engine._decorGen > decorGen, 'the label measurements are retaken');
    assert.ok(engine._barLayoutGen > layoutGen, 'the cached stacking offsets are dropped');
});

test('setTheme on a disposed engine does nothing', () => {
    const engine = makeThemedEngine();
    engine._disposed = true;
    engine.setTheme(THEME);
    assert.equal(engine.config.colors.bar, '#74c0fc');
    assert.equal(engine.config.dateLabelFont, '12px sans-serif');
});
