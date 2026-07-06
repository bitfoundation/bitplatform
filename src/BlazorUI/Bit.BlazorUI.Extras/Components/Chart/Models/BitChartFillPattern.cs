namespace Bit.BlazorUI;

/// <summary>A repeating SVG pattern fill (hatching, dots, grid, ...).</summary>
public sealed class BitChartFillPattern
{
    public BitChartPatternStyle Style { get; set; } = BitChartPatternStyle.DiagonalUp;
    public string Color { get; set; } = "#36a2eb";
    /// <summary>Background color behind the pattern (null = transparent).</summary>
    public string? Background { get; set; }
    /// <summary>Tile size in pixels.</summary>
    public double Size { get; set; } = 8;
    /// <summary>Stroke/dot thickness.</summary>
    public double LineWidth { get; set; } = 2;

    public BitChartFillPattern() { }

    public BitChartFillPattern(BitChartPatternStyle style, string color, string? background = null)
    {
        Style = style;
        Color = color;
        Background = background;
    }
}
