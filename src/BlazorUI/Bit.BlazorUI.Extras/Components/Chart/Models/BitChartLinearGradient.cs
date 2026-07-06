namespace Bit.BlazorUI;

/// <summary>
/// A linear gradient fill. By default it runs vertically (top→bottom of the chart area),
/// which is the common case for area fills.
/// </summary>
public sealed class BitChartLinearGradient : BitChartGradientBase
{
    /// <summary>When true the gradient runs vertically, otherwise horizontally.</summary>
    public bool Vertical { get; set; } = true;

    public BitChartLinearGradient() { }

    public BitChartLinearGradient(bool vertical, params BitChartGradientStop[] stops)
    {
        Vertical = vertical;
        Stops.AddRange(stops);
    }

    /// <summary>Convenience: a two-stop top→bottom gradient between two colors.</summary>
    public static BitChartLinearGradient Vertical2(string top, string bottom) =>
        new(true, new BitChartGradientStop(0, top), new BitChartGradientStop(1, bottom));

    /// <summary>Convenience: a two-stop left→right gradient between two colors.</summary>
    public static BitChartLinearGradient Horizontal2(string left, string right) =>
        new(false, new BitChartGradientStop(0, left), new BitChartGradientStop(1, right));
}
