namespace Bit.BlazorUI;

/// <summary>
/// A scale/axis definition, mirroring Chart.js cartesian and radial scale options.
/// </summary>
public sealed class BitChartScaleOptions
{
    public string Id { get; set; } = "";
    public BitChartScaleType Type { get; set; } = BitChartScaleType.Linear;
    public bool Display { get; set; } = true;
    public BitChartPosition? Position { get; set; }

    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? SuggestedMin { get; set; }
    public double? SuggestedMax { get; set; }
    /// <summary>Extra padding added to the data range, as a fraction of the range (e.g. 0.1 = 10%).</summary>
    public double Grace { get; set; }
    public bool BeginAtZero { get; set; }
    public bool Reverse { get; set; }
    public bool Stacked { get; set; }
    /// <summary>When stacked, normalize each category to 100% (percentage stack).</summary>
    public bool Stacked100 { get; set; }

    /// <summary>BitChartPadding (fraction of a step) applied at the ends of a category axis.</summary>
    public bool Offset { get; set; }

    public BitChartGridOptions Grid { get; set; } = new();
    public BitChartTickOptions Ticks { get; set; } = new();
    public BitChartScaleTitleOptions Title { get; set; } = new();
    /// <summary>The axis border line (drawn at the axis edge).</summary>
    public BitChartAxisBorderOptions Border { get; set; } = new();

    /// <summary>Angle lines configuration (radial scales only).</summary>
    public bool AngleLines { get; set; } = true;
    public string AngleLineColor { get; set; } = "var(--bit-clr-brd-sec, rgba(0,0,0,0.1))";
    public double AngleLineWidth { get; set; } = 1;
    public List<double>? AngleLineDash { get; set; }
    /// <summary>Start angle in degrees for radial scales.</summary>
    public double StartAngle { get; set; }
    /// <summary>Point (category) labels around a radial scale.</summary>
    public BitChartPointLabelOptions PointLabels { get; set; } = new();
    /// <summary>Show a filled backdrop behind radial tick labels.</summary>
    public bool ShowLabelBackdrop { get; set; } = true;
    /// <summary>Backdrop color for radial tick labels.</summary>
    public string BackdropColor { get; set; } = "rgba(255,255,255,0.75)";

    // ---- Time scale ----
    /// <summary>The unit for a time axis. Auto picks a sensible unit from the data range.</summary>
    public BitChartTimeUnit TimeUnit { get; set; } = BitChartTimeUnit.Auto;
    /// <summary>Optional custom date formatter for time-axis tick labels.</summary>
    public Func<DateTime, string>? TimeFormat { get; set; }
}
