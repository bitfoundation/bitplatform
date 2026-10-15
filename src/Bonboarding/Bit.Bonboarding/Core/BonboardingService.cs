using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Bonboarding;

/// <summary>
/// The default <see cref="IBonboardingService"/>. Scoped, so it is per-circuit on Blazor Server and
/// per-app on WebAssembly, which is exactly the lifetime a user's onboarding state wants.
/// </summary>
internal sealed class BonboardingService : IBonboardingService, IDisposable, IAsyncDisposable
{
    private readonly BonboardingOptions _options;
    private readonly IBonboardingInterop _interop;
    private readonly IBonboardingStore _store;
    private readonly IBonboardingLocalizer _localizer;
    private readonly NavigationManager? _navigation;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BonboardingService> _logger;
    private readonly IReadOnlyList<IBonboardingObserver> _observers;

    private readonly Dictionary<string, BonboardingTourDefinition> _registered = new(StringComparer.Ordinal);
    private readonly List<BonboardingTourSession> _sessions = [];
    private readonly SemaphoreSlim _startGate = new(1, 1);

    private bool _disposed;

    public BonboardingService(
        IOptions<BonboardingOptions> options,
        IBonboardingInterop interop,
        IBonboardingStore store,
        IBonboardingLocalizer localizer,
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        IEnumerable<IBonboardingObserver> observers,
        NavigationManager? navigation = null)
    {
        _options = options.Value;
        _interop = interop;
        _store = store;
        _localizer = localizer;
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BonboardingService>();
        _observers = observers.ToArray();
        _navigation = navigation;
    }

    public IBonboardingTourSession? Active { get; private set; }

    public IReadOnlyList<IBonboardingTourSession> Sessions => _sessions;

    public IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours => _registered.Values;

    public event EventHandler<BonboardingEvent>? EventRaised;

    public event EventHandler? SessionsChanged;

    // ---- Registration ------------------------------------------------------

    public void Register(BonboardingTourDefinition tour)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ArgumentException.ThrowIfNullOrWhiteSpace(tour.Id);

