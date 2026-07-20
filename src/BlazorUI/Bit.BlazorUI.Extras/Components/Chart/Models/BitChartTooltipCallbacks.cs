namespace Bit.BlazorUI;

/// <summary>
/// Tooltip text/styling callbacks, mirroring Chart.js <c>tooltip.callbacks</c>. Any callback
/// returning null falls back to the default behavior. Multi-line results may use '\n'.
/// </summary>
public sealed class BitChartTooltipCallbacks
{
    /// <summary>Builds the tooltip title from the active items.</summary>
    public Func<IReadOnlyList<BitChartTooltipItemContext>, string?>? Title { get; set; }
    /// <summary>Lines rendered before the body items.</summary>
    public Func<IReadOnlyList<BitChartTooltipItemContext>, string?>? BeforeBody { get; set; }
    /// <summary>Builds the body line for a single item (replaces the default "label: value").</summary>
    public Func<BitChartTooltipItemContext, string?>? Label { get; set; }
    /// <summary>Overrides the color swatch shown next to a body line.</summary>
    public Func<BitChartTooltipItemContext, string?>? LabelColor { get; set; }
    /// <summary>Lines rendered after the body items.</summary>
    public Func<IReadOnlyList<BitChartTooltipItemContext>, string?>? AfterBody { get; set; }
    /// <summary>Footer lines rendered below the body.</summary>
    public Func<IReadOnlyList<BitChartTooltipItemContext>, string?>? Footer { get; set; }
}
