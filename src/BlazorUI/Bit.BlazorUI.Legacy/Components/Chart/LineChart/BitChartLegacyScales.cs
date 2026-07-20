using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Defines the scales for cartesian charts by holding the x and y axes.
/// </summary>
public class BitChartLegacyScales
{
    /// <summary>
    /// Gets or sets the configurations for the x-axes.
    /// </summary>
    [JsonProperty("xAxes")]
    public List<BitChartLegacyCartesianAxis>? XAxes { get; set; }

    /// <summary>
    /// Gets or sets the configurations for the y-axes.
    /// </summary>
    [JsonProperty("yAxes")]
    public List<BitChartLegacyCartesianAxis>? YAxes { get; set; }
}
