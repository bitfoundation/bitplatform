namespace Bit.Bonboarding;

/// <summary>Arguments for <see cref="BonboardingTourDefinition.OnEnded"/>.</summary>
public sealed class BonboardingTourEndedEventArgs(
    IBonboardingTourSession session,
    BonboardingTourEndReason reason,
    BonboardingStepDefinition? lastStep,
    int lastIndex,
    Exception? error = null)
{
    public IBonboardingTourSession Session { get; } = session;
    public BonboardingTourEndReason Reason { get; } = reason;

    /// <summary>The step the user was on when the tour stopped.</summary>
    public BonboardingStepDefinition? LastStep { get; } = lastStep;

    public int LastIndex { get; } = lastIndex;

    /// <summary>Set when <see cref="Reason"/> is <see cref="BonboardingTourEndReason.Failed"/>.</summary>
    public Exception? Error { get; } = error;

    /// <summary>True when the tour ran to the end rather than being abandoned.</summary>
    public bool IsSuccess => Reason == BonboardingTourEndReason.Completed;
}
