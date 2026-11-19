using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// Keeps only the fine detail of an image: each channel becomes mid grey plus its difference from a blurred copy.
/// Blended over the image with <see cref="BmageBlendMode.Overlay"/> or <see cref="BmageBlendMode.SoftLight"/> it
/// sharpens; on its own it shows edges and texture, and finds blurred areas.
/// </summary>
public sealed class BmageHighPassFilter : BmageRegionOperation
{
    /// <param name="radius">The radius of the blur subtracted, in pixels: the size of the detail kept.</param>
    /// <param name="region">The part of the image to filter, or null for all of it.</param>
    public BmageHighPassFilter(float radius = 3f, Rectangle? region = null) : base(region)
    {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be a positive number of pixels.");
        Radius = radius;
    }

    /// <summary>The radius (standard deviation) of the blur subtracted, in pixels.</summary>
    public float Radius { get; }

    public override string Name => "High pass";

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var region = GetRegion(source.Size);
        var target = GetMutableCopy(source, context);
        if (region.IsEmptyArea()) return target;
        // The blur hands over each blurred row while the original row is still in place, as sharpening uses it.
        BmageBoxBlur.Gaussian(target, region, Radius, context, (y, blurred) =>
        {
            var row = target.GetRow(region.Y + y).Slice(region.X, region.Width);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                var b = blurred[x];
                row[x] = new BmageRgba32(BmageRgba32.ClampToByte(128 + p.R - b.R), BmageRgba32.ClampToByte(128 + p.G - b.G), BmageRgba32.ClampToByte(128 + p.B - b.B), p.A);
            }
        });
        return target;
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageHighPassFilter((float)Math.Max(0.5, Radius * scale), ScaleRegion(Region, scale));
}
