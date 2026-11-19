using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Rasterises outlines into masks.</summary>
internal static class BmageMaskRasterizer
{
    /// <summary>
    /// Adds a filled outline's coverage, scaled by <paramref name="opacity"/>, to the mask holding
    /// <paramref name="window"/> of the image: each value becomes the larger of itself and the coverage, or with
    /// <paramref name="erase"/> is reduced by it. The outline is in pixels of the image, and a pixel's coverage does not
    /// depend on the window it is drawn in.
    /// </summary>
    public static void Fill(BmageMaskBuffer target, Rectangle window, BmageVectorPath path, BmageFillRule rule, float opacity = 1f, bool erase = false)
    {
        var scale = Math.Clamp(opacity, 0f, 1f) * 255f;
        if (scale <= 0) return;
        BmageRasterizer.Rasterize(path, window, rule, (y, x0, coverage) =>
        {
            var row = target.GetRow(y - window.Top).Slice(x0 - window.Left, coverage.Length);
            for (var i = 0; i < coverage.Length; i++)
            {
                var c = coverage[i];
                if (c <= 0.0005f) continue;
                var v = BmageRgba32.ClampToByte(MathF.Min(c, 1f) * scale);
                row[i] = erase ? (byte)((row[i] * (255 - v) + 127) / 255) : Math.Max(row[i], v);
            }
        });
    }

    public static Matrix3x2 ToPixels(BmageMaskUnits units, Size size)
        => units == BmageMaskUnits.Relative ? Matrix3x2.CreateScale(size.Width, size.Height) : Matrix3x2.Identity;

    public static RectangleF Transform(RectangleF rect, Matrix3x2 m) => BmageAffineParts.MapRectangle(rect, m);

    public static RectangleF BoundsOf(IEnumerable<Vector2> points)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        return minX > maxX ? RectangleF.Empty : RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }
}
