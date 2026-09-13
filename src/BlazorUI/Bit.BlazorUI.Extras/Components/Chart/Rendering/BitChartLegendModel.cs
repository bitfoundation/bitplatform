
namespace Bit.BlazorUI;

public sealed class BitChartLegendModel
{
    public List<BitChartLegendItemModel> Items { get; set; } = new();
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public BitChartLegendLabelOptions Labels { get; set; } = new();
    public string? Title { get; set; }
    public bool OnClickToggle { get; set; } = true;
}
