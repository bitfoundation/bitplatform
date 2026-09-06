namespace Bit.BlazorUI;

/// <summary>Title / subtitle plugin options.</summary>
public sealed class BitChartTitleOptions
{
    public bool Display { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-clr-fg-pri, #1A1A1A)";
    /// <summary>
    /// Which edge of the chart the title is drawn against (default is the top).
    /// </summary>
    /// <remarks>
    /// A title is a band across the chart rather than a column beside it, so only Top and Bottom are meaningful
    /// here; every other side, the physical pair included, leaves the title at the top. Use <see cref="Align"/>
    /// to move it along the edge it is drawn against.
    /// </remarks>
    public BitSide Position { get; set; } = BitSide.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public BitChartFont Font { get; set; } = new() { Size = 16, Weight = "bold" };
    public BitChartPadding Padding { get; set; } = 10;
}
