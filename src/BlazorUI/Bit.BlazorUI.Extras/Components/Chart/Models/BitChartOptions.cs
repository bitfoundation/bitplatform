namespace Bit.BlazorUI;

/// <summary>
/// Top-level chart options, mirroring Chart.js <c>options</c>.
/// </summary>
public sealed class BitChartOptions
{
    public bool Responsive { get; set; } = true;
    public bool MaintainAspectRatio { get; set; } = true;
    /// <summary>Canvas aspect ratio (width/height). When null, defaults to 2 for cartesian charts and 1 for circular/radar charts.</summary>
    public double? AspectRatio { get; set; }

    public BitChartIndexAxis IndexAxis { get; set; } = BitChartIndexAxis.X;

    public BitChartLayoutOptions Layout { get; set; } = new();
    public BitChartInteractionOptions Interaction { get; set; } = new();
    public BitChartAnimationOptions Animation { get; set; } = new();
    public BitChartElementOptions Elements { get; set; } = new();
    public BitChartPluginOptions Plugins { get; set; } = new();
    public BitChartZoomOptions Zoom { get; set; } = new();

    /// <summary>Named scales, keyed by id (e.g. "x", "y", "r", "y2"). Missing scales are created on the
    /// fly by the renderer without mutating this dictionary, so the same options instance can safely be
    /// shared between charts of different types.</summary>
    public Dictionary<string, BitChartScaleOptions> Scales { get; set; } = new();

    /// <summary>
    /// Culture used to format every number and date the chart renders (tick labels, tooltips and data
    /// labels). When null the invariant culture is used, so output stays stable regardless of the
    /// thread culture. Set it to <c>CultureInfo.CurrentCulture</c> to follow the user's locale.
    /// </summary>
    public System.Globalization.CultureInfo? Culture { get; set; }

    // ---- Doughnut / pie / polar specific ----
    /// <summary>Inner radius as a percentage string for doughnut charts (0-100).</summary>
    public double CutoutPercentage { get; set; } = 50;
    /// <summary>Sweep of the chart in degrees (default 360).</summary>
    public double CircumferenceDegrees { get; set; } = 360;
    /// <summary>Starting angle in degrees (Chart.js default -90 = top).</summary>
    public double RotationDegrees { get; set; } = -90;

    /// <summary>Gets the scale with the given id, creating a default if missing.</summary>
    public BitChartScaleOptions GetOrAddScale(string id, BitChartScaleType type)
    {
        if (!Scales.TryGetValue(id, out var s))
        {
            s = new BitChartScaleOptions { Id = id, Type = type };
            Scales[id] = s;
        }
        return s;
    }
}
