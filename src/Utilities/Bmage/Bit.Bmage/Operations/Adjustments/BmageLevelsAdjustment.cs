using System.Numerics;

namespace Bit.Bmage;

/// <summary>Photoshop style levels: remaps the input range [InputBlack, InputWhite] to [OutputBlack, OutputWhite] with a mid-tone gamma.</summary>
public sealed class BmageLevelsAdjustment : BmageCurveOperation
{
    public BmageLevelsAdjustment(float inputBlack = 0f, float inputWhite = 1f, float gamma = 1f, float outputBlack = 0f, float outputWhite = 1f, BmageColorChannels channels = BmageColorChannels.Rgb)
        : base(
            channels.HasFlag(BmageColorChannels.Red) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            channels.HasFlag(BmageColorChannels.Green) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            channels.HasFlag(BmageColorChannels.Blue) ? Table(inputBlack, inputWhite, gamma, outputBlack, outputWhite) : null,
            null, "Levels")
    {
        InputBlack = inputBlack; InputWhite = inputWhite; Gamma = gamma; OutputBlack = outputBlack; OutputWhite = outputWhite;
        Channels = channels;
    }

    /// <summary>The channels the levels apply to.</summary>
    public BmageColorChannels Channels { get; }

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
