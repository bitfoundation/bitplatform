using System.Numerics;

namespace Bit.Bmage.Operations.Adjustments;

/// <summary>
/// Builds each output channel from a weighted mix of the input channels, as the channel mixer of photo editors does: swap
/// or blend channels, correct a cast, or make a black and white conversion that favours one colour.
/// </summary>
/// <remarks>
/// The weights of one output channel usually add up to 1, which keeps the image's brightness. A
/// <see cref="Monochrome(float, float, float, float)"/> mixer gives every output channel the same weights: a black and
/// white image whose tones come from the channels in those proportions, as a red filter on black and white film darkens
/// a blue sky.
/// </remarks>
public sealed class ChannelMixerAdjustment : ColorMatrixOperation
{
    /// <param name="red">How much of the input red, green and blue goes into the output red.</param>
    /// <param name="green">How much of each goes into the output green.</param>
    /// <param name="blue">How much of each goes into the output blue.</param>
    /// <param name="constant">A constant added to each output channel, in [-1,1] of the channel's range.</param>
    public ChannelMixerAdjustment(Vector3 red, Vector3 green, Vector3 blue, Vector3 constant = default)
        : base(Build(red, green, blue, constant), "Channel mixer")
    {
        Red = red;
        Green = green;
        Blue = blue;
        Constant = constant;
    }

    /// <summary>
    /// A black and white conversion mixing the channels in the given proportions. The default weights are close to how
    /// the eye weighs them; raising red darkens skies and lightens skin, raising blue does the opposite.
    /// </summary>
    public static ChannelMixerAdjustment Monochrome(float red = 0.3f, float green = 0.59f, float blue = 0.11f, float constant = 0f)
    {
        var weights = new Vector3(red, green, blue);
        return new ChannelMixerAdjustment(weights, weights, weights, new Vector3(constant));
    }

    /// <summary>The weights of input red, green and blue in the output red.</summary>
    public Vector3 Red { get; }

    /// <summary>The weights of input red, green and blue in the output green.</summary>
    public Vector3 Green { get; }

    /// <summary>The weights of input red, green and blue in the output blue.</summary>
    public Vector3 Blue { get; }

    /// <summary>The constant added to each output channel.</summary>
    public Vector3 Constant { get; }

    /// <summary>True when every output channel has the same weights, so the result is black and white.</summary>
    public bool IsMonochrome => Red == Green && Green == Blue && Constant.X == Constant.Y && Constant.Y == Constant.Z;

    private static ColorMatrix Build(Vector3 red, Vector3 green, Vector3 blue, Vector3 constant)
    {
        foreach (var v in (ReadOnlySpan<Vector3>)[red, green, blue, constant])
            if (!float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z))
                throw new ArgumentOutOfRangeException(nameof(red), "Channel mixer weights must be finite numbers.");
        return new ColorMatrix(new Vector4(red, 0), new Vector4(green, 0), new Vector4(blue, 0), Vector4.UnitW, new Vector4(constant, 0));
    }
}

/// <summary>
/// Neutralises a colour cast by scaling each channel: picking something that should be grey, or white, and making it so.
/// The white balance of a photo taken under tungsten light, or the yellowed paper of a scan.
/// </summary>
/// <remarks>
/// The gains apply to the stored, gamma-encoded values. sRGB's transfer curve is close to a power law, and scaling a
/// power law's input scales its output by a fixed factor, so a picked colour comes out neutral exactly and the rest of
/// the image closely follows what a gain in linear light would give. It is a correction of the encoded image, not a
/// camera's raw white balance: highlights a channel clipped stay clipped.
/// </remarks>
public sealed class WhiteBalanceAdjustment : ColorMatrixOperation
{
    /// <summary>The smallest and largest gain a channel is given, so a near-black pick cannot blow the image out.</summary>
    public const float MinGain = 0.25f, MaxGain = 4f;

    /// <summary>Scales the channels by the given gains, each clamped to [0.25, 4].</summary>
    public WhiteBalanceAdjustment(float redGain, float greenGain, float blueGain)
        : this(new Vector3(Gain(redGain), Gain(greenGain), Gain(blueGain)))
    {
    }

    private WhiteBalanceAdjustment(Vector3 gains)
        : base(ColorMatrix.Scale(gains.X, gains.Y, gains.Z), "White balance")
    {
        RedGain = gains.X;
        GreenGain = gains.Y;
        BlueGain = gains.Z;
    }

