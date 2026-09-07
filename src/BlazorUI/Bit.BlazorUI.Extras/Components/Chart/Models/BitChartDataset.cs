namespace Bit.BlazorUI;

/// <summary>
/// A dataset, mirroring Chart.js dataset configuration. A dataset can carry either a
/// simple list of values (<see cref="Data"/>) for value based charts, or a list of
/// <see cref="BitChartDataPoint"/> (<see cref="Points"/>) for scatter/bubble charts.
/// </summary>
public sealed class BitChartDataset
{
    /// <summary>Dataset label shown in legends and tooltips.</summary>
    public string? Label { get; set; }

    /// <summary>Per-index values (line, bar, radar, pie, doughnut, polarArea).</summary>
    public List<double?> Data { get; set; } = new();

    /// <summary>Floating-bar ranges [low, high] per index. When set, bars span low→high.</summary>
    public List<(double Low, double High)?>? RangeData { get; set; }

    /// <summary>Point data for scatter/bubble charts. When set, takes precedence over <see cref="Data"/>.</summary>
    public List<BitChartDataPoint>? Points { get; set; }

    /// <summary>Optional per-dataset type override for mixed charts.</summary>
    public BitChartType? Type { get; set; }

    // ---- Colors (single value applies to the whole dataset; list applies per data index) ----
    public string? BackgroundColor { get; set; }
    public List<string>? BackgroundColors { get; set; }
    /// <summary>A repeating pattern fill, used by bars and area fills (overrides solid color).</summary>
    public BitChartFillPattern? BackgroundPattern { get; set; }
    public string? BorderColor { get; set; }
    public List<string>? BorderColors { get; set; }

    /// <summary>
    /// Border/line thickness. When null a per-type default applies: <see cref="BitChartElementOptions.LineBorderWidth"/>
    /// for lines and radar, <see cref="BitChartElementOptions.ArcBorderWidth"/> for arcs, and for bars
    /// <see cref="BitChartElementOptions.BarBorderWidth"/> unless a border color was supplied (then 1).
    /// </summary>
    public double? BorderWidth { get; set; }

    /// <summary>Fill color used while the element is hovered (bars and arcs).</summary>
    public string? HoverBackgroundColor { get; set; }
    /// <summary>Border color used while the element is hovered (bars and arcs).</summary>
    public string? HoverBorderColor { get; set; }
    /// <summary>Border width used while the element is hovered (bars and arcs).</summary>
    public double? HoverBorderWidth { get; set; }
    /// <summary>Extra pixels a hovered arc is pushed out from the center (pie/doughnut/polar area).</summary>
    public double HoverOffset { get; set; } = 6;

    // ---- BitChartScriptable options (evaluated per element; take precedence over the constants above) ----
    /// <summary>BitChartScriptable background color: <c>ctx => color</c>.</summary>
    public Func<BitChartScriptableContext, string?>? BackgroundColorFn { get; set; }
    /// <summary>BitChartScriptable border color: <c>ctx => color</c>.</summary>
    public Func<BitChartScriptableContext, string?>? BorderColorFn { get; set; }
    /// <summary>BitChartScriptable border width: <c>ctx => width</c>.</summary>
    public Func<BitChartScriptableContext, double?>? BorderWidthFn { get; set; }
    /// <summary>BitChartScriptable point radius: <c>ctx => radius</c>.</summary>
    public Func<BitChartScriptableContext, double?>? PointRadiusFn { get; set; }
    /// <summary>BitChartScriptable point background color.</summary>
    public Func<BitChartScriptableContext, string?>? PointBackgroundColorFn { get; set; }
    /// <summary>BitChartScriptable point border color.</summary>
    public Func<BitChartScriptableContext, string?>? PointBorderColorFn { get; set; }
    /// <summary>BitChartScriptable point style.</summary>
    public Func<BitChartScriptableContext, BitChartPointStyle?>? PointStyleFn { get; set; }

