namespace Bit.BlazorUI;

/// <summary>Font definition mirroring Chart.js Font options.</summary>
public sealed class BitChartFont
{
    /// <summary>
    /// The font family. The default reads the public <c>--bit-Chart-font-family</c> custom property, then the
    /// theme's font family, so the chart sets its text in the app's typeface.
    /// </summary>
    public string Family { get; set; } = DefaultFamily;
    public double Size { get; set; } = 12;

    internal const string DefaultFamily = "var(--bit-Chart-font-family, var(--bit-tpg-font-family, Helvetica, Arial, sans-serif))";
    public string Style { get; set; } = "normal";
    public string Weight { get; set; } = "normal";
    public double LineHeight { get; set; } = 1.2;

    public BitChartFont Clone() => new()
    {
        Family = Family,
        Size = Size,
        Style = Style,
        Weight = Weight,
        LineHeight = LineHeight
    };

    /// <summary>The total line height in pixels.</summary>
    public double LineHeightPx => Size * LineHeight;
}
