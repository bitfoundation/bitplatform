using System.Drawing;

namespace Bit.Bmage;

/// <summary>Base class for operations that optionally act on a sub-region of the image (e.g. redacting part of a screenshot).</summary>
public abstract class BmageRegionOperation : BmageImageOperation
{
    protected BmageRegionOperation(Rectangle? region) => Region = region;

    /// <summary>The region affected, in input coordinates, or null for the whole image.</summary>
    public Rectangle? Region { get; }

    /// <summary>Resolves the effective region for an input size.</summary>
    protected Rectangle GetRegion(Size input)
    {
        var full = new Rectangle(Point.Empty, input);
        if (Region is not { } r) return full;
        r.Intersect(full);
        return r;
    }

    /// <summary>Scales the region for a proxy image.</summary>
    protected static Rectangle? ScaleRegion(Rectangle? region, double scale)
    {
        if (region is not { } r || scale == 1) return region;
        var x = (int)Math.Round(r.X * scale);
        var y = (int)Math.Round(r.Y * scale);
        return new Rectangle(x, y, Math.Max(1, (int)Math.Round(r.Right * scale) - x), Math.Max(1, (int)Math.Round(r.Bottom * scale) - y));
    }
}
