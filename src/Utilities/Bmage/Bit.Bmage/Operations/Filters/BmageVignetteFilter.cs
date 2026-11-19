namespace Bit.Bmage;

/// <summary>Darkens (or tints) the corners of the image with a smooth radial falloff.</summary>
public sealed class BmageVignetteFilter : BmageImageOperation
{
    public BmageVignetteFilter(float amount = 0.5f, float radius = 0.75f, float softness = 0.5f, BmageRgba32? color = null)
    {
        Amount = Math.Clamp(amount, 0f, 1f);
        Radius = Math.Clamp(radius, 0.05f, 2f);
        Softness = Math.Clamp(softness, 0.01f, 1f);
        Color = color ?? BmageRgba32.Black;
    }

    /// <summary>Strength 0..1.</summary>
    public float Amount { get; }
    /// <summary>Distance from centre (as a fraction of the half diagonal) where the effect reaches full strength.</summary>
    public float Radius { get; }
    /// <summary>Width of the transition band as a fraction of the radius.</summary>
    public float Softness { get; }
    public BmageRgba32 Color { get; }
    public override string Name => "Vignette";
    public override bool IsIdentity => Amount == 0;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var target = GetMutableCopy(source, context);
        if (IsIdentity) return target;
        int w = target.Width, h = target.Height;
        var cx = w / 2f; var cy = h / 2f;
        var halfDiag = MathF.Sqrt(cx * cx + cy * cy);
        var start = Radius * (1f - Softness) * halfDiag;
        var end = Radius * halfDiag;
        var invRange = 1f / MathF.Max(1e-3f, end - start);
        var cr = Color.R; var cg = Color.G; var cb = Color.B;
        var amount = Amount;
        // Each pixel depends on its own position and value alone.
        BmageParallelRows.For(context, 0, h, w, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 31) == 0) band.ThrowIfCancellationRequested();
                var row = target.GetRow(y);
                var dy = y + 0.5f - cy;
                for (var x = 0; x < w; x++)
                {
                    var dx = x + 0.5f - cx;
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    var t = Math.Clamp((d - start) * invRange, 0f, 1f);
                    t = t * t * (3f - 2f * t) * amount;
                    if (t <= 0f) continue;
                    var p = row[x];
                    row[x] = new BmageRgba32((byte)(p.R + (cr - p.R) * t), (byte)(p.G + (cg - p.G) * t), (byte)(p.B + (cb - p.B) * t), p.A);
                }
            }
        });
        return target;
    }
}
