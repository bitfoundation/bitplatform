
namespace Bit.BlazorUI;

public sealed class BitChartTitleModel
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-Chart-title-color, var(--bit-clr-fg-pri))";
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public BitChartFont Font { get; set; } = new();
    public BitChartPadding Padding { get; set; } = 10;
}
