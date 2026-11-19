namespace Bit.Bmage;

/// <summary>
/// Moves the brightest and darkest tones: whites set where the highlights clip to white, blacks where the shadows clip
/// to black, without moving the mid-tones much. The whites and blacks sliders of a raw developer.
/// </summary>
/// <remarks>
/// Both amounts are in [-1,1]. Positive whites brighten the top of the range until it clips; negative whites pull clipped
/// highlights back below white. Negative blacks deepen the shadows into black; positive blacks lift them. The curve is
/// monotonic at every setting, so tones never swap order.
/// </remarks>
public sealed class BmageWhitesBlacksAdjustment : BmageCurveOperation
{
    public BmageWhitesBlacksAdjustment(float whites, float blacks) : this(Clamp(whites), Clamp(blacks), null) { }

    // The same curve moves every channel, so one table serves all three.
    private BmageWhitesBlacksAdjustment(float whites, float blacks, float[]? table)
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
        v + 0.25f * whites * BmageColorMath.SmoothStep(0.4f, 1f, v) + 0.25f * blacks * (1f - BmageColorMath.SmoothStep(0f, 0.6f, v)));
}
