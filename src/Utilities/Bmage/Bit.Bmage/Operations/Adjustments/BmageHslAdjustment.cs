using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Adjusts the hue, saturation and lightness of each colour separately: deepen a blue sky without touching skin, mute
/// a distracting green, turn autumn leaves more orange. The HSL panel of a photo editor, and its selective colour.
/// </summary>
/// <remarks>
/// <para>
/// Each pixel belongs to the two bands its hue lies between, in proportion to how close it is to each, so adjustments
/// blend smoothly across the colour wheel instead of breaking at band edges. Near-neutral pixels, whose hue is
/// meaningless, are affected less the closer to grey they are, and greys not at all.
/// </para>
/// <para>
/// Work happens in the HSL model of the stored sRGB values, which is fast and predictable but not perceptually uniform:
/// equal lightness shifts look stronger on some hues than others. This is the trade simple editors make.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A deeper, slightly darker sky and warmer greens.
/// pipeline.Apply(new BmageHslAdjustment()
///     .With(BmageHueBand.Blues, new BmageHslShift(Saturation: 0.3f, Lightness: -0.25f))
///     .With(BmageHueBand.Greens, new BmageHslShift(Hue: -10)));
/// </code>
/// </example>
public sealed class BmageHslAdjustment : BmagePointOperation
{
    private const int Bands = 8;

    // Band centres in sextants of the hue circle (degrees / 60), with the first repeated a full turn on.
    private static readonly float[] Centres = [0f, 0.5f, 1f, 2f, 3f, 4f, 4.5f, 5f, 6f];

    // The interval between centres each half sextant of hue falls in: [0, 0.5) is the first, [1, 2) the third.
    private static readonly int[] BandOfHalfSextant = [0, 1, 2, 2, 3, 3, 4, 4, 5, 6, 7, 7];

    private readonly BmageHslShift[] _shifts;

    /// <summary>An adjustment that changes nothing; add bands with <see cref="With"/>.</summary>
    public BmageHslAdjustment() => _shifts = new BmageHslShift[Bands];

    /// <summary>An adjustment with the given shifts; bands not listed are left alone.</summary>
    public BmageHslAdjustment(IEnumerable<KeyValuePair<BmageHueBand, BmageHslShift>> shifts) : this()
    {
        ArgumentNullException.ThrowIfNull(shifts);
        foreach (var (band, shift) in shifts) _shifts[Index(band)] = shift.Clamped();
    }

    private BmageHslAdjustment(BmageHslShift[] shifts) => _shifts = shifts;

    /// <summary>The shift of one band.</summary>
    public BmageHslShift this[BmageHueBand band] => _shifts[Index(band)];

    /// <summary>Every band with a shift, in band order.</summary>
    public IEnumerable<KeyValuePair<BmageHueBand, BmageHslShift>> Shifts
    {
        get
        {
            for (var i = 0; i < Bands; i++)
                if (!_shifts[i].IsZero) yield return new((BmageHueBand)i, _shifts[i]);
        }
    }

    /// <summary>A copy with one band's shift replaced.</summary>
    public BmageHslAdjustment With(BmageHueBand band, BmageHslShift shift)
    {
        var shifts = (BmageHslShift[])_shifts.Clone();
        shifts[Index(band)] = shift.Clamped();
        return new BmageHslAdjustment(shifts);
    }

    public override string Name => "HSL";

    public override bool IsIdentity => _shifts.All(s => s.IsZero);

    private static int Index(BmageHueBand band) => (uint)band < Bands ? (int)band : throw new ArgumentOutOfRangeException(nameof(band));

    public override void ProcessRow(Span<Vector4> pixels)
    {
        Span<float> hue = stackalloc float[Bands + 1];
        Span<float> saturation = stackalloc float[Bands + 1];
        Span<float> lightness = stackalloc float[Bands + 1];
        // Whether the interval between a band's centre and the next one's changes anything at all.
        Span<bool> active = stackalloc bool[Bands];
        for (var i = 0; i <= Bands; i++)
        {
            var shift = _shifts[i % Bands];
            hue[i] = shift.Hue / 60f;
            saturation[i] = shift.Saturation;
            lightness[i] = shift.Lightness;
        }
        for (var i = 0; i < Bands; i++) active[i] = !_shifts[i].IsZero || !_shifts[(i + 1) % Bands].IsZero;

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = Vector4.Clamp(pixels[i], Vector4.Zero, Vector4.One);
            var max = MathF.Max(p.X, MathF.Max(p.Y, p.Z));
            var min = MathF.Min(p.X, MathF.Min(p.Y, p.Z));
            var chroma = max - min;
            if (chroma < 1e-4f) continue;

            // The hue first, which alone says whether the pixel lies between two bands that change anything; most
            // pixels do not when only a band or two are adjusted.
            float h;
            if (max == p.X) h = (p.Y - p.Z) / chroma + (p.Y < p.Z ? 6f : 0f);
            else if (max == p.Y) h = (p.Z - p.X) / chroma + 2f;
            else h = (p.X - p.Y) / chroma + 4f;
            if (h >= 6f) h -= 6f;
            var band = BandOfHalfSextant[Math.Min(11, (int)(h * 2))];
            if (!active[band]) continue;
            var l = (max + min) / 2f;
            var s = l > 0.5f ? chroma / (2f - max - min) : chroma / (max + min);

            // The two bands either side of the hue, weighted by a smooth step between their centres.
            var t = BmageColorMath.SmoothStep(0f, 1f, (h - Centres[band]) / (Centres[band + 1] - Centres[band]));
            // Close to grey the hue says little, so the adjustment fades out with the colour.
            var weight = MathF.Min(1f, chroma * 5f);
            var dh = (hue[band] + (hue[band + 1] - hue[band]) * t) * weight;
            var ds = (saturation[band] + (saturation[band + 1] - saturation[band]) * t) * weight;
            var dl = (lightness[band] + (lightness[band + 1] - lightness[band]) * t) * weight;
            if (dh == 0 && ds == 0 && dl == 0) continue;

            h = (h + dh) % 6f;
            if (h < 0) h += 6f;
            s = Math.Clamp(s * (1f + ds), 0f, 1f);
            l += dl * (dl > 0 ? 1f - l : l) * 0.5f;
            pixels[i] = new Vector4(BmageColorMath.HslToRgb(h, s, l), p.W);
        }
    }
}
