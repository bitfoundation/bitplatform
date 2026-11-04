// Edit commit gate: host cancel, overlap, locked bars.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeIndexedEngine, indexViolations } from './helpers/engine-fixture.mjs';

function alloc(id, resourceId, startTime, endTime, extra = {}) {
    return { id, resourceId, startTime, endTime, ...extra };
}

function makeEditEngine(allocations, overrides = {}) {
    const engine = makeIndexedEngine(allocations, overrides);
    engine._rowIndexById = new Map(engine._rows.map((r, i) => [r.resource.id, i]));
    engine.timeRange = { start: 0, end: 1_000_000 };
    engine._pixelsPerMs = 1;
    engine._relayout = () => {};
    engine.render = () => {};
    engine._announcements = [];
    engine._announce = (m) => { engine._announcements.push(m); };
    engine._scrollFocusIntoView = () => {};
    return engine;
}

function preview(engine, alloc, { start, end, resourceIndex = 0, mode = 'move' }) {
    return {
        alloc,
        mode,
        origStart: alloc.startTime,
        origEnd: alloc.endTime,
        origResourceId: alloc.resourceId,
        origResourceIndex: engine._rowIndexById.get(alloc.resourceId) ?? 0,
        previewStart: start,
        previewEnd: end,
        previewResourceIndex: resourceIndex
    };
}

test('_editZone returns null for a locked bar', () => {
    const engine = makeEditEngine([alloc('a', 'r0', 100, 400)], {
        config: { editResizeHandlePx: 6 }
    });
    const bar = engine.allocations[0];
    const x = engine.getTimeToX(250);

    assert.equal(engine._editZone(bar, x), 'move');
    bar.locked = true;
    assert.equal(engine._editZone(bar, x), null);
});

test('_overlapsUnlocked: touching ends are allowed, interior overlap is not', () => {
    const engine = makeEditEngine([
        alloc('a', 'r0', 0, 100),
        alloc('b', 'r0', 200, 300),
        alloc('d', 'r1', 50, 150)
    ]);

    const cases = [
        { start: 100, end: 200, except: 'x', resource: 'r0', want: false, note: 'gap between a and b, touching both' },
        { start: 0, end: 100, except: 'a', resource: 'r0', want: false, note: 'self excluded' },
        { start: 0, end: 100, except: 'x', resource: 'r0', want: true, note: 'same span as a' },
        { start: 50, end: 150, except: 'x', resource: 'r0', want: true, note: 'interior of a' },
        { start: 50, end: 150, except: 'x', resource: 'r1', want: true, note: 'same as d' },
        { start: 50, end: 150, except: 'd', resource: 'r1', want: false, note: 'self on r1' }
    ];

    for (const c of cases) {
        assert.equal(
            engine._overlapsUnlocked(c.resource, c.start, c.end, c.except),
            c.want,
            c.note);
    }
});

test('_overlapsUnlocked ignores locked neighbours', () => {
    const engine = makeEditEngine([
        alloc('lock', 'r0', 0, 100, { locked: true }),
        alloc('free', 'r0', 200, 300)
    ]);

    assert.equal(engine._overlapsUnlocked('r0', 40, 80, 'mover'), false);
    assert.equal(engine._overlapsUnlocked('r0', 220, 250, 'mover'), true);
});

test('_commitEdit restores times when the host returns false', async () => {
    const engine = makeEditEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 400, 500)
    ]);
    const edited = [];
    engine.dotNetRef = {
        invokeMethodAsync: async (name, ...args) => {
            if (name === 'OnAllocationChanging') return false;
            if (name === 'OnAllocationEdited') edited.push(args);
            return undefined;
        }
    };

    const target = engine.allocations.find(a => a.id === 'a');
    await engine._commitEdit(preview(engine, target, { start: 150, end: 250 }));

    assert.equal(target.startTime, 100);
    assert.equal(target.endTime, 200);
    assert.equal(target.resourceId, 'r0');
    assert.deepEqual(indexViolations(engine), []);
    assert.deepEqual(edited, []);
    assert.ok(engine._announcements.some(m => /refused/i.test(m)));
});

test('_commitEdit notifies OnAllocationEdited only after a successful change', async () => {
    const engine = makeEditEngine([alloc('a', 'r0', 100, 200)]);
    const edited = [];
    engine.dotNetRef = {
        invokeMethodAsync: async (name, ...args) => {
            if (name === 'OnAllocationChanging') return true;
            if (name === 'OnAllocationEdited') edited.push(args[0]);
            return undefined;
        }
    };

    const target = engine.allocations[0];
    await engine._commitEdit(preview(engine, target, { start: 150, end: 250 }));

    assert.equal(target.startTime, 150);
    assert.equal(target.endTime, 250);
    assert.deepEqual(edited, ['a']);
    assert.deepEqual(indexViolations(engine), []);
});

test('_commitEdit refuses an overlapping drop when allowOverlap is false', async () => {
    const engine = makeEditEngine(
        [alloc('a', 'r0', 100, 200), alloc('b', 'r0', 300, 400)],
        { config: { allowOverlap: false } }
    );
    let changingCalls = 0;
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => {
            if (name === 'OnAllocationChanging') changingCalls++;
            return true;
        }
    };

    const target = engine.allocations.find(a => a.id === 'a');
    await engine._commitEdit(preview(engine, target, { start: 250, end: 350 }));

    assert.equal(target.startTime, 100);
    assert.equal(target.endTime, 200);
    assert.equal(changingCalls, 0);
    assert.deepEqual(indexViolations(engine), []);
    assert.ok(engine._announcements.some(m => /overlap/i.test(m)));
});

