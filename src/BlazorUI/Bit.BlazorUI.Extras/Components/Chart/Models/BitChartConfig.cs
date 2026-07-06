namespace Bit.BlazorUI;

/// <summary>
/// The top-level chart configuration, mirroring the object you pass to <c>new Chart(ctx, config)</c>
/// in Chart.js: a <see cref="BitChartType"/>, <see cref="BitChartData"/> and <see cref="BitChartOptions"/>.
/// </summary>
public sealed class BitChartConfig
{
    public BitChartType Type { get; set; } = BitChartType.Line;
    public BitChartData Data { get; set; } = new();
    public BitChartOptions Options { get; set; } = new();

    public BitChartConfig() { }

    public BitChartConfig(BitChartType type, BitChartData data, BitChartOptions? options = null)
    {
        Type = type;
        Data = data;
        Options = options ?? new BitChartOptions();
    }
}
