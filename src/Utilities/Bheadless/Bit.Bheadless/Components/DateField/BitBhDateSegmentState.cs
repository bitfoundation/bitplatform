using System.Globalization;
using System.Text;

namespace Bit.Bheadless;

/// <summary>
/// The editing model behind a segmented date / time field (React Aria's DateField): which segments there are and in
/// which order (from a .NET date / time pattern), what each holds, and how keys change them. Values are the culture
/// calendar's own (a Persian year, month and day for <c>fa-IR</c>); a 12-hour hour is kept as 1–12 next to its AM / PM.
/// </summary>
internal sealed class BitBhDateSegmentState
{
    /// <summary>One segment of the layout: an editable part, or literal text between parts.</summary>
    internal readonly record struct Spec(BitBhDateSegmentType Type, string Literal = "", bool Pad = false);

    private readonly int?[] _values = new int?[8];

    public List<Spec> Layout { get; private set; } = [];

    public bool Is12Hour { get; private set; }

    public BitBhCalendarSystem System { get; private set; } = BitBhCalendarSystem.For(CultureInfo.InvariantCulture);

    /// <summary>The segment digits are being typed into, and those digits (shown until the segment is left or full).</summary>
    public BitBhDateSegmentType? TypingSegment { get; private set; }

    public string Buffer { get; private set; } = string.Empty;

    public IEnumerable<BitBhDateSegmentType> Editable => Layout.Where(s => s.Type != BitBhDateSegmentType.Literal).Select(s => s.Type);

    public bool Has(BitBhDateSegmentType type) => Layout.Any(s => s.Type == type);

    public int? this[BitBhDateSegmentType type]
    {
        get => _values[(int)type];
        private set => _values[(int)type] = value;
    }

    public bool IsComplete => Layout.Count > 0 && Editable.All(t => this[t] is not null);

    public bool IsEmpty => Editable.All(t => this[t] is null);

    // ── Layout ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the segments of a .NET custom date / time pattern ("M/d/yyyy", "dd.MM.yyyy", "h:mm:ss tt"): day, month,
    /// year, hour, minute, second and AM / PM become segments, quoted and other text becomes literals. Week-day names,
    /// eras, fractions and offsets are left out.
    /// </summary>
    public static List<Spec> Parse(string pattern)
    {
        var specs = new List<Spec>();
        var literal = new StringBuilder();

        void Flush()
        {
            if (literal.Length == 0) return;
            specs.Add(new Spec(BitBhDateSegmentType.Literal, literal.ToString()));
            literal.Clear();
        }

        void Add(BitBhDateSegmentType type, bool pad)
        {
            Flush();
            // A pattern that names a part twice keeps the first.
            if (specs.All(s => s.Type != type)) specs.Add(new Spec(type, Pad: pad));
        }

        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c is '\'' or '"')
            {
                var end = pattern.IndexOf(c, i + 1);
                if (end < 0) end = pattern.Length;
                literal.Append(pattern, i + 1, end - i - 1);
                i = end;
                continue;
            }
            if (c == '\\')
            {
                if (i + 1 < pattern.Length) literal.Append(pattern[++i]);
                continue;
            }
            if (c == '%') continue;

