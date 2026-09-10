namespace Bit.BlazorUI;

/// <summary>
/// Downsampling options for large line datasets. Decimation applies to unstacked lines and areas:
/// stacked ones are accumulated index by index across their datasets, so thinning them separately
/// would leave the layers no longer adding up.
/// </summary>
public sealed class BitChartDecimationOptions
{
    public bool Enabled { get; set; }
    /// <summary>Target number of points to keep (LTTB).</summary>
    public int Samples { get; set; } = 200;
    /// <summary>Only decimate when the dataset exceeds this many points.</summary>
    public int Threshold { get; set; } = 500;
}
