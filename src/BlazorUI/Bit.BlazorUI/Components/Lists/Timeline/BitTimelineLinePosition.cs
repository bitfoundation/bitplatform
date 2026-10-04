namespace Bit.BlazorUI;

/// <summary>
/// Determines where the connecting line of the BitTimeline runs across its items.
/// </summary>
public enum BitTimelineLinePosition
{
    /// <summary>
    /// The line runs through the middle of the timeline, with the primary contents of the items on one side of it
    /// and the secondary contents on the other.
    /// </summary>
    Center,

    /// <summary>
    /// The line runs along the start edge of the timeline (the top in a horizontal one), with the primary and the
    /// secondary contents of each item stacked after it, as in an activity feed.
    /// </summary>
    Start,

    /// <summary>
    /// The line runs along the end edge of the timeline (the bottom in a horizontal one), with the primary and the
    /// secondary contents of each item stacked before it.
    /// </summary>
    End
}
