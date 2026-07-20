using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// A generator for legend labels.
/// </summary>
/// <param name="chart">The chart for which to generate the labels.</param>
/// <returns>The <see cref="BitChartLegacyLegendItem"/>s the chart should display.</returns>
public delegate ICollection<BitChartLegacyLegendItem> BitChartLegendLabelsGenerator(JObject chart);
