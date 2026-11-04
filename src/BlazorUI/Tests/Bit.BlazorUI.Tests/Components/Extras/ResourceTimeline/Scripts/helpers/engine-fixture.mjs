// Test fixture for the BitResourceTimeline engine.
//
// The engine's constructor needs a live DOM (wrapper element, ResizeObserver,
// a 2D context for text measurement, a renderer). Most of what is worth testing
// - the zone/date math, the allocation index, lane assignment, the virtual
// scroll mapping - touches none of that, so these helpers build an object on
// the engine's prototype carrying only the state those methods read.
//
// Anything that genuinely needs a DOM belongs in a browser-driven test instead.
//
// The engine is the compiled output of the Extras package's TypeScript
// (Bit.BlazorUI.Extras/wwwroot/scripts/modules/BitResourceTimeline*.js). Those
// are lazy ES modules written for a browser: each adds its class to the global
// BitBlazorUI namespace it finds on `window`, rather than exporting it. So the
// browser globals they read at load are put in place first, the modules are
// imported dynamically after that, and the classes are read off the namespace.
//
// The modules are taken from the wwwroot folder beside this test project, or
// from the folder the BIT_RTM_MODULES environment variable names when it is set.

import path from 'node:path';
import { pathToFileURL } from 'node:url';

const MODULE_DIR = process.env.BIT_RTM_MODULES
    ? pathToFileURL(path.resolve(process.env.BIT_RTM_MODULES) + path.sep)
    : new URL('../../../../../../../Bit.BlazorUI.Extras/wwwroot/scripts/modules/', import.meta.url);

// The modules sit under the Extras project's package.json, which declares no
// "type", so Node finds their module syntax by itself - and says so on stderr,
// once per test file. That is expected here and says nothing about the code
// under test, so this one warning is dropped; every other warning still prints.
const warningListeners = process.listeners('warning');
process.removeAllListeners('warning');
process.on('warning', (warning) => {
    if (warning && warning.code === 'MODULE_TYPELESS_PACKAGE_JSON') return;
    for (const listener of warningListeners) listener.call(process, warning);
});

// The modules open with `window.BitBlazorUI || (window.BitBlazorUI = {})`.
if (typeof globalThis.window === 'undefined') globalThis.window = globalThis;
const BitBlazorUI = globalThis.BitBlazorUI || (globalThis.BitBlazorUI = {});

// handleKeyDown asks the core library whether a key belongs to an IME
// composition before anything else. That function lives in Bit.BlazorUI's own
// scripts, which are not loaded here; this stands in for it, with its logic.
BitBlazorUI.Utils = BitBlazorUI.Utils || {};
if (typeof BitBlazorUI.Utils.isComposingKey !== 'function') {
    BitBlazorUI.Utils.isComposingKey = (e) => !!(e.isComposing || e.keyCode === 229);
}

// The engine module imports the zoned-time and tooltip modules itself.
await import(new URL('BitResourceTimelineEngine.js', MODULE_DIR).href);

for (const name of ['ResourceTimelineEngine', 'ResourceTimelineZonedTime', 'ResourceTimelineTooltip']) {
    if (typeof BitBlazorUI[name] !== 'function') {
        throw new Error(`BitBlazorUI.${name} was not defined by the modules in ${MODULE_DIR}`);
    }
}

export const ResourceTimelineEngine = BitBlazorUI.ResourceTimelineEngine;
export const ResourceTimelineZonedTime = BitBlazorUI.ResourceTimelineZonedTime;
export const ResourceTimelineTooltip = BitBlazorUI.ResourceTimelineTooltip;

// The names the tests were written against.
export const ZonedTime = ResourceTimelineZonedTime;
export const Tooltip = ResourceTimelineTooltip;
export function utcHourBoundaries(visStart, visEnd, step) {
    return ResourceTimelineZonedTime.utcHourBoundaries(visStart, visEnd, step);
}

