
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
    /// <summary>Optional shape used when this element is the active/hovered one.</summary>
    public BitChartSvgNode? HoverShape { get; init; }
    /// <summary>Optional secondary shape drawn with the element (e.g. a bar's skipped-edge border)
    /// so it animates together with the fill.</summary>
    public BitChartSvgNode? BorderShape { get; init; }
    /// <summary>Per-element entry animation CSS class (e.g. grow from baseline for bars, pop for points).</summary>
    public string? EnterAnim { get; init; }
    /// <summary>Transform origin (in view-box/pixel coordinates) for the entry animation — the bar baseline
    /// or the point center.</summary>
    public double AnimOriginX { get; init; }
    public double AnimOriginY { get; init; }
    /// <summary>Primary numeric value of this element (for tooltip templates).</summary>
    public double Value { get; init; }
    /// <summary>Series (dataset) label, for tooltip templates.</summary>
    public string? SeriesLabel { get; init; }
}
