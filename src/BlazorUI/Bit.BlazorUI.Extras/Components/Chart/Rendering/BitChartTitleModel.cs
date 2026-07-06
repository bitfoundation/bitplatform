
namespace Bit.BlazorUI;

public sealed class BitChartTitleModel
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#333";
    public BitChartPosition Position { get; set; } = BitChartPosition.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public BitChartFont Font { get; set; } = new();
}
