namespace Bit.BlazorUI;

/// <summary>
/// One of the edges of a component or of the area it lives in, used wherever something is pinned to a side
/// rather than positioned at a point of a box - a label beside its control, a panel sliding in from an edge,
/// the header row of a pivot, the gap of a gauge.
/// </summary>
/// <remarks>
/// Start and End are logical and are what most components want: Start is the left in a left-to-right context
/// and the right in a right-to-left one, so the layout follows the reading direction of its content. Left and
/// Right are physical and stay on the same side of the screen in either direction, for the cases that must not
/// flip - a chart axis, above all.
/// <br />
/// Not every parameter typed as a side honours every value: the two combined values are only meaningful where
/// something can be pinned to both edges of an axis at once, and the physical pair only where a component draws
/// itself without reference to the reading direction. Each parameter names the values it accepts, and falls back
/// to its own default for the rest.
/// </remarks>
public enum BitSide
{
    /// <summary>
    /// The top edge.
    /// </summary>
    Top,

    /// <summary>
    /// The bottom edge.
    /// </summary>
    Bottom,

    /// <summary>
    /// The edge the reading direction starts from - the left in LTR, the right in RTL.
    /// </summary>
    Start,

    /// <summary>
    /// The edge the reading direction ends at - the right in LTR, the left in RTL.
    /// </summary>
    End,

    /// <summary>
    /// The left edge, in both reading directions.
    /// </summary>
    Left,

    /// <summary>
    /// The right edge, in both reading directions.
    /// </summary>
    Right,

    /// <summary>
    /// Both edges of the block axis at once.
    /// </summary>
    TopAndBottom,

    /// <summary>
    /// Both edges of the inline axis at once, following the reading direction the way Start and End do.
    /// </summary>
    StartAndEnd,
}
