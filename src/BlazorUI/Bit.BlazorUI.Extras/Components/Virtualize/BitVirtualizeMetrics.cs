namespace Bit.BlazorUI;

/// <summary>
/// The scroll metrics of the BitVirtualize viewport reported by the JavaScript side.
/// </summary>
public sealed class BitVirtualizeMetrics
{
    /// <summary>
    /// The current scroll position (px) along the scroll axis.
    /// </summary>
    public double ScrollOffset { get; set; }

    /// <summary>
    /// The size (px) of the viewport along the scroll axis.
    /// </summary>
    public double ViewportSize { get; set; }
}
