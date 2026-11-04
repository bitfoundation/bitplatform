// Zone-aware time axis: hour-boundary generation, DST handling and tick
// density. These run against Intl, so they depend only on the ICU data Node
// ships with.

import test from 'node:test';
import assert from 'node:assert/strict';

import { makeBareEngine, makeZonedEngine, ZonedTime, utcHourBoundaries } from './helpers/engine-fixture.mjs';

// Oracle: scan the window minute by minute and keep every instant whose local
// wall clock reads mm:ss = 00:00 on an hour that is a multiple of `step`. Zone
// offsets are always whole minutes, so this cannot miss a boundary. It is
// deliberately naive - its job is to be obviously correct, not fast.
function referenceBoundaries(engine, visStart, visEnd, step) {
    const out = [];
    for (let ts = Math.ceil(visStart / 60000) * 60000; ts <= visEnd; ts += 60000) {
        const p = engine.parts(ts);
        if (p.minute === 0 && p.second === 0 && p.hour % step === 0) {
            out.push({ ts, hour: p.hour });
        }
    }
    return out;
}

const ZONES = ['UTC', 'Europe/Berlin', 'America/New_York', 'Asia/Tokyo', 'Asia/Kolkata'];
const STEPS = [1, 2, 3, 4, 6, 8, 12, 24];

test('hour boundaries match a brute-force scan across zones and steps', () => {
    // An ordinary window with no transition in it.
    const start = Date.parse('2026-04-01T00:00:00Z');
    const end = Date.parse('2026-04-03T00:00:00Z');

    for (const zone of ZONES) {
        const engine = new ZonedTime(zone);
        for (const step of STEPS) {
            assert.deepEqual(
                engine.hourBoundaries(start, end, step),
                referenceBoundaries(engine, start, end, step),
                `zone ${zone}, step ${step}`
            );
        }
    }
});

test('hour boundaries handle an unaligned window start', () => {
    const start = Date.parse('2026-01-15T06:37:11Z');
    const end = Date.parse('2026-01-16T19:04:53Z');
    for (const zone of ZONES) {
        const engine = new ZonedTime(zone);
        for (const step of STEPS) {
            const got = engine.hourBoundaries(start, end, step);
            assert.deepEqual(got, referenceBoundaries(engine, start, end, step),
                `zone ${zone}, step ${step}`);
            for (const b of got) {
                assert.ok(b.ts >= start && b.ts <= end, 'boundary outside the requested window');
            }
        }
    }
});

test('a spring-forward transition does not emit the skipped wall-clock hour', () => {
    // 2026-03-08: New York jumps 02:00 -> 03:00, so local 02:00 never happens.
    const engine = new ZonedTime('America/New_York');
    const start = Date.parse('2026-03-08T04:00:00Z');
    const end = Date.parse('2026-03-08T10:00:00Z');
    const got = engine.hourBoundaries(start, end, 1);

    assert.deepEqual(got, referenceBoundaries(engine, start, end, 1));
    assert.ok(!got.some(b => b.hour === 2), 'local 02:00 does not exist on this date');
    // Every emitted boundary really is on the hour it claims.
    for (const b of got) {
        assert.equal(engine.parts(b.ts).hour, b.hour);
        assert.equal(engine.parts(b.ts).minute, 0);
    }
});

test('a spring-forward transition terminates (regression: non-advancing walk)', () => {
    // The previous implementation stopped advancing across this transition and
    // looped until an iteration cap bailed it out, truncating the axis.
    for (const zone of ['America/New_York', 'Europe/Berlin', 'America/St_Johns']) {
        const engine = new ZonedTime(zone);
        const start = Date.parse('2026-03-01T00:00:00Z');
        const end = Date.parse('2026-04-05T00:00:00Z');
        const got = engine.hourBoundaries(start, end, 1);
        // ~35 days of hourly boundaries, minus the one skipped hour.
        assert.ok(got.length > 800, `${zone}: only ${got.length} boundaries - walk stalled?`);
        for (let i = 1; i < got.length; i++) {
            assert.ok(got[i].ts > got[i - 1].ts, `${zone}: boundaries not strictly increasing`);
        }
    }
});

