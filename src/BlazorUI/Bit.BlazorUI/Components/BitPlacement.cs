namespace Bit.BlazorUI;

/// <summary>
/// Where something is placed on a single axis: against one of the edges, in the middle, or - where a
/// component can hold both edges at once - against a pair of them.
/// </summary>
/// <remarks>
/// This is the vocabulary for every one-dimensional placement in the library, and it answers two related
/// questions. Which side of a box or of an anchor something sits on - a label beside its control, a panel
/// sliding in from an edge, the header row of a pivot, the gap of a gauge, the side of the anchor a tooltip
/// is on. And, where a placement has already picked a side, where along that side the thing lines up, which
/// is the axis the side leaves free: a surface above or below its anchor is aligned horizontally, one beside
/// it vertically.
/// <br />
/// Start and End are logical and are what most components want: Start is the left in a left-to-right context
/// and the right in a right-to-left one, so the layout follows the reading direction of its content. Left and
/// Right are physical and stay on the same side of the screen in either direction, for the cases that must not
/// flip - a chart axis, above all. On the vertical axis, where there is no left or right, Top and Bottom are
/// themselves the physical pair.
/// <br />
/// Not every parameter typed as a placement honours every value: the two combined values are only meaningful
/// where something can be pinned to both edges of an axis at once, Center only where something can sit in the
/// middle rather than against an edge, and the physical pairs only where a component draws itself without
/// reference to the reading direction. Each parameter names the values it accepts, and falls back to its own
/// default for the rest.
/// <br />
/// A placement that names a point of a two-dimensional area rather than one axis - a corner, a centered edge -
/// is a <see cref="BitPosition"/> instead.
/// </remarks>
public enum BitPlacement
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
    /// The edge the reading direction starts from - the left in LTR, the right in RTL. On the vertical axis,
    /// which does not turn around, it is the top.
    /// </summary>
    Start,

    /// <summary>
    /// The edge the reading direction ends at - the right in LTR, the left in RTL. On the vertical axis,
    /// which does not turn around, it is the bottom.
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
    /// The middle of the axis, against neither edge.
    /// </summary>
    Center,

    /// <summary>
    /// Both edges of the block axis at once.
    /// </summary>
    TopAndBottom,

    /// <summary>
    /// Both edges of the inline axis at once, following the reading direction the way Start and End do.
    /// </summary>
    StartAndEnd,
}
