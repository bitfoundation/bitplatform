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

test('a continuous drag previews whole milliseconds', () => {
    // EditSnapMinutes 0: the pointer delta divided by the scale is a fraction, and the preview is what .NET is asked
    // about (OnAllocationChanging, OnAllocationCreating), as a long.
    const engine = makeEditEngine([alloc('a', 'r0', 100, 200)]);
    engine.config.editSnapMinutes = 0;
    engine._pixelsPerMs = 0.37;
    const a = engine.allocations[0];
    for (const mode of ['move', 'resize-start', 'resize-end', 'create']) {
        engine.edit = { ...preview(engine, a, { start: 100, end: 200, mode }), grabX: 0, moved: false };
        engine._applyEditPreview({ x: 10, y: 0 }, 0);
        assert.ok(Number.isInteger(engine.edit.previewStart), `${mode}: start ${engine.edit.previewStart}`);
        assert.ok(Number.isInteger(engine.edit.previewEnd), `${mode}: end ${engine.edit.previewEnd}`);
    }
});

test('a held keyboard edit waits for the host before the next one starts', async () => {
    const engine = makeEditEngine([alloc('a', 'r0', 100, 200)]);
    const a = engine.allocations[0];
    engine._focusAlloc = a;
    engine._focusResource = 0;
    engine._announceEdit = () => {};
    const asked = [];
    const answers = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name, ...args) => {
            if (name !== 'OnAllocationChanging') return Promise.resolve();
            asked.push(args);
            return new Promise(resolve => answers.push(resolve));
        }
    };

    const first = engine._keyboardEdit('move-time', 1);
    // The key repeats before the host answered: it starts no edit from the unconfirmed preview.
    await engine._keyboardEdit('move-time', 1);
    assert.equal(asked.length, 1);

    answers[0](false);
    await first;
    assert.equal(a.startTime, 100, 'the refused edit is put back');
    assert.equal(a.endTime, 200);

    // The next press, once the host answered, starts from where the bar actually is.
    const third = engine._keyboardEdit('move-time', 1);
    assert.equal(asked.length, 2);
    assert.deepEqual(asked[1].slice(4, 7), ['r0', 100, 200]);
    answers[1](true);
    await third;
    assert.ok(a.startTime > 100);
});

test('a multi-bar move re-files every bar before one pass over the row metrics, and so does its revert', async () => {
    const bars = [];
    for (let i = 0; i < 20; i++) bars.push(alloc('b' + i, 'r' + (i % 3), i * 1000, i * 1000 + 500));
    const engine = makeEditEngine(bars);
    for (const b of engine.allocations) engine.selectedBars.add(b.id);
    const original = bars.map(b => [b.resourceId, b.startTime, b.endTime]);
    let metrics = 0;
    let reports = 0;
    let relayouts = 0;
    const recompute = engine._recomputeRowMetrics;
    engine._recomputeRowMetrics = function () { metrics++; return recompute.call(this); };
    engine._reportResourceRows = () => { reports++; };
    engine._relayout = () => { relayouts++; };
    engine.dotNetRef = {
        invokeMethodAsync: async (name) => name === 'OnAllocationsChanging' ? false : undefined
    };

    const a = engine.allocations.find(x => x.id === 'b0');
    const ed = preview(engine, a, { start: a.startTime + 100, end: a.endTime + 100, mode: 'move' });
    ed.companions = engine._companionEdits(a);
    assert.equal(ed.companions.length, 19);
    await engine._commitEdit(ed);

    assert.equal(metrics, 2, 'once for the move, once for its revert');
    assert.equal(reports, 2);
    assert.equal(relayouts, 2);
    assert.deepEqual(bars.map(b => [b.resourceId, b.startTime, b.endTime]), original, 'every bar is back');
    assert.deepEqual(indexViolations(engine), []);
});

// A host that answers `held` only when the test says so, and records every
// other call the engine makes to it.
function holdHost(engine, held) {
    const answers = [];
    const told = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name, ...args) => {
            if (name === held) return new Promise(resolve => answers.push(resolve));
            told.push(name);
            return Promise.resolve();
        }
    };
    return { answers, told };
}

// Replaces the data with fresh copies of the same bars, as a new Config of the
// app does: the same ids, other objects.
function replaceData(engine, bars) {
    engine._hideTooltip = () => {};
    engine.setData([{ id: 'r0', name: 'r0' }], 0, 1_000_000, bars.map(b => ({ ...b })));
}

function loaded(engine) {
    return engine.allocations.map(a => [a.id, a.resourceId, a.startTime, a.endTime]);
}

const BARS = [alloc('a', 'r0', 100, 200), alloc('b', 'r0', 400, 500)];
const LOADED = [['a', 'r0', 100, 200], ['b', 'r0', 400, 500]];

test('a pointer edit the host answers after the data was replaced is neither reverted nor reported', async () => {
    const engine = makeEditEngine(BARS.map(b => ({ ...b })));
    const host = holdHost(engine, 'OnAllocationChanging');

    const commit = engine._commitEdit(preview(engine, engine.allocations[0], { start: 150, end: 250 }));
    replaceData(engine, BARS);
    host.answers[0](false);
    await commit;

    // The refused bar is not put back into the new data, nor announced as refused.
    assert.deepEqual(loaded(engine), LOADED);
    assert.deepEqual(indexViolations(engine), []);
    assert.deepEqual(engine._announcements, []);
});