test('_commitEdit allows a touching drop when allowOverlap is false', async () => {
    const engine = makeEditEngine(
        [alloc('a', 'r0', 100, 200), alloc('b', 'r0', 300, 400)],
        { config: { allowOverlap: false } }
    );
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => name === 'OnAllocationChanging' ? true : undefined
    };

    const target = engine.allocations.find(a => a.id === 'a');
    await engine._commitEdit(preview(engine, target, { start: 200, end: 300 }));

    assert.equal(target.startTime, 200);
    assert.equal(target.endTime, 300);
    assert.deepEqual(indexViolations(engine), []);
});

const MINUTE = 60_000;

test('create preview respects snap and min duration', () => {
    const engine = makeEditEngine([alloc('a', 'r0', 0, 100)], {
        config: {
            editable: true,
            emptyDragAction: 'create',
            editSnapMinutes: 15,
            editMinDurationMinutes: 5
        }
    });
    engine._pixelsPerMs = 1 / MINUTE;
    engine.timeRange = { start: 0, end: 24 * 60 * MINUTE };
    engine.edit = {
        mode: 'create',
        alloc: { resourceId: 'r0', startTime: 0, endTime: 5 * MINUTE },
        origStart: 0,
        origEnd: 5 * MINUTE,
        origResourceIndex: 0,
        grabX: 0,
        previewStart: 0,
        previewEnd: 5 * MINUTE,
        previewResourceIndex: 0,
        moved: false
    };

    engine._applyEditPreview({ x: 45 }, engine.config.timeAxisHeight + 10);

    assert.equal(engine.edit.previewStart, 0);
    assert.equal(engine.edit.previewEnd, 45 * MINUTE);
    assert.equal(engine.edit.moved, true);

    engine.edit.previewStart = 0;
    engine.edit.previewEnd = 5 * MINUTE;
    engine.edit.moved = false;
    engine._applyEditPreview({ x: 1 }, engine.config.timeAxisHeight + 10);
    assert.ok(engine.edit.previewEnd - engine.edit.previewStart >= 5 * MINUTE);
});

test('additive modifier still takes the marquee path when EmptyDragAction is Create', () => {
    const engine = makeEditEngine([alloc('a', 'r0', 100, 200)], {
        config: { editable: true, emptyDragAction: 'create', editMinDurationMinutes: 5 }
    });
    engine.wrapper.focus = () => {};
    engine.renderer = {
        surface: {
            setPointerCapture() {},
            getBoundingClientRect: () => ({ left: 0, top: 0 })
        }
    };
    engine._surfaceRect = { left: 0, top: 0 };
    engine._hideTooltip = () => {};

    engine.handlePointerDown({
        pointerType: 'mouse',
        button: 0,
        pointerId: 1,
        clientX: 500,
        clientY: 80,
        ctrlKey: true,
        metaKey: false,
        preventDefault() {}
    });

    assert.ok(!engine.edit);
    assert.ok(engine.drag);
    assert.equal(engine.drag.additive, true);
});

test('_commitCreate upserts a host-returned bar and skips upsert on null', async () => {
    const engine = makeEditEngine([alloc('a', 'r0', 100, 200)]);
    const upserted = [];
    engine.upsertAllocations = (batch) => { upserted.push(...batch); };
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => {
            if (name === 'OnAllocationCreating') return null;
            return undefined;
        }
    };

    await engine._commitCreate({
        mode: 'create',
        previewStart: 500,
        previewEnd: 800,
        previewResourceIndex: 0
    });
    assert.deepEqual(upserted, []);

    engine.dotNetRef.invokeMethodAsync = async (name) => {
        if (name === 'OnAllocationCreating') {
            return { id: 'new-1', resourceId: 'r0', startTime: 500, endTime: 800 };
        }
        return undefined;
    };
    await engine._commitCreate({
        mode: 'create',
        previewStart: 500,
        previewEnd: 800,
        previewResourceIndex: 0
    });
    assert.equal(upserted.length, 1);
    assert.equal(upserted[0].id, 'new-1');
});

test('delete removes ids and selection', async () => {
    const engine = makeEditEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 300, 400)
    ]);
    engine.config.allowDelete = true;
    engine.selectedBars.add('a');
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => name === 'OnAllocationsDeleting' ? true : undefined
    };

    await engine._deleteSelection();

    assert.equal(engine.allocations.some(x => x.id === 'a'), false);
    assert.equal(engine.selectedBars.has('a'), false);
    assert.ok(engine.allocations.some(x => x.id === 'b'));
});

test('multi-move keeps relative gaps; reject restores all', async () => {
    const engine = makeEditEngine([
        alloc('a', 'r0', 100, 200),
        alloc('b', 'r0', 400, 500)
    ]);
    engine.selectedBars.add('a');
    engine.selectedBars.add('b');
    const a = engine.allocations.find(x => x.id === 'a');
    const b = engine.allocations.find(x => x.id === 'b');
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => name === 'OnAllocationsChanging' ? false : true
    };

    const ed = preview(engine, a, { start: 200, end: 300, mode: 'move' });
    ed.companions = engine._companionEdits(a);
    await engine._commitEdit(ed);

    assert.equal(a.startTime, 100);
    assert.equal(b.startTime, 400);

    engine.dotNetRef.invokeMethodAsync = async () => true;
    const ed2 = preview(engine, a, { start: 200, end: 300, mode: 'move' });
    ed2.companions = engine._companionEdits(a);
    await engine._commitEdit(ed2);

    assert.equal(a.startTime, 200);
    assert.equal(b.startTime, 500);
    assert.equal(b.startTime - a.startTime, 300);
});
