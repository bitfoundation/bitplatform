namespace Bit.Bonboarding;

/// <summary>Information handed to tour-level hooks such as <see cref="BonboardingTourDefinition.CanStart"/>.</summary>
public sealed class BonboardingTourContext(BonboardingTourDefinition tour, IServiceProvider services, IBonboardingTourSession? session = null)
{
    /// <summary>The tour being evaluated.</summary>
    public BonboardingTourDefinition Tour { get; } = tour;

    /// <summary>The application's service provider, for resolving anything the hook needs.</summary>
    public IServiceProvider Services { get; } = services;

    /// <summary>The live session, when one already exists.</summary>
    public IBonboardingTourSession? Session { get; } = session;
}
