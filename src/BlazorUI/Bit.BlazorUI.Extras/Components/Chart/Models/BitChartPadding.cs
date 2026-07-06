namespace Bit.BlazorUI;

/// <summary>BitChartPadding values mirroring Chart.js padding (number or per-side).</summary>
public readonly record struct BitChartPadding(double Top, double Right, double Bottom, double Left)
{
    public static BitChartPadding All(double v) => new(v, v, v, v);
    public static BitChartPadding Symmetric(double vertical, double horizontal) => new(vertical, horizontal, vertical, horizontal);
    public double Vertical => Top + Bottom;
    public double Horizontal => Left + Right;

    public static implicit operator BitChartPadding(double v) => All(v);
}
