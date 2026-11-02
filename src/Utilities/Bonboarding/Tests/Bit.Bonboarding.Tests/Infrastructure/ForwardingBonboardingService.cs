namespace Bit.Bonboarding.Tests.Infrastructure;

/// <summary>
/// What an application's decorator of <see cref="IBonboardingService"/> looks like (a logging or telemetry wrapper): every
/// member forwarded to the service underneath, and nothing else.
/// </summary>
internal sealed class ForwardingBonboardingService(IBonboardingService inner) : IBonboardingService
{
    public IBonboardingTourSession? Active => inner.Active;

    public IReadOnlyList<IBonboardingTourSession> Sessions => inner.Sessions;

    public IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours => inner.RegisteredTours;

    public event EventHandler<BonboardingEvent>? EventRaised
    {
        add => inner.EventRaised += value;
        remove => inner.EventRaised -= value;
    }

    public event EventHandler? SessionsChanged
    {
        add => inner.SessionsChanged += value;
        remove => inner.SessionsChanged -= value;
    }

    public void Register(BonboardingTourDefinition tour) => inner.Register(tour);

    public void Unregister(string tourId) => inner.Unregister(tourId);

    public BonboardingTourDefinition? GetTour(string tourId) => inner.GetTour(tourId);

    public Task<IBonboardingTourSession> StartAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
        => inner.StartAsync(tour, options, cancellationToken);

    public Task<IBonboardingTourSession> StartAsync(string tourId, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
        => inner.StartAsync(tourId, options, cancellationToken);

    public Task<IBonboardingTourSession?> StartOnceAsync(BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
        => inner.StartOnceAsync(tour, options, cancellationToken);

    public Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default)
        => inner.RestartAsync(tour, cancellationToken);

    public Task StopAllAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Cancelled, CancellationToken cancellationToken = default)
        => inner.StopAllAsync(reason, cancellationToken);

    public IBonboardingTourSession? FindSession(string tourId) => inner.FindSession(tourId);

    public Task<bool> IsCompletedAsync(string tourId, CancellationToken cancellationToken = default) => inner.IsCompletedAsync(tourId, cancellationToken);

    public Task<bool> IsClosedAsync(string tourId, CancellationToken cancellationToken = default) => inner.IsClosedAsync(tourId, cancellationToken);

    public Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken cancellationToken = default) => inner.GetRecordAsync(tourId, cancellationToken);

    public Task ResetAsync(string tourId, CancellationToken cancellationToken = default) => inner.ResetAsync(tourId, cancellationToken);

    public Task ResetAllAsync(CancellationToken cancellationToken = default) => inner.ResetAllAsync(cancellationToken);

    public Task<bool> SignalAsync(string signal, CancellationToken cancellationToken = default) => inner.SignalAsync(signal, cancellationToken);

    public Task SetChecklistItemAsync(string checklistId, string itemId, bool done = true, CancellationToken cancellationToken = default)
        => inner.SetChecklistItemAsync(checklistId, itemId, done, cancellationToken);

    public Task<IReadOnlyCollection<string>> GetCompletedChecklistItemsAsync(string checklistId, CancellationToken cancellationToken = default)
        => inner.GetCompletedChecklistItemsAsync(checklistId, cancellationToken);

    public Task DismissChecklistAsync(string checklistId, CancellationToken cancellationToken = default) => inner.DismissChecklistAsync(checklistId, cancellationToken);

    public Task<bool> IsChecklistDismissedAsync(string checklistId, CancellationToken cancellationToken = default)
        => inner.IsChecklistDismissedAsync(checklistId, cancellationToken);

    public Task ResetChecklistAsync(string checklistId, CancellationToken cancellationToken = default) => inner.ResetChecklistAsync(checklistId, cancellationToken);
}
