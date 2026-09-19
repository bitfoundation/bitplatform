namespace Bit.BlazorUI;

/// <summary>
/// The reading of a <see cref="BitPlacement"/> as the edge of the screen a surface slides in from, shared by
/// every surface that does - BitPanel, and the responsive panel modes of BitCallout and BitDropMenu.
/// </summary>
internal static class BitPlacementExtensions
{
    /// <summary>
    /// The edge a panel actually slides in from.
    /// </summary>
    /// <remarks>
    /// A panel draws its inline edges logically - one class for Start and one for End - so the physical pair is
    /// read against the direction the panel is laid out in: Left is Start in a left-to-right panel and End in a
    /// right-to-left one, Right the other way round. Every consumer of a panel placement goes through this rather
    /// than through the parameter itself, which is what keeps the class a panel draws, the axis it locks its swipe
    /// to and the edge the gesture is registered with from ever disagreeing. Center and the two combined values
    /// name no single edge, so they, like an unset placement, resolve to End, the default of every panel.
    /// </remarks>
    internal static BitPlacement ToPanelSide(this BitPlacement? placement, bool rtl) => placement switch
    {
        BitPlacement.Top or BitPlacement.Bottom or BitPlacement.Start or BitPlacement.End => placement.Value,
        BitPlacement.Left => rtl ? BitPlacement.End : BitPlacement.Start,
        BitPlacement.Right => rtl ? BitPlacement.Start : BitPlacement.End,
        _ => BitPlacement.End
    };

    /// <summary>
    /// The name a side crosses to the scripts under. The swipe gestures and the callout positioning both take
    /// their edge as one of these four words, so the order of <see cref="BitPlacement"/> is no contract with
    /// either. A side that is not one of the four edges becomes <paramref name="fallback"/>, which is the
    /// caller's to choose: a swipe is set up for the default edge, a callout is left to pick a side of its own.
    /// </summary>
    internal static string ToEdgeName(this BitPlacement? side, string fallback) => side switch
    {
        BitPlacement.Top => "top",
        BitPlacement.Bottom => "bottom",
        BitPlacement.Start => "start",
        BitPlacement.End => "end",
        _ => fallback
    };

    /// <inheritdoc cref="ToEdgeName(BitPlacement?, string)"/>
    internal static string ToEdgeName(this BitPlacement side, string fallback) => ((BitPlacement?)side).ToEdgeName(fallback);

    /// <summary>
    /// Whether a panel side is on the inline axis, which decides both the axis its swipe gesture is locked to and
    /// which of the two coordinates the swipe callbacks are measured in.
    /// </summary>
    internal static bool IsInlineSide(this BitPlacement side) => side is BitPlacement.Start or BitPlacement.End;

    /// <summary>
    /// The axis a surface pinned to this side is swiped away along: the one it slid in on. The lock is what takes
    /// that axis from the page, since a top or bottom panel dragged with the wrong lock follows the finger while the
    /// page scrolls out from under it at the same time.
    /// </summary>
    internal static BitSwipeOrientation ToSwipeOrientation(this BitPlacement side)
        => side.IsInlineSide() ? BitSwipeOrientation.Horizontal : BitSwipeOrientation.Vertical;
}
