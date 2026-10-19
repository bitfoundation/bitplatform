namespace Bit.Bonboarding;

/// <summary>Browser facts sampled once per host.</summary>
public sealed record BonboardingBrowserEnvironment
{
    /// <summary>Safe defaults for prerendering, where no browser is available.</summary>
    public static BonboardingBrowserEnvironment Unknown { get; } = new();

    public bool ReducedMotion { get; init; }
    public bool RightToLeft { get; init; }
    public BonboardingSize Viewport { get; init; } = new(1024, 768);
    public bool CoarsePointer { get; init; }
}
