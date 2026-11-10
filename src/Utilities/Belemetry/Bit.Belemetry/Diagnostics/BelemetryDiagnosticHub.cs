using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>
/// Routes what Belemetry observes to the right diagnostic timeline, and observes what only diagnostics need: the
/// framework's component activities (whether or not traces are exported) and, optionally, its render statistics.
/// </summary>
/// <remarks>
/// <para>
/// On WebAssembly and Hybrid there is one user, and one timeline (<see cref="Global"/>). A server serves everyone at once:
/// each circuit's <see cref="BelemetryDiagnosticsService"/> registers a timeline of its own under the renderer's
/// <see cref="SynchronizationContext"/>, which only that circuit's code runs on, and an event is recorded on a circuit's
/// timeline only when it was produced there - on that context, or under one of that circuit's UI events or navigations
/// (an activity the hub tagged while it started on that context). Anything else is never put on any user's timeline:
/// a missing event, never another user's.
/// </para>
/// <para>Every entry point catches its own failures: diagnostics never break the app, nor diagnose themselves.</para>
/// </remarks>
internal sealed class BelemetryDiagnosticHub : IDisposable
{
    private const string RecorderProperty = "Bit.Belemetry.DiagnosticRecorder";

    private static readonly AsyncLocal<bool> _suppressed = new();

    private readonly BelemetrySettings _settings;
    private readonly BelemetryConsentState _consent;
    private readonly IServiceProvider _services;
    private readonly ConditionalWeakTable<SynchronizationContext, BelemetryDiagnosticRecorder> _byContext = new();
    private readonly object _registrationLock = new();
    private BelemetryDiagnosticEventFactory? _factory;
    private BelemetryDiagnosticSanitizer? _sanitizer;
    private IBelemetryDiagnosticComponentFilter[]? _componentFilters;
    private ActivityListener? _listener;
    private MeterListener? _meters;
    private int _started;
    private int _disposed;

    public BelemetryDiagnosticHub(BelemetrySettings settings, BelemetryConsentState consent, IServiceProvider services)
    {
        _settings = settings;
        _consent = consent;
        _services = services;
        Enabled = settings.Options.Enabled && settings.Options.Diagnostics.Enabled;

        if (Enabled is false) return;

        Global = settings.IsServer ? null : new BelemetryDiagnosticRecorder(Options);
        TraceIndex = settings.IsServer ? new BelemetryTraceIndex() : null;
        _consent.Changed += OnConsentChanged;
    }

    public bool Enabled { get; }

    public BelemetryDiagnosticsOptions Options => _settings.Options.Diagnostics;

    public BelemetrySettings Settings => _settings;

    /// <summary>The one timeline of a WebAssembly or Hybrid app; null on a server.</summary>
    public BelemetryDiagnosticRecorder? Global { get; }

    /// <summary>Server only: unattributed server events by trace id.</summary>
    public BelemetryTraceIndex? TraceIndex { get; }

    public BelemetryDiagnosticSanitizer Sanitizer => _sanitizer ??= new BelemetryDiagnosticSanitizer(_settings.Options, _services.GetServices<IBelemetryDiagnosticSanitizer>());

    public BelemetryDiagnosticEventFactory Factory => _factory ??= new BelemetryDiagnosticEventFactory(_settings, Sanitizer, _services.GetServices<IBelemetryExceptionClassifier>());

    /// <summary>Whether the current flow is diagnostics' own work (a capture), whose logs and activities are not recorded.</summary>
    public static bool IsSuppressed => _suppressed.Value;

    /// <summary>Marks the current flow as diagnostics' own work until disposed.</summary>
    public static IDisposable Suppress()
    {
        var previous = _suppressed.Value;
        _suppressed.Value = true;
        return new Restore(previous);
    }

