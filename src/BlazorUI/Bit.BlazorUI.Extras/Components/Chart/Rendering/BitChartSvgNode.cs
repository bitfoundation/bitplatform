namespace Bit.BlazorUI;

/// <summary>Base class for renderable SVG primitives produced by the renderer.</summary>
public abstract class BitChartSvgNode
{
    public string? Title { get; set; }
    public double Opacity { get; set; } = 1;
    public string? CssClass { get; set; }
}
