namespace Bit.Bheadless;

/// <summary>The part of a date or time a segment of a <see cref="BitBhDateField"/> / <see cref="BitBhTimeField"/> edits.</summary>
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

/// <summary>Whether a <see cref="BitBhTimeField"/> shows a 12-hour clock (with AM / PM) or a 24-hour clock.</summary>
public enum BitBhHourCycle
{
    /// <summary>1–12 with an AM / PM segment.</summary>
    H12,

    /// <summary>0–23.</summary>
    H24,
}

/// <summary>The smallest unit a <see cref="BitBhTimeField"/> edits.</summary>
public enum BitBhTimeGranularity
{
    /// <summary>Hours only (minutes and seconds are 0).</summary>
    Hour,

    /// <summary>Hours and minutes.</summary>
    Minute,

    /// <summary>Hours, minutes and seconds.</summary>
    Second,
}

/// <summary>
/// One segment of a date or time field, in the order the culture writes the parts: what a
/// <see cref="BitBhDateFieldSegment"/> renders, and the context of <see cref="BitBhDateFieldSegments"/>' content.
/// A snapshot taken when the segments rendered.
/// </summary>
public sealed class BitBhDateSegment
{
    internal BitBhDateSegment() { }

    /// <summary>What the segment edits (<see cref="BitBhDateSegmentType.Literal"/> for a separator).</summary>
    public required BitBhDateSegmentType Type { get; init; }

    /// <summary>The text shown: the value (padded as the culture writes it), the digits being typed, the placeholder or the separator.</summary>
    public required string Text { get; init; }

    /// <summary>Whether the segment can be edited (false for a separator).</summary>
    public bool IsEditable => Type != BitBhDateSegmentType.Literal;

    /// <summary>Whether the segment is empty and shows its placeholder.</summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>The value: a year, month, day … of the culture's calendar (1–12 for a 12-hour hour, 0 / 1 for AM / PM), or null when empty.</summary>
    public int? Value { get; init; }

    /// <summary>The smallest value the segment takes.</summary>
    public int MinValue { get; init; }

    /// <summary>The largest value the segment takes (the days of the month entered so far, for the day).</summary>
    public int MaxValue { get; init; }
}
