
namespace Bit.BlazorUI;

public sealed class BitChartTooltipItem
{
    public string Color { get; set; } = "#000";
    public string Text { get; set; } = "";
    /// <summary>Optional point style for the tooltip swatch (when tooltip usePointStyle is enabled).</summary>
    public BitChartPointStyle? PointStyle { get; set; }
}
