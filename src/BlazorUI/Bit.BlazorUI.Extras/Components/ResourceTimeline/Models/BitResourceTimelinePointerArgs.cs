namespace Bit.BlazorUI;

/// <summary>
/// Describes a click, double-click or right-click on the timeline: what was
/// under the pointer (bar, overflow cluster, resource row, time) and where
/// the pointer was, both in the surface and in the viewport.
/// </summary>
public class BitResourceTimelinePointerArgs
{
    /// <summary>
    /// The bar under the pointer, or <c>null</c> when the click did not hit a
    /// bar (empty content, an axis, or an overflow <c>+N</c> label). The same
    /// instance supplied in the config, so it can be compared by reference
    /// against the caller's own data.
    /// </summary>
    public BitResourceTimelineAllocation? Allocation { get; init; }

    /// <summary>
    /// Hidden overflow bars when the pointer hit a cluster's <c>+N</c> label;
    /// empty otherwise. Same instances as in the config.
    /// </summary>
    public IReadOnlyList<BitResourceTimelineAllocation> OverflowAllocations { get; init; } =
        [];

    /// <summary>
    /// The resource row under the pointer - set for clicks on bars, on empty
    /// content slots and on resource-axis rows; <c>null</c> on the time axis,
    /// the corner, or below the last row.
    /// </summary>
    public BitResourceTimelineResource? Resource { get; init; }

    /// <summary>
    /// The time at the pointer's horizontal position, or <c>null</c> when the
    /// click was on the resource axis or the corner (which have no time
    /// coordinate).
    /// </summary>
    public DateTimeOffset? Time { get; init; }

    /// <summary>Which region of the surface the pointer was in.</summary>
    public BitResourceTimelineHitArea Area { get; init; }

    /// <summary>
    /// Pointer X in surface-local coordinates (origin at the top-left of the
    /// timeline, including the sticky axes). Independent of page scroll.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Pointer Y in surface-local coordinates (origin at the top-left of the
    /// timeline, including the sticky axes). Independent of page scroll.
    /// </summary>
    public double Y { get; init; }

    /// <summary>Pointer X in viewport coordinates, suited for a <c>position: fixed</c> overlay.</summary>
    public double ClientX { get; init; }

    /// <summary>Pointer Y in viewport coordinates, suited for a <c>position: fixed</c> overlay.</summary>
    public double ClientY { get; init; }

    /// <summary>Whether <c>Ctrl</c> was held.</summary>
    public bool CtrlKey { get; init; }

    /// <summary>Whether <c>Shift</c> was held.</summary>
    public bool ShiftKey { get; init; }

    /// <summary>Whether <c>Meta</c> (<c>Cmd</c> on macOS, Windows key elsewhere) was held.</summary>
    public bool MetaKey { get; init; }

    /// <summary>Whether <c>Alt</c> was held.</summary>
    public bool AltKey { get; init; }
}