test('a fall-back transition keeps boundaries strictly increasing', () => {
    const engine = new ZonedTime('America/New_York');
    const start = Date.parse('2026-11-01T00:00:00Z');
    const end = Date.parse('2026-11-02T12:00:00Z');
    const got = engine.hourBoundaries(start, end, 1);

    for (let i = 1; i < got.length; i++) {
        assert.ok(got[i].ts > got[i - 1].ts, 'boundaries must advance in real time');
        assert.equal(engine.parts(got[i].ts).minute, 0);
    }
    // The repeated wall-clock hour is emitted once, leaving a two-hour real gap.
    const gaps = got.slice(1).map((b, i) => b.ts - got[i].ts);
    assert.equal(gaps.filter(g => g === 2 * 3600000).length, 1);
});

test('boundaries are bounded by the viewport, not the window length', () => {
    // _hourStep keeps ticks ~44px apart, so a very wide time window at a
    // readable zoom still yields only a screenful of boundaries.
    const time = new ZonedTime('UTC');
    const start = Date.parse('2026-01-01T00:00:00Z');
    const end = start + 400 * 24 * 3600000;   // 400 days

    // The step comes from the engine (it is a zoom concern), the boundaries
    // from ZonedTime.
    const engine = makeBareEngine();
    engine._pixelsPerHour = 2;                 // step will be 24
    const step = engine._hourStep();
    assert.equal(step, 24);
    const got = time.hourBoundaries(start, end, step);
    // Both ends of the window land exactly on midnight and the range is
    // inclusive, so 400 days spans 401 boundaries.
    assert.equal(got.length, 401);
    for (const b of got) assert.equal(b.hour, 0);
});

test('_hourStep picks tidy divisors of a day and bails out when unreadable', () => {
    const engine = makeBareEngine();
    const stepAt = (pph) => { engine._pixelsPerHour = pph; return engine._hourStep(); };

    assert.equal(stepAt(0.5), 0, 'a day narrower than 48px shows no hour ticks');
    for (const pph of [2, 3, 5, 8, 12, 20, 44, 100, 400]) {
        const step = stepAt(pph);
        assert.ok([1, 2, 3, 4, 6, 8, 12, 24].includes(step), `step ${step} is not a divisor of 24`);
        assert.equal(24 % step, 0);
    }
    // Denser zoom never needs a coarser step.
    let prev = 24;
    for (const pph of [2, 4, 8, 16, 32, 64]) {
        const step = stepAt(pph);
        assert.ok(step <= prev, 'step must not grow as the zoom increases');
        prev = step;
    }
});

test('zoned parts and the wall-clock round trip agree across a DST year', () => {
    for (const zone of ZONES) {
        const engine = new ZonedTime(zone);
        for (let day = 0; day < 365; day += 7) {
            const ts = Date.parse('2026-01-01T12:34:00Z') + day * 86400000;
            const p = engine.parts(ts);
            const round = engine.wallClockToTs(p.year, p.month, p.day, p.hour, p.minute, p.second);
            assert.equal(round, Math.floor(ts / 1000) * 1000,
                `${zone}: wall-clock round trip lost the instant at day ${day}`);
        }
    }

    // 2026-10-25: Berlin falls back 03:00 CEST -> 02:00 CET, so 02:30 happens twice, at 00:30Z (+02:00) and 01:30Z
    // (+01:00). It is the first of them, the one hourBoundaries emits for the repeated hour.
    const berlin = new ZonedTime('Europe/Berlin');
    const first = Date.parse('2026-10-25T00:30:00Z');
    assert.equal(berlin.wallClockToTs(2026, 10, 25, 2, 30, 0), first);
    const two = berlin.hourBoundaries(Date.parse('2026-10-24T23:30:00Z'), Date.parse('2026-10-25T02:30:00Z'), 1)
        .find(b => b.hour === 2);
    assert.equal(berlin.wallClockToTs(2026, 10, 25, 2, 0, 0), two.ts);
});

