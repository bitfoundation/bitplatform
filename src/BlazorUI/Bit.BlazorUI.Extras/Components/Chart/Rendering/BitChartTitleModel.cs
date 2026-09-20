
namespace Bit.BlazorUI;

public sealed class BitChartTitleModel
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-clr-fg-pri, #1A1A1A)";
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public BitChartFont Font { get; set; } = new();
    public BitChartPadding Padding { get; set; } = 10;
}
