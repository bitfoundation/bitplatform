namespace Bit.BlazorUI;

/// <summary>
/// Determines where the dot of each item of the BitTimeline sits along its item, with the contents of the item aligned to it.
/// </summary>
public enum BitTimelineDotAlignment
{
    /// <summary>
    /// The dot sits at the middle of its item.
    /// </summary>
    Center,

    /// <summary>
    /// The dot sits at the start of its item (the top in a vertical timeline), next to the first line of the contents,
    /// which keeps it pinned to the heading of an item whose contents run over several lines.
    /// </summary>
    Start,

    /// <summary>
    /// The dot sits at the end of its item (the bottom in a vertical timeline).
    /// </summary>
    End
}
