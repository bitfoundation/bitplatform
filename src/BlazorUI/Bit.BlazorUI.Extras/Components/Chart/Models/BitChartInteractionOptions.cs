namespace Bit.BlazorUI;

/// <summary>
/// Interaction options, mirroring Chart.js <c>options.interaction</c>. They decide which elements
/// become active when the pointer moves over the chart, and the tooltip inherits them unless it
/// overrides <see cref="BitChartTooltipOptions.Mode"/> / <see cref="BitChartTooltipOptions.Intersect"/>.
/// </summary>
public sealed class BitChartInteractionOptions
{
    /// <summary>How the active element set is derived from the hovered element.</summary>
    public BitChartInteractionMode Mode { get; set; } = BitChartInteractionMode.Nearest;

    /// <summary>
    /// When true the pointer must be directly over an element for it to activate. When false (the
    /// default) the chart also reacts anywhere inside the plot area: an invisible hit band per
    /// category/x position activates that index, so thin lines and datasets drawn without point
    /// markers stay hoverable.
    /// </summary>
    public bool Intersect { get; set; }

    /// <summary>Draw a crosshair line through the active index (index-style interactions only).</summary>
    public bool Crosshair { get; set; } = true;

    /// <summary>Color of the crosshair line.</summary>
    public string CrosshairColor { get; set; } = "var(--bit-clr-fg-sec, rgba(0,0,0,0.45))";

    /// <summary>
    /// Show the active index in a small chip where the crosshair meets the index axis, so the reader can
    /// see which category is being compared without leaving the plot.
    /// </summary>
    public bool CrosshairLabel { get; set; } = true;
}
