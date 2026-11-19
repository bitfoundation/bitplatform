using System.Buffers;
using System.Drawing;

namespace Bit.Bmage;

/// <summary>Unsharp-mask sharpening: adds the difference between the image and a blurred copy.</summary>
public sealed class BmageSharpenFilter : BmageRegionOperation
{
    public BmageSharpenFilter(float amount = 0.5f, float radius = 1f, float threshold = 0f, Rectangle? region = null) : base(region)
    {
        if (amount < 0 || !float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (radius <= 0 || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        Amount = amount; Radius = radius; Threshold = Math.Clamp(threshold, 0f, 1f);
    }

    /// <summary>Strength of the effect. 0 = none, 1 = strong.</summary>
    public float Amount { get; }
    /// <summary>Radius of the blur used to find edges.</summary>
    public float Radius { get; }
    /// <summary>Minimum difference (0..1) before sharpening is applied; suppresses noise amplification.</summary>
    public float Threshold { get; }
    public override string Name => "Sharpen";
    public override bool IsIdentity => Amount == 0;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var region = GetRegion(source.Size);
        var target = GetMutableCopy(source, context);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var threshold = (int)(Threshold * 255);
        var amount = Amount;
        // Each channel's result depends only on its value and its blurred value, so above a few thousand pixels it is
        // cheaper to work out all 65536 pairs once than to repeat the arithmetic per pixel.
        var table = (long)region.Width * region.Height > 1 << 17 ? SharpenTable(amount, threshold) : null;
        try
        {
            // The blur streams the region's rows and hands over each blurred row once it no longer needs the original
            // above it, so the original row is still there to sharpen and no blurred copy of the region is kept.
            BmageBoxBlur.Gaussian(target, region, Radius, context, (y, blurred) =>
            {
                var dst = target.GetRow(region.Y + y).Slice(region.X, region.Width);
                if (table is not null)
                {
                    for (var x = 0; x < dst.Length; x++)
                    {
                        var p = dst[x];
                        var b = blurred[x];
                        dst[x] = new BmageRgba32(table[p.R << 8 | b.R], table[p.G << 8 | b.G], table[p.B << 8 | b.B], p.A);
                    }
                    return;
                }
                for (var x = 0; x < dst.Length; x++)
                {
                    var p = dst[x];
                    var b = blurred[x];
                    dst[x] = new BmageRgba32(Sharpen(p.R, b.R, amount, threshold), Sharpen(p.G, b.G, amount, threshold), Sharpen(p.B, b.B, amount, threshold), p.A);
                }
            });
        }
        finally
        {
            if (table is not null) ArrayPool<byte>.Shared.Return(table);
        }
        return target;
    }

    /// <summary>
    /// <see cref="Sharpen"/> for every pair of a value and its blurred value, indexed value * 256 + blurred, in an array
    /// rented from the pool.
    /// </summary>
    private static byte[] SharpenTable(float amount, int threshold)
    {
        var table = ArrayPool<byte>.Shared.Rent(256 * 256);
        for (var v = 0; v < 256; v++)
            for (var b = 0; b < 256; b++)
                table[v << 8 | b] = Sharpen((byte)v, (byte)b, amount, threshold);
        return table;
    }

    private static byte Sharpen(byte v, byte blurred, float amount, int threshold)
    {
        var diff = v - blurred;
        if (Math.Abs(diff) < threshold) return v;
        return BmageRgba32.ClampToByte(v + diff * amount);
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageSharpenFilter(Amount, (float)Math.Max(0.5, Radius * scale), Threshold, ScaleRegion(Region, scale));
}
