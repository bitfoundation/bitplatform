namespace Bit.BlazorUI;

public sealed class BitChartSvgPath : BitChartSvgNode
{
    public string D = "";
    public string Fill = "none";
    public string? Stroke;
    public double StrokeWidth = 0;
    public string? Dash;
    public string LineCap = "butt";
    public string LineJoin = "miter";
    /// <summary>When true the path animates a "draw-on" effect via stroke-dashoffset.</summary>
    public bool AnimateDraw;
    /// <summary>When true the path fades in (used where draw-on is unavailable, e.g. dashed/segmented lines).</summary>
    public bool AnimateFade;
}
