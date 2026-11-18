namespace Bit.Bmage;

/// <summary>
/// Adjusts brightness and contrast so the image's mean luminance moves towards a target. A gentler alternative to
/// <see cref="BmageAutoLevelsOperation"/> for images that are correctly exposed but flat.
/// </summary>
public sealed class BmageAutoContrastOperation : BmageImageOperation
{
    public BmageAutoContrastOperation(float targetMean = 0.5f, float strength = 0.7f)
    {
        TargetMean = Math.Clamp(targetMean, 0.1f, 0.9f);
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>The mean luminance to aim for, in [0,1].</summary>
    public float TargetMean { get; }

    /// <summary>How far towards the target to move, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => "Auto contrast";

    public override bool IsIdentity => Strength <= 0;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        if (IsIdentity) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var statistics = BmageImageStatistics.Measure(source);
        context.ThrowIfCancellationRequested();

        var mean = (float)(statistics.Luminance.Mean / 255.0);
        if (mean <= 0.001f || mean >= 0.999f)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);

        // Solve mean^gamma = target for gamma, so applying v^gamma moves the mean onto the target, then damp it by
        // strength. Note the exponent is gamma itself, not its reciprocal: inverting it here darkens dark images,
        // which is the opposite of what this operation is for.
        var gamma = MathF.Log(TargetMean) / MathF.Log(mean);
        gamma = 1f + (gamma - 1f) * Strength;
        gamma = Math.Clamp(gamma, 0.25f, 4f);

        var table = BmageCurveOperation.BuildTable(v => MathF.Pow(v, gamma));
        return new BmageCurveOperation(table, table, table, null, Name).Apply(source, context);
    }
}
