namespace Bit.BlazorUI;

/// <summary>
/// The direction a <see cref="BitPullToRefresh"/> is pulled in to refresh.
/// </summary>
public enum BitPullToRefreshDirection
{
    /// <summary>
    /// Pulled down while the scroller is at its top, opening the strip over the top of the anchor.
    /// </summary>
    Down,

    /// <summary>
    /// Pulled up while the scroller is at its bottom, opening the strip over the bottom of the anchor.
    /// </summary>
    Up
}
