namespace Bit.BlazorUI;

/// <summary>Interaction options, mirroring Chart.js <c>options.interaction</c>.</summary>
public sealed class BitChartInteractionOptions
{
    public BitChartInteractionMode Mode { get; set; } = BitChartInteractionMode.Nearest;
    public bool Intersect { get; set; } = true;
}
