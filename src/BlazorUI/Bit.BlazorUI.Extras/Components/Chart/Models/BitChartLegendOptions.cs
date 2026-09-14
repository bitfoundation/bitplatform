namespace Bit.BlazorUI;

/// <summary>Legend plugin options.</summary>
public sealed class BitChartLegendOptions
{
    public bool Display { get; set; } = true;
    public BitChartPosition Position { get; set; } = BitChartPosition.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public bool Reverse { get; set; }
    /// <summary>Allow clicking a legend item to toggle dataset/data visibility.</summary>
    public bool OnClickToggle { get; set; } = true;
    public BitChartLegendLabelOptions Labels { get; set; } = new();
    public string? Title { get; set; }

    /// <summary>
    /// Caps the legend's height in pixels and lets it scroll past that. A chart of twenty series would
    /// otherwise give most of its box to the legend; with a cap the plot keeps its space and the
    /// remaining entries stay one scroll away rather than pushed off the chart.
    /// </summary>
    public double? MaxHeight { get; set; }
    /// <summary>Keeps only the items this predicate accepts, mirroring Chart.js <c>legend.labels.filter</c>.</summary>
    public Func<BitChartLegendItemModel, bool>? Filter { get; set; }
}
