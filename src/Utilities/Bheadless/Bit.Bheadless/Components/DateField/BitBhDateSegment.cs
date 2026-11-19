namespace Bit.Bheadless;

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
