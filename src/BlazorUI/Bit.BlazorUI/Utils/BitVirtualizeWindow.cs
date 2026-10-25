namespace Bit.BlazorUI;

/// <summary>
/// How many items a virtualized list renders before anything has been measured. Virtualize renders no item until
/// its JS has measured the viewport, which never happens before the page is interactive (a prerender, a static SSR
/// page), so every virtualized component of the library renders this first window itself: a viewport's worth of
/// items, the overscan on either side of it, and the item partially scrolled into view - about what Virtualize
/// renders once it has measured a viewport that size, so its first window is already there when it takes over.
/// </summary>
internal static class BitVirtualizeWindow
{
    /// <summary>
    /// The viewport, in px, assumed before one has been measured.
    /// </summary>
    internal const double AssumedViewportSize = 600;

    /// <summary>
    /// The overscan of Blazor's own Virtualize, which renders the windows of the components that do not set one.
    /// </summary>
    internal const int DefaultOverscanCount = 3;



    /// <summary>
    /// The number of items of the given size, in px, a viewport of the given size holds, plus the overscan on either
    /// side of them and the item partially scrolled into view.
    /// </summary>
    internal static int Estimate(double itemSize, int overscanCount = DefaultOverscanCount, double viewportSize = AssumedViewportSize)
    {
        return (int)Math.Ceiling(viewportSize / Math.Max(1d, itemSize)) + (Math.Max(0, overscanCount) * 2) + 1;
    }
}
