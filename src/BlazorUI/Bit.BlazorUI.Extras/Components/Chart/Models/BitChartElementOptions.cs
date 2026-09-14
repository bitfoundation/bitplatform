namespace Bit.BlazorUI;

/// <summary>
/// Default element options, mirroring Chart.js <c>options.elements</c>. Every value here is the
/// fallback used when the matching dataset property is left unset.
/// </summary>
public sealed class BitChartElementOptions
{
    /// <summary>Default point radius for datasets that do not set <see cref="BitChartDataset.PointRadius"/>.</summary>
    public double PointRadius { get; set; } = 3;
    /// <summary>Default bezier tension for datasets that do not set <see cref="BitChartDataset.Tension"/>.</summary>
    public double LineTension { get; set; }
    /// <summary>Default line thickness for line and radar datasets.</summary>
    public double LineBorderWidth { get; set; } = 3;
    /// <summary>Default bar border thickness (Chart.js draws no bar border by default).</summary>
    public double BarBorderWidth { get; set; }
    /// <summary>Default arc border thickness for pie/doughnut/polar area.</summary>
    public double ArcBorderWidth { get; set; } = 2;
    /// <summary>Default arc border color; follows the theme background so arcs separate cleanly.</summary>
    public string ArcBorderColor { get; set; } = "var(--bit-clr-bg-pri, #fff)";
}
