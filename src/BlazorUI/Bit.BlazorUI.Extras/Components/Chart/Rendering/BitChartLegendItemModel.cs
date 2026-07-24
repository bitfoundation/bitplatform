
namespace Bit.BlazorUI;

/// <summary>A legend entry.</summary>
public sealed class BitChartLegendItemModel
{
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#000";
    public string? StrokeColor { get; set; }
    public bool Hidden { get; set; }
    public bool UsePointStyle { get; set; }
    public BitChartPointStyle PointStyle { get; set; }
    /// <summary>Dataset index, or for pie/doughnut/polar the data index.</summary>
    public int Index { get; set; }
    public bool IsDataIndex { get; set; }
}
