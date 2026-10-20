using System.Numerics;

namespace Bit.Bmage.Operations.Adjustments;

/// <summary>Adds a constant to the RGB channels. <c>Amount</c> is in [-1,1]; 0 is unchanged.</summary>
public sealed class BrightnessAdjustment : ColorMatrixOperation
{
    public BrightnessAdjustment(float amount) : base(ColorMatrix.Translate(amount, amount, amount), "Brightness") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Scales contrast around mid grey. <c>Amount</c> is in [-1,1]; 0 is unchanged, -1 is flat grey.</summary>
public sealed class ContrastAdjustment : ColorMatrixOperation
{
    public ContrastAdjustment(float amount) : base(ColorMatrix.Contrast(1f + Math.Max(-1f, amount)), "Contrast") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Multiplies RGB by 2^EV, mimicking camera exposure compensation. Typical range [-5,5].</summary>
public sealed class ExposureAdjustment : ColorMatrixOperation
{
    public ExposureAdjustment(float ev) : base(ColorMatrix.Scale(MathF.Pow(2f, ev), MathF.Pow(2f, ev), MathF.Pow(2f, ev)), "Exposure") => Ev = ev;
    public float Ev { get; }
}

/// <summary>Adjusts colour saturation. <c>Amount</c> in [-1,1]; -1 is grayscale, 0 unchanged.</summary>
public sealed class SaturationAdjustment : ColorMatrixOperation
{
    public SaturationAdjustment(float amount) : base(ColorMatrix.Saturate(1f + Math.Max(-1f, amount)), "Saturation") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Rotates hue by the given number of degrees.</summary>
public sealed class HueAdjustment : ColorMatrixOperation
{
    public HueAdjustment(float degrees) : base(ColorMatrix.HueRotate(degrees), "Hue") => Degrees = degrees;
    public float Degrees { get; }
}

/// <summary>Warms (positive) or cools (negative) the image. <c>Amount</c> in [-1,1].</summary>
public sealed class TemperatureAdjustment : ColorMatrixOperation
{
    public TemperatureAdjustment(float amount) : base(ColorMatrix.Scale(1f + 0.25f * amount, 1f, 1f - 0.25f * amount), "Temperature") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Shifts towards magenta (positive) or green (negative). <c>Amount</c> in [-1,1].</summary>
public sealed class TintAdjustment : ColorMatrixOperation
{
    public TintAdjustment(float amount) : base(ColorMatrix.Scale(1f, 1f - 0.25f * amount, 1f), "Tint") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Multiplies the alpha channel. <c>Opacity</c> in [0,1].</summary>
public sealed class OpacityAdjustment : ColorMatrixOperation
{
    public OpacityAdjustment(float opacity) : base(ColorMatrix.Scale(1f, 1f, 1f, Math.Clamp(opacity, 0f, 1f)), "Opacity") => Opacity = opacity;
    public float Opacity { get; }
}

/// <summary>Converts towards grayscale. <c>Amount</c> in [0,1]; 1 is fully grey.</summary>
public sealed class GrayscaleFilter : ColorMatrixOperation
{
    public GrayscaleFilter(float amount = 1f) : base(ColorMatrix.Grayscale(amount), "Grayscale") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Applies a sepia tone. <c>Amount</c> in [0,1].</summary>
public sealed class SepiaFilter : ColorMatrixOperation
{
    public SepiaFilter(float amount = 1f) : base(ColorMatrix.Sepia(amount), "Sepia") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Inverts colours. <c>Amount</c> in [0,1].</summary>
public sealed class InvertFilter : ColorMatrixOperation
{
    public InvertFilter(float amount = 1f) : base(ColorMatrix.Invert(amount), "Invert") => Amount = amount;
    public float Amount { get; }
}

/// <summary>Applies a gamma curve <c>v^(1/gamma)</c>. Gamma &gt; 1 brightens mid-tones, &lt; 1 darkens them.</summary>
public sealed class GammaAdjustment : CurveOperation
{
    public GammaAdjustment(float gamma)
        : base(Table(gamma), Table(gamma), Table(gamma), null, "Gamma") => Gamma = gamma;

    public float Gamma { get; }

