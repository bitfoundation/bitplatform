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
    /// <summary>
    /// The dataset a primitive (a line, an area fill, a radar web, a data label) draws, so it can be dimmed with the rest
    /// of its dataset when the legend highlights another one. Null for what belongs to no one dataset.
    /// </summary>
    public int? DatasetIndex { get; set; }
    /// <summary>The data index a primitive stands for - a data label - so a slice legend can fade it with its slice.</summary>
    public int? DataIndex { get; set; }
}
