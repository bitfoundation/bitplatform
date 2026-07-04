
namespace Bit.BlazorUI;

/// <summary>Tooltip payload attached to an interactive data element.</summary>
public sealed class BitChartTooltipInfo
{
    public string? Title { get; set; }
    public List<BitChartTooltipItem> Items { get; set; } = new();
    /// <summary>Lines rendered before the body (from the BeforeBody callback).</summary>
    public List<string> BeforeBody { get; set; } = new();
    /// <summary>Lines rendered after the body (from the AfterBody callback).</summary>
    public List<string> AfterBody { get; set; } = new();
    /// <summary>Footer lines (from the Footer callback).</summary>
    public List<string> Footer { get; set; } = new();
    /// <summary>Anchor position (in chart pixel coordinates) the tooltip points at.</summary>
    public double AnchorX { get; set; }
    public double AnchorY { get; set; }
}
