using System.Globalization;

namespace Bit.BbScanner;

/// <summary>
/// A point in image space, measured in pixels from the top-left corner of the
/// frame that was decoded.
/// </summary>
/// <param name="X">Horizontal offset in pixels.</param>
/// <param name="Y">Vertical offset in pixels.</param>
public readonly record struct BbScanPoint(float X, float Y)
{
    /// <summary>Euclidean distance between two points.</summary>
    public static float Distance(BbScanPoint a, BbScanPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({X:0.##}, {Y:0.##})");
}
