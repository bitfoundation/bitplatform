namespace Bit.BlazorUI;

/// <summary>Default element options, mirroring Chart.js <c>options.elements</c>.</summary>
public sealed class BitChartElementOptions
{
    public double PointRadius { get; set; } = 3;
    public double LineTension { get; set; }
    public double LineBorderWidth { get; set; } = 3;
    public double BarBorderWidth { get; set; }
    public double ArcBorderWidth { get; set; } = 2;
    public string ArcBorderColor { get; set; } = "#fff";
}
