namespace Bit.BlazorUI;

/// <summary>Context passed to a custom tooltip template (<c>TooltipTemplate</c>).</summary>
public sealed class BitChartTooltipContext
{
    public string? Title { get; init; }
    public IReadOnlyList<BitChartTooltipPoint> Points { get; init; } = Array.Empty<BitChartTooltipPoint>();
}
