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
    /// A placement carries sides a panel has no styles for - the physical pair, Center and the two combined values -
    /// so every consumer of a panel placement goes through this rather than through the parameter itself, which is
    /// what keeps the class a panel draws, the axis it locks its swipe to and the edge the gesture is registered with
    /// from ever disagreeing. Those sides, and an unset placement, resolve to End, the default of every panel.
    /// </remarks>
    internal static BitPlacement ToPanelSide(this BitPlacement? placement) => placement switch
    {
        BitPlacement.Top or BitPlacement.Bottom or BitPlacement.Start or BitPlacement.End => placement.Value,
        _ => BitPlacement.End
    };

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