    /// <summary>Starts listening to the framework. Idempotent; called by the pipeline's start.</summary>
    public void Start()
    {
        if (Enabled is false || Interlocked.Exchange(ref _started, 1) == 1) return;

        if ((Options.Categories & (BelemetryDiagnosticCategories.Navigation | BelemetryDiagnosticCategories.Component | BelemetryDiagnosticCategories.Circuit)) != 0)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name is BelemetryDiagnosticEventFactory.ComponentsSource or BelemetryDiagnosticEventFactory.CircuitsSource,

                // AllData, not AllDataAndRecorded: the activity carries its tags but is never sampled for export or
                // propagated as sampled. And only when creating it changes nothing for the app's traces (CreatesActivity).
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => Sample(options.Source.Name, options.Name),
                SampleUsingParentId = (ref ActivityCreationOptions<string> options) => Sample(options.Source.Name, options.Name),
                ActivityStarted = OnActivityStarted,
                ActivityStopped = OnActivityStopped,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        if (Options.ComponentRenderStatistics && _settings.MetricsSupported)
        {
            _meters = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "Microsoft.AspNetCore.Components.Lifecycle" && instrument.Name == "aspnetcore.components.render_diff.duration") listener.EnableMeasurementEvents(instrument);
                },
            };
            _meters.SetMeasurementEventCallback<double>(OnRenderMeasured);
            _meters.Start();
        }
    }

    private ActivitySamplingResult Sample(string source, string name) =>
        BelemetrySuppression.IsActive || CreatesActivity(source, name) is false ? ActivitySamplingResult.None : ActivitySamplingResult.AllData;

    /// <summary>
    /// Whether the hub's listener makes the framework create an activity (<see cref="BelemetryDiagnosticsOptions.CreateFrameworkActivities"/>).
    /// When Belemetry's own traces cover the source, they decide (the collector creates it anyway, recorded or not), and an
    /// ignored name is never created: an activity that exists is the parent of the spans under it, and an unsampled parent
    /// makes a parent-based sampler drop them. Otherwise, only where Belemetry is the app's tracer (not on a server).
    /// </summary>
    internal bool CreatesActivity(string source, string name)
    {
        var exportCollects = _settings.CollectDotnet && _settings.HasExporters && (_settings.ConfiguredFeatures & BelemetryFeatures.Traces) != 0;
        if (exportCollects && _settings.ExportsTraceSource(source))
        {
            return _settings.Options.Traces.IgnoredActivityNames.Count == 0 || BelemetrySettings.Matches(name, _settings.Options.Traces.IgnoredActivityNames) is false;
        }

        if (BelemetrySettings.Matches(name, _settings.Options.Traces.IgnoredActivityNames)) return false;
        return Options.CreateFrameworkActivities ?? _settings.IsServer is false;
    }

    /// <summary>
    /// The timeline of the circuit whose renderer runs on <paramref name="context"/>, created on first use and shared by
    /// every service of that circuit - a nested scope's (an <c>OwningComponentBase</c>) included - until the last one goes.
    /// </summary>
    public BelemetryDiagnosticRecorder Acquire(SynchronizationContext context, BelemetryDiagnosticsService owner)
    {
        lock (_registrationLock)
        {
            if (_byContext.TryGetValue(context, out var recorder) is false || recorder.IsClosed)
            {
                recorder = new BelemetryDiagnosticRecorder(Options);
                _byContext.AddOrUpdate(context, recorder);
            }
            recorder.AddOwner(owner);
            return recorder;
        }
    }

    /// <summary>Releases a service's share of its circuit's timeline; the last one closes it and forgets the context.</summary>
    public void Release(SynchronizationContext context, BelemetryDiagnosticRecorder recorder, BelemetryDiagnosticsService owner)
    {
        lock (_registrationLock)
        {
            if (recorder.RemoveOwner(owner, closeWhenUnowned: true) > 0) return;
            if (_byContext.TryGetValue(context, out var registered) && ReferenceEquals(registered, recorder)) _byContext.Remove(context);
        }
    }

    /// <summary>The timeline what runs now belongs to; null when it belongs to none (a server's request pipeline, a background job).</summary>
    public BelemetryDiagnosticRecorder? Resolve()
    {
        if (Enabled is false) return null;
        if (Global is not null) return Global;

        if (SynchronizationContext.Current is { } context && _byContext.TryGetValue(context, out var recorder))
        {
            return recorder.IsClosed ? null : recorder;
        }

        // Work that left the renderer's context while a UI event or navigation of the circuit still runs (a library's
        // ConfigureAwait(false)) is under that activity, tagged when it started on the circuit's context. Only while it
        // runs: the activity flows into whatever was started under it - a timer, a singleton's background loop - and what
        // those do later, for anyone, is not this circuit's.
        var activity = Activity.Current;
        for (var hops = 0; activity is not null && hops < 32; hops++, activity = activity.Parent)
        {
            if (activity.GetCustomProperty(RecorderProperty) is WeakReference<BelemetryDiagnosticRecorder> tag)
            {
                return activity.IsStopped is false && tag.TryGetTarget(out var tagged) && tagged.IsClosed is false ? tagged : null;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether an <c>ILogger</c> entry at <paramref name="level"/> would be recorded: the logger produces the record for
    /// diagnostics alone when the export does not want it.
    /// </summary>
    public bool WantsLog(LogLevel level)
    {
        if (Enabled is false || IsSuppressed || level < Options.MinimumLogLevel || level == LogLevel.None) return false;
        if ((Options.Categories & (BelemetryDiagnosticCategories.Log | BelemetryDiagnosticCategories.Exception | BelemetryDiagnosticCategories.JsInterop | BelemetryDiagnosticCategories.Circuit)) == 0) return false;

        // On a server, an entry no circuit can be attributed to is still worth indexing by trace when it is an error.
        return Global is not null || level >= LogLevel.Error || Resolve() is not null;
    }

    /// <summary>A log record the pipeline accepted (after privacy and processors).</summary>
    public void OnLog(BelemetryLogRecord log, bool relayed)
    {
        // Relayed records come from the browser script, whose own timeline is the source of browser evidence.
        if (Enabled is false || relayed || IsSuppressed) return;

        // An entry the logger produced for the export alone (below MinimumLogLevel) stays off the timeline. Errors are
        // always kept: they are what the timeline is for.
        if (log.Feature == BelemetryFeatures.Logs && BelemetryLogger.ToMinimumSeverity(Options.MinimumLogLevel) is { } minimum && log.Severity < minimum) return;

        try
        {
            var recorder = Resolve();
            if (recorder is null && TraceIndex is null) return;

            if (Factory.FromLog(log, recorder ?? _indexRecorder.Value) is not { } e)
            {
                recorder?.CountDropped("filtered");
                return;
            }

            if (recorder is null)
            {
                if (e.IsFailure) TraceIndex!.Add(e);
                return;
            }

            Record(recorder, e);
        }
        catch
        {
            // Never break logging.
        }
    }

    /// <summary>A span the pipeline accepted (after privacy and processors).</summary>
    public void OnSpan(BelemetrySpan span, bool relayed)
    {
        if (Enabled is false || relayed || IsSuppressed) return;

        // The framework's own activities arrive through the hub's listener, exported or not.
        if (span.Scope.Name.StartsWith(BelemetryDiagnosticEventFactory.ComponentsSource, StringComparison.Ordinal)) return;

        try
        {
            var recorder = Resolve();
            if (recorder is null)
            {
                // Nobody's span (a request's, a background job's) is indexed by trace only when it failed, as nobody's logs
                // are: converting and indexing every request of every user, under one lock, would cost the whole server for
                // the rare capture that looks a trace up.
                if (TraceIndex is null || BelemetryDiagnosticEventFactory.IsFailure(span) is false) return;
                if (Factory.FromSpan(span, _indexRecorder.Value) is { } indexed) TraceIndex.Add(indexed);
                return;
            }

            if (Factory.FromSpan(span, recorder) is { } e) Record(recorder, e);
        }
        catch
        {
            // Never break tracing.
        }
    }

    // Numbers the events of the trace index, which belong to no session.
    private readonly Lazy<BelemetryDiagnosticRecorder> _indexRecorder = new(() => new BelemetryDiagnosticRecorder(new BelemetryDiagnosticsOptions()));

    /// <summary>Records an event and checks the automatic capture triggers.</summary>
    public void Record(BelemetryDiagnosticRecorder recorder, BelemetryDiagnosticEvent e)
    {
        if ((Options.Categories & e.Category) == 0)
        {
            recorder.CountDropped("category_off");
            return;
        }

        recorder.Add(e);

        if (e.Exception is { } exception)
        {
            recorder.CountException(exception, new BelemetryExceptionOccurrence(
                e.Sequence, BelemetryTime.ToDateTime(e.TimeUnixNano), Factory.Release, e.TraceId, e.Route, e.ComponentType, exception.Message, exception.Origin));
        }

        if (TriggerOf(e) is { } reason) ScheduleAutomaticCapture(recorder, e, reason);
    }

    /// <summary>The trigger an event fires, if any.</summary>
    internal string? TriggerOf(BelemetryDiagnosticEvent e)
    {
        var triggers = Options.AutoCaptureTriggers;
        if (triggers == BelemetryDiagnosticTriggers.None) return null;

        if (e.Exception is { } exception)
        {
            if ((triggers & BelemetryDiagnosticTriggers.Critical) != 0 && exception.Critical) return "critical-exception";
            if ((triggers & BelemetryDiagnosticTriggers.UnhandledExceptions) != 0 && exception.Origin == "unhandled") return "unhandled-exception";
            if ((triggers & BelemetryDiagnosticTriggers.ErrorBoundary) != 0 && exception.Origin == "error-boundary") return "error-boundary";
            if ((triggers & BelemetryDiagnosticTriggers.CircuitFailures) != 0 && exception.Origin == "circuit") return "circuit-failure";
            if ((triggers & BelemetryDiagnosticTriggers.BrowserErrors) != 0 && exception.Origin == "browser") return "browser-error";
            if ((triggers & BelemetryDiagnosticTriggers.AllExceptions) != 0) return "exception";
            return null;
        }

        if ((triggers & BelemetryDiagnosticTriggers.CircuitFailures) != 0 && e.Category == BelemetryDiagnosticCategories.Circuit && e.Outcome == BelemetryDiagnosticOutcome.Failure) return "circuit-failure";
        return null;
    }

    /// <summary>Starts an automatic capture after <see cref="BelemetryDiagnosticsOptions.PostTriggerWindow"/>, within the recorder's limits.</summary>
    public void ScheduleAutomaticCapture(BelemetryDiagnosticRecorder recorder, BelemetryDiagnosticEvent trigger, string reason)
    {
        var key = trigger.Exception?.Fingerprint ?? (reason + ":" + trigger.Name);
        if (recorder.TryBeginAutomaticCapture(key, DateTimeOffset.UtcNow, out var generation) is false) return;

        // Its own flow: the code that failed must not wait for it, and nothing the capture does is recorded.
        using (Suppress())
        {
            _ = RunAutomaticCaptureAsync(recorder, new BelemetryCaptureTrigger(BelemetryCaptureTriggerKind.Automatic, reason, trigger.Sequence, trigger.Exception?.Fingerprint), generation);
        }
    }

    private async Task RunAutomaticCaptureAsync(BelemetryDiagnosticRecorder recorder, BelemetryCaptureTrigger trigger, long generation)
    {
        BelemetryDiagnosticCapture? capture = null;
        try
        {
            if (Options.PostTriggerWindow > TimeSpan.Zero) await Task.Delay(Options.PostTriggerWindow);

            var options = new BelemetryDiagnosticCaptureOptions { Reason = trigger.Reason };
            capture = recorder.Owner is { IsDisposed: false } owner
                ? await owner.CaptureForTriggerAsync(options, trigger)
                : await new BelemetryDiagnosticCaptureBuilder(this, recorder, session: null).BuildAsync(options, trigger, CancellationToken.None);
        }
        catch (Exception ex)
        {
            recorder.CountDropped("automatic_capture_failed");
            BelemetryDiagnosticsService.LogFailure(_services, ex, "An automatic diagnostic capture failed.");
            capture = null;
        }
        finally
        {
            recorder.EndAutomaticCapture(capture is { Succeeded: true } ? capture : null, generation);
        }
    }

    private void OnActivityStarted(Activity activity)
    {
        if (_settings.IsServer is false || IsSuppressed) return;

        // Tagged with the circuit it started on, so work that leaves the renderer's context under it is still attributed.
        // Weakly: the activity flows into timers and tasks started under it, which must not keep the circuit's timeline alive.
        if (SynchronizationContext.Current is { } context && _byContext.TryGetValue(context, out var recorder) && recorder.IsClosed is false)
        {
            activity.SetCustomProperty(RecorderProperty, new WeakReference<BelemetryDiagnosticRecorder>(recorder));
        }
    }

    private void OnActivityStopped(Activity activity)
    {
        if (IsSuppressed) return;

        try
        {
            var recorder = Global
                           ?? (activity.GetCustomProperty(RecorderProperty) is WeakReference<BelemetryDiagnosticRecorder> tag && tag.TryGetTarget(out var tagged) ? tagged : null)
                           ?? Resolve();
            if (recorder is null || recorder.IsClosed) return;

            // A tool's own UI (BevTools' panels) handles events through the app's renderer: its activity, not the app's.
            if (activity.GetTagItem(BelemetryDiagnosticEventFactory.ComponentTypeTag) is string type && IsExcludedComponent(type)) return;

            if (Factory.FromFrameworkActivity(activity, recorder) is { } e && CanRecordFor(e.Feature)) Record(recorder, e);
        }
        catch
        {
            // Never break the renderer.
        }
    }

    /// <summary>Whether a tool registered <paramref name="componentType"/> as its own (<see cref="IBelemetryDiagnosticComponentFilter"/>).</summary>
    internal bool IsExcludedComponent(string componentType)
    {
        if (_componentFilters is null)
        {
            try
            {
                _componentFilters = _services.GetServices<IBelemetryDiagnosticComponentFilter>().ToArray();
            }
            catch
            {
                // Registered other than as a singleton (scope validation refuses it here): it excludes nothing.
                _componentFilters = [];
            }
        }

        foreach (var filter in _componentFilters)
        {
            try
            {
                if (filter.IsExcluded(componentType)) return true;
            }
            catch
            {
                // A filter that throws excludes nothing.
            }
        }
        return false;
    }

    private void OnRenderMeasured(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (IsSuppressed) return;

        try
        {
            var recorder = Resolve();
            if (recorder is null) return;

            foreach (var (key, value) in tags)
            {
                if (key == BelemetryDiagnosticEventFactory.ComponentTypeTag && value is string type)
                {
                    recorder.CountRender(type, measurement * 1000);
                    return;
                }
            }
        }
        catch
        {
            // Never break rendering.
        }
    }

    /// <summary>
    /// Whether the user's consent lets the timeline hold a record of <paramref name="feature"/> now, or once decided
    /// (held, then purged when not granted). A server's own records answer to the operator configuration.
    /// </summary>
    public bool CanRecordFor(BelemetryFeatures feature)
    {
        if ((_settings.ConfiguredFeatures & feature) == 0) return false;
        if (_settings.IsServer) return (_settings.Options.Server.Features & feature) != 0;
        return _consent.IsGranted(feature) || _consent.IsDecided is false;
    }

    /// <summary>Whether an event of <paramref name="feature"/> may be part of a capture now (granted, not merely pending).</summary>
    public bool MayCapture(BelemetryFeatures feature)
    {
        if (feature == BelemetryFeatures.None) return true;
        if (_settings.IsServer) return (_settings.Options.Server.Features & feature) != 0;
        return _consent.IsGranted(feature);
    }

    /// <summary>The user's grant and whether they decided (a server's own records answer to configuration instead).</summary>
    public (BelemetryFeatures Granted, bool Decided) ConsentState() =>
        _settings.IsServer ? (_settings.Options.Server.Features & _settings.ConfiguredFeatures, true) : (_consent.Granted, _consent.IsDecided);

    private void OnConsentChanged(BelemetryFeatures previous, BelemetryFeatures current)
    {
        var revoked = previous & ~current;
        if (revoked != BelemetryFeatures.None) Global?.Purge(revoked, discardCaptures: true);

        // Undecided records were held in case they were granted: the decision drops what was not.
        var notGranted = ~current & BelemetryFeatures.All & ~revoked;
        if (_consent.IsDecided && notGranted != BelemetryFeatures.None) Global?.Purge(notGranted, discardCaptures: false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        if (Enabled) _consent.Changed -= OnConsentChanged;
        _listener?.Dispose();
        _meters?.Dispose();
    }

    private sealed class Restore(bool previous) : IDisposable
    {
        public void Dispose() => _suppressed.Value = previous;
    }
}