test('wall-clock snap in a repeated hour stays in the occurrence it was in', () => {
    const engine = makeZonedEngine('Europe/Berlin');
    engine.config.editSnapMinutes = 15;
    engine.config.snapToTimeZone = true;

    // 02:40 of each occurrence of Berlin's repeated hour snaps to 02:45 of the same one.
    assert.equal(engine._snapTime(Date.parse('2026-10-25T00:40:00Z')), Date.parse('2026-10-25T00:45:00Z'));
    assert.equal(engine._snapTime(Date.parse('2026-10-25T01:40:00Z')), Date.parse('2026-10-25T01:45:00Z'));
});

test('the UTC row\'s boundaries match the zoned walk through UTC', () => {
    // The UTC row skips Intl entirely (UTC has no offset and no DST), so its
    // arithmetic is checked against the general zoned implementation - and
    // through it against the brute-force scan the tests above pin down.
    const utc = new ZonedTime('UTC');
    const windows = [
        ['2026-04-01T00:00:00Z', '2026-04-03T00:00:00Z'],   // aligned both ends
        ['2026-01-15T06:37:11Z', '2026-01-16T19:04:53Z'],   // unaligned
        ['1969-12-30T05:00:00Z', '1970-01-02T05:00:00Z']    // spanning the epoch
    ];

    for (const [from, to] of windows) {
        const start = Date.parse(from);
        const end = Date.parse(to);
        for (const step of STEPS) {
            assert.deepEqual(
                utcHourBoundaries(start, end, step),
                utc.hourBoundaries(start, end, step),
                `window ${from}..${to}, step ${step}`);
        }
        assert.deepEqual(utcHourBoundaries(start, end, 0), [], 'step 0 shows no ticks');
    }
});

test('the two axis rows line up only where the zone offset is a whole hour', () => {
    // This offset is the whole point of the UTC row: same density, boundaries
    // that coincide under a whole-hour offset and sit shifted where the offset
    // carries minutes.
    const start = Date.parse('2026-06-15T00:00:00Z');
    const end = start + 86400000;
    const tsOf = (list) => list.map(b => b.ts);

    const berlin = new ZonedTime('Europe/Berlin').hourBoundaries(start, end, 2);
    assert.deepEqual(tsOf(utcHourBoundaries(start, end, 2)), tsOf(berlin),
        '+02:00 puts both rows on the same instants');

    // Widened by a day each way so every UTC tick has a zone neighbour on both
    // sides; the outermost ones would otherwise only find the far one.
    const kolkata = new ZonedTime('Asia/Kolkata')
        .hourBoundaries(start - 86400000, end + 86400000, 2);
    const utc = utcHourBoundaries(start, end, 2);
    assert.ok(utc.every(u => !tsOf(kolkata).includes(u.ts)),
        '+05:30 must offset the UTC row from the zone row');
    for (const u of utc) {
        const nearest = kolkata.reduce((a, b) =>
            Math.abs(b.ts - u.ts) < Math.abs(a.ts - u.ts) ? b : a);
        assert.equal(Math.abs(nearest.ts - u.ts), 1800000, 'the shift is the offset\'s 30 minutes');
    }
    // Either way the labels themselves stay whole hours.
    assert.ok(utc.every(b => Number.isInteger(b.hour) && b.hour >= 0 && b.hour < 24));
});

test('day boundaries are local midnight in the configured zone', () => {
    for (const zone of ZONES) {
        const engine = new ZonedTime(zone);
        let ts = engine.startOfDay(Date.parse('2026-06-15T09:00:00Z'));
        for (let i = 0; i < 40; i++) {
            const p = engine.parts(ts);
            assert.equal(p.hour, 0, `${zone}: day boundary is not local midnight`);
            assert.equal(p.minute, 0);
            const next = engine.nextDay(ts);
            assert.ok(next > ts, `${zone}: day walk did not advance`);
            ts = next;
        }
    }
});

