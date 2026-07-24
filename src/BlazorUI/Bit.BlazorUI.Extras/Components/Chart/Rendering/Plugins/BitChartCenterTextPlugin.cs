
namespace Bit.BlazorUI;

/// <summary>
/// Draws one or more lines of text in the center of a doughnut/pie chart - a very common
/// dashboard pattern (e.g. a total or KPI in the cutout).
/// </summary>
public sealed class BitChartCenterTextPlugin : IBitChartPlugin
{
    public string Id => "centerText";

    /// <summary>The main (largest) line of text.</summary>
    public string Text { get; set; } = "";
    /// <summary>Optional secondary line shown beneath the main text.</summary>
    public string? Subtext { get; set; }
    public string Color { get; set; } = "#333";
    public string? SubtextColor { get; set; }
    public BitChartFont Font { get; set; } = new() { Size = 28, Weight = "bold" };
    public BitChartFont SubtextFont { get; set; } = new() { Size = 13 };
    /// <summary>When true (default) the text is sized to fit the cutout.</summary>
    public bool FitToCutout { get; set; } = true;

    public BitChartCenterTextPlugin() { }
    public BitChartCenterTextPlugin(string text, string? subtext = null)
    {
        Text = text;
        Subtext = subtext;
    }

    public void AfterDatasetsDraw(BitChartPluginContext ctx)
    {
        if (ctx.IsCartesian || string.IsNullOrEmpty(Text)) return;

        double size = Font.Size;
        if (FitToCutout && ctx.InnerRadius > 0)
        {
            double avail = ctx.InnerRadius * 1.7;
            double w = BitChartTextMeasure.Width(Text, size, Font.Weight);
            if (w > avail && w > 0) size *= avail / w;
        }

        bool hasSub = !string.IsNullOrEmpty(Subtext);
        double cy = ctx.CenterY + (hasSub ? -size * 0.25 : 0);

        ctx.AddFront(new BitChartSvgText
        {
            X = ctx.CenterX, Y = cy, Text = Text, Fill = Color,
            FontFamily = Font.Family, FontSize = size, FontWeight = Font.Weight,
            Anchor = "middle", Baseline = "central"
        });

        if (hasSub)
            ctx.AddFront(new BitChartSvgText
            {
                X = ctx.CenterX, Y = cy + size * 0.75, Text = Subtext!, Fill = SubtextColor ?? Color,
                FontFamily = SubtextFont.Family, FontSize = SubtextFont.Size, FontWeight = SubtextFont.Weight,
                Anchor = "middle", Baseline = "central"
            });
    }
}
