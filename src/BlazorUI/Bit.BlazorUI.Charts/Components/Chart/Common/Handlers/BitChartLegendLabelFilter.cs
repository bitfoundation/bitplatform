using Newtonsoft.Json.Linq;

namespace Bit.BlazorUI;

/// <summary>
/// A filter for legend items. Should return <see langword="true"/> if you want to show the legend item;
/// <see langword="false"/> if not.
/// </summary>
/// <param name="legendItem">The <see cref="BitChartLegendItem"/> to either include or filter out.</param>
/// <param name="chartData">The chart data. This object is large so consider applying a
/// <see cref="Interop.IgnoreCallbackValueAttribute"/> if you don't use the value.</param>
/// <returns><see langword="true"/> if you want to show the legend item; <see langword="false"/> if not.</returns>
public delegate bool BitChartLegendLabelFilter(BitChartLegendItem legendItem, JObject chartData);