test('addDays lands on local midnight and inverts', () => {
    for (const zone of ZONES) {
        const z = new ZonedTime(zone);
        const mid = Date.parse('2026-06-15T15:45:00Z');
        const start = z.startOfDay(mid);
        assert.equal(z.addDays(mid, 0), start, `${zone}: addDays(0) is start of day`);
        assert.equal(z.addDays(start, 1), z.nextDay(start), `${zone}: addDays(1) matches nextDay`);
        assert.equal(z.addDays(z.addDays(start, 5), -5), start, `${zone}: addDays inverts`);
    }
});

test('a two-digit year keeps its calendar year', () => {
    // Date.UTC reads a year of 0-99 as 1900-1999.
    const z = new ZonedTime('UTC');
    const ts = z.wallClockToTs(50, 3, 1, 0, 0, 0);
    const expected = new Date(0);
    expected.setUTCFullYear(50, 2, 1);

    assert.equal(ts, expected.getTime());
    assert.equal(z.startOfDay(ts + 5 * 3600000), ts);
    const next = z.parts(z.addDays(ts, 1));
    assert.deepEqual([next.year, next.month, next.day, next.hour], [50, 3, 2, 0]);
    assert.equal(z.offsetMs(ts), 0);
});

test('addDays treats a DST transition as one calendar day', () => {
    const z = new ZonedTime('America/New_York');
    const sunday = z.wallClockToTs(2026, 3, 8, 0, 0, 0);
    const monday = z.addDays(sunday, 1);
    assert.equal(monday - sunday, 23 * 3600000);
    const p = z.parts(monday);
    assert.equal(p.hour, 0);
    assert.equal(p.day, 9);

    const novSun = z.wallClockToTs(2026, 11, 1, 0, 0, 0);
    const novMon = z.addDays(novSun, 1);
    assert.equal(novMon - novSun, 25 * 3600000);
});

test('addDays rolls over the year', () => {
    const z = new ZonedTime('UTC');
    const nye = z.wallClockToTs(2026, 12, 31, 0, 0, 0);
    const next = z.addDays(nye, 1);
    const p = z.parts(next);
    assert.equal(p.year, 2027);
    assert.equal(p.month, 1);
    assert.equal(p.day, 1);
});

test('wall-clock snap in America/New_York lands on :00/:15/:30/:45 local', () => {
    const engine = makeZonedEngine('America/New_York');
    engine.config.editSnapMinutes = 15;
    engine.config.snapToTimeZone = true;
    const z = engine._time;
    const samples = [
        z.wallClockToTs(2026, 7, 1, 10, 7, 0),
        z.wallClockToTs(2026, 7, 1, 10, 8, 0),
        z.wallClockToTs(2026, 3, 8, 1, 52, 0),
        z.wallClockToTs(2026, 3, 8, 3, 7, 0)
    ];
    for (const t of samples) {
        const snapped = engine._snapTime(t);
        const p = z.parts(snapped);
        assert.equal(p.minute % 15, 0, `minute ${p.minute} at ${snapped}`);
        assert.equal(p.second, 0);
    }
});

test('wall-clock snap on Europe/Berlin spring-forward skips 02:00', () => {
    const engine = makeZonedEngine('Europe/Berlin');
    engine.config.editSnapMinutes = 15;
    engine.config.snapToTimeZone = true;
    const z = engine._time;
    const around = z.wallClockToTs(2026, 3, 29, 1, 52, 0);
    const snapped = engine._snapTime(around);
    const p = z.parts(snapped);
    assert.notEqual(p.hour, 2);
    assert.equal(p.minute % 15, 0);
});

