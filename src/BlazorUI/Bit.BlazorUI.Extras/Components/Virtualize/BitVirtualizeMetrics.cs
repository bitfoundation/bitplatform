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

    /// <summary>
    /// The size (px) of the list across the scroll axis.
    /// </summary>
    public double CrossSize { get; set; }

    /// <summary>
    /// How far (px) the start of the scroll range lies before the start of the items (e.g. the HeaderTemplate).
    /// </summary>
    public double HeadSize { get; set; }

    /// <summary>
    /// How far (px) the end of the scroll range lies past the end of the items (e.g. the FooterTemplate).
    /// </summary>
    public double TailSize { get; set; }
}
