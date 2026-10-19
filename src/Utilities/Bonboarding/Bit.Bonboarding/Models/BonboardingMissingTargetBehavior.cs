namespace Bit.Bonboarding;

/// <summary>What to do when a step's target element cannot be found.</summary>
public enum BonboardingMissingTargetBehavior
{
    /// <summary>Wait for the element to appear, up to <see cref="BonboardingStepOptions.WaitTimeout"/>, then apply <see cref="BonboardingStepOptions.WaitTimeoutBehavior"/>.</summary>
    Wait,
    /// <summary>Silently advance to the next step.</summary>
    Skip,
    /// <summary>Show the step as a centred modal with no spotlight.</summary>
    Center,
    /// <summary>Fail the tour with an <see cref="BonboardingException"/>.</summary>
    Fail,
}
