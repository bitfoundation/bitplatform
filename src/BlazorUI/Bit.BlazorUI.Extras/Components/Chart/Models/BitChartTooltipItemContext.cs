namespace Bit.BlazorUI;

/// <summary>
/// A single tooltip item exposed to tooltip callbacks, mirroring Chart.js <c>BitChartTooltipItem</c>.
/// </summary>
public sealed class BitChartTooltipItemContext
{
    public int DatasetIndex { get; init; }
    public int DataIndex { get; init; }
    /// <summary>The dataset label.</summary>
    public string? DatasetLabel { get; init; }
    /// <summary>The category/point label.</summary>
    public string? Label { get; init; }
    /// <summary>The parsed primary (y) value.</summary>
    public double Value { get; init; }
    /// <summary>The parsed x value for point datasets, else null.</summary>
    public double? ValueX { get; init; }
    /// <summary>The dataset color (for the legend swatch).</summary>
    public string Color { get; init; } = "#000";
    /// <summary>The default formatted value string.</summary>
    public string FormattedValue { get; init; } = "";
}
