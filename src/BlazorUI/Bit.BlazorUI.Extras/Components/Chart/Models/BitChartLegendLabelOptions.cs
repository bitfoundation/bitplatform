namespace Bit.BlazorUI;

/// <summary>Legend label configuration.</summary>
public sealed class BitChartLegendLabelOptions
{
    public string Color { get; set; } = "var(--bit-clr-fg-sec, #666)";
    public BitChartFont Font { get; set; } = new();
    public double BoxWidth { get; set; } = 40;
    public double BoxHeight { get; set; } = 12;
    public double Padding { get; set; } = 10;
    public bool UsePointStyle { get; set; }
    public BitChartPointStyle PointStyle { get; set; } = BitChartPointStyle.Circle;
}
