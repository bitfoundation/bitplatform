using System.Drawing;

namespace Bit.Bmage;

/// <summary>Adds pseudo-random noise. Deterministic for a given seed.</summary>
public sealed class BmageNoiseFilter : BmageRegionOperation
{
    public BmageNoiseFilter(float amount, bool monochrome = false, int seed = 0, Rectangle? region = null) : base(region)
    {
        Amount = Math.Clamp(amount, 0f, 1f); Monochrome = monochrome; Seed = seed;
    }

    /// <summary>Noise strength 0..1.</summary>
    public float Amount { get; }
    public bool Monochrome { get; }
    public int Seed { get; }
    public override string Name => "Noise";
    public override bool IsIdentity => Amount == 0;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var range = (int)(Amount * 255);
        var state = Seed == 0 ? 0x9E3779B9u : (uint)Seed;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(y).Slice(region.Left, region.Width);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                int nr, ng, nb;
                if (Monochrome) { nr = ng = nb = Next(ref state, range); }
                else { nr = Next(ref state, range); ng = Next(ref state, range); nb = Next(ref state, range); }
                row[x] = new BmageRgba32(BmageRgba32.ClampToByte(p.R + nr), BmageRgba32.ClampToByte(p.G + ng), BmageRgba32.ClampToByte(p.B + nb), p.A);
            }
        }
        return target;
    }

    private static int Next(ref uint state, int range)
    {
        // xorshift32
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        if (range == 0) return 0;
        return (int)(state % (uint)(range * 2 + 1)) - range;
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageNoiseFilter(Amount, Monochrome, Seed, ScaleRegion(Region, scale));
}
