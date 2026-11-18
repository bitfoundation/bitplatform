namespace Bit.Bheadless;

/// <summary>
/// The part of a date or time a segment of a <see cref="BitBhDateField"/> / <see cref="BitBhTimeField"/> /
/// <see cref="BitBhDateTimeField"/> edits.
/// </summary>
public enum BitBhDateSegmentType
{
    /// <summary>Text between the editable segments (a separator such as "/", "." or ":"). Not editable.</summary>
    Literal,

    /// <summary>The year.</summary>
    Year,

    /// <summary>The month (a number).</summary>
    Month,

    /// <summary>The day of the month.</summary>
    Day,

    /// <summary>The hour (1–12 on a 12-hour clock, 0–23 on a 24-hour clock).</summary>
    Hour,

    /// <summary>The minute.</summary>
    Minute,

    /// <summary>The second.</summary>
    Second,

    /// <summary>AM or PM (12-hour clock).</summary>
    DayPeriod,
}
