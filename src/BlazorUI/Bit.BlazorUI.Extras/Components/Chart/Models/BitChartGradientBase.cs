namespace Bit.BlazorUI;

/// <summary>Base type for gradient fills (linear or radial).</summary>
public abstract class BitChartGradientBase
{
    public List<BitChartGradientStop> Stops { get; set; } = new();
}
