using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// Gaussian blur approximated with three successive box blurs (O(n) per pixel, independent of radius), operating on
/// premultiplied alpha so transparent pixels do not darken edges.
/// </summary>
public sealed class BmageBlurFilter : BmageRegionOperation
{
    public BmageBlurFilter(float radius, Rectangle? region = null) : base(region)
    {
        if (radius < 0 || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    /// <summary>Blur radius (standard deviation) in pixels.</summary>
    public float Radius { get; }
    public override string Name => "Blur";
    public override bool IsIdentity => Radius < 0.05f;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        BmageBoxBlur.GaussianInPlace(target, region, Radius, context);
        return target;
    }

    // Clamped first: past float.MaxValue the cast is infinity, which the constructor refuses.
    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageBlurFilter((float)Math.Min(Radius * scale, float.MaxValue), ScaleRegion(Region, scale));
}
