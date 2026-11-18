using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.Bmage;

public static partial class BmageCompositor
{
    /// <summary>
    /// Composites <paramref name="source"/> over <paramref name="destination"/> with a blend mode and an extra
    /// coverage or opacity factor.
    /// </summary>
    /// <remarks>
    /// <see cref="BmageBlendMode.SourceOver"/> gives exactly <see cref="SourceOver"/>, <see cref="BmageBlendMode.Copy"/> replaces
    /// the destination (moving it towards the source, alpha included, when <paramref name="alpha"/> is below 1), and
    /// <see cref="BmageBlendMode.Erase"/> removes the source's coverage from the destination. Every other mode is the W3C
    /// blend: <c>Cs' = (1 - ab) Cs + ab B(Cb, Cs)</c>, composited source-over.
    /// </remarks>
    public static BmageRgba32 Blend(BmageRgba32 destination, BmageRgba32 source, BmageBlendMode mode, float alpha = 1f)
    {
        switch (mode)
        {
            case BmageBlendMode.SourceOver:
                return SourceOver(destination, source, alpha);
            case BmageBlendMode.Copy:
                // Replacing at partial strength moves the destination towards the source, transparency included.
                return alpha >= 1f ? source : BmageMaskedOperation.Mix(destination, source, (int)MathF.Round(Math.Clamp(alpha, 0f, 1f) * 255f));
            case BmageBlendMode.Erase:
                return Erase(destination, source.A / 255f * alpha);
        }
        var sa = source.A / 255f * Math.Clamp(alpha, 0f, 1f);
        if (sa <= 0f) return destination;
        var da = destination.A / 255f;
        var cs = new Vector3(source.R, source.G, source.B) * (1f / 255f);
        var cb = new Vector3(destination.R, destination.G, destination.B) * (1f / 255f);
        // Where the destination is transparent there is nothing to blend with, so the source shows as it is.
        var mixed = da <= 0f ? cs : Vector3.Lerp(cs, Vector3.Clamp(BlendColor(cb, cs, mode), Vector3.Zero, Vector3.One), da);
        var outA = sa + da * (1 - sa);
        var premultiplied = mixed * sa + cb * (da * (1 - sa));
        var color = premultiplied * (255f / outA);
        return new BmageRgba32(BmageRgba32.ClampToByte(color.X), BmageRgba32.ClampToByte(color.Y), BmageRgba32.ClampToByte(color.Z), BmageRgba32.ClampToByte(outA * 255f));
    }

    /// <summary>
    /// Blends a row of source pixels into a row of destination pixels, each weighted by <paramref name="opacity"/> and,
    /// when it is not empty, by the matching byte of <paramref name="coverage"/>.
    /// </summary>
    public static void BlendRow(ReadOnlySpan<BmageRgba32> source, Span<BmageRgba32> destination, BmageBlendMode mode, float opacity = 1f, ReadOnlySpan<byte> coverage = default)
    {
        if (destination.Length < source.Length) throw new ArgumentException("The destination row is shorter than the source row.", nameof(destination));
        if (!coverage.IsEmpty && coverage.Length < source.Length) throw new ArgumentException("The coverage row is shorter than the source row.", nameof(coverage));
        opacity = Math.Clamp(opacity, 0f, 1f);
        if (opacity <= 0f) return;
        if (coverage.IsEmpty)
        {
            if (mode == BmageBlendMode.Copy && opacity >= 1f)
            {
                source.CopyTo(destination);
                return;
            }
            for (var i = 0; i < source.Length; i++)
            {
                var s = source[i];
                if (s.A == 0 && mode != BmageBlendMode.Copy) continue;
                destination[i] = Blend(destination[i], s, mode, opacity);
            }
            return;
        }
        const float inv = 1f / 255f;
        for (var i = 0; i < source.Length; i++)
        {
            var c = coverage[i];
            if (c == 0) continue;
            var s = source[i];
            if (s.A == 0 && mode != BmageBlendMode.Copy) continue;
            destination[i] = Blend(destination[i], s, mode, c == 255 ? opacity : opacity * c * inv);
        }
    }

