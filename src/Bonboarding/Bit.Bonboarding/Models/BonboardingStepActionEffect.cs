namespace Bit.Bonboarding;

/// <summary>What the tour does after a <see cref="BonboardingStepAction"/>'s handler completes.</summary>
public enum BonboardingStepActionEffect
{
    /// <summary>Stay on the current step.</summary>
    None,
    Next,
    Previous,
    /// <summary>Jump to <see cref="BonboardingStepAction.TargetStepId"/>.</summary>
    GoTo,
    Complete,
    Skip,
    Dismiss,
}
