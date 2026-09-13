namespace Bit.BlazorUI;

/// <summary>
/// Determines which ends of the connecting line of the BitTimeline are truncated at the first and the last dot.
/// </summary>
public enum BitTimelineTruncateLine
{
    /// <summary>
    /// The line runs through the whole extent of the timeline, past the first and the last dot.
    /// </summary>
    None,

    /// <summary>
    /// The line starts at the first dot instead of the leading edge of the timeline.
    /// </summary>
    Start,

    /// <summary>
    /// The line ends at the last dot instead of the trailing edge of the timeline.
    /// </summary>
    End,

    /// <summary>
    /// The line spans from the first dot to the last dot only.
    /// </summary>
    Both
}
