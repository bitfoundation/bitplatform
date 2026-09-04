namespace Bit.BlazorUI;

/// <summary>
/// Where the gap of a gauge-shaped BitProgress sits, which is also where its stroke begins and ends.
/// </summary>
public enum BitProgressGapPosition
{
    /// <summary>
    /// At the bottom of the ring, which is where a gauge is normally opened. This is the default.
    /// </summary>
    Bottom,

    /// <summary>
    /// At the top of the ring.
    /// </summary>
    Top,

    /// <summary>
    /// At the starting side of the ring - the left in a left-to-right context, the right in a
    /// right-to-left one.
    /// </summary>
    Start,

    /// <summary>
    /// At the ending side of the ring - the right in a left-to-right context, the left in a
    /// right-to-left one.
    /// </summary>
    End
}
