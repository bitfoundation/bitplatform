namespace Bit.BlazorUI;

/// <summary>Axis title configuration.</summary>
public sealed class BitChartScaleTitleOptions
{
    public bool Display { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "var(--bit-clr-fg-sec, #666)";
    public BitChartFont Font { get; set; } = new() { Weight = "bold" };
    public BitChartPadding Padding { get; set; } = new(4, 0, 4, 0);
}
