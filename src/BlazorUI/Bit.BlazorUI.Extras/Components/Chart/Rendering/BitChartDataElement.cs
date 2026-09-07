
namespace Bit.BlazorUI;

/// <summary>An interactive data-driven element (bar, point, arc, ...).</summary>
public sealed class BitChartDataElement
{
    public required BitChartSvgNode Shape { get; init; }
    public int DatasetIndex { get; init; }
    public int DataIndex { get; init; }
    public BitChartTooltipInfo Tooltip { get; init; } = new();
    /// <summary>Hit-test centroid in chart pixel coordinates.</summary>
    public double CenterX { get; init; }
    public double CenterY { get; init; }
    /// <summary>
    /// The shape drawn on top of this element while it is active (hovered or keyboard-focused). It is
    /// precomputed by the renderer with the dataset's hover styling - and with
    /// <see cref="BitChartScriptableContext.Active"/> set - so hovering never costs a re-layout.
    /// </summary>
    public BitChartSvgNode? HoverShape { get; init; }
    /// <summary>Optional secondary shape drawn with the element (e.g. a bar's skipped-edge border)
    /// so it animates together with the fill.</summary>
    public BitChartSvgNode? BorderShape { get; init; }
    /// <summary>Primary numeric value of this element (for tooltip templates).</summary>
    public double Value { get; init; }
    /// <summary>Series (dataset) label, for tooltip templates.</summary>
    public string? SeriesLabel { get; init; }
}
