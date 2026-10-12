namespace Bit.BlazorUI;

/// <summary>Legend plugin options.</summary>
public sealed class BitChartLegendOptions
{
    public bool Display { get; set; } = true;
    /// <summary>
    /// Which edge of the chart the legend is drawn against (default is the top).
    /// </summary>
    /// <remarks>
    /// Only Top, Bottom, Left, Right, Start and End are meaningful here. Left and Right stay on the side they are named
    /// for in both reading directions, while Start and End follow it: a legend at the End sits on the right of a
    /// left-to-right chart and on the left of a right-to-left one. Every other value leaves the legend at the top.
    /// </remarks>
    public BitPlacement Placement { get; set; } = BitPlacement.Top;
    /// <summary>
    /// Where the legend items line up along the edge the legend is drawn against (default is the center).
    /// </summary>
    /// <remarks>
    /// Start, Center and End are meaningful here, and Left and Right for a legend above or below the chart. Start
    /// and End follow the reading direction, while Left and Right stay on the same side of the screen in both; along
    /// a legend beside the chart, which runs top to bottom, Start and End are its top and its bottom. Every other value
    /// centers the items.
    /// </remarks>
    public BitPlacement Align { get; set; } = BitPlacement.Center;
    public bool Reverse { get; set; }
    /// <summary>Allow clicking a legend item to toggle dataset/data visibility.</summary>
    public bool OnClickToggle { get; set; } = true;
    /// <summary>
    /// Brings a series forward while its legend item is hovered or focused, by fading every other one - the series,
    /// or the slice of a pie, doughnut or polar-area chart. How far the others fade is the public
    /// <c>--bit-Chart-inactive-opacity</c> custom property.
    /// </summary>
    public bool HighlightOnHover { get; set; } = true;
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
