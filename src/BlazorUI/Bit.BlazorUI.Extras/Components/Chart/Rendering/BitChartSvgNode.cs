namespace Bit.BlazorUI;

/// <summary>Base class for renderable SVG primitives produced by the renderer.</summary>
public abstract class BitChartSvgNode
{
    /// <summary>Native SVG tooltip text, rendered as a child &lt;title&gt; element.</summary>
    public string? Title { get; set; }
    public double Opacity { get; set; } = 1;
    public string? CssClass { get; set; }
    /// <summary>Optional SVG transform applied to the primitive (e.g. a marker rotation).</summary>
    public string? Transform { get; set; }
}
