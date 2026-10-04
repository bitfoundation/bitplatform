namespace Bit.BlazorUI;

/// <summary>
/// Defines per-part CSS class/style values for <see cref="BitChart"/>.
/// </summary>
public class BitChartClassStyles
{
    /// <summary>
    /// Custom class or style applied to the root element.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom class or style applied to the title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom class or style applied to the subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Custom class or style applied to the legend container.
    /// </summary>
    public string? Legend { get; set; }

    /// <summary>
    /// Custom class or style applied to each legend item.
    /// </summary>
    public string? LegendItem { get; set; }

    /// <summary>
    /// Custom class or style applied to the plot container that holds the SVG, the tooltip and the empty state.
    /// </summary>
    public string? Plot { get; set; }

    /// <summary>
    /// Custom class or style applied to the tooltip box, the default one and a custom template's alike.
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Custom class or style applied to the empty state shown when there is nothing to draw.
    /// </summary>
    public string? NoData { get; set; }

    /// <summary>
    /// Custom class or style applied to the loading state shown over the plot while the chart is loading.
    /// </summary>
    public string? Loading { get; set; }
}
