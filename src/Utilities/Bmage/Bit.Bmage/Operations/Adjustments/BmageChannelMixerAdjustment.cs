using System.Numerics;

namespace Bit.Bmage;

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
public sealed class BmageChannelMixerAdjustment : BmageColorMatrixOperation
{
    /// <param name="red">How much of the input red, green and blue goes into the output red.</param>
    /// <param name="green">How much of each goes into the output green.</param>
    /// <param name="blue">How much of each goes into the output blue.</param>
    /// <param name="constant">A constant added to each output channel, in [-1,1] of the channel's range.</param>
    public BmageChannelMixerAdjustment(Vector3 red, Vector3 green, Vector3 blue, Vector3 constant = default)
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
    public static BmageChannelMixerAdjustment Monochrome(float red = 0.3f, float green = 0.59f, float blue = 0.11f, float constant = 0f)
    {
        var weights = new Vector3(red, green, blue);
        return new BmageChannelMixerAdjustment(weights, weights, weights, new Vector3(constant));
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

    private static BmageColorMatrix Build(Vector3 red, Vector3 green, Vector3 blue, Vector3 constant)
    {
        foreach (var v in (ReadOnlySpan<Vector3>)[red, green, blue, constant])
            if (!float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z))
                throw new ArgumentOutOfRangeException(nameof(red), "Channel mixer weights must be finite numbers.");
        return new BmageColorMatrix(new Vector4(red, 0), new Vector4(green, 0), new Vector4(blue, 0), Vector4.UnitW, new Vector4(constant, 0));
    }
}
