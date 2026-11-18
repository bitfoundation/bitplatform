namespace Bit.Bmage;

/// <summary>Applies a gamma curve <c>v^(1/gamma)</c>. Gamma &gt; 1 brightens mid-tones, &lt; 1 darkens them.</summary>
public sealed class BmageGammaAdjustment : BmageCurveOperation
{
    public BmageGammaAdjustment(float gamma)
        : base(Table(gamma), Table(gamma), Table(gamma), null, "Gamma") => Gamma = gamma;

    public float Gamma { get; }

    private static float[] Table(float gamma)
    {
        if (gamma <= 0 || float.IsNaN(gamma)) throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be positive.");
        var inv = 1f / gamma;
        return BuildTable(v => MathF.Pow(v, inv));
    }
}