test('wall-clock snap in Asia/Kolkata follows the :30 offset', () => {
    const engine = makeZonedEngine('Asia/Kolkata');
    engine.config.editSnapMinutes = 15;
    engine.config.snapToTimeZone = true;
    const z = engine._time;
    const t = z.wallClockToTs(2026, 6, 1, 10, 7, 0);
    const snapped = engine._snapTime(t);
    const p = z.parts(snapped);
    assert.equal(p.minute % 15, 0);
});

test('Hour12 labels use a 12-hour clock', () => {
    const z = new ZonedTime('UTC', 'en-US');
    const label = z.formatHour(15, true);
    assert.match(label, /3\s*PM/i);
    assert.equal(z.formatHour(15, false), '15');
});

test('_weekStartDay honours FirstDayOfWeek Sunday', () => {
    const engine = makeBareEngine({ config: { firstDayOfWeek: 0 } });
    assert.equal(engine._weekStartDay(), 0);
    engine.config.firstDayOfWeek = null;
    engine.config.locale = 'de-DE';
    assert.equal(engine._weekStartDay(), 1);
});

test('_weekStartDay follows the viewer locale while no Locale is set', () => {
    // Intl.Locale takes no default of its own, so the engine asks Intl for the locale it formats in.
    const engine = makeBareEngine({ config: { firstDayOfWeek: null, locale: null } });
    const viewer = new Intl.Locale(new Intl.DateTimeFormat().resolvedOptions().locale);
    const info = viewer.weekInfo || (typeof viewer.getWeekInfo === 'function' ? viewer.getWeekInfo() : null);
    const expected = info && info.firstDay != null ? (info.firstDay === 7 ? 0 : info.firstDay) : 1;
    assert.equal(engine._weekStartDay(), expected);
});

test('a DST change that skips midnight still starts every day, and the day walk advances', () => {
    // America/Santiago springs forward at local midnight: 2025-09-07 00:00 never happens there, and the day starts at
    // 01:00 (-03), 04:00 UTC. Its earlier reading, 23:00 of the day before, made startOfDay and nextDay hand back the
    // same instant again and again, which hung every loop stepping a day at a time over the visible range.
    const z = new ZonedTime('America/Santiago');
    const gapDay = z.startOfDay(Date.parse('2025-09-07T12:00:00Z'));
    assert.equal(gapDay, Date.parse('2025-09-07T04:00:00Z'));
    const g = z.parts(gapDay);
    assert.deepEqual([g.year, g.month, g.day, g.hour, g.minute], [2025, 9, 7, 1, 0]);

    // A wall-clock time inside the gap moves forward by the gap's hour, as Temporal's 'compatible' resolution does.
    assert.equal(z.wallClockToTs(2025, 9, 7, 0, 30, 0), gapDay + 30 * 60000);

    for (const zone of ['America/Santiago', 'America/Havana', 'America/Asuncion']) {
        const zt = new ZonedTime(zone);
        let ts = zt.startOfDay(Date.parse('2025-01-01T12:00:00Z'));
        let prevDay = zt.parts(ts).day;
        for (let i = 0; i < 400; i++) {
            const next = zt.nextDay(ts);
            assert.ok(next > ts, `${zone}: the day walk is stuck at ${new Date(ts).toISOString()}`);
            const p = zt.parts(next);
            assert.notEqual(p.day, prevDay, `${zone}: ${new Date(next).toISOString()} starts no new day`);
            assert.ok(p.hour <= 1, `${zone}: a day starts at ${p.hour}:${p.minute}`);
            prevDay = p.day;
            ts = next;
        }
    }
});

test('continuous editing (EditSnapMinutes 0) snaps to whole milliseconds', () => {
    // An edited instant reaches .NET as a long, which System.Text.Json refuses a fraction for.
    const engine = makeZonedEngine('UTC');
    engine.config.editSnapMinutes = 0;
    assert.equal(engine._snapTime(1767340800123.457), 1767340800123);
    engine.config.editSnapMinutes = 0.1;
    assert.ok(Number.isInteger(engine._snapTime(1767340800123.457)));
});