    /// <summary>
    /// The gains that turn <paramref name="neutral"/>, a colour that should have none, into the grey of the same
    /// luminance. Use it with a colour picked from a grey card, a white wall or a shadow on snow.
    /// </summary>
    /// <param name="neutral">The colour to make neutral.</param>
    /// <param name="strength">How far to correct, in [0,1]: 1 makes the colour exactly grey.</param>
    public static WhiteBalanceAdjustment FromNeutral(Rgba32 neutral, float strength = 1f)
    {
        var c = new Vector3(neutral.R, neutral.G, neutral.B) / 255f;
        // A channel at zero has no colour to scale; treat it as barely lit rather than divide by it.
        c = Vector3.Max(c, new Vector3(1f / 255f));
        var luminance = Vector3.Dot(c, ColorMath.Rec709);
        var gains = new Vector3(luminance) / c;
        var t = Math.Clamp(float.IsNaN(strength) ? 0 : strength, 0f, 1f);
        gains = Vector3.One + (gains - Vector3.One) * t;
        return new WhiteBalanceAdjustment(gains.X, gains.Y, gains.Z);
    }

    public float RedGain { get; }
    public float GreenGain { get; }
    public float BlueGain { get; }

    private static float Gain(float gain)
        => float.IsFinite(gain) ? Math.Clamp(gain, MinGain, MaxGain) : throw new ArgumentOutOfRangeException(nameof(gain), "A gain must be a finite number.");
}

/// <summary>
/// Removes a colour cast by measuring the image: the average of its mid-tones, which in most photos is close to grey,
/// is made grey. The automatic counterpart of <see cref="WhiteBalanceAdjustment"/>.
/// </summary>
/// <remarks>
/// <para>
/// The "grey world" assumption holds for most scenes and fails for some: a sunset or a forest is meant to be dominated
/// by one colour, which is why <see cref="Strength"/> defaults to a partial correction. Clipped highlights and deep
/// shadows are left out of the average, as their colour says little about the light.
/// </para>
/// <para>
/// The gains come from the pixels the operation is applied to, so on a proxy preview they come from the proxy. The two
/// agree closely because the average of an image barely changes when it is shrunk.
/// </para>
/// </remarks>
public sealed class AutoWhiteBalanceOperation : ImageOperation
{
    /// <param name="strength">How far to correct, in [0,1].</param>
    public AutoWhiteBalanceOperation(float strength = 0.8f) => Strength = Math.Clamp(float.IsNaN(strength) ? 0 : strength, 0f, 1f);

    /// <summary>How far towards fully neutral to correct, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto white balance";

    public override bool IsIdentity => Strength <= 0;

    /// <summary>
    /// The correction the operation would make to <paramref name="image"/>, or null when the image has no mid-tones to
    /// measure.
    /// </summary>
    public WhiteBalanceAdjustment? Measure(ImageBuffer image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        double r = 0, g = 0, b = 0;
        long count = 0;
        for (var y = 0; y < image.Height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            foreach (var p in image.GetRow(y))
            {
                if (p.A < 128) continue;
                var max = Math.Max(p.R, Math.Max(p.G, p.B));
                var min = Math.Min(p.R, Math.Min(p.G, p.B));
                // Clipped highlights and near-black shadows carry no reliable colour.
                if (max >= 250 || max < 16) continue;
                // Strongly saturated pixels are the scene's colours, not the light's.
                if (max - min > 160) continue;
                r += p.R;
                g += p.G;
                b += p.B;
                count++;
            }
        }
        if (count == 0) return null;
        var mean = new Rgba32((byte)Math.Round(r / count), (byte)Math.Round(g / count), (byte)Math.Round(b / count));
        return WhiteBalanceAdjustment.FromNeutral(mean, Strength);
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        if (IsIdentity || Measure(source, context.CancellationToken) is not { } correction)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        return correction.Apply(source, context);
    }
}

/// <summary>
/// Moves the brightest and darkest tones: whites set where the highlights clip to white, blacks where the shadows clip
/// to black, without moving the mid-tones much. The whites and blacks sliders of a raw developer.
/// </summary>
/// <remarks>
/// Both amounts are in [-1,1]. Positive whites brighten the top of the range until it clips; negative whites pull clipped
/// highlights back below white. Negative blacks deepen the shadows into black; positive blacks lift them. The curve is
/// monotonic at every setting, so tones never swap order.
/// </remarks>
public sealed class WhitesBlacksAdjustment : CurveOperation
{
    public WhitesBlacksAdjustment(float whites, float blacks) : this(Clamp(whites), Clamp(blacks), null) { }

