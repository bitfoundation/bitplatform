namespace BitBlazorUI {
    // Time-zone-aware date arithmetic for the BitResourceTimeline axis.
    //
    // Every boundary the axis draws - midnights, hour ticks - is a *wall-clock*
    // concept in some IANA zone, while the timeline itself is laid out in absolute
    // instants. This class is the only place that converts between the two.
    //
    // It holds no timeline state, so it can be constructed and exercised on its own
    // (see the time-axis tests). The formatters are cached because building an
    // Intl.DateTimeFormat is comparatively expensive and the axis converts many
    // boundaries per frame.

    /** The wall-clock parts of an instant in a zone. */
    export interface ResourceTimelineWallClock {
        year: number;
        month: number;
        day: number;
        hour: number;
        minute: number;
        second: number;
    }

    /** One hour boundary of an axis row: the instant, and the wall-clock hour (0-23) it is labelled with. */
    export interface ResourceTimelineHourBoundary {
        ts: number;
        hour: number;
    }

    export class ResourceTimelineZonedTime {
        public timeZone: string | null;
        public locale: string | null;

        // The instant of a UTC wall-clock time (`month` 1-12). Date.UTC alone reads
        // a year of 0-99 as 1900-1999, so such a year is set explicitly; the month,
        // day and time still normalize their overflow (a day 0, a day 32) the way
        // Date.UTC does. Every other year takes Date.UTC itself, which allocates
        // nothing (the axis converts many boundaries per frame).
        public static utc(year: number, month: number, day: number, hour: number = 0, minute: number = 0, second: number = 0): number {
            if (year < 0 || year > 99) return Date.UTC(year, month - 1, day, hour, minute, second);
            const date = new Date(0);
            date.setUTCFullYear(year, month - 1, day);
            date.setUTCHours(hour, minute, second, 0);
            return date.getTime();
        }

        private _partsDtf: Intl.DateTimeFormat;
        private _dateDtf: Intl.DateTimeFormat;
        private _dateTimeDtf: Intl.DateTimeFormat;
        // Created on the first 12-hour label (see formatHour).
        private _hour12Dtf: Intl.DateTimeFormat | undefined;

        // `timeZone` is an IANA id (e.g. "Europe/Berlin"), or null for the viewer's
        // local zone. `locale` is a BCP 47 tag for human-facing labels, or null for
        // the viewer's locale.
        constructor(timeZone: string | null = null, locale: string | null = null) {
            this.timeZone = timeZone || null;
            this.locale = locale || null;

            const zone = timeZone || undefined;
            const loc = locale || undefined;

            // Numeric wall-clock parts in the zone, used for day/hour math and the
            // hour label. en-US + h23 guarantees parseable 0-23 hour values
            // regardless of the display locale configured above.
            const partsOpts: Intl.DateTimeFormatOptions = {
                hourCycle: 'h23',
                year: 'numeric', month: '2-digit', day: '2-digit',
                hour: '2-digit', minute: '2-digit', second: '2-digit'
            };
            if (zone) partsOpts.timeZone = zone;
            this._partsDtf = new Intl.DateTimeFormat('en-US', partsOpts);

            // Localized day label; configured (or viewer) locale for i18n, zone for
            // correctness.
            const dateOpts: Intl.DateTimeFormatOptions = { weekday: 'short', month: 'short', day: 'numeric' };
            if (zone) dateOpts.timeZone = zone;
            this._dateDtf = new Intl.DateTimeFormat(loc, dateOpts);

            // Spoken/short date+time, used for screen-reader announcements, the
            // drag ghost readout and tooltips.
            const dateTimeOpts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' };
            if (zone) dateTimeOpts.timeZone = zone;
            this._dateTimeDtf = new Intl.DateTimeFormat(loc, dateTimeOpts);
        }

        // Wall-clock parts of an instant, in this zone.
        public parts(ts: number): ResourceTimelineWallClock {
            const parts = this._partsDtf.formatToParts(ts);
            const map: { [type: string]: string } = {};
            for (const p of parts) {
                if (p.type !== 'literal') map[p.type] = p.value;
            }
            let hour = +map.hour;
            if (hour === 24) hour = 0; // some engines emit '24' at midnight
            return {
                year: +map.year, month: +map.month, day: +map.day,
                hour, minute: +map.minute, second: +map.second
            };
        }

        // Offset (zone - UTC, in ms) in effect at the given instant.
        public offsetMs(ts: number): number {
            const p = this.parts(ts);
            const asUtc = ResourceTimelineZonedTime.utc(p.year, p.month, p.day, p.hour, p.minute, p.second);
            return asUtc - Math.floor(ts / 1000) * 1000;
        }

        // Converts a wall-clock time in this zone to an instant. Two-pass so it
        // resolves correctly across a DST offset change.
        //
        // A wall-clock time inside a spring-forward gap has no instant at all.
        // Neither offset round-trips then, and the time resolves to the later
        // candidate - moved forward by the length of the gap, as Temporal's
        // 'compatible' disambiguation does. That matters most for midnight: in
        // zones whose DST change skips it (America/Santiago, America/Havana,
        // America/Asuncion), it resolves to the first instant of the day, where
        // the earlier candidate, 23:00 of the previous day, would make startOfDay
        // and nextDay hand back the same instant again and again.
        //
        // A wall-clock time inside a fall-back overlap happens twice, and both
        // offsets round-trip. It resolves to the earlier instant, as Temporal's
        // 'compatible' disambiguation does - the first occurrence, which is the
        // one hourBoundaries emits for a repeated hour.
        public wallClockToTs(year: number, month: number, day: number, hour: number, minute: number, second: number): number {
            const guess = ResourceTimelineZonedTime.utc(year, month, day, hour, minute, second);
            const offset1 = this.offsetMs(guess);
            const ts1 = guess - offset1;
            const offset2 = this.offsetMs(ts1);
            if (offset2 === offset1) return this._firstOccurrence(guess, ts1, offset1);
            const ts2 = guess - offset2;
            if (this.offsetMs(ts2) === offset2) return this._firstOccurrence(guess, ts2, offset2);
            return Math.max(ts1, ts2);
        }

        // The earlier instant reading the wall clock `guess` (as a UTC number)
        // reads at `ts` under `offset`, when there is one: under the larger offset
        // in force a day earlier, when that offset round-trips too - which is only
        // the case in a fall-back overlap. Otherwise `ts` itself.
        private _firstOccurrence(guess: number, ts: number, offset: number): number {
            const earlierOffset = this.offsetMs(ts - 86400000);
            if (earlierOffset <= offset) return ts;
            const earlier = guess - earlierOffset;
            return this.offsetMs(earlier) === earlierOffset ? earlier : ts;
        }

        // Start of the day containing the given instant: local midnight, or the
        // first instant of the day where a DST change skips midnight.
        public startOfDay(ts: number): number {
            const p = this.parts(ts);
            return this.wallClockToTs(p.year, p.month, p.day, 0, 0, 0);
        }

        // Local midnight of the day `n` calendar days after the day containing
        // `ts` (`n` may be negative). The time of day is discarded, so a DST
        // 23- or 25-hour day is a single step. ResourceTimelineZonedTime.utc normalizes month/year
        // rollover.
        public addDays(ts: number, n: number): number {
            const p = this.parts(ts);
            const next = new Date(ResourceTimelineZonedTime.utc(p.year, p.month, p.day + n));
            return this.wallClockToTs(
                next.getUTCFullYear(), next.getUTCMonth() + 1, next.getUTCDate(), 0, 0, 0);
        }

        // First local midnight strictly after the given day start. Always later
        // than `dayStart`, so a loop stepping a day at a time over the visible
        // range always ends; a zone whose day could not be advanced (no such
        // zone is known) steps a plain 24 hours rather than standing still.
        public nextDay(dayStart: number): number {
            const next = this.addDays(dayStart, 1);
            return next > dayStart ? next : dayStart + 86400000;
        }

        // Hour boundaries within [visStart, visEnd] that land on a multiple of
        // `step` wall-clock hours, as [{ ts, hour }]. `step` must divide 24, which
        // is what makes step multiples align with local midnight.
        //
        // Iteration runs in wall-clock space (instant + zone offset), advancing by
        // the full step and converting back, so it costs one zone lookup per
        // *emitted* boundary instead of several per intervening hour. Callers pick
        // the step to keep boundaries a readable distance apart, which bounds the
        // iteration count by the viewport width at any zoom - no iteration cap
        // needed.
        //
        // DST: a spring-forward skips a wall-clock hour, which is detected and not
        // emitted. A fall-back repeats one, and only its first occurrence is
        // emitted (so an hour-level axis shows a two-hour gap between the two
        // surrounding ticks on that day) - long-standing behavior, not worth the
        // bookkeeping to change.
        public hourBoundaries(visStart: number, visEnd: number, step: number): ResourceTimelineHourBoundary[] {
            const out: ResourceTimelineHourBoundary[] = [];
            if (!(step > 0)) return out;

            const HOUR_MS = 3600000;
            const DAY_MS = 86400000;
            const stepMs = step * HOUR_MS;

            let offset = this.offsetMs(visStart);
            // First aligned wall-clock boundary at or after the window start.
            let wall = Math.ceil((visStart + offset) / stepMs) * stepMs;

            for (; ;) {
                // Resolve the wall-clock boundary back to an instant, re-checking
                // the offset so a DST transition inside the window stays aligned.
                let ts = wall - offset;
                const actual = this.offsetMs(ts);
                let exists = true;
                if (actual !== offset) {
                    offset = actual;
                    ts = wall - offset;
                    // The offset only disagrees at a transition. At a spring-forward
                    // the wall-clock hour is skipped entirely and has no instant at
                    // all (e.g. 02:00 never happens in New York on a March
                    // transition day); emitting it would put a mislabeled tick an
                    // hour off. Round-trip once more to tell the two cases apart.
                    exists = this.offsetMs(ts) === offset;
                }
                if (ts > visEnd) break;
                if (exists && ts >= visStart) {
                    out.push({ ts, hour: Math.floor((((wall % DAY_MS) + DAY_MS) % DAY_MS) / HOUR_MS) });
                }
                wall += stepMs;
            }
            return out;
        }

        // Localized day label for the date row ("Mon, Jul 20").
        public formatDate(ts: number): string {
            return this._dateDtf.format(ts);
        }

        // Short localized date + time, for announcements, tooltips and the ghost.
        public formatDateTime(ts: number): string {
            return this._dateTimeDtf.format(ts);
        }

        // Hour-row label. 24-hour is a zero-padded number; 12-hour uses the
        // locale's h12 cycle (en-US → "3 PM").
        public formatHour(hour: number, hour12: boolean): string {
            if (!hour12) return String(hour).padStart(2, '0');
            if (!this._hour12Dtf) {
                const opts: Intl.DateTimeFormatOptions = { hour: 'numeric', hourCycle: 'h12' };
                if (this.timeZone) opts.timeZone = this.timeZone;
                this._hour12Dtf = new Intl.DateTimeFormat(this.locale || undefined, opts);
            }
            const ts = this.wallClockToTs(2000, 1, 1, hour, 0, 0);
            return this._hour12Dtf.format(ts);
        }

        // Hour boundaries in UTC, for the optional second axis row - same shape and
        // contract as hourBoundaries, so the two rows are built identically.
        //
        // UTC has no offset and no DST, so its boundaries are simply the multiples of
        // `step` hours since the epoch (which is itself UTC midnight, and `step`
        // divides 24). That makes this exact arithmetic with no Intl involved, which
        // matters because it runs alongside the zoned walk on every frame.
        //
        // These sit apart from the zone row's by the minutes of the axis zone's offset
        // where it has any (Asia/Kolkata at +05:30, Australia/Eucla at +08:45) - which
        // is exactly the horizontal shift the UTC row is meant to show. Under a
        // whole-hour offset the row takes the zone row's instants instead (see
        // utcRowBoundaries).
        public static utcHourBoundaries(visStart: number, visEnd: number, step: number): ResourceTimelineHourBoundary[] {
            const out: ResourceTimelineHourBoundary[] = [];
            if (!(step > 0)) return out;

            const HOUR_MS = 3600000;
            const DAY_MS = 86400000;
            const stepMs = step * HOUR_MS;

            // First aligned boundary at or after the window start. Math.ceil keeps this
            // correct for pre-epoch (negative) instants too.
            for (let ts = Math.ceil(visStart / stepMs) * stepMs; ts <= visEnd; ts += stepMs) {
                out.push({ ts, hour: Math.floor((((ts % DAY_MS) + DAY_MS) % DAY_MS) / HOUR_MS) });
            }
            return out;
        }

        // The boundaries of the UTC row beside the given zone row. Where every zone
        // boundary is a whole UTC hour too (a whole-hour offset, DST changes of whole
        // hours included), the UTC row ticks on those very instants, labelled with
        // their UTC hours: on UTC's own multiples of `step` it would sit an hour off
        // the zone row wherever `step` does not divide the offset (a two-hour step
        // at +01:00). Otherwise (an offset with minutes in it) it is UTC's own row,
        // shifted by those minutes (see utcHourBoundaries).
        public static utcRowBoundaries(zoneHours: ResourceTimelineHourBoundary[], visStart: number, visEnd: number, step: number): ResourceTimelineHourBoundary[] {
            const HOUR_MS = 3600000;
            const DAY_MS = 86400000;

            let whole = zoneHours.length > 0;
            for (let i = 0; whole && i < zoneHours.length; i++) {
                if ((((zoneHours[i].ts % HOUR_MS) + HOUR_MS) % HOUR_MS) !== 0) whole = false;
            }
            if (!whole) return ResourceTimelineZonedTime.utcHourBoundaries(visStart, visEnd, step);

            const out: ResourceTimelineHourBoundary[] = new Array(zoneHours.length);
            for (let i = 0; i < zoneHours.length; i++) {
                const ts = zoneHours[i].ts;
                out[i] = { ts, hour: Math.floor((((ts % DAY_MS) + DAY_MS) % DAY_MS) / HOUR_MS) };
            }
            return out;
        }
    }
}
