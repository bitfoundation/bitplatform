namespace Bit.BlazorUI;

/// <summary>Container for all plugin options, mirroring Chart.js <c>options.plugins</c>.</summary>
public sealed class BitChartPluginOptions
{
    public BitChartLegendOptions Legend { get; set; } = new();
    public BitChartTitleOptions Title { get; set; } = new();
    public BitChartTitleOptions Subtitle { get; set; } = new() { Font = new BitChartFont { Size = 13 } };
    public BitChartTooltipOptions Tooltip { get; set; } = new();
    public BitChartDataLabelOptions DataLabels { get; set; } = new();
    public BitChartDecimationOptions Decimation { get; set; } = new();

    /// <summary>User-registered drawing plugins (annotations, custom overlays, ...).</summary>
    public List<IBitChartPlugin> Custom { get; set; } = new();
}
