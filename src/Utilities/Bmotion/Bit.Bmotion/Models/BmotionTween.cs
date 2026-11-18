namespace Bit.Bmotion;

/// <summary>Duration- and easing-based interpolation between values.</summary>
public sealed class BmotionTween : BmotionTransition
{
    /// <summary>Duration in seconds. Default: 0.3.</summary>
    public double Duration { get; set; } = 0.3;

    /// <summary>Named easing preset. Default: <see cref="BmotionEase.Out"/>.</summary>
    public BmotionEase Ease { get; set; } = BmotionEase.Out;

    /// <summary>
    /// Custom cubic-bezier as [x1, y1, x2, y2]; overrides <see cref="Ease"/> when set.
    /// X coordinates must be within [0, 1].
    /// </summary>
    public double[]? Bezier { get; set; }

    /// <summary>
    /// Number of discrete steps for a stepped (CSS <c>steps()</c>) easing. When greater than zero,
    /// this overrides <see cref="Ease"/> and <see cref="Bezier"/>. Default: 0 (not stepped).
    /// </summary>
    public int Steps { get; set; }

    /// <summary>Where a stepped easing's jumps occur (see <see cref="Steps"/>). Default: End.</summary>
    public BmotionStepJump StepJump { get; set; } = BmotionStepJump.End;

    /// <summary>
    /// Progress offsets (0-1) for each keyframe value; evenly distributed when omitted.
    /// Length must match the keyframe array being animated.
    /// </summary>
    public double[]? Times { get; set; }

    /// <summary>
    /// Per-segment easing for keyframe sequences: <c>Eases[i]</c> eases the segment from keyframe
    /// <c>i</c> to keyframe <c>i+1</c> (one fewer entry than keyframes; the last entry repeats when
    /// the array is shorter). Overrides <see cref="Ease"/> segment-by-segment when set.
    /// </summary>
    public BmotionEase[]? Eases { get; set; }

    private protected override BmotionTransitionConfig CreateConfig() => new()
    {
        Type = BmotionTransitionType.Tween,
        Duration = Duration,
        Ease = Ease,
        EaseCubicBezier = Bezier,
        StepCount = Steps,
        StepJump = StepJump,
        Times = Times,
        Eases = Eases,
    };

    private protected override BmotionTransition CloneTransition()
    {
        var copy = new BmotionTween
        {
            Duration = Duration,
            Ease = Ease,
            Bezier = Bezier,
            Steps = Steps,
            StepJump = StepJump,
            Times = Times,
            Eases = Eases,
        };
        CopyBaseTo(copy);
        return copy;
    }

    internal override bool ValueEquals(BmotionTransition other)
        => other is BmotionTween t
           && Duration == t.Duration && Ease == t.Ease
           && Steps == t.Steps && StepJump == t.StepJump
           && ArraysEqual(Bezier, t.Bezier) && ArraysEqual(Times, t.Times)
           && EasesEqual(Eases, t.Eases)
           && base.ValueEquals(other);

    private static bool EasesEqual(BmotionEase[]? a, BmotionEase[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.AsSpan().SequenceEqual(b);
    }
}
