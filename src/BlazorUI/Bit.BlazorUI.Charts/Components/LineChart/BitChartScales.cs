using Newtonsoft.Json;

namespace Bit.BlazorUI;

/// <summary>
/// Defines the scales for cartesian charts by holding the x and y axes.
/// </summary>
public class BitChartScales
{
    /// <summary>
    /// Gets or sets the configurations for the x-axes.
    /// </summary>
    [JsonProperty("xAxes")]
    public IList<BitChartCartesianAxis> XAxes { get; set; }

    /// <summary>
    /// Gets or sets the configurations for the y-axes.
    /// </summary>
    [JsonProperty("yAxes")]
    public IList<BitChartCartesianAxis> YAxes { get; set; }
}
