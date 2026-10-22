namespace Bit.BlazorUI;

public sealed class BitChartSvgText : BitChartSvgNode
{
    public double X, Y;
    public string Text = "";
    public string Fill = "#000";
    public string FontFamily = "sans-serif";
    public double FontSize = 12;
    public string FontWeight = "normal";
    public string FontStyle = "normal";
    /// <summary>start | middle | end</summary>
    public string Anchor = "start";
    /// <summary>
    /// Reads <see cref="Anchor"/> left to right in either reading direction - start the left edge of the text, end its
    /// right one - for a label placed on a side of the screen, the way the values of an axis are. Off, start and end
    /// follow the reading direction, as they do in SVG. The text itself reads in the chart's direction either way.
    /// </summary>
    public bool PhysicalAnchor;
    /// <summary>auto | middle | hanging | central</summary>
    public string Baseline = "auto";
    public double Rotation;
}
