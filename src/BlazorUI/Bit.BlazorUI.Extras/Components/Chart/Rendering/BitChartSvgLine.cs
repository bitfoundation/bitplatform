namespace Bit.BlazorUI;

public sealed class BitChartSvgLine : BitChartSvgNode
{
    public double X1, Y1, X2, Y2;
    public string Stroke = "#000";
    public double StrokeWidth = 1;
    public string? Dash;
}
