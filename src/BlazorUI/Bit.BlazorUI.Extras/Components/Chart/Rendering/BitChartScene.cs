
namespace Bit.BlazorUI;

/// <summary>The full computed scene the component renders.</summary>
public sealed class BitChartScene
{
    public double Width { get; set; }
    public double Height { get; set; }

    public List<BitChartSvgNode> Background { get; } = new();
    /// <summary>Series paths (lines + area fills) drawn above the grid and below the data points.
    /// Rendered in an animated group so every line — solid, dashed or per-segment — animates uniformly.</summary>
    public List<BitChartSvgNode> Series { get; } = new();
    public List<BitChartDataElement> Elements { get; } = new();
    public List<BitChartSvgNode> Foreground { get; } = new();

    /// <summary>Gradient definitions referenced via url(#id).</summary>
    public List<BitChartGradientDef> Defs { get; } = new();

    /// <summary>Pattern definitions referenced via url(#id).</summary>
    public List<BitChartPatternDef> Patterns { get; } = new();

    public BitChartLegendModel? Legend { get; set; }
    public BitChartTitleModel? Title { get; set; }
    public BitChartTitleModel? Subtitle { get; set; }

    /// <summary>The cartesian plotting area (null for circular/radar charts).</summary>
    public BitChartArea? PlotArea { get; set; }
    /// <summary>True for pie/doughnut/polar/radar charts.</summary>
    public bool IsRadialOrCircular { get; set; }
    /// <summary>True when the cartesian chart contains bar datasets.</summary>
    public bool HasBars { get; set; }
    /// <summary>True when bars are horizontal (indexAxis = y).</summary>
    public bool HorizontalBars { get; set; }
    /// <summary>True when the line series should animate with a progressive draw-on (stroke reveal,
    /// left to right) rather than the default group rise. Set by the renderer for line/area charts
    /// when <see cref="BitChartAnimationOptions.Progressive"/> is enabled.</summary>
    public bool ProgressiveDraw { get; set; }
    /// <summary>The value-axis baseline pixel the bars grow from (y for vertical bars, x for
    /// horizontal bars). Used as the transform-origin for the bar entry animation so they scale
    /// out of the axis line rather than the edge of the SVG.</summary>
    public double BarBaseline { get; set; }

    /// <summary>Effective value range per axis id after data/zoom resolution.</summary>
    public Dictionary<string, (double Min, double Max)> AxisRanges { get; } = new();
    /// <summary>Axis ids that support zoom/pan (linear/time/logarithmic).</summary>
    public HashSet<string> ZoomableAxes { get; } = new();
}
