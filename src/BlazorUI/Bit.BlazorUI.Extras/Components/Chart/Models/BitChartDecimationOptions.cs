namespace Bit.BlazorUI;

/// <summary>BitChartDecimation (downsampling) options for large line datasets.</summary>
public sealed class BitChartDecimationOptions
{
    public bool Enabled { get; set; }
    /// <summary>Target number of points to keep (LTTB).</summary>
    public int Samples { get; set; } = 200;
    /// <summary>Only decimate when the dataset exceeds this many points.</summary>
    public int Threshold { get; set; } = 500;
}