    // The same curve moves every channel, so one table serves all three.
    private WhitesBlacksAdjustment(float whites, float blacks, float[]? table)
        : base(table ??= Table(whites, blacks), table, table, null, "Whites/Blacks")
    {
        Whites = whites;
        Blacks = blacks;
    }

    public float Whites { get; }

    public float Blacks { get; }

    public override bool IsIdentity => Whites == 0 && Blacks == 0;

    private static float Clamp(float value) => float.IsNaN(value) ? 0 : Math.Clamp(value, -1f, 1f);

    private static float[] Table(float whites, float blacks) => BuildTable(v =>
        v + 0.25f * whites * ColorMath.SmoothStep(0.4f, 1f, v) + 0.25f * blacks * (1f - ColorMath.SmoothStep(0f, 0.6f, v)));
}

/// <summary>
/// Gives the whole image one hue and saturation, keeping its lightness: a sepia, cyanotype or monochrome tint whose
/// colour is chosen freely. The colorize option of a hue and saturation dialog.
/// </summary>
public sealed class ColorizeAdjustment : PointOperation
{
    /// <param name="hue">The hue in degrees: 0 red, 120 green, 240 blue.</param>
    /// <param name="saturation">The saturation in [0,1].</param>
    /// <param name="lightness">A shift of lightness in [-1,1]: towards black when negative, white when positive.</param>
    /// <param name="amount">How much of the colorised result replaces the original, in [0,1].</param>
    public ColorizeAdjustment(float hue, float saturation = 0.25f, float lightness = 0f, float amount = 1f)
    {
        if (!float.IsFinite(hue)) throw new ArgumentOutOfRangeException(nameof(hue), "The hue must be a finite number of degrees.");
        Hue = ((hue % 360) + 360) % 360;
        Saturation = Math.Clamp(float.IsNaN(saturation) ? 0 : saturation, 0f, 1f);
        Lightness = Math.Clamp(float.IsNaN(lightness) ? 0 : lightness, -1f, 1f);
        Amount = Math.Clamp(float.IsNaN(amount) ? 0 : amount, 0f, 1f);
        // With the hue and saturation fixed, the colour is a function of lightness alone, and a piecewise linear one: the
        // lightness shift is linear, and the HSL to RGB conversion is linear either side of half. Linear interpolation
        // in this table is therefore exact at the kink, which falls on an entry, and within a hair of it elsewhere.
        // Alpha zero, so blending towards an entry leaves a pixel's alpha alone.
        _table = new Vector4[TableSize + 1];
        var h = Hue / 60f;
        for (var i = 0; i <= TableSize; i++)
        {
            var l = i / (float)TableSize;
            l += Lightness * (Lightness > 0 ? 1 - l : l);
            _table[i] = new Vector4(ColorMath.HslToRgb(h, Saturation, l), 0);
        }
    }

    private const int TableSize = 1024;

    private readonly Vector4[] _table;

    /// <summary>The hue in degrees, normalised to [0,360).</summary>
    public float Hue { get; }

    public float Saturation { get; }

    public float Lightness { get; }

    public float Amount { get; }

    public override string Name => "Colorize";

    public override bool IsIdentity => Amount <= 0;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        // Whole vectors, multiplied by vectors, as the gradient map does, for the interpreter's sake.
        var table = _table;
        var amount = new Vector4(Amount, Amount, Amount, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = Vector4.Clamp(pixels[i], Vector4.Zero, Vector4.One);
            var position = (MathF.Max(p.X, MathF.Max(p.Y, p.Z)) + MathF.Min(p.X, MathF.Min(p.Y, p.Z))) / 2 * TableSize;
            var index = Math.Min(TableSize - 1, (int)position);
            var low = table[index];
            var c = low + (table[index + 1] - low) * new Vector4(position - index);
            pixels[i] = p + (c - p) * amount;
        }
    }
}

/// <summary>The eight hue bands an <see cref="HslAdjustment"/> adjusts separately, named for their centre.</summary>
public enum HueBand
{
    /// <summary>Centred on 0°.</summary>
    Reds = 0,
    /// <summary>Centred on 30°.</summary>
    Oranges,
    /// <summary>Centred on 60°.</summary>
    Yellows,
    /// <summary>Centred on 120°.</summary>
    Greens,
    /// <summary>Centred on 180°.</summary>
    Aquas,
    /// <summary>Centred on 240°.</summary>
    Blues,
    /// <summary>Centred on 270°.</summary>
    Purples,
    /// <summary>Centred on 300°.</summary>
    Magentas,
}

