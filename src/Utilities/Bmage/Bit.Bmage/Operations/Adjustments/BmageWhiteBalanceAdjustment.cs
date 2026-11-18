using System.Numerics;

namespace Bit.Bmage;

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
public sealed class BmageWhiteBalanceAdjustment : BmageColorMatrixOperation
{
    /// <summary>The smallest and largest gain a channel is given, so a near-black pick cannot blow the image out.</summary>
    public const float MinGain = 0.25f, MaxGain = 4f;

    /// <summary>Scales the channels by the given gains, each clamped to [0.25, 4].</summary>
    public BmageWhiteBalanceAdjustment(float redGain, float greenGain, float blueGain)
        : this(new Vector3(Gain(redGain), Gain(greenGain), Gain(blueGain)))
    {
    }

    private BmageWhiteBalanceAdjustment(Vector3 gains)
        : base(BmageColorMatrix.Scale(gains.X, gains.Y, gains.Z), "White balance")
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
    public static BmageWhiteBalanceAdjustment FromNeutral(BmageRgba32 neutral, float strength = 1f)
    {
        var c = new Vector3(neutral.R, neutral.G, neutral.B) / 255f;
        // A channel at zero has no colour to scale; treat it as barely lit rather than divide by it.
        c = Vector3.Max(c, new Vector3(1f / 255f));
        var luminance = Vector3.Dot(c, BmageColorMath.Rec709);
        var gains = new Vector3(luminance) / c;
        var t = Math.Clamp(float.IsNaN(strength) ? 0 : strength, 0f, 1f);
        gains = Vector3.One + (gains - Vector3.One) * t;
        return new BmageWhiteBalanceAdjustment(gains.X, gains.Y, gains.Z);
    }

    public float RedGain { get; }
    public float GreenGain { get; }
    public float BlueGain { get; }

    private static float Gain(float gain)
        => float.IsFinite(gain) ? Math.Clamp(gain, MinGain, MaxGain) : throw new ArgumentOutOfRangeException(nameof(gain), "A gain must be a finite number.");
}
