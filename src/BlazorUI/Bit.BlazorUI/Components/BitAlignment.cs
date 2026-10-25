namespace Bit.BlazorUI;

/// <summary>
/// Defines how the content is placed, or the free space around it shared out, along an axis.
/// </summary>
public enum BitAlignment
{
    /// <summary>
    /// Packs the content at the start of the axis.
    /// </summary>
    Start,

    /// <summary>
    /// Packs the content at the end of the axis.
    /// </summary>
    End,

    /// <summary>
    /// Centers the content on the axis.
    /// </summary>
    Center,

    /// <summary>
    /// Distributes the free space between the items, with no space at the two edges.
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// Distributes the free space around the items, so the edges get half of what sits between the items.
    /// </summary>
    SpaceAround,

    /// <summary>
    /// Distributes the free space evenly between the items and at the two edges.
    /// </summary>
    SpaceEvenly,

    /// <summary>
    /// Aligns the items on their baseline.
    /// </summary>
    Baseline,

    /// <summary>
    /// Stretches the items to fill the axis.
    /// </summary>
    Stretch
}
