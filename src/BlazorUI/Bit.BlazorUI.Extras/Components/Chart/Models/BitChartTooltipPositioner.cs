namespace Bit.BlazorUI;

/// <summary>Where the tooltip is anchored relative to the active items.</summary>
public enum BitChartTooltipPositioner
{
    /// <summary>Anchor at the nearest (hovered) item.</summary>
    Nearest,
    /// <summary>Anchor at the average position of all active items.</summary>
    Average
}
