namespace Bit.Bonboarding;

/// <summary>When a declarative tour should start itself.</summary>
public enum BonboardingAutoStart
{
    /// <summary>Never; call <see cref="BonboardingTour.StartAsync"/> yourself.</summary>
    None,

    /// <summary>Once per user, skipped when the tour is already completed or dismissed.</summary>
    Once,

    /// <summary>On every render of the component, resuming persisted progress.</summary>
    Always,
}
