namespace Bit.Bonboarding;

/// <summary>
/// Information handed to step-level hooks. Everything needed to make a decision about the current
/// step, plus the service provider for anything else.
/// </summary>
public sealed class BonboardingStepContext(
    IBonboardingTourSession session,
    BonboardingStepDefinition step,
    int index,
    IServiceProvider services,
    CancellationToken cancellationToken = default)
{
    /// <summary>The running session.</summary>
    public IBonboardingTourSession Session { get; } = session;

    /// <summary>The tour the step belongs to.</summary>
    public BonboardingTourDefinition Tour => Session.Tour;

    /// <summary>The step being evaluated.</summary>
    public BonboardingStepDefinition Step { get; } = step;

    /// <summary>Zero-based position of <see cref="Step"/> in <see cref="BonboardingTourDefinition.Steps"/>.</summary>
    public int Index { get; } = index;

    /// <summary>Total number of declared steps.</summary>
    public int Count => Tour.Steps.Count;

    /// <summary>The application's service provider.</summary>
    public IServiceProvider Services { get; } = services;

    /// <summary>Cancelled when the session stops or the step is left.</summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>Per-session scratch state, shared by every hook in the tour.</summary>
    public IDictionary<string, object?> State => Session.State;
}