    private static float[] Table(float gamma)
    {
        if (gamma <= 0 || float.IsNaN(gamma)) throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be positive.");
        var inv = 1f / gamma;
        return BuildTable(v => MathF.Pow(v, inv));
    }
}

/// <summary>Photoshop style levels: remaps the input range [InputBlack, InputWhite] to [OutputBlack, OutputWhite] with a mid-tone gamma.</summary>
public sealed class LevelsAdjustment : CurveOperation
{
    public LevelsAdjustment(float inputBlack = 0f, float inputWhite = 1f, float gamma = 1f, float outputBlack = 0f, float outputWhite = 1f, ColorChannels channels = ColorChannels.Rgb)
        : base(
            channels.HasFlag(ColorChannels.Red) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            channels.HasFlag(ColorChannels.Green) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            channels.HasFlag(ColorChannels.Blue) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            null, "Levels")
    {
        InputBlack = inputBlack; InputWhite = inputWhite; Gamma = gamma; OutputBlack = outputBlack; OutputWhite = outputWhite;
    }

    public float InputBlack { get; }
    public float InputWhite { get; }
    public float Gamma { get; }
    public float OutputBlack { get; }
    public float OutputWhite { get; }

    private static float[] Table(float ib, float iw, float gamma, float ob, float ow)
    {
        if (gamma <= 0) throw new ArgumentOutOfRangeException(nameof(gamma));
        var range = Math.Max(1e-4f, iw - ib);
        var inv = 1f / gamma;
        return BuildTable(v =>
        {
            var t = Math.Clamp((v - ib) / range, 0f, 1f);
            t = MathF.Pow(t, inv);
            return ob + t * (ow - ob);
        });
    }
}

/// <summary>Reduces each channel to a fixed number of levels (2..256).</summary>
public sealed class PosterizeFilter : CurveOperation
{
    public PosterizeFilter(int levels) : base(Table(levels), Table(levels), Table(levels), null, "Posterize") => Levels = levels;
    public int Levels { get; }

    private static float[] Table(int levels)
    {
        if (levels is < 2 or > 256) throw new ArgumentOutOfRangeException(nameof(levels), "Levels must be between 2 and 256.");
        var steps = levels - 1;
        return BuildTable(v => MathF.Round(v * steps) / steps);
    }
}

/// <summary>Converts to pure black and white by comparing luminance with <c>Level</c> (0..1).</summary>
public sealed class ThresholdFilter : PointOperation
{
    public ThresholdFilter(float level = 0.5f) => Level = Math.Clamp(level, 0f, 1f);
    public float Level { get; }
    public override string Name => "Threshold";

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0f);
        var level = Level;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var l = Vector4.Dot(p, weights);
            var v = l >= level ? 1f : 0f;
            pixels[i] = new Vector4(v, v, v, p.W);
        }
    }
}

/// <summary>
/// Increases saturation of muted colours more than already saturated ones, avoiding clipping of skin tones.
/// <c>Amount</c> in [-1,1].
/// </summary>
public sealed class VibranceAdjustment : PointOperation
{
    public VibranceAdjustment(float amount) => Amount = Math.Clamp(amount, -1f, 1f);
    public float Amount { get; }
    public override string Name => "Vibrance";
    public override bool IsIdentity => Amount == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var amount = Amount;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var max = MathF.Max(p.X, MathF.Max(p.Y, p.Z));
            var avg = (p.X + p.Y + p.Z) / 3f;
            // Move channels away from (positive) or towards (negative) the max channel, scaled by how unsaturated the pixel is.
            var saturation = Math.Clamp((max - avg) * 1.5f, 0f, 1f);
            var k = (1f - saturation) * amount * 0.6f;
            p.X += (p.X - max) * k;
            p.Y += (p.Y - max) * k;
            p.Z += (p.Z - max) * k;
            pixels[i] = p;
        }
    }
}

/// <summary>Lifts or darkens shadows and recovers or boosts highlights based on pixel luminance. Both amounts in [-1,1].</summary>
public sealed class ShadowsHighlightsAdjustment : PointOperation
{
    public ShadowsHighlightsAdjustment(float shadows, float highlights)
    {
        Shadows = Math.Clamp(shadows, -1f, 1f);
        Highlights = Math.Clamp(highlights, -1f, 1f);
    }

    public float Shadows { get; }
    public float Highlights { get; }
    public override string Name => "Shadows/Highlights";
    public override bool IsIdentity => Shadows == 0f && Highlights == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var weights = new Vector4(0.2126f, 0.7152f, 0.0722f, 0f);
        var s = Shadows * 0.5f;
        var h = Highlights * 0.5f;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var l = Math.Clamp(Vector4.Dot(p, weights), 0f, 1f);
            var ws = (1f - l) * (1f - l);
            var wh = l * l;
            var delta = s * ws + h * wh;
            pixels[i] = new Vector4(p.X + delta, p.Y + delta, p.Z + delta, p.W);
        }
    }
}
