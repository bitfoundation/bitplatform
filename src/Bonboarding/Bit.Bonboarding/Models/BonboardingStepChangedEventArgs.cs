namespace Bit.Bonboarding;

/// <summary>Arguments for <see cref="BonboardingTourDefinition.OnStepChanged"/>.</summary>
public sealed class BonboardingStepChangedEventArgs(
    IBonboardingTourSession session,
    BonboardingStepDefinition step,
    int index,
    BonboardingStepDefinition? previousStep,
    BonboardingStepChangeReason reason)
{
    public IBonboardingTourSession Session { get; } = session;
    public BonboardingStepDefinition Step { get; } = step;
    public int Index { get; } = index;
    public BonboardingStepDefinition? PreviousStep { get; } = previousStep;
    public BonboardingStepChangeReason Reason { get; } = reason;
}
