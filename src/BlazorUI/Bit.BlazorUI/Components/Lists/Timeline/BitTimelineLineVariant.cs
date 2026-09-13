namespace Bit.BlazorUI;

/// <summary>
/// Determines how the connecting line of the BitTimeline is painted.
/// </summary>
public enum BitTimelineLineVariant
{
    /// <summary>
    /// An uninterrupted line.
    /// </summary>
    Solid,

    /// <summary>
    /// A line drawn as a series of dashes, which usually marks a stretch of the timeline as pending or estimated.
    /// </summary>
    Dashed,

    /// <summary>
    /// A line drawn as a series of dots, a lighter version of the dashed line.
    /// </summary>
    Dotted
}