/// <summary>A change to the hue, saturation and lightness of one <see cref="HueBand"/>.</summary>
/// <param name="Hue">Degrees to turn the band's hue, clamped to [-60, 60].</param>
/// <param name="Saturation">Relative change of saturation in [-1,1]: -1 removes the colour, 1 doubles it.</param>
/// <param name="Lightness">Shift of lightness in [-1,1]: towards black when negative, white when positive.</param>
public readonly record struct HslShift(float Hue = 0, float Saturation = 0, float Lightness = 0)
{
    /// <summary>True when the shift changes nothing.</summary>
    public bool IsZero => Hue == 0 && Saturation == 0 && Lightness == 0;

    internal HslShift Clamped() => new(
        Math.Clamp(float.IsNaN(Hue) ? 0 : Hue, -60f, 60f),
        Math.Clamp(float.IsNaN(Saturation) ? 0 : Saturation, -1f, 1f),
        Math.Clamp(float.IsNaN(Lightness) ? 0 : Lightness, -1f, 1f));
}

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
/// pipeline.Apply(new HslAdjustment()
///     .With(HueBand.Blues, new HslShift(Saturation: 0.3f, Lightness: -0.25f))
///     .With(HueBand.Greens, new HslShift(Hue: -10)));
/// </code>
/// </example>
public sealed class HslAdjustment : PointOperation
{
    private const int Bands = 8;

    // Band centres in sextants of the hue circle (degrees / 60), with the first repeated a full turn on.
    private static readonly float[] Centres = [0f, 0.5f, 1f, 2f, 3f, 4f, 4.5f, 5f, 6f];

    // The interval between centres each half sextant of hue falls in: [0, 0.5) is the first, [1, 2) the third.
    private static readonly int[] BandOfHalfSextant = [0, 1, 2, 2, 3, 3, 4, 4, 5, 6, 7, 7];

    private readonly HslShift[] _shifts;

    /// <summary>An adjustment that changes nothing; add bands with <see cref="With"/>.</summary>
    public HslAdjustment() => _shifts = new HslShift[Bands];

    /// <summary>An adjustment with the given shifts; bands not listed are left alone.</summary>
    public HslAdjustment(IEnumerable<KeyValuePair<HueBand, HslShift>> shifts) : this()
    {
        ArgumentNullException.ThrowIfNull(shifts);
        foreach (var (band, shift) in shifts) _shifts[Index(band)] = shift.Clamped();
    }

    private HslAdjustment(HslShift[] shifts) => _shifts = shifts;

    /// <summary>The shift of one band.</summary>
    public HslShift this[HueBand band] => _shifts[Index(band)];

    /// <summary>Every band with a shift, in band order.</summary>
    public IEnumerable<KeyValuePair<HueBand, HslShift>> Shifts
    {
        get
        {
            for (var i = 0; i < Bands; i++)
                if (!_shifts[i].IsZero) yield return new((HueBand)i, _shifts[i]);
        }
    }

    /// <summary>A copy with one band's shift replaced.</summary>
    public HslAdjustment With(HueBand band, HslShift shift)
    {
        var shifts = (HslShift[])_shifts.Clone();
        shifts[Index(band)] = shift.Clamped();
        return new HslAdjustment(shifts);
    }

    public override string Name => "HSL";

    public override bool IsIdentity => _shifts.All(s => s.IsZero);

    private static int Index(HueBand band) => (uint)band < Bands ? (int)band : throw new ArgumentOutOfRangeException(nameof(band));

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
            var t = ColorMath.SmoothStep(0f, 1f, (h - Centres[band]) / (Centres[band + 1] - Centres[band]));
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
            pixels[i] = new Vector4(ColorMath.HslToRgb(h, s, l), p.W);
        }
    }
}

/// <summary>
/// Maps every pixel's luminance to a colour along a gradient: shadows take the first stop's colour, highlights the
/// last's. Duotones, split toning and stylised colour grades.
/// </summary>
/// <remarks>
/// The gradient is interpolated in the stored sRGB values, between stops ordered by offset. The stops' alpha is ignored:
/// the result keeps each pixel's own alpha, and <see cref="Amount"/> mixes it with the original.
/// </remarks>
/// <example>
/// <code>
/// // A navy and gold duotone.
/// pipeline.Apply(GradientMapOperation.Duotone(new Rgba32(20, 30, 90), new Rgba32(250, 210, 120)));
/// </code>
/// </example>
public sealed class GradientMapOperation : PointOperation
{
    private readonly (float Offset, Rgba32 Color)[] _stops;
    // The gradient at 256 positions, alpha zero so blending towards it leaves a pixel's alpha alone.
    private readonly Vector4[] _colors;