        _registered[tour.Id] = tour;
    }

    public void Unregister(string tourId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tourId);
        _registered.Remove(tourId);
    }

    public BonboardingTourDefinition? GetTour(string tourId)
        => _registered.GetValueOrDefault(tourId);

    // ---- Starting ----------------------------------------------------------

    public async Task<IBonboardingTourSession> StartAsync(
        BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (tour.Steps.Count == 0)
        {
            throw new BonboardingException($"Tour '{tour.Id}' has no steps.") { TourId = tour.Id };
        }

        var startOptions = options ?? new BonboardingStartOptions();

        if (!startOptions.Force && tour.CanStart is not null)
        {
            var allowed = await tour.CanStart(new BonboardingTourContext(tour, _services)).ConfigureAwait(false);
            if (!allowed)
            {
                throw new BonboardingException($"Tour '{tour.Id}' declined to start.") { TourId = tour.Id };
            }
        }

        BonboardingTourSession session;

        // The gate only covers the bookkeeping. Starting runs application hooks, and a hook that
        // starts another tour would otherwise wait on a gate its own caller is holding.
        await _startGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (startOptions.StopOthers)
            {
                await StopRunningSessionsAsync(BonboardingTourEndReason.Cancelled).ConfigureAwait(false);
            }

            // Restarting a tour that is already running should not leave the old session attached.
            var existing = _sessions.FirstOrDefault(s => s.Tour.Id == tour.Id && s.IsActive);
            if (existing is not null)
            {
                await existing.CancelAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Register(tour);

            session = new BonboardingTourSession(
                tour, _options, _interop, _store, _localizer, _navigation, _services,
                _loggerFactory.CreateLogger<BonboardingTourSession>(), PublishAsync);

            session.Changed += OnSessionChanged;
            _sessions.Add(session);
            Active = session;
        }
        finally
        {
            _startGate.Release();
        }

        RaiseSessionsChanged();

        await session.StartAsync(startOptions, cancellationToken).ConfigureAwait(false);
        return session;
    }

    public Task<IBonboardingTourSession> StartAsync(string tourId, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
    {
        var tour = GetTour(tourId)
            ?? throw new BonboardingException($"No tour is registered with id '{tourId}'.") { TourId = tourId };

        return StartAsync(tour, options, cancellationToken);
    }

    public async Task<IBonboardingTourSession?> StartOnceAsync(
        BonboardingTourDefinition tour, BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);

        var startOptions = options ?? new BonboardingStartOptions();

        if (!startOptions.Force)
        {
            // Compare against this definition rather than the registry, which may not hold it yet.
            var record = await ReadRecordAsync(tour.Id, cancellationToken).ConfigureAwait(false);
            if (record is not null && record.Version == tour.Version && record.IsClosed) return null;
        }

        if (!startOptions.Force && tour.CanStart is not null)
        {
            var allowed = await tour.CanStart(new BonboardingTourContext(tour, _services)).ConfigureAwait(false);
            if (!allowed) return null;
        }

        // CanStart has already been consulted; skip the second evaluation inside StartAsync.
        return await StartAsync(tour, CloneForced(startOptions), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);

        await ResetAsync(tour.Id, cancellationToken).ConfigureAwait(false);

        return await StartAsync(
            tour,
            new BonboardingStartOptions { Resume = false, Force = true },
            cancellationToken).ConfigureAwait(false);
    }

    private static BonboardingStartOptions CloneForced(BonboardingStartOptions source) => new()
    {
        StartAtStepId = source.StartAtStepId,
        StartAtIndex = source.StartAtIndex,
        Resume = source.Resume,
        Force = true,
        State = source.State,
        StopOthers = source.StopOthers,
    };

    // ---- Stopping and querying --------------------------------------------

    public async Task StopAllAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Cancelled, CancellationToken cancellationToken = default)
    {
        await StopRunningSessionsAsync(reason).ConfigureAwait(false);
        RaiseSessionsChanged();
    }

    private async Task StopRunningSessionsAsync(BonboardingTourEndReason reason)
    {
        foreach (var session in _sessions.Where(s => s.IsActive).ToArray())
        {
            switch (reason)
            {
                case BonboardingTourEndReason.Completed:
                    await session.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
                    break;
                case BonboardingTourEndReason.Skipped:
                    await session.SkipAsync(CancellationToken.None).ConfigureAwait(false);
                    break;
                case BonboardingTourEndReason.Dismissed:
                    await session.DismissAsync(CancellationToken.None).ConfigureAwait(false);
                    break;
                default:
                    await session.CancelAsync(CancellationToken.None).ConfigureAwait(false);
                    break;
            }
        }
    }

    public IBonboardingTourSession? FindSession(string tourId)
        => _sessions.FirstOrDefault(s => s.Tour.Id == tourId);

    public async Task<bool> IsCompletedAsync(string tourId, CancellationToken cancellationToken = default)
        => (await GetRecordAsync(tourId, cancellationToken).ConfigureAwait(false))?.Completed ?? false;

    public async Task<bool> IsClosedAsync(string tourId, CancellationToken cancellationToken = default)
        => (await GetRecordAsync(tourId, cancellationToken).ConfigureAwait(false))?.IsClosed ?? false;

    public async Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken cancellationToken = default)
    {
        var record = await ReadRecordAsync(tourId, cancellationToken).ConfigureAwait(false);

        // A record written by an earlier version of the tour is not usable.
        if (record is not null && GetTour(tourId) is { } tour && record.Version != tour.Version)
        {
            return null;
        }

        return record;
    }

    private async Task<BonboardingRecord?> ReadRecordAsync(string tourId, CancellationToken cancellationToken)
    {
        try
        {
            return await _store.GetAsync(tourId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingTourSession.IsTransientInteropFailure(ex))
        {
            _logger.LogDebug(ex, "BonboardingService could not read the record for tour {TourId}.", tourId);
            return null;
        }
    }

    public async Task ResetAsync(string tourId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.RemoveAsync(tourId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingTourSession.IsTransientInteropFailure(ex))
        {
            _logger.LogDebug(ex, "BonboardingService could not reset tour {TourId}.", tourId);
        }
    }

    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (BonboardingTourSession.IsTransientInteropFailure(ex))
        {
            _logger.LogDebug(ex, "BonboardingService could not reset stored progress.");
        }
    }

    // ---- Event fan-out -----------------------------------------------------

    private async ValueTask PublishAsync(BonboardingEvent onboardingEvent)
    {
        try
        {
            EventRaised?.Invoke(this, onboardingEvent);
        }
        catch (Exception ex)
        {
            // An analytics handler must never be able to break a tour.
            _logger.LogWarning(ex, "An onboarding event handler threw.");
        }

        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnEventAsync(onboardingEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BonboardingService observer {Observer} threw.", observer.GetType().Name);
            }
        }
    }

    private void OnSessionChanged(object? sender, BonboardingTourSessionChangedEventArgs e)
    {
        if (!e.Change.HasFlag(BonboardingTourSessionChange.Ended)) return;

        if (ReferenceEquals(Active, sender))
        {
            Active = _sessions.FirstOrDefault(s => s.IsActive);
        }

        RaiseSessionsChanged();
    }

    private void RaiseSessionsChanged()
    {
        try
        {
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An onboarding SessionsChanged handler threw.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var session in _sessions)
        {
            session.Changed -= OnSessionChanged;
            await session.DisposeAsync().ConfigureAwait(false);
        }

        Cleanup();
    }

    /// <summary>
    /// Synchronous teardown. Blazor disposes its scopes asynchronously, but plenty of hosts and
    /// test frameworks do not, and a service that only supports async disposal makes those throw.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var session in _sessions)
        {
            session.Changed -= OnSessionChanged;
            session.Dispose();
        }

        Cleanup();
    }

    private void Cleanup()
    {
        _sessions.Clear();
        Active = null;
        _startGate.Dispose();
    }
}
