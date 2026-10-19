namespace Bit.Bonboarding;

/// <summary>Payload of <see cref="IBonboardingTourSession.Changed"/>.</summary>
public sealed class BonboardingTourSessionChangedEventArgs(BonboardingTourSessionChange change) : EventArgs
{
    /// <summary>Lets the host skip work when only geometry moved.</summary>
    public BonboardingTourSessionChange Change { get; } = change;
}
