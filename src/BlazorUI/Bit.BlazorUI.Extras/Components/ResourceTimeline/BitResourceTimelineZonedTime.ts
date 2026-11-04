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
            const asUtc = Date.UTC(p.year, p.month - 1, p.day, p.hour, p.minute, p.second);
            return asUtc - Math.floor(ts / 1000) * 1000;
        }

        // Converts a wall-clock time in this zone to an instant. Two-pass so it
        // resolves correctly across a DST offset change.
        public wallClockToTs(year: number, month: number, day: number, hour: number, minute: number, second: number): number {
            const guess = Date.UTC(year, month - 1, day, hour, minute, second);
            const offset1 = this.offsetMs(guess);
            let ts = guess - offset1;
            const offset2 = this.offsetMs(ts);
            if (offset2 !== offset1) {
                ts = guess - offset2;
            }
            return ts;
        }

        // Local midnight of the day containing the given instant.
        public startOfDay(ts: number): number {
            const p = this.parts(ts);
            return this.wallClockToTs(p.year, p.month, p.day, 0, 0, 0);
        }

        // Local midnight of the day `n` calendar days after the day containing
        // `ts` (`n` may be negative). The time of day is discarded, so a DST
        // 23- or 25-hour day is a single step. Date.UTC normalizes month/year
        // rollover.
        public addDays(ts: number, n: number): number {
            const p = this.parts(ts);
            const next = new Date(Date.UTC(p.year, p.month - 1, p.day + n));
            return this.wallClockToTs(
                next.getUTCFullYear(), next.getUTCMonth() + 1, next.getUTCDate(), 0, 0, 0);
        }

        // First local midnight strictly after the given day start.
        public nextDay(dayStart: number): number {
            return this.addDays(dayStart, 1);
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
        // The two rows' boundaries coincide wherever the axis zone's offset is a whole
        // number of hours, and sit apart by the offset's minutes where it is not
        // (Asia/Kolkata at +05:30, Australia/Eucla at +08:45) - which is exactly the
        // horizontal shift the UTC row is meant to show.
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
    }
}
