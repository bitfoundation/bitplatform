namespace Bit.BlazorUI;

/// <summary>Point label configuration for radial (radar/polar) scales.</summary>
public sealed class BitChartPointLabelOptions
{
    public bool Display { get; set; } = true;
    public string Color { get; set; } = "var(--bit-clr-fg-sec, #666)";
    public BitChartFont Font { get; set; } = new() { Size = 11 };
    /// <summary>Extra padding (px) between the outer grid and the labels.</summary>
    public double Padding { get; set; } = 5;
    /// <summary>Optional per-label formatter: (label, index) => text.</summary>
    public Func<string, int, string>? Callback { get; set; }
}
