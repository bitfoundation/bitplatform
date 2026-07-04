namespace Bit.BlazorUI;

/// <summary>A rectangular plotting area in SVG pixel coordinates.</summary>
public struct BitChartArea
{
    public double Left, Top, Right, Bottom;
    public BitChartArea(double left, double top, double right, double bottom)
    {
        Left = left; Top = top; Right = right; Bottom = bottom;
    }
    public readonly double Width => Right - Left;
    public readonly double Height => Bottom - Top;
    public readonly double CenterX => (Left + Right) / 2;
    public readonly double CenterY => (Top + Bottom) / 2;
}
