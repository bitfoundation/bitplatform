namespace Bit.BlazorUI;

/// <summary>The chart data, mirroring Chart.js <c>data</c>: labels + datasets.</summary>
public sealed class BitChartData
{
    public List<string> Labels { get; set; } = new();
    public List<BitChartDataset> Datasets { get; set; } = new();
}
