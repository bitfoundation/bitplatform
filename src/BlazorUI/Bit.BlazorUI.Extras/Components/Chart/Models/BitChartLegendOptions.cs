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
}
