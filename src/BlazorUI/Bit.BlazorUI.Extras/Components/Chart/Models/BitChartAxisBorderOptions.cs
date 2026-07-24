namespace Bit.BlazorUI;

/// <summary>Axis border line configuration, mirroring Chart.js <c>scale.border</c>.</summary>
public sealed class BitChartAxisBorderOptions
{
    public bool Display { get; set; } = true;
    public string Color { get; set; } = "var(--bit-clr-brd-pri, rgba(0,0,0,0.25))";
    public double Width { get; set; } = 1;
    public List<double>? Dash { get; set; }
}