    /// <param name="stops">At least two stops; offsets are clamped to [0,1].</param>
    /// <param name="amount">How much of the mapped colour replaces the original, in [0,1].</param>
    public GradientMapOperation(IReadOnlyList<(float Offset, Rgba32 Color)> stops, float amount = 1f)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient map needs at least two stops.", nameof(stops));
        if (stops.Any(s => !float.IsFinite(s.Offset))) throw new ArgumentException("Stop offsets must be finite numbers.", nameof(stops));
        _stops = [.. stops.Select(s => (Math.Clamp(s.Offset, 0f, 1f), s.Color)).OrderBy(s => s.Item1)];
        Amount = Math.Clamp(float.IsNaN(amount) ? 0 : amount, 0f, 1f);
        _colors = [.. BuildTable(_stops).Select(color => new Vector4(color, 0))];
    }

    /// <summary>A two-colour gradient map: <paramref name="shadows"/> in the darks, <paramref name="highlights"/> in the lights.</summary>
    public static GradientMapOperation Duotone(Rgba32 shadows, Rgba32 highlights, float amount = 1f)
        => new([(0f, shadows), (1f, highlights)], amount);

    /// <summary>The stops, ordered by offset.</summary>
    public IReadOnlyList<(float Offset, Rgba32 Color)> Stops => _stops;

    public float Amount { get; }

    public override string Name => "Gradient map";

    public override bool IsIdentity => Amount <= 0;

    private static Vector3[] BuildTable((float Offset, Rgba32 Color)[] stops)
    {
        var table = new Vector3[256];
        for (var i = 0; i < 256; i++)
        {
            var t = i / 255f;
            var next = 0;
            while (next < stops.Length && stops[next].Offset < t) next++;
            Vector3 color;
            if (next == 0) color = ToVector(stops[0].Color);
            else if (next == stops.Length) color = ToVector(stops[^1].Color);
            else
            {
                var (o0, c0) = stops[next - 1];
                var (o1, c1) = stops[next];
                var f = o1 > o0 ? (t - o0) / (o1 - o0) : 1f;
                color = Vector3.Lerp(ToVector(c0), ToVector(c1), f);
            }
            table[i] = color;
        }
        return table;

        static Vector3 ToVector(Rgba32 c) => new Vector3(c.R, c.G, c.B) / 255f;
    }

    public override void ProcessRow(Span<Vector4> pixels)
    {
        // Whole vectors, multiplied by vectors: on the WebAssembly interpreter building a Vector3 from a pixel, or scaling
        // a vector by one float, costs far more than the arithmetic. Alpha's weight and amount are zero, so it is kept.
        var table = _colors;
        var amount = new Vector4(Amount, Amount, Amount, 0);
        var weights = new Vector4(ColorMath.Rec709, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var position = Math.Clamp(Vector4.Dot(p, weights), 0f, 1f) * 255f;
            var index = Math.Min(254, (int)position);
            var low = table[index];
            var mapped = low + (table[index + 1] - low) * new Vector4(position - index);
            pixels[i] = p + (mapped - p) * amount;
        }
    }
}

/// <summary>Colour conversions and curves shared by the colour adjustments.</summary>
internal static class ColorMath
{
    /// <summary>The Rec. 709 luma weights, as the library's other luminance measures use.</summary>
    public static readonly Vector3 Rec709 = new(0.2126f, 0.7152f, 0.0722f);

    /// <summary>A smooth step from 0 at <paramref name="edge0"/> to 1 at <paramref name="edge1"/>.</summary>
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>The colour of a hue in sextants [0,6), a saturation and a lightness.</summary>
    public static Vector3 HslToRgb(float h, float s, float l)
    {
        l = Math.Clamp(l, 0f, 1f);
        if (s <= 0) return new Vector3(l);
        var q = l < 0.5f ? l * (1f + s) : l + s - l * s;
        var p = 2f * l - q;
        return new Vector3(Channel(p, q, h + 2f), Channel(p, q, h), Channel(p, q, h - 2f));

        static float Channel(float p, float q, float t)
        {
            if (t < 0) t += 6f;
            if (t >= 6f) t -= 6f;
            if (t < 1f) return p + (q - p) * t;
            if (t < 3f) return q;
            if (t < 4f) return p + (q - p) * (4f - t);
            return p;
        }
    }
}