    /// <summary>The blend function <c>B(Cb, Cs)</c> of the W3C specification, on straight colours in [0,1].</summary>
    internal static Vector3 BlendColor(Vector3 cb, Vector3 cs, BmageBlendMode mode) => mode switch
    {
        BmageBlendMode.Multiply => cb * cs,
        BmageBlendMode.Screen => cb + cs - cb * cs,
        BmageBlendMode.Overlay => new(HardLight(cs.X, cb.X), HardLight(cs.Y, cb.Y), HardLight(cs.Z, cb.Z)),
        BmageBlendMode.Darken => Vector3.Min(cb, cs),
        BmageBlendMode.Lighten => Vector3.Max(cb, cs),
        BmageBlendMode.ColorDodge => new(ColorDodge(cb.X, cs.X), ColorDodge(cb.Y, cs.Y), ColorDodge(cb.Z, cs.Z)),
        BmageBlendMode.ColorBurn => new(ColorBurn(cb.X, cs.X), ColorBurn(cb.Y, cs.Y), ColorBurn(cb.Z, cs.Z)),
        BmageBlendMode.HardLight => new(HardLight(cb.X, cs.X), HardLight(cb.Y, cs.Y), HardLight(cb.Z, cs.Z)),
        BmageBlendMode.SoftLight => new(SoftLight(cb.X, cs.X), SoftLight(cb.Y, cs.Y), SoftLight(cb.Z, cs.Z)),
        BmageBlendMode.Difference => Vector3.Abs(cb - cs),
        BmageBlendMode.Exclusion => cb + cs - 2 * cb * cs,
        BmageBlendMode.Hue => SetLum(SetSat(cs, Sat(cb)), Lum(cb)),
        BmageBlendMode.Saturation => SetLum(SetSat(cb, Sat(cs)), Lum(cb)),
        BmageBlendMode.Color => SetLum(cs, Lum(cb)),
        BmageBlendMode.Luminosity => SetLum(cb, Lum(cs)),
        BmageBlendMode.Add => Vector3.Min(cb + cs, Vector3.One),
        _ => cs,
    };

    private static float HardLight(float cb, float cs)
        => cs <= 0.5f ? cb * 2 * cs : Screen(cb, 2 * cs - 1);

    private static float Screen(float cb, float cs) => cb + cs - cb * cs;

    private static float ColorDodge(float cb, float cs)
        => cb <= 0f ? 0f : cs >= 1f ? 1f : MathF.Min(1f, cb / (1 - cs));

    private static float ColorBurn(float cb, float cs)
        => cb >= 1f ? 1f : cs <= 0f ? 0f : 1 - MathF.Min(1f, (1 - cb) / cs);

    private static float SoftLight(float cb, float cs)
    {
        if (cs <= 0.5f) return cb - (1 - 2 * cs) * cb * (1 - cb);
        var d = cb <= 0.25f ? ((16 * cb - 12) * cb + 4) * cb : MathF.Sqrt(cb);
        return cb + (2 * cs - 1) * (d - cb);
    }

    // The non-separable modes, exactly as the specification defines them.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lum(Vector3 c) => 0.3f * c.X + 0.59f * c.Y + 0.11f * c.Z;

    private static Vector3 ClipColor(Vector3 c)
    {
        var l = Lum(c);
        var n = MathF.Min(c.X, MathF.Min(c.Y, c.Z));
        var x = MathF.Max(c.X, MathF.Max(c.Y, c.Z));
        if (n < 0f && l - n > 1e-6f) c = new Vector3(l) + (c - new Vector3(l)) * (l / (l - n));
        if (x > 1f && x - l > 1e-6f) c = new Vector3(l) + (c - new Vector3(l)) * ((1 - l) / (x - l));
        return c;
    }

    private static Vector3 SetLum(Vector3 c, float l) => ClipColor(c + new Vector3(l - Lum(c)));

    private static float Sat(Vector3 c) => MathF.Max(c.X, MathF.Max(c.Y, c.Z)) - MathF.Min(c.X, MathF.Min(c.Y, c.Z));

    /// <summary>
    /// Scales the colour so its saturation is <paramref name="s"/>: the largest channel becomes s, the smallest 0 and the
    /// middle one keeps its place between them. A grey has no hue to keep and stays black.
    /// </summary>
    private static Vector3 SetSat(Vector3 c, float s)
    {
        var max = MathF.Max(c.X, MathF.Max(c.Y, c.Z));
        var min = MathF.Min(c.X, MathF.Min(c.Y, c.Z));
        var range = max - min;
        if (range <= 1e-6f) return Vector3.Zero;
        return (c - new Vector3(min)) * (s / range);
    }
}
