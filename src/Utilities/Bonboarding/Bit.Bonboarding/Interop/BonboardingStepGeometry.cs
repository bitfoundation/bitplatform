namespace Bit.Bonboarding;

/// <summary>A measurement pushed from the browser.</summary>
public sealed record BonboardingStepGeometry
{
    /// <summary>A geometry meaning "there is no target and none is expected".</summary>
    public static BonboardingStepGeometry Missing { get; } = new();

    /// <summary>True when at least one target element was located.</summary>
    public bool Found { get; init; }

    /// <summary>Union of the target rectangles in viewport coordinates.</summary>
    public BonboardingRect Target { get; init; }

    /// <summary>The visual viewport.</summary>
    public BonboardingRect Viewport { get; init; }

    /// <summary>Measured popover size; zero until the popover has rendered.</summary>
    public BonboardingSize Popover { get; init; }

    /// <summary>False when the target is scrolled out of sight or hidden.</summary>
    public bool TargetVisible { get; init; }

    /// <summary>Monotonic counter, used to drop out-of-order frames.</summary>
    public long Sequence { get; init; }

    /// <summary>The <see cref="BonboardingStepActivation.Activation"/> this frame was measured for.</summary>
    public long Activation { get; init; }

    /// <summary>
    /// The popover's measured corner radius in pixels, so the arrow stays clear of the corners
    /// whatever the theme sets. <see langword="null"/> when unknown.
    /// </summary>
    public double? PopoverRadius { get; init; }

    /// <summary>
    /// A target selector the browser could not parse, reported with the first measurement of the step instead
    /// of being waited on. <see langword="null"/> when every selector is valid.
    /// </summary>
    public string? InvalidSelector { get; init; }

    /// <summary>
    /// Why the target was not found, when <see cref="Found"/> is false and the step has a target: <c>"not-found"</c>,
    /// <c>"hidden"</c> or <c>"invalid-selector"</c>. <see langword="null"/> otherwise, and from a browser layer that does
    /// not say. Read it as <see cref="Issue"/>.
    /// </summary>
    public string? TargetIssue { get; init; }

    /// <summary>
    /// <see cref="TargetIssue"/> as an enum: <see cref="BonboardingTargetIssue.None"/> when the target was found,
    /// <see cref="BonboardingTargetIssue.InvalidSelector"/> when <see cref="InvalidSelector"/> is set, and
    /// <see cref="BonboardingTargetIssue.NotFound"/> for a reason the browser layer did not give.
    /// </summary>
    public BonboardingTargetIssue Issue => Found ? BonboardingTargetIssue.None
        : InvalidSelector is not null ? BonboardingTargetIssue.InvalidSelector
        : BonboardingStepGeometryIssues.Parse(TargetIssue);
}

internal static class BonboardingStepGeometryIssues
{
    public static BonboardingTargetIssue Parse(string? issue) => issue switch
    {
        null or "" or "none" => BonboardingTargetIssue.NotFound,
        "hidden" => BonboardingTargetIssue.Hidden,
        "invalid-selector" => BonboardingTargetIssue.InvalidSelector,
        _ => BonboardingTargetIssue.NotFound,
    };
}