// Mirrors the defaults in the engine's constructor for the fields the
// DOM-independent methods touch. Kept explicit rather than reaching into a real
// instance so a test failure points at the behaviour, not at fixture drift.
//
// config.texts is deliberately left out: the engine's built-in texts are not
// exported, and _text falls back to them for a config without texts (which is
// what the English assertions exercise); _applyOptions creates it from those
// defaults the first time an options object sets a text.
export function makeBareEngine(overrides = {}) {
    const engine = Object.create(ResourceTimelineEngine.prototype);

    engine.config = {
        resourceHeight: 40,
        timeAxisHeight: 60,
        resourceAxisWidth: 150,
        resourceIndent: 16,
        dateRowHeight: 22,
        barHeight: 4,
        barMargin: 2,
        stackLabelClearance: true,
        stackOnLabelCollision: true,
        minBarWidth: 2,
        minBarWidthForLabels: 24,
        barLabelGap: 3,
        barIconSize: 16,
        hitTolerance: 3,
        dragThreshold: 4,
        timeZone: null,
        locale: null,
        showUtcTime: false,
        pixelsPerHour: null,
        minPixelsPerHour: 0.25,
        maxPixelsPerHour: 1200,
        autoScrollToNow: false,
        preserveScrollOnReload: false,
        nowLineRefreshMs: 60000,
        panToDayStart: false,
        barLabelFont: '11px sans-serif',
        dateLabelFont: '12px sans-serif',
        hourLabelFont: '12px sans-serif',
        resourceLabelFont: '13px sans-serif',
        resourceGroupFont: 'bold 13px sans-serif',
        resourceChevronFont: '10px sans-serif',
        resourceChevronGap: 14,
        resourceAxisResizable: true,
        resourceAxisMinWidth: 80,
        resourceAxisMaxWidth: 0,
        snapToTimeZone: true,
        hour12: false,
        firstDayOfWeek: null,
        nonWorkingDays: [],
        workingHoursStart: null,
        workingHoursEnd: null,
        maxStackLanes: 0,
        allowDelete: false,
        showTooltips: true,
        tooltipDelayMs: 300,
        tooltipTemplate: false,
        disabled: false,
        ...(overrides.config || {}),
        // Always present on the real config, and _applyOptions merges into it.
        colors: { bar: '#74c0fc', barSelected: '#4dabf7', label: '#495057', nonWorking: 'rgba(0,0,0,0.06)' }
    };

    // Theme layering (setTheme): no theme, and nothing set explicitly yet.
    engine._theme = null;
    engine._explicitColors = {};
    engine._explicitFonts = new Set();

    engine.allocations = [];
    engine.allocationsByResource = new Map();
    engine.resources = [];
    engine._rows = [];
    engine._childrenById = new Map();
    engine._resourceRoots = [];
    engine._collapsed = new Set();
    engine._rowIndexById = new Map();
    engine.selectedBars = new Set();
    engine.timeRange = { start: null, end: null };

    engine._laneInfo = new WeakMap();
    engine._barLayoutGen = 1;
    engine._configGen = 1;

    // Lane assignment measures bar labels, which the real engine does through a
    // 2D context created in its constructor. A fixed width per character keeps
    // the geometry a test asserts on predictable.
    engine._measureCtx = { font: '', measureText: (t) => ({ width: t.length * 7 }) };
    engine._decorGen = 1;
    engine._decorBoxes = new WeakMap();
    engine._labelMetricsCache = null;
    engine._labelWidths = null;
    engine._labelWidthsGen = 0;
    engine._spanScratch = { lead: 0, trail: 0 };
    engine._laneScale = null;
    engine._configSnap = null;
    engine._configSnapGen = -1;
    engine._rowHeights = [];
    engine._rowTops = [0];

    engine.scrollX = 0;
    engine.scrollY = 0;
    engine._viewportW = 1200;
    engine._viewportH = 600;
    engine._visibleWidth = 1050;
    engine._pixelsPerMs = 0;
    engine._pixelsPerHour = 0;
    engine._userPixelsPerHour = null;
    engine._virtualWidth = 0;
    engine._virtualScrollMaxX = 0;
    engine._scrollScaleX = 1;
    engine._firstLoadDone = false;
    engine._pendingScroll = null;
    engine._streamScroll = null;

    // Scroll positions are written through to the wrapper element; the
    // DOM-independent paths only ever set scrollLeft/scrollTop on it, so a
    // plain object records them just as well. Having no setAttribute, it also
    // turns the data-bit-esc claim into a no-op (see _syncEscapeClaim); a test
    // of the claim gives it one.
    engine.wrapper = { scrollLeft: 0, scrollTop: 0 };
    engine._escClaimed = false;

    // Reporting rows back to .NET is interop; there is no dotNetRef here.
    engine.dotNetRef = null;

    engine._overflowHits = [];
    engine._lastView = null;
    engine._warnedAllocIds = new Set();
    engine._resourceIdSet = new Set();
    engine._selectionAnchorId = null;
    engine._copyClipboard = [];
    engine._lastHoverId = undefined;

    Object.assign(engine, overrides.engine || {});
    return engine;
}

// A bare engine with its ResourceTimelineZonedTime built, for the axis helpers.
export function makeZonedEngine(timeZone, locale = null) {
    const engine = makeBareEngine({ config: { timeZone, locale } });
    engine._rebuildDateFormatters();
    return engine;
}

