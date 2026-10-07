namespace Bit.BlazorUI;

/// <summary>
/// The readings of a <see cref="BitPlacement"/> shared by more than one component: as the edge of the screen a
/// surface slides in from - BitPanel, and the responsive panel modes of BitCallout and BitDropMenu - and as the
/// names the side and the alignment of a callout cross to the scripts under.
/// </summary>
internal static class BitPlacementExtensions
{
    /// <summary>
    /// The edge a panel actually slides in from.
    /// </summary>
    /// <remarks>
    /// The six edges are kept as they are named: Start and End are drawn logically and follow the reading
    /// direction, Left and Right are drawn physically and stay where they are in both. The physical pair is not
    /// turned into a logical one here, since the direction a panel is laid out in is not known until it is - a
    /// panel given no Dir reads the way the page or the box around it does - and a pair resolved against a guess
    /// lands on the wrong side of a right-to-left page. Every consumer of a panel placement goes through this
    /// rather than through the parameter itself, which is what keeps the class a panel draws, the axis it locks
    /// its swipe to and the edge the gesture is registered with from ever disagreeing. Center and the two combined
    /// values name no single edge, so they, like an unset placement, resolve to End, the default of every panel.
    /// </remarks>
    internal static BitPlacement ToPanelSide(this BitPlacement? placement) => placement switch
    {
        BitPlacement.Top or BitPlacement.Bottom or BitPlacement.Start or BitPlacement.End
            or BitPlacement.Left or BitPlacement.Right => placement.Value,
        _ => BitPlacement.End
    };

    /// <summary>
    /// The name a side crosses to the scripts under. The swipe gestures and the callout positioning both take
    /// their edge as one of these six words, so the order of <see cref="BitPlacement"/> is no contract with
    /// either. A side that is not one of the six edges becomes <paramref name="fallback"/>, which is the
    /// caller's to choose: a swipe is set up for the default edge, a callout is left to pick a side of its own.
    /// </summary>
    internal static string ToEdgeName(this BitPlacement? side, string fallback) => side switch
    {
        BitPlacement.Top => "top",
        BitPlacement.Bottom => "bottom",
        BitPlacement.Start => "start",
        BitPlacement.End => "end",
        BitPlacement.Left => "left",
        BitPlacement.Right => "right",
        _ => fallback
    };

    /// <summary>
    /// The name an alignment crosses to the callout positioning under (Callouts.ts), shared by BitCallout and
    /// BitDropMenu. Start is the default the script applies to an empty name, so it, like every value that names
    /// no edge to line up with, crosses as one.
    /// </summary>
    internal static string ToAlignmentName(this BitPlacement? alignment) => alignment switch
    {
        BitPlacement.Center => "center",
        BitPlacement.End => "end",
        BitPlacement.Left => "left",
        BitPlacement.Right => "right",
        BitPlacement.Top => "top",
        BitPlacement.Bottom => "bottom",
        _ => ""
    };

    /// <inheritdoc cref="ToEdgeName(BitPlacement?, string)"/>
    internal static string ToEdgeName(this BitPlacement side, string fallback) => ((BitPlacement?)side).ToEdgeName(fallback);

    /// <summary>
    /// Whether a panel side is on the inline axis, which decides both the axis its swipe gesture is locked to and
    /// which of the two coordinates the swipe callbacks are measured in.
    /// </summary>
    internal static bool IsInlineSide(this BitPlacement side) => side is BitPlacement.Start or BitPlacement.End
                                                                      or BitPlacement.Left or BitPlacement.Right;

    /// <summary>
    /// The axis a surface pinned to this side is swiped away along: the one it slid in on. The lock is what takes
    /// that axis from the page, since a top or bottom panel dragged with the wrong lock follows the finger while the
    /// page scrolls out from under it at the same time.
    /// </summary>
    internal static BitSwipeOrientation ToSwipeOrientation(this BitPlacement side)
        => side.IsInlineSide() ? BitSwipeOrientation.Horizontal : BitSwipeOrientation.Vertical;
}
