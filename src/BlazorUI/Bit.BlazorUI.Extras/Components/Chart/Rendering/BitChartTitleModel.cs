
namespace Bit.BlazorUI;

public sealed class BitChartTitleModel
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#333";
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public BitChartFont Font { get; set; } = new();
}
