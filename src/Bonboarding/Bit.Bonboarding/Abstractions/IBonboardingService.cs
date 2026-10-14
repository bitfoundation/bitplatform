namespace Bit.Bonboarding;

/// <summary>
/// The entry point for starting, querying and controlling tours. Registered as a scoped service by
/// <c>AddBitBonboardingServices</c>, so it is per-circuit on Blazor Server and per-app on WebAssembly.
/// </summary>
public interface IBonboardingService
{
    /// <summary>The session a host should currently render, if any.</summary>
    IBonboardingTourSession? Active { get; }

    /// <summary>Every session created in this scope that has not been discarded.</summary>
    IReadOnlyList<IBonboardingTourSession> Sessions { get; }

    /// <summary>Every tour registered with <see cref="Register"/> or by an <c>BonboardingTour</c> component.</summary>
    IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours { get; }

    /// <summary>Raised for every lifecycle moment; the simplest analytics hook.</summary>
    event EventHandler<BonboardingEvent>? EventRaised;

    /// <summary>Raised when <see cref="Active"/> or <see cref="Sessions"/> changes.</summary>
    event EventHandler? SessionsChanged;

    /// <summary>
    /// Makes a tour startable by id, e.g. from a help menu. Declarative
    /// <c>&lt;BonboardingTour&gt;</c> components register themselves automatically.
    /// </summary>
    void Register(BonboardingTourDefinition tour);

    /// <summary>Removes a registration.</summary>
    void Unregister(string tourId);

    /// <summary>Looks up a registered tour.</summary>
    BonboardingTourDefinition? GetTour(string tourId);

    /// <summary>Starts a tour and returns its session.</summary>
    Task<IBonboardingTourSession> StartAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Starts a previously registered tour by id.</summary>
    Task<IBonboardingTourSession> StartAsync(string tourId, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the tour only if the user has not already completed or dismissed it. Returns
    /// <see langword="null"/> when it was skipped. This is the call most applications want on first
    /// login.
    /// </summary>
    Task<IBonboardingTourSession?> StartOnceAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>Clears persisted progress and starts the tour from the beginning.</summary>
    Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default);

    /// <summary>Ends every running session.</summary>
    Task StopAllAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Cancelled, CancellationToken cancellationToken = default);

    /// <summary>The running session for a tour id, if there is one.</summary>
    IBonboardingTourSession? FindSession(string tourId);

    /// <summary>True when the tour was run to the end at some point.</summary>
    Task<bool> IsCompletedAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>True when the tour was completed, skipped or closed, so it should not auto-start.</summary>
    Task<bool> IsClosedAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Reads the persisted record for a tour.</summary>
    Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Forgets a tour, so it can run again.</summary>
    Task ResetAsync(string tourId, CancellationToken cancellationToken = default);

    /// <summary>Forgets every tour.</summary>
    Task ResetAllAsync(CancellationToken cancellationToken = default);
}
