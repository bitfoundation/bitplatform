namespace Bit.Bink;

/// <summary>Shape of the mapping from input pressure to width.</summary>
public enum BinkPressureCurveKind : byte
{
    /// <summary>Width grows linearly with pressure.</summary>
    Linear = 0,
    /// <summary>Slow start, fast finish (light touches stay thin).</summary>
    EaseIn = 1,
    /// <summary>Fast start, slow finish (light touches already draw fairly wide).</summary>
    EaseOut = 2,
    /// <summary>Smooth S-curve.</summary>
    EaseInOut = 3,
    /// <summary>Ignores pressure: always the maximum width.</summary>
    Constant = 4,
    /// <summary>Custom lookup table (see <see cref="BinkPressureCurve.Samples"/>).</summary>
    Custom = 5,
}

/// <summary>
/// Maps normalized pressure (0..1) to a width factor between <see cref="MinFactor"/> and <see cref="MaxFactor"/>.
/// Immutable; brushes hold one and the stroke outliner evaluates it per point.
/// </summary>
public sealed record BinkPressureCurve
{
    /// <summary>Linear mapping from 20% to 100% of the brush width.</summary>
    public static readonly BinkPressureCurve Linear = new(BinkPressureCurveKind.Linear, 0.2f, 1f);

    /// <summary>
    /// The default pen response: an S-curve from 20% to 100% of the brush width. Light touches stay noticeably
    /// thin, a normal touch (pressure 0.5) draws at about 60% of the nominal width, and pressing hard reaches it.
    /// </summary>
    public static readonly BinkPressureCurve Pen = new(BinkPressureCurveKind.EaseInOut, 0.2f, 1f);

    /// <summary>Ignores pressure (always full width).</summary>
    public static readonly BinkPressureCurve Constant = new(BinkPressureCurveKind.Constant, 1f, 1f);

    private readonly float[]? _samples;

    /// <summary>Creates a curve.</summary>
    /// <param name="kind">Shape of the curve.</param>
    /// <param name="minFactor">Width factor at zero pressure (0..1).</param>
    /// <param name="maxFactor">Width factor at full pressure (0..1 or larger for emphasis).</param>
    public BinkPressureCurve(BinkPressureCurveKind kind, float minFactor = 0.2f, float maxFactor = 1f)
    {
        if (kind == BinkPressureCurveKind.Custom) throw new ArgumentException("Use PressureCurve.FromSamples to create a custom curve.", nameof(kind));
        Kind = kind;
        MinFactor = Math.Clamp(minFactor, 0f, 4f);
        MaxFactor = Math.Clamp(maxFactor, 0f, 4f);
    }

    private BinkPressureCurve(float[] samples, float minFactor, float maxFactor)
    {
        Kind = BinkPressureCurveKind.Custom;
        _samples = samples;
        MinFactor = Math.Clamp(minFactor, 0f, 4f);
        MaxFactor = Math.Clamp(maxFactor, 0f, 4f);
    }

    /// <summary>Curve shape.</summary>
    public BinkPressureCurveKind Kind { get; }

    /// <summary>Width factor at zero pressure.</summary>
    public float MinFactor { get; }

    /// <summary>Width factor at full pressure.</summary>
    public float MaxFactor { get; }

    /// <summary>For custom curves: uniformly spaced samples of the eased value (0..1) from pressure 0 to 1. Empty otherwise.</summary>
    public ReadOnlySpan<float> Samples => _samples;

    /// <summary>Creates a custom curve from uniformly spaced samples (at least two, each 0..1) that are linearly interpolated.</summary>
    public static BinkPressureCurve FromSamples(ReadOnlySpan<float> samples, float minFactor = 0.2f, float maxFactor = 1f)
    {
        if (samples.Length < 2) throw new ArgumentException("At least two samples are required.", nameof(samples));
        float[] copy = new float[samples.Length];
        for (int i = 0; i < copy.Length; i++) copy[i] = Math.Clamp(samples[i], 0f, 1f);
        return new BinkPressureCurve(copy, minFactor, maxFactor);
    }

    /// <summary>Evaluates the eased 0..1 value for a pressure.</summary>
    public float Ease(float pressure)
    {
        float p = Math.Clamp(pressure, 0f, 1f);
        switch (Kind)
        {
            case BinkPressureCurveKind.Linear: return p;
            case BinkPressureCurveKind.EaseIn: return p * p;
            case BinkPressureCurveKind.EaseOut: return 1f - (1f - p) * (1f - p);
            case BinkPressureCurveKind.EaseInOut: return p < 0.5f ? 2f * p * p : 1f - MathF.Pow(-2f * p + 2f, 2f) / 2f;
            case BinkPressureCurveKind.Constant: return 1f;
            default:
            {
                float[] s = _samples!;
                float f = p * (s.Length - 1);
                int i = (int)f;
                if (i >= s.Length - 1) return s[^1];
                float t = f - i;
                return s[i] + (s[i + 1] - s[i]) * t;
            }
        }
    }

    /// <summary>Evaluates the width factor for a pressure: <c>MinFactor + (MaxFactor - MinFactor) * Ease(pressure)</c>.</summary>
    public float Apply(float pressure) => MinFactor + (MaxFactor - MinFactor) * Ease(pressure);

    /// <inheritdoc />
    public bool Equals(BinkPressureCurve? other)
        => other is not null && Kind == other.Kind && MinFactor == other.MinFactor && MaxFactor == other.MaxFactor && Samples.SequenceEqual(other.Samples);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, MinFactor, MaxFactor, _samples?.Length ?? 0);
}
