using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bit.Bonboarding;

/// <summary>
/// The default <see cref="IBonboardingService"/>. Scoped, so it is per-circuit on Blazor Server and
/// per-app on WebAssembly, which is exactly the lifetime a user's onboarding state wants.
/// </summary>
/// <remarks>
/// Like the session, this deliberately does not use <c>ConfigureAwait(false)</c>: tour hooks such as
/// <see cref="BonboardingTourDefinition.CanStart"/> run after its awaits and may touch components.
/// </remarks>
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

    // Components register tours on the renderer while sessions end from browser callbacks and
    // applications start tours from anywhere, so both collections are only touched under _sync.
    private readonly object _sync = new();
    private readonly Dictionary<string, BonboardingTourDefinition> _registered = new(StringComparer.Ordinal);
    private readonly List<BonboardingTourSession> _sessions = [];

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

    public IReadOnlyList<IBonboardingTourSession> Sessions
    {
        get { lock (_sync) return _sessions.ToArray(); }
    }

    public IReadOnlyCollection<BonboardingTourDefinition> RegisteredTours
    {
        get { lock (_sync) return _registered.Values.ToArray(); }
    }

    public event EventHandler<BonboardingEvent>? EventRaised;

    public event EventHandler? SessionsChanged;

    // ---- Registration ------------------------------------------------------

    public void Register(BonboardingTourDefinition tour)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ArgumentException.ThrowIfNullOrWhiteSpace(tour.Id);

        lock (_sync) _registered[tour.Id] = tour;
    }

    public void Unregister(string tourId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tourId);
        lock (_sync) _registered.Remove(tourId);
    }

    public BonboardingTourDefinition? GetTour(string tourId)
    {
        lock (_sync) return _registered.GetValueOrDefault(tourId);
    }

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
            var allowed = await tour.CanStart(new BonboardingTourContext(tour, _services));
            if (!allowed)
            {
                throw new BonboardingException($"Tour '{tour.Id}' declined to start.") { TourId = tour.Id };
            }
        }

        // Starting does not register the tour: a definition built on the fly (one per user, say)
        // would otherwise stay in RegisteredTours for the lifetime of the scope. Register it
        // explicitly to make it startable by id.
        var session = new BonboardingTourSession(
            tour, _options, _interop, _store, _localizer, _navigation, _services,
            _loggerFactory.CreateLogger<BonboardingTourSession>(), PublishAsync);

        // The bookkeeping is one atomic step: the sessions to retire are chosen and the new one
        // takes their place before anything is awaited, so two starts racing each other (a double
        // click, or a layout's StartOnceAsync alongside a page's AutoStart) cannot both survive.
        // A session that is still starting counts as well: it is Idle until the browser and the
        // store have answered, and that is exactly the window such a race falls into.
        BonboardingTourSession[] superseded;
        lock (_sync)
        {
            superseded = _sessions
                .Where(s => !s.IsFinished && (startOptions.StopOthers || s.Tour.Id == tour.Id))
                .ToArray();

            session.Changed += OnSessionChanged;
            _sessions.Add(session);
            Active = session;
        }

        RaiseSessionsChanged();

        // Ending a session runs application code (observers, OnEnded), and that code may start
        // another tour. It runs outside every lock this service holds so that it can.
        foreach (var old in superseded)
        {
            await old.CancelAsync(CancellationToken.None);
        }

        await session.StartAsync(startOptions, cancellationToken);
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
            var record = await ReadRecordAsync(tour.Id, cancellationToken);
            if (record is not null && record.Version == tour.Version && record.IsClosed) return null;
        }

        if (!startOptions.Force && tour.CanStart is not null)
        {
            var allowed = await tour.CanStart(new BonboardingTourContext(tour, _services));
            if (!allowed) return null;
        }

        // CanStart has already been consulted; skip the second evaluation inside StartAsync.
        return await StartAsync(tour, CloneForced(startOptions), cancellationToken);
    }

    public async Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);

        await ResetAsync(tour.Id, cancellationToken);

        return await StartAsync(
            tour,
            new BonboardingStartOptions { Resume = false, Force = true },
            cancellationToken);
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
        await StopRunningSessionsAsync(reason);
        RaiseSessionsChanged();
    }

    private async Task StopRunningSessionsAsync(BonboardingTourEndReason reason)
    {
        // Paused sessions are still running as far as the application is concerned. A session that
        // is still starting is stopped too, but only ever as cancelled: it has shown nothing, so it
        // has nothing to complete, skip or dismiss.
        BonboardingTourSession[] unfinished;
        lock (_sync) unfinished = _sessions.Where(s => !s.IsFinished).ToArray();

        foreach (var session in unfinished)
        {
            switch (session.IsLive ? reason : BonboardingTourEndReason.Cancelled)
            {
                case BonboardingTourEndReason.Completed:
                    await session.CompleteAsync(CancellationToken.None);
                    break;
                case BonboardingTourEndReason.Skipped:
                    await session.SkipAsync(CancellationToken.None);
                    break;
                case BonboardingTourEndReason.Dismissed:
                    await session.DismissAsync(CancellationToken.None);
                    break;
                default:
                    await session.CancelAsync(CancellationToken.None);
                    break;
            }
        }
    }

    public IBonboardingTourSession? FindSession(string tourId)
    {
        lock (_sync) return _sessions.LastOrDefault(s => s.Tour.Id == tourId && !s.IsFinished);
    }

    public async Task<bool> IsCompletedAsync(string tourId, CancellationToken cancellationToken = default)
        => (await GetRecordAsync(tourId, cancellationToken))?.Completed ?? false;

    public async Task<bool> IsClosedAsync(string tourId, CancellationToken cancellationToken = default)
        => (await GetRecordAsync(tourId, cancellationToken))?.IsClosed ?? false;

    public async Task<BonboardingRecord?> GetRecordAsync(string tourId, CancellationToken cancellationToken = default)
    {
        var record = await ReadRecordAsync(tourId, cancellationToken);

        // A record written by an earlier version of the tour is not usable. The registered
        // definition says which version is current, or failing that the one a session is running.
        if (record is not null && (GetTour(tourId) ?? FindRunningTour(tourId)) is { } tour && record.Version != tour.Version)
        {
            return null;
        }

        return record;
    }

    private BonboardingTourDefinition? FindRunningTour(string tourId)
    {
        lock (_sync) return _sessions.LastOrDefault(s => s.Tour.Id == tourId)?.Tour;
    }

    private async Task<BonboardingRecord?> ReadRecordAsync(string tourId, CancellationToken cancellationToken)
    {
        try
        {
            return await _store.GetAsync(tourId, cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not read the record for tour {TourId}.", tourId);
            return null;
        }
    }

    public async Task ResetAsync(string tourId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.RemoveAsync(tourId, cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not reset tour {TourId}.", tourId);
        }
    }

    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.ClearAsync(cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not reset stored progress.");
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
                await observer.OnEventAsync(onboardingEvent);
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

        // A finished session is discarded: it can never run again (a restart creates a new one),
        // and keeping it would grow the list for the lifetime of the circuit.
        if (sender is BonboardingTourSession ended)
        {
            ended.Changed -= OnSessionChanged;
            lock (_sync) _sessions.Remove(ended);
        }

        if (ReferenceEquals(Active, sender))
        {
            lock (_sync) Active = _sessions.FirstOrDefault(s => s.IsActive);
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

        foreach (var session in TakeSessions())
        {
            session.Changed -= OnSessionChanged;
            await session.DisposeAsync();
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

        foreach (var session in TakeSessions())
        {
            session.Changed -= OnSessionChanged;
            session.Dispose();
        }

        Cleanup();
    }

    private BonboardingTourSession[] TakeSessions()
    {
        lock (_sync)
        {
            var sessions = _sessions.ToArray();
            _sessions.Clear();
            return sessions;
        }
    }

    private void Cleanup()
    {
        Active = null;
    }
}
