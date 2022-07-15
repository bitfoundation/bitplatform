using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// A generator for legend labels.
/// </summary>
/// <param name="chart">The chart for which to generate the labels.</param>
/// <returns>The <see cref="LegendItem"/>s the chart should display.</returns>
public delegate ICollection<LegendItem> LegendLabelsGenerator(JObject chart);
