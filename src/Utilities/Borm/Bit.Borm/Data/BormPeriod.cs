using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// The text forms of a month (<c>2024-05</c>) and an ISO 8601 week (<c>2024-W19</c>) — what an
/// <c>&lt;input type="month"&gt;</c> and an <c>&lt;input type="week"&gt;</c> speak — and the first day
/// each stands for, which is how a <c>DateOnly</c> or <c>DateTime</c> holds one.
/// </summary>
/// <remarks>
/// Browsers without a picker of their own (Firefox and Safari on the desktop) show a plain text box, so
/// what arrives is whatever was typed; anything that is not one of these forms is not read as one,
/// rather than guessed at.
/// </remarks>
internal static class BormPeriod
{
    public static string FormatMonth(DateTime date) => date.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static string FormatWeek(DateTime date)
        => string.Create(CultureInfo.InvariantCulture, $"{ISOWeek.GetYear(date):D4}-W{ISOWeek.GetWeekOfYear(date):D2}");

    /// <summary>Reads <c>yyyy-MM</c> as the first day of that month.</summary>
    public static bool TryParseMonth(string? text, out DateTime start)
    {
        start = default;
        if (text is not { Length: 7 } || text[4] != '-') return false;
        if (!int.TryParse(text.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(text.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || year < 1 || month is < 1 or > 12)
            return false;
        start = new DateTime(year, month, 1);
        return true;
    }

    /// <summary>Reads <c>yyyy-Www</c> as the Monday of that ISO week.</summary>
    public static bool TryParseWeek(string? text, out DateTime start)
    {
        start = default;
        if (text is not { Length: 8 } || text[4] != '-' || text[5] is not ('W' or 'w')) return false;
        if (!int.TryParse(text.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(text.AsSpan(6, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var week)
            || year < 1 || week < 1 || week > ISOWeek.GetWeeksInYear(year))
            return false;
        try
        {
            start = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            // The first week of year 1 begins before the calendar does.
            return false;
        }
    }

    /// <summary>Reads either form, as the field type says.</summary>
    public static bool TryParse(string? text, BormFieldType type, out DateTime start)
        => type == BormFieldType.Week ? TryParseWeek(text, out start) : TryParseMonth(text, out start);

    /// <summary>Reads either form, whichever it is — for code that has the text but not the field.</summary>
    public static bool TryParseAny(string? text, out DateTime start)
        => TryParseWeek(text, out start) || TryParseMonth(text, out start);
}
