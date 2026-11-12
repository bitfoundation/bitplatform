namespace Bit.Bonboarding;

/// <summary>
/// Why a step's target could not be pointed at, reported on <see cref="BonboardingEventKind.TargetMissing"/> and in the
/// log, so a tour that skips or waits on a step says what to fix. New members may be added; treat an unknown one as
/// <see cref="NotFound"/>.
/// </summary>
public enum BonboardingTargetIssue
{
    /// <summary>The target was found, or the step has none.</summary>
    None,

    /// <summary>No element matches the target: it has not rendered yet, was never rendered, or was removed.</summary>
    NotFound,

    /// <summary>
    /// An element matches, but it is not shown: <c>display: none</c>, <c>visibility: hidden</c>, no size, or inside a
    /// closed <c>&lt;details&gt;</c>. A responsive copy of the control may be the one on screen; anchor that one.
    /// </summary>
    Hidden,

    /// <summary>The target's selector is not valid CSS, so nothing can ever match it.</summary>
    InvalidSelector,
}
