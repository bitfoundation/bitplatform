using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Defines the scales for bar charts by holding the x and y axes.
/// </summary>
public class BitChartLegacyBarScales
{
    /// <summary>
    /// Gets or sets the configurations for the x-axes.
    /// <para>You can use any <see cref="BitChartLegacyCartesianAxis"/> but there are extended axes which contain additional properties to customize the bar chart axes.</para>
    /// </summary>
    [JsonProperty("xAxes")]
    public List<BitChartLegacyCartesianAxis>? XAxes { get; set; }

    /// <summary>
    /// Gets or sets the configurations for the y-axes.
    /// <para>You can use any <see cref="BitChartLegacyCartesianAxis"/> but there are extended axes which contain additional properties to customize the bar chart axes.</para>
    /// </summary>
    [JsonProperty("yAxes")]
    public List<BitChartLegacyCartesianAxis>? YAxes { get; set; }
}