// A bare engine holding the given allocations, indexed as the real one would.
// When resources are not supplied, one leaf row is synthesized per resourceId
// seen in the allocations so variable row-height metrics have something to size.
export function makeIndexedEngine(allocations, overrides = {}) {
    const engine = makeBareEngine(overrides);
    engine.allocations = allocations.slice().sort((a, b) => a.startTime - b.startTime);
    if (!engine._rows.length) {
        const seen = new Set();
        for (const a of engine.allocations) {
            if (!seen.has(a.resourceId)) {
                seen.add(a.resourceId);
                engine._rows.push({
                    resource: { id: a.resourceId, name: a.resourceId },
                    depth: 0,
                    hasChildren: false
                });
            }
        }
    }
    engine._rowIndexById = new Map(engine._rows.map((r, i) => [r.resource.id, i]));
    engine._indexAllocations();
    engine._resourceIdSet = new Set(engine._rows.map(r => r.resource.id));
    return engine;
}

// Deterministic PRNG so a failing case can be reproduced from its seed.
export function rng(seed) {
    let s = seed >>> 0;
    return () => {
        s = (s * 1664525 + 1013904223) >>> 0;
        return s / 4294967296;
    };
}

// Random allocations across `resources`, sorted by start time. Some carry edge
// bars and explicit heights, which feed the row bounds and lane heights.
export function genAllocations(rand, count, resources) {
    const out = [];
    for (let i = 0; i < count; i++) {
        const start = Math.floor(rand() * 1000000);
        const a = {
            id: 'a' + i,
            resourceId: resources[Math.floor(rand() * resources.length)],
            startTime: start,
            endTime: start + 1 + Math.floor(rand() * 50000)
        };
        if (rand() < 0.25) a.startBar = { duration: Math.floor(rand() * 20000) };
        if (rand() < 0.25) a.endBar = { duration: Math.floor(rand() * 20000) };
        if (rand() < 0.30) a.height = 2 + Math.floor(rand() * 20);
        out.push(a);
    }
    return out.sort((x, y) => x.startTime - y.startTime);
}

// Invariants the allocation index must satisfy at all times. Returns the list
// of violations (empty when healthy). These are absolute checks, not a
// comparison against another implementation, so they still catch a bug that
// happens to be present in both an incremental and a full rebuild.
export function indexViolations(engine) {
    const errs = [];
    let indexed = 0;

    for (const [resourceId, row] of engine.allocationsByResource) {
        indexed += row.items.length;
        let trueSpan = 0;
        let trueStartEdge = 0;

        for (let i = 0; i < row.items.length; i++) {
            const a = row.items[i];
            if (i > 0 && row.items[i - 1].startTime > a.startTime) {
                errs.push(`row ${resourceId}: items not sorted at index ${i}`);
            }
            if (a.resourceId !== resourceId) {
                errs.push(`row ${resourceId}: holds allocation of ${a.resourceId}`);
            }
            const se = a.startBar && a.startBar.duration > 0 ? a.startBar.duration : 0;
            const ee = a.endBar && a.endBar.duration > 0 ? a.endBar.duration : 0;
            trueSpan = Math.max(trueSpan, (a.endTime - a.startTime) + se + ee);
            trueStartEdge = Math.max(trueStartEdge, se);
        }

        // The bounds may be wider than necessary (they only widen on edit) but
        // must never be narrower, or scans would skip bars that are on screen.
        if (row.maxSpanMs < trueSpan) {
            errs.push(`row ${resourceId}: maxSpanMs ${row.maxSpanMs} < actual ${trueSpan}`);
        }
        if (row.maxStartEdgeMs < trueStartEdge) {
            errs.push(`row ${resourceId}: maxStartEdgeMs ${row.maxStartEdgeMs} < actual ${trueStartEdge}`);
        }
    }

    for (let i = 1; i < engine.allocations.length; i++) {
        if (engine.allocations[i - 1].startTime > engine.allocations[i].startTime) {
            errs.push(`global allocation list not sorted at index ${i}`);
            break;
        }
    }
    if (indexed !== engine.allocations.length) {
        errs.push(`index holds ${indexed} allocations, global list has ${engine.allocations.length}`);
    }
    const ids = new Set(engine.allocations.map(a => a.id));
    if (ids.size !== engine.allocations.length) {
        errs.push('duplicate allocation ids in global list');
    }
    return errs;
}

// Comparable view of the index: order, lanes and resolved stacking offsets.
// Cluster objects differ by reference between engines, so they are compared
// structurally.
export function indexSnapshot(engine) {
    return [...engine.allocationsByResource]
        .sort((a, b) => (a[0] < b[0] ? -1 : 1))
        .map(([resourceId, row]) => ({
            resourceId,
            order: row.items.map(a => a.id),
            lanes: row.items.map(a => engine._laneInfo.get(a).lane),
            laneHeights: row.items.map(a => engine._laneInfo.get(a).cluster.laneHeights.join(',')),
            offsets: row.items.map(a => engine._stackOffset(a))
        }));
}
