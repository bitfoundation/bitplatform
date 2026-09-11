namespace Bit.BlazorUI;

/// <summary>Legend plugin options.</summary>
public sealed class BitChartLegendOptions
{
    public bool Display { get; set; } = true;
    /// <summary>
    /// Which edge of the chart the legend is drawn against (default is the top).
    /// </summary>
    /// <remarks>
    /// A chart is laid out physically, so only Top, Bottom, Left and Right are meaningful here; any other side
    /// leaves the legend at the top.
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
    public BitChartLegendLabelOptions Labels { get; set; } = new();
    public string? Title { get; set; }
}
