using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// Date arithmetic and text in a culture's own calendar (<see cref="DateTimeFormatInfo.Calendar"/>): the Gregorian
/// calendar for most cultures, the Persian calendar for <c>fa-IR</c>, Umm al-Qura for <c>ar-SA</c>, Thai Buddhist for
/// <c>th-TH</c> … Months, years and days of the month are the calendar's, so a month view, a heading and a date
/// field all follow the calendar the reader uses; <see cref="DateOnly"/> values themselves are calendar-neutral.
/// Dates outside the range the calendar supports (the Persian calendar starts in 622) are clamped into it.
/// </summary>
internal sealed class BitBhCalendarSystem
{
    private BitBhCalendarSystem(CultureInfo culture)
    {
        Culture = culture;
        Format = culture.DateTimeFormat;
        Calendar = Format.Calendar;
        MinSupported = DateOnly.FromDateTime(Calendar.MinSupportedDateTime);
        MaxSupported = DateOnly.FromDateTime(Calendar.MaxSupportedDateTime);
    }

    /// <summary>The calendar of <paramref name="culture"/> (the current culture when null).</summary>
    public static BitBhCalendarSystem For(CultureInfo? culture)
    {
        culture ??= CultureInfo.CurrentCulture;
        return _last is { } last && ReferenceEquals(last.Culture, culture) ? last : _last = new BitBhCalendarSystem(culture);
    }

    [ThreadStatic]
    private static BitBhCalendarSystem? _last;

    public CultureInfo Culture { get; }

    public DateTimeFormatInfo Format { get; }

    public Calendar Calendar { get; }

    /// <summary>The first date the calendar supports.</summary>
    public DateOnly MinSupported { get; }

    /// <summary>The last date the calendar supports.</summary>
    public DateOnly MaxSupported { get; }

    public DateOnly ClampSupported(DateOnly date) => date < MinSupported ? MinSupported : date > MaxSupported ? MaxSupported : date;

    private static DateTime ToDateTime(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);

    public int Year(DateOnly date) => Calendar.GetYear(ToDateTime(ClampSupported(date)));

    public int Month(DateOnly date) => Calendar.GetMonth(ToDateTime(ClampSupported(date)));

    public int Day(DateOnly date) => Calendar.GetDayOfMonth(ToDateTime(ClampSupported(date)));

    private int Era(DateOnly date) => Calendar.GetEra(ToDateTime(ClampSupported(date)));

    /// <summary>The first day of <paramref name="date"/>'s month.</summary>
    public DateOnly FirstOfMonth(DateOnly date)
    {
        date = ClampSupported(date);
        return ClampSupported(date.AddDays(1 - Day(date)));
    }

    /// <summary>The number of days in <paramref name="date"/>'s month.</summary>
    public int DaysInMonth(DateOnly date)
    {
        date = ClampSupported(date);
        return Calendar.GetDaysInMonth(Year(date), Month(date), Era(date));
    }

    /// <summary>The last day of <paramref name="date"/>'s month.</summary>
    public DateOnly LastOfMonth(DateOnly date)
    {
        var first = FirstOfMonth(date);
        return first.DayNumber + DaysInMonth(first) - 1 > MaxSupported.DayNumber ? MaxSupported : first.AddDays(DaysInMonth(first) - 1);
    }

    /// <summary>
    /// <paramref name="date"/> moved by <paramref name="months"/> months, its day of the month kept when the target month
    /// has it (31 January + 1 month is the last day of February). Clamped to the calendar's range.
    /// </summary>
    public DateOnly AddMonths(DateOnly date, int months)
    {
        if (months == 0) return ClampSupported(date);
        try
        {
            return ClampSupported(DateOnly.FromDateTime(Calendar.AddMonths(ToDateTime(ClampSupported(date)), months)));
        }
        catch (ArgumentException)
        {
            return months > 0 ? MaxSupported : MinSupported;
        }
    }

    /// <summary><paramref name="date"/> moved by <paramref name="years"/> years. Clamped to the calendar's range.</summary>
    public DateOnly AddYears(DateOnly date, int years)
    {
        if (years == 0) return ClampSupported(date);
        try
        {
            return ClampSupported(DateOnly.FromDateTime(Calendar.AddYears(ToDateTime(ClampSupported(date)), years)));
        }
        catch (ArgumentException)
        {
            return years > 0 ? MaxSupported : MinSupported;
        }
    }

    /// <summary><paramref name="date"/> moved by <paramref name="days"/> days. Clamped to the calendar's range.</summary>
    public DateOnly AddDays(DateOnly date, int days)
    {
        var number = (long)date.DayNumber + days;
        if (number <= MinSupported.DayNumber) return MinSupported;
        if (number >= MaxSupported.DayNumber) return MaxSupported;
        return DateOnly.FromDayNumber((int)number);
    }

    /// <summary>The number of months of <paramref name="year"/> (13 in a Hebrew leap year).</summary>
    public int MonthsInYear(int year)
    {
        try
        {
            return Calendar.GetMonthsInYear(year);
        }
        catch (ArgumentException)
        {
            return 12;
        }
    }

    /// <summary>The number of days of a month of the calendar, or null when the year or month is out of range.</summary>
    public int? DaysInMonth(int year, int month)
    {
        try
        {
            return Calendar.GetDaysInMonth(year, month);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The date of a year, month and day of the calendar, or null when there is none.</summary>
    public DateOnly? TryCreate(int year, int month, int day)
    {
        try
        {
            var date = DateOnly.FromDateTime(Calendar.ToDateTime(year, month, day, 0, 0, 0, 0));
            return date < MinSupported || date > MaxSupported ? null : date;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The first and last years of the calendar.</summary>
    public (int Min, int Max) YearRange => (Year(MinSupported), Year(MaxSupported));

    /// <summary>The month and year of <paramref name="date"/> as the culture writes them: "November 2026".</summary>
    public string MonthYear(DateOnly date) => ToDateTime(FirstOfMonth(date)).ToString(Format.YearMonthPattern, Culture);

    /// <summary>The name of <paramref name="date"/>'s month: "November".</summary>
    public string MonthName(DateOnly date)
    {
        var month = Month(date);
        var name = Format.GetMonthName(month);
        return string.IsNullOrEmpty(name) ? month.ToString(Culture) : name;
    }

    /// <summary>
    /// The months from <paramref name="first"/> to <paramref name="last"/>: "November 2026", "November – December 2026",
    /// "December 2026 – January 2027".
    /// </summary>
    public string MonthRange(DateOnly first, DateOnly last)
    {
        if (FirstOfMonth(first) == FirstOfMonth(last)) return MonthYear(first);
        var dash = $" {(char)0x2013} ";
        // The shorter form drops the first year only when the culture writes the month first.
        return Year(first) == Year(last) && Format.YearMonthPattern.TrimStart().StartsWith("MMMM", StringComparison.Ordinal)
            ? MonthName(first) + dash + MonthYear(last)
            : MonthYear(first) + dash + MonthYear(last);
    }

    /// <summary>The full date as the culture writes it: "Sunday, November 8, 2026".</summary>
    public string LongDate(DateOnly date) => ToDateTime(ClampSupported(date)).ToString(Format.LongDatePattern, Culture);

    /// <summary>The number of the day in its month: "8".</summary>
    public string DayNumber(DateOnly date) => Day(date).ToString(Culture);
}
