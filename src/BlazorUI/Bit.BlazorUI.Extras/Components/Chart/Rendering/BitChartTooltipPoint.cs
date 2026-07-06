namespace Bit.BlazorUI;

/// <summary>A single item exposed to a custom tooltip template.</summary>
public sealed class BitChartTooltipPoint
{
    public int DatasetIndex { get; init; }
    public int DataIndex { get; init; }
    public string? Label { get; init; }
    public double Value { get; init; }
    public string Color { get; init; } = "#000";
    public string FormattedValue { get; init; } = "";
}
