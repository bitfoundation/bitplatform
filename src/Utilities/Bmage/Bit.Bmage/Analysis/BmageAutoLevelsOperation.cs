namespace Bit.Bmage;

/// <summary>
/// Stretches the tonal range so the darkest pixels become black and the brightest become white, ignoring a small
/// fraction of outliers at each end. The most useful single "improve this photo" operation.
/// </summary>
/// <remarks>
/// Channels are stretched together by default, which fixes exposure without changing colour. Stretching each channel
/// separately also removes colour casts, but can shift the colour of an image that is legitimately dominated by one
/// hue, so it is opt-in.
/// </remarks>
public sealed class BmageAutoLevelsOperation : BmageImageOperation
{
    public BmageAutoLevelsOperation(double clipFraction = 0.005, bool perChannel = false, float strength = 1f)
    {
        if (clipFraction is < 0 or > 0.2) throw new ArgumentOutOfRangeException(nameof(clipFraction), "Clip fraction must be between 0 and 0.2.");
        ClipFraction = clipFraction;
        PerChannel = perChannel;
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>Fraction of pixels ignored at each end of the range. 0.005 means the darkest and brightest 0.5%.</summary>
    public double ClipFraction { get; }

    /// <summary>Stretch each channel independently, which also neutralises a colour cast.</summary>
    public bool PerChannel { get; }

    /// <summary>How far towards the fully stretched result to go, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto levels";

    public override bool IsIdentity => Strength <= 0;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var statistics = BmageImageStatistics.Measure(source);
        context.ThrowIfCancellationRequested();

        var curve = PerChannel
            ? BuildPerChannelCurve(statistics)
            : BuildSharedCurve(statistics);

        return curve.Apply(source, context);
    }

    private BmageCurveOperation BuildSharedCurve(BmageImageStatistics statistics)
    {
        var (low, high) = FindRange(statistics.Luminance);
        var table = BuildTable(low, high, Strength);
        return new BmageCurveOperation(table, table, table, null, Name);
    }

    private BmageCurveOperation BuildPerChannelCurve(BmageImageStatistics statistics)
    {
        var (rl, rh) = FindRange(statistics.Red);
        var (gl, gh) = FindRange(statistics.Green);
        var (bl, bh) = FindRange(statistics.Blue);
        return new BmageCurveOperation(
            BuildTable(rl, rh, Strength),
            BuildTable(gl, gh, Strength),
            BuildTable(bl, bh, Strength),
            null, Name);
    }

    private (int Low, int High) FindRange(BmageHistogram histogram)
    {
        var low = histogram.GetPercentile(ClipFraction);
        var high = histogram.GetPercentile(1 - ClipFraction);
        // A flat or nearly flat histogram must not be stretched into noise.
        if (high - low < 8) return (histogram.Minimum, Math.Max(histogram.Minimum + 8, histogram.Maximum));
        return (low, high);
    }

    private static float[] BuildTable(int low, int high, float strength)
    {
        var lowF = low / 255f;
        var highF = high / 255f;
        var range = MathF.Max(1f / 255f, highF - lowF);
        return BmageCurveOperation.BuildTable(v =>
        {
            var stretched = Math.Clamp((v - lowF) / range, 0f, 1f);
            return v + (stretched - v) * strength;
        });
    }
}
