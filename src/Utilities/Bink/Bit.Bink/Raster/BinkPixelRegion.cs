
namespace Bit.Bink;

/// <summary>
/// A rectangle of whole pixels, [X0, X1) by [Y0, Y1). Kept in integers because a float cannot hold every column
/// of a very wide surface exactly.
/// </summary>
internal readonly record struct BinkPixelRegion(int X0, int Y0, int X1, int Y1)
{
    /// <summary>No pixels.</summary>
    public static readonly BinkPixelRegion Empty = new(0, 0, 0, 0);

    /// <summary>True when the region holds no pixel.</summary>
    public bool IsEmpty => X1 <= X0 || Y1 <= Y0;

    /// <summary>The pixels a device-space rectangle touches, rounded outwards as the rasterizer rounds its clip.</summary>
    public static BinkPixelRegion Covering(BinkRect rect)
        => new(ToPixel(MathF.Floor(rect.Left)), ToPixel(MathF.Floor(rect.Top)), ToPixel(MathF.Ceiling(rect.Right)), ToPixel(MathF.Ceiling(rect.Bottom)));

    /// <summary>The smallest region holding both.</summary>
    public BinkPixelRegion Union(BinkPixelRegion other)
    {
        if (IsEmpty) return other;
        if (other.IsEmpty) return this;
        return new BinkPixelRegion(Math.Min(X0, other.X0), Math.Min(Y0, other.Y0), Math.Max(X1, other.X1), Math.Max(Y1, other.Y1));
    }

    /// <summary>The pixels in both, or <see cref="Empty"/>.</summary>
    public BinkPixelRegion Intersect(BinkPixelRegion other)
    {
        var result = new BinkPixelRegion(Math.Max(X0, other.X0), Math.Max(Y0, other.Y0), Math.Min(X1, other.X1), Math.Min(Y1, other.Y1));
        return result.IsEmpty ? Empty : result;
    }

    // A clip can lie far outside the surface; saturating keeps it from wrapping round when converted.
    private static int ToPixel(float value) => float.IsNaN(value) ? 0 : (int)Math.Clamp(value, int.MinValue / 2f, int.MaxValue / 2f);
}