test('a keyboard edit the host accepts after the data was replaced is not reported, and the next one can start', async () => {
    const engine = makeEditEngine(BARS.map(b => ({ ...b })));
    engine._focusAlloc = engine.allocations[0];
    engine._focusResource = 0;
    engine._announceEdit = () => { engine._announcements.push('edited'); };
    const host = holdHost(engine, 'OnAllocationChanging');

    const edit = engine._keyboardEdit('move-time', 1);
    replaceData(engine, BARS);
    host.answers[0](true);
    await edit;

    assert.equal(host.told.includes('OnAllocationEdited'), false);
    assert.deepEqual(engine._announcements, []);
    assert.equal(engine._editPending, false);
    assert.deepEqual(loaded(engine), LOADED);
});

test('a pointer edit or a multi-bar move committed while another edit waits for the host is dropped', async () => {
    const engine = makeEditEngine(BARS.map(b => ({ ...b })));
    engine._focusAlloc = engine.allocations[0];
    engine._focusResource = 0;
    const asked = [];
    const answers = [];
    engine.dotNetRef = {
        invokeMethodAsync: (name) => {
            if (name !== 'OnAllocationChanging' && name !== 'OnAllocationsChanging') return Promise.resolve();
            asked.push(name);
            return new Promise(resolve => answers.push(resolve));
        }
    };

    const edit = engine._keyboardEdit('move-time', 1);
    assert.equal(asked.length, 1);

    const b = engine.allocations.find(x => x.id === 'b');
    await engine._commitEdit(preview(engine, b, { start: 600, end: 700 }));
    engine.selectedBars.add('a');
    engine.selectedBars.add('b');
    const multi = preview(engine, b, { start: 650, end: 750 });
    multi.companions = engine._companionEdits(b);
    await engine._commitEdit(multi);

    assert.equal(asked.length, 1, 'no other edit asks the host while an answer is out');
    assert.deepEqual([b.startTime, b.endTime], [400, 500]);

    answers[0](false);
    await edit;
    assert.deepEqual(loaded(engine), LOADED, 'the refused keyboard edit is put back');

    // With the answer in, the next pointer edit goes through.
    const next = engine._commitEdit(preview(engine, b, { start: 600, end: 700 }));
    assert.equal(asked.length, 2);
    answers[1](true);
    await next;
    assert.deepEqual([b.startTime, b.endTime], [600, 700]);
});

test('a multi-bar move the host answers after the data was replaced leaves the new data alone', async () => {
    const engine = makeEditEngine(BARS.map(b => ({ ...b })));
    engine.selectedBars.add('a');
    engine.selectedBars.add('b');
    const a = engine.allocations[0];
    const host = holdHost(engine, 'OnAllocationsChanging');

    const ed = preview(engine, a, { start: 200, end: 300, mode: 'move' });
    ed.companions = engine._companionEdits(a);
    const commit = engine._commitEdit(ed);
    replaceData(engine, BARS);
    host.answers[0](false);
    await commit;

    assert.deepEqual(loaded(engine), LOADED);
    assert.deepEqual(indexViolations(engine), []);
    assert.deepEqual(engine._announcements, []);
});

test('a bar created, a paste or a delete the host answers after the data was replaced changes nothing', async () => {
    const created = makeEditEngine(BARS.map(b => ({ ...b })));
    const createHost = holdHost(created, 'OnAllocationCreating');
    const create = created._commitCreate({ mode: 'create', previewStart: 600, previewEnd: 800, previewResourceIndex: 0 });
    replaceData(created, BARS);
    createHost.answers[0]({ id: 'new-1', resourceId: 'r0', startTime: 600, endTime: 800 });
    await create;
    assert.deepEqual(loaded(created), LOADED);
    assert.equal(createHost.told.includes('OnAllocationEdited'), false);

    const pasted = makeEditEngine(BARS.map(b => ({ ...b })));
    pasted._copyClipboard = ['a'];
    const pasteHost = holdHost(pasted, 'OnAllocationsCopying');
    const paste = pasted._pasteClipboard();
    replaceData(pasted, BARS);
    pasteHost.answers[0]([{ id: 'a2', resourceId: 'r0', startTime: 600, endTime: 700 }]);
    await paste;
    assert.deepEqual(loaded(pasted), LOADED);

    const deleted = makeEditEngine(BARS.map(b => ({ ...b })));
    deleted.selectedBars.add('a');
    const deleteHost = holdHost(deleted, 'OnAllocationsDeleting');
    const remove = deleted._deleteSelection();
    replaceData(deleted, BARS);
    deleteHost.answers[0](true);
    await remove;
    // The new data's own a is not deleted on the strength of an answer about the old one.
    assert.deepEqual(loaded(deleted), LOADED);
    assert.deepEqual(deleted._announcements, []);
});