    // ---- Line / radar element options ----
    public BitChartFillMode Fill { get; set; } = BitChartFillMode.None;
    public string? FillColor { get; set; }
    /// <summary>Optional gradient (linear or radial) used for the area fill (takes precedence over <see cref="FillColor"/>).</summary>
    public BitChartGradientBase? FillGradient { get; set; }
    /// <summary>Target dataset index when <see cref="Fill"/> is <see cref="BitChartFillMode.Dataset"/>.</summary>
    public int? FillTargetIndex { get; set; }
    /// <summary>Bezier curve tension (0 = straight lines). Falls back to <see cref="BitChartElementOptions.LineTension"/> when unset.</summary>
    public double? Tension { get; set; }
    /// <summary>Cubic interpolation mode. <see cref="BitChartCubicInterpolationMode.Monotone"/> avoids overshoot.</summary>
    public BitChartCubicInterpolationMode CubicInterpolationMode { get; set; } = BitChartCubicInterpolationMode.Default;
    public BitChartSteppedLine Stepped { get; set; } = BitChartSteppedLine.False;
    public List<double>? BorderDash { get; set; }
    /// <summary>Offset (px) of the first dash in <see cref="BorderDash"/>.</summary>
    public double BorderDashOffset { get; set; }
    public string BorderJoinStyle { get; set; } = "round";
    public string BorderCapStyle { get; set; } = "round";
    public bool ShowLine { get; set; } = true;
    public bool SpanGaps { get; set; }
    /// <summary>Optional per-segment styling callbacks (color/width/dash per line segment).</summary>
    public BitChartLineSegmentStyle? Segment { get; set; }
    /// <summary>When <see cref="Fill"/> is <see cref="BitChartFillMode.Value"/>, the absolute axis value to fill to.</summary>
    public double? FillValue { get; set; }

    // ---- Point element options ----
    /// <summary>Marker radius. Zero hides the marker but keeps the point hoverable; null falls back to
    /// <see cref="BitChartElementOptions.PointRadius"/>.</summary>
    public double? PointRadius { get; set; }
    public double PointHoverRadius { get; set; } = 4;
    public double PointBorderWidth { get; set; } = 1;
    public string? PointBackgroundColor { get; set; }
    public string? PointBorderColor { get; set; }
    public BitChartPointStyle PointStyle { get; set; } = BitChartPointStyle.Circle;
    /// <summary>Rotation of the point marker in degrees.</summary>
    public double PointRotation { get; set; }
    /// <summary>Extra pixel radius around a point that still counts as a hit for hover/tooltip.</summary>
    public double HitRadius { get; set; } = 1;
    /// <summary>Point fill color when hovered (falls back to <see cref="PointBackgroundColor"/>).</summary>
    public string? PointHoverBackgroundColor { get; set; }
    /// <summary>Point border color when hovered.</summary>
    public string? PointHoverBorderColor { get; set; }
    /// <summary>Point border width when hovered.</summary>
    public double? PointHoverBorderWidth { get; set; }

    // ---- Bar element options ----
    public double? BarThickness { get; set; }
    public double? MaxBarThickness { get; set; }
    public double BarPercentage { get; set; } = 0.9;
    public double CategoryPercentage { get; set; } = 0.8;
    public double BorderRadius { get; set; }
    /// <summary>Optional per-corner bar radius. When set, overrides <see cref="BorderRadius"/>.</summary>
    public BitChartBorderRadiusCorners? BorderRadiusCorners { get; set; }
    /// <summary>Pixels to grow each bar by to avoid anti-aliasing gaps between stacked bars.</summary>
    public double? InflateAmount { get; set; }
    /// <summary>Minimum bar length in pixels, so very small values stay visible.</summary>
    public double? MinBarLength { get; set; }
    /// <summary>The value bars start from. Defaults to zero clamped into the axis range.</summary>
    public double? Base { get; set; }
    /// <summary>When false the dataset is not grouped with the other bar datasets and keeps the full band.</summary>
    public bool Grouped { get; set; } = true;
    /// <summary>When true, null values leave no gap: the remaining bars in the group expand to fill the band.</summary>
    public bool SkipNull { get; set; }
    /// <summary>Which bar edge omits its border. Default skips the baseline edge.</summary>
    public BitChartBorderSkipped BorderSkipped { get; set; } = BitChartBorderSkipped.Start;

    // ---- Arc (pie/doughnut/polar) element options ----
    /// <summary>Pixels every arc of this dataset is pushed out from the center.</summary>
    public double Offset { get; set; }
    /// <summary>Angular gap (in pixels along the outer edge) left between neighbouring arcs.</summary>
    public double SpacingArc { get; set; }

    // ---- Axis assignment / stacking / ordering ----
    public string XAxisID { get; set; } = "x";
    public string YAxisID { get; set; } = "y";
    public string RAxisID { get; set; } = "r";
    public string? Stack { get; set; }
    public int Order { get; set; }
    public bool Hidden { get; set; }

    /// <summary>Number of logical data items in this dataset.</summary>
    public int Count => Points?.Count ?? RangeData?.Count ?? Data.Count;
}
