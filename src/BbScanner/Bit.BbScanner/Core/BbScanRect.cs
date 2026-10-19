using System.Globalization;

namespace Bit.BbScanner;

/// <summary>An axis aligned rectangle in image space, in pixels.</summary>
/// <param name="X">Left edge in pixels.</param>
/// <param name="Y">Top edge in pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct BbScanRect(float X, float Y, float Width, float Height)
{
    /// <summary>Right edge in pixels.</summary>
    public float Right => X + Width;

    /// <summary>Bottom edge in pixels.</summary>
    public float Bottom => Y + Height;

    /// <summary>Center of the rectangle.</summary>
    public BbScanPoint Center => new(X + (Width / 2), Y + (Height / 2));

    /// <summary>Computes the smallest rectangle containing every supplied point.</summary>
    /// <param name="points">Points to enclose. An empty span yields an empty rectangle.</param>
    public static BbScanRect FromPoints(ReadOnlySpan<BbScanPoint> points)
    {
        if (points.IsEmpty)
        {
            return default;
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }

        return new BbScanRect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"[{X:0.##}, {Y:0.##} {Width:0.##}x{Height:0.##}]");
}
