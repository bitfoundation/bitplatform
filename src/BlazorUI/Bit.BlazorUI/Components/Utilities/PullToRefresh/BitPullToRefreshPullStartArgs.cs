namespace Bit.BlazorUI;

public class BitPullToRefreshPullStartArgs(decimal top, decimal left, decimal width)
{
    /// <summary>
    /// The top offset of the pull to refresh element in pixels.
    /// </summary>
    public decimal Top { get; set; } = top;

    /// <summary>
    /// The left offset of the pull to refresh element in pixels.
    /// </summary>
    public decimal Left { get; set; } = left;

    /// <summary>
    /// The width of the pull to refresh element in pixels.
    /// </summary>
    public decimal Width { get; set; } = width;
}