            var run = 1;
            while (i + run < pattern.Length && pattern[i + run] == c) run++;
            switch (c)
            {
                case 'd' when run <= 2: Add(BitBhDateSegmentType.Day, run == 2); break;
                case 'M': Add(BitBhDateSegmentType.Month, run >= 2); break;
                case 'y': Add(BitBhDateSegmentType.Year, false); break;
                case 'h': Add(BitBhDateSegmentType.Hour, run >= 2); break;
                case 'H': Add(BitBhDateSegmentType.Hour, run >= 2); break;
                case 'm': Add(BitBhDateSegmentType.Minute, true); break;
                case 's': Add(BitBhDateSegmentType.Second, true); break;
                case 't': Add(BitBhDateSegmentType.DayPeriod, false); break;
                case 'd' or 'g' or 'f' or 'F' or 'z' or 'K':
                    // Week-day names, eras, fractions and offsets: not edited, and their spacing goes with them.
                    Flush();
                    specs.Add(new Spec(BitBhDateSegmentType.Literal, string.Empty));
                    break;
                default:
                    literal.Append(c, run);
                    break;
            }
            i += run - 1;
        }
        Flush();
        return Normalize(specs);
    }

    /// <summary>Merges neighbouring literals and drops the ones at the ends (and those left empty).</summary>
    private static List<Spec> Normalize(List<Spec> specs)
    {
        var result = new List<Spec>();
        foreach (var spec in specs)
        {
            if (spec.Type == BitBhDateSegmentType.Literal && result.Count > 0 && result[^1].Type == BitBhDateSegmentType.Literal)
            {
                var merged = result[^1].Literal + spec.Literal;
                result[^1] = new Spec(BitBhDateSegmentType.Literal, merged);
                continue;
            }
            result.Add(spec);
        }
        while (result.Count > 0 && result[0].Type == BitBhDateSegmentType.Literal) result.RemoveAt(0);
        while (result.Count > 0 && result[^1].Type == BitBhDateSegmentType.Literal) result.RemoveAt(result.Count - 1);
        // A literal that only held what was dropped (an era) still separates two parts: keep a space.
        for (var i = 0; i < result.Count; i++)
            if (result[i].Type == BitBhDateSegmentType.Literal && result[i].Literal.Length == 0) result[i] = new Spec(BitBhDateSegmentType.Literal, " ");
        return result;
    }

    /// <summary>Removes the segment of <paramref name="type"/> and the separator that went with it.</summary>
    public static void Remove(List<Spec> specs, BitBhDateSegmentType type)
    {
        var index = specs.FindIndex(s => s.Type == type);
        if (index < 0) return;
        specs.RemoveAt(index);
        if (index > 0 && specs[index - 1].Type == BitBhDateSegmentType.Literal) specs.RemoveAt(index - 1);
        else if (index < specs.Count && specs[index].Type == BitBhDateSegmentType.Literal) specs.RemoveAt(index);
    }

    /// <summary>Applies a layout. Values of parts that are still there are kept; the hour follows a change of clock.</summary>
    public void Configure(BitBhCalendarSystem system, List<Spec> layout, bool is12Hour)
    {
        if (!ReferenceEquals(system.Calendar, System.Calendar) && system.Calendar.GetType() != System.Calendar.GetType())
        {
            // Another calendar: the year, month and day mean other things now. The owner reloads them from its value.
            this[BitBhDateSegmentType.Year] = this[BitBhDateSegmentType.Month] = this[BitBhDateSegmentType.Day] = null;
        }
        System = system;
        if (is12Hour != Is12Hour && this[BitBhDateSegmentType.Hour] is { } hour)
        {
            if (is12Hour)
            {
                this[BitBhDateSegmentType.DayPeriod] = hour >= 12 ? 1 : 0;
                this[BitBhDateSegmentType.Hour] = hour % 12 == 0 ? 12 : hour % 12;
            }
            else
            {
                this[BitBhDateSegmentType.Hour] = (hour % 12) + (this[BitBhDateSegmentType.DayPeriod] == 1 ? 12 : 0);
            }
        }
        Is12Hour = is12Hour;
        Layout = layout;
        foreach (var type in Enum.GetValues<BitBhDateSegmentType>())
            if (type != BitBhDateSegmentType.Literal && !Has(type)) this[type] = null;
        if (TypingSegment is { } typing && !Has(typing)) EndTyping();
    }

    // ── Values ───────────────────────────────────────────────────────────────

    public void Clear()
    {
        Array.Clear(_values);
        EndTyping();
    }

    public void SetDate(DateOnly date)
    {
        date = System.ClampSupported(date);
        this[BitBhDateSegmentType.Year] = System.Year(date);
        this[BitBhDateSegmentType.Month] = System.Month(date);
        this[BitBhDateSegmentType.Day] = System.Day(date);
        EndTyping();
    }

    public void SetTime(TimeOnly time)
    {
        if (Is12Hour)
        {
            this[BitBhDateSegmentType.Hour] = time.Hour % 12 == 0 ? 12 : time.Hour % 12;
            this[BitBhDateSegmentType.DayPeriod] = time.Hour >= 12 ? 1 : 0;
        }
        else
        {
            this[BitBhDateSegmentType.Hour] = time.Hour;
        }
        if (Has(BitBhDateSegmentType.Minute)) this[BitBhDateSegmentType.Minute] = time.Minute;
        if (Has(BitBhDateSegmentType.Second)) this[BitBhDateSegmentType.Second] = time.Second;
        EndTyping();
    }

    /// <summary>
    /// The date the year, month and day make, the day kept within its month (31 February is the last day of February,
    /// and the day segment says so); null while a part is missing or the date doesn't exist in the calendar.
    /// </summary>
    public DateOnly? ComposeDate()
    {
        if (this[BitBhDateSegmentType.Year] is not { } year || this[BitBhDateSegmentType.Month] is not { } month || this[BitBhDateSegmentType.Day] is not { } day)
            return null;
        if (month > System.MonthsInYear(year)) return null;
        if (System.DaysInMonth(year, month) is { } days && day > days)
        {
            day = days;
            this[BitBhDateSegmentType.Day] = day;
        }
        return System.TryCreate(year, month, day);
    }

    /// <summary>The time the hour, minute, second (and AM / PM) make; null while a part is missing.</summary>
    public TimeOnly? ComposeTime()
    {
        if (this[BitBhDateSegmentType.Hour] is not { } hour) return null;
        if (Is12Hour)
        {
            if (this[BitBhDateSegmentType.DayPeriod] is not { } period) return null;
            hour = (hour % 12) + (period == 1 ? 12 : 0);
        }
        var minute = Has(BitBhDateSegmentType.Minute) ? this[BitBhDateSegmentType.Minute] : 0;
        var second = Has(BitBhDateSegmentType.Second) ? this[BitBhDateSegmentType.Second] : 0;
        if (minute is null || second is null) return null;
        return new TimeOnly(Math.Clamp(hour, 0, 23), Math.Clamp(minute.Value, 0, 59), Math.Clamp(second.Value, 0, 59));
    }

    /// <summary>The values a segment takes, given what the other segments hold (and the year to assume while it is empty).</summary>
    public (int Min, int Max) Bounds(BitBhDateSegmentType type, int placeholderYear)
    {
        switch (type)
        {
            case BitBhDateSegmentType.Year:
                return System.YearRange;
            case BitBhDateSegmentType.Month:
                return (1, System.MonthsInYear(this[BitBhDateSegmentType.Year] ?? placeholderYear));
            case BitBhDateSegmentType.Day:
                return (1, MaxDay(placeholderYear));
            case BitBhDateSegmentType.Hour:
                return Is12Hour ? (1, 12) : (0, 23);
            case BitBhDateSegmentType.Minute:
            case BitBhDateSegmentType.Second:
                return (0, 59);
            case BitBhDateSegmentType.DayPeriod:
                return (0, 1);
            default:
                return (0, 0);
        }
    }

    private int MaxDay(int placeholderYear)
    {
        var month = this[BitBhDateSegmentType.Month];
        if (this[BitBhDateSegmentType.Year] is { } year)
        {
            if (month is { } m) return System.DaysInMonth(year, m) ?? 31;
            return Enumerable.Range(1, System.MonthsInYear(year)).Max(mm => System.DaysInMonth(year, mm) ?? 0);
        }
        // No year yet: the longest the month gets in the years around (29 February is fine until a year says otherwise).
        var max = 0;
        var (minYear, maxYear) = System.YearRange;
        for (var y = Math.Max(minYear, placeholderYear - 4); y <= Math.Min(maxYear, placeholderYear + 4); y++)
        {
            if (month is { } m)
            {
                if (m <= System.MonthsInYear(y)) max = Math.Max(max, System.DaysInMonth(y, m) ?? 0);
            }
            else
            {
                for (var mm = 1; mm <= System.MonthsInYear(y); mm++) max = Math.Max(max, System.DaysInMonth(y, mm) ?? 0);
            }
        }
        return max > 0 ? max : 31;
    }

    // ── Editing ──────────────────────────────────────────────────────────────

    public void EndTyping()
    {
        TypingSegment = null;
        Buffer = string.Empty;
    }

    /// <summary>
    /// Steps a segment by <paramref name="delta"/>, wrapping around its bounds (the year stops at them). An empty segment
    /// takes <paramref name="placeholder"/> first.
    /// </summary>
    public void Step(BitBhDateSegmentType type, int delta, int placeholder, int placeholderYear)
    {
        EndTyping();
        var (min, max) = Bounds(type, placeholderYear);
        if (this[type] is not { } value)
        {
            this[type] = Math.Clamp(placeholder, min, max);
            return;
        }
        if (type == BitBhDateSegmentType.Year)
        {
            this[type] = Math.Clamp(value + delta, min, max);
            return;
        }
        var span = max - min + 1;
        var next = ((value - min + delta) % span + span) % span + min;
        this[type] = next;
    }

    public void SetToBound(BitBhDateSegmentType type, bool max, int placeholderYear)
    {
        EndTyping();
        var bounds = Bounds(type, placeholderYear);
        this[type] = max ? bounds.Max : bounds.Min;
    }

    public void SetDayPeriod(bool pm)
    {
        EndTyping();
        this[BitBhDateSegmentType.DayPeriod] = pm ? 1 : 0;
    }

    /// <summary>
    /// Types a digit into a segment (React Aria's rules): the digits typed so far are read as one number, which starts
    /// over at this digit when it passes the segment's maximum. Answers whether the segment is full, so the focus moves on.
    /// </summary>
    public bool TypeDigit(BitBhDateSegmentType type, int digit, int placeholderYear)
    {
        var (min, max) = Bounds(type, placeholderYear);
        var text = (TypingSegment == type ? Buffer : string.Empty) + digit.ToString(CultureInfo.InvariantCulture);
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number > max)
        {
            text = digit.ToString(CultureInfo.InvariantCulture);
            number = digit;
        }
        var maxLength = max.ToString(CultureInfo.InvariantCulture).Length;

        // A leading zero the segment can't hold yet (month 0): kept as typed, the segment is empty until the next digit.
        if (number < min)
        {
            this[type] = null;
            if (text.Length >= maxLength)
            {
                EndTyping();
                return false;
            }
            TypingSegment = type;
            Buffer = text;
            return false;
        }

        this[type] = (int)number;
        if ((number * 10) > max || text.Length >= maxLength)
        {
            EndTyping();
            return true;
        }
        TypingSegment = type;
        Buffer = text;
        return false;
    }

    /// <summary>
    /// Deletes the last digit of a segment (empty once none are left). Answers whether the segment was already empty,
    /// so Backspace moves the focus back.
    /// </summary>
    public bool Backspace(BitBhDateSegmentType type)
    {
        if (this[type] is null && !(TypingSegment == type && Buffer.Length > 0))
        {
            EndTyping();
            return true;
        }
        if (type == BitBhDateSegmentType.DayPeriod)
        {
            this[type] = null;
            EndTyping();
            return false;
        }
        var text = TypingSegment == type && Buffer.Length > 0 ? Buffer : this[type]!.Value.ToString(CultureInfo.InvariantCulture);
        text = text[..^1];
        if (text.Length == 0 || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            this[type] = null;
            EndTyping();
            return false;
        }
        this[type] = number;
        TypingSegment = type;
        Buffer = text;
        return false;
    }

    // ── Text ─────────────────────────────────────────────────────────────────

    public string AmDesignator => string.IsNullOrEmpty(System.Format.AMDesignator) ? "AM" : System.Format.AMDesignator;

    public string PmDesignator => string.IsNullOrEmpty(System.Format.PMDesignator) ? "PM" : System.Format.PMDesignator;

    /// <summary>What a segment shows: the digits being typed, the value (padded as the pattern asks) or the placeholder.</summary>
    public string Text(Spec spec, BitBhStrings strings)
    {
        if (spec.Type == BitBhDateSegmentType.Literal) return spec.Literal;
        if (TypingSegment == spec.Type && Buffer.Length > 0) return Buffer;
        if (this[spec.Type] is not { } value)
            return spec.Type == BitBhDateSegmentType.DayPeriod ? AmDesignator : strings.DateSegmentPlaceholder(spec.Type);
        return spec.Type switch
        {
            BitBhDateSegmentType.DayPeriod => value == 1 ? PmDesignator : AmDesignator,
            BitBhDateSegmentType.Minute or BitBhDateSegmentType.Second => value.ToString("D2", CultureInfo.InvariantCulture),
            _ => value.ToString(spec.Pad ? "D2" : "D", CultureInfo.InvariantCulture),
        };
    }

    /// <summary>What a segment's value is announced as (<c>aria-valuetext</c>): "11 – November", "3 PM", "Empty".</summary>
    public string ValueText(BitBhDateSegmentType type, BitBhStrings strings)
    {
        if (this[type] is not { } value) return strings.EmptySegment;
        var number = value.ToString(CultureInfo.InvariantCulture);
        switch (type)
        {
            case BitBhDateSegmentType.Month:
                var name = value <= 13 ? System.Format.GetMonthName(value) : string.Empty;
                return string.IsNullOrEmpty(name) ? number : $"{number} {(char)0x2013} {name}";
            case BitBhDateSegmentType.Hour when Is12Hour && this[BitBhDateSegmentType.DayPeriod] is { } period:
                return $"{number} {(period == 1 ? PmDesignator : AmDesignator)}";
            case BitBhDateSegmentType.Minute or BitBhDateSegmentType.Second:
                return value.ToString("D2", CultureInfo.InvariantCulture);
            case BitBhDateSegmentType.DayPeriod:
                return value == 1 ? PmDesignator : AmDesignator;
            default:
                return number;
        }
    }
}
