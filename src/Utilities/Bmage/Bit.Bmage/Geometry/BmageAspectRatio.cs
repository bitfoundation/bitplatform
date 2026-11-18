using System.Drawing;

namespace Bit.Bmage;

/// <summary>An aspect ratio expressed as width : height.</summary>
public readonly record struct BmageAspectRatio(double Width, double Height)
{
    public static BmageAspectRatio Square { get; } = new(1, 1);
    public static BmageAspectRatio Ratio4x3 { get; } = new(4, 3);
    public static BmageAspectRatio Ratio3x4 { get; } = new(3, 4);
    public static BmageAspectRatio Ratio3x2 { get; } = new(3, 2);
    public static BmageAspectRatio Ratio2x3 { get; } = new(2, 3);
    public static BmageAspectRatio Ratio16x9 { get; } = new(16, 9);
    public static BmageAspectRatio Ratio9x16 { get; } = new(9, 16);
    public static BmageAspectRatio Ratio21x9 { get; } = new(21, 9);

    /// <summary>The ratio width / height.</summary>
    public double Value => Height == 0 ? double.PositiveInfinity : Width / Height;

    /// <summary>Creates a ratio from a size.</summary>
    public static BmageAspectRatio Of(Size size) => new(size.Width, size.Height);

    /// <summary>Creates a ratio from a decimal value, e.g. 1.5 for 3:2.</summary>
    public static BmageAspectRatio FromValue(double value) => new(value, 1);

    /// <summary>The inverse ratio (height : width).</summary>
    public BmageAspectRatio Inverse => new(Height, Width);

    /// <summary>
    /// Computes the largest rectangle with this aspect ratio that fits inside <paramref name="bounds"/>,
    /// positioned by <paramref name="anchor"/>.
    /// </summary>
    public Rectangle FitInside(Rectangle bounds, BmageAnchor anchor = BmageAnchor.Center)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return Rectangle.Empty;
        var r = Value;
        int w, h;
        if (bounds.Width / (double)bounds.Height > r)
        {
            h = bounds.Height;
            w = Math.Max(1, (int)Math.Round(h * r));
        }
        else
        {
            w = bounds.Width;
            h = Math.Max(1, (int)Math.Round(w / r));
        }
        return anchor.Place(new Size(w, h), bounds);
    }

    /// <summary>Formats as "W:H" using invariant culture.</summary>
    public override string ToString() => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Width:0.###}:{Height:0.###}");
}
