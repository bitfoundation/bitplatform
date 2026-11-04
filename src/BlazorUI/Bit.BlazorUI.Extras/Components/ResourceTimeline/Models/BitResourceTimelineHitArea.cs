namespace Bit.BlazorUI;

/// <summary>
/// Which region of the timeline a pointer event landed in.
/// </summary>
public enum BitResourceTimelineHitArea
{
    /// <summary>
    /// The scrollable grid of resource rows and allocation bars (right of the
    /// resource column, below the time axis).
    /// </summary>
    Content,

    /// <summary>
    /// The sticky resource column on the left (row labels / group chevrons).
    /// Has a resource but no time coordinate.
    /// </summary>
    ResourceAxis,

    /// <summary>
    /// The sticky time axis along the top. Has a time but no resource.
    /// </summary>
    TimeAxis,

    /// <summary>
    /// The top-left cell where the time axis and resource column meet.
    /// Neither a time nor a resource.
    /// </summary>
    Corner
}
