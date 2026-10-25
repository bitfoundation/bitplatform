
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
    /// Where this element stands along the index axis of a cartesian chart, in chart pixels. An element drawn at an
    /// index stands at that index's place on the primary index axis - its tick - however it is drawn: a bar in its
    /// slot beside it, or a point against a secondary x axis - so every element drawn at one index shares the place.
    /// A point placed by its own x stands where that x is. The crosshair, the hit bands and the Average tooltip
    /// positioner all go by it.
    /// </summary>
    internal double IndexPosition { get; init; }
    /// <summary>What the index axis reads at <see cref="IndexPosition"/>: the category there, or the value the way the axis spells it.</summary>
    internal string? IndexLabel { get; init; }
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
