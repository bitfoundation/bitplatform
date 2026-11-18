using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Applies an independent transfer curve (lookup table) to each channel. Consecutive curves fuse by composing their tables.
/// Tables have 256 entries; inputs are clamped to [0,1] before lookup.
/// </summary>
public class BmageCurveOperation : BmagePointOperation
{
    private readonly float[]? _r, _g, _b, _a;
    private readonly string _name;

    /// <summary>Creates a curve operation from explicit 256-entry tables. A null table leaves the channel unchanged.</summary>
    public BmageCurveOperation(float[]? red, float[]? green, float[]? blue, float[]? alpha = null, string? name = null)
    {
        Validate(red); Validate(green); Validate(blue); Validate(alpha);
        _r = red; _g = green; _b = blue; _a = alpha;
        _name = name ?? "Curve";
    }

    /// <summary>Creates a curve operation by sampling a function on the selected channels.</summary>
    public static BmageCurveOperation FromFunction(Func<float, float> curve, BmageColorChannels channels = BmageColorChannels.Rgb, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(curve);
        var table = BuildTable(curve);
        return new BmageCurveOperation(
            channels.HasFlag(BmageColorChannels.Red) ? table : null,
            channels.HasFlag(BmageColorChannels.Green) ? table : null,
            channels.HasFlag(BmageColorChannels.Blue) ? table : null,
            channels.HasFlag(BmageColorChannels.Alpha) ? table : null,
            name);
    }

    /// <summary>Samples a function into a 256-entry table.</summary>
    public static float[] BuildTable(Func<float, float> curve)
    {
        var t = new float[256];
        for (var i = 0; i < 256; i++)
        {
            var v = curve(i / 255f);
            t[i] = float.IsNaN(v) ? 0f : Math.Clamp(v, 0f, 1f);
        }
        return t;
    }

    public override string Name => _name;

    /// <summary>The red table or null when the channel is unchanged.</summary>
    public IReadOnlyList<float>? Red => _r;
    public IReadOnlyList<float>? Green => _g;
    public IReadOnlyList<float>? Blue => _b;
    public IReadOnlyList<float>? Alpha => _a;

    public override bool IsIdentity => IsIdentityTable(_r) && IsIdentityTable(_g) && IsIdentityTable(_b) && IsIdentityTable(_a);

    internal override bool IsChannelSeparable => IsBuiltIn;

    private static bool IsIdentityTable(float[]? t)
    {
        if (t is null) return true;
        for (var i = 0; i < 256; i++)
            if (Math.Abs(t[i] - i / 255f) > 0.5f / 255f) return false;
        return true;
    }

    private static void Validate(float[]? t)
    {
        if (t is not null && t.Length != 256) throw new ArgumentException("Curve tables must have 256 entries.");
    }

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var r = _r; var g = _g; var b = _b; var a = _a;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (r is not null) p.X = r[Index(p.X)];
            if (g is not null) p.Y = g[Index(p.Y)];
            if (b is not null) p.Z = b[Index(p.Z)];
            if (a is not null) p.W = a[Index(p.W)];
            pixels[i] = p;
        }
    }

    private static int Index(float v)
    {
        var i = (int)(v * 255f + 0.5f);
        return i < 0 ? 0 : i > 255 ? 255 : i;
    }

    public override BmagePointOperation? TryFuse(BmagePointOperation next)
    {
        if (next is not BmageCurveOperation c) return null;
        return new BmageCurveOperation(Compose(_r, c._r), Compose(_g, c._g), Compose(_b, c._b), Compose(_a, c._a), "Curve");
    }

    private static float[]? Compose(float[]? first, float[]? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        var t = new float[256];
        for (var i = 0; i < 256; i++) t[i] = second[Index(first[i])];
        return t;
    }
}
