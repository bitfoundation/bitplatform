namespace Bit.BlazorUI;

/// <summary>Title / subtitle plugin options.</summary>
public sealed class BitChartTitleOptions
{
    public bool Display { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-clr-fg-pri, #333)";
    public BitChartPosition Position { get; set; } = BitChartPosition.Top;
    public BitChartAlign Align { get; set; } = BitChartAlign.Center;
    public BitChartFont Font { get; set; } = new() { Size = 16, Weight = "bold" };
    public BitChartPadding Padding { get; set; } = 10;
}
