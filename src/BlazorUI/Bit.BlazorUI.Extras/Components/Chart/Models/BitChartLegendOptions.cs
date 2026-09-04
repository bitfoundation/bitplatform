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
    public BitSide Position { get; set; } = BitSide.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public bool Reverse { get; set; }
    /// <summary>Allow clicking a legend item to toggle dataset/data visibility.</summary>
    public bool OnClickToggle { get; set; } = true;
    public BitChartLegendLabelOptions Labels { get; set; } = new();
    public string? Title { get; set; }
}
