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
    private readonly BonboardingGuidanceRegistry _guidance;

    // Components register tours on the renderer while sessions end from browser callbacks and
    // applications start tours from anywhere, so both collections are only touched under _sync.
    private readonly object _sync = new();
    private readonly Dictionary<string, BonboardingTourDefinition> _registered = new(StringComparer.Ordinal);
    private readonly List<BonboardingTourSession> _sessions = [];

    /// <summary>Starts waiting for the running tour to end (<see cref="BonboardingBusyBehavior.Queue"/>), under <see cref="_sync"/>.</summary>
    private readonly List<QueuedStart> _queue = [];

    /// <summary>The sessions last seen <see cref="BonboardingTourSession.IsAway"/>, under <see cref="_sync"/>.</summary>
    private readonly HashSet<BonboardingTourSession> _away = [];

    private readonly TimeProvider _time;

    private long _queueSequence;
    private bool _disposed;

    public BonboardingService(
        IOptions<BonboardingOptions> options,
        IBonboardingInterop interop,
        IBonboardingStore store,
        IBonboardingLocalizer localizer,
        IServiceProvider services,
        ILoggerFactory loggerFactory,
        IEnumerable<IBonboardingObserver> observers,
        BonboardingGuidanceRegistry guidance,
        NavigationManager? navigation = null,
        TimeProvider? time = null)
    {
        _options = options.Value;
        _interop = interop;
        _store = store;
        _localizer = localizer;
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<BonboardingService>();
        _observers = observers.ToArray();
        _guidance = guidance;
        _navigation = navigation;
        _time = time ?? TimeProvider.System;

        if (_store is IBonboardingStoreEvents events) events.Changed += OnStoreChanged;
    }

    // ---- Changes made elsewhere ----------------------------------------------

    /// <summary>
    /// The store says a record changed somewhere else - another tab, another device. Checklists and tour lists refresh,
    /// and a tour that was finished with there stops here, before it can write over that.
    /// </summary>
    private void OnStoreChanged(object? sender, BonboardingStoreChangedEventArgs e)
    {
        if (_disposed) return;

        var id = e.TourId;

        if (id is null || !id.StartsWith(BonboardingChecklistDefinition.StorageKeyPrefix, StringComparison.Ordinal))
        {
            _ = StopToursFinishedElsewhereAsync(id);
        }

        RaiseRecordChanged(id);
    }

    /// <summary>
    /// Tells what is on screen that a stored record changed: <paramref name="key"/> as the store knows it (a tour id, or
    /// <c>checklist:{id}</c>), or <see langword="null"/> for every record. A tour's record decides whether a checklist
    /// item for that tour is done, so the checklists naming the tour refresh too.
    /// </summary>
    private void RaiseRecordChanged(string? key)
    {
        var checklistPrefix = BonboardingChecklistDefinition.StorageKeyPrefix;

        string[] checklists = key is not null && key.StartsWith(checklistPrefix, StringComparison.Ordinal)
            ? [key[checklistPrefix.Length..]]
            : _guidance.Checklists
                .Where(checklist => key is null || checklist.Items.Exists(item => (item.Tour?.Id ?? item.TourId) == key))
                .Select(checklist => checklist.Id)
                .Distinct()
                .ToArray();

        foreach (var checklist in checklists) RaiseChecklistChanged(checklist);

        try
        {
            _guidance.RaiseRecordsChanged();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An onboarding record change handler threw.");
        }
    }

    private async Task StopToursFinishedElsewhereAsync(string? tourId)
    {
        try
        {
            BonboardingTourSession[] live;
            lock (_sync) live = _sessions.Where(session => session.IsLive && (tourId is null || session.Tour.Id == tourId)).ToArray();

            foreach (var session in live)
            {
                var record = BonboardingRecordMigration.Apply(session.Tour, await ReadRecordAsync(session.Tour.Id, CancellationToken.None), _logger);
                if (record is null) continue;

                // Ended there since it started here: refused, snoozed, or completed. Progress written there is not an
                // ending, and neither is a completion from before this run (completion is sticky, and this may be a replay).
                var endedElsewhere = record.Dismissed
                    || record.IsSnoozedAt(_time.GetUtcNow())
                    || (record.Completed && record.CompletedAt > session.StartedAt);

                // Cancelled, which writes nothing: the record the other tab wrote is the one that stands.
                if (endedElsewhere) await session.CancelAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BonboardingService could not reconcile a tour with a change made elsewhere.");
        }
    }

    /// <summary>A start that waits its turn. <see cref="Once"/> starts re-check the store when their turn comes.</summary>
    private sealed record QueuedStart(BonboardingTourSession Session, BonboardingStartOptions Options, bool Once, long Sequence);

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

        bool added;
        lock (_sync)
        {
            added = !_registered.TryGetValue(tour.Id, out var existing) || !ReferenceEquals(existing, tour);
            _registered[tour.Id] = tour;
        }

        // Declarative tours register on every parameter change; only a new definition is news.
        if (added) RaiseRegistrationsChanged();
    }

    public void Unregister(string tourId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tourId);

        bool removed;
        lock (_sync) removed = _registered.Remove(tourId);

        if (removed) RaiseRegistrationsChanged();
    }

    private void RaiseRegistrationsChanged()
    {
        try
        {
            _guidance.RaiseRegistrationsChanged();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An onboarding registration handler threw.");
        }
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
        ThrowIfUnstartable(tour);

        var startOptions = options ?? new BonboardingStartOptions();

        if (!startOptions.Force && tour.CanStart is not null)
        {
            var allowed = await tour.CanStart(new BonboardingTourContext(tour, _services));
            if (!allowed)
            {
                throw new BonboardingException($"Tour '{tour.Id}' declined to start.") { TourId = tour.Id };
            }
        }

        return await StartCoreAsync(tour, startOptions, once: false, cancellationToken)
            ?? throw new BonboardingException($"Tour '{tour.Id}' was not started: another tour is running and the start asked to skip.") { TourId = tour.Id };
    }

    private void ThrowIfUnstartable(BonboardingTourDefinition tour)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (tour.Steps.Count == 0)
        {
            throw new BonboardingException($"Tour '{tour.Id}' has no steps.") { TourId = tour.Id };
        }

        ThrowIfInvalid(tour);
    }

    /// <summary>
    /// Creates the session and either starts it, replacing whatever runs, or queues it behind the running tour,
    /// as <see cref="BonboardingStartOptions.WhenBusy"/> asks. <see langword="null"/> when it was skipped. The caller
    /// has validated the definition and asked <c>CanStart</c> (or was told not to).
    /// </summary>
    private async Task<IBonboardingTourSession?> StartCoreAsync(
        BonboardingTourDefinition tour, BonboardingStartOptions startOptions, bool once, CancellationToken cancellationToken)
    {
        // Starting does not register the tour: a definition built on the fly (one per user, say)
        // would otherwise stay in RegisteredTours for the lifetime of the scope. Register it
        // explicitly to make it startable by id.
        var session = new BonboardingTourSession(
            tour, _options, _interop, _store, _localizer, _navigation, _services,
            _loggerFactory.CreateLogger<BonboardingTourSession>(), PublishAsync, _time);

        // The bookkeeping is one atomic step: the sessions to retire are chosen and the new one
        // takes their place before anything is awaited, so two starts racing each other (a double
        // click, or a layout's StartOnceAsync alongside a page's AutoStart) cannot both survive.
        // A session that is still starting counts as well: it is Idle until the browser and the
        // store have answered, and that is exactly the window such a race falls into.
        BonboardingTourSession[] superseded;
        var queued = false;
        lock (_sync)
        {
            // Another tour on screen, paused, or starting. A queued start is not running anything yet, and the
            // same tour is always replaced: that is a restart, not a conflict. Nor is a tour the user walked away
            // from: nothing of it is on screen and they may never go back, so it is replaced rather than waited for
            // (cancelled, it keeps its progress).
            var busy = startOptions.StopOthers
                && _sessions.Any(s => !s.IsFinished && !IsQueued(s) && !s.IsAway && s.Tour.Id != tour.Id);

            if (busy && startOptions.WhenBusy == BonboardingBusyBehavior.Skip) return null;

            queued = busy && startOptions.WhenBusy == BonboardingBusyBehavior.Queue;

            // A queued start only retires an earlier queued start of the same tour; it waits for the rest.
            superseded = _sessions
                .Where(s => !s.IsFinished && (queued
                    ? s.Tour.Id == tour.Id && IsQueued(s)
                    : startOptions.StopOthers || s.Tour.Id == tour.Id))
                .ToArray();

            session.Changed += OnSessionChanged;
            _sessions.Add(session);

            if (queued)
            {
                _queue.Add(new QueuedStart(session, startOptions, once, ++_queueSequence));
            }
            else
            {
                Active = session;
            }
        }

        RaiseSessionsChanged();

        // Ending a session runs application code (observers, OnEnded), and that code may start
        // another tour. It runs outside every lock this service holds so that it can.
        foreach (var old in superseded)
        {
            await old.CancelAsync(CancellationToken.None);
        }

        if (queued)
        {
            await PublishAsync(new BonboardingEvent { Kind = BonboardingEventKind.TourQueued, Session = session, Timestamp = _time.GetUtcNow() });

            // The running tour may have ended while this start was being queued.
            StartNextQueued();
            return session;
        }

        await session.StartAsync(startOptions, cancellationToken);

        // Cancelled before the tour got going: the session is still Idle, and an Idle session counts as starting, so
        // it would hold up every queued start for the rest of the scope. It is ended instead, as a start that never ran.
        if (session.Status == BonboardingTourStatus.Idle)
        {
            await session.CancelAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
        }

        return session;
    }

    private bool IsQueued(BonboardingTourSession session) => _queue.Exists(entry => ReferenceEquals(entry.Session, session));

    /// <summary>
    /// Starts the queued tour whose turn it is, once nothing else runs: the highest priority, then the earliest.
    /// Called whenever a session ends.
    /// </summary>
    private void StartNextQueued()
    {
        QueuedStart? next;
        lock (_sync)
        {
            if (_disposed || _sessions.Any(s => !s.IsFinished && !IsQueued(s))) return;

            next = _queue
                .OrderByDescending(entry => entry.Session.Tour.Priority)
                .ThenBy(entry => entry.Sequence)
                .FirstOrDefault();

            if (next is null) return;

            _queue.Remove(next);
            Active = next.Session;
        }

        RaiseSessionsChanged();
        _ = RunQueuedAsync(next);
    }

    private async Task RunQueuedAsync(QueuedStart entry)
    {
        var tour = entry.Session.Tour;

        try
        {
            // Asked again now: while it waited the user may have finished it elsewhere, snoozed it, or moved on to
            // where it no longer applies. A start that no longer qualifies is cancelled, never thrown at anyone.
            var eligible = entry.Options.Force
                || ((!entry.Once || await IsDueOnceAsync(tour, CancellationToken.None))
                    && (tour.CanStart is null || await tour.CanStart(new BonboardingTourContext(tour, _services))));

            if (!eligible)
            {
                await entry.Session.CancelAsync(CancellationToken.None);
                return;
            }

            await entry.Session.StartAsync(entry.Options, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BonboardingService could not start the queued tour {TourId}.", tour.Id);
            await entry.Session.CancelAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// True when a once-only start should run: the user has neither finished with the tour (in this version, or one
    /// it migrates) nor snoozed it.
    /// </summary>
    private async Task<bool> IsDueOnceAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken)
    {
        // Compare against this definition rather than the registry, which may not hold it yet.
        var record = BonboardingRecordMigration.Apply(tour, await ReadRecordAsync(tour.Id, cancellationToken), _logger);

        return record is null || (!record.IsClosed && !record.IsSnoozedAt(_time.GetUtcNow()));
    }

    /// <summary>
    /// Refuses a definition with errors, listing every one of them, and logs its warnings. A tour with two steps of
    /// the same id or a jump to nowhere would otherwise start and misbehave in front of a user.
    /// </summary>
    private void ThrowIfInvalid(BonboardingTourDefinition tour)
    {
        var issues = tour.Validate();
        if (issues.Count == 0) return;

        foreach (var warning in issues.Where(issue => issue.Severity == BonboardingValidationSeverity.Warning))
        {
            _logger.LogWarning("BonboardingService tour {TourId}: {Problem} ({Code})", tour.Id, warning.Message, warning.Code);
        }

        var errors = issues.Where(issue => issue.Severity == BonboardingValidationSeverity.Error).ToArray();
        if (errors.Length == 0) return;

        var message = $"Tour '{tour.Id}' is not valid:{string.Concat(errors.Select(error => $"{Environment.NewLine}- {error.Message}"))}";
        _logger.LogError("BonboardingService refused to start an invalid tour. {Problems}", message);

        throw new BonboardingException(message) { TourId = tour.Id, StepId = errors[0].StepId };
    }

    public async Task<bool> SignalAsync(string signal, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signal);

        BonboardingTourSession[] running;
        lock (_sync) running = _sessions.Where(session => session.IsActive).ToArray();

        var items = _guidance.Checklists
            .SelectMany(checklist => checklist.Items
                .Where(item => string.Equals(item.CompleteOnSignal, signal, StringComparison.Ordinal))
                .Select(item => (Checklist: checklist.Id, Item: item.Id)))
            .Distinct()
            .ToArray();

        var handled = false;
        foreach (var session in running)
        {
            handled |= await session.SignalAsync(signal);
        }

        foreach (var (checklist, item) in items)
        {
            handled |= await SetChecklistItemCoreAsync(checklist, item, done: true, cancellationToken);
        }

        return handled;
    }

    // ---- Checklists ---------------------------------------------------------

    /// <summary>Where checklist progress goes when <see cref="BonboardingOptions.Persist"/> is off: this scope only.</summary>
    private readonly BonboardingInMemoryStore _transientChecklists = new();

    /// <summary>
    /// Serialises the changes to checklist records. Each one reads the record, changes it and writes it back, and two
    /// running together (two items ticked at once, two signals) would both start from the same record: the last write
    /// would drop the other's tick for good.
    /// </summary>
    private readonly SemaphoreSlim _checklistGate = new(1, 1);

    private IBonboardingStore ChecklistStore => _options.Persist ? _store : _transientChecklists;

    private static string ChecklistKey(string checklistId) => BonboardingChecklistDefinition.StorageKeyPrefix + checklistId;

    public async Task SetChecklistItemAsync(string checklistId, string itemId, bool done = true, CancellationToken cancellationToken = default)
        => await SetChecklistItemCoreAsync(checklistId, itemId, done, cancellationToken);

    /// <summary>True when the item's stored state changed.</summary>
    private async Task<bool> SetChecklistItemCoreAsync(string checklistId, string itemId, bool done, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checklistId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        await _checklistGate.WaitAsync(cancellationToken);
        try
        {
            var record = await ReadChecklistAsync(checklistId, cancellationToken);
            var data = new Dictionary<string, string>(record?.Data ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            var now = _time.GetUtcNow();

            if (done ? !data.TryAdd(itemId, now.ToString("O", System.Globalization.CultureInfo.InvariantCulture)) : !data.Remove(itemId))
            {
                return false;
            }

            await WriteChecklistAsync((record ?? new BonboardingRecord { TourId = ChecklistKey(checklistId) }) with { Data = data, UpdatedAt = now }, cancellationToken);
        }
        finally
        {
            _checklistGate.Release();
        }

        RaiseChecklistChanged(checklistId);
        return true;
    }

    public async Task<IReadOnlyCollection<string>> GetCompletedChecklistItemsAsync(string checklistId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checklistId);

        var record = await ReadChecklistAsync(checklistId, cancellationToken);
        return record?.Data is { Count: > 0 } data ? data.Keys.ToArray() : [];
    }

    public async Task DismissChecklistAsync(string checklistId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checklistId);

        await _checklistGate.WaitAsync(cancellationToken);
        try
        {
            var record = await ReadChecklistAsync(checklistId, cancellationToken);
            if (record?.Dismissed == true) return;

            await WriteChecklistAsync(
                (record ?? new BonboardingRecord { TourId = ChecklistKey(checklistId) }) with { Dismissed = true, UpdatedAt = _time.GetUtcNow() },
                cancellationToken);
        }
        finally
        {
            _checklistGate.Release();
        }

        RaiseChecklistChanged(checklistId);
    }

    public async Task<bool> IsChecklistDismissedAsync(string checklistId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checklistId);
        return (await ReadChecklistAsync(checklistId, cancellationToken))?.Dismissed == true;
    }

    public async Task ResetChecklistAsync(string checklistId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checklistId);

        // Behind the changes already under way, so a tick that read the record before the reset cannot write it back.
        await _checklistGate.WaitAsync(cancellationToken);
        try
        {
            await ChecklistStore.RemoveAsync(ChecklistKey(checklistId), cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not reset checklist {ChecklistId}.", checklistId);
        }
        finally
        {
            _checklistGate.Release();
        }

        RaiseChecklistChanged(checklistId);
    }

    private async Task<BonboardingRecord?> ReadChecklistAsync(string checklistId, CancellationToken cancellationToken)
    {
        try
        {
            return await ChecklistStore.GetAsync(ChecklistKey(checklistId), cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not read checklist {ChecklistId}.", checklistId);
            return null;
        }
    }

    private async Task WriteChecklistAsync(BonboardingRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await ChecklistStore.SetAsync(record, cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not save checklist {Key}.", record.TourId);
        }
    }

    private void RaiseChecklistChanged(string checklistId)
    {
        try
        {
            _guidance.RaiseChecklistChanged(checklistId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "A checklist change handler threw.");
        }
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
        ThrowIfUnstartable(tour);

        var startOptions = options ?? new BonboardingStartOptions();

        if (!startOptions.Force)
        {
            if (!await IsDueOnceAsync(tour, cancellationToken)) return null;

            if (tour.CanStart is not null && !await tour.CanStart(new BonboardingTourContext(tour, _services))) return null;
        }

        return await StartCoreAsync(tour, startOptions, once: true, cancellationToken);
    }

    public async Task<IBonboardingTourSession> RestartAsync(BonboardingTourDefinition tour, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);

        // Refused before anything is forgotten: a tour that cannot start must not cost the user their progress.
        ThrowIfUnstartable(tour);

        await ResetAsync(tour.Id, cancellationToken);

        // Forced, so CanStart has no say, and replacing whatever runs, so it is never skipped.
        return (await StartCoreAsync(tour, new BonboardingStartOptions { Resume = false, Force = true }, once: false, cancellationToken))!;
    }

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
        //
        // Queued starts go first, so a running tour that ends cannot hand its place to one of them on the way out.
        BonboardingTourSession[] unfinished;
        lock (_sync) unfinished = _sessions.Where(s => !s.IsFinished).OrderBy(s => IsQueued(s) ? 0 : 1).ToArray();

        foreach (var session in unfinished)
        {
            switch (session.IsLive ? reason : BonboardingTourEndReason.Cancelled)
            {
                case BonboardingTourEndReason.Snoozed:
                    await session.SnoozeAsync(BonboardingDefaults.SnoozeDuration, CancellationToken.None);
                    break;
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

        // A record written by another version of the tour is only usable as the tour migrates it. The
        // registered definition says which version is current, or failing that the one a session is running.
        return (GetTour(tourId) ?? FindRunningTour(tourId)) is { } tour
            ? BonboardingRecordMigration.Apply(tour, record, _logger)
            : record;
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

        RaiseRecordChanged(tourId);
    }

    public async Task ResetAllAsync(CancellationToken cancellationToken = default)
    {
        // The store holds the checklists' progress next to the tours' records, so clearing it forgets them too; with
        // Persist off their progress is kept here instead, and is forgotten the same way.
        await _checklistGate.WaitAsync(cancellationToken);
        try
        {
            await _transientChecklists.ClearAsync(cancellationToken);
            await _store.ClearAsync(cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not reset stored progress.");
        }
        finally
        {
            _checklistGate.Release();
        }

        RaiseRecordChanged(null);
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
        if (!e.Change.HasFlag(BonboardingTourSessionChange.Ended))
        {
            if (e.Change != BonboardingTourSessionChange.Geometry && sender is BonboardingTourSession session) TrackAway(session);
            return;
        }

        // A finished session is discarded: it can never run again (a restart creates a new one),
        // and keeping it would grow the list for the lifetime of the circuit.
        if (sender is BonboardingTourSession ended)
        {
            ended.Changed -= OnSessionChanged;
            lock (_sync)
            {
                _sessions.Remove(ended);
                _queue.RemoveAll(entry => ReferenceEquals(entry.Session, ended));
                _away.Remove(ended);
            }
        }

        if (ReferenceEquals(Active, sender))
        {
            lock (_sync) Active = _sessions.FirstOrDefault(s => s.IsActive);
        }

        RaiseSessionsChanged();

        // The tour that was in the way has gone: whatever waited for it can start.
        StartNextQueued();
    }

    /// <summary>
    /// Notices a tour the user walked away from, or came back to. What gives way to a running tour (beacons, the
    /// buttons of a list of tours) follows, through <see cref="SessionsChanged"/>.
    /// </summary>
    private void TrackAway(BonboardingTourSession session)
    {
        bool changed;
        lock (_sync) changed = session.IsAway ? _away.Add(session) : _away.Remove(session);

        if (changed) RaiseSessionsChanged();
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
            _queue.Clear();
            _away.Clear();
            return sessions;
        }
    }

    private void Cleanup()
    {
        Active = null;

        if (_store is IBonboardingStoreEvents events) events.Changed -= OnStoreChanged;
    }
}
