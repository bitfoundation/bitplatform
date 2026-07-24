
namespace Bit.BlazorUI;

public sealed class BitChartLegendModel
{
    public List<BitChartLegendItemModel> Items { get; set; } = new();
    public BitChartPosition Position { get; set; } = BitChartPosition.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public BitChartLegendLabelOptions Labels { get; set; } = new();
    public string? Title { get; set; }
    public bool OnClickToggle { get; set; } = true;
}
