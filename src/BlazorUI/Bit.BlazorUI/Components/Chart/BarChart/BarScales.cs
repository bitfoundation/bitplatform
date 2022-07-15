using Newtonsoft.Json;

namespace Bit.BlazorUI;

/// <summary>
/// Defines the scales for bar charts by holding the x and y axes.
/// </summary>
public class BarScales
{
    /// <summary>
    /// Gets or sets the configurations for the x-axes.
    /// <para>You can use any <see cref="CartesianAxis"/> but there are extended axes in the <see cref="BarChart.Axes"/> namespace which contain additional properties to customize the bar chart axes.</para>
    /// </summary>
    [JsonProperty("xAxes")]
    public IList<CartesianAxis> XAxes { get; set; }

    /// <summary>
    /// Gets or sets the configurations for the y-axes.
    /// <para>You can use any <see cref="CartesianAxis"/> but there are extended axes in the <see cref="BarChart.Axes"/> namespace which contain additional properties to customize the bar chart axes.</para>
    /// </summary>
    [JsonProperty("yAxes")]
    public IList<CartesianAxis> YAxes { get; set; }
}
